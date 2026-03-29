using BubbleBot.Cli.Services.Maps;

namespace BubbleBot.Cli.Services.Clients.Game;

internal sealed class GameSessionService : GameClientServiceBase
{
    public GameSessionService(BotGameClientContext    context,
                              ClientTransportService  transportService,
                              GameNotificationService notificationService)
        : base(context, transportService, notificationService)
    {
    }

    public void OnConnected(GameWorkflowService workflowService)
    {
        LogInfo("Game Connected to {BotId}", BotId);
        _ = workflowService.MainLoop();

        SendRequest(new IdentificationRequest
                    {
                        TicketKey = _token,
                        LanguageCode = "fr"
                    },
                    IdentificationRequest.TypeUrl);

        Task.Run(async () =>
        {
            while (true)
            {
                if (!Client.IsConnected)
                {
                    LogInfo("On arrête la boucle de connexion");
                    return;
                }

                LogInfo("Envoi d'un ping");
                SendRequest(new PingRequest { Quiet = true }, PingRequest.TypeUrl, true);
                await Task.Delay(30000);

                SendRequest(new DateRequest(), DateRequest.TypeUrl);
                await Task.Delay(30000);
            }
        });

        Task.Run(async () =>
        {
            if (!IsBank)
            {
                return;
            }

            while (true)
            {
                if (!Client.IsConnected)
                {
                    LogInfo("On arrête la boucle de connexion de la banque");
                    return;
                }

                await Task.Delay(TimeSpan.FromMinutes(20));
                if (!IsBank || AutoPath.Count > 0)
                {
                    continue;
                }

                LogInfo("On est une banque ! Donc on rentre dans l'havre sac pour l'anti afk");
                HavenBag.EnterHavenBag(HavenBagEnterReason.LeaveInstant);
            }
        });
    }

    public void OnDisconnected()
    {
        LogInfo("Game Disconnected from {BotId}", BotId);
        Connected = false;

        LogDiscord("� Déconnecté du serveur de jeu", true);
        if (IsDisconnectionPlanned)
        {
            LogDiscord("� Déconnexion planifiée", true);
            return;
        }

        if (IsStopped)
        {
            return;
        }

        LogDiscord("� Déconnexion non planifiée, tentative de reconnexion", true);
        ReconnectFromScratch();

        Party?.Members.Remove(PlayerId);
        Party = null;
    }

    public void PlanifyDisconnect()
    {
        LogDiscord("� On planifie une déconnexion", true);
        IsDisconnectionPlanned = true;
        Client.Disconnect();
        NeedEmptyToBank = false;
    }

    public bool ExchangeToBankCharacter(int retry = 0)
    {
        while (true)
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                    "BubbleBot",
                                    $"{ServerId}.txt");
            if (!File.Exists(path))
            {
                LogError("File {Path} not found", path);
                return false;
            }

            var characterId = long.Parse(File.ReadAllText(path));
            if (characterId == PlayerId || Map == null)
            {
                return false;
            }

            LogInfo("On va échanger avec le personnage {CharacterId}", characterId);
            var actor = Map.GetActor(characterId);
            if (actor != null)
            {
                LogInfo("On va échanger avec {CharacterName}",
                        actor.ActorInformationValue.RolePlayActorValue.NamedActorValue.Name);

                _ = SendRequestWithDelay(new ExchangePlayerRequest
                                         {
                                             TargetId = characterId
                                         },
                                         ExchangePlayerRequest.TypeUrl,
                                         Random.Shared.Next(1000, 15000));
                return true;
            }

            retry++;
            if (retry >= 3)
            {
                return false;
            }

            LogInfo("On va réessayer dans 1 seconde, au cas où si il est dans son havre sac");
            Thread.Sleep(1000);
        }
    }

    public void ReconnectFromScratch()
    {
        LogDiscord("Le bot va se reconnecter car il est afk", true);
        Client.Disconnect();
    }
}
