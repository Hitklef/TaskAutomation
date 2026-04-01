using TaskAutomation.Domain.Models;

namespace TaskAutomation.Application.Services;

public interface IAnalysisCoordinator
{
    Task<WorkItemAnalysisResult> AnalyzeAsync(WorkItemSnapshot workItem, CancellationToken cancellationToken);
}
