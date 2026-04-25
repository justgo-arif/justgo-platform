#if NET9_0_OR_GREATER
using System.Threading.Channels;

namespace JustGo.Authentication.Infrastructure.HostedServices;

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Func<IServiceProvider, CancellationToken, Task>> _queue;

    public BackgroundTaskQueue()
    {
        _queue = Channel.CreateUnbounded<Func<IServiceProvider, CancellationToken, Task>>();
    }

    public async Task QueueBackgroundWorkItem(Func<IServiceProvider, CancellationToken, Task> workItem,
        CancellationToken cancellationToken)
    {
        if (workItem == null) throw new ArgumentNullException(nameof(workItem));
        await _queue.Writer.WriteAsync(workItem, cancellationToken);
    }

    public IAsyncEnumerable<Func<IServiceProvider, CancellationToken, Task>> DequeueAsync(CancellationToken cancellationToken)
    {
        var workItems = _queue.Reader.ReadAllAsync(cancellationToken);
        return workItems;
    }
}
#endif