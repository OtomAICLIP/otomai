#nullable disable

namespace Bubble.SourceGenerators.MessageDispatcher.Models;

public sealed class MessageHandler
{
    public string HandlerTypeName { get; set; }

    public string HandlerMethodName { get; set; }

    public string MessageTypeName { get; set; }

    public string SessionTypeName { get; set; }

    public MessageTypes Type { get; set; }
}