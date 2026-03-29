using System.Collections.Concurrent;
using System.Net;
using Bubble.Core.Network.Framing;
using Bubble.Core.Network.Framing.Abstractions.Metadata;
using Bubble.Core.Network.Framing.Extensions;
using Bubble.Core.Network.Framing.Serialization;
using Bubble.Core.Network.Transport.Sockets.Internal;
using Microsoft.AspNetCore.Connections;
using Serilog;

namespace Bubble.Core.Network.Dispatcher;

public class DefaultSession<TMetadata> : IAsyncDisposable
    where TMetadata : class, IFrameMetadata
{
    private const int MaxQueueSize = 20;

    private readonly SocketConnection _context;
    public readonly IFrameMessageEncoder<TMetadata> Encoder;
    
    protected readonly ConcurrentQueue<ReadOnlyMemory<byte>> PendingMessages = new ConcurrentQueue<ReadOnlyMemory<byte>>();
    
    public EndPoint? LocalEndPoint
    {
        get => _context.LocalEndPoint;
        set => throw new InvalidOperationException($"{nameof(LocalEndPoint)} cannot be set on {nameof(DefaultSession<TMetadata>)}.");
    }

    public EndPoint? RemoteEndPoint
    {
        get => _context.RemoteEndPoint;
        set => throw new InvalidOperationException($"{nameof(RemoteEndPoint)} cannot be set on {nameof(DefaultSession<TMetadata>)}.");
    }

    public string ConnectionId
    {
        get => _context.ConnectionId;
        set => throw new InvalidOperationException($"{nameof(ConnectionId)} cannot be set on {nameof(DefaultSession<TMetadata>)}.");
    }

    public CancellationToken ConnectionClosed
    {
        get => _context.ConnectionClosed;
        set => throw new InvalidOperationException($"{nameof(ConnectionClosed)} cannot be set on {nameof(DefaultSession<TMetadata>)}.");
    }

    protected DefaultSession(SocketConnection connection, MetadataParser<TMetadata> parser, IMessageWriter writer, SemaphoreSlim? singleWriter = default)
    {
        _context = connection;
        Encoder = connection.Transport.Output.AsFrameMessageEncoder(parser, writer, singleWriter);
        
        _ = SendLoop();
        _context.ConnectionClosed.Register(() => Encoder.DisposeAsync().AsTask().Wait());
    }
    
    public void Enqueue(byte[] message)
    {
        if (PendingMessages.Count >= MaxQueueSize)
        {
            Log.Warning("Session {sessionId} has reached the maximum queue size of {maxQueueSize}.", ConnectionId, MaxQueueSize);
            return;
        }

        PendingMessages.Enqueue(message);
    }


    private async Task SendLoop()
    {
        while (!_context.ConnectionClosed.IsCancellationRequested)
        {
            if (PendingMessages.TryDequeue(out var message))
            {
                await Encoder.WriteAsync(message, _context.ConnectionClosed);
            }
            else
            {
                await Task.Yield();
            }
        }
    }

    public void Abort()
    {
        _context.Abort(new ConnectionAbortedException("The connection was aborted by the server."));
    }

    public void Abort(ConnectionAbortedException abortReason)
    {
        _context.Abort(abortReason);
    }

    public void Abort(string reason)
    {
        _context.Abort(new ConnectionAbortedException(reason));
    }

    public void Abort(string reason, Exception innerException)
    {
        _context.Abort(new ConnectionAbortedException(reason, innerException));
    }

    protected ValueTask SendAsync<T>(in T message)
    {
        if (ConnectionClosed.IsCancellationRequested)
        {
            Log.Warning("Session {sessionId} tried to send a message after it was disposed.", ConnectionId);
            return ValueTask.CompletedTask;
        }

        return Encoder.WriteAsync(in message, _context.ConnectionClosed);
    }

    public async ValueTask DisposeAsync()
    {
        await Encoder.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}