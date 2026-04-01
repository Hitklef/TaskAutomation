using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskAutomation.Application.Services;
using TaskAutomation.Persistence.Configuration;
using TaskAutomation.Persistence.Services;

namespace TaskAutomation.Persistence.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTaskAutomationPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PersistenceOptions>(configuration.GetSection(PersistenceOptions.SectionName));
        services.AddSingleton<IProcessingStateStore, SqliteProcessingStateStore>();

        return services;
    }
}
