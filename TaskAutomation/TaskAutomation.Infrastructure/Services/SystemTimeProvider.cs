using TaskAutomation.Application.Services;

namespace TaskAutomation.Infrastructure.Services;

public sealed class SystemTimeProvider : ITimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
