using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;

namespace Tgs.Agent.Dividend.Core.Workers;

public interface IBackgroundTaskQueue
{
    ValueTask QueueBackgroundWorkItemAsync(Func<IServiceScope, CancellationToken, ValueTask> workItem);

    ValueTask<Func<IServiceScope, CancellationToken, ValueTask>> DequeueAsync(
        CancellationToken cancellationToken);
}

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<IServiceScope, CancellationToken, ValueTask>> _queue = Channel
        .CreateUnbounded<Func<IServiceScope, CancellationToken, ValueTask>>(new UnboundedChannelOptions
        {
            SingleReader = true
        });

    public async ValueTask QueueBackgroundWorkItemAsync(
        Func<IServiceScope, CancellationToken, ValueTask> model)
    {
        if (model == null) throw new ArgumentNullException(nameof(model));

        await _queue.Writer.WriteAsync(model);
    }

    public async ValueTask<Func<IServiceScope, CancellationToken, ValueTask>> DequeueAsync(
        CancellationToken cancellationToken)
    {
        var workItem = await _queue.Reader.ReadAsync(cancellationToken);

        return workItem;
    }
}