using TaskAutomation.Contracts.Messages;

namespace TaskAutomation.Application.Services;

public interface IWebhookEventHandler
{
    Task<WebhookReceiptResponse> HandleAsync(string payloadJson, string correlationId, CancellationToken cancellationToken);
}
