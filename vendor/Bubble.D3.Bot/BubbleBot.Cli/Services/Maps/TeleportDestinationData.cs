using BubbleBot.Cli.Repository.Maps;
using Serilog;

namespace BubbleBot.Cli.Services.Maps;

public enum TeleportDestinationOpenedReason
{
    None,
    GoToHuntHouse,
    GoToFirstStep,
    Empty,
    BuyStuff,
    CreateGuild,
    GoToTrajet
}

public class TeleportDestinationData
{
    private BotGameClient _client;
    public TeleportDestinationOpenedReason Reason { get; set; } = TeleportDestinationOpenedReason.None;

    public TeleportDestinationData(BotGameClient client)
    {
        _client = client;
    }

    private bool HasAllNeededZaap(TeleportDestinationsEvent ev)
    {
        var neededZaap = _client.MapsWithZaap;
        var neededCount = neededZaap.Count;
        var hasCount = ev.Destinations.Count(x => neededZaap.Contains(x.MapId));

        // on va mettre 1 de différence pour éviter les erreurs de calcul
        if (hasCount < neededCount - 1)
        {
            return false;
        }

        return true;
    }


    public void OnTeleportDestination(TeleportDestinationsEvent ev)
    {
        switch (Reason)
        {
            case TeleportDestinationOpenedReason.GoToHuntHouse:
                if (!HasAllNeededZaap(ev))
                {
                    _client.LogInfo(
                        "On n'a pas tous les zaaps sont disponibles, on va d'abord à astrub pour tout prendre");
                    _client.ZaapMode = true;
                    _ = _client.SendRequestWithDelay(new TeleportRequest
                                                     {
                                                         SourceType = Teleporter.TeleporterHavenBag,
                                                         DestinationType = Teleporter.TeleporterZaap,
                                                         MapId = 191105026
                                                     },
                                                     TeleportRequest.TypeUrl,
                                                     500);
                    return;
                }

                _ = _client.SendRequestWithDelay(new TeleportRequest
                                                 {
                                                     SourceType = Teleporter.TeleporterHavenBag,
                                                     DestinationType = Teleporter.TeleporterZaap,
                                                     MapId = 142087694
                                                 },
                                                 TeleportRequest.TypeUrl,
                                                 500);
                break;
            case TeleportDestinationOpenedReason.Empty:
            case TeleportDestinationOpenedReason.BuyStuff:
                _ = _client.SendRequestWithDelay(new TeleportRequest
                                                 {
                                                     SourceType = Teleporter.TeleporterHavenBag,
                                                     DestinationType = Teleporter.TeleporterZaap,
                                                     MapId = 212600323
                                                 },
                                                 TeleportRequest.TypeUrl,
                                                 500);
                break;
            case TeleportDestinationOpenedReason.CreateGuild:
                _ = _client.SendRequestWithDelay(new TeleportRequest
                                                 {
                                                     SourceType = Teleporter.TeleporterHavenBag,
                                                     DestinationType = Teleporter.TeleporterZaap,
                                                     MapId = 68552706
                                                 },
                                                 TeleportRequest.TypeUrl,
                                                 500);
                break;

            case TeleportDestinationOpenedReason.GoToFirstStep:
            {
                if (_client.TreasureHuntData.NextClue == null)
                {
                    _client.LogInfo(
                        "Le prochain indice est null alors que l'on veut se téléporter à la première étape");
                    return;
                }

                var mapScores = new Dictionary<long, int>();
                foreach (var destination in ev.Destinations)
                {
                    if (destination.MapId is 28050436 or 20973313) // on ignore les villages otomai
                    {
                        continue;
                    }

                    var map = MapRepository.Instance.GetMap(destination.MapId);
                    var x = map?.PosX ?? 0;
                    var y = map?.PosY ?? 0;

                    // get the distance between the current map and the destination
                    var distance =
                        (int)Math.Sqrt(Math.Pow(_client.TreasureHuntData.NextClue.FromX - x, 2) +
                                       Math.Pow(_client.TreasureHuntData.NextClue.FromY - y, 2));

                    mapScores[destination.MapId] = distance;
                }

                var bestMap = mapScores.OrderBy(x => x.Value).First().Key;

                _client.TreasureHuntData.MapHistory.Clear();


                _ = _client.SendRequestWithDelay(new TeleportRequest
                                                 {
                                                     SourceType = Teleporter.TeleporterHavenBag,
                                                     DestinationType = Teleporter.TeleporterZaap,
                                                     MapId = bestMap
                                                 },
                                                 TeleportRequest.TypeUrl,
                                                 500);
                break;
            }
            case TeleportDestinationOpenedReason.GoToTrajet:
            {
                if(_client.Trajet == null)
                {
                    Log.Error("On a pas de zaap le plus proche, on ne peut pas se téléporter");
                    _client.HavenBag.LeaveHavenBag();
                    return;
                }
                
                var mapScores = new Dictionary<long, int>();
                foreach (var destination in ev.Destinations)
                {
                    if (destination.MapId is 28050436 or 20973313) // on ignore les villages otomai
                    {
                        continue;
                    }

                    var map = MapRepository.Instance.GetMap(destination.MapId);
                    var x = map?.PosX ?? 0;
                    var y = map?.PosY ?? 0;

                    // get the distance between the current map and the destination
                    var distance =
                        (int)Math.Sqrt(Math.Pow(_client.Trajet.ClosestZaap.X - x, 2) +
                                       Math.Pow(_client.Trajet.ClosestZaap.Y - y, 2));

                    mapScores[destination.MapId] = distance;
                }

                var bestMap = mapScores.OrderBy(x => x.Value).First().Key;

                _ = _client.SendRequestWithDelay(new TeleportRequest
                                                 {
                                                     SourceType = Teleporter.TeleporterHavenBag,
                                                     DestinationType = Teleporter.TeleporterZaap,
                                                     MapId = bestMap
                                                 },
                                                 TeleportRequest.TypeUrl,
                                                 500);
                break;
            }
        }

        Reason = TeleportDestinationOpenedReason.None;
    }

    public void OnDataReceived(TeleportDestinationsEvent teleportDestinationsEvent)
    {
        OnTeleportDestination(teleportDestinationsEvent);
    }
}