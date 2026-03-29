using Serilog;

namespace Bubble.Core.Queue;

public class Result
{
    public object? Value { get; set; }
    public bool IsError { get; set; }
}
public sealed class ScheduledTask
{
    private Action? _action;
    private CancellationToken _cancellationToken;
    private TimeSpan? _recurrenceInterval;
    private bool _shouldRequeue;
    private Func<Task>? _task;
    private Func<Task<object>>? _taskWithResult;
    private PooledTaskCompletionSource _tcs;
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime NextRun { get; private set; }

    public bool IsRequeue { get; set; }
    public bool IsRequeuePlanned { get; set; }

    public ScheduledTask()
    {
        _tcs = PooledTaskCompletionSource.Rent();
    }

    public async Task ExecuteAsync()
    {
        if (_cancellationToken.IsCancellationRequested)
        {
            _tcs.SetCanceled(_cancellationToken);
            return;
        }

        try
        {
            if (_action != null)
            {
                _action();
                _tcs.SetResult(null);
            }
            else if (_task != null)
            {
                await _task();
                _tcs.SetResult(null);
            }
            else if (_taskWithResult != null)
            {
                var result = await _taskWithResult();
                _tcs.SetResult(result);
            }
            else
                _tcs.SetException(new InvalidOperationException("No task set"));

            // Handle recurrence
            if (_recurrenceInterval.HasValue)
                NextRun = DateTime.UtcNow.Add(_recurrenceInterval.Value);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error executing task");
            _tcs.SetException(ex);
        }
    }

    public void Reset()
    {
        if (_shouldRequeue && !_cancellationToken.IsCancellationRequested)
            return;

        _cancellationToken = CancellationToken.None;
        NextRun = DateTime.MinValue;
    }

    public void SetAction(Action action, TimeSpan delay, CancellationToken cancellationToken, TimeSpan? recurrenceInterval = null)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        _task = null;
        _taskWithResult = null;
        _recurrenceInterval = recurrenceInterval;
        _cancellationToken = cancellationToken;
        _shouldRequeue = _recurrenceInterval.HasValue;
        SetDelay(delay);
        SetTcs();
    }

    private void SetDelay(TimeSpan delay)
    {
        NextRun = DateTime.UtcNow.Add(delay);
    }

    public void SetResult(object? result)
    {
        _tcs.SetResult(result);
    }

    public void SetTask(Func<Task> task, TimeSpan delay, CancellationToken cancellationToken, TimeSpan? recurrenceInterval = null)
    {
        _task = task ?? throw new ArgumentNullException(nameof(task));
        _action = null;
        _taskWithResult = null;
        _recurrenceInterval = recurrenceInterval;
        _cancellationToken = cancellationToken;
        _shouldRequeue = _recurrenceInterval.HasValue;

        SetDelay(delay);
        SetTcs();
    }

    public void SetTask<T>(Func<Task<T>> task, TimeSpan delay, CancellationToken cancellationToken, TimeSpan? recurrenceInterval = null)
    {
        _taskWithResult = async () => (await task())!;
        _action = null;
        _task = null;
        _recurrenceInterval = recurrenceInterval;
        _cancellationToken = cancellationToken;
        _shouldRequeue = _recurrenceInterval.HasValue;

        SetDelay(delay);
        SetTcs();
    }

    private void SetTcs()
    {
        _tcs = PooledTaskCompletionSource.Rent();
    }

    public Task Task()
    {
        return _tcs.Task;
    }

    public Task<T> Task<T>()
    {
        return _tcs.Task.ContinueWith(t =>
        {
            if (t.IsFaulted)
                throw t.Exception!.InnerException!;
            return (T)t.Result!;
        }, _cancellationToken);
    }
}