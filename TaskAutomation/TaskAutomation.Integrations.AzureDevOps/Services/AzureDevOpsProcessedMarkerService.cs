using Microsoft.Extensions.Options;
using TaskAutomation.Application.Configuration;
using TaskAutomation.Application.Services;
using TaskAutomation.Domain.Models;
using TaskAutomation.Integrations.AzureDevOps.Configuration;

namespace TaskAutomation.Integrations.AzureDevOps.Services;

public sealed class AzureDevOpsProcessedMarkerService(
    AzureDevOpsApiClient apiClient,
    IOptions<WorkflowOptions> workflowOptions,
    IOptions<AzureDevOpsOptions> options) : IProcessedMarkerService
{
    public async Task MarkProcessedAsync(WorkItemSnapshot workItem, CancellationToken cancellationToken)
    {
        var processedTag = workflowOptions.Value.ProcessedTag;
        if (workItem.HasTag(processedTag))
        {
            return;
        }

        var tags = workItem.Tags.ToList();
        tags.Add(processedTag);

        var operations = new[]
        {
            new Dictionary<string, object?>
            {
                ["op"] = "add",
                ["path"] = "/fields/System.Tags",
                ["value"] = string.Join("; ", tags.Distinct(StringComparer.OrdinalIgnoreCase))
            }
        };

        await apiClient.PatchAsync($"wit/workitems/{workItem.Id}?api-version={options.Value.WorkItemsApiVersion}", operations, cancellationToken);
    }
}
