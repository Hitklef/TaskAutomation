using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskAutomation.Application.Services;
using TaskAutomation.Integrations.AI.Configuration;
using TaskAutomation.Integrations.AI.Services;

namespace TaskAutomation.Integrations.AI.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTaskAutomationAi(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
        services.AddScoped<ITaskAnalysisService, StubTaskAnalysisService>();

        return services;
    }
}
