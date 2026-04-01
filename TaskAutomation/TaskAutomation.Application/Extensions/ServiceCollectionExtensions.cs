using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskAutomation.Application.Configuration;
using TaskAutomation.Application.Services;

namespace TaskAutomation.Application.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTaskAutomationApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<WorkflowOptions>(configuration.GetSection(WorkflowOptions.SectionName));

        services.AddScoped<IWebhookEventHandler, WebhookEventHandler>();
        services.AddScoped<IEligibilityChecker, EligibilityChecker>();
        services.AddScoped<IAnalysisCoordinator, AnalysisCoordinator>();
        services.AddScoped<IWorkItemProcessingOrchestrator, WorkItemProcessingOrchestrator>();

        return services;
    }
}
