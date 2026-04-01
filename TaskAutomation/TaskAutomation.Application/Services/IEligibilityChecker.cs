using TaskAutomation.Domain.Models;

namespace TaskAutomation.Application.Services;

public interface IEligibilityChecker
{
    Task<ProcessingDecision> EvaluateAsync(WorkItemSnapshot workItem, CancellationToken cancellationToken);
}
