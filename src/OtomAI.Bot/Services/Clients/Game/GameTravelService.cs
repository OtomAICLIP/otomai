using OtomAI.Bot.Client;
using OtomAI.Bot.Services.Clients.Contracts;
using Serilog;

namespace OtomAI.Bot.Services.Clients.Game;

/// <summary>
/// A* world pathfinding: GoToMap() navigates across the world graph.
/// Mirrors Bubble.D3.Bot's GameTravelService.
/// </summary>
public sealed class GameTravelService : GameClientServiceBase, IMapTravelService
{
    public GameTravelService(BotGameClient client) : base(client) { }

    public async Task<bool> GoToMapAsync(long targetMapId, CancellationToken ct = default)
    {
        if (CurrentMapId == targetMapId) return true;

        Log.Information("{Name} traveling to map {TargetMap} from {CurrentMap}",
            CharacterName, targetMapId, CurrentMapId);

        // TODO: Use WorldPathFinderService to compute inter-map path
        // Then follow each step: scroll transitions, interactive elements, etc.
        await Task.CompletedTask;

        return false; // Not yet implemented
    }

    public async Task<bool> GoToCellAsync(long mapId, int cellId, CancellationToken ct = default)
    {
        if (!await GoToMapAsync(mapId, ct)) return false;
        return await Client.Navigation.MoveToCellAsync(cellId, ct);
    }

    public async Task<bool> UseZaapAsync(long targetMapId, CancellationToken ct = default)
    {
        Log.Information("{Name} using zaap to map {TargetMap}", CharacterName, targetMapId);
        // TODO: Find nearest zaap, teleport
        await Task.CompletedTask;
        return false;
    }
}
