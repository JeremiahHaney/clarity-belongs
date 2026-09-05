namespace ClarityBelongs.Web.Services;

public static class WorkerHealth
{
    public static WorkerHeartbeatRegistry Registry { get; } = new();
}
