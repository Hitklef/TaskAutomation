using Microsoft.Extensions.Options;
using TaskAutomation.Application.Services;
using TaskAutomation.Domain.Models;
using TaskAutomation.Integrations.AzureDevOps.Configuration;

namespace TaskAutomation.Integrations.AzureDevOps.Services;

public sealed class AzureDevOpsTransitionService(
    AzureDevOpsApiClient apiClient,
    IOptions<AzureDevOpsOptions> options) : ITransitionService
{
    public async Task TransitionAsync(WorkItemSnapshot workItem, BoardTransitionPlan plan, CancellationToken cancellationToken)
    {
        if (plan.Mode == TransitionMode.None)
        {
            return;
        }

        var operations = new List<Dictionary<string, object?>>();

        if ((plan.Mode == TransitionMode.StateOnly || plan.Mode == TransitionMode.StateAndBoardColumn)
            && !string.IsNullOrWhiteSpace(plan.TargetState)
            && !string.Equals(workItem.State, plan.TargetState, StringComparison.OrdinalIgnoreCase))
        {
            operations.Add(new Dictionary<string, object?>
            {
                ["op"] = "add",
                ["path"] = "/fields/System.State",
                ["value"] = plan.TargetState
            });
        }

        if ((plan.Mode == TransitionMode.BoardColumnOnly || plan.Mode == TransitionMode.StateAndBoardColumn)
            && !string.IsNullOrWhiteSpace(plan.TargetColumn)
            && !string.Equals(workItem.BoardColumn, plan.TargetColumn, StringComparison.OrdinalIgnoreCase))
        {
            operations.Add(new Dictionary<string, object?>
            {
                ["op"] = "add",
                ["path"] = $"/fields/{options.Value.BoardColumnFieldReferenceName}",
                ["value"] = plan.TargetColumn
            });
        }

        if (operations.Count == 0)
        {
            return;
        }

        await apiClient.PatchAsync($"wit/workitems/{workItem.Id}?api-version={options.Value.WorkItemsApiVersion}", operations, cancellationToken);
    }
}
