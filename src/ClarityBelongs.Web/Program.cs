using ClarityBelongs.Web.Components;
using ClarityBelongs.Web.Data;
using ClarityBelongs.Web.Domain;
using ClarityBelongs.Web.Observation;
using ClarityBelongs.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "clarity.csrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueCountLimit = 24;
    options.ValueLengthLimit = 8192;
    options.MultipartBodyLengthLimit = 16 * 1024;
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "clarity.session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
    });

builder.Services.AddAuthorization();

builder.Services.Configure<DatabaseStorageOptions>(
    builder.Configuration.GetSection("DatabaseStorage"));
builder.Services.AddSingleton<DatabasePathProvider>();
builder.Services.AddSingleton<DatabaseRuntimeState>();
builder.Services.AddSingleton<SqliteBackupService>();
builder.Services.AddDbContext<ClarityDbContext>((services, options) =>
    options.UseSqlite(
        services.GetRequiredService<DatabasePathProvider>().ConnectionString));

builder.Services
    .AddOptions<EmailOptions>()
    .Bind(builder.Configuration.GetSection("Email"))
    .Validate(
        options => !options.Enabled
            || (!string.IsNullOrWhiteSpace(options.Host)
                && !string.IsNullOrWhiteSpace(options.FromAddress)),
        "Enabled email delivery requires Email:Host and Email:FromAddress.")
    .ValidateOnStart();
builder.Services
    .AddOptions<StripeOptions>()
    .Bind(builder.Configuration.GetSection("Stripe"))
    .Validate(
        options => !options.Enabled
            || (!string.IsNullOrWhiteSpace(options.SecretKey)
                && !string.IsNullOrWhiteSpace(options.WebhookSecret)
                && !string.IsNullOrWhiteSpace(options.PersonalPriceId)
                && !string.IsNullOrWhiteSpace(options.BusinessPriceId)),
        "Enabled Stripe billing requires secret key, webhook secret, and price IDs.")
    .ValidateOnStart();

builder.Services.AddSingleton<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddSingleton<PlanCatalog>();
builder.Services.AddSingleton<SecurityThrottle>();
builder.Services.AddSingleton<LoginAttemptProtector>();
builder.Services.AddScoped<DatabaseSchemaService>();
builder.Services.AddScoped<DatabaseStartupService>();
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

var restoreArgumentIndex = Array.FindIndex(
    args,
    value => string.Equals(
        value,
        "--restore-database",
        StringComparison.OrdinalIgnoreCase));

if (restoreArgumentIndex >= 0)
{
    if (restoreArgumentIndex + 1 >= args.Length)
        throw new InvalidOperationException("--restore-database requires a backup file name.");

    using var restoreScope = app.Services.CreateScope();
    var backupService = restoreScope.ServiceProvider.GetRequiredService<SqliteBackupService>();
    await backupService.RestoreAsync(args[restoreArgumentIndex + 1]);

    var startup = restoreScope.ServiceProvider.GetRequiredService<DatabaseStartupService>();
    await startup.InitializeAsync();
    Console.WriteLine("Clarity database restore completed and startup validation passed.");
    return;
}

if (args.Any(value => string.Equals(
        value,
        "--backup-database",
        StringComparison.OrdinalIgnoreCase)))
{
    using var backupScope = app.Services.CreateScope();
    var startup = backupScope.ServiceProvider.GetRequiredService<DatabaseStartupService>();
    await startup.InitializeAsync();

    var backupService = backupScope.ServiceProvider.GetRequiredService<SqliteBackupService>();
    var backup = await backupService.BackupAsync();
    Console.WriteLine(
        $"Clarity database backup created: {backup.BackupFileName} ({backup.LengthBytes} bytes).");
    return;
}

using (var scope = app.Services.CreateScope())
{
    var startup = scope.ServiceProvider.GetRequiredService<DatabaseStartupService>();
    await startup.InitializeAsync();
}

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

if (app.Environment.IsDevelopment())
{
    app.MapGet(
        "/dev/login",
        async (
            HttpContext context,
            AccountService accounts,
            ClarityDbContext db,
            CancellationToken cancellationToken) =>
        {
            var remoteAddress = context.Connection.RemoteIpAddress;

            if (remoteAddress is null
                || !IPAddress.IsLoopback(remoteAddress))
            {
                return Results.NotFound();
            }

            const string email = "explorer@clarity.local";
            var user = await db.Users
                .FirstOrDefaultAsync(
                    x => x.Email == email,
                    cancellationToken);

            if (user is null)
            {
                var temporaryPassword = Convert.ToHexString(
                    RandomNumberGenerator.GetBytes(32));

                user = await accounts.CreateAsync(
                    email,
                    "Clarity Explorer",
                    temporaryPassword,
                    cancellationToken);
            }

            await context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                accounts.CreatePrincipal(user),
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });

            return Results.Redirect("/my-clarity");
        });
}

