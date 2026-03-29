namespace BubbleBot.Cli.Services.Maps.World;

public enum TransitionTypeEnum
{
    Unspecified = 0,
    Scroll = 1,
    ScrollAction = 2,
    MapEvent = 4,
    MapAction = 8,
    MapObstacle = 16,
    Interactive = 32,
    NpcAction = 64,
}
