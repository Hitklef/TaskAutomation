namespace TaskAutomation.Application.Configuration;

public sealed class WorkflowOptions
{
    public const string SectionName = "Workflow";

    public List<string> TodoStates { get; init; } = ["To Do", "New"];

    public List<string> TodoColumns { get; init; } = ["To Do"];

    public List<string> AllowedWorkItemTypes { get; init; } = ["Task", "Product Backlog Item", "User Story", "Bug"];

    public List<string> ClosedStates { get; init; } = ["Closed", "Done", "Removed"];

    public string ProcessedTag { get; init; } = "ai-analyzed";

    public bool AutoTransitionEnabled { get; init; } = true;

    public string? TargetState { get; init; } = "Ready for Development";

    public string? TargetColumn { get; init; } = "Ready for development";

    public string ServiceSignature { get; init; } = "TaskAutomation Bot";

    public int MinimumAnalysisCharacters { get; init; } = 120;

    public int LeaseDurationMinutes { get; init; } = 15;
}
