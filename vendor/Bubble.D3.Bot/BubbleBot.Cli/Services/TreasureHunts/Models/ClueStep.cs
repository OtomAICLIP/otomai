using BubbleBot.Cli.Repository.Maps;

namespace BubbleBot.Cli.Services.TreasureHunts.Models;

public class ClueStep
{
    public int FromX { get; set; }
    public int FromY { get; set; }
    public long FropMapId { get; set; }
    public int ClueId { get; set; }
    public int MapX { get; set; }
    public int MapY { get; set; }
    public long ToMapId { get; set; } = -1;
    public Direction Direction { get; set; }
    public int Distance { get; set; }
    public int PhorreurId { get; set; }
}
