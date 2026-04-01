using Microsoft.Extensions.Options;
using TaskAutomation.Application.Services;
using TaskAutomation.Integrations.RepositoryContext.Configuration;

namespace TaskAutomation.Integrations.RepositoryContext.Services;

public sealed class NullRepositorySyncService(IOptions<RepositoryContextOptions> options) : IRepositorySyncService
{
    public Task<string> TrySyncAsync(CancellationToken cancellationToken)
    {
        var status = string.IsNullOrWhiteSpace(options.Value.LocalRepositoryPath)
            ? "Repository sync skipped: no repository configured."
            : $"Repository sync skipped for '{options.Value.LocalRepositoryPath}': Stage 1 placeholder.";

        return Task.FromResult(status);
    }
}
