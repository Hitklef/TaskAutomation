namespace TaskAutomation.Application.Models;

public sealed record ProcessingLeaseResult(
    bool Acquired,
    bool AlreadyProcessed,
    bool AlreadyInProgress,
    string Reason)
{
    public static ProcessingLeaseResult LeaseAcquired()
        => new(true, false, false, "Lease acquired");

    public static ProcessingLeaseResult AlreadyCompleted(string reason)
        => new(false, true, false, reason);

    public static ProcessingLeaseResult InProgress(string reason)
        => new(false, false, true, reason);
}
