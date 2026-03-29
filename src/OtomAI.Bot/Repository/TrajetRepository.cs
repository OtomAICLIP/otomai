using System.Text.Json;
using OtomAI.Bot.Models;
using Serilog;

namespace OtomAI.Bot.Repository;

/// <summary>
/// Trajet (route) data repository. Mirrors Bubble.D3.Bot's TrajetRepository.
/// </summary>
public sealed class TrajetRepository
{
    private static readonly Lazy<TrajetRepository> _instance = new(() => new TrajetRepository());
    public static TrajetRepository Instance => _instance.Value;

    private readonly Dictionary<string, TrajetSettings> _trajets = [];
    private TrajetRepository() { }

    public void Load(string dataPath)
    {
        var dir = Path.Combine(dataPath, "trajets");
        if (!Directory.Exists(dir))
        {
            Log.Debug("No trajets directory found at {Path}", dir);
            return;
        }

        foreach (var file in Directory.GetFiles(dir, "*.json"))
        {
            var json = File.ReadAllText(file);
            var trajet = JsonSerializer.Deserialize<TrajetSettings>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (trajet is not null)
                _trajets[trajet.Name] = trajet;
        }

        Log.Information("Loaded {Count} trajets", _trajets.Count);
    }

    public TrajetSettings? Get(string name) => _trajets.GetValueOrDefault(name);
    public IEnumerable<TrajetSettings> GetAll() => _trajets.Values;
}
