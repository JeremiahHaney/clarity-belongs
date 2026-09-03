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
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string PersonalPriceId { get; set; } = string.Empty;
    public string BusinessPriceId { get; set; } = string.Empty;
    public string SuccessUrl { get; set; } = "https://claritybelongs.com/account?checkout=success";
    public string CancelUrl { get; set; } = "https://claritybelongs.com/pricing?checkout=canceled";
    public string PortalReturnUrl { get; set; } = "https://claritybelongs.com/account";
}

public sealed class StripeBillingService(
    HttpClient http,
    ClarityDbContext db,
    IOptions<StripeOptions> options)
{
    private readonly StripeOptions _options = options.Value;

    public bool IsConfigured => _options.Enabled
        && !string.IsNullOrWhiteSpace(_options.SecretKey)
        && !string.IsNullOrWhiteSpace(_options.PersonalPriceId)
        && !string.IsNullOrWhiteSpace(_options.BusinessPriceId);

    public async Task<string> CreateCheckoutUrlAsync(
        AccountSession account,
        string planCode,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var priceId = GetPriceId(planCode);
        var membership = await db.Memberships
            .FirstAsync(x => x.UserId == account.UserId, cancellationToken);

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

        using var response = await http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(GetStripeError(body, response.StatusCode.ToString()));

        using var document = JsonDocument.Parse(body);
        var url = GetString(document.RootElement, "url");

        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException("Stripe did not return a Checkout URL.");

        return url;
    }

    public async Task<string> CreatePortalUrlAsync(
        AccountSession account,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();

        var membership = await db.Memberships
            .FirstAsync(x => x.UserId == account.UserId, cancellationToken);

        if (string.IsNullOrWhiteSpace(membership.StripeCustomerId))
            throw new InvalidOperationException("No Stripe billing account exists yet.");

        var fields = new[]
        {
            new KeyValuePair<string, string>("customer", membership.StripeCustomerId),
            new KeyValuePair<string, string>("return_url", _options.PortalReturnUrl)
        };

        using var request = CreateStripeRequest(
            HttpMethod.Post,
            "https://api.stripe.com/v1/billing_portal/sessions",
            new FormUrlEncodedContent(fields));

        using var response = await http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(GetStripeError(body, response.StatusCode.ToString()));

        using var document = JsonDocument.Parse(body);
        var url = GetString(document.RootElement, "url");

        if (string.IsNullOrWhiteSpace(url))
            throw new InvalidOperationException("Stripe did not return a billing portal URL.");

        return url;
    }

    public async Task HandleWebhookAsync(
        string payload,
        string signatureHeader,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.WebhookSecret))
            throw new InvalidOperationException("Stripe webhook verification is not configured.");

        VerifySignature(payload, signatureHeader);

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        var eventType = GetString(root, "type");

        if (!root.TryGetProperty("data", out var data)
            || !data.TryGetProperty("object", out var stripeObject))
        {
            return;
        }

        switch (eventType)
        {
            case "checkout.session.completed":
                await ApplyCheckoutCompletedAsync(stripeObject, cancellationToken);
                break;

            case "customer.subscription.created":
            case "customer.subscription.updated":
            case "customer.subscription.deleted":
                await ApplySubscriptionAsync(stripeObject, cancellationToken);
                break;

            case "invoice.payment_failed":
                await ApplyPaymentFailedAsync(stripeObject, cancellationToken);
                break;
        }
    }

    private async Task ApplyCheckoutCompletedAsync(
        JsonElement session,
        CancellationToken cancellationToken)
    {
        var metadata = GetMetadata(session);
        if (!TryGetLong(metadata, "user_id", out var userId))
            return;

        var membership = await db.Memberships
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        if (membership is null)
            return;

        membership.StripeCustomerId = GetString(session, "customer")
            ?? membership.StripeCustomerId;
        membership.StripeSubscriptionId = GetString(session, "subscription")
            ?? membership.StripeSubscriptionId;
        membership.PlanCode = metadata.GetValueOrDefault("plan_code")
            ?? membership.PlanCode;
        membership.Status = MembershipStatuses.Active;
        membership.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplySubscriptionAsync(
        JsonElement subscription,
        CancellationToken cancellationToken)
    {
        var subscriptionId = GetString(subscription, "id");
        var customerId = GetString(subscription, "customer");
        var metadata = GetMetadata(subscription);

        Membership? membership = null;

        if (TryGetLong(metadata, "user_id", out var userId))
        {
            membership = await db.Memberships
                .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        }

        if (membership is null && !string.IsNullOrWhiteSpace(subscriptionId))
        {
            membership = await db.Memberships
                .FirstOrDefaultAsync(x => x.StripeSubscriptionId == subscriptionId, cancellationToken);
        }

        if (membership is null && !string.IsNullOrWhiteSpace(customerId))
        {
            membership = await db.Memberships
                .FirstOrDefaultAsync(x => x.StripeCustomerId == customerId, cancellationToken);
        }

        if (membership is null)
            return;

        var priceId = GetSubscriptionPriceId(subscription);
        var stripeStatus = GetString(subscription, "status") ?? "incomplete";

        membership.StripeCustomerId = customerId ?? membership.StripeCustomerId;
        membership.StripeSubscriptionId = subscriptionId ?? membership.StripeSubscriptionId;
        membership.StripePriceId = priceId;
        membership.PlanCode = GetPlanCode(priceId, metadata.GetValueOrDefault("plan_code"));
        membership.Status = MapStatus(stripeStatus);
        membership.CancelAtPeriodEnd = GetBoolean(subscription, "cancel_at_period_end");
        membership.CurrentPeriodEndUtc = GetUnixDateTime(subscription, "current_period_end");
        membership.UpdatedAtUtc = DateTime.UtcNow;

        if (membership.Status == MembershipStatuses.Canceled)
            membership.PlanCode = MembershipPlans.Free;

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task ApplyPaymentFailedAsync(
        JsonElement invoice,
        CancellationToken cancellationToken)
    {
        var customerId = GetString(invoice, "customer");
        if (string.IsNullOrWhiteSpace(customerId))
            return;

        var membership = await db.Memberships
            .FirstOrDefaultAsync(x => x.StripeCustomerId == customerId, cancellationToken);

        if (membership is null)
            return;

        membership.Status = MembershipStatuses.PastDue;
        membership.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
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

    private void VerifySignature(string payload, string header)
    {
        var parts = header
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
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
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.WebhookSecret));
        var expected = Convert.ToHexString(
            hmac.ComputeHash(Encoding.UTF8.GetBytes(signedPayload)))
            .ToLowerInvariant();

        var valid = signatures.Any(signature => FixedTimeEquals(expected, signature));
        if (!valid)
            throw new InvalidOperationException("Stripe webhook signature verification failed.");

        var age = Math.Abs(
            DateTimeOffset.UtcNow.ToUnixTimeSeconds() - timestamp);

        if (age > 300)
            throw new InvalidOperationException("Stripe webhook timestamp is outside the allowed window.");
    }

    private string GetPriceId(string planCode)
    {
        return planCode switch
        {
            MembershipPlans.Personal when !string.IsNullOrWhiteSpace(_options.PersonalPriceId) => _options.PersonalPriceId,
            MembershipPlans.Business when !string.IsNullOrWhiteSpace(_options.BusinessPriceId) => _options.BusinessPriceId,
            _ => throw new InvalidOperationException("Choose a paid Clarity plan.")
        };
    }

    private string GetPlanCode(string? priceId, string? metadataPlan)
    {
        if (!string.IsNullOrWhiteSpace(priceId))
        {
            if (string.Equals(priceId, _options.PersonalPriceId, StringComparison.Ordinal))
                return MembershipPlans.Personal;

            if (string.Equals(priceId, _options.BusinessPriceId, StringComparison.Ordinal))
                return MembershipPlans.Business;
        }

        return metadataPlan is MembershipPlans.Personal or MembershipPlans.Business
            ? metadataPlan
            : MembershipPlans.Free;
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
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (!element.TryGetProperty("metadata", out var metadata)
            || metadata.ValueKind != JsonValueKind.Object)
        {
            return values;
        }

        foreach (var property in metadata.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.String)
                values[property.Name] = property.Value.GetString() ?? string.Empty;
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

    private static string? GetString(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value))
            return null;

        return value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static bool GetBoolean(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value)
            && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            && value.GetBoolean();
    }

    private static DateTime? GetUnixDateTime(JsonElement element, string name)
    {
        if (!element.TryGetProperty(name, out var value)
            || !value.TryGetInt64(out var seconds))
        {
            return null;
        }

        return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);

        return leftBytes.Length == rightBytes.Length
            && CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }

    private static string GetStripeError(string body, string fallback)
    {
        try
        {
            using var document = JsonDocument.Parse(body);

            if (document.RootElement.TryGetProperty("error", out var error))
                return GetString(error, "message") ?? fallback;
        }
        catch (JsonException)
        {
        }

        return $"Stripe request failed: {fallback}";
    }

    private void EnsureConfigured()
    {
        if (!IsConfigured)
            throw new InvalidOperationException("Stripe billing is not configured yet.");
    }
}
