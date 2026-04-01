using TaskAutomation.Contracts.Messages;

namespace TaskAutomation.Application.Services;

public interface IWorkItemProcessingOrchestrator
{
    Task ProcessAsync(WorkItemWebhookMessage message, CancellationToken cancellationToken);
}
