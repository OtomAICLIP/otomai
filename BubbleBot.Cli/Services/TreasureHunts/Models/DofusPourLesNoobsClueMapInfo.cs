
using System.Text.Json.Serialization;

namespace BubbleBot.Cli.Services.TreasureHunts.Models;

public class DofusPourLesNoobsClueMapInfo
{
    [JsonPropertyName("x")] public int X { get; set; }

    [JsonPropertyName("y")] public int Y { get; set; }
    
    [JsonPropertyName("clues")] public List<int> Clues { get; set; } = new List<int>();
}