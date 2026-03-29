using System.Text.Json.Serialization;

namespace BubbleBot.Cli.Models;

public class TrajetSettings
{
    [JsonPropertyName("auto_fight")]
    public bool AutoFight { get; set; }
    
    [JsonPropertyName("min_monsters")]
    public int MinMonsters { get; set; }
    
    [JsonPropertyName("max_monsters")]
    public int MaxMonsters { get; set; }

    [JsonPropertyName("min_groups_players")]
    public int MinGroupsPlayers { get; set; }
    
    [JsonPropertyName("max_groups_players")]
    public int MaxGroupsPlayers { get; set; }
    
    [JsonPropertyName("items_to_keep")]
    public int[] ItemsToKeep { get; set; } = [];
    
    [JsonPropertyName("closest_zaap")]
    public MapWorldPosition ClosestZaap { get; set; } = new();
    
    [JsonPropertyName("maps")]
    public MapWorldPosition[] Maps { get; set; } = [];
}

public class MapWorldPosition
{
    [JsonPropertyName("x")]
    public int X { get; set; }
    [JsonPropertyName("y")]
    public int Y { get; set; }
}