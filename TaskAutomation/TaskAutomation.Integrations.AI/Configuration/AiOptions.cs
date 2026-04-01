namespace TaskAutomation.Integrations.AI.Configuration;

public sealed class AiOptions
{
    public const string SectionName = "AI";

    public string Provider { get; init; } = "StubTaskAnalysisService";

    public string SummaryPrefix { get; init; } = "Automated technical analysis";
}
