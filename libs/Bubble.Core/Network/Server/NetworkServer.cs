using System.Net;
using Bubble.Core.Network.Internal;
using Microsoft.Extensions.Logging;

namespace Bubble.Core.Network.Server;

public sealed class NetworkServer
{
    private readonly ServerBuilder _builder;
    private readonly ILogger<NetworkServer> _logger;
    private readonly List<ServerListener> _runningListeners;
    private readonly TaskCompletionSource<object?> _shutdownTcs;
    private readonly TimerAwaitable _timerAwaitable;

    private Task _timerTask;

    public IEnumerable<EndPoint> EndPoints =>
        _runningListeners.Select(x => x.Listener.EndPoint);

    internal NetworkServer(ServerBuilder builder)
    {
        _shutdownTcs = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _logger = builder.ApplicationServices.GetLoggerFactory().CreateLogger<NetworkServer>();
        _builder = builder;
        _timerAwaitable = new TimerAwaitable(_builder.HeartBeatInterval, _builder.HeartBeatInterval);
        _runningListeners = [];
        _timerTask = Task.CompletedTask;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            foreach (var (endPoint, connectionDelegate, listenerFactory) in _builder.Bindings)
            {
                var connectionListener = await listenerFactory.BindAsync(endPoint, cancellationToken).ConfigureAwait(false);
                var serverListener = new ServerListener(connectionListener, connectionDelegate, _shutdownTcs.Task, _logger);
                _runningListeners.Add(serverListener);
                serverListener.Start();
            }
        }
        catch
        {
            await StopAsync(cancellationToken).ConfigureAwait(false);
            throw; // rethrow unexpected exception
        }

        _timerAwaitable.Start();
        _timerTask = StartTimerAsync();
    }

    private async Task StartTimerAsync()
    {
        using (_timerAwaitable)
            while (await _timerAwaitable)
                foreach (var listener in _runningListeners)
                    listener.TickHeartbeat();
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        var tasks = new Task[_runningListeners.Count];

        for (var i = 0; i < _runningListeners.Count; i++)
            tasks[i] = _runningListeners[i]
                .Listener.UnbindAsync(cancellationToken)
                .AsTask();

        await Task.WhenAll(tasks).ConfigureAwait(false);

        // Signal to all of the listeners that it's time to start the shutdown process
        // We call this after unbind so that we're not touching the listener anymore (each loop will dispose the listener)
        _shutdownTcs.TrySetResult(null);

        for (var i = 0; i < _runningListeners.Count; i++)
            tasks[i] = _runningListeners[i].ExecutionTask ?? Task.CompletedTask;

        var shutdownTask = Task.WhenAll(tasks);

        if (cancellationToken.CanBeCanceled)
            await shutdownTask.WithCancellation(cancellationToken).ConfigureAwait(false);
        else
            await shutdownTask.ConfigureAwait(false);

        try
        {
            _timerAwaitable.Stop();
            await _timerTask.ConfigureAwait(false);
        }
        catch
        {
            /* discard any timer exception in case it's already completed */
        }
    }
}