using System.Text.Json.Serialization;

namespace BubbleBot.Cli.Services.TreasureHunts.Models;

public class DofusPourLesNoobsFile
{
    [JsonPropertyName("clues")]
    public List<DofusPourLesNoobsClueInfo> Clues { get; set; } = new List<DofusPourLesNoobsClueInfo>();
    [JsonPropertyName("maps")]
    public List<DofusPourLesNoobsClueMapInfo> Maps { get; set; } = new List<DofusPourLesNoobsClueMapInfo>();

}