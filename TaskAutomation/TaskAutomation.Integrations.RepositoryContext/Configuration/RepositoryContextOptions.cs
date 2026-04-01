namespace TaskAutomation.Integrations.RepositoryContext.Configuration;

public sealed class RepositoryContextOptions
{
    public const string SectionName = "RepositoryContext";

    public string? LocalRepositoryPath { get; init; }

    public string? BranchName { get; init; }
}
