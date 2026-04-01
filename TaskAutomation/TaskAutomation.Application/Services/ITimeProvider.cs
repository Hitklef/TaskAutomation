namespace TaskAutomation.Application.Services;

public interface ITimeProvider
{
    DateTimeOffset UtcNow { get; }
}
