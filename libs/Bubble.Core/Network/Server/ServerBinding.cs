using System.Net;
using Bubble.Core.Network.Transport.Sockets;

namespace Bubble.Core.Network.Server;

public record ServerBinding(EndPoint EndPoint, SocketConnectionDelegate Application, SocketTransportFactory ConnectionListenerFactory);

public sealed record LocalHostBinding : ServerBinding
{
    public LocalHostBinding(int port, SocketConnectionDelegate application, SocketTransportFactory connectionListenerFactory)
        : base(new IPEndPoint(IPAddress.Loopback, port), application, connectionListenerFactory)
    {
    }
}