using Microsoft.Extensions.Options;
using TaskAutomation.Application.Services;
using TaskAutomation.Domain.Models;
using TaskAutomation.Integrations.AI.Configuration;

namespace TaskAutomation.Integrations.AI.Services;

public sealed class StubTaskAnalysisService(IOptions<AiOptions> aiOptions) : ITaskAnalysisService
{
    public Task<WorkItemAnalysisResult> AnalyzeAsync(WorkItemAnalysisRequest request, CancellationToken cancellationToken)
    {
        var workItem = request.WorkItem;
        var cleanDescription = string.IsNullOrWhiteSpace(request.CleanDescription)
            ? "No detailed description was provided, so implementation should start with clarifying business rules and expected acceptance criteria."
            : request.CleanDescription;

        var impactedAreas = new List<string>
        {
            $"Review services or endpoints that own the '{workItem.Title}' workflow.",
            $"Check validation and state-transition rules related to work item type '{workItem.WorkItemType}'."
        };

        if (!string.IsNullOrWhiteSpace(workItem.AreaPath))
        {
            impactedAreas.Add($"Coordinate changes with the Azure DevOps area path '{workItem.AreaPath}'.");
        }

        var risks = new List<string>
        {
            "The task may hide board-specific rules that are not obvious from the title and description alone.",
            "If automation changes work item state or tags, duplicate webhook events can be triggered."
        };

        if (request.RepositoryContext.IsAvailable)
        {
            risks.Add("Repository context is available, but the current stub analysis may not fully exploit it yet.");
        }

        var suggestedTests = new List<string>
        {
            "Verify that the task is processed once when it enters To Do.",
            "Verify that duplicate webhook deliveries do not create duplicate comments or repeated transitions.",
            "Verify that invalid or empty analysis responses are rejected before publishing a comment."
        };

        var summary =
            $"{aiOptions.Value.SummaryPrefix}: '{workItem.Title}' should be implemented as a flow that respects the current Azure DevOps state, derives a clean problem statement from the work item content, and leaves an auditable analysis trail.";

        var implementationNotes =
            $"Start by clarifying the expected outcome for work item #{workItem.Id}. Current state is '{workItem.State}'"
            + $"{(string.IsNullOrWhiteSpace(workItem.BoardColumn) ? string.Empty : $" in board column '{workItem.BoardColumn}'")}. "
            + $"Use the provided description as the primary source of truth: {cleanDescription} "
            + "Design the implementation so the orchestration, Azure DevOps integration, and AI reasoning can evolve independently.";

        var result = new WorkItemAnalysisResult(
            summary,
            implementationNotes,
            impactedAreas,
            risks,
            suggestedTests,
            aiOptions.Value.Provider,
            null);

        return Task.FromResult(result);
    }
}
