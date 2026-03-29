namespace OtomAI.Bot.Services.Clients.Contracts;

/// <summary>
/// World-level travel service interface. Mirrors Bubble.D3.Bot's IMapTravelService.
/// </summary>
public interface IMapTravelService
{
    Task<bool> GoToMapAsync(long targetMapId, CancellationToken ct = default);
    Task<bool> GoToCellAsync(long mapId, int cellId, CancellationToken ct = default);
}
