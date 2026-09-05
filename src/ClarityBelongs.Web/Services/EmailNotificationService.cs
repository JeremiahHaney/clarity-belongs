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
    public bool PublicDeliveryEnabled { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "alerts@claritybelongs.com";
    public string FromName { get; set; } = "Clarity Belongs";
    public string ReplyToAddress { get; set; } = string.Empty;
    public string DeliveryMode { get; set; } = "Immediate";
    public int DigestHourUtc { get; set; } = 15;
    public int MaxDeliveryAttempts { get; set; } = 5;
    public int RetryBaseSeconds { get; set; } = 60;
    public int RetryMaxSeconds { get; set; } = 3600;
}

public static class EmailConfiguration
{
    public static bool IsPublicReady(EmailOptions options) =>
        options.Enabled
        && options.PublicDeliveryEnabled
        && TryValidate(options, out _);

    public static bool TryValidate(
        EmailOptions options,
        out string reason)
    {
        reason = string.Empty;

        if (options.PublicDeliveryEnabled && !options.Enabled)
        {
            reason = "Public email delivery requires Email:Enabled.";
            return false;
        }

        if (!options.Enabled)
            return true;

        if (string.IsNullOrWhiteSpace(options.Host))
        {
            reason = "Email host is required.";
            return false;
        }

        if (options.Port is < 1 or > 65535)
        {
            reason = "Email port must be between 1 and 65535.";
            return false;
        }

        if (!TryMailAddress(options.FromAddress))
        {
            reason = "Email from address is invalid.";
            return false;
        }

        if (!string.IsNullOrWhiteSpace(options.ReplyToAddress)
            && !TryMailAddress(options.ReplyToAddress))
        {
            reason = "Email reply-to address is invalid.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(options.FromName)
            || options.FromName.Length > 100)
        {
            reason = "Email from name is required and must be at most 100 characters.";
            return false;
        }

        var usernameConfigured = !string.IsNullOrWhiteSpace(options.Username);
        var passwordConfigured = !string.IsNullOrWhiteSpace(options.Password);

        if (usernameConfigured != passwordConfigured)
        {
            reason = "Email username and password must be configured together.";
            return false;
        }

        if (options.PublicDeliveryEnabled && !options.EnableSsl)
        {
            reason = "Public email delivery requires TLS/SSL.";
            return false;
        }

        if (!string.Equals(
                options.DeliveryMode,
                "Immediate",
                StringComparison.OrdinalIgnoreCase)
            && !string.Equals(
                options.DeliveryMode,
                "DailyDigest",
                StringComparison.OrdinalIgnoreCase))
        {
            reason = "Email delivery mode must be Immediate or DailyDigest.";
            return false;
        }

        if (options.DigestHourUtc is < 0 or > 23)
        {
            reason = "Digest hour must be between 0 and 23 UTC.";
            return false;
        }

        if (options.MaxDeliveryAttempts is < 1 or > 10)
        {
            reason = "Email delivery attempts must be between 1 and 10.";
            return false;
        }

        if (options.RetryBaseSeconds is < 1 or > 3600
            || options.RetryMaxSeconds < options.RetryBaseSeconds
            || options.RetryMaxSeconds > 86400)
        {
            reason = "Email retry bounds are invalid.";
            return false;
        }

        return true;
    }

    private static bool TryMailAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            var parsed = new MailAddress(value);
            return string.Equals(
                parsed.Address,
                value.Trim(),
                StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }
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
    IOptions<EmailOptions> options,
    ILogger<SmtpClarityEmailSender> logger) : IClarityEmailSender
{
    private readonly EmailOptions _options = options.Value;

    public bool IsEnabled => EmailConfiguration.IsPublicReady(_options);

    public async Task SendAsync(
        string recipient,
        string subject,
        string textBody,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled)
            throw new InvalidOperationException("Clarity email delivery is unavailable.");

        if (!EmailConfiguration.TryValidate(_options, out var reason))
            throw new InvalidOperationException(reason);

        using var client = new SmtpClient(
            _options.Host,
            _options.Port)
        {
            EnableSsl = _options.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
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
                SanitizeHeader(_options.FromName, 100)),
            Subject = SanitizeHeader(subject, 160),
            Body = SanitizeBody(textBody),
            IsBodyHtml = false
        };

        if (!string.IsNullOrWhiteSpace(_options.ReplyToAddress))
            message.ReplyToList.Add(new MailAddress(_options.ReplyToAddress));

        message.To.Add(new MailAddress(recipient));
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (Exception ex) when (ex is SmtpException or InvalidOperationException)
        {
            logger.LogWarning(
                ex,
                "SMTP delivery failed for recipient domain {RecipientDomain}.",
                GetDomain(recipient));
            throw;
        }
    }

    internal static string SanitizeHeader(
        string value,
        int maxLength)
    {
        var sanitized = new string(
            (value ?? string.Empty)
                .Where(character => character is not '\r' and not '\n' && !char.IsControl(character))
                .ToArray())
            .Trim();

        return sanitized.Length <= maxLength
            ? sanitized
            : sanitized[..maxLength];
    }

    internal static string SanitizeBody(string value)
    {
        var builder = new StringBuilder();

        foreach (var character in value ?? string.Empty)
        {
            if (character == '\0')
                continue;

            if (char.IsControl(character)
                && character is not '\r' and not '\n' and not '\t')
            {
                continue;
            }

            builder.Append(character);

            if (builder.Length >= 12000)
                break;
        }

        return builder.ToString().Trim();
    }

    private static string GetDomain(string recipient)
    {
        var at = recipient.LastIndexOf('@');
        return at >= 0 && at < recipient.Length - 1
            ? recipient[(at + 1)..]
            : "unknown";
    }
}

