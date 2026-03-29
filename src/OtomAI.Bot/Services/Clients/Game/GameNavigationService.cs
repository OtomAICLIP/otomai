using OtomAI.Bot.Client;
using Serilog;

namespace OtomAI.Bot.Services.Clients.Game;

/// <summary>
/// Auto-path following, transition movement between maps.
/// Mirrors Bubble.D3.Bot's GameNavigationService.
/// </summary>
public sealed class GameNavigationService : GameClientServiceBase
{
    public GameNavigationService(BotGameClient client) : base(client) { }

    public async Task<bool> MoveToCellAsync(int targetCellId, CancellationToken ct = default)
    {
        if (CurrentCellId == targetCellId) return true;

        Log.Debug("{Name} moving from cell {From} to {To}", CharacterName, CurrentCellId, targetCellId);
        State.IsMoving = true;

        // TODO: Compute path using Pathfinder, send movement request
        // Wait for movement confirmation from server
        await Task.CompletedTask;

        return true;
    }

    public async Task<bool> MoveToMapEdgeAsync(int direction, CancellationToken ct = default)
    {
        Log.Debug("{Name} moving to map edge direction {Dir}", CharacterName, direction);
        // TODO: Find edge cell for direction, move there, handle map transition
        await Task.CompletedTask;
        return true;
    }

    public void HandleMovementConfirmed(int cellId)
    {
        State.CurrentCellId = cellId;
        State.IsMoving = false;
    }

    public void HandleMovementRejected()
    {
        State.IsMoving = false;
        Log.Warning("{Name} movement rejected", CharacterName);
    }
}
