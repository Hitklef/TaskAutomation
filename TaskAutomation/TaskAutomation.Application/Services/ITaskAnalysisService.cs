using TaskAutomation.Domain.Models;

namespace TaskAutomation.Application.Services;

public interface ITaskAnalysisService
{
    Task<WorkItemAnalysisResult> AnalyzeAsync(WorkItemAnalysisRequest request, CancellationToken cancellationToken);
}
