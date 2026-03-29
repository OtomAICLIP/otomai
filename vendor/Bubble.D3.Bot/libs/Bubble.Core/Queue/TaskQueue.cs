using System.Collections.Concurrent;
using Microsoft.Extensions.ObjectPool;
using Serilog;

namespace Bubble.Core.Queue;

public sealed class TaskQueue : IDisposable
{
    private const int ProcessingInterval = 10;

    private static readonly ObjectPool<ScheduledTask> TaskPool =
        ObjectPool.Create(new DefaultPooledObjectPolicy<ScheduledTask>());

    private readonly CancellationTokenSource _cts = new CancellationTokenSource();
    private readonly Task _processingTask;
    private readonly ConcurrentQueue<ScheduledTask> _taskQueue = new ConcurrentQueue<ScheduledTask>();

    private ScheduledTask? _runningTask;

    public bool Debug { get; set; }

    public string Name { get; set; }

    public TaskQueue(string name)
    {
        Name = name;
        _processingTask = Task.Run(ProcessTasks);
    }

    private async Task ProcessTasks()
    {
        try
        {
            while (!_cts.IsCancellationRequested)
            {
                if (Debug)
                    Log.Debug("Processing tasks for {Name} ({Count} items)", Name, _taskQueue.Count);

                var processed = false;

                while (_taskQueue.TryDequeue(out var task))
                {
                    var now = DateTime.UtcNow;
                    if (task.NextRun <= now)
                    {
                        if (Debug)
                            Log.Debug("Executing task for {Name} {TaskId}", Name, task.Id);

                        _runningTask = task;

                        await task.ExecuteAsync();

                        // Re-enqueue recurrent tasks
                        if (task.NextRun > now)
                        {
                            task.IsRequeuePlanned = true;
                            if (Debug)
                                Log.Debug("Re-enqueueing task for {Name} {TaskId}", Name, task.Id);
                            _taskQueue.Enqueue(task);
                        }
                        else
                        {
                            if (Debug)
                                Log.Debug("Reset task for {Name} {TaskId}", Name, task.Id);

                            task.Reset();
                            TaskPool.Return(task);
                        }

                        processed = true;
                    }
                    else
                    {
                        task.IsRequeue = true;
                        if (Debug)
                            Log.Debug("Re-enqueueing task for {Name} {TaskId} cause it's not ready", Name, task.Id);
                        _taskQueue.Enqueue(task);
                        break;
                    }
                }

                if (!processed)
                    await Task.Delay(ProcessingInterval, _cts.Token);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing tasks for {Name}", Name);
        }
    }

    private ValueTask QueueInternal(Action action, TimeSpan delay, CancellationToken cancellationToken, TimeSpan? recurrenceInterval)
    {
        if (_cts.IsCancellationRequested)
            return ValueTask.CompletedTask;

        var scheduledTask = TaskPool.Get();
        scheduledTask.SetAction(action, delay, cancellationToken, recurrenceInterval);
        _taskQueue.Enqueue(scheduledTask);

        return new ValueTask(scheduledTask.Task());
    }

    private ValueTask QueueInternal(Func<Task> task, TimeSpan delay, CancellationToken cancellationToken, TimeSpan? recurrenceInterval)
    {
        if (_cts.IsCancellationRequested)
            return ValueTask.CompletedTask;

        var scheduledTask = TaskPool.Get();
        scheduledTask.SetTask(task, delay, cancellationToken, recurrenceInterval);
        _taskQueue.Enqueue(scheduledTask);

        return new ValueTask(scheduledTask.Task());
    }

    private ValueTask<T> QueueInternal<T>(Func<Task<T>> task, TimeSpan delay, CancellationToken cancellationToken, TimeSpan? recurrenceInterval)
    {
        if (_cts.IsCancellationRequested)
            return new ValueTask<T>(Task.FromCanceled<T>(cancellationToken));

        var scheduledTask = TaskPool.Get();
        scheduledTask.SetTask(task, delay, cancellationToken, recurrenceInterval);
        _taskQueue.Enqueue(scheduledTask);

        return new ValueTask<T>(scheduledTask.Task<T>());
    }


    public ValueTask QueueTask(Action action, TimeSpan delay = default, CancellationToken cancellationToken = default, TimeSpan? recurrenceInterval = null)
    {
        return QueueInternal(action, delay, cancellationToken, recurrenceInterval);
    }

    public ValueTask QueueTask(Func<Task> asyncTask, TimeSpan delay = default, CancellationToken cancellationToken = default, TimeSpan? recurrenceInterval = null)
    {
        return QueueInternal(asyncTask, delay, cancellationToken, recurrenceInterval);
    }

    public ValueTask<T> QueueTask<T>(Func<Task<T>> asyncTask, TimeSpan delay = default, CancellationToken cancellationToken = default, TimeSpan? recurrenceInterval = null)
    {
        return QueueInternal(asyncTask, delay, cancellationToken, recurrenceInterval);
    }

    public void QueueTaskForget(Func<Task> asyncTask, TimeSpan delay = default, CancellationToken cancellationToken = default, TimeSpan? recurrenceInterval = null)
    {
        QueueInternal(asyncTask, delay, cancellationToken, recurrenceInterval);
    }


    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}

public class PooledTaskCompletionSource
{
    private static readonly ObjectPool<PooledTaskCompletionSource> Pool =
        ObjectPool.Create(new DefaultPooledObjectPolicy<PooledTaskCompletionSource>());

    private TaskCompletionSource<object?> _tcs;

    public Task<object?> Task => _tcs.Task;

    public PooledTaskCompletionSource()
    {
        _tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public static PooledTaskCompletionSource Rent()
    {
        var pooledTcs = Pool.Get();
        pooledTcs.Reset();
        return pooledTcs;
    }

    private void Reset()
    {
        _tcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public void Return()
    {
        if (!_tcs.Task.IsCompleted)
            throw new InvalidOperationException("Cannot return a TaskCompletionSource that is not completed.");

        Pool.Return(this);
    }

    public void SetCanceled(CancellationToken cancellationToken)
    {
        _tcs.TrySetCanceled(cancellationToken);
    }

    public void SetException(Exception ex)
    {
        _tcs.TrySetException(ex);
    }

    public void SetResult(object? result)
    {
        _tcs.TrySetResult(result);
    }
}