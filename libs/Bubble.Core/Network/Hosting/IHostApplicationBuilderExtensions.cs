using System.Net;
using Bubble.Core.Network.Server;
using Bubble.Core.Network.Transport;
using Bubble.Core.Network.Transport.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bubble.Core.Network.Hosting;

public static class IHostApplicationBuilderExtensions
{
    public static IHostApplicationBuilder AddServer<TConnectionHandler>(this IHostApplicationBuilder builder)
        where TConnectionHandler : SocketConnectionHandler
    {
        var ipAddress = IPAddress.Any;
        var port = builder.Configuration.GetValue<int>("Server:Port");

        return builder.ConfigureServer(serverBuilder =>
        {
            serverBuilder.UseSockets(socketsServerBuilder =>
            {
                socketsServerBuilder.Listen(ipAddress, port, connectionBuilder =>
                {
                    connectionBuilder.UseConnectionHandler<TConnectionHandler>();
                });
            });
        });
    }

    private static IHostApplicationBuilder ConfigureServer(this IHostApplicationBuilder builder, Action<ServerBuilder> configure)
    {
        builder
            .Services
            .AddHostedService<ServerHostedService>()
            .AddOptions<ServerHostedServiceOptions>()
            .Configure<IServiceProvider>((options, sp) =>
            {
                options.ServerBuilder = new ServerBuilder(sp);
                configure(options.ServerBuilder);
            });

        return builder;
    }
}