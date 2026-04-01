using TaskAutomation.Application.Extensions;
using TaskAutomation.Application.Services;
using TaskAutomation.Infrastructure.Extensions;
using TaskAutomation.Integrations.AI.Extensions;
using TaskAutomation.Integrations.AzureDevOps.Extensions;
using TaskAutomation.Integrations.RepositoryContext.Extensions;
using TaskAutomation.Persistence.Extensions;
using TaskAutomation.Worker.Health;
using TaskAutomation.Worker.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddTaskAutomationApplication(builder.Configuration);
builder.Services.AddTaskAutomationInfrastructure();
builder.Services.AddTaskAutomationAzureDevOps(builder.Configuration);
builder.Services.AddTaskAutomationAi(builder.Configuration);
builder.Services.AddTaskAutomationRepositoryContext(builder.Configuration);
builder.Services.AddTaskAutomationPersistence(builder.Configuration);

builder.Services.AddSingleton<IBackgroundWorkItemQueue, ChannelBackgroundWorkItemQueue>();
builder.Services.AddHostedService<PersistenceInitializationHostedService>();
builder.Services.AddHostedService<QueuedWorkItemBackgroundService>();
builder.Services.AddHealthChecks()
    .AddCheck<ProcessingStateHealthCheck>("processing-state");

var app = builder.Build();

app.MapGet("/", () => Results.Ok(new
{
    service = "TaskAutomation",
    status = "running"
}));

app.MapHealthChecks("/health");

app.MapPost("/webhooks/azure-devops/work-items", async (
    HttpRequest request,
    IWebhookEventHandler webhookEventHandler,
    CancellationToken cancellationToken) =>
{
    using var reader = new StreamReader(request.Body);
    var payload = await reader.ReadToEndAsync(cancellationToken);
    var correlationId = request.Headers.TryGetValue("x-correlation-id", out var correlationHeader)
        && !string.IsNullOrWhiteSpace(correlationHeader)
        ? correlationHeader.ToString()
        : Guid.NewGuid().ToString("N");

    var response = await webhookEventHandler.HandleAsync(payload, correlationId, cancellationToken);
    return Results.Ok(response);
});

app.Run();
