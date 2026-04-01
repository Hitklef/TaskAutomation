using TaskAutomation.Domain.Models;

namespace TaskAutomation.Application.Services;

public interface ICommentPublisher
{
    Task PublishAsync(int workItemId, TaskCommentPayload payload, CancellationToken cancellationToken);
}
