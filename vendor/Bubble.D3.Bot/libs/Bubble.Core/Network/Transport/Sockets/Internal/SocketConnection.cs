// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using Bubble.Core.Network.Internal;
using Bubble.Core.Network.Internal.MemoryPool;
using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;

namespace Bubble.Core.Network.Transport.Sockets.Internal;

public sealed class SocketConnection : IAsyncDisposable
{
    private static readonly int MinAllocBufferSize = PinnedBlockMemoryPool.BlockSize / 2;

    private readonly CancellationTokenSource _connectionClosedTokenSource;
    private readonly IDuplexPipe _originalTransport;
    private readonly SocketReceiver _receiver;
    private readonly object _shutdownLock;
    private readonly SocketSenderPool _socketSenderPool;
    private readonly ISocketsTrace _trace;
    private readonly TaskCompletionSource _waitForConnectionClosedTcs;
    private readonly bool _waitForData;

    private bool _connectionClosed;
    private string? _connectionId;
    private Task? _receivingTask;
    private SocketSender? _sender;
    private Task? _sendingTask;
    private volatile Exception? _shutdownReason;
    private volatile bool _socketDisposed;

    public PipeWriter Input =>
        Application.Output;

    public PipeReader Output =>
        Application.Input;

    private Socket Socket { get; }

    public IDuplexPipe Transport { get; }

    private IDuplexPipe Application { get; }

    public EndPoint? LocalEndPoint { get; }

    public EndPoint? RemoteEndPoint { get; }

    public CancellationToken ConnectionClosed { get; }

    public string ConnectionId
    {
        get => _connectionId ??= CorrelationIdGenerator.GetNextId();
        set => _connectionId = value;
    }

    internal SocketConnection(
        Socket socket,
        PipeScheduler transportScheduler,
        ISocketsTrace trace,
        SocketSenderPool socketSenderPool,
        PipeOptions inputOptions,
        PipeOptions outputOptions,
        bool waitForData = true)
    {
        Debug.Assert(socket is not null);
        Debug.Assert(trace is not null);

        _trace = trace;
        _waitForData = waitForData;
        _socketSenderPool = socketSenderPool;
        _shutdownLock = new object();
        _connectionClosedTokenSource = new CancellationTokenSource();
        _waitForConnectionClosedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        Socket = socket;

        LocalEndPoint = Socket.LocalEndPoint;
        RemoteEndPoint = Socket.RemoteEndPoint;

        ConnectionClosed = _connectionClosedTokenSource.Token;

        // On *nix platforms, Sockets already dispatches to the ThreadPool.
        // Yes, the IOQueues are still used for the PipeSchedulers. This is intentional.
        // https://github.com/aspnet/KestrelHttpServer/issues/2573
        var awaiterScheduler = OperatingSystem.IsWindows() ? transportScheduler : PipeScheduler.Inline;

        _receiver = new SocketReceiver(awaiterScheduler);

        var pair = DuplexPipe.CreateConnectionPair(inputOptions, outputOptions);

        // Set the transport and connection id
        Transport = _originalTransport = pair.Transport;
        Application = pair.Application;
    }

    public void Abort(ConnectionAbortedException abortReason)
    {
        // Try to gracefully close the socket to match libuv behavior.
        Shutdown(abortReason);

        // Cancel ProcessSends loop after calling shutdown to ensure the correct _shutdownReason gets set.
        Output.CancelPendingRead();
    }

    private void CancelConnectionClosedToken()
    {
        try
        {
            _connectionClosedTokenSource.Cancel();
        }
        catch (Exception ex)
        {
            _trace.LogError(0, ex, $"Unexpected exception in {nameof(SocketConnection)}.{nameof(CancelConnectionClosedToken)}.");
        }
    }

    private async Task DoReceive()
    {
        Exception? error = null;

        try
        {
            while (!ConnectionClosed.IsCancellationRequested)
            {
                if (_waitForData)
                    // Wait for data before allocating a buffer.
                    await _receiver.WaitForDataAsync(Socket);

                // Ensure we have some reasonable amount of buffer space
                var buffer = Input.GetMemory(MinAllocBufferSize);

                var bytesReceived = await _receiver.ReceiveAsync(Socket, buffer);

                if (bytesReceived is 0)
                {
                    // FIN
                    _trace.ConnectionReadFin(this);
                    break;
                }

                Input.Advance(bytesReceived);

                var flushTask = Input.FlushAsync(ConnectionClosed);

                var paused = !flushTask.IsCompleted;

                if (paused)
                    _trace.ConnectionPause(this);

                var result = await flushTask;

                if (paused)
                    _trace.ConnectionResume(this);

                if (result.IsCompleted || result.IsCanceled)
                    // Pipe consumer is shut down, do we stop writing
                    break;
            }
        }
        catch (SocketException ex) when (IsConnectionResetError(ex.SocketErrorCode))
        {
            // This could be ignored if _shutdownReason is already set.
            error = new ConnectionResetException(ex.Message, ex);

            // There's still a small chance that both DoReceive() and DoSend() can log the same connection reset.
            // Both logs will have the same ConnectionId. I don't think it's worthwhile to lock just to avoid this.
            if (!_socketDisposed)
                _trace.ConnectionReset(this);
        }
        catch (Exception ex) when ((ex is SocketException socketEx && IsConnectionAbortError(socketEx.SocketErrorCode)) || ex is ObjectDisposedException)
        {
            // This exception should always be ignored because _shutdownReason should be set.
            error = ex;

            if (!_socketDisposed)
                // This is unexpected if the socket hasn't been disposed yet.
                _trace.ConnectionError(this, error);
        }
        catch (Exception ex)
        {
            // This is unexpected.
            error = ex;
            _trace.ConnectionError(this, error);
        }
        finally
        {
            // If Shutdown() has already bee called, assume that was the reason ProcessReceives() exited.
            await Input.CompleteAsync(_shutdownReason ?? error);

            FireConnectionClosed();
            
            await _waitForConnectionClosedTcs.Task;
        }
    }

