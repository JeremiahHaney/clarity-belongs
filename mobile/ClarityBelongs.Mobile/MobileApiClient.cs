using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace ClarityBelongs.Mobile;

public sealed class MobileApiClient
{
    private const string TokenKey = "clarity.mobile.token";
    private readonly HttpClient _http = new()
    {
        BaseAddress = new Uri("https://claritybelongs.com/")
    };

    public async Task<bool> HasSessionAsync()
    {
        var token = await SecureStorage.Default.GetAsync(TokenKey);
        return !string.IsNullOrWhiteSpace(token);
    }

    public async Task LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync(
            "api/mobile/login",
            new MobileLoginRequest(email, password),
            cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));

        var result = await response.Content.ReadFromJsonAsync<MobileLoginResponse>(
            cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Clarity did not return a mobile session.");

        await SecureStorage.Default.SetAsync(TokenKey, result.Token);
    }

    public Task<MobileDashboardResponse> GetDashboardAsync(
        CancellationToken cancellationToken = default) =>
        GetAsync<MobileDashboardResponse>("api/mobile/dashboard", cancellationToken);

    public Task<MobileAccountResponse> GetAccountAsync(
        CancellationToken cancellationToken = default) =>
        GetAsync<MobileAccountResponse>("api/mobile/account", cancellationToken);

    public Task<MobileProductResponse[]> GetProductsAsync(
        CancellationToken cancellationToken = default) =>
        GetAsync<MobileProductResponse[]>("api/mobile/products", cancellationToken);

    public Task<MobileFollowDetailResponse> GetFollowAsync(
        long followId,
        CancellationToken cancellationToken = default) =>
        GetAsync<MobileFollowDetailResponse>($"api/mobile/follows/{followId}", cancellationToken);

    public async Task<long> CreateFollowAsync(
        MobileCreateFollowRequest request,
        CancellationToken cancellationToken = default)
    {
        using var message = await CreateRequestAsync(
            HttpMethod.Post,
            "api/mobile/follows",
            cancellationToken);
        message.Content = JsonContent.Create(request);

        using var response = await _http.SendAsync(message, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));

        var result = await response.Content.ReadFromJsonAsync<MobileCreateFollowResponse>(
            cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Clarity did not return the new watch.");

        return result.FollowId;
    }

    public Task SetPausedAsync(
        long followId,
        bool paused,
        CancellationToken cancellationToken = default) =>
        PostAsync(
            $"api/mobile/follows/{followId}/pause",
            new MobilePauseRequest(paused),
            cancellationToken);

    public Task RunNowAsync(
        long followId,
        CancellationToken cancellationToken = default) =>
        PostAsync<object?>(
            $"api/mobile/follows/{followId}/run",
            null,
            cancellationToken);

    public Task AcknowledgeAsync(
        long followId,
        long changeId,
        CancellationToken cancellationToken = default) =>
        PostAsync<object?>(
            $"api/mobile/follows/{followId}/changes/{changeId}/acknowledge",
            null,
            cancellationToken);

    public void SignOut()
    {
        SecureStorage.Default.Remove(TokenKey);
    }

    private async Task<T> GetAsync<T>(
        string path,
        CancellationToken cancellationToken)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Get,
            path,
            cancellationToken);
        using var response = await _http.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            SignOut();
            throw new UnauthorizedAccessException("Your Clarity session expired. Sign in again.");
        }

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));

        return await response.Content.ReadFromJsonAsync<T>(
            cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Clarity returned an empty response.");
    }

    private async Task PostAsync<T>(
        string path,
        T body,
        CancellationToken cancellationToken)
    {
        using var request = await CreateRequestAsync(
            HttpMethod.Post,
            path,
            cancellationToken);
        request.Content = JsonContent.Create(body);
        using var response = await _http.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            SignOut();
            throw new UnauthorizedAccessException("Your Clarity session expired. Sign in again.");
        }

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(await ReadErrorAsync(response, cancellationToken));
    }

    private async Task<HttpRequestMessage> CreateRequestAsync(
        HttpMethod method,
        string path,
        CancellationToken cancellationToken)
    {
        var token = await SecureStorage.Default.GetAsync(TokenKey);

        if (string.IsNullOrWhiteSpace(token))
            throw new UnauthorizedAccessException("Sign in to Clarity first.");

        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static async Task<string> ReadErrorAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var error = await response.Content.ReadFromJsonAsync<MobileErrorResponse>(
                cancellationToken: cancellationToken);
            if (!string.IsNullOrWhiteSpace(error?.Error))
                return error.Error;
        }
        catch
        {
        }

        return $"Clarity returned {(int)response.StatusCode} {response.ReasonPhrase}.";
    }
}

public sealed record MobileLoginRequest(string Email, string Password);
public sealed record MobileLoginResponse(string Token, string Email, string DisplayName);
public sealed record MobileErrorResponse(string Error);
public sealed record MobileDashboardResponse(
    int FollowingCount,
    int NeedsAttentionCount,
    int RecentChangeCount,
    int NotificationCount,
    IReadOnlyList<MobileFollowSummary> Follows,
    IReadOnlyList<MobileChangeSummary> Changes,
    IReadOnlyList<MobileNotificationSummary> Notifications);
public sealed record MobileFollowSummary(
    long FollowId,
    string Name,
    string MonitorType,
    string Status,
    string Importance,
    DateTime? LastCheckedAtUtc,
    DateTime NextCheckAtUtc,
    int UnacknowledgedChangeCount,
    string? LatestChangeTitle,
    string? LatestChangeSeverity);
public sealed record MobileChangeSummary(
    long FollowId,
    string FollowName,
    long ChangeId,
    DateTime DetectedAtUtc,
    string Severity,
    string Title,
    string Summary,
    bool IsAcknowledged);
public sealed record MobileNotificationSummary(
    long NotificationId,
    long FollowId,
    long ChangeId,
    string Subject,
    string BodySummary,
    string Status,
    DateTime CreatedAtUtc);
public sealed record MobileAccountResponse(
    string DisplayName,
    string Email,
    string PlanName,
    string PlanDescription,
    int ActiveFollowCount,
    int RemainingFollows,
    int MaxActiveFollows,
    int MinimumCadenceMinutes,
    int HistoryDays,
    bool EmailAlerts);
public sealed record MobileProductResponse(
    string Slug,
    string Name,
    string Family,
    string ShortDescription,
    string TargetLabel,
    string TargetPlaceholder,
    int DefaultCadenceMinutes,
    string DefaultImportance);
public sealed record MobileCreateFollowRequest(
    string ProductSlug,
    string Name,
    string Target,
    int CadenceMinutes,
    string Importance);
public sealed record MobileCreateFollowResponse(long FollowId);
public sealed record MobileFollowDetailResponse(
    long FollowId,
    string Name,
    string Target,
    string MonitorType,
    string Status,
    string Importance,
    int CheckCadenceMinutes,
    DateTime? LastCheckedAtUtc,
    DateTime NextCheckAtUtc,
    IReadOnlyList<MobileChangeSummary> Changes,
    IReadOnlyList<MobileNotificationSummary> Notifications);
public sealed record MobilePauseRequest(bool Paused);
