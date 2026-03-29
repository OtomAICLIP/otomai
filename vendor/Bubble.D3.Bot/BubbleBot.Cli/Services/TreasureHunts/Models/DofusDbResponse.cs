
using System.Text.Json.Serialization;

namespace BubbleBot.Cli.Services.TreasureHunts.Models;

public class DofusDbResponse
{
    [JsonPropertyName("total")]
    public int Total { get; set; }
    
    [JsonPropertyName("limit")]
    public int Limit { get; set; }
    
    [JsonPropertyName("skip")]
    public int Skip { get; set; }
    
    [JsonPropertyName("data")]
    public List<DofusDbResponseData> Data { get; set; } = new();
}