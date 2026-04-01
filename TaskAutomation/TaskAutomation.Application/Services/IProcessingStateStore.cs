using TaskAutomation.Application.Models;
using TaskAutomation.Contracts.Messages;
using TaskAutomation.Domain.Models;

namespace TaskAutomation.Application.Services;

public interface IProcessingStateStore
{
    Task InitializeAsync(CancellationToken cancellationToken);

    Task RecordWebhookAsync(WorkItemWebhookMessage message, CancellationToken cancellationToken);

    Task<ProcessingLeaseResult> TryAcquireLeaseAsync(
        int workItemId,
        int revision,
        string correlationId,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    Task<bool> HasProcessedRevisionAsync(int workItemId, int revision, CancellationToken cancellationToken);

    Task MarkCompletedAsync(
        int workItemId,
        int revision,
        string correlationId,
        ProcessingStatus status,
        string details,
        CancellationToken cancellationToken);

    Task AppendAuditAsync(AuditEntry entry, CancellationToken cancellationToken);

    Task<bool> CanConnectAsync(CancellationToken cancellationToken);
}
