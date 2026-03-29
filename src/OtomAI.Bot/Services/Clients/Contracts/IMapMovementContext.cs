namespace OtomAI.Bot.Services.Clients.Contracts;

/// <summary>
/// Context for map movement operations. Mirrors Bubble.D3.Bot's IMapMovementContext.
/// </summary>
public interface IMapMovementContext
{
    long CurrentMapId { get; }
    int CurrentCellId { get; }
    bool IsMoving { get; }
    Task MoveToCell(int cellId, CancellationToken ct = default);
    Task WaitForMovementComplete(CancellationToken ct = default);
}
