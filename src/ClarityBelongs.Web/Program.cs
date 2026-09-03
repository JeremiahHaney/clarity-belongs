using ClarityBelongs.Web.Components;
using ClarityBelongs.Web.Data;
using ClarityBelongs.Web.Domain;
using ClarityBelongs.Web.Observation;
using ClarityBelongs.Web.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<ClarityDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("Clarity")
            ?? "Data Source=clarity.db"));

builder.Services.Configure<EmailOptions>(
    builder.Configuration.GetSection("Email"));

builder.Services.AddHttpClient<HttpObservationAdapter>();
builder.Services.AddHttpClient<DomainObservationAdapter>();
builder.Services.AddScoped<PublicEndpointGuard>();
builder.Services.AddScoped<IObservationAdapter>(sp =>
    sp.GetRequiredService<HttpObservationAdapter>());
builder.Services.AddScoped<IObservationAdapter, TlsObservationAdapter>();
builder.Services.AddScoped<IObservationAdapter, DnsObservationAdapter>();
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
app.UseAntiforgery();
app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ClarityDbContext>();
    await db.Database.EnsureCreatedAsync();
    await EnsurePersonalWorkspaceAsync(db);
}

app.MapGet("/health", () => Results.Ok(new
{
    service = "Clarity Belongs",
    status = "Healthy",
    utc = DateTime.UtcNow
}));

app.MapPost(
    "/api/follows",
    async (
        CreateFollowRequest request,
        FollowManagementService follows,
        CancellationToken cancellationToken) =>
    {
        var followId = await follows.CreateAsync(
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
    });

app.MapPost(
    "/api/follows/{id:long}/run",
    async (
        long id,
        ObservationEngine engine,
        CancellationToken cancellationToken) =>
    {
        await engine.RunFollowAsync(id, cancellationToken);
        return Results.Accepted();
    });

app.MapPost(
    "/api/changes/{followId:long}/{changeId:long}/acknowledge",
    async (
        long followId,
        long changeId,
        FollowManagementService follows,
        CancellationToken cancellationToken) =>
    {
        await follows.AcknowledgeAsync(
            followId,
            changeId,
            cancellationToken);
        return Results.NoContent();
    });

app.Run();

static async Task EnsurePersonalWorkspaceAsync(ClarityDbContext db)
{
    if (await db.Users.AnyAsync())
        return;

    var user = new AppUser
    {
        Email = "owner@claritybelongs.local",
        DisplayName = "Owner"
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    db.Workspaces.Add(new Workspace
    {
        OwnerUserId = user.Id,
        Name = "My Clarity"
    });

    await db.SaveChangesAsync();
}

public sealed record CreateFollowRequest(
    string Name,
    string Target,
    string TargetType = "Website",
    string MonitorType = "WebsiteChange",
    string AdapterType = AdapterTypes.Http,
    string SourceConfigurationJson = "{\"mode\":\"content\"}",
    string Importance = "Normal",
    int CheckCadenceMinutes = 15,
    string AlertRuleType = "AnyMeaningfulChange");
