using OtomAI.Bot.Client;
using Serilog;

namespace OtomAI.Bot.Services.Clients.Game;

/// <summary>
/// Map creation, new map entry, auto-path setup.
/// Mirrors Bubble.D3.Bot's GameMapLifecycleService.
/// </summary>
public sealed class GameMapLifecycleService : GameClientServiceBase
{
    public GameMapLifecycleService(BotGameClient client) : base(client) { }

    public event Action<long>? OnMapChanged;

    public void HandleNewMap(long mapId, int cellId)
    {
        State.CurrentMapId = mapId;
        State.CurrentCellId = cellId;
        Context.IsMapLoaded = true;
        State.IsMoving = false;

        Log.Information("{Name} entered map {MapId} at cell {Cell}",
            CharacterName, mapId, cellId);

        OnMapChanged?.Invoke(mapId);
    }

    public void HandleMapComplement(long mapId)
    {
        // Additional map data received (entities, interactives, etc.)
        Log.Debug("Map complement received for {MapId}", mapId);
    }
}
