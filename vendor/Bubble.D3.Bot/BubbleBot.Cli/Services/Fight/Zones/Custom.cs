using Bubble.DamageCalculation;
using BubbleBot.Cli.Repository.Maps;

namespace BubbleBot.Cli.Services.Fight.Zones;

public class Custom : DisplayZone
{
    public IList<Cell> Cells { get; }

    public Custom(IList<Cell> cells, Map map) : base(SpellShape.Unknown, 0, 0, map)
    {
        Cells = cells;
    }

    public override IEnumerable<Cell> GetCells(uint cellId = 0)
    {
        return Cells;
    }

    public override uint Surface => (uint)Cells.Count;
}