using TaskAutomation.Domain.Models;

namespace TaskAutomation.Application.Services;

public interface IWorkItemReader
{
    Task<WorkItemSnapshot?> GetAsync(int workItemId, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> GetRecentCommentsAsync(int workItemId, CancellationToken cancellationToken);
}
