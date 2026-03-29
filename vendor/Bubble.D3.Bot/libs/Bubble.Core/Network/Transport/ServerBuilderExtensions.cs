using Bubble.Core.Network.Server;

namespace Bubble.Core.Network.Transport;

public static class ServerBuilderExtensions
{
    public static ServerBuilder UseSockets(this ServerBuilder serverBuilder, Action<SocketsServerBuilder> configure)
    {
        var socketsBuilder = new SocketsServerBuilder();
        configure(socketsBuilder);

        socketsBuilder.Apply(serverBuilder);
        return serverBuilder;
    }
}