using TaskAutomation.Domain.Models;

namespace TaskAutomation.Application.Models;

public sealed record AuditEntry(
    string CorrelationId,
    int? WorkItemId,
    string Stage,
    ProcessingStatus Status,
    string Message,
    DateTimeOffset CreatedAtUtc);
