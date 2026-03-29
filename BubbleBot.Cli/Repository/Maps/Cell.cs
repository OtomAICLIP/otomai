using System.Text.Json.Serialization;

namespace BubbleBot.Cli.Repository.Maps;

public class Cell
{
    [JsonPropertyName("cellNumber")] public int Id { get; set; }

    [JsonPropertyName("speed")] public int Speed { get; set; }

    [JsonPropertyName("mapChangeData")] public int MapChangeData { get; set; }

    [JsonPropertyName("moveZone")] public int MoveZone { get; set; }

    [JsonPropertyName("linkedZone")] public int LinkedZone { get; set; }
    
    public int LinkedZoneRp => (LinkedZone & 0xF0) >> 4;

    [JsonPropertyName("mov")] public bool Mov { get; set; }

    [JsonPropertyName("los")] public bool Los { get; set; }

    [JsonPropertyName("nonWalkableDuringFight")]
    public bool NonWalkableDuringFight { get; set; }

    [JsonPropertyName("nonWalkableDuringRP")]
    public bool NonWalkableDuringRp { get; set; }

    [JsonPropertyName("havenbagCell")] public bool HavenbagCell { get; set; }

    [JsonPropertyName("farmCell")] public bool FarmCell { get; set; }

    [JsonPropertyName("visible")] public bool Visible { get; set; }

    [JsonPropertyName("floor")] public int Floor { get; set; }

    [JsonPropertyName("red")] public bool Red { get; set; }

    [JsonPropertyName("blue")] public bool Blue { get; set; }

    [JsonPropertyName("arrow")] public int Arrow { get; set; }
}