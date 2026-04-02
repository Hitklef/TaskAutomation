using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskAutomation.Application.Configuration;
using TaskAutomation.Application.Services;
using TaskAutomation.Domain.Models;
using TaskAutomation.Integrations.AzureDevOps.Configuration;

namespace TaskAutomation.Integrations.AzureDevOps.Services;

public sealed class AzureDevOpsWorkItemClient(
    AzureDevOpsApiClient apiClient,
    IOptions<WorkflowOptions> workflowOptions,
    IOptions<AzureDevOpsOptions> options,
    ILogger<AzureDevOpsWorkItemClient> logger) : IWorkItemReader, IWorkItemQueryService
{
    public async Task<WorkItemSnapshot?> GetAsync(int workItemId, CancellationToken cancellationToken)
    {
        try
        {
            var document = await apiClient.GetJsonAsync(
                $"wit/workitems/{workItemId}?$expand=all&api-version={options.Value.WorkItemsApiVersion}",
                cancellationToken);

            return AzureDevOpsFieldMapper.MapToSnapshot(document.RootElement, options.Value.BoardColumnFieldReferenceName);
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(exception, "Failed to fetch work item {WorkItemId}", workItemId);
            return null;
        }
    }

    public async Task<IReadOnlyList<string>> GetRecentCommentsAsync(int workItemId, CancellationToken cancellationToken)
    {
        if (!options.Value.FetchComments)
        {
            return Array.Empty<string>();
        }

        try
        {
            var document = await apiClient.GetJsonAsync(
                $"wit/workitems/{workItemId}/comments?$top={options.Value.CommentFetchCount}&api-version={options.Value.CommentsApiVersion}",
                cancellationToken);

            var comments = new List<string>();
            if (document.RootElement.TryGetProperty("comments", out var commentsElement))
            {
                foreach (var comment in commentsElement.EnumerateArray())
                {
                    if (comment.TryGetProperty("text", out var text))
                    {
                        comments.Add(text.GetString() ?? string.Empty);
                    }
                }
            }

            return comments;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to fetch recent comments for work item {WorkItemId}", workItemId);
            return Array.Empty<string>();
        }
    }

    public async Task<IReadOnlyList<int>> QueryTodoCandidateIdsAsync(CancellationToken cancellationToken)
    {
        var wiql = BuildTodoQuery();
        var payload = new Dictionary<string, string>
        {
            ["query"] = wiql
        };

        try
        {
            var document = await apiClient.PostJsonAsync(
                $"wit/wiql?api-version={options.Value.WorkItemsApiVersion}",
                payload,
                cancellationToken);

            var ids = new List<int>();
            if (document.RootElement.TryGetProperty("workItems", out var workItemsElement))
            {
                foreach (var item in workItemsElement.EnumerateArray())
                {
                    if (item.TryGetProperty("id", out var idElement) && idElement.TryGetInt32(out var id))
                    {
                        ids.Add(id);
                    }
                }
            }

            return ids;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "Failed to query To Do work items from Azure DevOps.");
            return Array.Empty<int>();
        }
    }

    private string BuildTodoQuery()
    {
        var workflow = workflowOptions.Value;
        var conditions = new List<string>
        {
            "[System.TeamProject] = @project"
        };

        if (workflow.AllowedWorkItemTypes.Count > 0)
        {
            conditions.Add($"[System.WorkItemType] IN ({JoinQuotedValues(workflow.AllowedWorkItemTypes)})");
        }

        var todoConditions = new List<string>();
        if (workflow.TodoStates.Count > 0)
        {
            todoConditions.Add($"[System.State] IN ({JoinQuotedValues(workflow.TodoStates)})");
        }

        if (workflow.TodoColumns.Count > 0)
        {
            todoConditions.Add($"[{options.Value.BoardColumnFieldReferenceName}] IN ({JoinQuotedValues(workflow.TodoColumns)})");
        }

        if (todoConditions.Count > 0)
        {
            conditions.Add($"({string.Join(" OR ", todoConditions)})");
        }

        if (workflow.ClosedStates.Count > 0)
        {
            conditions.Add($"[System.State] NOT IN ({JoinQuotedValues(workflow.ClosedStates)})");
        }

        return
            $"""
             SELECT TOP {workflow.TodoPollingBatchSize} [System.Id]
             FROM WorkItems
             WHERE {string.Join(Environment.NewLine + "  AND ", conditions)}
             ORDER BY [System.ChangedDate] DESC
             """;
    }

    private static string JoinQuotedValues(IEnumerable<string> values)
        => string.Join(", ", values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => $"'{value.Replace("'", "''")}'"));
}
