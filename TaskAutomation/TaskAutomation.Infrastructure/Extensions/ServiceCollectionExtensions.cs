using Microsoft.Extensions.DependencyInjection;
using TaskAutomation.Application.Services;
using TaskAutomation.Infrastructure.Services;

namespace TaskAutomation.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTaskAutomationInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ITimeProvider, SystemTimeProvider>();
        services.AddSingleton<IHtmlTextSanitizer, HtmlTextSanitizer>();
        services.AddSingleton<ICommentFormatter, MarkdownCommentFormatter>();

        return services;
    }
}
