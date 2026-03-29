using System.Net;
using Bubble.Core.Network.Server;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Bubble.Core.Network.Hosting;

public sealed class ServerHostedService : IHostedService
{
    private readonly NetworkServer _server;

    public IEnumerable<EndPoint> EndPoints =>
        _server.EndPoints;

    public ServerHostedService(IOptions<ServerHostedServiceOptions> options)
    {
        _server = options.Value.ServerBuilder.Build();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return _server.StartAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _server.StopAsync(cancellationToken);
    }
}