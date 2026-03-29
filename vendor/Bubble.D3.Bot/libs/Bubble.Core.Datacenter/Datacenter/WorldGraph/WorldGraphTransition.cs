using AssetsTools.NET;

namespace Bubble.Core.Datacenter.Datacenter.WorldGraph;

public class WorldGraphTransition
{
    public int Type { get; set; }
    public int Direction { get; set; }
    public int SkillId { get; set; }
    public string Criterion { get; set; } = string.Empty;
    public long TransitionMapId { get; set; }
    public int CellId { get; set; }
    public long Id { get; set; }

    public static WorldGraphTransition Read(AssetsFileReader reader)
    {
        var type = reader.ReadInt32();
        var direction = reader.ReadInt32();
        var skillId = reader.ReadInt32();
        var criterions = reader.ReadCountStringInt32();
        reader.Align();

        var transitionMapId = reader.ReadInt64();
        var cellId = reader.ReadInt32();
        var id = reader.ReadInt64();

        return new WorldGraphTransition
        {
            Type = type,
            Direction = direction,
            SkillId = skillId,
            Criterion = criterions,
            TransitionMapId = transitionMapId,
            CellId = cellId,
            Id = id
        };
    }
}