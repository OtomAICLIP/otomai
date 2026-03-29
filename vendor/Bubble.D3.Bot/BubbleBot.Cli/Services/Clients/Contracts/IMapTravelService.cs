using BubbleBot.Cli.Repository.Maps;

namespace BubbleBot.Cli.Services.Clients.Contracts;

public interface IMapTravelService
{
    void GoToMap(Map currentMap, int x, int y);
    void GoToMapSafe(Map currentMap, long mapId);
    void GoToMap(Map currentMap, long mapId);
}
