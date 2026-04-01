using Microsoft.Extensions.Options;
using TaskAutomation.Application.Configuration;
using TaskAutomation.Domain.Models;

namespace TaskAutomation.Application.Services;

public sealed class EligibilityChecker(
    IProcessingStateStore processingStateStore,
    IOptions<WorkflowOptions> workflowOptions) : IEligibilityChecker
{
    public async Task<ProcessingDecision> EvaluateAsync(WorkItemSnapshot workItem, CancellationToken cancellationToken)
    {
        var options = workflowOptions.Value;

        if (options.AllowedWorkItemTypes.Count > 0
            && !options.AllowedWorkItemTypes.Contains(workItem.WorkItemType, StringComparer.OrdinalIgnoreCase))
        {
            return new(false, ProcessingStatus.Skipped, $"Work item type '{workItem.WorkItemType}' is not allowed.");
        }

        if (options.ClosedStates.Contains(workItem.State, StringComparer.OrdinalIgnoreCase))
        {
            return new(false, ProcessingStatus.Skipped, $"Work item is already in closed state '{workItem.State}'.");
        }

        var matchesTodoState = options.TodoStates.Contains(workItem.State, StringComparer.OrdinalIgnoreCase);
        var matchesTodoColumn = !string.IsNullOrWhiteSpace(workItem.BoardColumn)
                                && options.TodoColumns.Contains(workItem.BoardColumn, StringComparer.OrdinalIgnoreCase);

        if (!matchesTodoState && !matchesTodoColumn)
        {
            return new(false, ProcessingStatus.Skipped, "Work item is not currently in the configured To Do state or column.");
        }

        if (workItem.HasTag(options.ProcessedTag))
        {
            return new(false, ProcessingStatus.Skipped, $"Work item already contains processed tag '{options.ProcessedTag}'.");
        }

        if (await processingStateStore.HasProcessedRevisionAsync(workItem.Id, workItem.Revision, cancellationToken))
        {
            return new(false, ProcessingStatus.Skipped, "Current work item revision was already processed.");
        }

        return new(true, ProcessingStatus.InProgress, "Eligible for analysis.");
    }
}