app.MapGet(
    "/health",
    (DatabaseRuntimeState databaseState) =>
    {
        var database = databaseState.Get();
        var backupAgeHours = database.LastBackupUtc.HasValue
            ? Math.Round(
                (DateTime.UtcNow - database.LastBackupUtc.Value).TotalHours,
                1)
            : (double?)null;
        var payload = new
        {
            service = "Clarity Belongs",
            status = database.Ready ? "Healthy" : "Unhealthy",
            utc = DateTime.UtcNow,
            database = new
            {
                reachable = database.Reachable,
                schemaCurrent = database.SchemaCurrent,
                writable = database.Writable,
                lastBackupUtc = database.LastBackupUtc,
                backupAgeHours
            }
        };

        return database.Ready
            ? Results.Ok(payload)
            : Results.Json(payload, statusCode: StatusCodes.Status503ServiceUnavailable);
    });

app.MapPost(
    "/auth/signup",
    async (
        HttpContext context,
        AccountService accounts,
        SecurityThrottle throttle,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken) =>
    {
        var logger = loggerFactory.CreateLogger("AuthSignup");
        var clientKey = SecurityThrottle.ClientKey(context);

        if (!throttle.TryAcquire(
            "signup",
            clientKey,
            5,
            TimeSpan.FromHours(1),
            out _))
        {
            return Results.Redirect("/signup?error=generic");
        }

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
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14)
                });

            return Results.Redirect("/");
        }
        catch (InvalidOperationException ex)
        {
            logger.LogInformation(ex, "Signup rejected.");
            var code = ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase)
                ? "password"
                : ex.Message.Contains("valid email", StringComparison.OrdinalIgnoreCase)
                    ? "email"
                    : "generic";

            return Results.Redirect($"/signup?error={code}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Signup failed unexpectedly.");
            return Results.Redirect("/signup?error=generic");
        }
    })
    .RequireAntiforgery();

app.MapPost(
    "/auth/login",
    async (
        HttpContext context,
        AccountService accounts,
        SecurityThrottle throttle,
        LoginAttemptProtector attempts,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken) =>
    {
        var logger = loggerFactory.CreateLogger("AuthLogin");
        var clientKey = SecurityThrottle.ClientKey(context);

        if (!throttle.TryAcquire(
            "login",
            clientKey,
            20,
            TimeSpan.FromMinutes(5),
            out _))
        {
            return Results.Redirect("/login?error=invalid");
        }

        var form = await context.Request.ReadFormAsync(cancellationToken);
        var email = form["email"].ToString();
        var password = form["password"].ToString();
        var returnUrl = SafeLocalReturnUrl(form["returnUrl"].ToString());
        var attemptKey = $"{clientKey}:{email.Trim().ToLowerInvariant()}";

        if (!attempts.CanAttempt(attemptKey, out _))
        {
            return Results.Redirect(
                $"/login?error=invalid&returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

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
        catch (Exception ex)
        {
            logger.LogError(ex, "Login validation failed unexpectedly.");
            user = null;
        }

        if (user is null)
        {
            attempts.RecordFailure(attemptKey);
            return Results.Redirect(
                $"/login?error=invalid&returnUrl={Uri.EscapeDataString(returnUrl)}");
        }

        attempts.RecordSuccess(attemptKey);

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            accounts.CreatePrincipal(user),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(14)
            });

        return Results.Redirect(returnUrl);
    })
    .RequireAntiforgery();

app.MapPost(
    "/auth/logout",
    async (HttpContext context) =>
    {
        await context.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Redirect("/login");
    })
    .RequireAuthorization()
    .RequireAntiforgery();

