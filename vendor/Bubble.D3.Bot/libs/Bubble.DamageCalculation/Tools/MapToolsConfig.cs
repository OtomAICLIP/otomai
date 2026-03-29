namespace Bubble.DamageCalculation.Tools;

public class MapToolsConfig
{
    public static readonly MapToolsConfig Dofus2Config = new()
    {
        MapGridWidth  = 14,
        MapGridHeight = 20,
        MinXCoord     = 0,
        MaxXCoord     = 33,
        MinYCoord     = -19,
        MaxYCoord     = 13,
    };

    public required int MinYCoord { get; init; }
    public required int MinXCoord { get; init; }
    public required int MaxYCoord { get; init; }
    public required int MaxXCoord { get; init; }
    public required int MapGridWidth { get; init; }
    public required int MapGridHeight { get; init; }

    public MapToolsConfig()
    {
    }
}