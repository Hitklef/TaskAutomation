namespace TaskAutomation.Domain.Models;

public sealed record WorkItemSnapshot(
    int Id,
    int Revision,
    string WorkItemType,
    string Title,
    string? Description,
    string State,
    string? BoardColumn,
    string? AssignedTo,
    string? AreaPath,
    string? IterationPath,
    IReadOnlyList<string> Tags,
    string? Url,
    string? ChangedBy,
    DateTimeOffset? ChangedDateUtc,
    IReadOnlyDictionary<string, string?> AdditionalFields)
{
    public bool HasTag(string tag)
        => Tags.Any(existing => string.Equals(existing, tag, StringComparison.OrdinalIgnoreCase));
}
