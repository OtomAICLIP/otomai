using System.Text.Json;
using Serilog;

namespace OtomAI.Protocol.Services;

/// <summary>
/// Maps proto short type names to full type names.
/// Mirrors Bubble.D3.Bot's ProtoService which loads game_mappings.json.
/// </summary>
public sealed class ProtoService
{
    private readonly Dictionary<string, string> _shortToFull = new();
    private readonly Dictionary<string, string> _fullToShort = new();

    public void LoadMappings(string path)
    {
        if (!File.Exists(path))
        {
            Log.Warning("Proto mappings file not found: {Path}", path);
            return;
        }

        var json = File.ReadAllText(path);
        var mappings = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];

        foreach (var (shortName, fullName) in mappings)
        {
            _shortToFull[shortName] = fullName;
            _fullToShort[fullName] = shortName;
        }

        Log.Information("Loaded {Count} proto type mappings", _shortToFull.Count);
    }

    public string? GetFullName(string shortCode) =>
        _shortToFull.GetValueOrDefault(shortCode);

    public string? GetShortCode(string fullName) =>
        _fullToShort.GetValueOrDefault(fullName);

    public bool HasMapping(string shortCode) =>
        _shortToFull.ContainsKey(shortCode);
}
