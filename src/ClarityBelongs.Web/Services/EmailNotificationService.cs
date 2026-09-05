using ClarityBelongs.Web.Data;
using ClarityBelongs.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace ClarityBelongs.Web.Services;

public sealed class EmailOptions
{
    public bool Enabled { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "alerts@claritybelongs.com";
    public string FromName { get; set; } = "Clarity Belongs";
    public string DeliveryMode { get; set; } = "Immediate";
    public int DigestHourUtc { get; set; } = 15;
}

public interface IClarityEmailSender
{
    bool IsEnabled { get; }

    Task SendAsync(
        string recipient,
        string subject,
        string textBody,
        CancellationToken cancellationToken = default);
}

public sealed class SmtpClarityEmailSender(
    IOptions<EmailOptions> options) : IClarityEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public bool IsEnabled => _options.Enabled
        && !string.IsNullOrWhiteSpace(_options.Host)
        && !string.IsNullOrWhiteSpace(_options.FromAddress);

    public async Task SendAsync(
        string recipient,
        string subject,
        string textBody,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
            throw new InvalidOperationException("Clarity email delivery is not configured.");

        using var client = new SmtpClient(
            _options.Host,
            _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        if (!string.IsNullOrWhiteSpace(_options.Username))
        {
            client.Credentials = new NetworkCredential(
                _options.Username,
                _options.Password);
        }

        using var message = new MailMessage
        {
            From = new MailAddress(
                _options.FromAddress,
                _options.FromName),
            Subject = subject,
            Body = textBody,
            IsBodyHtml = false
        };

        message.To.Add(recipient);
        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, cancellationToken);
    }
}

public sealed class NotificationDeliveryWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<EmailOptions> options,
    ILogger<NotificationDeliveryWorker> logger) : BackgroundService
{
    private readonly EmailOptions _options = options.Value;
    private DateOnly? _lastDigestDateUtc;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            WorkerHealth.Registry.Mark("notification");

            try
            {
                await DeliverPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Clarity notification delivery cycle failed");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(30),
                stoppingToken);
        }
    }

    private async Task DeliverPendingAsync(
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClarityDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<IClarityEmailSender>();

        if (!sender.IsEnabled)
            return;

        if (string.Equals(
            _options.DeliveryMode,
            "DailyDigest",
            StringComparison.OrdinalIgnoreCase))
        {
            await DeliverDigestAsync(
                db,
                sender,
                cancellationToken);
            return;
        }

        await DeliverImmediateAsync(
            db,
            sender,
            cancellationToken);
    }

    private async Task DeliverImmediateAsync(
        ClarityDbContext db,
        IClarityEmailSender sender,
        CancellationToken cancellationToken)
    {
        var pending = await db.Notifications
            .Where(x => x.Channel == "Email")
            .Where(x => x.Status == "Pending")
            .OrderBy(x => x.CreatedAtUtc)
            .Take(25)
            .ToListAsync(cancellationToken);

        foreach (var notification in pending)
        {
            var user = await GetDeliverableUserAsync(
                db,
                notification.UserId,
                cancellationToken);

            if (user is null)
            {
                Suppress(notification);
                continue;
            }

            try
            {
                await sender.SendAsync(
                    user.Email,
                    notification.Subject,
                    $"{notification.BodySummary}\n\nOpen My Clarity to review the evidence and history.",
                    cancellationToken);

                MarkSent(notification);
            }
            catch (Exception ex)
            {
                MarkFailed(notification, ex);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task DeliverDigestAsync(
        ClarityDbContext db,
        IClarityEmailSender sender,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        if (now.Hour < Math.Clamp(_options.DigestHourUtc, 0, 23)
            || _lastDigestDateUtc == today)
        {
            return;
        }

        var pending = await db.Notifications
            .Where(x => x.Channel == "Email")
            .Where(x => x.Status == "Pending")
            .OrderBy(x => x.UserId)
            .ThenBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        foreach (var userGroup in pending.GroupBy(x => x.UserId))
        {
            var user = await GetDeliverableUserAsync(
                db,
                userGroup.Key,
                cancellationToken);

            if (user is null)
            {
                foreach (var notification in userGroup)
                    Suppress(notification);

                continue;
            }

            var items = userGroup.ToList();
            var body = BuildDigest(items);

            try
            {
                await sender.SendAsync(
                    user.Email,
                    $"Clarity: {items.Count} thing{(items.Count == 1 ? string.Empty : "s")} worth knowing",
                    body,
                    cancellationToken);

                foreach (var notification in items)
                    MarkSent(notification);
            }
            catch (Exception ex)
            {
                foreach (var notification in items)
                    MarkFailed(notification, ex);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        _lastDigestDateUtc = today;
    }

    private static async Task<AppUser?> GetDeliverableUserAsync(
        ClarityDbContext db,
        long userId,
        CancellationToken cancellationToken)
    {
        var membership = await db.Memberships
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.UserId == userId,
                cancellationToken);

        if (membership is null
            || !MembershipService.IsPaidActive(membership))
        {
            return null;
        }

        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == userId,
                cancellationToken);

        if (user is null
            || string.IsNullOrWhiteSpace(user.Email)
            || user.Email.EndsWith(
                ".local",
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return user;
    }

    private static string BuildDigest(
        IReadOnlyList<Notification> notifications)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Here is what changed while you were not looking.");
        builder.AppendLine();

        foreach (var notification in notifications)
        {
            builder.Append("- ");
            builder.AppendLine(notification.Subject);
            builder.Append("  ");
            builder.AppendLine(notification.BodySummary);
            builder.AppendLine();
        }

        builder.AppendLine("Open My Clarity to review history and evidence.");
        return builder.ToString();
    }

    private static void Suppress(Notification notification)
    {
        notification.Status = "Suppressed";
        notification.FailureReason = "Email delivery is unavailable for this account or plan.";
    }

    private static void MarkSent(Notification notification)
    {
        notification.Status = "Sent";
        notification.SentAtUtc = DateTime.UtcNow;
        notification.FailedAtUtc = null;
        notification.FailureReason = null;
    }

    private void MarkFailed(
        Notification notification,
        Exception ex)
    {
        notification.Status = "Failed";
        notification.FailedAtUtc = DateTime.UtcNow;
        notification.FailureReason = ex.Message;

        logger.LogWarning(
            ex,
            "Unable to deliver Clarity notification {NotificationId}",
            notification.Id);
    }
}
