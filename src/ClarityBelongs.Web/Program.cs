using ClarityBelongs.Web.Components;
using ClarityBelongs.Web.Data;
using ClarityBelongs.Web.Domain;
using ClarityBelongs.Web.Observation;
using ClarityBelongs.Web.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddDbContext<ClarityDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Clarity") ?? "Data Source=clarity.db"));

builder.Services.AddHttpClient<HttpObservationAdapter>();
builder.Services.AddHttpClient<DomainObservationAdapter>();
builder.Services.AddScoped<PublicEndpointGuard>();
builder.Services.AddScoped<IObservationAdapter>(sp => sp.GetRequiredService<HttpObservationAdapter>());
builder.Services.AddScoped<IObservationAdapter, TlsObservationAdapter>();
builder.Services.AddScoped<IObservationAdapter, DnsObservationAdapter>();
builder.Services.AddScoped<IObservationAdapter>(sp => sp.GetRequiredService<DomainObservationAdapter>());
builder.Services.AddScoped<ObservationEngine>();
builder.Services.AddScoped<MyClarityService>();
builder.Services.AddHostedService<ObservationWorker>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
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

app.MapPost("/api/follows", async (CreateFollowRequest request, ClarityDbContext db, CancellationToken cancellationToken) =>
{
    var workspace = await db.Workspaces.OrderBy(x => x.Id).FirstAsync(cancellationToken);
    var canonicalKey = $"{request.AdapterType}:{request.Target.Trim().ToLowerInvariant()}";

    var target = await db.Targets.FirstOrDefaultAsync(x => x.CanonicalKey == canonicalKey, cancellationToken);
    if (target is null)
    {
        target = new Target
        {
            TargetType = request.TargetType,
            CanonicalKey = canonicalKey,
            DisplayName = request.Name,
            PrimaryUri = request.Target.Trim()
        };

        db.Targets.Add(target);
        await db.SaveChangesAsync(cancellationToken);
    }

    var source = await db.SourceDefinitions
        .FirstOrDefaultAsync(x => x.TargetId == target.Id && x.AdapterType == request.AdapterType, cancellationToken);

    if (source is null)
    {
        source = new SourceDefinition
        {
            TargetId = target.Id,
            AdapterType = request.AdapterType
        };

        db.SourceDefinitions.Add(source);
        await db.SaveChangesAsync(cancellationToken);
    }

    var follow = new Follow
    {
        WorkspaceId = workspace.Id,
        TargetId = target.Id,
        SourceDefinitionId = source.Id,
        MonitorType = request.MonitorType,
        Name = request.Name,
        Importance = request.Importance,
        CheckCadenceMinutes = Math.Clamp(request.CheckCadenceMinutes, 1, 10080),
        NextCheckAtUtc = DateTime.UtcNow
    };

    db.Follows.Add(follow);
    await db.SaveChangesAsync(cancellationToken);

    db.AlertRules.Add(new AlertRule
    {
        FollowId = follow.Id,
        RuleType = "AnyMeaningfulChange",
        MinimumSeverity = ChangeSeverities.Notice
    });

    await db.SaveChangesAsync(cancellationToken);
    return Results.Created($"/api/follows/{follow.Id}", new { follow.Id });
});

app.MapPost("/api/follows/{id:long}/run", async (long id, ObservationEngine engine, CancellationToken cancellationToken) =>
{
    await engine.RunFollowAsync(id, cancellationToken);
    return Results.Accepted();
});

app.MapPost("/api/changes/{followId:long}/{changeId:long}/acknowledge", async (
    long followId,
    long changeId,
    ClarityDbContext db,
    CancellationToken cancellationToken) =>
{
    var link = await db.FollowChanges.FindAsync([followId, changeId], cancellationToken);
    if (link is null)
        return Results.NotFound();

    link.IsAcknowledged = true;
    link.AcknowledgedAtUtc = DateTime.UtcNow;
    await db.SaveChangesAsync(cancellationToken);
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
    string Importance = "Normal",
    int CheckCadenceMinutes = 15);
