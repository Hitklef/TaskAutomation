namespace TaskAutomation.Domain.Models;

public sealed record BoardTransitionPlan(
    string? SourceColumn,
    string? TargetColumn,
    string? TargetState,
    TransitionMode Mode);
