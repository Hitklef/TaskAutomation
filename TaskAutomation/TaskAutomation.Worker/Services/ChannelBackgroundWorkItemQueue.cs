using System.Threading.Channels;
using TaskAutomation.Application.Services;
using TaskAutomation.Contracts.Messages;

namespace TaskAutomation.Worker.Services;

public sealed class ChannelBackgroundWorkItemQueue : IBackgroundWorkItemQueue
{
    private readonly Channel<WorkItemWebhookMessage> channel = Channel.CreateUnbounded<WorkItemWebhookMessage>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public ValueTask EnqueueAsync(WorkItemWebhookMessage message, CancellationToken cancellationToken)
        => channel.Writer.WriteAsync(message, cancellationToken);

    public ValueTask<WorkItemWebhookMessage> DequeueAsync(CancellationToken cancellationToken)
        => channel.Reader.ReadAsync(cancellationToken);
}
