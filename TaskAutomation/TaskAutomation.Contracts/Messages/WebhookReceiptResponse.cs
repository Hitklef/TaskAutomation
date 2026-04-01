namespace TaskAutomation.Contracts.Messages;

public sealed record WebhookReceiptResponse(
    string CorrelationId,
    int? WorkItemId,
    string Status,
    string Message);