    private async Task DoSend()
    {
        Exception? shutdownReason = null;
        Exception? unexpectedError = null;

        try
        {
            while (!ConnectionClosed.IsCancellationRequested)
            {
                var result = await Output.ReadAsync(ConnectionClosed);

                if (result.IsCanceled)
                    break;

                var buffer = result.Buffer;

                if (!buffer.IsEmpty)
                {
                    _sender = _socketSenderPool.Rent();
                    await _sender.SendAsync(Socket, buffer);
                    // We don't return to the pool if there was an exception, and
                    // we keep the _sender assigned so that we can dispose it in StartAsync.
                    _socketSenderPool.Return(_sender);
                    _sender = null;
                }

                Output.AdvanceTo(buffer.End);

                if (result.IsCompleted)
                    break;
            }
        }
        catch (SocketException ex) when (IsConnectionResetError(ex.SocketErrorCode))
        {
            shutdownReason = new ConnectionResetException(ex.Message, ex);
            _trace.ConnectionReset(this);
        }
        catch (Exception ex) when ((ex is SocketException socketEx && IsConnectionAbortError(socketEx.SocketErrorCode)) || ex is ObjectDisposedException)
        {
            // This should always be ignored since Shutdown() must have already been called by Abort().
            shutdownReason = ex;
        }
        catch (Exception ex)
        {
            shutdownReason = ex;
            unexpectedError = ex;
            _trace.ConnectionError(this, unexpectedError);
        }
        finally
        {
            Shutdown(shutdownReason);

            // Complete the output after disposing the socket
            await Output.CompleteAsync(unexpectedError);

            // Cancel any pending flushes so that the input loop is un-paused
            Input.CancelPendingFlush();

            FireConnectionClosed();
        }
    }

    private void FireConnectionClosed()
    {
        // Guard against scheduling this multiple times
        if (_connectionClosed)
            return;

        _connectionClosed = true;

        ThreadPool.UnsafeQueueUserWorkItem(static state =>
            {
                state.CancelConnectionClosedToken();

                state._waitForConnectionClosedTcs.TrySetResult();
            },
            this,
            false);
    }

    private static bool IsConnectionAbortError(SocketError errorCode)
    {
        // Calling Dispose after ReceiveAsync can cause an "InvalidArgument" error on *nix.
        return errorCode is SocketError.OperationAborted ||
               errorCode is SocketError.Interrupted ||
               (errorCode is SocketError.InvalidArgument && !OperatingSystem.IsWindows());
    }

    private static bool IsConnectionResetError(SocketError errorCode)
    {
        return errorCode is SocketError.ConnectionReset ||
               errorCode is SocketError.Shutdown ||
               (errorCode is SocketError.ConnectionAborted && OperatingSystem.IsWindows());
    }

    private void Shutdown(Exception? shutdownReason)
    {
        lock (_shutdownLock)
        {
            if (_socketDisposed)
                return;

            FireConnectionClosed();

            // Make sure to close the connection only after the _aborted flag is set.
            // Without this, the RequestsCanBeAbortedMidRead test will sometimes fail when
            // a BadHttpRequestException is thrown instead of a TaskCanceledException.
            _socketDisposed = true;

            // shutdownReason should only be null if the output was completed gracefully, so no one should ever
            // ever observe the nondescript ConnectionAbortedException except for connection middleware attempting
            // to half close the connection which is currently unsupported.
            _shutdownReason = shutdownReason ?? new ConnectionAbortedException("The Socket transport's send loop completed gracefully.");

            _trace.ConnectionWriteFin(this, _shutdownReason.Message);

            try
            {
                // Try to gracefully close the socket even for aborts to match libuv behavior.
                Socket.Shutdown(SocketShutdown.Both);
                Socket.Close();
                Socket.Dispose();
            }
            catch(Exception e)
            {
                _trace.LogError(0, e, "Unexpected exception in SocketConnection.Shutdown.");
                // Ignore any errors from Socket.Shutdown() since we're tearing down the connection anyway.
            }

        }
    }

    public void Start()
    {
        try
        {
            // Spawn send and receive logic
            _receivingTask = DoReceive();
            _sendingTask = DoSend();
        }
        catch (Exception ex)
        {
            _trace.LogError(0, ex, $"Unexpected exception in {nameof(SocketConnection)}.{nameof(Start)}.");
        }
    }

    // Only called after connection middleware is complete which means the ConnectionClosed token has fired.
    public async ValueTask DisposeAsync()
    {
        await _originalTransport.Input.CompleteAsync();
        await _originalTransport.Output.CompleteAsync();

        try
        {
            // Now wait for both to complete
            if (_receivingTask is not null)
                await _receivingTask;

            if (_sendingTask is not null)
                await _sendingTask;
        }
        catch (Exception ex)
        {
            _trace.LogError(0, ex, $"Unexpected exception in {nameof(SocketConnection)}.{nameof(Start)}.");
        }
        finally
        {
            _receiver.Dispose();
            _sender?.Dispose();
        }

        _connectionClosedTokenSource.Dispose();
    }
}