// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Net;
using Bubble.Core.Network.Transport.Sockets.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bubble.Core.Network.Transport.Sockets;

public sealed class SocketTransportFactory
{
    private readonly SocketTransportOptions _options;
    private readonly SocketsTrace _trace;

    public SocketTransportFactory(IOptions<SocketTransportOptions> options, ILoggerFactory loggerFactory)
    {
        _options = options.Value;
        _trace = new SocketsTrace(loggerFactory.CreateLogger("Microsoft.AspNetCore.Server.Kestrel.Transport.Sockets"));
    }

    public ValueTask<SocketConnectionListener> BindAsync(EndPoint endpoint, CancellationToken cancellationToken = default)
    {
        var transport = new SocketConnectionListener(endpoint, _options, _trace);
        transport.Bind();
        return new ValueTask<SocketConnectionListener>(transport);
    }
}