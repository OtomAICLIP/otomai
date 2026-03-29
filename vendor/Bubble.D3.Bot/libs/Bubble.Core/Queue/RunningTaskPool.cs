using Serilog;

namespace Bubble.Core.Queue;

public class RunningTaskPool : IDisposable
{
    private readonly List<ScheduledTask> _tasks = new();
    private readonly object _lock = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _backgroundTask;

    public string Name { get; set; }
    
    /// <summary>
    /// Event triggered when a task throws an exception.
    /// </summary>
    public event Action<Exception>? OnTaskException;

    public RunningTaskPool(string name = "TaskPool")
    {
        Name = name;
        _backgroundTask = Task.Run(BackgroundTaskLoop);
    }

    /// <summary>
    /// Schedules a one-shot action to run after a delay, with an optional retry limit.
    /// </summary>
    public CancellationTokenSource ScheduleAction(Action action, TimeSpan delay, int maxRetryCount = 0)
    {
        var cts = new CancellationTokenSource();
        var scheduledTask = new ScheduledTask(action, DateTime.UtcNow + delay, null, cts, maxRetryCount);
        lock (_lock)
        {
            _tasks.Add(scheduledTask);
        }
        return cts;
    }

    /// <summary>
    /// Schedules a periodic action to run at specified intervals, with an optional retry limit.
    /// </summary>
    public CancellationTokenSource ScheduleAction(Action action, TimeSpan initialDelay, TimeSpan interval, int maxRetryCount = 0)
    {
        var cts = new CancellationTokenSource();
        var scheduledTask = new ScheduledTask(action, DateTime.UtcNow + initialDelay, interval, cts, maxRetryCount);
        lock (_lock)
        {
            _tasks.Add(scheduledTask);
        }
        return cts;
    }

    /// <summary>
    /// Schedules a one-shot function to run after a delay, with an optional retry limit.
    /// </summary>
    public CancellationTokenSource ScheduleFunc<T>(Func<T> func, TimeSpan delay, int maxRetryCount = 0)
    {
        var cts = new CancellationTokenSource();
        var scheduledTask = new ScheduledTask(func, DateTime.UtcNow + delay, null, cts, maxRetryCount);
        lock (_lock)
        {
            _tasks.Add(scheduledTask);
        }
        return cts;
    }

    /// <summary>
    /// Schedules a periodic function to run at specified intervals, with an optional retry limit.
    /// </summary>
    public CancellationTokenSource ScheduleFunc<T>(Func<T> func, TimeSpan initialDelay, TimeSpan interval, int maxRetryCount = 0)
    {
        var cts = new CancellationTokenSource();
        var scheduledTask = new ScheduledTask(func, DateTime.UtcNow + initialDelay, interval, cts, maxRetryCount);
        lock (_lock)
        {
            _tasks.Add(scheduledTask);
        }
        return cts;
    }

    /// <summary>
    /// Schedules a one-shot asynchronous function to run after a delay, with an optional retry limit.
    /// </summary>
    public CancellationTokenSource ScheduleAsyncFunc(Func<Task> func, TimeSpan delay, int maxRetryCount = 0)
    {
        var cts = new CancellationTokenSource();
        var scheduledTask = new ScheduledTask(func, DateTime.UtcNow + delay, null, cts, maxRetryCount);
        lock (_lock)
        {
            _tasks.Add(scheduledTask);
        }
        return cts;
    }

    /// <summary>
    /// Schedules a periodic asynchronous function to run at specified intervals, with an optional retry limit.
    /// </summary>
    public CancellationTokenSource ScheduleAsyncFunc(Func<Task> func, TimeSpan initialDelay, TimeSpan interval, int maxRetryCount = 0)
    {
        var cts = new CancellationTokenSource();
        var scheduledTask = new ScheduledTask(func, DateTime.UtcNow + initialDelay, interval, cts, maxRetryCount);
        lock (_lock)
        {
            _tasks.Add(scheduledTask);
        }
        return cts;
    }

    private async Task BackgroundTaskLoop()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            ScheduledTask? taskToExecute = null;
            TimeSpan delay;

