using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskAutomation.Application.Services;
using TaskAutomation.Integrations.RepositoryContext.Configuration;
using TaskAutomation.Integrations.RepositoryContext.Services;

namespace TaskAutomation.Integrations.RepositoryContext.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTaskAutomationRepositoryContext(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RepositoryContextOptions>(configuration.GetSection(RepositoryContextOptions.SectionName));
        services.AddScoped<IRepositorySyncService, NullRepositorySyncService>();
        services.AddScoped<IRepositoryContextProvider, NullRepositoryContextProvider>();

        return services;
    }
}
