using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskAutomation.Application.Configuration;
using TaskAutomation.Application.Models;
using TaskAutomation.Contracts.Messages;
using TaskAutomation.Domain.Models;

namespace TaskAutomation.Application.Services;

public sealed class WorkItemProcessingOrchestrator(
    IWorkItemReader workItemReader,
    IEligibilityChecker eligibilityChecker,
    IAnalysisCoordinator analysisCoordinator,
    ICommentFormatter commentFormatter,
    ICommentPublisher commentPublisher,
    ITransitionService transitionService,
    IProcessedMarkerService processedMarkerService,
    IProcessingStateStore processingStateStore,
    ITimeProvider timeProvider,
    IOptions<WorkflowOptions> workflowOptions,
    ILogger<WorkItemProcessingOrchestrator> logger) : IWorkItemProcessingOrchestrator
{
    public async Task ProcessAsync(WorkItemWebhookMessage message, CancellationToken cancellationToken)
    {
        var correlationId = message.CorrelationId;
        var workItemId = message.WorkItemId;
        var options = workflowOptions.Value;

        await processingStateStore.AppendAuditAsync(
            new AuditEntry(correlationId, workItemId, "processing-started", ProcessingStatus.InProgress, "Dequeued work item for processing.", timeProvider.UtcNow),
            cancellationToken);

        var workItem = await workItemReader.GetAsync(workItemId, cancellationToken);
        if (workItem is null)
        {
            await processingStateStore.MarkCompletedAsync(workItemId, message.Revision ?? 0, correlationId, ProcessingStatus.Skipped, "Work item was not found.", cancellationToken);
            return;
        }

        var decision = await eligibilityChecker.EvaluateAsync(workItem, cancellationToken);
        if (!decision.ShouldProcess)
        {
            logger.LogInformation("Skipping work item {WorkItemId}: {Reason}", workItemId, decision.Reason);
            await processingStateStore.MarkCompletedAsync(workItem.Id, workItem.Revision, correlationId, decision.Status, decision.Reason, cancellationToken);
            await processingStateStore.AppendAuditAsync(
                new AuditEntry(correlationId, workItem.Id, "eligibility-skipped", decision.Status, decision.Reason, timeProvider.UtcNow),
                cancellationToken);
            return;
        }

        var lease = await processingStateStore.TryAcquireLeaseAsync(
            workItem.Id,
            workItem.Revision,
            correlationId,
            TimeSpan.FromMinutes(options.LeaseDurationMinutes),
            cancellationToken);

        if (!lease.Acquired)
        {
            logger.LogInformation("Work item {WorkItemId} did not get lease: {Reason}", workItem.Id, lease.Reason);
            await processingStateStore.MarkCompletedAsync(workItem.Id, workItem.Revision, correlationId, ProcessingStatus.Skipped, lease.Reason, cancellationToken);
            return;
        }

        try
        {
            var analysis = await analysisCoordinator.AnalyzeAsync(workItem, cancellationToken);
            var commentPayload = commentFormatter.Build(workItem, analysis);

            await commentPublisher.PublishAsync(workItem.Id, commentPayload, cancellationToken);

            if (options.AutoTransitionEnabled)
            {
                var transitionPlan = new BoardTransitionPlan(
                    workItem.BoardColumn,
                    options.TargetColumn,
                    options.TargetState,
                    string.IsNullOrWhiteSpace(options.TargetColumn)
                        ? TransitionMode.StateOnly
                        : TransitionMode.StateAndBoardColumn);

                await transitionService.TransitionAsync(workItem, transitionPlan, cancellationToken);
            }

            await processedMarkerService.MarkProcessedAsync(workItem, cancellationToken);
            await processingStateStore.MarkCompletedAsync(workItem.Id, workItem.Revision, correlationId, ProcessingStatus.Succeeded, "Analysis published successfully.", cancellationToken);
            await processingStateStore.AppendAuditAsync(
                new AuditEntry(correlationId, workItem.Id, "processing-succeeded", ProcessingStatus.Succeeded, "Comment published and transition completed.", timeProvider.UtcNow),
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to process work item {WorkItemId}", workItem.Id);
            await processingStateStore.MarkCompletedAsync(workItem.Id, workItem.Revision, correlationId, ProcessingStatus.Failed, exception.Message, cancellationToken);
            await processingStateStore.AppendAuditAsync(
                new AuditEntry(correlationId, workItem.Id, "processing-failed", ProcessingStatus.Failed, exception.ToString(), timeProvider.UtcNow),
                cancellationToken);
            throw;
        }
    }
}
