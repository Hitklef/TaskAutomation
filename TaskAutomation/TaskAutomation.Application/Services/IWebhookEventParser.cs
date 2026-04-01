using TaskAutomation.Contracts.Messages;

namespace TaskAutomation.Application.Services;

public interface IWebhookEventParser
{
    WebhookParseResult Parse(string payloadJson, string correlationId);
}
