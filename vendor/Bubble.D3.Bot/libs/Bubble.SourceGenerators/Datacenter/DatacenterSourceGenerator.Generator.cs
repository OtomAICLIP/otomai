using System.Collections.Immutable;
using Bubble.SourceGenerators.Datacenter.Models;
using Bubble.SourceGenerators.Infrastructure;
using Bubble.SourceGenerators.Infrastructure.Extensions;
using Microsoft.CodeAnalysis;

namespace Bubble.SourceGenerators.Datacenter;

public sealed partial class DatacenterSourceGenerator
{
    private static void Generate(SourceProductionContext context, ImmutableArray<DatacenterObject> datacenterObjects, bool isEnabled)
    {
        if (!isEnabled)
            return;

        if (datacenterObjects.IsEmpty)
            return;

        GenerateDatacenterObjectFactory(context, datacenterObjects);
        GeneratePartialClasses(context, datacenterObjects);
    }

    private static void GenerateDatacenterObjectFactory(SourceProductionContext context, ImmutableArray<DatacenterObject> datacenterObjects)
    {
        var writer = new SourceWriter()
            .AppendLine("namespace Bubble.Core.Datacenter.Datacenter;")
            .AppendLine()
            .AppendLine("public static class DatacenterObjectFactory");

        using (writer.CreateScope())
        {
            writer.AppendIndentedLine("public static IDofusObject Create(string fullClassName)");

            using (writer.CreateScope())
            {
                writer
                    .AppendIndentedLine("return fullClassName switch")
                    .AppendIndentedLine('{')
                    .Indent();

                foreach (var datacenterObject in datacenterObjects)
                    writer.AppendIndentedLine("\"{0}.{1}.{2}\" => {3}.{2}.Empty,", datacenterObject.ObjectAssembly, datacenterObject.ObjectNamespace, datacenterObject.ObjectName,
                        datacenterObject.Namespace);

                writer
                    .AppendIndentedLine("_ => throw new Exception($\"Unknown datacenter object: {fullClassName}.\")")
                    .Unindent()
                    .AppendIndentedLine("};");
            }
        }

        context.AddSource("DatacenterObjectFactory.g.cs", writer.ToSourceText());
    }

