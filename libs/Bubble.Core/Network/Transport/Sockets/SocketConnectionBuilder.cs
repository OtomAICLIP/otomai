using System.Diagnostics.CodeAnalysis;
using Bubble.Core.Network.Transport.Sockets;
using Bubble.Core.Network.Transport.Sockets.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Bubble.Core.Network.Transport;

public class SocketConnectionBuilder
{
    private readonly IList<Func<SocketConnectionDelegate, SocketConnectionDelegate>> _components = [];

    public IServiceProvider ApplicationServices { get; }

    public SocketConnectionBuilder(IServiceProvider applicationServices)
    {
        ApplicationServices = applicationServices;
    }

    public SocketConnectionDelegate Build()
    {
        SocketConnectionDelegate app = _ => Task.CompletedTask;

        foreach (var component in _components.Reverse())
            app = component(app);

        return app;
    }

    public SocketConnectionBuilder Run(Func<SocketConnection, Task> middleware)
    {
        return Use(_ =>
        {
            return context => middleware(context);
        });
    }

    public SocketConnectionBuilder Use(Func<SocketConnectionDelegate, SocketConnectionDelegate> middleware)
    {
        _components.Add(middleware);
        return this;
    }

    public SocketConnectionBuilder Use(Func<SocketConnection, Func<Task>, Task> middleware)
    {
        return Use(next =>
        {
            return context =>
            {
                return middleware(context, SimpleNext);

                Task SimpleNext()
                {
                    return next(context);
                }
            };
        });
    }

    public SocketConnectionBuilder Use(Func<SocketConnection, SocketConnectionDelegate, Task> middleware)
    {
        return Use(next => context => middleware(context, next));
    }

    public SocketConnectionBuilder UseConnectionHandler<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TConnectionHandler>()
        where TConnectionHandler : SocketConnectionHandler
    {
        var handler = ActivatorUtilities.GetServiceOrCreateInstance<TConnectionHandler>(ApplicationServices);

        // This is a terminal middleware, so there's no need to use the 'next' parameter
        return Run(handler.OnConnectedAsync);
    }
}