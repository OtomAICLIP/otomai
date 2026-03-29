using System.Collections.Concurrent;
using System.Numerics;
using Bubble.Core.Datacenter.Datacenter.WorldGraph;
using Bubble.Shared.Protocol;
using BubbleBot.Cli.Models;
using BubbleBot.Cli.Repository.Maps;
using BubbleBot.Cli.Services.Fight;
using BubbleBot.Cli.Services.Maps;
using BubbleBot.Cli.Services.Parties;
using BubbleBot.Cli.Services.TreasureHunts;
using BubbleBot.Cli.Services.Clients.Contracts;
using Direction = BubbleBot.Cli.Repository.Maps.Direction;

namespace BubbleBot.Cli.Services.Clients.Game;

internal abstract class GameClientServiceBase
{
    protected GameClientServiceBase(BotGameClientContext   context,
                                    ClientTransportService transportService,
                                    GameNotificationService notificationService)
    {
        _context = context;
        _transportService = transportService;
        _notificationService = notificationService;
    }

    protected BotGameClientContext _context { get; }
    protected ClientTransportService _transportService { get; }
    protected GameNotificationService _notificationService { get; }

    protected BotGameClient Client => _context.Client;
    protected BotClient _client => _context.BotController;
    protected string _token => _context.Token;
    protected BotSettings _settings => _context.Settings;
    protected long _characterId
    {
        get => _context.CharacterId;
        set => _context.CharacterId = value;
    }

    protected CancellationTokenSource _moveToCellReqCts
    {
        get => _context.MoveToCellRequestCts;
        set => _context.MoveToCellRequestCts = value;
    }

    protected CancellationTokenSource _connectionTimeoutCts
    {
        get => _context.ConnectionTimeoutCts;
        set => _context.ConnectionTimeoutCts = value;
    }

    protected int _lastReqUid
    {
        get => _context.LastRequestUid;
        set => _context.LastRequestUid = value;
    }

    protected int _sequenceNumber
    {
        get => _context.SequenceNumber;
        set => _context.SequenceNumber = value;
    }

    protected BigInteger Cvlf => _context.Cvlf;
    protected BigInteger Cvld => _context.Cvld;

    protected ConcurrentQueue<Func<Task>> _requests => _context.Requests;

    protected bool Debug => BotGameClient.Debug;
    protected bool DebugAutoFight => BotGameClient.DebugAutoFight;
    protected string BotId => Client.BotId;
    protected string Hwid => Client.Hwid;
    protected int ServerId => Client.ServerId;
    protected string ServerName => Client.ServerName;
    protected long LastMessageSent
    {
        get => Client.LastMessageSent;
        set => Client.SetLastMessageSent(value);
    }

    protected SaharachAccount Account
    {
        get => Client.Account;
        set => Client.Account = value;
    }

    protected bool ZaapMode
    {
        get => Client.ZaapMode;
        set => Client.ZaapMode = value;
    }

    protected TreasureHuntData TreasureHuntData => Client.TreasureHuntData;
    protected Map? Map
    {
        get => Client.Map;
        set => Client.Map = value;
    }

    protected bool IsInFight
    {
        get => Client.IsInFight;
        set => Client.IsInFight = value;
    }

    protected long AutoPathEndMapId
    {
        get => Client.AutoPathEndMapId;
        set => Client.AutoPathEndMapId = value;
    }

    protected long PlayerId => Client.PlayerId;
    protected FightActor? Fighter
    {
        get => Client.Fighter;
        set => Client.Fighter = value;
    }

    protected List<SpellWrapper> Spells => Client.Spells;

    protected int LastRosesAmount
    {
        get => Client.LastRosesAmount;
        set => Client.LastRosesAmount = value;
    }

    protected bool IsAtDailyLimit
    {
        get => Client.IsAtDailyLimit;
        set => Client.IsAtDailyLimit = value;
    }

    protected CharacterInfo Info
    {
        get => Client.Info;
        set => Client.Info = value;
    }

