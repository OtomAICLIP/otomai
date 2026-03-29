using System.Text.Json.Serialization;

namespace BubbleBot.Cli.Services.TreasureHunts.Models;

public class DofusDbResponseData
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("posX")]
    public int PosX { get; set; }
    
    [JsonPropertyName("posY")]
    public int PosY { get; set; }
    
    [JsonPropertyName("distance")]
    public int Distance { get; set; }
    
    [JsonPropertyName("pois")]
    public List<DofusDbResponseDataPoi> Pois { get; set; } = new();
}