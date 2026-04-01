using System.Text.Json;
using TaskAutomation.Domain.Models;

namespace TaskAutomation.Integrations.AzureDevOps.Services;

internal static class AzureDevOpsFieldMapper
{
    public static WorkItemSnapshot MapToSnapshot(JsonElement root, string boardColumnFieldReferenceName)
    {
        var id = root.GetProperty("id").GetInt32();
        var revision = root.TryGetProperty("rev", out var rev) ? rev.GetInt32() : 0;
        var fields = root.GetProperty("fields");
        var tags = SplitTags(GetString(fields, "System.Tags"));
        var additionalFields = fields.EnumerateObject()
            .ToDictionary(property => property.Name, property => ExtractFieldValue(property.Value));

        var boardColumn = GetString(fields, boardColumnFieldReferenceName)
                          ?? fields.EnumerateObject()
                              .Where(property => property.Name.Contains("BoardColumn", StringComparison.OrdinalIgnoreCase))
                              .Select(property => ExtractFieldValue(property.Value))
                              .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        return new WorkItemSnapshot(
            id,
            revision,
            GetString(fields, "System.WorkItemType") ?? "Unknown",
            GetString(fields, "System.Title") ?? $"Work item {id}",
            GetString(fields, "System.Description"),
            GetString(fields, "System.State") ?? "Unknown",
            boardColumn,
            GetIdentityString(fields, "System.AssignedTo"),
            GetString(fields, "System.AreaPath"),
            GetString(fields, "System.IterationPath"),
            tags,
            TryGetUrl(root),
            GetIdentityString(fields, "System.ChangedBy"),
            GetDateTimeOffset(fields, "System.ChangedDate"),
            additionalFields);
    }

    public static string? GetIdentityString(JsonElement fields, string fieldName)
    {
        if (!fields.TryGetProperty(fieldName, out var property))
        {
            return null;
        }

        return ExtractFieldValue(property);
    }

    public static string? GetString(JsonElement fields, string fieldName)
    {
        if (!fields.TryGetProperty(fieldName, out var property))
        {
            return null;
        }

        return ExtractFieldValue(property);
    }

    private static DateTimeOffset? GetDateTimeOffset(JsonElement fields, string fieldName)
    {
        var text = GetString(fields, fieldName);
        return DateTimeOffset.TryParse(text, out var value) ? value : null;
    }

    private static string? TryGetUrl(JsonElement root)
    {
        if (!root.TryGetProperty("_links", out var links)
            || !links.TryGetProperty("html", out var html)
            || !html.TryGetProperty("href", out var href))
        {
            return null;
        }

        return href.GetString();
    }

    private static IReadOnlyList<string> SplitTags(string? tags)
        => string.IsNullOrWhiteSpace(tags)
            ? Array.Empty<string>()
            : tags.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    private static string? ExtractFieldValue(JsonElement property)
    {
        return property.ValueKind switch
        {
            JsonValueKind.String => property.GetString(),
            JsonValueKind.Number => property.ToString(),
            JsonValueKind.True => bool.TrueString,
            JsonValueKind.False => bool.FalseString,
            JsonValueKind.Object when property.TryGetProperty("displayName", out var displayName) => displayName.GetString(),
            JsonValueKind.Object when property.TryGetProperty("name", out var name) => name.GetString(),
            JsonValueKind.Object when property.TryGetProperty("uniqueName", out var uniqueName) => uniqueName.GetString(),
            JsonValueKind.Object => property.ToString(),
            _ => null
        };
    }
}
