using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskAutomation.Application.Configuration;
using TaskAutomation.Domain.Models;

namespace TaskAutomation.Application.Services;

public sealed class AnalysisCoordinator(
    IHtmlTextSanitizer htmlTextSanitizer,
    IRepositorySyncService repositorySyncService,
    IRepositoryContextProvider repositoryContextProvider,
    ITaskAnalysisService taskAnalysisService,
    IWorkItemReader workItemReader,
    IOptions<WorkflowOptions> workflowOptions,
    ILogger<AnalysisCoordinator> logger) : IAnalysisCoordinator
{
    public async Task<WorkItemAnalysisResult> AnalyzeAsync(WorkItemSnapshot workItem, CancellationToken cancellationToken)
    {
        var cleanDescription = htmlTextSanitizer.Sanitize(workItem.Description);
        var syncStatus = await repositorySyncService.TrySyncAsync(cancellationToken);
        var repositoryContext = await repositoryContextProvider.GetContextAsync(workItem, cancellationToken);
        var comments = await workItemReader.GetRecentCommentsAsync(workItem.Id, cancellationToken);

        logger.LogInformation(
            "Preparing analysis request for work item {WorkItemId}. Repository sync status: {SyncStatus}",
            workItem.Id,
            syncStatus);

        var request = new WorkItemAnalysisRequest(
            workItem,
            cleanDescription,
            repositoryContext,
            comments);

        var result = await taskAnalysisService.AnalyzeAsync(request, cancellationToken);
        if (!result.IsMeaningful() || (result.Summary.Length + result.ImplementationNotes.Length) < workflowOptions.Value.MinimumAnalysisCharacters)
        {
            throw new InvalidOperationException($"Analysis result for work item {workItem.Id} is too short or invalid.");
        }

        return result;
    }
}
