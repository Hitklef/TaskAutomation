using TaskAutomation.Domain.Models;

namespace TaskAutomation.Application.Services;

public interface IProcessedMarkerService
{
    Task MarkProcessedAsync(WorkItemSnapshot workItem, CancellationToken cancellationToken);
}
