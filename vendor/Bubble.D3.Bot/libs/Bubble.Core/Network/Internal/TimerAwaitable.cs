// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;

namespace Bubble.Core.Network.Internal;

internal sealed class TimerAwaitable : IDisposable, ICriticalNotifyCompletion
{
    private static readonly Action _callbackCompleted = () =>
    {
    };

    private readonly TimeSpan _dueTime;
    private readonly object _lockObj;
    private readonly TimeSpan _period;

    private Action? _callback;
    private bool _disposed;
    private bool _running = true;
    private Timer? _timer;

    public bool IsCompleted =>
        ReferenceEquals(_callback, _callbackCompleted);

    public TimerAwaitable(TimeSpan dueTime, TimeSpan period)
    {
        _dueTime = dueTime;
        _period = period;
        _lockObj = new object();
    }

    public TimerAwaitable GetAwaiter()
    {
        return this;
    }

    public bool GetResult()
    {
        _callback = null;
        return _running;
    }

    public void Start()
    {
        if (_timer is not null)
            return;

        lock (_lockObj)
        {
            if (_disposed)
                return;

            _timer ??= new Timer(state => ((TimerAwaitable?)state)!.Tick(), this, _dueTime, _period);
        }
    }

    public void Stop()
    {
        lock (_lockObj)
        {
            // Stop should be used to trigger the call to end the loop which disposes
            ObjectDisposedException.ThrowIf(_disposed, nameof(TimerAwaitable));
            _running = false;
        }

        // Call tick here to make sure that we yield the callback,
        // if it's currently waiting, we don't need to wait for the next period
        Tick();
    }

    private void Tick()
    {
        Interlocked.Exchange(ref _callback, _callbackCompleted)?.Invoke();
    }

    public void OnCompleted(Action continuation)
    {
        if (ReferenceEquals(_callback, _callbackCompleted) || ReferenceEquals(Interlocked.CompareExchange(ref _callback, continuation, null), _callbackCompleted))
            Task.Run(continuation);
    }

    public void UnsafeOnCompleted(Action continuation)
    {
        OnCompleted(continuation);
    }

    void IDisposable.Dispose()
    {
        lock (_lockObj)
        {
            _disposed = true;
            _timer?.Dispose();
            _timer = null;
        }
    }
}