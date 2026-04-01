using Microsoft.Extensions.Diagnostics.HealthChecks;
using TaskAutomation.Application.Services;

namespace TaskAutomation.Worker.Health;

public sealed class ProcessingStateHealthCheck(IProcessingStateStore processingStateStore) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var isHealthy = await processingStateStore.CanConnectAsync(cancellationToken);
        return isHealthy
            ? HealthCheckResult.Healthy("Processing state store is reachable.")
            : HealthCheckResult.Unhealthy("Processing state store is not reachable.");
    }
}
