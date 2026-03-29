using System.Net;
using Bubble.Core.Network.Internal;
using Bubble.Core.Network.Transport;
using Bubble.Core.Network.Transport.Sockets;

namespace Bubble.Core.Network.Server;

public sealed class ServerBuilder
{
    public IList<ServerBinding> Bindings { get; }

    public TimeSpan HeartBeatInterval { get; set; }

    public IServiceProvider ApplicationServices { get; }

    public ServerBuilder(IServiceProvider serviceProvider)
    {
        ApplicationServices = serviceProvider;
        Bindings = [];
        HeartBeatInterval = TimeSpan.FromSeconds(1);
    }

    public ServerBuilder() : this(EmptyServiceProvider.Instance)
    {
    }

    public NetworkServer Build()
    {
        return new NetworkServer(this);
    }
}

public static class ServerBuilderExtensions
{
    public static ServerBuilder Listen(this ServerBuilder builder, EndPoint endPoint, SocketTransportFactory connectionListenerFactory, Action<SocketConnectionBuilder> configure)
    {
        var connectionBuilder = new SocketConnectionBuilder(builder.ApplicationServices);
        configure(connectionBuilder);

        builder.Bindings.Add(new ServerBinding(endPoint, connectionBuilder.Build(), connectionListenerFactory));
        return builder;
    }

    public static ServerBuilder Listen(this ServerBuilder builder, EndPoint endPoint, SocketTransportFactory connectionListenerFactory, SocketConnectionDelegate application)
    {
        builder.Bindings.Add(new ServerBinding(endPoint, application, connectionListenerFactory));
        return builder;
    }

    public static ServerBuilder ListenLocalhost(this ServerBuilder builder, int port, SocketTransportFactory connectionListenerFactory, Action<SocketConnectionBuilder> configure)
    {
        var connectionBuilder = new SocketConnectionBuilder(builder.ApplicationServices);
        configure(connectionBuilder);

        builder.Bindings.Add(new LocalHostBinding(port, connectionBuilder.Build(), connectionListenerFactory));
        return builder;
    }

    public static ServerBuilder ListenLocalhost(this ServerBuilder builder, int port, SocketTransportFactory connectionListenerFactory, SocketConnectionDelegate application)
    {
        builder.Bindings.Add(new LocalHostBinding(port, application, connectionListenerFactory));
        return builder;
    }
}