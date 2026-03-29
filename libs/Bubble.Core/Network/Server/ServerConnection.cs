using Bubble.Core.Network.Transport.Sockets.Internal;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.Extensions.Logging;

namespace Bubble.Core.Network.Server;

internal sealed class ServerConnection : IConnectionCompleteFeature, IConnectionHeartbeatFeature, IConnectionLifetimeNotificationFeature
{
    private readonly CancellationTokenSource _connectionClosingCts;
    private readonly object _heartbeatLock;
    private readonly ILogger _logger;

    private bool _completed;
    private List<(Action<object> handler, object state)>? _heartbeatHandlers;
    private Stack<KeyValuePair<Func<object, Task>, object>>? _onCompleted;

    public SocketConnection Transport { get; }

    public long Id { get; }

    public CancellationToken ConnectionClosedRequested { get; set; }

    public ServerConnection(long id, SocketConnection transport, ILogger logger)
    {
        _connectionClosingCts = new CancellationTokenSource();
        _heartbeatLock = new object();
        _logger = logger;

        ConnectionClosedRequested = _connectionClosingCts.Token;
        Transport = transport;
        Id = id;
    }

    private Task CompleteAsyncMayAwait(Stack<KeyValuePair<Func<object, Task>, object>> onCompleted)
    {
        while (onCompleted.TryPop(out var entry))
            try
            {
                var task = entry.Key.Invoke(entry.Value);

                if (!task.IsCompletedSuccessfully)
                    return CompleteAsyncAwaited(task, onCompleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred running an IConnectionCompleteFeature.OnCompleted callback.");
            }

        return Task.CompletedTask;

        async Task CompleteAsyncAwaited(Task currentTask, Stack<KeyValuePair<Func<object, Task>, object>> remaining)
        {
            try
            {
                await currentTask.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred running an IConnectionCompleteFeature.OnCompleted callback.");
            }

            while (remaining.TryPop(out var entry))
                try
                {
                    await entry.Key.Invoke(entry.Value).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred running an IConnectionCompleteFeature.OnCompleted callback.");
                }
        }
    }

    public Task FireOnCompletedAsync()
    {
        if (_completed)
            throw new InvalidOperationException("The connection is already complete.");

        _completed = true;

        var onCompleted = _onCompleted;

        if (onCompleted is null || onCompleted.Count is 0)
            return Task.CompletedTask;

        return CompleteAsyncMayAwait(onCompleted);
    }

    public void TickHeartbeat()
    {
        lock (_heartbeatLock)
        {
            if (_heartbeatHandlers is null) return;

            foreach (var (handler, state) in _heartbeatHandlers)
                handler(state);
        }
    }

    public override string ToString()
    {
        return Transport.ConnectionId;
    }

    void IConnectionCompleteFeature.OnCompleted(Func<object, Task> callback, object state)
    {
        if (_completed)
            throw new InvalidOperationException("The connection is already complete.");

        _onCompleted ??= new Stack<KeyValuePair<Func<object, Task>, object>>();
        _onCompleted.Push(new KeyValuePair<Func<object, Task>, object>(callback, state));
    }

    public void OnHeartbeat(Action<object> action, object state)
    {
        lock (_heartbeatLock)
        {
            _heartbeatHandlers ??= [];
            _heartbeatHandlers.Add((action, state));
        }
    }

    public void RequestClose()
    {
        try
        {
            _connectionClosingCts.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // There's a race where the token could be disposed
            // swallow the exception and no-op
        }
    }
}