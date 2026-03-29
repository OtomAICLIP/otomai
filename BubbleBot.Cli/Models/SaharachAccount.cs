using System.Text.Json.Serialization;
using Bubble.Shared;

namespace BubbleBot.Cli.Models;

public class SaharachAccount
{
    [JsonPropertyName("id")]
    public int Id { get; set; }
    
    [JsonPropertyName("hardwareId")]
    public string HardwareId { get; set; } = string.Empty;
    
    [JsonPropertyName("server")]
    public int Server { get; set; }
    
    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;
    
    [JsonPropertyName("account")]
    [JsonIgnore]
    public AnkamaAccount Infos { get; set; } = new();
    
    [JsonPropertyName("toLoad")]
    public bool ToLoad { get; set; } = false;
    
    [JsonPropertyName("isBank")]
    public bool IsBank { get; set; } = false;
    
    [JsonPropertyName("isKoli")]
    public bool IsKoli { get; set; } = false;


    [JsonPropertyName("proxy")]
    public string? Proxy { get; set; }
    
    [JsonPropertyName("trajet")]
    public string? Trajet { get; set; }
    
    [JsonPropertyName("autoPass")]
    public bool AutoPass { get; set; } = false;
}