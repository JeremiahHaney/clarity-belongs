using System.Globalization;
using System.Text;
using ClarityBelongs.Web.Data;
using ClarityBelongs.Web.Domain;
using ClarityBelongs.Web.Observation;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;

namespace ClarityBelongs.Web.Services;

public sealed record MobileSession(
    long UserId,
    long WorkspaceId,
    DateTime ExpiresAtUtc);

public sealed class MobileSessionTokenService(
    IDataProtectionProvider dataProtection)
{
    private readonly IDataProtector _protector =
        dataProtection.CreateProtector("ClarityBelongs.Mobile.Session.v1");

    public string Issue(
        long userId,
        long workspaceId)
    {
        var expiresAtUtc = DateTime.UtcNow.AddDays(30);
        var payload = string.Join(
            "|",
            userId.ToString(CultureInfo.InvariantCulture),
            workspaceId.ToString(CultureInfo.InvariantCulture),
            expiresAtUtc.Ticks.ToString(CultureInfo.InvariantCulture));

        return _protector.Protect(payload);
    }

    public bool TryRead(
        string? token,
        out MobileSession session)
    {
        session = default!;

        if (string.IsNullOrWhiteSpace(token))
            return false;

        try
        {
            var payload = _protector.Unprotect(token);
            var parts = payload.Split('|');

            if (parts.Length != 3
                || !long.TryParse(
                    parts[0],
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var userId)
                || !long.TryParse(
                    parts[1],
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var workspaceId)
                || !long.TryParse(
                    parts[2],
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var ticks))
            {
                return false;
            }

            var expiresAtUtc = new DateTime(
                ticks,
                DateTimeKind.Utc);

            if (expiresAtUtc <= DateTime.UtcNow)
                return false;

            session = new MobileSession(
                userId,
                workspaceId,
                expiresAtUtc);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public static class MobileApiEndpoints
{
    public static void MapClarityMobileApi(this WebApplication app)
    {
        var api = app.MapGroup("/api/mobile");

        api.MapPost(
            "/login",
            LoginAsync);

        api.MapGet(
            "/dashboard",
            DashboardAsync);

        api.MapGet(
            "/account",
            AccountAsync);

        api.MapGet(
            "/products",
            ProductsAsync);

        api.MapPost(
            "/follows",
            CreateFollowAsync);

        api.MapGet(
            "/follows/{followId:long}",
            FollowAsync);

        api.MapPost(
            "/follows/{followId:long}/pause",
            PauseAsync);

        api.MapPost(
            "/follows/{followId:long}/run",
            RunAsync);

        api.MapPost(
            "/follows/{followId:long}/changes/{changeId:long}/acknowledge",
            AcknowledgeAsync);
    }

    private static async Task<IResult> LoginAsync(
        MobileLoginRequest request,
        AccountService accounts,
        ClarityDbContext db,
        MobileSessionTokenService sessions,
        SecurityThrottle throttle,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        var clientKey = SecurityThrottle.ClientKey(context);

        if (!throttle.TryAcquire(
            "mobile-login",
            clientKey,
            20,
            TimeSpan.FromMinutes(5),
            out _))
        {
            return Results.Json(
                new MobileErrorResponse("Too many sign-in attempts. Try again shortly."),
                statusCode: StatusCodes.Status429TooManyRequests);
        }

        AppUser? user;

        try
        {
            user = await accounts.ValidateCredentialsAsync(
                request.Email,
                request.Password,
                cancellationToken);
        }
        catch (InvalidOperationException)
        {
            user = null;
        }

        if (user is null)
        {
            return Results.Json(
                new MobileErrorResponse("Email or password was not accepted."),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var workspace = await db.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.OwnerUserId == user.Id,
                cancellationToken);

        if (workspace is null)
        {
            return Results.Json(
                new MobileErrorResponse("Your Clarity workspace is not available."),
                statusCode: StatusCodes.Status409Conflict);
        }

        var token = sessions.Issue(
            user.Id,
            workspace.Id);

        return Results.Ok(
            new MobileLoginResponse(
                token,
                user.Email,
                user.DisplayName));
    }

    private static async Task<IResult> DashboardAsync(
        HttpContext context,
        MobileSessionTokenService sessions,
        MyClarityService clarity,
        CancellationToken cancellationToken)
    {
        if (!TryAuthorize(
            context,
            sessions,
            out var session))
        {
            return Results.Unauthorized();
        }

        var dashboard = await clarity.GetAsync(
            session.WorkspaceId,
            cancellationToken);

        return Results.Ok(
            new MobileDashboardResponse(
                dashboard.Following.Count,
                dashboard.NeedsAttention.Count,
                dashboard.RecentChanges.Count,
                dashboard.Notifications.Count,
                dashboard.Following
                    .Select(follow => new MobileFollowSummary(
                        follow.FollowId,
                        follow.Name,
                        follow.MonitorType,
                        follow.Status,
                        follow.Importance,
                        follow.LastCheckedAtUtc,
                        follow.NextCheckAtUtc,
                        follow.UnacknowledgedChangeCount,
                        follow.LatestChangeTitle,
                        follow.LatestChangeSeverity))
                    .ToArray(),
                dashboard.RecentChanges
                    .Select(change => new MobileChangeSummary(
                        change.FollowId,
                        change.FollowName,
                        change.ChangeId,
                        change.DetectedAtUtc,
                        change.Severity,
                        change.Title,
                        change.Summary,
                        change.IsAcknowledged))
                    .ToArray(),
                dashboard.Notifications
                    .Where(notification => notification.Channel == "InApp")
                    .Select(notification => new MobileNotificationSummary(
                        notification.Id,
                        notification.FollowId,
                        notification.ChangeId,
                        notification.Subject,
                        notification.BodySummary,
                        notification.Status,
                        notification.CreatedAtUtc))
                    .ToArray()));
    }

    private static async Task<IResult> AccountAsync(
        HttpContext context,
        MobileSessionTokenService sessions,
        ClarityDbContext db,
        MembershipService memberships,
        CancellationToken cancellationToken)
    {
        if (!TryAuthorize(
            context,
            sessions,
            out var session))
        {
            return Results.Unauthorized();
        }

        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == session.UserId,
                cancellationToken);

        if (user is null)
            return Results.Unauthorized();

        var summary = await memberships.GetAsync(
            session.UserId,
            session.WorkspaceId,
            cancellationToken);

        return Results.Ok(
            new MobileAccountResponse(
                user.DisplayName,
                user.Email,
                summary.Plan.Name,
                summary.Plan.Description,
                summary.ActiveFollowCount,
                summary.RemainingFollows,
                summary.Plan.MaxActiveFollows,
                summary.Plan.MinimumCadenceMinutes,
                summary.Plan.HistoryDays,
                summary.Plan.EmailAlerts));
    }

    private static IResult ProductsAsync(
        HttpContext context,
        MobileSessionTokenService sessions,
        ClarityProductCatalog catalog)
    {
        if (!TryAuthorize(
            context,
            sessions,
            out _))
        {
            return Results.Unauthorized();
        }

        var products = new PublicClarityProductCatalog(catalog)
            .GetAll()
            .OrderBy(product => product.Family)
            .ThenBy(product => product.Name)
            .Select(product => new MobileProductResponse(
                product.Slug,
                product.Name,
                product.Family,
                product.ShortDescription,
                product.TargetLabel,
                product.TargetPlaceholder,
                product.DefaultCadenceMinutes,
                product.DefaultImportance))
            .ToArray();

        return Results.Ok(products);
    }

    private static async Task<IResult> CreateFollowAsync(
        MobileCreateFollowRequest request,
        HttpContext context,
        MobileSessionTokenService sessions,
        ClarityProductCatalog catalog,
        FollowManagementService follows,
        PublicEndpointGuard endpointGuard,
        CancellationToken cancellationToken)
    {
        if (!TryAuthorize(
            context,
            sessions,
            out var session))
        {
            return Results.Unauthorized();
        }

        var product = new PublicClarityProductCatalog(catalog)
            .GetBySlug(request.ProductSlug);

        if (product is null)
        {
            return Results.BadRequest(
                new MobileErrorResponse("Choose an available Clarity watch."));
        }

        var name = string.IsNullOrWhiteSpace(request.Name)
            ? product.Name
            : request.Name.Trim();
        var target = request.Target?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(target))
        {
            return Results.BadRequest(
                new MobileErrorResponse("Enter the public target to watch."));
        }

        try
        {
            await ValidateTargetAsync(
                product,
                target,
                endpointGuard,
                cancellationToken);

            var followId = await follows.CreateAsync(
                session.UserId,
                session.WorkspaceId,
                new CreateFollowInput(
                    name,
                    target,
                    product.TargetType,
                    product.MonitorType,
                    product.AdapterType,
                    product.SourceConfigurationJson,
                    string.IsNullOrWhiteSpace(request.Importance)
                        ? product.DefaultImportance
                        : request.Importance,
                    request.CadenceMinutes > 0
                        ? request.CadenceMinutes
                        : product.DefaultCadenceMinutes,
                    product.DefaultAlertRule),
                cancellationToken);

            return Results.Ok(
                new MobileCreateFollowResponse(followId));
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(
                new MobileErrorResponse(ex.Message));
        }
    }

    private static async Task<IResult> FollowAsync(
        long followId,
        HttpContext context,
        MobileSessionTokenService sessions,
        FollowManagementService follows,
        CancellationToken cancellationToken)
    {
        if (!TryAuthorize(
            context,
            sessions,
            out var session))
        {
            return Results.Unauthorized();
        }

        var model = await follows.GetAsync(
            session.WorkspaceId,
            followId,
            cancellationToken);

        if (model is null)
            return Results.NotFound();

        return Results.Ok(
            new MobileFollowDetailResponse(
                model.Follow.Id,
                model.Follow.Name,
                model.Target.PrimaryUri,
                model.Follow.MonitorType,
                model.Follow.Status,
                model.Follow.Importance,
                model.Follow.CheckCadenceMinutes,
                model.Follow.LastCheckedAtUtc,
                model.Follow.NextCheckAtUtc,
                model.Changes
                    .Select(change => new MobileChangeSummary(
                        change.FollowId,
                        change.FollowName,
                        change.ChangeId,
                        change.DetectedAtUtc,
                        change.Severity,
                        change.Title,
                        change.Summary,
                        change.IsAcknowledged))
                    .ToArray(),
                model.Notifications
                    .Where(notification => notification.Channel == "InApp")
                    .Select(notification => new MobileNotificationSummary(
                        notification.Id,
                        notification.FollowId,
                        notification.ChangeId,
                        notification.Subject,
                        notification.BodySummary,
                        notification.Status,
                        notification.CreatedAtUtc))
                    .ToArray()));
    }

    private static async Task<IResult> PauseAsync(
        long followId,
        MobilePauseRequest request,
        HttpContext context,
        MobileSessionTokenService sessions,
        FollowManagementService follows,
        CancellationToken cancellationToken)
    {
        if (!TryAuthorize(
            context,
            sessions,
            out var session))
        {
            return Results.Unauthorized();
        }

        await follows.SetPausedAsync(
            session.WorkspaceId,
            followId,
            request.Paused,
            cancellationToken);

        return Results.NoContent();
    }

    private static async Task<IResult> RunAsync(
        long followId,
        HttpContext context,
        MobileSessionTokenService sessions,
        FollowManagementService follows,
        ObservationEngine engine,
        CancellationToken cancellationToken)
    {
        if (!TryAuthorize(
            context,
            sessions,
            out var session))
        {
            return Results.Unauthorized();
        }

        var follow = await follows.GetAsync(
            session.WorkspaceId,
            followId,
            cancellationToken);

        if (follow is null)
            return Results.NotFound();

        await engine.RunFollowAsync(
            followId,
            cancellationToken);

        return Results.Accepted();
    }

    private static async Task<IResult> AcknowledgeAsync(
        long followId,
        long changeId,
        HttpContext context,
        MobileSessionTokenService sessions,
        FollowManagementService follows,
        CancellationToken cancellationToken)
    {
        if (!TryAuthorize(
            context,
            sessions,
            out var session))
        {
            return Results.Unauthorized();
        }

        await follows.AcknowledgeAsync(
            session.WorkspaceId,
            followId,
            changeId,
            cancellationToken);

        return Results.NoContent();
    }

    private static bool TryAuthorize(
        HttpContext context,
        MobileSessionTokenService sessions,
        out MobileSession session)
    {
        session = default!;
        var authorization = context.Request.Headers.Authorization.ToString();

        if (!authorization.StartsWith(
            "Bearer ",
            StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return sessions.TryRead(
            authorization["Bearer ".Length..].Trim(),
            out session);
    }

    private static async Task ValidateTargetAsync(
        ClarityProduct product,
        string target,
        PublicEndpointGuard guard,
        CancellationToken cancellationToken)
    {
        if (product.AdapterType == AdapterTypes.Http)
        {
            if (!Uri.TryCreate(
                target,
                UriKind.Absolute,
                out var uri))
            {
                throw new InvalidOperationException(
                    "Enter a valid public URL.");
            }

            await guard.ValidateAsync(
                uri,
                cancellationToken);
            return;
        }

        if (product.AdapterType == AdapterTypes.Tls)
        {
            var uri = Uri.TryCreate(
                target,
                UriKind.Absolute,
                out var absolute)
                ? absolute
                : new Uri($"https://{target}");

            await guard.ValidateAsync(
                uri,
                cancellationToken);
            return;
        }

        if (product.AdapterType == AdapterTypes.Dns)
        {
            await guard.ValidateHostAsync(
                DnsObservationAdapter.NormalizeHost(target),
                cancellationToken);
        }
    }
}

public sealed record MobileLoginRequest(
    string Email,
    string Password);

public sealed record MobileLoginResponse(
    string Token,
    string Email,
    string DisplayName);

public sealed record MobileErrorResponse(
    string Error);

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

public sealed record MobileCreateFollowResponse(
    long FollowId);

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

public sealed record MobilePauseRequest(
    bool Paused);
