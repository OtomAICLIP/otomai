using System.Text.Json.Serialization;

namespace BubbleBot.Cli.Services.TreasureHunts.Models;

public class DofusPourLesNoobsClueInfo
{
    [JsonPropertyName("clueid")]
    public int ClueId { get; set; }
    
    [JsonPropertyName("hintfr")]
    public string HintFr { get; set; }
}