namespace TaskAutomation.Contracts.Messages;

public sealed record WebhookParseResult(
    bool IsRelevant,
    string? Reason,
    WorkItemWebhookMessage? Message);
