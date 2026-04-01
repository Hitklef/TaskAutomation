using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskAutomation.Application.Services;
using TaskAutomation.Domain.Models;
using TaskAutomation.Integrations.AzureDevOps.Configuration;

namespace TaskAutomation.Integrations.AzureDevOps.Services;

public sealed class AzureDevOpsWorkItemClient(
    AzureDevOpsApiClient apiClient,
    IOptions<AzureDevOpsOptions> options,
    ILogger<AzureDevOpsWorkItemClient> logger) : IWorkItemReader
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
}
