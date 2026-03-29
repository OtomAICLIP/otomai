using Serilog;

namespace OtomAI.Datacenter;

/// <summary>
/// Loads typed game data from Unity asset bundles.
/// Mirrors Bubble.Core.Datacenter's DatacenterService.
/// </summary>
public sealed class DatacenterService
{
    private readonly Dictionary<Type, object> _repositories = [];

    public void LoadFromAssetBundles(string assetPath)
    {
        Log.Information("Loading datacenter from {Path}...", assetPath);
        // TODO: Use Unity asset reader to extract datacenter data
        // Load Breeds, Items, Spells, Monsters, Jobs, WorldGraph, etc.
    }

    public void LoadFromJson(string jsonPath)
    {
        Log.Information("Loading datacenter from JSON at {Path}...", jsonPath);
        // TODO: Load from pre-extracted JSON files
    }

    public IReadOnlyList<T> GetAll<T>() where T : class
    {
        if (_repositories.TryGetValue(typeof(T), out var repo))
            return (IReadOnlyList<T>)repo;
        return [];
    }

    public T? GetById<T>(int id) where T : class
    {
        // TODO: Index-based lookup
        return null;
    }

    internal void Register<T>(List<T> items) where T : class
    {
        _repositories[typeof(T)] = items.AsReadOnly();
        Log.Debug("Registered {Count} {Type} datacenter objects", items.Count, typeof(T).Name);
    }
}
