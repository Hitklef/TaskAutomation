using TaskAutomation.Application.Services;

namespace TaskAutomation.Worker.Services;

public sealed class PersistenceInitializationHostedService(
    IServiceProvider serviceProvider,
    ILogger<PersistenceInitializationHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var processingStateStore = scope.ServiceProvider.GetRequiredService<IProcessingStateStore>();

        logger.LogInformation("Initializing persistence store.");
        await processingStateStore.InitializeAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
