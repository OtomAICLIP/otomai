using System.Diagnostics;
using Bubble.Core.Datacenter.Datacenter.WorldGraph;
using Bubble.Core.Network;
using Bubble.Core.Network.Proxy;
using Bubble.Shared.Protocol;
using BubbleBot.Cli.Models;
using BubbleBot.Cli.Repository.Maps;
using BubbleBot.Cli.Services.Clients;
using BubbleBot.Cli.Services.Clients.Contracts;
using BubbleBot.Cli.Services.Clients.Koli;
using BubbleBot.Cli.Services.Fight;
using BubbleBot.Cli.Services.Maps;
using BubbleBot.Cli.Services.Parties;
using BubbleBot.Cli.Services.TreasureHunts;

namespace BubbleBot.Cli;

public class BotKoliClient : TcpClient, IMapMovementContext, IFightClientContext
{
    public const bool Debug = false;
    public const bool DebugAutoFight = false;

    private readonly BotKoliClientContext _context;
    private readonly KoliNotificationService _notificationService;
    private readonly ClientTransportService _transportService;
    private readonly ClientVerificationService _verificationService;
    private readonly KoliWorkflowService _workflowService;
    private readonly KoliMessageRouter _messageRouter;
    private KoliRuntimeState State => _context.State;

    public BotKoliClient(
        BotClient       client,
        string          id,
        SaharachAccount account,
        string          token,
        string?         serverName,
        int             serverId,
        string          hwid,
        string          address,
        int             port,
        Socks5Options?  proxy,
        BotSettings     settings)
        : base(address, port, proxy)
    {
        var resolvedServerName = serverName ?? "Unknown";

        _context = new BotKoliClientContext(id, hwid, serverId, resolvedServerName, client, account, token, settings);
        State.ConnectedAt = Stopwatch.GetTimestamp();
        State.LastReconnect = DateTime.UtcNow;
        State.IsKoli = true;
        State.IsSpecial = File.Exists("special") || File.Exists("special.txt");

        if (resolvedServerName.Contains(' '))
        {
            var serverNumber = resolvedServerName.Split(" ")[1];
            State.ServerPrefix = $"{resolvedServerName[0]}/{serverNumber}";
        }

        _notificationService = new KoliNotificationService(_context, PlanifyDisconnect);
        _transportService = new ClientTransportService(_context, _notificationService, SendAsync);
        _verificationService = new ClientVerificationService(_context, _transportService);
        _workflowService = new KoliWorkflowService(this, _context, _transportService, _notificationService);
        _messageRouter = new KoliMessageRouter(this,
                                               _context,
                                               _transportService,
                                               _notificationService,
                                               _workflowService,
                                               _verificationService);
        _transportService.AttachRouter(_messageRouter);
    }

    public string BotId => _context.BotId;
    public string Hwid => _context.Hwid;
    public int ServerId => _context.ServerId;
    public string ServerName => _context.ServerName;
    public SaharachAccount Account
    {
        get => _context.Account;
        set => _context.Account = value;
    }

    public long LastMessageSent => _context.LastMessageSent;

    public BotGameClient? GameClient
    {
        get => State.GameClient;
        set => State.GameClient = value;
    }

    public Map? Map
    {
        get => State.Map;
        set => State.Map = value;
    }

    public WorldPath WorldPath => State.WorldPath;

    public List<WorldGraphEdge> AutoPath
    {
        get => State.AutoPath;
        set => State.AutoPath = value;
    }

    public int AutoPathIndex
    {
        get => State.AutoPathIndex;
        set => State.AutoPathIndex = value;
    }

    public bool IsInFight
    {
        get => State.IsInFight;
        set => State.IsInFight = value;
    }

    public long AutoPathEndMapId
    {
        get => State.AutoPathEndMapId;
        set => State.AutoPathEndMapId = value;
    }

    public long PlayerId => _context.CharacterId;

    public long CharacterId
    {
        get => _context.CharacterId;
        internal set => _context.CharacterId = value;
    }

    public PartyInfo? Party => GameClient?.Party;
    public TrajetSettings? Trajet => GameClient?.Trajet;
    public TreasureHuntData? TreasureHuntData => GameClient?.TreasureHuntData;

    public FightActor? Fighter
    {
        get => State.Fighter;
        set => State.Fighter = value;
    }

    public List<SpellWrapper> Spells
    {
        get => State.Spells;
        internal set => State.Spells = value;
    }

    public CharacterInfo Info
    {
        get => State.Info;
        set => State.Info = value;
    }

    public MapComplementaryInformationEvent? MapCurrentEvent
    {
        get => State.MapCurrentEvent;
        set => State.MapCurrentEvent = value;
    }

    public bool NeedNextContext
    {
        get => State.NeedNextContext;
        set => State.NeedNextContext = value;
    }

    public bool Connected
    {
        get => State.Connected;
        set => State.Connected = value;
    }

