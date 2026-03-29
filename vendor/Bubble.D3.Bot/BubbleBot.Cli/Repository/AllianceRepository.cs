using System.Text.Json;
using System.Text.Json.Serialization;
using Bubble.Core.Services;

namespace BubbleBot.Cli.Repository;

public class AllianceRepository : Singleton<AllianceRepository>
{
    private List<AllianceRecord> _alliances = new();
    public void Initialize()
    {
        if(File.Exists("Data/alliances.json"))
        {
            var json = File.ReadAllText("Data/alliances.json");
            _alliances = JsonSerializer.Deserialize<List<AllianceRecord>>(json) ?? new();
        }
    }
    
    public AllianceRecord? GetRandomAlliance()
    {
        return _alliances.Count == 0 ? null : _alliances[Random.Shared.Next(0, _alliances.Count)];
    }
}

public class AllianceRecord
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
    [JsonPropertyName("symbol_color")]
    public string SymbolColor { get; set; }
    [JsonPropertyName("background_shape")]
    public string BackgroundShape { get; set; }
    [JsonPropertyName("background_color")]
    public string BackgroundColor { get; set; }
    [JsonPropertyName("symbol_shape")]
    public string SymbolShape { get; set; }
    [JsonPropertyName("motd")]
    public string Motd { get; set; }
}