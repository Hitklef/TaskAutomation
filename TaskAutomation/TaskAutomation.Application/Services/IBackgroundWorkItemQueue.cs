using TaskAutomation.Contracts.Messages;

namespace TaskAutomation.Application.Services;

public interface IBackgroundWorkItemQueue
{
    ValueTask EnqueueAsync(WorkItemWebhookMessage message, CancellationToken cancellationToken);

    ValueTask<WorkItemWebhookMessage> DequeueAsync(CancellationToken cancellationToken);
}