app.MapPost(
    "/auth/forgot-password",
    async (
        HttpContext context,
        AccountService accounts,
        SecurityThrottle throttle,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken) =>
    {
        var clientKey = SecurityThrottle.ClientKey(context);
        var form = await context.Request.ReadFormAsync(cancellationToken);
        var email = form["email"].ToString();
        var emailKey = email.Trim().ToLowerInvariant();
        var allowedByIp = throttle.TryAcquire(
            "forgot-ip",
            clientKey,
            5,
            TimeSpan.FromMinutes(15),
            out _);
        var allowedByAddress = throttle.TryAcquire(
            "forgot-address",
            emailKey,
            3,
            TimeSpan.FromHours(1),
            out _);

        if (allowedByIp && allowedByAddress)
        {
            var baseUrl = builder.Configuration["PublicBaseUrl"]
                ?? $"{context.Request.Scheme}://{context.Request.Host}";

            try
            {
                await accounts.RequestPasswordResetAsync(
                    email,
                    baseUrl,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                loggerFactory
                    .CreateLogger("ForgotPassword")
                    .LogWarning(ex, "Password reset request could not be completed.");
            }
        }

        return Results.Redirect("/forgot-password?sent=1");
    })
    .RequireAntiforgery();

app.MapPost(
    "/auth/reset-password",
    async (
        HttpContext context,
        AccountService accounts,
        SecurityThrottle throttle,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken) =>
    {
        var clientKey = SecurityThrottle.ClientKey(context);

        if (!throttle.TryAcquire(
            "reset-password",
            clientKey,
            10,
            TimeSpan.FromMinutes(15),
            out _))
        {
            return Results.Redirect("/reset-password?error=invalid");
        }

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
                : Results.Redirect("/reset-password?error=invalid");
        }
        catch (InvalidOperationException)
        {
            return Results.Redirect(
                $"/reset-password?error=password&token={Uri.EscapeDataString(token)}");
        }
        catch (Exception ex)
        {
            loggerFactory
                .CreateLogger("ResetPassword")
                .LogError(ex, "Password reset failed unexpectedly.");
            return Results.Redirect("/reset-password?error=invalid");
        }
    })
    .RequireAntiforgery();

