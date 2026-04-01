namespace TaskAutomation.Domain.Models;

public sealed record RepositoryContextSnapshot(
    bool IsAvailable,
    string Status,
    string? RepositoryPath,
    string? Branch,
    IReadOnlyDictionary<string, string?> Metadata)
{
    public static RepositoryContextSnapshot None(string status = "No repository context configured")
        => new(false, status, null, null, new Dictionary<string, string?>());
}
