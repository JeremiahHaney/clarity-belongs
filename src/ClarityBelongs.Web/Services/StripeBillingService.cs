using ClarityBelongs.Web.Data;
using ClarityBelongs.Web.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ClarityBelongs.Web.Services;

public sealed class StripeOptions
{
    public bool Enabled { get; set; }
    public bool PublicPaidPlansEnabled { get; set; }
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string PersonalPriceId { get; set; } = string.Empty;
    public string BusinessPriceId { get; set; } = string.Empty;
    public string PersonalDisplayPrice { get; set; } = string.Empty;
    public string BusinessDisplayPrice { get; set; } = string.Empty;
    public string SuccessUrl { get; set; } = "https://claritybelongs.com/account?checkout=success";
    public string CancelUrl { get; set; } = "https://claritybelongs.com/pricing?checkout=canceled";
    public string PortalReturnUrl { get; set; } = "https://claritybelongs.com/account";
}

public static class StripeConfiguration
{
    public static bool IsConfigured(StripeOptions options) =>
        options.Enabled
        && TryValidate(options, out _);

    public static bool IsPublicReady(StripeOptions options) =>
        IsConfigured(options)
        && options.PublicPaidPlansEnabled
        && !string.IsNullOrWhiteSpace(options.PersonalDisplayPrice)
        && !string.IsNullOrWhiteSpace(options.BusinessDisplayPrice);

    public static bool TryValidate(
        StripeOptions options,
        out string reason)
    {
        reason = string.Empty;

        if (options.PublicPaidPlansEnabled && !options.Enabled)
        {
            reason = "Public paid plans require Stripe:Enabled.";
            return false;
        }

        if (!options.Enabled)
            return true;

        if (string.IsNullOrWhiteSpace(options.SecretKey)
            || string.IsNullOrWhiteSpace(options.WebhookSecret)
            || string.IsNullOrWhiteSpace(options.PersonalPriceId)
            || string.IsNullOrWhiteSpace(options.BusinessPriceId))
        {
            reason = "Stripe keys, webhook secret, and paid price IDs are required.";
            return false;
        }

        if (!TryHttpsUrl(options.SuccessUrl)
            || !TryHttpsUrl(options.CancelUrl)
            || !TryHttpsUrl(options.PortalReturnUrl))
        {
            reason = "Stripe redirect URLs must be absolute HTTPS URLs.";
            return false;
        }

        if (options.PublicPaidPlansEnabled
            && (string.IsNullOrWhiteSpace(options.PersonalDisplayPrice)
                || string.IsNullOrWhiteSpace(options.BusinessDisplayPrice)))
        {
            reason = "Public paid plans require approved display prices.";
            return false;
        }

        return true;
    }

    public static string GetDisplayPrice(
        StripeOptions options,
        string planCode)
    {
        return planCode switch
        {
            MembershipPlans.Free => "$0",
            MembershipPlans.Personal when IsPublicReady(options) => options.PersonalDisplayPrice.Trim(),
            MembershipPlans.Business when IsPublicReady(options) => options.BusinessDisplayPrice.Trim(),
            _ => string.Empty
        };
    }

