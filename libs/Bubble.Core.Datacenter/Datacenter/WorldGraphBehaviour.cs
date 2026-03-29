using AssetsTools.NET;
using Bubble.Core.Datacenter.Datacenter.WorldGraph;
using Bubble.Core.Datacenter.Extensions;

namespace Bubble.Core.Datacenter.Datacenter;


public class WorldGraphBehaviour
{
    public required PPtr GameObject { get; set; }
    public required bool Enabled { get; set; }
    public required PPtr Script { get; set; }
    public required string Name { get; set; }
    public required WorldGraphEntry Object { get; set; }


    public static WorldGraphBehaviour Read(AssetsFileReader reader)
    {
        return new WorldGraphBehaviour
        {
            GameObject = PPtr.Read(reader),
            Enabled = reader.ReadBoolean(true),
            Script = PPtr.Read(reader),
            Name = reader.ReadCountStringInt32(true),
            Object = ReadObject(reader),
        };
    }

    private static WorldGraphEntry ReadObject(AssetsFileReader reader)
    {
        var vertices = ReadVertices(reader);
        var edges = ReadEdges(reader);
        var outGoingEdges = ReadOutgoingEdges(reader);

        return new WorldGraphEntry
        {
            Vertices = vertices,
            Edges = edges,
            OutGoingEdges = outGoingEdges,
        };
    }

    private static Dictionary<long, Dictionary<long, WorldGraphEdge>> ReadEdges(AssetsFileReader reader)
    {      
        var len = reader.ReadInt32();

        var edges = new Dictionary<long, Dictionary<long, WorldGraphEdge>>(len);
        
        var keys = new long[len];
        for (var i = 0; i < len; i++)
        {
            keys[i] = reader.ReadInt64();
        }

        var totalLen = reader.ReadInt32();
        foreach (var key in keys)
        {
            if (key == 230)
            {
                
            }
            var keysLen = reader.ReadInt32();
            var edgesDict = new Dictionary<long, WorldGraphEdge>(keysLen);
            var valueKeys = new List<long>(keysLen);
            
            for (var i = 0; i < keysLen; i++)
            {
                valueKeys.Add(reader.ReadInt64());
            }
            
            var valuesLen = reader.ReadInt32();
            var values = new List<WorldGraphEdge>(valuesLen);
            for (var i = 0; i < valuesLen; i++)
            {
                values.Add(WorldGraphEdge.Read(reader));
            }
            
            for (var i = 0; i < keysLen; i++)
            {
                edgesDict.Add(valueKeys[i], values[i]);
            }
            
            edges.Add(key, edgesDict);
        }
        
        return edges;
    }
    
    private static Dictionary<long, List<WorldGraphEdge>> ReadOutgoingEdges(AssetsFileReader reader)
    {      
        var len = reader.ReadInt32();

        var edges = new Dictionary<long, List<WorldGraphEdge>>(len);
        
        var keys = new long[len];
        for (var i = 0; i < len; i++)
        {
            keys[i] = reader.ReadInt64();
        }
        
        var totalLen = reader.ReadInt32();
        for (var i = 0; i < len; i++)
        {
            var valuesLen = reader.ReadInt32();
            var values = new List<WorldGraphEdge>();
            for (var j = 0; j < valuesLen; j++)
            {
                values.Add(WorldGraphEdge.Read(reader));
            }

            edges.Add(keys[i], values);
        }
        
        return edges;
    }

    private static Dictionary<long, Dictionary<int, WorldGraphVertex>> ReadVertices(AssetsFileReader reader)
    {
        var len = reader.ReadInt32();
        var vertices = new Dictionary<long, Dictionary<int, WorldGraphVertex>>(len);
        var keys = new long[len];
        for (var i = 0; i < len; i++)
        {
            keys[i] = reader.ReadInt64();
        }
        
        var totalLen = reader.ReadInt32();

        foreach (var key in keys)
        {       
            var keysLen = reader.ReadInt32();

            var verticesDict = new Dictionary<int, WorldGraphVertex>(keysLen);
            var valueKeys = new List<int>(keysLen);
            for (var i = 0; i < keysLen; i++)
            {
                valueKeys.Add(reader.ReadInt32());
            }
            
            var valuesLen = reader.ReadInt32();
            var values = new List<WorldGraphVertex>(valuesLen);
            for (var i = 0; i < valuesLen; i++)
            {
                values.Add(WorldGraphVertex.Read(reader));
            }

            for (var i = 0; i < keysLen; i++)
            {
                verticesDict.Add(valueKeys[i], values[i]);
            }
            
            vertices.Add(key, verticesDict);
        }
        
        return vertices;
    }

    public void Write(AssetsFileWriter writer)
    {
        GameObject.Write(writer);
        writer.Write(Enabled, true);
        Script.Write(writer);
        writer.WriteCountStringInt32(Name, true);
    }
}