    protected WorldPath WorldPath
    {
        get => Client.WorldPath;
        set => Client.WorldPath = value;
    }

    protected List<WorldGraphEdge> AutoPath
    {
        get => Client.AutoPath;
        set => Client.AutoPath = value;
    }

    protected int AutoPathIndex
    {
        get => Client.AutoPathIndex;
        set => Client.AutoPathIndex = value;
    }

    protected HavenBag HavenBag
    {
        get => Client.HavenBag;
        set => Client.HavenBag = value;
    }

    protected MapComplementaryInformationEvent? MapCurrentEvent
    {
        get => Client.MapCurrentEvent;
        set => Client.MapCurrentEvent = value;
    }

    protected bool NeedToTakeHavenBagAsSoonAsPossible
    {
        get => Client.NeedToTakeHavenBagAsSoonAsPossible;
        set => Client.NeedToTakeHavenBagAsSoonAsPossible = value;
    }

    protected TeleportDestinationData TeleportDestinationData
    {
        get => Client.TeleportDestinationData;
        set => Client.TeleportDestinationData = value;
    }

    protected bool NeedNextContext
    {
        get => Client.NeedNextContext;
        set => Client.NeedNextContext = value;
    }

    protected Inventory Inventory
    {
        get => Client.Inventory;
        set => Client.Inventory = value;
    }

    protected bool Connected
    {
        get => Client.Connected;
        set => Client.Connected = value;
    }

    protected long ConnectedAt
    {
        get => Client.ConnectedAt;
        set => Client.ConnectedAt = value;
    }

    protected bool ShouldAutoOpen
    {
        get => Client.ShouldAutoOpen;
        set => Client.ShouldAutoOpen = value;
    }

    protected bool NeedEmptyToBank
    {
        get => Client.NeedEmptyToBank;
        set => Client.NeedEmptyToBank = value;
    }

    protected bool FirstDoWork
    {
        get => Client.FirstDoWork;
        set => Client.FirstDoWork = value;
    }

    protected int CooldownTime
    {
        get => Client.CooldownTime;
        set => Client.CooldownTime = value;
    }

    protected HashSet<ObjectItemInventoryWrapper> ItemsToUse
    {
        get => Client.ItemsToUse;
        set => Client.ItemsToUse = value;
    }

    protected HashSet<ObjectItemInventoryWrapper> ItemsToDestroy
    {
        get => Client.ItemsToDestroy;
        set => Client.ItemsToDestroy = value;
    }

    protected BotKoliClient? KoliClient
    {
        get => Client.KoliClient;
        set => Client.KoliClient = value;
    }

    protected bool FirstAction
    {
        get => Client.FirstAction;
        set => Client.FirstAction = value;
    }

    protected bool IsSpecial
    {
        get => Client.IsSpecial;
        set => Client.IsSpecial = value;
    }

    protected int ObjectsInExchange
    {
        get => Client.ObjectsInExchange;
        set => Client.ObjectsInExchange = value;
    }

    protected DateTime LastFlagRequest
    {
        get => Client.LastFlagRequest;
        set => Client.LastFlagRequest = value;
    }

    protected DateTime LastReconnect
    {
        get => Client.LastReconnect;
        set => Client.LastReconnect = value;
    }

    protected DateTime LastFight
    {
        get => Client.LastFight;
        set => Client.LastFight = value;
    }

    protected string ServerPrefix
    {
        get => Client.ServerPrefix;
        set => Client.ServerPrefix = value;
    }

    protected long LastFightMapId
    {
        get => Client.LastFightMapId;
        set => Client.LastFightMapId = value;
    }

    protected FightInfo? FightInfo
    {
        get => Client.FightInfo;
        set => Client.FightInfo = value;
    }

    protected bool HasResetStats
    {
        get => Client.HasResetStats;
        set => Client.HasResetStats = value;
    }

    protected DateTime LastArenaStatusChange
    {
        get => Client.LastArenaStatusChange;
        set => Client.LastArenaStatusChange = value;
    }

