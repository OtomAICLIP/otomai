using System.Text.Json;
using Bubble.Core.Services;

namespace BubbleBot.Cli.Services;

public class ProtoService : Singleton<ProtoService>
{
    private Dictionary<string, string> _mappings = new Dictionary<string, string>();
    
    public void Initialize()
    {
        const string fileName = "Data/game_mappings.json";

        if (File.Exists(fileName))
        {
            var json = File.ReadAllText(fileName);
            _mappings = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ??
                        new Dictionary<string, string>();
        }
        else
        {
            throw new FileNotFoundException($"File {fileName} not found.");
        }
    }
    
    public string? GetMapping(string key)
    {
        return _mappings.TryGetValue(key, out var mapping) ? mapping : null;
    }
}