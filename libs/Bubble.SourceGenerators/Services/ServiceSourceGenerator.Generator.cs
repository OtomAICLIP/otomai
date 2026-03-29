using System.Collections.Immutable;
using Bubble.SourceGenerators.Infrastructure;
using Bubble.SourceGenerators.Services.Models;
using Microsoft.CodeAnalysis;

namespace Bubble.SourceGenerators.Services;

public sealed partial class ServiceSourceGenerator
{
    public static void Generate(SourceProductionContext context, ImmutableArray<Service> services, string assemblyName, bool isEnabled)
    {
        if (!isEnabled)
            return;

        var prefix = assemblyName.Split(['.'], StringSplitOptions.RemoveEmptyEntries).Last().Replace("Server", string.Empty);

        var groupedServices = services
            .GroupBy(x => x.Priority)
            .OrderBy(x => x.Key)
            .ToArray();

        var writer = new SourceWriter()
            .AppendLine("namespace {0};", assemblyName)
            .AppendLine()
            .AppendLine("public static class {0}ServiceInitializer", prefix);

        using (writer.CreateScope())
        {
            writer.AppendLine("public static async Task InitializeAsync()");

            using (writer.CreateScope())
            {
                writer
                    .AppendIndentedLine("var sw = System.Diagnostics.Stopwatch.StartNew();")
                    .AppendLine()
                    .AppendIndentedLine("Serilog.Log.Information(\"Initializing {0} services...\");", prefix)
                    .AppendLine();
                
                writer
                    .AppendIndentedLine("var groups = new Func<Task>[][]")
                    .AppendIndentedLine('{')
                    .Indent();

                foreach (var group in groupedServices)
                {
                    var groupComma = groupedServices.Last() == group ? string.Empty : ",";

                    writer
                        .AppendIndentedLine("new Func<Task>[]")
                        .AppendIndentedLine('{')
                        .Indent();

                    foreach (var service in group)
                    {
                        var serviceComma = group.Last() == service ? string.Empty : ",";

                        writer.AppendIndentedLine(service.IsAsync ? "() => {0}.Instance.InitializeAsync(){1}" : "() => Task.Run({0}.Instance.Initialize){1}", service.Name,
                            serviceComma);
                    }

                    writer
                        .Unindent()
                        .AppendLine("}}{0}", groupComma);
                }

                writer
                    .Unindent()
                    .AppendIndentedLine("};")
                    .AppendLine()
                    .AppendIndentedLine("foreach (var group in groups)")
                    .AppendIndentedLine('{')
                    .Indent()
                    .AppendIndentedLine("await Parallel.ForEachAsync(group, async (task, _) => await task());")
                    .Unindent()
                    .AppendIndentedLine('}');
                
                writer
                    .AppendLine()
                    .AppendIndentedLine($"Serilog.Log.Information(\"{prefix} services initialized in {{ElapsedMilliseconds}}ms\", sw.ElapsedMilliseconds);");
            }
            
        }

        context.AddSource($"{prefix}ServiceInitializer.cs", writer.ToSourceText());
    }
}