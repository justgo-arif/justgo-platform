#if NET9_0_OR_GREATER
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace JustGo.Authentication.Infrastructure.HostedServices;

public class QueuedHostedService : BackgroundService
{
    private readonly ILogger<QueuedHostedService> _logger;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue;
    private readonly IServiceProvider _serviceProvider;
    private readonly int _maxConcurrentTasks;
    private readonly SemaphoreSlim _semaphore;
    private readonly ConcurrentBag<Task> _runningTasks;

    public QueuedHostedService(
        ILogger<QueuedHostedService> logger,
        IBackgroundTaskQueue backgroundTaskQueue,
        IServiceProvider serviceProvider,
        int maxConcurrentTasks = 5)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _backgroundTaskQueue = backgroundTaskQueue ?? throw new ArgumentNullException(nameof(backgroundTaskQueue));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _maxConcurrentTasks = maxConcurrentTasks;
        _semaphore = new SemaphoreSlim(_maxConcurrentTasks, _maxConcurrentTasks);
        _runningTasks = [];
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // _logger.LogInformation("Background worker started with max {MaxConcurrentTasks} concurrent tasks.",
        //     _maxConcurrentTasks);

        try
        {
            await foreach (var workItem in _backgroundTaskQueue.DequeueAsync(stoppingToken))
            {
                try
                {
                    await _semaphore.WaitAsync(stoppingToken);

                    var task = ProcessWorkItemAsync(workItem, stoppingToken);
                    _runningTasks.Add(task);

                    CleanupCompletedTasks();

                    // _logger.LogInformation("Started new background task. Active tasks: {ActiveTasks}",
                    //     _maxConcurrentTasks - _semaphore.CurrentCount);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Background worker was cancelled while waiting for work items.");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while starting background work item.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Background worker was cancelled.");
        }
        finally
        {
            await WaitForRunningTasksAsync();
        }

        _logger.LogInformation("Background worker stopped.");
    }
    
    private async Task ProcessWorkItemAsync(Func<IServiceProvider, CancellationToken, Task> workItem,
        CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            await workItem(scope.ServiceProvider, stoppingToken);

           // _logger.LogInformation("Background work item completed successfully.");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Background work item was cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred executing background work item.");
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    private void CleanupCompletedTasks()
    {
        if (_runningTasks.Count <= _maxConcurrentTasks * 2) return;
        var newTasks = new ConcurrentBag<Task>();

        while (_runningTasks.TryTake(out var task))
        {
            if (!task.IsCompleted)
            {
                newTasks.Add(task);
            }
        }

        foreach (var task in newTasks)
        {
            _runningTasks.Add(task);
        }
    }
    
    private async Task WaitForRunningTasksAsync()
    {
        var tasksToWait = new List<Task>();

        while (_runningTasks.TryTake(out var task))
        {
            if (!task.IsCompleted)
            {
                tasksToWait.Add(task);
            }
        }

        if (tasksToWait.Count > 0)
        {
            //_logger.LogInformation("Waiting for {TaskCount} running tasks to complete...", tasksToWait.Count);

            try
            {
                await Task.WhenAll(tasksToWait);
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex, "Error occurred while waiting for running tasks to complete.");
            }
        }
    }

    public override void Dispose()
    {
        _semaphore?.Dispose();
        base.Dispose();
    }
}

#endif