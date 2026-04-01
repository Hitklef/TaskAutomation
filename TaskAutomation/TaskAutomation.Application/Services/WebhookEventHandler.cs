using Microsoft.Extensions.Logging;
using TaskAutomation.Application.Models;
using TaskAutomation.Contracts.Messages;
using TaskAutomation.Domain.Models;

namespace TaskAutomation.Application.Services;

public sealed class WebhookEventHandler(
    IWebhookEventParser webhookEventParser,
    IBackgroundWorkItemQueue backgroundWorkItemQueue,
    IProcessingStateStore processingStateStore,
    ITimeProvider timeProvider,
    ILogger<WebhookEventHandler> logger) : IWebhookEventHandler
{
    public async Task<WebhookReceiptResponse> HandleAsync(string payloadJson, string correlationId, CancellationToken cancellationToken)
    {
        var parseResult = webhookEventParser.Parse(payloadJson, correlationId);
        if (!parseResult.IsRelevant || parseResult.Message is null)
        {
            logger.LogInformation("Ignoring webhook with correlation {CorrelationId}: {Reason}", correlationId, parseResult.Reason);

            await processingStateStore.AppendAuditAsync(
                new AuditEntry(correlationId, null, "webhook-ignored", ProcessingStatus.Skipped, parseResult.Reason ?? "Webhook ignored", timeProvider.UtcNow),
                cancellationToken);

            return new(correlationId, null, "ignored", parseResult.Reason ?? "Webhook ignored");
        }

        await processingStateStore.RecordWebhookAsync(parseResult.Message, cancellationToken);
        await backgroundWorkItemQueue.EnqueueAsync(parseResult.Message, cancellationToken);
        await processingStateStore.AppendAuditAsync(
            new AuditEntry(correlationId, parseResult.Message.WorkItemId, "webhook-accepted", ProcessingStatus.Queued, "Webhook accepted and queued.", timeProvider.UtcNow),
            cancellationToken);

        logger.LogInformation(
            "Accepted webhook for work item {WorkItemId} with correlation {CorrelationId}",
            parseResult.Message.WorkItemId,
            correlationId);

        return new(correlationId, parseResult.Message.WorkItemId, "queued", "Webhook accepted and queued for background processing.");
    }
}
