namespace TaskAutomation.Domain.Models;

public enum ProcessingStatus
{
    Received = 0,
    Queued = 1,
    InProgress = 2,
    Succeeded = 3,
    Skipped = 4,
    Failed = 5,
    RetryScheduled = 6
}
