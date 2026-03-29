using AssetsTools.NET;
using Bubble.Core.Datacenter.Extensions;
using Serilog;

namespace Bubble.Core.Datacenter.Datacenter;

public class DatacenterBehaviour
{
    public required PPtr GameObject { get; set; }
    public required bool Enabled { get; set; }
    public required PPtr Script { get; set; }
    public required string Name { get; set; }
    public required MetadataDictionaryContainer Metadata { get; set; }
    public required DictionaryContainer Data { get; set; }

    public long GetMaxRefId()
    {
        return Metadata.ObjectsById.Values.Max();
    }

    public IDofusObject? GetObject(long id)
    {
        if (Metadata.ObjectsById.TryGetValue(id, out var refId))
            return Data.Objects[refId];

        return null;
    }

    public static DatacenterBehaviour Read(AssetsFileReader reader)
    {
        return new DatacenterBehaviour
        {
            GameObject = PPtr.Read(reader),
            Enabled = reader.ReadBoolean(true),
            Script = PPtr.Read(reader),
            Name = reader.ReadCountStringInt32(true),
            Metadata = MetadataDictionaryContainer.Read(reader),
            Data = DictionaryContainer.Read(reader)
        };
    }   

    public void SetObjects(List<IDofusObject> objects)
    {
        // we have to convert it to a dictionary
        var maxRefId = GetMaxRefId() + 1;

        var dic = new Dictionary<long, IDofusObject>();

        foreach (var obj in objects)
            dic.Add(maxRefId++, obj);

        SetObjects(dic);
    }

    private void SetObjects(Dictionary<long, IDofusObject> objects)
    {
        Metadata.ObjectsById = new Dictionary<long, long>();

        foreach (var (key, value) in objects)
        {
            if (value.PrimaryKey == 0)
                continue;

            Metadata.ObjectsById.Add(key, value.PrimaryKey);
        }

        Data.Objects = objects;
    }

    public void Write(AssetsFileWriter writer)
    {
        GameObject.Write(writer);
        writer.Write(Enabled, true);
        Script.Write(writer);
        writer.WriteCountStringInt32(Name, true);
        Metadata.Write(writer);
        Data.Write(writer);
    }
}

public class DictionaryContainer
{
    public required int Version { get; set; }
    public required int Size { get; set; }
    public required Dictionary<long, IDofusObject> Objects { get; set; }

    public static DictionaryContainer Read(AssetsFileReader reader)
    {
        var version = reader.ReadInt32();
        var size = reader.ReadInt32();

        var dic = new Dictionary<long, IDofusObject>(size);

        for (var i = 0; i < size; i++)
        {
            var key = reader.ReadInt64();
            
            var @class = reader.ReadCountStringInt32(true);
            var ns = reader.ReadCountStringInt32(true);
            var asm = reader.ReadCountStringInt32(true);
            
            var obj = DatacenterObjectFactory.Create($"{asm}.{ns}.{@class}");
            
            obj.Read(reader);
            dic.Add(key, obj);
        }

        reader.Align();

        foreach (var obj in dic.Values)
            obj.AfterRead(dic);

        // we can remove every IDataSubObject
        foreach (var obj in dic)
            if (obj.Value is IDofusSubObject)
                dic.Remove(obj.Key);

        return new DictionaryContainer
        {
            Version = version,
            Size = size,
            Objects = dic
        };
    }

    public void Write(AssetsFileWriter writer)
    {
        writer.Write(Version);
        writer.Write(Size);

        var subObjects = new Dictionary<long, IDofusObject>();

        foreach (var (key, value) in Objects)
        {
            writer.Write(key);

            writer.WriteCountStringInt32(value.Class, true);
            writer.WriteCountStringInt32(value.Namespace, true);
            writer.WriteCountStringInt32(value.Assembly, true);

            value.Write(writer, subObjects);
        }

        foreach (var (key, value) in subObjects)
        {
            writer.Write(key);

            writer.WriteCountStringInt32(value.Class, true);
            writer.WriteCountStringInt32(value.Namespace, true);
            writer.WriteCountStringInt32(value.Assembly, true);

            value.Write(writer, subObjects);
        }

        writer.Align();
    }
}

public class MetadataDictionaryContainer
{
    public required int Size { get; set; }
    public required Dictionary<long, long> ObjectsById { get; set; }

    public static MetadataDictionaryContainer Read(AssetsFileReader reader)
    {
        var size = reader.ReadInt32();
        var keys = new List<long>(size);
        var values = new List<long>(size);
        var dictionary = new Dictionary<long, long>();

        for (var i = 0; i < size; i++)
            keys.Add(reader.ReadInt64());

        reader.Align();

        size = reader.ReadInt32();

        for (var i = 0; i < size; i++)
            values.Add(reader.ReadInt64());

        for (var i = 0; i < size; i++)
            dictionary.Add(keys[i], values[i]);

        return new MetadataDictionaryContainer
        {
            Size = size,
            ObjectsById = dictionary
        };
    }

    public void Write(AssetsFileWriter writer)
    {
        writer.Write(ObjectsById.Count);
        foreach (var key in ObjectsById.Keys)
            writer.Write(key);

        writer.Align();

        writer.Write(ObjectsById.Count);
        foreach (var value in ObjectsById.Values)
            writer.Write(value);
    }
}

public class PPtr
{
    public int FieldId { get; set; }
    public long PathId { get; set; }

    public static PPtr Read(AssetsFileReader reader)
    {
        return new PPtr
        {
            FieldId = reader.ReadInt32(),
            PathId = reader.ReadInt64()
        };
    }

    public void Write(AssetsFileWriter writer)
    {
        writer.Write(FieldId);
        writer.Write(PathId);
    }
}