using TaskAutomation.Application.Services;

namespace TaskAutomation.Worker.Services;

public sealed class QueuedWorkItemBackgroundService(
    IBackgroundWorkItemQueue backgroundWorkItemQueue,
    IServiceProvider serviceProvider,
    ILogger<QueuedWorkItemBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var message = await backgroundWorkItemQueue.DequeueAsync(stoppingToken);
                using var scope = serviceProvider.CreateScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<IWorkItemProcessingOrchestrator>();

                await orchestrator.ProcessAsync(message, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Unhandled exception while processing queued work item.");
            }
        }
    }
}
