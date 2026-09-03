using ClarityBelongs.Web.Components;
using ClarityBelongs.Web.Data;
using ClarityBelongs.Web.Domain;
using ClarityBelongs.Web.Observation;
using ClarityBelongs.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "clarity.session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
    });

builder.Services.AddAuthorization();

builder.Services.AddDbContext<ClarityDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("Clarity")
            ?? "Data Source=clarity.db"));

builder.Services.Configure<EmailOptions>(
    builder.Configuration.GetSection("Email"));
builder.Services.Configure<StripeOptions>(
    builder.Configuration.GetSection("Stripe"));

builder.Services.AddSingleton<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddSingleton<PlanCatalog>();
builder.Services.AddScoped<DatabaseSchemaService>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<CurrentAccountService>();
builder.Services.AddScoped<MembershipService>();
builder.Services.AddHttpClient<StripeBillingService>();

builder.Services.AddHttpClient<HttpObservationAdapter>();
builder.Services.AddHttpClient<DomainObservationAdapter>();
builder.Services.AddHttpClient<DnsRecordObservationAdapter>();
builder.Services.AddScoped<PublicEndpointGuard>();
builder.Services.AddScoped<IObservationAdapter>(sp =>
    sp.GetRequiredService<HttpObservationAdapter>());
builder.Services.AddScoped<IObservationAdapter, TlsObservationAdapter>();
builder.Services.AddScoped<IObservationAdapter, DnsObservationAdapter>();
builder.Services.AddScoped<IObservationAdapter>(sp =>
    sp.GetRequiredService<DnsRecordObservationAdapter>());
builder.Services.AddScoped<IObservationAdapter>(sp =>
    sp.GetRequiredService<DomainObservationAdapter>());

builder.Services.AddScoped<ObservationEngine>();
builder.Services.AddScoped<MyClarityService>();
builder.Services.AddScoped<FollowManagementService>();
builder.Services.AddSingleton<ClarityProductCatalog>();
builder.Services.AddSingleton<IClarityEmailSender, SmtpClarityEmailSender>();
builder.Services.AddHostedService<ObservationWorker>();
builder.Services.AddHostedService<ExpirationAlertWorker>();
builder.Services.AddHostedService<NotificationDeliveryWorker>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ClarityDbContext>();
    await db.Database.EnsureCreatedAsync();

    var schema = scope.ServiceProvider.GetRequiredService<DatabaseSchemaService>();
    await schema.UpgradeAsync();
}

app.MapGet("/health", () => Results.Ok(new
{
    service = "Clarity Belongs",
    status = "Healthy",
    utc = DateTime.UtcNow
}));

app.MapPost(
    "/auth/signup",
    async (
        HttpContext context,
        AccountService accounts,
        CancellationToken cancellationToken) =>
    {
        if (!IsSameOrigin(context.Request))
            return Results.BadRequest();

        var form = await context.Request.ReadFormAsync(cancellationToken);
        var email = form["email"].ToString();
        var displayName = form["displayName"].ToString();
        var password = form["password"].ToString();

        try
        {
            var user = await accounts.CreateAsync(
                email,
                displayName,
                password,
                cancellationToken);

            await context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                accounts.CreatePrincipal(user),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
                });

            return Results.Redirect("/");
        }
        catch (InvalidOperationException ex)
        {
            var code = ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase)
                ? "exists"
                : ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase)
                    ? "password"
                    : "email";

            return Results.Redirect($"/signup?error={code}");
        }
    })
    .DisableAntiforgery();

