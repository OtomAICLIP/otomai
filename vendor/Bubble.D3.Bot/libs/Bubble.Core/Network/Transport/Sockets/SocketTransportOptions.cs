// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Buffers;
using System.Net;
using System.Net.Sockets;
using Bubble.Core.Network.Internal.MemoryPool;
using Microsoft.AspNetCore.Connections;

namespace Bubble.Core.Network.Transport.Sockets;

public sealed class SocketTransportOptions
{
    public int IOQueueCount { get; set; } = Math.Min(Environment.ProcessorCount, 16);

    public bool WaitForDataBeforeAllocatingBuffer { get; set; } = true;

    public bool NoDelay { get; set; } = true;

    public int Backlog { get; set; } = 512;

    public long? MaxReadBufferSize { get; set; } = 1024 * 1024;

    public long? MaxWriteBufferSize { get; set; } = 64 * 1024;

    public bool UnsafePreferInlineScheduling { get; set; }

    public Func<EndPoint, Socket> CreateBoundListenSocket { get; set; } = CreateDefaultBoundListenSocket;

    internal Func<MemoryPool<byte>> MemoryPoolFactory { get; set; } = () => new PinnedBlockMemoryPool();

    public static Socket CreateDefaultBoundListenSocket(EndPoint endpoint)
    {
        Socket listenSocket;
        switch (endpoint)
        {
            case FileHandleEndPoint fileHandle:
                // We're passing "ownsHandle: true" here even though we don't necessarily
                // own the handle because Socket.Dispose will clean-up everything safely.
                // If the handle was already closed or disposed then the socket will
                // be torn down gracefully, and if the caller never cleans up their handle
                // then we'll do it for them.
                //
                // If we don't do this then we run the risk of Kestrel hanging because the
                // the underlying socket is never closed and the transport manager can hang
                // when it attempts to stop.
                listenSocket = new Socket(
                    new SafeSocketHandle((IntPtr)fileHandle.FileHandle, true)
                );
                break;
            case UnixDomainSocketEndPoint unix:
                listenSocket = new Socket(unix.AddressFamily, SocketType.Stream, ProtocolType.Unspecified);
                break;
            case IPEndPoint ip:
                listenSocket = new Socket(ip.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                // Kestrel expects IPv6Any to bind to both IPv6 and IPv4
                if (ip.Address == IPAddress.IPv6Any)
                    listenSocket.DualMode = true;

                break;
            default:
                listenSocket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                break;
        }

        // we only call Bind on sockets that were _not_ created
        // using a file handle; the handle is already bound
        // to an underlying socket so doing it again causes the
        // underlying PAL call to throw
        if (endpoint is not FileHandleEndPoint)
            listenSocket.Bind(endpoint);

        return listenSocket;
    }
}