    protected string WithBot
    {
        get => Client.WithBot;
        set => Client.WithBot = value;
    }

    protected string AgainstBot
    {
        get => Client.AgainstBot;
        set => Client.AgainstBot = value;
    }

    protected bool NeedBuyRing
    {
        get => Client.NeedBuyRing;
        set => Client.NeedBuyRing = value;
    }

    protected bool NeedGuild
    {
        get => Client.NeedGuild;
        set => Client.NeedGuild = value;
    }

    protected bool GuildCreated
    {
        get => Client.GuildCreated;
        set => Client.GuildCreated = value;
    }

    protected int FightTotalCount
    {
        get => Client.FightTotalCount;
        set => Client.FightTotalCount = value;
    }

    protected bool NoAllianceFound
    {
        get => Client.NoAllianceFound;
        set => Client.NoAllianceFound = value;
    }

    protected MapCurrentEvent? LastMapCurrentEvent
    {
        get => Client.LastMapCurrentEvent;
        set => Client.LastMapCurrentEvent = value;
    }

    protected DateTime LastMapChange
    {
        get => Client.LastMapChange;
        set => Client.LastMapChange = value;
    }

    protected TrajetSettings? Trajet
    {
        get => Client.Trajet;
        set => Client.Trajet = value;
    }

    protected PartyInfo? Party
    {
        get => Client.Party;
        set => Client.Party = value;
    }

    protected bool AutoPass
    {
        get => Client.AutoPass;
        set => Client.AutoPass = value;
    }

    protected int OccupiedStuckCounter
    {
        get => Client.OccupiedStuckCounter;
        set => Client.OccupiedStuckCounter = value;
    }

    protected ContextCreationEvent.GameContext Context
    {
        get => Client.Context;
        set => Client.Context = value;
    }

    protected int FightIdToJoin
    {
        get => Client.FightIdToJoin;
        set => Client.FightIdToJoin = value;
    }

    protected long FightMemberToJoin
    {
        get => Client.FightMemberToJoin;
        set => Client.FightMemberToJoin = value;
    }

    protected long FightMapIdToJoin
    {
        get => Client.FightMapIdToJoin;
        set => Client.FightMapIdToJoin = value;
    }

    protected DateTime LastMessageReceived
    {
        get => Client.LastMessageReceived;
        set => Client.LastMessageReceived = value;
    }

    protected bool IsDisconnectionPlanned
    {
        get => Client.IsDisconnectionPlanned;
        set => Client.IsDisconnectionPlanned = value;
    }

    protected bool IsBank
    {
        get => Client.IsBank;
        set => Client.IsBank = value;
    }

    protected bool IsKoli
    {
        get => Client.IsKoli;
        set => Client.IsKoli = value;
    }

    protected bool LeaveHavenBagAsSoonAsPossible
    {
        get => Client.LeaveHavenBagAsSoonAsPossible;
        set => Client.LeaveHavenBagAsSoonAsPossible = value;
    }

    protected int MapNullCount
    {
        get => Client.MapNullCount;
        set => Client.MapNullCount = value;
    }

    protected bool IsStopped
    {
        get => Client.IsStopped;
        set => Client.IsStopped = value;
    }

    protected int KoliFightDones
    {
        get => Client.KoliFightDones;
        set => Client.KoliFightDones = value;
    }

    protected bool ArenaStatus
    {
        get => Client.ArenaStatus;
        set => Client.ArenaStatus = value;
    }

    protected int BuyRingId
    {
        get => Client.BuyRingId;
        set => Client.BuyRingId = value;
    }

    protected long LastZaapTaken
    {
        get => Client.LastZaapTaken;
        set => Client.LastZaapTaken = value;
    }

    protected int CurrentZaapIndex
    {
        get => Client.CurrentZaapIndex;
        set => Client.CurrentZaapIndex = value;
    }

    protected bool IsKoliReady
    {
        get => Client.IsKoliReady;
        set => Client.IsKoliReady = value;
    }

