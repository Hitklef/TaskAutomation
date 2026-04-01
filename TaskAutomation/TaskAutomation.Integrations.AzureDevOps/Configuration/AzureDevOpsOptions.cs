namespace TaskAutomation.Integrations.AzureDevOps.Configuration;

public sealed class AzureDevOpsOptions
{
    public const string SectionName = "AzureDevOps";

    public string OrganizationUrl { get; init; } = string.Empty;

    public string Project { get; init; } = string.Empty;

    public string PersonalAccessToken { get; init; } = string.Empty;

    public string WorkItemsApiVersion { get; init; } = "7.1";

    public string CommentsApiVersion { get; init; } = "7.1-preview.4";

    public string BoardColumnFieldReferenceName { get; init; } = "System.BoardColumn";

    public bool FetchComments { get; init; } = false;

    public int CommentFetchCount { get; init; } = 10;
}
