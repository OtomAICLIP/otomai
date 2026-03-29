using Bubble.Core.Network.Transport.Sockets.Internal;

namespace Bubble.Core.Network.Transport.Sockets;

public abstract class SocketConnectionHandler
{
    protected internal abstract Task OnConnectedAsync(SocketConnection connection);
}