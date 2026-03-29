// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using Bubble.Core.Network.Transport.Sockets.Internal;
using Microsoft.AspNetCore.Connections;

namespace Bubble.Core.Network.Transport.Sockets;

public sealed class SocketConnectionListener
{
    private readonly MemoryPool<byte> _memoryPool;
    private readonly SocketTransportOptions _options;
    private readonly Settings[] _settings;
    private readonly int _settingsCount;
    private readonly ISocketsTrace _trace;

    private Socket? _listenSocket;
    private int _settingsIndex;

    public EndPoint EndPoint { get; private set; }

    internal SocketConnectionListener(EndPoint endpoint, SocketTransportOptions options, ISocketsTrace trace)
    {
        EndPoint = endpoint;
        _trace = trace;
        _options = options;
        _memoryPool = _options.MemoryPoolFactory();

        var ioQueueCount = options.IOQueueCount;
        var maxReadBufferSize = _options.MaxReadBufferSize ?? 0;
        var maxWriteBufferSize = _options.MaxWriteBufferSize ?? 0;
        var applicationScheduler = options.UnsafePreferInlineScheduling ? PipeScheduler.Inline : PipeScheduler.ThreadPool;

        if (ioQueueCount > 0)
        {
            _settingsCount = ioQueueCount;
            _settings = new Settings[_settingsCount];

            for (var i = 0; i < _settingsCount; i++)
            {
                var transportScheduler = options.UnsafePreferInlineScheduling ? PipeScheduler.Inline : new IOQueue();
                // https://github.com/aspnet/KestrelHttpServer/issues/2573
                var awaiterScheduler = OperatingSystem.IsWindows() ? transportScheduler : PipeScheduler.Inline;

                _settings[i] = new Settings
                {
                    Scheduler = transportScheduler,
                    InputOptions = new PipeOptions(_memoryPool, applicationScheduler, transportScheduler, maxReadBufferSize, maxReadBufferSize / 2,
                        useSynchronizationContext: false),
                    OutputOptions = new PipeOptions(_memoryPool, transportScheduler, applicationScheduler, maxWriteBufferSize, maxWriteBufferSize / 2,
                        useSynchronizationContext: false),
                    SocketSenderPool = new SocketSenderPool(awaiterScheduler)
                };
            }
        }
        else
        {
            var transportScheduler = options.UnsafePreferInlineScheduling ? PipeScheduler.Inline : PipeScheduler.ThreadPool;
            // https://github.com/aspnet/KestrelHttpServer/issues/2573
            var awaiterScheduler = OperatingSystem.IsWindows() ? transportScheduler : PipeScheduler.Inline;

            var directScheduler = new Settings[]
            {
                new()
                {
                    Scheduler = transportScheduler,
                    InputOptions = new PipeOptions(_memoryPool, applicationScheduler, transportScheduler, maxReadBufferSize, maxReadBufferSize / 2,
                        useSynchronizationContext: false),
                    OutputOptions = new PipeOptions(_memoryPool, transportScheduler, applicationScheduler, maxWriteBufferSize, maxWriteBufferSize / 2,
                        useSynchronizationContext: false),
                    SocketSenderPool = new SocketSenderPool(awaiterScheduler)
                }
            };

            _settingsCount = directScheduler.Length;
            _settings = directScheduler;
        }
    }

    public async ValueTask<SocketConnection?> AcceptAsync(CancellationToken cancellationToken = default)
    {
        while (true)
            try
            {
                Debug.Assert(_listenSocket is not null, "Bind must be called first.");

                var acceptSocket = await _listenSocket.AcceptAsync(cancellationToken);

                // Only apply no delay to Tcp based endpoints
                if (acceptSocket.LocalEndPoint is IPEndPoint)
                    acceptSocket.NoDelay = _options.NoDelay;

                var setting = _settings[_settingsIndex];

                var connection = new SocketConnection(acceptSocket,
                    setting.Scheduler,
                    _trace,
                    setting.SocketSenderPool,
                    setting.InputOptions,
                    setting.OutputOptions,
                    _options.WaitForDataBeforeAllocatingBuffer);

                connection.Start();

                _settingsIndex = (_settingsIndex + 1) % _settingsCount;

                return connection;
            }
            catch (ObjectDisposedException)
            {
                // A call was made to UnbindAsync/DisposeAsync just return null which signals we're done
                return null!;
            }
            catch (SocketException e) when (e.SocketErrorCode is SocketError.OperationAborted)
            {
                // A call was made to UnbindAsync/DisposeAsync just return null which signals we're done
                return null!;
            }
            catch (SocketException)
            {
                // The connection got reset while it was in the backlog, so we try again.
                _trace.ConnectionReset("(null)");
            }
    }

    internal void Bind()
    {
        if (_listenSocket is not null)
            throw new InvalidOperationException(SocketsStrings.TransportAlreadyBound);

        Socket listenSocket;
        try
        {
            listenSocket = _options.CreateBoundListenSocket(EndPoint);
        }
        catch (SocketException e) when (e.SocketErrorCode == SocketError.AddressAlreadyInUse)
        {
            throw new AddressInUseException(e.Message, e);
        }

        Debug.Assert(listenSocket.LocalEndPoint is not null);
        EndPoint = listenSocket.LocalEndPoint;

        listenSocket.Listen(_options.Backlog);

        _listenSocket = listenSocket;
    }

    public ValueTask DisposeAsync()
    {
        _listenSocket?.Dispose();

        // Dispose the memory pool
        _memoryPool.Dispose();

        // Dispose any pooled senders
        foreach (var setting in _settings)
            setting.SocketSenderPool.Dispose();

        return default;
    }

    public ValueTask UnbindAsync(CancellationToken cancellationToken = default)
    {
        _listenSocket?.Dispose();
        return default;
    }

    private sealed class Settings
    {
        public required PipeScheduler Scheduler { get; init; }
        public required PipeOptions InputOptions { get; init; }
        public required PipeOptions OutputOptions { get; init; }
        public required SocketSenderPool SocketSenderPool { get; init; }
    }
}