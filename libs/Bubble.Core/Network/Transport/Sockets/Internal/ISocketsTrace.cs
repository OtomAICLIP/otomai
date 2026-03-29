// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.Logging;

namespace Bubble.Core.Network.Transport.Sockets.Internal;

internal interface ISocketsTrace : ILogger
{
    void ConnectionError(SocketConnection connection, Exception ex);

    void ConnectionPause(SocketConnection connection);

    void ConnectionReadFin(SocketConnection connection);

    void ConnectionReset(string connectionId);

    void ConnectionReset(SocketConnection connection);

    void ConnectionResume(SocketConnection connection);

    void ConnectionWriteFin(SocketConnection connection, string reason);
}