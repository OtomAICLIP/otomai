using System.Text;
using BubbleBot.Cli.Repository.Maps;

namespace BubbleBot.Cli.Services.Clients.Game;

internal sealed class GameMonsterDiscoveryService : GameClientServiceBase
{
    public GameMonsterDiscoveryService(BotGameClientContext    context,
                                       ClientTransportService  transportService,
                                       GameNotificationService notificationService)
        : base(context, transportService, notificationService)
    {
    }

    public void ScanMapActors(Map map, IEnumerable<ActorPositionInformation> actors)
    {
        try
        {
            foreach (var actor in actors)
            {
                var identification = actor.ActorInformationValue?
                                          .RolePlayActorValue?
                                          .MonsterGroupActorValue?
                                          .Identification;

                if (identification?.Underlings == null)
                {
                    continue;
                }

                ParseMonsters(map, identification.Underlings, identification.MainCreature);
            }
        }
        catch (Exception exception)
        {
            LogError(exception, "Error while parsing monsters");
        }
    }

    private void ParseMonsters(Map                         map,
                               List<MonsterInGroupInformation> mobs,
                               MonsterInGroupInformation       main)
    {
        var summary = new StringBuilder();
        var mainMobData = MapRepository.Instance.GetMonster((ushort)main.Gid);

        if (mainMobData != null && (mainMobData.Race == 78 || mainMobData.Race == 32))
        {
            summary.Append($"**{mainMobData.Name} (Lv. {main.Level})**");
        }

        foreach (var monster in mobs)
        {
            var monsterData = MapRepository.Instance.GetMonster((ushort)monster.Gid);
            if (monsterData == null || (monsterData.Race != 78 && monsterData.Race != 32))
            {
                continue;
            }

            if (summary.Length > 0)
            {
                summary.Append(',');
            }

            var type = monsterData.Race == 78 ? "Archimonstre" : "Recherché";
            if (!MonsterDiscoveryRegistry.TryMarkLogged(map.Id, monsterData.Id))
            {
                return;
            }

            summary.Append($"**{monsterData.Name} (Lv. {monster.Level}) *{type}* **");
        }

        if (summary.Length == 0)
        {
            return;
        }

        LogArchiDiscord(
            $"\n Trouvé {summary} en :map: **[{map.Data.PosX}, {map.Data.PosY}]**. \n __Commande Travel :__ `/travel {map.Data.PosX} {map.Data.PosY}` \n __Zaap le plus proche :__ `{map.GetClosestZaap()}`");
    }
}
