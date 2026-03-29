using AssetsTools.NET;

namespace Bubble.Core.Datacenter.Datacenter.WorldGraph;

public class WorldGraphEdge
{
    public required WorldGraphVertex From { get; set; }
    public required WorldGraphVertex To { get; set; }
    public required List<WorldGraphTransition> Transitions { get; set; }

    public static WorldGraphEdge Read(AssetsFileReader reader)
    {
        var from = WorldGraphVertex.Read(reader);
        var to = WorldGraphVertex.Read(reader);
        
        var transitionCount = reader.ReadInt32();
        var transitions = new List<WorldGraphTransition>(transitionCount);
        for (var i = 0; i < transitionCount; i++)
        {
            transitions.Add(WorldGraphTransition.Read(reader));
        }
        
        return new WorldGraphEdge
        {
            From = from,
            To = to,
            Transitions = transitions
        };
    }
}