app.MapPost(
    "/auth/login",
    async (
        HttpContext context,
        AccountService accounts,
        CancellationToken cancellationToken) =>
    {
        if (!IsSameOrigin(context.Request))
            return Results.BadRequest();

        var form = await context.Request.ReadFormAsync(cancellationToken);
        var email = form["email"].ToString();
        var password = form["password"].ToString();
        var returnUrl = SafeLocalReturnUrl(form["returnUrl"].ToString());

        AppUser? user;

        try
        {
            user = await accounts.ValidateCredentialsAsync(
                email,
                password,
                cancellationToken);
        }
        catch (InvalidOperationException)
        {
            user = null;
        }

        if (user is null)
        {
            return Results.Redirect(
                $"/login?error=invalid&returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            accounts.CreatePrincipal(user),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            });

        return Results.Redirect(returnUrl);
    })
    .DisableAntiforgery();

app.MapPost(
    "/auth/logout",
    async (HttpContext context) =>
    {
        if (!IsSameOrigin(context.Request))
            return Results.BadRequest();

        await context.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Redirect("/login");
    })
    .DisableAntiforgery();

app.MapPost(
    "/auth/forgot-password",
    async (
        HttpContext context,
        AccountService accounts,
        CancellationToken cancellationToken) =>
    {
        if (!IsSameOrigin(context.Request))
            return Results.BadRequest();

        var form = await context.Request.ReadFormAsync(cancellationToken);
        var email = form["email"].ToString();
        var baseUrl = $"{context.Request.Scheme}://{context.Request.Host}";

        try
        {
            await accounts.RequestPasswordResetAsync(
                email,
                baseUrl,
                cancellationToken);
        }
        catch
        {
        }

        return Results.Redirect("/forgot-password?sent=1");
    })
    .DisableAntiforgery();

app.MapPost(
    "/auth/reset-password",
    async (
        HttpContext context,
        AccountService accounts,
        CancellationToken cancellationToken) =>
    {
        if (!IsSameOrigin(context.Request))
            return Results.BadRequest();

        var form = await context.Request.ReadFormAsync(cancellationToken);
        var token = form["token"].ToString();
        var password = form["password"].ToString();

        try
        {
            var reset = await accounts.ResetPasswordAsync(
                token,
                password,
                cancellationToken);

            return reset
                ? Results.Redirect("/login")
                : Results.Redirect($"/reset-password?error=invalid&token={Uri.EscapeDataString(token)}");
        }
        catch (InvalidOperationException)
        {
            return Results.Redirect($"/reset-password?error=password&token={Uri.EscapeDataString(token)}");
        }
    })
    .DisableAntiforgery();

app.MapPost(
    "/billing/checkout/{planCode}",
    async (
        string planCode,
        HttpContext context,
        CurrentAccountService currentAccount,
        StripeBillingService stripe,
        CancellationToken cancellationToken) =>
    {
        if (!IsSameOrigin(context.Request))
            return Results.BadRequest();

        var account = await currentAccount.RequireAsync(cancellationToken);
        var normalizedPlan = planCode.Equals("business", StringComparison.OrdinalIgnoreCase)
            ? MembershipPlans.Business
            : MembershipPlans.Personal;

        var url = await stripe.CreateCheckoutUrlAsync(
            account,
            normalizedPlan,
            cancellationToken);

        return Results.Redirect(url);
    })
    .DisableAntiforgery()
    .RequireAuthorization();

app.MapPost(
    "/billing/portal",
    async (
        HttpContext context,
        CurrentAccountService currentAccount,
        StripeBillingService stripe,
        CancellationToken cancellationToken) =>
    {
        if (!IsSameOrigin(context.Request))
            return Results.BadRequest();

        var account = await currentAccount.RequireAsync(cancellationToken);
        var url = await stripe.CreatePortalUrlAsync(
            account,
            cancellationToken);
        return Results.Redirect(url);
    })
    .DisableAntiforgery()
    .RequireAuthorization();

app.MapPost(
    "/webhooks/stripe",
    async (
        HttpContext context,
        StripeBillingService stripe,
        CancellationToken cancellationToken) =>
    {
        using var reader = new StreamReader(
            context.Request.Body,
            Encoding.UTF8);
        var payload = await reader.ReadToEndAsync(cancellationToken);
        var signature = context.Request.Headers["Stripe-Signature"].ToString();

        try
        {
            await stripe.HandleWebhookAsync(
                payload,
                signature,
                cancellationToken);
            return Results.Ok();
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    })
    .DisableAntiforgery();

app.MapPost(
    "/api/follows",
    async (
        CreateFollowRequest request,
        CurrentAccountService currentAccount,
        FollowManagementService follows,
        CancellationToken cancellationToken) =>
    {
        var account = await currentAccount.RequireAsync(cancellationToken);
        var followId = await follows.CreateAsync(
            account.UserId,
            account.WorkspaceId,
            new CreateFollowInput(
                request.Name,
                request.Target,
                request.TargetType,
                request.MonitorType,
                request.AdapterType,
                request.SourceConfigurationJson,
                request.Importance,
                request.CheckCadenceMinutes,
                request.AlertRuleType),
            cancellationToken);

        return Results.Created(
            $"/api/follows/{followId}",
            new
            {
                Id = followId
            });
    })
    .RequireAuthorization();

app.MapPost(
    "/api/follows/{id:long}/run",
    async (
        long id,
        CurrentAccountService currentAccount,
        FollowManagementService follows,
        ObservationEngine engine,
        CancellationToken cancellationToken) =>
    {
        var account = await currentAccount.RequireAsync(cancellationToken);
        var owned = await follows.GetAsync(
            account.WorkspaceId,
            id,
            cancellationToken);

        if (owned is null)
            return Results.NotFound();

        await engine.RunFollowAsync(id, cancellationToken);
        return Results.Accepted();
    })
    .RequireAuthorization();

app.MapPost(
    "/api/changes/{followId:long}/{changeId:long}/acknowledge",
    async (
        long followId,
        long changeId,
        CurrentAccountService currentAccount,
        FollowManagementService follows,
        CancellationToken cancellationToken) =>
    {
        var account = await currentAccount.RequireAsync(cancellationToken);
        await follows.AcknowledgeAsync(
            account.WorkspaceId,
            followId,
            changeId,
            cancellationToken);
        return Results.NoContent();
    })
    .RequireAuthorization();

app.Run();

static bool IsSameOrigin(HttpRequest request)
{
    var expected = $"{request.Scheme}://{request.Host}";
    var origin = request.Headers.Origin.ToString();

    if (!string.IsNullOrWhiteSpace(origin))
        return string.Equals(origin.TrimEnd('/'), expected, StringComparison.OrdinalIgnoreCase);

    var referer = request.Headers.Referer.ToString();
    return Uri.TryCreate(referer, UriKind.Absolute, out var uri)
        && string.Equals(uri.Scheme, request.Scheme, StringComparison.OrdinalIgnoreCase)
        && string.Equals(uri.Authority, request.Host.Value, StringComparison.OrdinalIgnoreCase);
}

static string SafeLocalReturnUrl(string value)
{
    if (string.IsNullOrWhiteSpace(value)
        || !value.StartsWith('/')
        || value.StartsWith("//", StringComparison.Ordinal))
    {
        return "/";
    }

    return value;
}

public sealed record CreateFollowRequest(
    string Name,
    string Target,
    string TargetType = "Website",
    string MonitorType = "WebsiteChange",
    string AdapterType = AdapterTypes.Http,
    string SourceConfigurationJson = "{\"mode\":\"content\"}",
    string Importance = "Normal",
    int CheckCadenceMinutes = 360,
    string AlertRuleType = "AnyMeaningfulChange");
