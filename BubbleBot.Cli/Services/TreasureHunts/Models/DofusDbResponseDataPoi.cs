using System.Text.Json.Serialization;

namespace BubbleBot.Cli.Services.TreasureHunts.Models;

public class DofusDbResponseDataPoi
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
}