    private static void GeneratePartialClasses(SourceProductionContext context, ImmutableArray<DatacenterObject> datacenterObjects)
    {
        foreach (var datacenterObject in datacenterObjects)
        {
            var writer = new SourceWriter()
                .AppendLine("using AssetsTools.NET;")
                .AppendLine("using Bubble.Core.Datacenter.Extensions;")
                .AppendLine()
                .AppendLine("namespace {0};", datacenterObject.Namespace)
                .AppendLine()
                .AppendLine("public {0}partial class {1} : IDofusObject", datacenterObject.IsSealed ? "sealed " : string.Empty, datacenterObject.Name);

            using (writer.CreateScope())
            {
                writer
                    .AppendIndentedLine("[JsonIgnore]")
                    .AppendIndentedLine("public {1}string Namespace => \"{0}\";", datacenterObject.ObjectNamespace, datacenterObject.HasBaseType ? "new " : string.Empty)
                    .AppendLine()
                    .AppendIndentedLine("[JsonIgnore]")
                    .AppendIndentedLine("public {1}string Class => \"{0}\";", datacenterObject.ObjectName, datacenterObject.HasBaseType ? "new " : string.Empty)
                    .AppendLine()
                    .AppendIndentedLine("[JsonIgnore]")
                    .AppendIndentedLine("public {1}string Assembly => \"{0}\";", datacenterObject.ObjectAssembly, datacenterObject.HasBaseType ? "new " : string.Empty)
                    .AppendLine()
                    .AppendIndentedLine("[JsonIgnore]")
                    .AppendIndentedLine("public {1}int PrimaryKey => (int){0};", datacenterObject.ObjectPrimaryKey, datacenterObject.HasBaseType ? "new " : string.Empty)
                    .AppendLine();

                foreach (var linkedProperty in datacenterObject.Properties.Where(x => x.LinkedProperty is not null).Select(x => x.LinkedProperty!))
                    writer
                        .AppendIndentedLine("public Dictionary<long, {0}> {1} {{ get; set; }} = [];", linkedProperty.Type, linkedProperty.Name)
                        .AppendLine();

                foreach (var propertyName in datacenterObject.Properties.Where(x => x.IsText).Select(x => x.Name.Replace("Id", string.Empty)))
                    writer
                        .AppendIndentedLine("public string {0} {{ get; private set; }}", propertyName)
                        .AppendLine();

                writer
                    .AppendIndentedLine("public {1}static {0} Empty => new {0}", datacenterObject.Name, datacenterObject.HasBaseType ? "new " : string.Empty)
                    .AppendIndentedLine('{')
                    .Indent();

                foreach (var property in datacenterObject.Properties)
                    writer.AppendIndentedLine("{0} = default!,", property.Name);

                foreach (var propertyName in datacenterObject.BasePropertiesNames)
                    writer.AppendIndentedLine("{0} = default!,", propertyName);

                writer
                    .Unindent()
                    .AppendIndentedLine("};")
                    .AppendLine()
                    .AppendIndentedLine("public {0}static {1} ReadFrom(AssetsFileReader reader)", datacenterObject.HasBaseType ? "new " : string.Empty, datacenterObject.Name);

                using (writer.CreateScope())
                    writer.AppendIndentedLine("return ({0})Empty.Read(reader);", datacenterObject.Name);

                writer
                    .AppendLine()
                    .AppendIndentedLine("public {0} IDofusObject Read(AssetsFileReader reader)",
                        datacenterObject.HasBaseType ? "override" : datacenterObject.IsSealed ? string.Empty : "virtual");

                using (writer.CreateScope())
                {
                    if (datacenterObject.HasBaseType)
                        writer.AppendIndentedLine("base.Read(reader);");

                    foreach (var property in datacenterObject.Properties)
                    {
                        var typeIsObject = string.IsNullOrEmpty(property.MethodName);

                        var propertyType = typeIsObject ? property.Type.GetLastSegment('.') : property.Type;

                        var isModuloFour = property.MethodName is "Int32" or "UInt32" or "Single" or "Int64" or "UInt64" or "Double" or "Decimal";

                        var listReadMethod = typeIsObject
                            ? $"() => {propertyType}.ReadFrom(reader)"
                            : $"reader.Read{property.MethodName}";

                        var enumMethodName = property.Type switch
                        {
                            "byte" => "Byte",
                            "sbyte" => "SByte",
                            "bool" or "System.Boolean" => "Boolean",
                            "char" or "System.Char" => "Char",
                            "decimal" or "System.Decimal" => "Decimal",
                            "double" or "System.Double" => "Double",
                            "float" or "System.Single" => "Single",
                            "int" or "System.Int32" => "Int32",
                            "uint" or "System.UInt32" => "UInt32",
                            "long" or "System.Int64" => "Int64",
                            "ulong" or "System.UInt64" => "UInt64",
                            "ushort" or "System.UInt16" => "UInt16",
                            "short" or "System.Int16" => "Int16",
                            "string" or "System.String" => "CountStringInt32Aligned",
                            _ => string.Empty
                        };

                        var readMethod = typeIsObject && property.SpecialType is null
                            ? $"{property.Name} = {propertyType}.ReadFrom(reader);"
                            : $"{property.Name} = {(property.SpecialType is not null ? $"({property.SpecialType})" : string.Empty)}reader.Read{(property.SpecialType is not null ? enumMethodName : property.MethodName)}{(!isModuloFour ? "(true)" : "()")};";

                        if (property.IsListOfList)
                            writer.AppendIndentedLine("{0} = reader.ReadList(() => reader.ReadList({1}, true), true);", property.Name, listReadMethod);

                        if (property.IsList)
                            writer.AppendIndentedLine("{0} = reader.ReadList({1}, true);", property.Name, listReadMethod);

                        if (property is { IsList: false, IsListOfList: false })
                        {
                            writer.AppendIndentedLine(readMethod);

                            if (property.IsText)
                            {
                                var cast = property.MethodName switch
                                {
                                    "Int32" => "(uint)",
                                    "Int16" => "(uint)",
                                    "Int64" => "(uint)",
                                    _ => string.Empty
                                };

                                writer.AppendIndentedLine("{0} = DatacenterService.GetText({1}{2});", property.Name.Replace("Id", string.Empty), cast, property.Name);
                            }
                        }
                    }

                    writer.AppendIndentedLine("return this;");
                }

                writer
                    .AppendLine()
                    .AppendIndentedLine("public {0} void AfterRead(IDictionary<long, IDofusObject> objects)",
                        datacenterObject.HasBaseType ? "override" : datacenterObject.IsSealed ? string.Empty : "virtual");

                using (writer.CreateScope())
                {
                    if (datacenterObject.HasBaseType)
                        writer.AppendIndentedLine("base.AfterRead(objects);");

                    foreach (var property in datacenterObject.Properties.Where(x => x.LinkedProperty is not null))
                        if (property.LinkedProperty is { } linkedProperty)
                        {
                            writer
                                .AppendIndentedLine("foreach (var item in {0})", property.Name);

                            using (writer.CreateScope())
                            {
                                writer.AppendIndentedLine("if (objects.TryGetValue(item, out var value))");

                                using (writer.CreateScope())
                                    writer.AppendIndentedLine("{0}.Add(item, ({1})value);", linkedProperty.Name, linkedProperty.Type);
                            }
                        }
                }

                writer
                    .AppendLine()
                    .AppendIndentedLine("public {0}void Write(AssetsFileWriter writer, IDictionary<long, IDofusObject> writeAfter)",
                        datacenterObject.HasBaseType ? "override " : datacenterObject.IsSealed ? string.Empty : "virtual ");

                using (writer.CreateScope())
                {
                    if (datacenterObject.HasBaseType)
                        writer.AppendIndentedLine("base.Write(writer, writeAfter);");

                    foreach (var property in datacenterObject.Properties)
                    {
                        var typeIsObject = string.IsNullOrEmpty(property.MethodName);

                        var isModuloFour = property.MethodName is "Int32" or "UInt32" or "Single" or "Int64" or "UInt64" or "Double" or "Decimal";

                        var methodName = property.MethodName is "CountStringInt32Aligned" ? property.MethodName : string.Empty;

                        var listWriteMethod =
                            $"{(!isModuloFour && typeIsObject ? "z => " : string.Empty)}{(typeIsObject ? "z.Write" : "writer.Write")}{methodName}{(!isModuloFour ? $"{(typeIsObject ? "(writer, writeAfter)" : string.Empty)}" : string.Empty)}";

                        var writeMethod = typeIsObject && property.SpecialType is null
                            ? $"{property.Name}.Write(writer, writeAfter);"
                            : $"writer.Write{methodName}({(property.SpecialType is not null ? $"({property.Type})" : string.Empty)}{property.Name}{(!isModuloFour ? ", true" : string.Empty)});";

                        if (property.IsListOfList)
                            writer.AppendIndentedLine("writer.WriteList({0}, x => writer.WriteList(x, {1}, true), true);", property.Name, listWriteMethod);

                        if (property.IsList)
                        {
                            if (property.LinkedProperty is { } linkedProperty)
                            {
                                writer
                                    .AppendIndentedLine("writer.WriteList({0}.Select(x => x.Key).ToList(), writer.Write, true);", linkedProperty.Name)
                                    .AppendIndentedLine("foreach (var item in {0})", linkedProperty.Name);

                                using (writer.CreateScope())
                                    writer.AppendIndentedLine("writeAfter.Add(item.Key, item.Value);");
                            }
                            else
                                writer.AppendIndentedLine("writer.WriteList({0}, {1}, true);", property.Name, listWriteMethod);
                        }

                        if (property is { IsList: false, IsListOfList: false })
                        {
                            if (typeIsObject)
                                writer.AppendIndentedLine("{0}", writeMethod);
                            else
                                writer.AppendIndentedLine(writeMethod);
                        }
                    }
                }
            }

            context.AddSource($"{datacenterObject.Name}.g.cs", writer.ToSourceText());
        }
    }
}