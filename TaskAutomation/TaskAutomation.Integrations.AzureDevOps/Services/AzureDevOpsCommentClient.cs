using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskAutomation.Application.Services;
using TaskAutomation.Domain.Models;
using TaskAutomation.Integrations.AzureDevOps.Configuration;

namespace TaskAutomation.Integrations.AzureDevOps.Services;

public sealed class AzureDevOpsCommentClient(
    AzureDevOpsApiClient apiClient,
    IOptions<AzureDevOpsOptions> options,
    ILogger<AzureDevOpsCommentClient> logger) : ICommentPublisher
{
    public async Task PublishAsync(int workItemId, TaskCommentPayload payload, CancellationToken cancellationToken)
    {
        if (options.Value.FetchComments)
        {
            var existingComments = await TryGetRecentCommentsAsync(workItemId, cancellationToken);
            if (existingComments.Any(comment => comment.Contains(payload.Signature, StringComparison.OrdinalIgnoreCase)))
            {
                logger.LogInformation("Comment signature already exists for work item {WorkItemId}. Skipping duplicate publish.", workItemId);
                return;
            }
        }

        var operations = new[]
        {
            new Dictionary<string, object?>
            {
                ["op"] = "add",
                ["path"] = "/fields/System.History",
                ["value"] = payload.Body
            }
        };

        await apiClient.PatchAsync($"wit/workitems/{workItemId}?api-version={options.Value.WorkItemsApiVersion}", operations, cancellationToken);
    }

    private async Task<IReadOnlyList<string>> TryGetRecentCommentsAsync(int workItemId, CancellationToken cancellationToken)
    {
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
            logger.LogWarning(exception, "Failed to inspect recent comments for work item {WorkItemId}", workItemId);
            return Array.Empty<string>();
        }
    }
}
