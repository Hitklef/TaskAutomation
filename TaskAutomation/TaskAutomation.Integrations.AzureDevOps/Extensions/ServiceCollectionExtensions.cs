using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskAutomation.Application.Services;
using TaskAutomation.Integrations.AzureDevOps.Configuration;
using TaskAutomation.Integrations.AzureDevOps.Services;

namespace TaskAutomation.Integrations.AzureDevOps.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTaskAutomationAzureDevOps(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<AzureDevOpsOptions>(configuration.GetSection(AzureDevOpsOptions.SectionName));
        services.AddHttpClient<AzureDevOpsApiClient>();
        services.AddScoped<IWebhookEventParser, AzureDevOpsWebhookParser>();
        services.AddScoped<IWorkItemReader, AzureDevOpsWorkItemClient>();
        services.AddScoped<IWorkItemQueryService, AzureDevOpsWorkItemClient>();
        services.AddScoped<ICommentPublisher, AzureDevOpsCommentClient>();
        services.AddScoped<ITransitionService, AzureDevOpsTransitionService>();
        services.AddScoped<IProcessedMarkerService, AzureDevOpsProcessedMarkerService>();

        return services;
    }
}
