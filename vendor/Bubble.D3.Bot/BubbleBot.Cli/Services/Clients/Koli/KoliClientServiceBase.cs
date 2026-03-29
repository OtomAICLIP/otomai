using Bubble.Core.Datacenter.Datacenter.WorldGraph;
using Bubble.Shared.Protocol;
using BubbleBot.Cli.Models;
using BubbleBot.Cli.Repository.Maps;
using BubbleBot.Cli.Services.Fight;
using BubbleBot.Cli.Services.Maps;
using BubbleBot.Cli.Services.Parties;
using BubbleBot.Cli.Services.TreasureHunts;

namespace BubbleBot.Cli.Services.Clients.Koli;

internal abstract class KoliClientServiceBase
{
    protected KoliClientServiceBase(BotKoliClient          owner,
                                    BotKoliClientContext   context,
                                    ClientTransportService transportService,
                                    KoliNotificationService notificationService)
    {
        Owner = owner;
        Context = context;
        TransportService = transportService;
        NotificationService = notificationService;
    }

    protected BotKoliClient Owner { get; }
    protected BotKoliClientContext Context { get; }
    protected ClientTransportService TransportService { get; }
    protected KoliNotificationService NotificationService { get; }
    protected KoliRuntimeState State => Context.State;

    protected BotClient BotController => Context.BotController;
    protected string Token => Context.Token;
    protected BotSettings Settings => Context.Settings;

    protected long CharacterId
    {
        get => Context.CharacterId;
        set => Context.CharacterId = value;
    }

    protected int SequenceNumber
    {
        get => Context.SequenceNumber;
        set => Context.SequenceNumber = value;
    }

    protected int LastRequestUid => Context.LastRequestUid;

    protected string BotId => Context.BotId;
    protected string Hwid => Context.Hwid;
    protected int ServerId => Context.ServerId;
    protected string ServerName => Context.ServerName;

    protected SaharachAccount Account
    {
        get => Context.Account;
        set => Context.Account = value;
    }

    protected BotGameClient? GameClient
    {
        get => State.GameClient;
        set => State.GameClient = value;
    }

    protected Map? Map
    {
        get => State.Map;
        set => State.Map = value;
    }

    protected WorldPath WorldPath => State.WorldPath;

    protected List<WorldGraphEdge> AutoPath
    {
        get => State.AutoPath;
        set => State.AutoPath = value;
    }

    protected int AutoPathIndex
    {
        get => State.AutoPathIndex;
        set => State.AutoPathIndex = value;
    }

    protected bool IsInFight
    {
        get => State.IsInFight;
        set => State.IsInFight = value;
    }

    protected long AutoPathEndMapId
    {
        get => State.AutoPathEndMapId;
        set => State.AutoPathEndMapId = value;
    }

    protected long PlayerId => Context.CharacterId;

    protected FightActor? Fighter
    {
        get => State.Fighter;
        set => State.Fighter = value;
    }

    protected List<SpellWrapper> Spells
    {
        get => State.Spells;
        set => State.Spells = value;
    }

    protected CharacterInfo Info
    {
        get => State.Info;
        set => State.Info = value;
    }

    protected MapComplementaryInformationEvent? MapCurrentEvent
    {
        get => State.MapCurrentEvent;
        set => State.MapCurrentEvent = value;
    }

    protected bool NeedNextContext
    {
        get => State.NeedNextContext;
        set => State.NeedNextContext = value;
    }

    protected bool Connected
    {
        get => State.Connected;
        set => State.Connected = value;
    }

    protected long ConnectedAt
    {
        get => State.ConnectedAt;
        set => State.ConnectedAt = value;
    }

    protected bool IsSpecial
    {
        get => State.IsSpecial;
        set => State.IsSpecial = value;
    }

    protected DateTime LastReconnect
    {
        get => State.LastReconnect;
        set => State.LastReconnect = value;
    }

    protected string ServerPrefix
    {
        get => State.ServerPrefix;
        set => State.ServerPrefix = value;
    }

    protected FightInfo? FightInfo
    {
        get => State.FightInfo;
        set => State.FightInfo = value;
    }

    protected bool AutoPass
    {
        get => State.AutoPass;
        set => State.AutoPass = value;
    }

    protected bool IsDisconnectionPlanned
    {
        get => State.IsDisconnectionPlanned;
        set => State.IsDisconnectionPlanned = value;
    }

    protected bool IsKoli
    {
        get => State.IsKoli;
        set => State.IsKoli = value;
    }

    protected PartyInfo? Party => GameClient?.Party;
    protected TrajetSettings? Trajet => GameClient?.Trajet;
    protected TreasureHuntData? TreasureHuntData => GameClient?.TreasureHuntData;

    protected Task SendRequestWithDelay(IProtoMessage message,
                                        string        typeUrl,
                                        int           delay,
                                        Predicate<IProtoMessage>? predicate = null)
    {
        return TransportService.SendRequestWithDelay(message, typeUrl, delay, predicate);
    }

    protected void SendRequest(IProtoMessage message, string typeUrl, bool setUid = false)
    {
        TransportService.SendRequest(message, typeUrl, setUid);
    }

    protected void SendRequest(IProtoMessage message, string typeUrl, int uid)
    {
        TransportService.SendRequest(message, typeUrl, uid);
    }

    protected void LogWarning(string messageTemplate)
    {
        NotificationService.LogWarning(messageTemplate);
    }

    protected void LogWarning<T>(string messageTemplate, T propertyValue)
    {
        NotificationService.LogWarning(messageTemplate, propertyValue);
    }

    protected void LogWarning<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1)
    {
        NotificationService.LogWarning(messageTemplate, propertyValue0, propertyValue1);
    }

    protected void LogWarning<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
    {
        NotificationService.LogWarning(messageTemplate, propertyValue0, propertyValue1, propertyValue2);
    }

    protected void LogInfo(string messageTemplate)
    {
        NotificationService.LogInfo(messageTemplate);
    }

    protected void LogInfo<T>(string messageTemplate, T propertyValue)
    {
        NotificationService.LogInfo(messageTemplate, propertyValue);
    }

    protected void LogInfo<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1)
    {
        NotificationService.LogInfo(messageTemplate, propertyValue0, propertyValue1);
    }

    protected void LogInfo<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
    {
        NotificationService.LogInfo(messageTemplate, propertyValue0, propertyValue1, propertyValue2);
    }

    protected void LogError(Exception? exception, string messageTemplate)
    {
        NotificationService.LogError(exception, messageTemplate);
    }

    protected void LogError(string messageTemplate)
    {
        NotificationService.LogError(messageTemplate);
    }

    protected void LogError<T>(string messageTemplate, T propertyValue)
    {
        NotificationService.LogError(messageTemplate, propertyValue);
    }

    protected void LogError<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1)
    {
        NotificationService.LogError(messageTemplate, propertyValue0, propertyValue1);
    }

    protected void LogError<T0>(Exception? exception, string messageTemplate, T0 propertyValue0)
    {
        NotificationService.LogError(exception, messageTemplate, propertyValue0);
    }

    protected void LogError<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
    {
        NotificationService.LogError(messageTemplate, propertyValue0, propertyValue1, propertyValue2);
    }
}
