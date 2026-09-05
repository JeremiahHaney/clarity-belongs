using ClarityBelongs.Web.Data;
using ClarityBelongs.Web.Domain;
using Microsoft.EntityFrameworkCore;

namespace ClarityBelongs.Web.Services;

public sealed record MembershipSummary(
    Membership Membership,
    PlanDefinition Plan,
    int ActiveFollowCount,
    int RemainingFollows);

public sealed class MembershipService(
    ClarityDbContext db,
    PlanCatalog plans)
{
    public async Task<MembershipSummary> GetAsync(
        long userId,
        long workspaceId,
        CancellationToken cancellationToken = default)
    {
        var membership = await db.Memberships
            .FirstOrDefaultAsync(
                x => x.UserId == userId
                    && x.WorkspaceId == workspaceId,
                cancellationToken);

        if (membership is null)
        {
            membership = new Membership
            {
                UserId = userId,
                WorkspaceId = workspaceId,
                PlanCode = MembershipPlans.Free,
                Status = MembershipStatuses.Free
            };

            db.Memberships.Add(membership);
            await db.SaveChangesAsync(cancellationToken);
        }

        var plan = GetEffectivePlan(membership, plans);
        var activeFollowCount = await db.Follows
            .CountAsync(
                x => x.WorkspaceId == workspaceId
                    && x.Status != FollowStatuses.Archived,
                cancellationToken);

        return new MembershipSummary(
            membership,
            plan,
            activeFollowCount,
            Math.Max(0, plan.MaxActiveFollows - activeFollowCount));
    }

    public async Task<PlanDefinition> GetEffectivePlanForWorkspaceAsync(
        long workspaceId,
        CancellationToken cancellationToken = default)
    {
        var workspace = await db.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Id == workspaceId,
                cancellationToken);

        if (workspace is null)
            return plans.Get(MembershipPlans.Free);

        var membership = await db.Memberships
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.WorkspaceId == workspaceId
                    && x.UserId == workspace.OwnerUserId,
                cancellationToken);

        return membership is null
            ? plans.Get(MembershipPlans.Free)
            : GetEffectivePlan(membership, plans);
    }

    public async Task<DateTime> GetHistoryCutoffUtcAsync(
        long workspaceId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        var plan = await GetEffectivePlanForWorkspaceAsync(
            workspaceId,
            cancellationToken);
        return utcNow.AddDays(-plan.HistoryDays);
    }

    public async Task ValidateNewFollowAsync(
        long userId,
        long workspaceId,
        int requestedCadenceMinutes,
        CancellationToken cancellationToken = default)
    {
        var summary = await GetAsync(
            userId,
            workspaceId,
            cancellationToken);

        if (summary.ActiveFollowCount >= summary.Plan.MaxActiveFollows)
        {
            throw new InvalidOperationException(
                $"Your {summary.Plan.Name} plan includes up to {summary.Plan.MaxActiveFollows} active follows. Upgrade or archive one to add another.");
        }

        if (requestedCadenceMinutes < summary.Plan.MinimumCadenceMinutes)
        {
            throw new InvalidOperationException(
                $"The fastest check cadence on {summary.Plan.Name} is every {FormatMinutes(summary.Plan.MinimumCadenceMinutes)}.");
        }
    }

    public async Task<int> ClampCadenceAsync(
        long userId,
        long workspaceId,
        int requestedCadenceMinutes,
        CancellationToken cancellationToken = default)
    {
        var summary = await GetAsync(
            userId,
            workspaceId,
            cancellationToken);
        return Math.Max(
            summary.Plan.MinimumCadenceMinutes,
            Math.Clamp(requestedCadenceMinutes, 1, 10080));
    }

    public static bool IsPaidActive(Membership membership)
    {
        return membership.PlanCode != MembershipPlans.Free
            && membership.Status is MembershipStatuses.Active or MembershipStatuses.Trialing;
    }

    public static PlanDefinition GetEffectivePlan(
        Membership membership,
        PlanCatalog plans)
    {
        return IsPaidActive(membership)
            ? plans.Get(membership.PlanCode)
            : plans.Get(MembershipPlans.Free);
    }

    private static string FormatMinutes(int minutes)
    {
        if (minutes % 1440 == 0)
            return $"{minutes / 1440} day";

        if (minutes % 60 == 0)
            return $"{minutes / 60} hour";

        return $"{minutes} minutes";
    }
}