app.MapPost(
    "/billing/checkout/{planCode}",
    async (
        string planCode,
        CurrentAccountService currentAccount,
        StripeBillingService stripe,
        CancellationToken cancellationToken) =>
    {
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
    .RequireAuthorization()
    .RequireAntiforgery();

app.MapPost(
    "/billing/portal",
    async (
        CurrentAccountService currentAccount,
        StripeBillingService stripe,
        CancellationToken cancellationToken) =>
    {
        var account = await currentAccount.RequireAsync(cancellationToken);
        var url = await stripe.CreatePortalUrlAsync(
            account,
            cancellationToken);
        return Results.Redirect(url);
    })
    .RequireAuthorization()
    .RequireAntiforgery();

app.MapPost(
    "/webhooks/stripe",
    async (
        HttpContext context,
        StripeBillingService stripe,
        ILoggerFactory loggerFactory,
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
        catch (Exception ex) when (ex is InvalidOperationException or CryptographicException)
        {
            loggerFactory
                .CreateLogger("StripeWebhook")
                .LogWarning(ex, "Stripe webhook rejected.");
            return Results.BadRequest(new { error = "Invalid webhook request." });
        }
    })
    .DisableAntiforgery();

app.MapPost(
    "/feedback/submit",
    async (
        HttpContext context,
        ClarityDbContext db,
        SecurityThrottle throttle,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken) =>
    {
        var clientKey = SecurityThrottle.ClientKey(context);

        if (!throttle.TryAcquire(
            "feedback",
            clientKey,
            5,
            TimeSpan.FromMinutes(10),
            out _))
        {
            return Results.Redirect("/feedback?status=limited");
        }

        if (context.Request.ContentLength is > 12_000)
            return Results.Redirect("/feedback?status=invalid");

        try
        {
            var form = await context.Request.ReadFormAsync(cancellationToken);
            var kind = Truncate(form["kind"].ToString().Trim(), 32);
            var message = form["message"].ToString().Trim();
            var contact = form["contact"].ToString().Trim();
            var product = form["product"].ToString().Trim();
            var source = form["source"].ToString().Trim();
            var version = form["version"].ToString().Trim();

            if (message.Length > 4000
                || contact.Length > 320
                || product.Length > 100
                || source.Length > 500
                || version.Length > 64)
            {
                return Results.Redirect("/feedback?status=invalid");
            }

            if (string.IsNullOrWhiteSpace(message)
                && kind is not "useful-yes" and not "useful-no")
            {
                return Results.Redirect("/feedback?status=invalid");
            }

            var storedMessage = message;

            if (!string.IsNullOrWhiteSpace(version))
            {
                storedMessage = string.IsNullOrWhiteSpace(storedMessage)
                    ? $"Version: {version}"
                    : $"{storedMessage}{Environment.NewLine}{Environment.NewLine}Version: {version}";
            }

            var productSlug = string.IsNullOrWhiteSpace(product)
                ? "clarity"
                : product;
            var path = string.IsNullOrWhiteSpace(source)
                ? "/feedback"
                : source;
            var duplicateSince = DateTime.UtcNow.AddMinutes(-10);
            var duplicate = await db.FeedbackSubmissions
                .AsNoTracking()
                .AnyAsync(
                    x => x.CreatedUtc >= duplicateSince
                        && x.Kind == kind
                        && x.ProductSlug == productSlug
                        && x.Message == storedMessage,
                    cancellationToken);

            if (!duplicate)
            {
                db.FeedbackSubmissions.Add(new FeedbackSubmission
                {
                    Kind = kind,
                    Message = storedMessage,
                    ProductSlug = productSlug,
                    Path = path,
                    Contact = string.IsNullOrWhiteSpace(contact)
                        ? null
                        : contact,
                    CreatedUtc = DateTime.UtcNow
                });

                await db.SaveChangesAsync(cancellationToken);
            }

            return Results.Redirect("/feedback?status=sent");
        }
        catch (Exception ex)
        {
            loggerFactory
                .CreateLogger("Feedback")
                .LogWarning(ex, "Anonymous feedback submission failed.");
            return Results.Redirect("/feedback?status=error");
        }
    })
    .RequireAntiforgery();

app.MapPost(
    "/api/follows",
    async (
        CreateFollowRequest request,
        HttpContext context,
        CurrentAccountService currentAccount,
        FollowManagementService follows,
        PublicEndpointGuard endpointGuard,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken) =>
    {
        if (!SameOriginRequestValidator.IsAllowed(context.Request))
            return Results.BadRequest(new { error = "Invalid request origin." });

        try
        {
            await ValidatePublicTargetAsync(
                request,
                endpointGuard,
                cancellationToken);

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
        }
        catch (InvalidOperationException ex)
        {
            loggerFactory
                .CreateLogger("FollowApi")
                .LogInformation(ex, "Follow creation rejected.");
            return Results.BadRequest(new { error = "The follow could not be created with that target or configuration." });
        }
        catch (Exception ex)
        {
            loggerFactory
                .CreateLogger("FollowApi")
                .LogError(ex, "Follow creation failed unexpectedly.");
            return Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "The follow could not be created.");
        }
    })
    .RequireAuthorization()
    .DisableAntiforgery();

app.MapPost(
    "/api/follows/{id:long}/run",
    async (
        long id,
        HttpContext context,
        CurrentAccountService currentAccount,
        FollowManagementService follows,
        ObservationEngine engine,
        CancellationToken cancellationToken) =>
    {
        if (!SameOriginRequestValidator.IsAllowed(context.Request))
            return Results.BadRequest();

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
    .RequireAuthorization()
    .DisableAntiforgery();

app.MapPost(
    "/api/changes/{followId:long}/{changeId:long}/acknowledge",
    async (
        long followId,
        long changeId,
        HttpContext context,
        CurrentAccountService currentAccount,
        FollowManagementService follows,
        CancellationToken cancellationToken) =>
    {
        if (!SameOriginRequestValidator.IsAllowed(context.Request))
            return Results.BadRequest();

        var account = await currentAccount.RequireAsync(cancellationToken);
        var owned = await follows.GetAsync(
            account.WorkspaceId,
            followId,
            cancellationToken);

        if (owned is null
            || owned.Changes.All(x => x.ChangeId != changeId))
        {
            return Results.NotFound();
        }

        await follows.AcknowledgeAsync(
            account.WorkspaceId,
            followId,
            changeId,
            cancellationToken);
        return Results.NoContent();
    })
    .RequireAuthorization()
    .DisableAntiforgery();

app.Run();

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

static string Truncate(string value, int maximumLength) =>
    value.Length <= maximumLength
        ? value
        : value[..maximumLength];

static async Task ValidatePublicTargetAsync(
    CreateFollowRequest request,
    PublicEndpointGuard guard,
    CancellationToken cancellationToken)
{
    if (request.AdapterType == AdapterTypes.Http)
    {
        if (!Uri.TryCreate(request.Target, UriKind.Absolute, out var uri))
            throw new InvalidOperationException("A valid public URL is required.");

        await guard.ValidateAsync(uri, cancellationToken);
        return;
    }

    if (request.AdapterType == AdapterTypes.Tls)
    {
        var value = request.Target.Trim();
        var uri = Uri.TryCreate(value, UriKind.Absolute, out var absolute)
            ? absolute
            : new Uri($"https://{value}");
        await guard.ValidateAsync(uri, cancellationToken);
        return;
    }

    if (request.AdapterType == AdapterTypes.Dns)
    {
        await guard.ValidateHostAsync(
            DnsObservationAdapter.NormalizeHost(request.Target),
            cancellationToken);
        return;
    }

    if (request.AdapterType is AdapterTypes.DnsRecord or AdapterTypes.Domain)
        return;

    throw new InvalidOperationException("Unsupported observation adapter.");
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

public partial class Program;