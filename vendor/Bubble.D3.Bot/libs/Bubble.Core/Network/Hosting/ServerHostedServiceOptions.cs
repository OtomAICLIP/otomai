using Bubble.Core.Network.Server;

namespace Bubble.Core.Network.Hosting;

public sealed class ServerHostedServiceOptions
{
    public required ServerBuilder ServerBuilder { get; set; }
}