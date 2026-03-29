using System.Collections.Immutable;
using Bubble.SourceGenerators.Infrastructure;
using Bubble.SourceGenerators.MessageDispatcher.Models;
using Microsoft.CodeAnalysis;

namespace Bubble.SourceGenerators.MessageDispatcher;

public sealed partial class MessageDispatcherSourceGenerator
{
    private static void Generate(SourceProductionContext context, ImmutableArray<MessageHandler> handlers, bool isEnabled)
    {
        if (!isEnabled)
            return;

        if (handlers.IsEmpty)
            return;

        var writer = new SourceWriter()
            .AppendLine("using Bubble.Core.Extensions;")
            .AppendLine("using Bubble.Core.Network.Dispatcher;")
            .AppendLine("using Bubble.Servers.GameServer.Network.Transport;")
            .AppendLine("using Serilog;")
            .AppendLine("using Com.ankama.dofus.server.game.protocol;")
            .AppendLine("using ProtoBuf;")
            .AppendLine()
            .AppendLine("namespace Bubble.Servers.GameServer.Network.Dispatcher;")
            .AppendLine()
            .AppendLine("public static class MessageDispatcher");

        using (writer.CreateScope())
        {
            writer.AppendIndentedLine("public static async Task<DispatchResult> DispatchMessageAsync(DofusSession session, GameMessage message)");

            using (writer.CreateScope())
            {
                writer
                    .AppendIndentedLine("var task = Task.CompletedTask;")
                    .AppendIndentedLine("var result = DispatchResult.NotFound;")
                    .AppendIndentedLine("var type = message.Request.Content.TypeUrl.GetLastSegment('/');")
                    .AppendLine()
                    .AppendIndentedLine("switch (message.ContentCase)");

                using (writer.CreateScope())
                {
                    foreach (var handler in handlers)
                        writer
                            .AppendIndentedLine("case GameMessage.ContentOneofCase.{0} when type == {1}.TypeUrl:", handler.Type, handler.MessageTypeName)
                            .Indent()
                            .AppendIndentedLine("session.SetUid<{0}>(message.{1}.Uid);", handler.MessageTypeName, handler.Type)
                            .AppendIndentedLine("task = {0}.{1}(session, Serializer.Deserialize<{2}>(message.Request.Content.Value.AsSpan()));", handler.HandlerTypeName,
                                handler.HandlerMethodName, handler.MessageTypeName)
                            .AppendIndentedLine("result = DispatchResult.Success;")
                            .Unindent()
                            .AppendIndentedLine("break;");

                    writer
                        .AppendIndentedLine("case GameMessage.ContentOneofCase.None:")
                        .AppendIndentedLine("default:")
                        .Indent()
                        .AppendIndentedLine("task = Task.CompletedTask;")
                        .Unindent()
                        .AppendIndentedLine("break;");
                }

                writer
                    .AppendLine()
                    .AppendIndentedLine("if (!task.IsCompletedSuccessfully)")
                    .Indent()
                    .AppendIndentedLine("await task.ConfigureAwait(false);")
                    .Unindent()
                    .AppendLine()
                    .AppendIndentedLine("return result;");
            }
        }

        context.AddSource("MessageDispatcher.g.cs", writer.ToSourceText());
    }
}