namespace Bubble.SourceGenerators.MessageDispatcher.Attributes;

public sealed class MessageHandlerAttribute
{
    public const string Name = "MessageHandler";

    public const string GlobalName = $"{Name}.g.cs";

    public const string Source =
        """
        using System;

        namespace Dofus.Protocol;

        [AttributeUsage(AttributeTargets.Method)]
        internal sealed class MessageHandlerAttribute : Attribute { }
        """;
}