public static class NotificationDeliveryCoordinator
{
    private static readonly TimeSpan SendingLease = TimeSpan.FromMinutes(15);

    public static async Task DeliverAsync(
        ClarityDbContext db,
        IClarityEmailSender sender,
        EmailOptions options,
        PlanCatalog plans,
        ILogger logger,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        await RecoverStaleClaimsAsync(
            db,
            utcNow,
            cancellationToken);

        if (!sender.IsEnabled)
        {
            if (!options.PublicDeliveryEnabled)
            {
                await SuppressLaunchDisabledAsync(
                    db,
                    cancellationToken);
            }

            return;
        }

        if (string.Equals(
            options.DeliveryMode,
            "DailyDigest",
            StringComparison.OrdinalIgnoreCase))
        {
            await DeliverDigestAsync(
                db,
                sender,
                options,
                plans,
                logger,
                utcNow,
                cancellationToken);
            return;
        }

        await DeliverImmediateAsync(
            db,
            sender,
            options,
            plans,
            logger,
            utcNow,
            cancellationToken);
    }

    private static async Task DeliverImmediateAsync(
        ClarityDbContext db,
        IClarityEmailSender sender,
        EmailOptions options,
        PlanCatalog plans,
        ILogger logger,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var ids = await EligibleNotifications(db, options, utcNow)
            .OrderBy(x => x.CreatedAtUtc)
            .Select(x => x.Id)
            .Take(25)
            .ToListAsync(cancellationToken);

        foreach (var id in ids)
        {
            if (!await TryClaimAsync(
                    db,
                    id,
                    options,
                    utcNow,
                    cancellationToken))
            {
                continue;
            }

            var notification = await db.Notifications
                .FirstAsync(x => x.Id == id, cancellationToken);
            var account = await GetDeliveryAccountAsync(
                db,
                notification.UserId,
                plans,
                cancellationToken);

            if (account is null
                || !account.Plan.EmailAlerts)
            {
                Suppress(
                    notification,
                    "Email delivery is not included for this account or plan.");
                await db.SaveChangesAsync(cancellationToken);
                continue;
            }

            try
            {
                await sender.SendAsync(
                    account.User.Email,
                    notification.Subject,
                    $"{notification.BodySummary}\n\nOpen My Clarity to review the evidence and history.",
                    cancellationToken);

                MarkSent(notification, utcNow);
            }
            catch (Exception ex)
            {
                MarkFailed(
                    notification,
                    options,
                    logger,
                    utcNow,
                    ex);
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task DeliverDigestAsync(
        ClarityDbContext db,
        IClarityEmailSender sender,
        EmailOptions options,
        PlanCatalog plans,
        ILogger logger,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (utcNow.Hour < options.DigestHourUtc)
            return;

        var userIds = await EligibleNotifications(db, options, utcNow)
            .Select(x => x.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var userId in userIds)
        {
            var account = await GetDeliveryAccountAsync(
                db,
                userId,
                plans,
                cancellationToken);

            if (account is null
                || !account.Plan.EmailAlerts
                || !account.Plan.DailyDigest)
            {
                await SuppressUserNotificationsAsync(
                    db,
                    userId,
                    options,
                    utcNow,
                    "Daily digest delivery is not included for this account or plan.",
                    cancellationToken);
                continue;
            }

            var digestDate = DateTime.SpecifyKind(
                utcNow.Date,
                DateTimeKind.Utc);
            var state = await db.DigestDeliveryStates
                .FirstOrDefaultAsync(
                    x => x.UserId == userId
                        && x.DigestDateUtc == digestDate,
                    cancellationToken);

            if (state?.Status == DigestDeliveryStatuses.Completed)
                continue;

            if (state?.Status == DigestDeliveryStatuses.Started
                && state.LastAttemptAtUtc > utcNow - SendingLease)
            {
                continue;
            }

            if (state?.Status == DigestDeliveryStatuses.Failed
                && state.NextAttemptAtUtc > utcNow)
            {
                continue;
            }

            if (state is null)
            {
                state = new DigestDeliveryState
                {
                    UserId = userId,
                    DigestDateUtc = digestDate
                };
                db.DigestDeliveryStates.Add(state);

                try
                {
                    await db.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException)
                {
                    db.Entry(state).State = EntityState.Detached;
                    state = await db.DigestDeliveryStates
                        .FirstAsync(
                            x => x.UserId == userId
                                && x.DigestDateUtc == digestDate,
                            cancellationToken);

                    if (state.Status == DigestDeliveryStatuses.Completed
                        || state.LastAttemptAtUtc > utcNow - SendingLease)
                    {
                        continue;
                    }
                }
            }

            state.Status = DigestDeliveryStatuses.Started;
            state.AttemptCount++;
            state.LastAttemptAtUtc = utcNow;
            state.NextAttemptAtUtc = null;
            state.LastError = null;
            await db.SaveChangesAsync(cancellationToken);

            var ids = await EligibleNotifications(db, options, utcNow)
                .Where(x => x.UserId == userId)
                .OrderBy(x => x.CreatedAtUtc)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);
            var claimed = new List<Notification>();

            foreach (var id in ids)
            {
                if (!await TryClaimAsync(
                        db,
                        id,
                        options,
                        utcNow,
                        cancellationToken))
                {
                    continue;
                }

                claimed.Add(await db.Notifications
                    .FirstAsync(x => x.Id == id, cancellationToken));
            }

            if (claimed.Count == 0)
            {
                state.Status = DigestDeliveryStatuses.Completed;
                state.CompletedAtUtc = utcNow;
                await db.SaveChangesAsync(cancellationToken);
                continue;
            }

            try
            {
                await sender.SendAsync(
                    account.User.Email,
                    $"Clarity: {claimed.Count} thing{(claimed.Count == 1 ? string.Empty : "s")} worth knowing",
                    BuildDigest(claimed),
                    cancellationToken);

                foreach (var notification in claimed)
                    MarkSent(notification, utcNow);

                state.Status = DigestDeliveryStatuses.Completed;
                state.CompletedAtUtc = utcNow;
                state.NextAttemptAtUtc = null;
                state.LastError = null;
            }
            catch (Exception ex)
            {
                foreach (var notification in claimed)
                {
                    MarkFailed(
                        notification,
                        options,
                        logger,
                        utcNow,
                        ex);
                }

                state.Status = DigestDeliveryStatuses.Failed;
                state.NextAttemptAtUtc = utcNow + RetryDelay(
                    state.AttemptCount,
                    options);
                state.LastError = SanitizeError(ex);
            }

            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static IQueryable<Notification> EligibleNotifications(
        ClarityDbContext db,
        EmailOptions options,
        DateTime utcNow)
    {
        return db.Notifications
            .Where(x => x.Channel == "Email")
            .Where(x => x.Status == NotificationStatuses.Pending
                || x.Status == NotificationStatuses.Failed)
            .Where(x => x.AttemptCount < options.MaxDeliveryAttempts)
            .Where(x => x.NextAttemptAtUtc == null
                || x.NextAttemptAtUtc <= utcNow);
    }

    private static async Task<bool> TryClaimAsync(
        ClarityDbContext db,
        long notificationId,
        EmailOptions options,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var updated = await db.Notifications
            .Where(x => x.Id == notificationId)
            .Where(x => x.Channel == "Email")
            .Where(x => x.Status == NotificationStatuses.Pending
                || x.Status == NotificationStatuses.Failed)
            .Where(x => x.AttemptCount < options.MaxDeliveryAttempts)
            .Where(x => x.NextAttemptAtUtc == null
                || x.NextAttemptAtUtc <= utcNow)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        x => x.Status,
                        NotificationStatuses.Sending)
                    .SetProperty(
                        x => x.AttemptCount,
                        x => x.AttemptCount + 1)
                    .SetProperty(
                        x => x.LastAttemptAtUtc,
                        utcNow)
                    .SetProperty(
                        x => x.NextAttemptAtUtc,
                        (DateTime?)null),
                cancellationToken);

        return updated == 1;
    }

    private static async Task RecoverStaleClaimsAsync(
        ClarityDbContext db,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var staleBefore = utcNow - SendingLease;

        await db.Notifications
            .Where(x => x.Channel == "Email")
            .Where(x => x.Status == NotificationStatuses.Sending)
            .Where(x => x.LastAttemptAtUtc == null
                || x.LastAttemptAtUtc <= staleBefore)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        x => x.Status,
                        NotificationStatuses.Failed)
                    .SetProperty(
                        x => x.FailedAtUtc,
                        utcNow)
                    .SetProperty(
                        x => x.NextAttemptAtUtc,
                        utcNow)
                    .SetProperty(
                        x => x.FailureReason,
                        "A previous email delivery attempt did not complete."),
                cancellationToken);
    }

