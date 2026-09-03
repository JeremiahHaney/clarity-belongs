using ClarityBelongs.Web.Data;
using ClarityBelongs.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

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
}

public interface IClarityEmailSender
{
    bool IsEnabled { get; }
    Task SendAsync(string recipient, string subject, string textBody, CancellationToken cancellationToken = default);
}

public sealed class SmtpClarityEmailSender(IOptions<EmailOptions> options) : IClarityEmailSender
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

        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        if (!string.IsNullOrWhiteSpace(_options.Username))
            client.Credentials = new NetworkCredential(_options.Username, _options.Password);

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromAddress, _options.FromName),
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
    ILogger<NotificationDeliveryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
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
                logger.LogError(ex, "Clarity notification delivery cycle failed");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task DeliverPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ClarityDbContext>();
        var sender = scope.ServiceProvider.GetRequiredService<IClarityEmailSender>();

        if (!sender.IsEnabled)
            return;

        var pending = await db.Notifications
            .Where(x => x.Channel == "Email" && x.Status == "Pending")
            .OrderBy(x => x.CreatedAtUtc)
            .Take(25)
            .ToListAsync(cancellationToken);

        foreach (var notification in pending)
        {
            var user = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == notification.UserId, cancellationToken);

            if (user is null || string.IsNullOrWhiteSpace(user.Email) || user.Email.EndsWith(".local", StringComparison.OrdinalIgnoreCase))
            {
                notification.Status = "Suppressed";
                notification.FailureReason = "No deliverable user email is configured.";
                continue;
            }

            try
            {
                await sender.SendAsync(
                    user.Email,
                    notification.Subject,
                    $"{notification.BodySummary}\n\nOpen My Clarity to review the evidence and history.",
                    cancellationToken);

                notification.Status = "Sent";
                notification.SentAtUtc = DateTime.UtcNow;
                notification.FailureReason = null;
            }
            catch (Exception ex)
            {
                notification.Status = "Failed";
                notification.FailedAtUtc = DateTime.UtcNow;
                notification.FailureReason = ex.Message;
                logger.LogWarning(ex, "Unable to deliver Clarity notification {NotificationId}", notification.Id);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
