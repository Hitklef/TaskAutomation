using TaskAutomation.Domain.Models;

namespace TaskAutomation.Application.Services;

public interface ITransitionService
{
    Task TransitionAsync(WorkItemSnapshot workItem, BoardTransitionPlan plan, CancellationToken cancellationToken);
}
