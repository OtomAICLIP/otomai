using System.Text.Json;
using OtomAI.Datacenter.Models;
using Serilog;

namespace OtomAI.Bot.Repository.Maps;

/// <summary>
/// Map data repository. Mirrors Bubble.D3.Bot's MapRepository (singleton).
/// Loads maps from JSON data files.
/// </summary>
public sealed class MapRepository
{
    private static readonly Lazy<MapRepository> _instance = new(() => new MapRepository());
    public static MapRepository Instance => _instance.Value;

    private readonly Dictionary<long, MapData> _maps = [];
    private readonly Dictionary<long, MapRecord> _records = [];

    private MapRepository() { }

    public void Load(string dataPath)
    {
        var mapsFile = Path.Combine(dataPath, "maps.json");
        if (!File.Exists(mapsFile))
        {
            Log.Warning("Maps data file not found: {Path}", mapsFile);
            return;
        }

        var json = File.ReadAllText(mapsFile);
        var records = JsonSerializer.Deserialize<List<MapRecord>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        foreach (var record in records)
            _records[record.MapId] = record;

        Log.Information("Loaded {Count} map records", _records.Count);
    }

    public MapData? GetMap(long mapId) => _maps.GetValueOrDefault(mapId);
    public MapRecord? GetRecord(long mapId) => _records.GetValueOrDefault(mapId);

    public void CacheMap(MapData map) => _maps[map.Id] = map;

    public IEnumerable<MapRecord> GetAllRecords() => _records.Values;
}

/// <summary>
/// Map metadata record (from maps.json). Mirrors Bubble.D3.Bot's MapRecord.
/// </summary>
public sealed class MapRecord
{
    public long MapId { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public int SubAreaId { get; set; }
    public int WorldMapId { get; set; }
    public bool HasPriorityOnWorldMap { get; set; }
}
