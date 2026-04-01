using Microsoft.Extensions.Options;
using TaskAutomation.Application.Services;
using TaskAutomation.Domain.Models;
using TaskAutomation.Integrations.RepositoryContext.Configuration;

namespace TaskAutomation.Integrations.RepositoryContext.Services;

public sealed class NullRepositoryContextProvider(IOptions<RepositoryContextOptions> options) : IRepositoryContextProvider
{
    public Task<RepositoryContextSnapshot> GetContextAsync(WorkItemSnapshot workItem, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.LocalRepositoryPath))
        {
            return Task.FromResult(RepositoryContextSnapshot.None());
        }

        var metadata = new Dictionary<string, string?>
        {
            ["workItemId"] = workItem.Id.ToString(),
            ["branch"] = options.Value.BranchName
        };

        return Task.FromResult(new RepositoryContextSnapshot(
            true,
            "Repository path configured, detailed context not implemented yet.",
            options.Value.LocalRepositoryPath,
            options.Value.BranchName,
            metadata));
    }
}
