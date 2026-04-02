using Microsoft.Extensions.Options;
using TaskAutomation.Application.Configuration;
using TaskAutomation.Application.Models;
using TaskAutomation.Application.Services;
using TaskAutomation.Contracts.Messages;
using TaskAutomation.Domain.Models;

namespace TaskAutomation.Worker.Services;

public sealed class TodoPollingBackgroundService(
    IServiceProvider serviceProvider,
    IOptionsMonitor<WorkflowOptions> workflowOptions,
    ITimeProvider timeProvider,
    ILogger<TodoPollingBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunPollingCycleAsync(stoppingToken);

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(Math.Max(1, workflowOptions.CurrentValue.TodoPollingIntervalMinutes)));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunPollingCycleAsync(stoppingToken);
        }
    }

    private async Task RunPollingCycleAsync(CancellationToken cancellationToken)
    {
        var workflow = workflowOptions.CurrentValue;
        if (!workflow.EnableTodoPolling)
        {
            return;
        }

        using var scope = serviceProvider.CreateScope();
        var queryService = scope.ServiceProvider.GetRequiredService<IWorkItemQueryService>();
        var backgroundWorkItemQueue = scope.ServiceProvider.GetRequiredService<IBackgroundWorkItemQueue>();
        var processingStateStore = scope.ServiceProvider.GetRequiredService<IProcessingStateStore>();

        try
        {
            var workItemIds = await queryService.QueryTodoCandidateIdsAsync(cancellationToken);
            var now = timeProvider.UtcNow;

            await processingStateStore.AppendAuditAsync(
                new AuditEntry(
                    $"poll-cycle-{now:yyyyMMddHHmmss}",
                    null,
                    "todo-poll-cycle",
                    ProcessingStatus.Queued,
                    $"Polling cycle found {workItemIds.Count} work item(s) in To Do.",
                    now),
                cancellationToken);

            foreach (var workItemId in workItemIds.Distinct())
            {
                var correlationId = $"poll-{workItemId}-{Guid.NewGuid():N}";
                var message = new WorkItemWebhookMessage(
                    correlationId,
                    "polling.todo-scan",
                    workItemId,
                    null,
                    $"polling:{workItemId}:{now:yyyyMMddHHmm}",
                    "{}",
                    now);

                await backgroundWorkItemQueue.EnqueueAsync(message, cancellationToken);
                await processingStateStore.AppendAuditAsync(
                    new AuditEntry(
                        correlationId,
                        workItemId,
                        "todo-poll-enqueued",
                        ProcessingStatus.Queued,
                        "Work item enqueued from periodic To Do polling.",
                        now),
                    cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unhandled exception during To Do polling cycle.");
            await processingStateStore.AppendAuditAsync(
                new AuditEntry(
                    $"poll-cycle-failed-{Guid.NewGuid():N}",
                    null,
                    "todo-poll-cycle",
                    ProcessingStatus.Failed,
                    exception.Message,
                    timeProvider.UtcNow),
                cancellationToken);
        }
    }
}
