using System.Net;
using System.Net.Sockets;
using Bubble.Core.Network.Internal;
using Bubble.Core.Network.Server;
using Bubble.Core.Network.Transport.Sockets;

namespace Bubble.Core.Network.Transport;

public sealed class SocketsServerBuilder
{
    private readonly List<(EndPoint? EndPoint, int Port, Action<SocketConnectionBuilder> Application)> _bindings;

    public SocketTransportOptions Options { get; }

    public SocketsServerBuilder()
    {
        _bindings = [];
        Options = new SocketTransportOptions();
    }

    internal void Apply(ServerBuilder builder)
    {
        var socketTransportFactory = new SocketTransportFactory(Microsoft.Extensions.Options.Options.Create(Options),
            builder.ApplicationServices.GetLoggerFactory());

        foreach (var binding in _bindings)
            if (binding.EndPoint is null)
            {
                var connectionBuilder = new SocketConnectionBuilder(builder.ApplicationServices);
                binding.Application(connectionBuilder);
                builder.ListenLocalhost(binding.Port, socketTransportFactory, connectionBuilder.Build());
            }
            else builder.Listen(binding.EndPoint, socketTransportFactory, binding.Application);
    }

    public SocketsServerBuilder Listen(EndPoint endPoint, Action<SocketConnectionBuilder> configure)
    {
        _bindings.Add((endPoint, 0, configure));
        return this;
    }

    public SocketsServerBuilder Listen(IPAddress address, int port, Action<SocketConnectionBuilder> configure)
    {
        return Listen(new IPEndPoint(address, port), configure);
    }

    public SocketsServerBuilder ListenAnyIP(int port, Action<SocketConnectionBuilder> configure)
    {
        return Listen(IPAddress.Any, port, configure);
    }

    public SocketsServerBuilder ListenLocalhost(int port, Action<SocketConnectionBuilder> configure)
    {
        _bindings.Add((null, port, configure));
        return this;
    }

    public SocketsServerBuilder ListenUnixSocket(string socketPath, Action<SocketConnectionBuilder> configure)
    {
        return Listen(new UnixDomainSocketEndPoint(socketPath), configure);
    }
}