            lock (_lock)
            {
                // Remove canceled tasks
                _tasks.RemoveAll(t => t.CancellationToken.IsCancellationRequested);

                if (_tasks.Count == 0)
                {
                    delay = TimeSpan.FromMilliseconds(20);
                }
                else
                {
                    _tasks.Sort((x, y) => x.NextRunTime.CompareTo(y.NextRunTime));
                    var nextTask = _tasks[0];
                    var now = DateTime.UtcNow;

                    if (nextTask.NextRunTime <= now)
                    {
                        _tasks.RemoveAt(0);

                        if (!nextTask.CancellationToken.IsCancellationRequested)
                        {
                            taskToExecute = nextTask;
                        }

                        delay = TimeSpan.Zero;
                    }
                    else
                    {
                        delay = nextTask.NextRunTime - now;
                    }
                }
            }

            if (taskToExecute != null)
            {
                try
                {
                    if (!taskToExecute.CancellationToken.IsCancellationRequested)
                    {
                        switch (taskToExecute.TaskDelegate)
                        {
                            case Action action:
                                action();
                                break;
                            case Func<Task> asyncFunc:
                                await asyncFunc();
                                break;
                            default:
                                var result = taskToExecute.TaskDelegate.DynamicInvoke();
                                if (result is Task taskResult)
                                {
                                    await taskResult;
                                }
                                break;
                        }
                    }

                    // Reset retry count after successful execution
                    taskToExecute.CurrentRetryCount = 0;

                    if (taskToExecute.IsPeriodic && !taskToExecute.CancellationToken.IsCancellationRequested)
                    {
                        lock (_lock)
                        {
                            taskToExecute.NextRunTime = DateTime.UtcNow + taskToExecute.Interval!.Value;
                            _tasks.Add(taskToExecute);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error executing task for {Name}", Name);
                    OnTaskException?.Invoke(ex);

                    if (taskToExecute.CurrentRetryCount < taskToExecute.MaxRetryCount)
                    {
                        taskToExecute.CurrentRetryCount++;
                        // Reschedule the task for retry
                        lock (_lock)
                        {
                            taskToExecute.NextRunTime = DateTime.UtcNow; // Retry immediately or add a delay if desired
                            _tasks.Add(taskToExecute);
                        }
                    }
                    else
                    {
                        // Max retries reached, reset retry count
                        taskToExecute.CurrentRetryCount = 0;
                        if (taskToExecute.IsPeriodic && !taskToExecute.CancellationToken.IsCancellationRequested)
                        {
                            lock (_lock)
                            {
                                taskToExecute.NextRunTime = DateTime.UtcNow + taskToExecute.Interval!.Value;
                                _tasks.Add(taskToExecute);
                            }
                        }
                        // For one-shot tasks, do not reschedule after max retries
                    }
                }
            }

            if (delay > TimeSpan.Zero)
            {
                try
                {
                    await Task.Delay(delay, _cts.Token);
                }
                catch (TaskCanceledException)
                {
                    // Ignore cancellation exceptions
                }
            }
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        try
        {
            _backgroundTask.Wait();
        }
        catch (AggregateException)
        {
            // Ignore exceptions thrown due to cancellation
        }
        _cts.Dispose();
    }

    private class ScheduledTask
    {
        public Delegate TaskDelegate { get; }
        public DateTime NextRunTime { get; set; }
        public TimeSpan? Interval { get; }
        public bool IsPeriodic => Interval.HasValue;
        public CancellationToken CancellationToken { get; }
        public int MaxRetryCount { get; }
        public int CurrentRetryCount { get; set; }

        public ScheduledTask(Delegate taskDelegate, DateTime nextRunTime, TimeSpan? interval, CancellationTokenSource cancellationTokenSource, int maxRetryCount)
        {
            TaskDelegate = taskDelegate;
            NextRunTime = nextRunTime;
            Interval = interval;
            CancellationToken = cancellationTokenSource.Token;
            MaxRetryCount = maxRetryCount;
            CurrentRetryCount = 0;
        }
    }
}