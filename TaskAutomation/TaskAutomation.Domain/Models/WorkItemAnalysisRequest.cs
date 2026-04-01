namespace TaskAutomation.Domain.Models;

public sealed record WorkItemAnalysisRequest(
    WorkItemSnapshot WorkItem,
    string CleanDescription,
    RepositoryContextSnapshot RepositoryContext,
    IReadOnlyList<string> RecentComments);
