namespace TaskAutomation.Domain.Models;

public sealed record ProcessingDecision(
    bool ShouldProcess,
    ProcessingStatus Status,
    string Reason);
