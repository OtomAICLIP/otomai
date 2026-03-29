using Bubble.Shared.Protocol;

namespace BubbleBot.Cli.Services.Clients.Game;

internal sealed class GameArenaKoliHandler : GameClientServiceBase, IGameMessageHandler
{
    public GameArenaKoliHandler(BotGameClientContext    context,
                                ClientTransportService  transportService,
                                GameNotificationService notificationService)
        : base(context, transportService, notificationService)
    {
    }

    public bool TryHandle(IProtoMessage message)
    {
        switch (message)
        {
            case ArenaFightPropositionEvent _:
                LogInfo("Proposition de combat en arène");
                SendRequest(new ArenaFightAnswerRequest
                            {
                                Accept = true
                            },
                            ArenaFightAnswerRequest.TypeUrl,
                            true);
                return true;
            case ArenaUpdatePlayerInformationEvent _:
                return true;
            case ArenaRegistrationStatusEvent arenaRegistrationStatusEvent:
                ArenaStatus = arenaRegistrationStatusEvent.Registered;
                CooldownTime = 0;
                LastArenaStatusChange = DateTime.UtcNow;
                AgainstBot = string.Empty;
                WithBot = string.Empty;
                return true;
            case ArenaRegistrationWarningEvent _:
                LogInfo("Avertissement d'inscription en arène");
                return true;
            case ArenaFightAnswerResponse _:
                LogInfo("Réponse de combat en arène");
                return true;
            case ArenaSwitchToFightServerEvent switchToFightServerEvent:
                HandleArenaSwitch(switchToFightServerEvent);
                return true;
            default:
                return false;
        }
    }

    private void HandleArenaSwitch(ArenaSwitchToFightServerEvent switchToFightServerEvent)
    {
        LogInfo("Switch to fight server");
        KoliFightDones++;
        LastFlagRequest = DateTime.UtcNow;

        if (Map == null)
        {
            SendRequest(new ContextCreationRequest(), ContextCreationRequest.TypeUrl);
        }

        var gameIp = switchToFightServerEvent.Address;
        var gamePort = switchToFightServerEvent.Ports.All(x => x == 443) ? 443 : switchToFightServerEvent.Ports.First();

        if (Client.Proxy != null)
        {
            Client.Proxy.DestinationHost = BotManager.GetIpFromHost(gameIp);
            Client.Proxy.DestinationPort = gamePort;
        }

        KoliClient = new BotKoliClient(_client,
                                       BotId,
                                       Account,
                                       switchToFightServerEvent.Token,
                                       ServerName,
                                       ServerId,
                                       Hwid,
                                       BotManager.GetIpFromHost(gameIp),
                                       gamePort,
                                       Client.Proxy,
                                       _settings);

        KoliClient.FightInfo = FightInfo;
        KoliClient.GameClient = Client;
        KoliClient.ConnectAsync();

        ArenaStatus = false;
        IsInFight = true;
    }
}
