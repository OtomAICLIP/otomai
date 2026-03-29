using System.Text.Json;
using Serilog;

namespace OtomAI.Bot.TreasureHunts;

/// <summary>
/// Treasure hunt clue resolver. Mirrors Bubble.D3.Bot's CluesSolver:
/// Combines DofusDB remote cache with local DPLN (dofuspourlesnoobs) data.
/// </summary>
public sealed class CluesSolver
{
    private Dictionary<int, ClueData> _clues = [];
    private Dictionary<int, ClueData> _localClues = [];

    public void LoadClues(string dataPath)
    {
        LoadClueFile(Path.Combine(dataPath, "clues.json"), ref _clues);
        LoadClueFile(Path.Combine(dataPath, "dofuspourlesnoobs_clues.json"), ref _localClues);
    }

    private static void LoadClueFile(string path, ref Dictionary<int, ClueData> target)
    {
        if (!File.Exists(path)) return;
        var json = File.ReadAllText(path);
        var clues = JsonSerializer.Deserialize<List<ClueData>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        target = clues.ToDictionary(c => c.Id);
        Log.Debug("Loaded {Count} clues from {Path}", clues.Count, path);
    }

    public ClueData? Resolve(int clueId)
    {
        return _clues.GetValueOrDefault(clueId) ?? _localClues.GetValueOrDefault(clueId);
    }

    public long? FindMapForClue(int clueId, long currentMapId, int direction)
    {
        var clue = Resolve(clueId);
        if (clue is null) return null;

        // TODO: Find map in the given direction that has this clue's POI
        return clue.MapId;
    }
}

public sealed class ClueData
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public long MapId { get; set; }
    public int PoiId { get; set; }
}
