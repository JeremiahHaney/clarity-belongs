using System.Collections.Concurrent;
using System.Net;

namespace ClarityBelongs.Web.Services;

public sealed class SecurityThrottle
{
    private readonly ConcurrentDictionary<string, WindowCounter> _windows = new();

    public bool TryAcquire(
        string bucket,
        string key,
        int permitLimit,
        TimeSpan window,
        out TimeSpan retryAfter)
    {
        var now = DateTime.UtcNow;
        var normalizedKey = string.IsNullOrWhiteSpace(key)
            ? "unknown"
            : key.Trim().ToLowerInvariant();
        var counter = _windows.GetOrAdd(
            $"{bucket}:{normalizedKey}",
            _ => new WindowCounter(now.Add(window)));

        lock (counter)
        {
            if (counter.ExpiresAtUtc <= now)
            {
                counter.Count = 0;
                counter.ExpiresAtUtc = now.Add(window);
            }

            if (counter.Count >= permitLimit)
            {
                retryAfter = counter.ExpiresAtUtc - now;
                return false;
            }

            counter.Count++;
            retryAfter = TimeSpan.Zero;
            return true;
        }
    }

    public static string ClientKey(HttpContext context)
    {
        return context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";
    }

    private sealed class WindowCounter(DateTime expiresAtUtc)
    {
        public int Count { get; set; }
        public DateTime ExpiresAtUtc { get; set; } = expiresAtUtc;
    }
}

public sealed class LoginAttemptProtector
{
    private const int FailureLimit = 6;
    private static readonly TimeSpan FailureWindow = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(5);
    private readonly ConcurrentDictionary<string, LoginState> _states = new();

    public bool CanAttempt(
        string email,
        out TimeSpan retryAfter)
    {
        var key = Normalize(email);
        var now = DateTime.UtcNow;

        if (!_states.TryGetValue(key, out var state))
        {
            retryAfter = TimeSpan.Zero;
            return true;
        }

        lock (state)
        {
            if (state.LockedUntilUtc > now)
            {
                retryAfter = state.LockedUntilUtc.Value - now;
                return false;
            }

            if (state.WindowStartedUtc.Add(FailureWindow) <= now)
            {
                state.Failures = 0;
                state.WindowStartedUtc = now;
                state.LockedUntilUtc = null;
            }

            retryAfter = TimeSpan.Zero;
            return true;
        }
    }

    public void RecordFailure(string email)
    {
        var key = Normalize(email);
        var now = DateTime.UtcNow;
        var state = _states.GetOrAdd(
            key,
            _ => new LoginState(now));

        lock (state)
        {
            if (state.WindowStartedUtc.Add(FailureWindow) <= now)
            {
                state.Failures = 0;
                state.WindowStartedUtc = now;
                state.LockedUntilUtc = null;
            }

            state.Failures++;

            if (state.Failures >= FailureLimit)
                state.LockedUntilUtc = now.Add(LockoutDuration);
        }
    }

    public void RecordSuccess(string email)
    {
        _states.TryRemove(Normalize(email), out _);
    }

    private static string Normalize(string value) =>
        (value ?? string.Empty).Trim().ToLowerInvariant();

    private sealed class LoginState(DateTime windowStartedUtc)
    {
        public int Failures { get; set; }
        public DateTime WindowStartedUtc { get; set; } = windowStartedUtc;
        public DateTime? LockedUntilUtc { get; set; }
    }
}

public static class SameOriginRequestValidator
{
    public static bool IsAllowed(HttpRequest request)
    {
        var expected = $"{request.Scheme}://{request.Host}";
        var origin = request.Headers.Origin.ToString();

        if (!string.IsNullOrWhiteSpace(origin))
        {
            return string.Equals(
                origin.TrimEnd('/'),
                expected,
                StringComparison.OrdinalIgnoreCase);
        }

        var referer = request.Headers.Referer.ToString();
        return Uri.TryCreate(referer, UriKind.Absolute, out var uri)
            && string.Equals(
                uri.Scheme,
                request.Scheme,
                StringComparison.OrdinalIgnoreCase)
            && string.Equals(
                uri.Authority,
                request.Host.Value,
                StringComparison.OrdinalIgnoreCase);
    }
}
