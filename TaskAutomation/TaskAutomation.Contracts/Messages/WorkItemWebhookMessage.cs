namespace TaskAutomation.Contracts.Messages;

public sealed record WorkItemWebhookMessage(
    string CorrelationId,
    string EventType,
    int WorkItemId,
    int? Revision,
    string Fingerprint,
    string PayloadJson,
    DateTimeOffset ReceivedAtUtc);
