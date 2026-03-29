using System.Collections.Immutable;
using Bubble.SourceGenerators.Infrastructure;
using Microsoft.CodeAnalysis;

namespace Bubble.SourceGenerators.MessageFactory;

public sealed partial class TypeFactorySourceGenerator
{
    private static void Generate(SourceProductionContext context, ImmutableArray<string> messages, bool isEnabled)
    {
        if (messages.IsEmpty)
            return;

        if (!isEnabled)
            return;

        var writer = new SourceWriter()
            .AppendLine("#nullable enable")
            .AppendLine()
            .AppendLine("using System.IO;")
            .AppendLine("using ProtoBuf;")
            .AppendLine()
            .AppendLine("namespace Bubble.Shared.Protocol;")
            .AppendLine()
            .AppendLine("public static class MessageFactory");

        using (writer.CreateScope())
        {
            writer.AppendIndentedLine("public static object? Create(string typeUrl, byte[] ms)");

            using (writer.CreateScope())
            {
                writer
                    .AppendIndentedLine("return typeUrl switch")
                    .AppendIndentedLine('{')
                    .Indent();

                foreach (var message in messages)
                    writer.AppendIndentedLine("var _ when typeUrl == {0}.TypeUrl => Serializer.Deserialize<{0}>(ms.AsSpan()),", message);

                writer
                    .AppendIndentedLine("_ => null")
                    .Unindent()
                    .AppendIndentedLine("};");
            }
        }

        context.AddSource("MessageFactory.g.cs", writer.ToSourceText());
    }
}