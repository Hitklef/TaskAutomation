using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TaskAutomation.Application.Services;
using TaskAutomation.Contracts.Messages;

namespace TaskAutomation.Integrations.AzureDevOps.Services;

public sealed class AzureDevOpsWebhookParser : IWebhookEventParser
{
    public WebhookParseResult Parse(string payloadJson, string correlationId)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return new(false, "Webhook payload is empty.", null);
        }

        using var document = JsonDocument.Parse(payloadJson);
        var root = document.RootElement;
        var eventType = TryGetString(root, "eventType") ?? "unknown";
        var workItemId = ResolveWorkItemId(root);

        if (workItemId is null)
        {
            return new(false, "Webhook payload does not contain a work item id.", null);
        }

        var revision = ResolveRevision(root);
        var fingerprint = ResolveFingerprint(root, eventType, workItemId.Value, revision, payloadJson);

        return new(
            true,
            null,
            new WorkItemWebhookMessage(
                correlationId,
                eventType,
                workItemId.Value,
                revision,
                fingerprint,
                payloadJson,
                DateTimeOffset.UtcNow));
    }

    private static int? ResolveWorkItemId(JsonElement root)
    {
        if (TryGetInt(root, "resource", "workItemId") is { } workItemId)
        {
            return workItemId;
        }

        if (TryGetInt(root, "resource", "id") is { } resourceId)
        {
            return resourceId;
        }

        return TryGetInt(root, "resource", "revision", "id");
    }

    private static int? ResolveRevision(JsonElement root)
        => TryGetInt(root, "resource", "revision", "rev")
           ?? TryGetInt(root, "resource", "rev")
           ?? TryGetInt(root, "revision");

    private static string ResolveFingerprint(JsonElement root, string eventType, int workItemId, int? revision, string payloadJson)
    {
        var eventId = TryGetString(root, "id");
        if (!string.IsNullOrWhiteSpace(eventId))
        {
            return eventId;
        }

        var seed = $"{eventType}:{workItemId}:{revision}:{payloadJson}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        return Convert.ToHexString(bytes);
    }

    private static int? TryGetInt(JsonElement element, params string[] path)
    {
        if (!TryResolve(element, path, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out var number) => number,
            JsonValueKind.String when int.TryParse(value.GetString(), out var parsed) => parsed,
            _ => null
        };
    }

    private static string? TryGetString(JsonElement element, params string[] path)
    {
        return TryResolve(element, path, out var value)
            ? value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString()
            : null;
    }

    private static bool TryResolve(JsonElement element, string[] path, out JsonElement value)
    {
        value = element;
        foreach (var segment in path)
        {
            if (value.ValueKind != JsonValueKind.Object || !value.TryGetProperty(segment, out value))
            {
                return false;
            }
        }

        return true;
    }
}
