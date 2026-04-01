namespace TaskAutomation.Persistence.Configuration;

public sealed class PersistenceOptions
{
    public const string SectionName = "Persistence";

    public string ConnectionString { get; init; } = "Data Source=task-automation.db";
}