    private static async Task SuppressLaunchDisabledAsync(
        ClarityDbContext db,
        CancellationToken cancellationToken)
    {
        await db.Notifications
            .Where(x => x.Channel == "Email")
            .Where(x => x.Status == NotificationStatuses.Pending
                || x.Status == NotificationStatuses.Failed
                || x.Status == NotificationStatuses.Sending)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        x => x.Status,
                        NotificationStatuses.Suppressed)
                    .SetProperty(
                        x => x.FailureReason,
                        "Email delivery is not enabled for the current launch."),
                cancellationToken);
    }

    private static async Task SuppressUserNotificationsAsync(
        ClarityDbContext db,
        long userId,
        EmailOptions options,
        DateTime utcNow,
        string reason,
        CancellationToken cancellationToken)
    {
        await EligibleNotifications(db, options, utcNow)
            .Where(x => x.UserId == userId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        x => x.Status,
                        NotificationStatuses.Suppressed)
                    .SetProperty(
                        x => x.FailureReason,
                        reason),
                cancellationToken);
    }

    private static async Task<DeliveryAccount?> GetDeliveryAccountAsync(
        ClarityDbContext db,
        long userId,
        PlanCatalog plans,
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

        return new DeliveryAccount(
            user,
            plans.Get(membership.PlanCode));
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
            builder.AppendLine(SmtpClarityEmailSender.SanitizeHeader(
                notification.Subject,
                160));
            builder.Append("  ");
            builder.AppendLine(SmtpClarityEmailSender.SanitizeBody(
                notification.BodySummary));
            builder.AppendLine();
        }

        builder.AppendLine("Open My Clarity to review history and evidence.");
        return builder.ToString();
    }

    private static void Suppress(
        Notification notification,
        string reason)
    {
        notification.Status = NotificationStatuses.Suppressed;
        notification.FailureReason = reason;
        notification.NextAttemptAtUtc = null;
    }

    private static void MarkSent(
        Notification notification,
        DateTime utcNow)
    {
        notification.Status = NotificationStatuses.Sent;
        notification.SentAtUtc = utcNow;
        notification.FailedAtUtc = null;
        notification.FailureReason = null;
        notification.NextAttemptAtUtc = null;
        notification.DeadLetterAtUtc = null;
    }

    private static void MarkFailed(
        Notification notification,
        EmailOptions options,
        ILogger logger,
        DateTime utcNow,
        Exception ex)
    {
        notification.FailedAtUtc = utcNow;
        notification.FailureReason = SanitizeError(ex);

        if (notification.AttemptCount >= options.MaxDeliveryAttempts)
        {
            notification.Status = NotificationStatuses.DeadLetter;
            notification.DeadLetterAtUtc = utcNow;
            notification.NextAttemptAtUtc = null;
        }
        else
        {
            notification.Status = NotificationStatuses.Failed;
            notification.NextAttemptAtUtc = utcNow + RetryDelay(
                notification.AttemptCount,
                options);
        }

        logger.LogWarning(
            ex,
            "Email delivery attempt {AttemptCount} failed for notification {NotificationId}; next state {NotificationStatus}.",
            notification.AttemptCount,
            notification.Id,
            notification.Status);
    }

    internal static TimeSpan RetryDelay(
        int attemptCount,
        EmailOptions options)
    {
        var exponent = Math.Clamp(attemptCount - 1, 0, 16);
        var multiplier = Math.Pow(2, exponent);
        var seconds = Math.Min(
            options.RetryMaxSeconds,
            options.RetryBaseSeconds * multiplier);
        return TimeSpan.FromSeconds(seconds);
    }

    internal static string SanitizeError(Exception ex)
    {
        var message = SmtpClarityEmailSender.SanitizeHeader(
            ex.Message,
            360);
        var value = $"{ex.GetType().Name}: {message}";
        return value.Length <= 400
            ? value
            : value[..400];
    }

    private sealed record DeliveryAccount(
        AppUser User,
        PlanDefinition Plan);
}

public sealed class NotificationDeliveryWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<EmailOptions> options,
    ILogger<NotificationDeliveryWorker> logger) : BackgroundService
{
    private readonly EmailOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ClarityDbContext>();
                var sender = scope.ServiceProvider.GetRequiredService<IClarityEmailSender>();
                var plans = scope.ServiceProvider.GetRequiredService<PlanCatalog>();

                await NotificationDeliveryCoordinator.DeliverAsync(
                    db,
                    sender,
                    _options,
                    plans,
                    logger,
                    DateTime.UtcNow,
                    stoppingToken);
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
}