    public long ConnectedAt
    {
        get => State.ConnectedAt;
        set => State.ConnectedAt = value;
    }

    public bool IsSpecial
    {
        get => State.IsSpecial;
        set => State.IsSpecial = value;
    }

    public DateTime LastReconnect
    {
        get => State.LastReconnect;
        set => State.LastReconnect = value;
    }

    public string ServerPrefix
    {
        get => State.ServerPrefix;
        set => State.ServerPrefix = value;
    }

    public FightInfo? FightInfo
    {
        get => State.FightInfo;
        set => State.FightInfo = value;
    }

    public bool AutoPass
    {
        get => State.AutoPass;
        set => State.AutoPass = value;
    }

    public bool IsDisconnectionPlanned
    {
        get => State.IsDisconnectionPlanned;
        set => State.IsDisconnectionPlanned = value;
    }

    public bool IsKoli
    {
        get => State.IsKoli;
        set => State.IsKoli = value;
    }

    protected override void OnConnected()
    {
        _workflowService.OnConnected();
    }

    protected override void OnDisconnected()
    {
        _workflowService.OnDisconnected();
    }

    protected override void OnReceived(byte[] buffer, long offset, long size)
    {
        _transportService.HandleReceived(buffer, offset, size);
    }

    public void PlanifyDisconnect()
    {
        _workflowService.PlanifyDisconnect();
    }

    public static string FormatKamas(long kamas, bool usePrefix)
    {
        return BotGameClient.FormatKamas(kamas, usePrefix);
    }

    public void DoWork(bool noDelay = false)
    {
        _workflowService.DoWork(noDelay);
    }

    public Task SendRequestWithDelay(IProtoMessage             message,
                                     string                    typeUrl,
                                     int                       delay,
                                     Predicate<IProtoMessage>? predicate = null)
    {
        return _transportService.SendRequestWithDelay(message, typeUrl, delay, predicate);
    }

    public void SendRequest(IProtoMessage message, string typeUrl, bool setUid = false)
    {
        _transportService.SendRequest(message, typeUrl, setUid);
    }

    public void SendRequest(IProtoMessage message, string typeUrl, int uid)
    {
        _transportService.SendRequest(message, typeUrl, uid);
    }

    public void UpdateCharacterInfoFrom(EntityLook look,
                                        ActorPositionInformation.ActorInformation.RolePlayActor.NamedActor namedActorValue,
                                        EntityDisposition actorDisposition)
    {
        _workflowService.UpdateCharacterInfoFrom(look, namedActorValue, actorDisposition);
    }

    public void ResetWorldPath()
    {
        _workflowService.ResetWorldPath();
    }

    public void OnCellChanged()
    {
    }

    public void OnCellPathNotFound()
    {
    }

    public bool IsPlayerBreed()
    {
        return true;
    }

    public void SetIsAgainstBot(string fighterName)
    {
        _workflowService.SetIsAgainstBot(fighterName);
    }

    public void SetWithBot(string fighterName)
    {
        _workflowService.SetWithBot(fighterName);
    }

    public void LogKoli()
    {
        _workflowService.LogKoli();
    }

    public void LogWarning(string messageTemplate) => _notificationService.LogWarning(messageTemplate);
    public void LogWarning<T>(string messageTemplate, T propertyValue) => _notificationService.LogWarning(messageTemplate, propertyValue);
    public void LogWarning<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1) => _notificationService.LogWarning(messageTemplate, propertyValue0, propertyValue1);
    public void LogWarning<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => _notificationService.LogWarning(messageTemplate, propertyValue0, propertyValue1, propertyValue2);
    public void LogInfo(string messageTemplate) => _notificationService.LogInfo(messageTemplate);
    public void LogInfo<T>(string messageTemplate, T propertyValue) => _notificationService.LogInfo(messageTemplate, propertyValue);
    public void LogInfo<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1) => _notificationService.LogInfo(messageTemplate, propertyValue0, propertyValue1);
    public void LogInfo<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => _notificationService.LogInfo(messageTemplate, propertyValue0, propertyValue1, propertyValue2);
    public void LogError(Exception? exception, string messageTemplate) => _notificationService.LogError(exception, messageTemplate);
    public void LogError(string messageTemplate) => _notificationService.LogError(messageTemplate);
    public void LogError<T>(string messageTemplate, T propertyValue) => _notificationService.LogError(messageTemplate, propertyValue);
    public void LogError<T0, T1>(string messageTemplate, T0 propertyValue0, T1 propertyValue1) => _notificationService.LogError(messageTemplate, propertyValue0, propertyValue1);
    public void LogError<T0>(Exception? exception, string messageTemplate, T0 propertyValue0) => _notificationService.LogError(exception, messageTemplate, propertyValue0);
    public void LogError<T0, T1, T2>(string messageTemplate, T0 propertyValue0, T1 propertyValue1, T2 propertyValue2) => _notificationService.LogError(messageTemplate, propertyValue0, propertyValue1, propertyValue2);
}
