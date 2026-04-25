#if NET9_0_OR_GREATER

namespace JustGo.Authentication.Infrastructure.HostedServices;

public interface IBackgroundTaskQueue
{
    Task QueueBackgroundWorkItem(Func<IServiceProvider, CancellationToken, Task> workItem, CancellationToken cancellationToken);
    IAsyncEnumerable<Func<IServiceProvider, CancellationToken,Task>> DequeueAsync(CancellationToken cancellationToken);
}

#endif