    private static bool TryHttpsUrl(string value)
    {
        return Uri.TryCreate(
                value,
                UriKind.Absolute,
                out var uri)
            && string.Equals(
                uri.Scheme,
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class StripeBillingService(
    HttpClient http,
    ClarityDbContext db,
    PlanCatalog plans,
    IOptions<StripeOptions> options,
    ILogger<StripeBillingService> logger)
{
    private readonly StripeOptions _options = options.Value;

    public bool IsConfigured => StripeConfiguration.IsConfigured(_options);
    public bool IsPublicBillingEnabled => StripeConfiguration.IsPublicReady(_options);

    public async Task<string> CreateCheckoutUrlAsync(
        AccountSession account,
        string planCode,
        CancellationToken cancellationToken = default)
    {
        EnsurePublicBillingEnabled();

        var priceId = GetPriceId(planCode);
        var membership = await db.Memberships
            .FirstAsync(
                x => x.UserId == account.UserId,
                cancellationToken);

        var fields = new List<KeyValuePair<string, string>>
        {
            new("mode", "subscription"),
            new("success_url", _options.SuccessUrl),
            new("cancel_url", _options.CancelUrl),
            new("client_reference_id", account.UserId.ToString(CultureInfo.InvariantCulture)),
            new("line_items[0][price]", priceId),
            new("line_items[0][quantity]", "1"),
            new("metadata[user_id]", account.UserId.ToString(CultureInfo.InvariantCulture)),
            new("metadata[workspace_id]", account.WorkspaceId.ToString(CultureInfo.InvariantCulture)),
            new("metadata[plan_code]", planCode),
            new("subscription_data[metadata][user_id]", account.UserId.ToString(CultureInfo.InvariantCulture)),
            new("subscription_data[metadata][workspace_id]", account.WorkspaceId.ToString(CultureInfo.InvariantCulture)),
            new("subscription_data[metadata][plan_code]", planCode),
            new("allow_promotion_codes", "true")
        };

        if (!string.IsNullOrWhiteSpace(membership.StripeCustomerId))
            fields.Add(new("customer", membership.StripeCustomerId));
        else
            fields.Add(new("customer_email", account.Email));

        using var request = CreateStripeRequest(
            HttpMethod.Post,
            "https://api.stripe.com/v1/checkout/sessions",
            new FormUrlEncodedContent(fields));

        using var response = await http.SendAsync(
            request,
            cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            LogProviderFailure(
                "checkout session",
                response.StatusCode.ToString(),
                body);
            throw new InvalidOperationException(
                "Billing is temporarily unavailable. Please try again later.");
        }

        using var document = JsonDocument.Parse(body);
        var url = GetString(document.RootElement, "url");

        if (string.IsNullOrWhiteSpace(url))
        {
            logger.LogError("Stripe checkout response did not include a URL.");
            throw new InvalidOperationException(
                "Billing is temporarily unavailable. Please try again later.");
        }

        return url;
    }

    public async Task<string> CreatePortalUrlAsync(
        AccountSession account,
        CancellationToken cancellationToken = default)
    {
        EnsurePublicBillingEnabled();

        var membership = await db.Memberships
            .FirstAsync(
                x => x.UserId == account.UserId,
                cancellationToken);

        if (string.IsNullOrWhiteSpace(membership.StripeCustomerId))
        {
            throw new InvalidOperationException(
                "No billing account is available for this membership.");
        }

        var fields = new[]
        {
            new KeyValuePair<string, string>(
                "customer",
                membership.StripeCustomerId),
            new KeyValuePair<string, string>(
                "return_url",
                _options.PortalReturnUrl)
        };

        using var request = CreateStripeRequest(
            HttpMethod.Post,
            "https://api.stripe.com/v1/billing_portal/sessions",
            new FormUrlEncodedContent(fields));

        using var response = await http.SendAsync(
            request,
            cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            LogProviderFailure(
                "billing portal session",
                response.StatusCode.ToString(),
                body);
            throw new InvalidOperationException(
                "Billing is temporarily unavailable. Please try again later.");
        }

        using var document = JsonDocument.Parse(body);
        var url = GetString(document.RootElement, "url");

        if (string.IsNullOrWhiteSpace(url))
        {
            logger.LogError("Stripe portal response did not include a URL.");
            throw new InvalidOperationException(
                "Billing is temporarily unavailable. Please try again later.");
        }

        return url;
    }

    public async Task HandleWebhookAsync(
        string payload,
        string signatureHeader,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Stripe webhook verification is unavailable.");

        VerifySignature(payload, signatureHeader);

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        var eventId = GetString(root, "id");
        var eventType = GetString(root, "type");
        var eventCreatedUtc = GetRequiredUnixDateTime(root, "created");

        if (string.IsNullOrWhiteSpace(eventId)
            || string.IsNullOrWhiteSpace(eventType))
        {
            throw new InvalidOperationException("Stripe event metadata is incomplete.");
        }

        if (await db.StripeWebhookEvents
            .AsNoTracking()
            .AnyAsync(
                x => x.EventId == eventId,
                cancellationToken))
        {
            return;
        }

        if (!root.TryGetProperty("data", out var data)
            || !data.TryGetProperty("object", out var stripeObject))
        {
            throw new InvalidOperationException("Stripe event payload is incomplete.");
        }

        await using var transaction = await db.Database
            .BeginTransactionAsync(cancellationToken);

        if (await db.StripeWebhookEvents
            .AnyAsync(
                x => x.EventId == eventId,
                cancellationToken))
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }

        switch (eventType)
        {
            case "checkout.session.completed":
                await ApplyCheckoutCompletedAsync(
                    stripeObject,
                    eventCreatedUtc,
                    cancellationToken);
                break;

            case "customer.subscription.created":
            case "customer.subscription.updated":
            case "customer.subscription.deleted":
                await ApplySubscriptionAsync(
                    stripeObject,
                    eventCreatedUtc,
                    cancellationToken);
                break;

            case "invoice.payment_failed":
                await ApplyPaymentFailedAsync(
                    stripeObject,
                    eventCreatedUtc,
                    cancellationToken);
                break;
        }

        db.StripeWebhookEvents.Add(new StripeWebhookEvent
        {
            EventId = eventId,
            EventType = eventType,
            StripeCreatedUtc = eventCreatedUtc,
            ProcessedAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task ApplyCheckoutCompletedAsync(
        JsonElement session,
        DateTime eventCreatedUtc,
        CancellationToken cancellationToken)
    {
        var metadata = GetMetadata(session);
        if (!TryGetLong(metadata, "user_id", out var userId))
            return;

        var membership = await db.Memberships
            .FirstOrDefaultAsync(
                x => x.UserId == userId,
                cancellationToken);

        if (membership is null
            || IsOlderThanAppliedEvent(membership, eventCreatedUtc))
        {
            return;
        }

        membership.StripeCustomerId = GetString(session, "customer")
            ?? membership.StripeCustomerId;
        membership.StripeSubscriptionId = GetString(session, "subscription")
            ?? membership.StripeSubscriptionId;
        membership.PlanCode = GetCheckoutPlanCode(
            metadata.GetValueOrDefault("plan_code"));
        membership.Status = membership.PlanCode == MembershipPlans.Free
            ? MembershipStatuses.Incomplete
            : MembershipStatuses.Active;
        membership.LastStripeEventCreatedUtc = eventCreatedUtc;
        membership.UpdatedAtUtc = DateTime.UtcNow;
        await NormalizeEntitlementsAsync(
            membership,
            cancellationToken);
    }

    private async Task ApplySubscriptionAsync(
        JsonElement subscription,
        DateTime eventCreatedUtc,
        CancellationToken cancellationToken)
    {
        var subscriptionId = GetString(subscription, "id");
        var customerId = GetString(subscription, "customer");
        var metadata = GetMetadata(subscription);

        Membership? membership = null;

        if (TryGetLong(metadata, "user_id", out var userId))
        {
            membership = await db.Memberships
                .FirstOrDefaultAsync(
                    x => x.UserId == userId,
                    cancellationToken);
        }

        if (membership is null
            && !string.IsNullOrWhiteSpace(subscriptionId))
        {
            membership = await db.Memberships
                .FirstOrDefaultAsync(
                    x => x.StripeSubscriptionId == subscriptionId,
                    cancellationToken);
        }

        if (membership is null
            && !string.IsNullOrWhiteSpace(customerId))
        {
            membership = await db.Memberships
                .FirstOrDefaultAsync(
                    x => x.StripeCustomerId == customerId,
                    cancellationToken);
        }

        if (membership is null
            || IsOlderThanAppliedEvent(membership, eventCreatedUtc))
        {
            return;
        }

        var priceId = GetSubscriptionPriceId(subscription);
        var stripeStatus = GetString(subscription, "status") ?? "incomplete";

        membership.StripeCustomerId = customerId
            ?? membership.StripeCustomerId;
        membership.StripeSubscriptionId = subscriptionId
            ?? membership.StripeSubscriptionId;
        membership.StripePriceId = priceId;
        membership.PlanCode = GetPlanCode(priceId);
        membership.Status = MapStatus(stripeStatus);
        membership.CancelAtPeriodEnd = GetBoolean(
            subscription,
            "cancel_at_period_end");
        membership.CurrentPeriodEndUtc = GetUnixDateTime(
            subscription,
            "current_period_end");
        membership.LastStripeEventCreatedUtc = eventCreatedUtc;
        membership.UpdatedAtUtc = DateTime.UtcNow;

        if (!MembershipService.IsPaidActive(membership))
            membership.PlanCode = MembershipPlans.Free;

        await NormalizeEntitlementsAsync(
            membership,
            cancellationToken);
    }

    private async Task ApplyPaymentFailedAsync(
        JsonElement invoice,
        DateTime eventCreatedUtc,
        CancellationToken cancellationToken)
    {
        var customerId = GetString(invoice, "customer");
        if (string.IsNullOrWhiteSpace(customerId))
            return;

        var membership = await db.Memberships
            .FirstOrDefaultAsync(
                x => x.StripeCustomerId == customerId,
                cancellationToken);

        if (membership is null
            || IsOlderThanAppliedEvent(membership, eventCreatedUtc))
        {
            return;
        }

        membership.Status = MembershipStatuses.PastDue;
        membership.PlanCode = MembershipPlans.Free;
        membership.LastStripeEventCreatedUtc = eventCreatedUtc;
        membership.UpdatedAtUtc = DateTime.UtcNow;
        await NormalizeEntitlementsAsync(
            membership,
            cancellationToken);
    }

    private async Task NormalizeEntitlementsAsync(
        Membership membership,
        CancellationToken cancellationToken)
    {
        var effectivePlan = MembershipService.GetEffectivePlan(
            membership,
            plans);
        var follows = await db.Follows
            .Where(x => x.WorkspaceId == membership.WorkspaceId)
            .Where(x => x.Status != FollowStatuses.Archived)
            .Where(x => x.CheckCadenceMinutes < effectivePlan.MinimumCadenceMinutes)
            .ToListAsync(cancellationToken);

        foreach (var follow in follows)
        {
            follow.CheckCadenceMinutes = effectivePlan.MinimumCadenceMinutes;
            follow.UpdatedAtUtc = DateTime.UtcNow;
        }
    }

    private HttpRequestMessage CreateStripeRequest(
        HttpMethod method,
        string url,
        HttpContent content)
    {
        var request = new HttpRequestMessage(method, url)
        {
            Content = content
        };

        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _options.SecretKey);
        return request;
    }

    private void VerifySignature(
        string payload,
        string header)
    {
        var parts = header
            .Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries
                    | StringSplitOptions.TrimEntries)
            .Select(x => x.Split('=', 2))
            .Where(x => x.Length == 2)
            .ToLookup(x => x[0], x => x[1]);

        var timestampText = parts["t"].FirstOrDefault();
        var signatures = parts["v1"].ToArray();

        if (!long.TryParse(timestampText, out var timestamp)
            || signatures.Length == 0)
        {
            throw new InvalidOperationException("Invalid Stripe signature header.");
        }

        var signedPayload = $"{timestamp}.{payload}";
        using var hmac = new HMACSHA256(
            Encoding.UTF8.GetBytes(_options.WebhookSecret));
        var expected = Convert.ToHexString(
                hmac.ComputeHash(
                    Encoding.UTF8.GetBytes(signedPayload)))
            .ToLowerInvariant();

        var valid = signatures.Any(
            signature => FixedTimeEquals(expected, signature));

        if (!valid)
        {
            throw new InvalidOperationException(
                "Stripe webhook signature verification failed.");
        }

        var age = Math.Abs(
            DateTimeOffset.UtcNow.ToUnixTimeSeconds() - timestamp);

        if (age > 300)
        {
            throw new InvalidOperationException(
                "Stripe webhook timestamp is outside the allowed window.");
        }
    }

    private string GetPriceId(string planCode)
    {
        return planCode switch
        {
            MembershipPlans.Personal when !string.IsNullOrWhiteSpace(_options.PersonalPriceId) => _options.PersonalPriceId,
            MembershipPlans.Business when !string.IsNullOrWhiteSpace(_options.BusinessPriceId) => _options.BusinessPriceId,
            _ => throw new InvalidOperationException("Choose an available Clarity plan.")
        };
    }

    private string GetCheckoutPlanCode(string? metadataPlan)
    {
        return metadataPlan switch
        {
            MembershipPlans.Personal when !string.IsNullOrWhiteSpace(_options.PersonalPriceId) => MembershipPlans.Personal,
            MembershipPlans.Business when !string.IsNullOrWhiteSpace(_options.BusinessPriceId) => MembershipPlans.Business,
            _ => MembershipPlans.Free
        };
    }

    private string GetPlanCode(string? priceId)
    {
        if (string.Equals(
            priceId,
            _options.PersonalPriceId,
            StringComparison.Ordinal))
        {
            return MembershipPlans.Personal;
        }

        if (string.Equals(
            priceId,
            _options.BusinessPriceId,
            StringComparison.Ordinal))
        {
            return MembershipPlans.Business;
        }

        return MembershipPlans.Free;
    }

    private static string MapStatus(string status)
    {
        return status switch
        {
            "active" => MembershipStatuses.Active,
            "trialing" => MembershipStatuses.Trialing,
            "past_due" or "unpaid" => MembershipStatuses.PastDue,
            "canceled" => MembershipStatuses.Canceled,
            _ => MembershipStatuses.Incomplete
        };
    }

    private static bool IsOlderThanAppliedEvent(
        Membership membership,
        DateTime eventCreatedUtc)
    {
        return membership.LastStripeEventCreatedUtc.HasValue
            && membership.LastStripeEventCreatedUtc.Value > eventCreatedUtc;
    }

    private static string? GetSubscriptionPriceId(JsonElement subscription)
    {
        if (!subscription.TryGetProperty("items", out var items)
            || !items.TryGetProperty("data", out var data)
            || data.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        var first = data.EnumerateArray().FirstOrDefault();
        if (first.ValueKind == JsonValueKind.Undefined
            || !first.TryGetProperty("price", out var price))
        {
            return null;
        }

        return GetString(price, "id");
    }

    private static Dictionary<string, string> GetMetadata(JsonElement element)
    {
        var values = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);

        if (!element.TryGetProperty("metadata", out var metadata)
            || metadata.ValueKind != JsonValueKind.Object)
        {
            return values;
        }

        foreach (var property in metadata.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.String)
            {
                values[property.Name] = property.Value.GetString()
                    ?? string.Empty;
            }
        }

        return values;
    }

    private static bool TryGetLong(
        IReadOnlyDictionary<string, string> values,
        string key,
        out long value)
    {
        value = 0;
        return values.TryGetValue(key, out var raw)
            && long.TryParse(raw, out value);
    }

    private static string? GetString(
        JsonElement element,
        string name)
    {
        if (!element.TryGetProperty(name, out var value))
            return null;

        return value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static bool GetBoolean(
        JsonElement element,
        string name)
    {
        return element.TryGetProperty(name, out var value)
            && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            && value.GetBoolean();
    }

    private static DateTime? GetUnixDateTime(
        JsonElement element,
        string name)
    {
        if (!element.TryGetProperty(name, out var value)
            || !value.TryGetInt64(out var seconds))
        {
            return null;
        }

        return DateTimeOffset
            .FromUnixTimeSeconds(seconds)
            .UtcDateTime;
    }

    private static DateTime GetRequiredUnixDateTime(
        JsonElement element,
        string name)
    {
        return GetUnixDateTime(element, name)
            ?? throw new InvalidOperationException(
                "Stripe event timestamp is missing.");
    }

    private static bool FixedTimeEquals(
        string left,
        string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);

        return leftBytes.Length == rightBytes.Length
            && CryptographicOperations.FixedTimeEquals(
                leftBytes,
                rightBytes);
    }

    private void LogProviderFailure(
        string operation,
        string status,
        string body)
    {
        logger.LogError(
            "Stripe {Operation} failed with status {StripeStatus}. Provider detail: {StripeError}",
            operation,
            status,
            GetStripeError(body, status));
    }

    private static string GetStripeError(
        string body,
        string fallback)
    {
        try
        {
            using var document = JsonDocument.Parse(body);

            if (document.RootElement.TryGetProperty("error", out var error))
            {
                return GetString(error, "message")
                    ?? fallback;
            }
        }
        catch (JsonException)
        {
        }

        return fallback;
    }

    private void EnsurePublicBillingEnabled()
    {
        if (!IsPublicBillingEnabled)
        {
            throw new InvalidOperationException(
                "Paid plans are not available in the current launch.");
        }
    }
}
