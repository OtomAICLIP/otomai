using AssetsTools.NET;

namespace Bubble.Core.Datacenter.Datacenter.WorldGraph;

public class WorldGraphVertex
{
    public required long MapId { get; set; }
    public required int ZoneId { get; set; }
    public required ulong Uid { get; set; }

    public static WorldGraphVertex Read(AssetsFileReader reader)
    {
        var vertice = new WorldGraphVertex
        {
            MapId = reader.ReadInt64(),
            ZoneId = reader.ReadInt32(),
            Uid = reader.ReadUInt64()
        };
        return vertice;
    }
}