    protected List<long> MapsWithZaap => Client.MapsWithZaap;

    protected bool IsTrajetReady
    {
        get => Client.IsTrajetReady;
        set => Client.IsTrajetReady = value;
    }

    protected bool IsWaitingPartyMembers
    {
        get => Client.IsWaitingPartyMembers;
        set => Client.IsWaitingPartyMembers = value;
    }

    protected void SendRequest(IProtoMessage message, string typeUrl, bool setUid = false)
    {
        _transportService.SendRequest(message, typeUrl, setUid);
    }

    protected void SendRequest(IProtoMessage message, string typeUrl, int uid)
    {
        _transportService.SendRequest(message, typeUrl, uid);
    }

    protected Task SendRequestWithDelay(IProtoMessage             message,
                                        string                    typeUrl,
                                        int                       delay,
                                        Predicate<IProtoMessage>? predicate = null)
    {
        return _transportService.SendRequestWithDelay(message, typeUrl, delay, predicate);
    }

    protected void LogWarning(string messageTemplate)
    {
        _notificationService.LogWarning(messageTemplate);
    }

    protected void LogWarning<T>(string messageTemplate, T propertyValue)
    {
        _notificationService.LogWarning(messageTemplate, propertyValue);
    }

    protected void LogWarning<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1)
    {
        _notificationService.LogWarning(messageTemplate, propertyValue0, propertyValue1);
    }

    protected void LogWarning<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
    {
        _notificationService.LogWarning(messageTemplate, propertyValue0, propertyValue1, propertyValue2);
    }

    protected void LogInfo(string messageTemplate)
    {
        _notificationService.LogInfo(messageTemplate);
    }

    protected void LogInfo<T>(string messageTemplate, T propertyValue)
    {
        _notificationService.LogInfo(messageTemplate, propertyValue);
    }

    protected void LogInfo<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1)
    {
        _notificationService.LogInfo(messageTemplate, propertyValue0, propertyValue1);
    }

    protected void LogInfo<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
    {
        _notificationService.LogInfo(messageTemplate, propertyValue0, propertyValue1, propertyValue2);
    }

    protected void LogError(Exception? exception, string messageTemplate)
    {
        _notificationService.LogError(exception, messageTemplate);
    }

    protected void LogError(string messageTemplate)
    {
        _notificationService.LogError(messageTemplate);
    }

    protected void LogError<T>(string messageTemplate, T propertyValue)
    {
        _notificationService.LogError(messageTemplate, propertyValue);
    }

    protected void LogError<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1)
    {
        _notificationService.LogError(messageTemplate, propertyValue0, propertyValue1);
    }

    protected void LogError<T0>(Exception? exception, string messageTemplate, T0 propertyValue0)
    {
        _notificationService.LogError(exception, messageTemplate, propertyValue0);
    }

    protected void LogError<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2)
    {
        _notificationService.LogError(messageTemplate, propertyValue0, propertyValue1, propertyValue2);
    }

    protected void LogDiscord(string message, bool force = false, Discord.Color? color = null)
    {
        _notificationService.LogDiscord(message, force, color);
    }

    protected void LogDiscordVente(string message, Discord.Color? color = null)
    {
        _notificationService.LogDiscordVente(message, color);
    }

    protected void LogMpDiscord(string message)
    {
        _notificationService.LogMpDiscord(message);
    }

    protected void LogArchiDiscord(string message)
    {
        _notificationService.LogArchiDiscord(message);
    }

    protected void LogMaxDaily()
    {
        _notificationService.LogMaxDaily();
    }

    protected void LogChassesFinished(long startedAt, bool giveUp, GiveUpReason? giveUpReason)
    {
        _notificationService.LogChassesFinished(startedAt, giveUp, giveUpReason);
    }

    protected static string FormatKamas(long kamas, bool usePrefix)
    {
        return BotGameClient.FormatKamas(kamas, usePrefix);
    }
}
