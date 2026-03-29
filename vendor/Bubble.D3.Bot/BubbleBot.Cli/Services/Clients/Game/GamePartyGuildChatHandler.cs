using Bubble.Shared.Protocol;
using BubbleBot.Cli.Repository;

namespace BubbleBot.Cli.Services.Clients.Game;

internal sealed class GamePartyGuildChatHandler : GameClientServiceBase, IGameMessageHandler
{
    private readonly GameWorkflowService _workflowService;

    public GamePartyGuildChatHandler(BotGameClientContext    context,
                                     ClientTransportService  transportService,
                                     GameNotificationService notificationService,
                                     GameWorkflowService     workflowService)
        : base(context, transportService, notificationService)
    {
        _workflowService = workflowService;
    }

    public bool TryHandle(IProtoMessage message)
    {
        switch (message)
        {
            case ChatChannelMessageEvent chatChannelMessageEvent:
                HandleChatChannelMessage(chatChannelMessageEvent);
                return true;
            case GuildCreationStartedEvent _:
                LogInfo("Création de guilde démarrée");
                _workflowService.OnGuildCreationStarted();
                return true;
            case GuildCreationResultEvent guildCreationResultEvent:
                LogInfo("Résultat de la création de guilde {Result}", guildCreationResultEvent.Result);
                _workflowService.OnGuildCreationResult(guildCreationResultEvent);
                return true;
            case PartyJoinEvent partyJoinEvent:
                HandlePartyJoin(partyJoinEvent);
                return true;
            case PartyNewMemberEvent _:
                if (Party != null && Party.Leader == PlayerId)
                {
                    Client.DoWork();
                }

                return true;
            case PartyInvitationEvent partyInvitationEvent:
                HandlePartyInvitation(partyInvitationEvent);
                return true;
            case PartyLeaveEvent _:
                LogInfo("Quitter le groupe");
                Party?.Members.Remove(PlayerId);
                Party = null;
                return true;
            case PartyMemberRemoveEvent partyMemberRemoveEvent:
                Party?.Members.Remove(partyMemberRemoveEvent.LeavingPlayerId);
                return true;
            case PartyMemberInFightEvent partyMemberInFightEvent:
                HandlePartyMemberInFight(partyMemberInFightEvent);
                return true;
            case PartyJoinErrorEvent partyJoinErrorEvent:
                LogError("Erreur de groupe {Error}", partyJoinErrorEvent.Reason);
                return true;
            case AutoFollowDeactivatedEvent _:
                LogInfo("Auto-follow désactivé");
                SendRequest(new AutoFollowActivationRequest(), AutoFollowActivationRequest.TypeUrl, true);
                return true;
            case AutoFollowActivatedEvent _:
                LogInfo("Auto-follow activé");
                return true;
            case FightAutoJoinDeactivatedEvent _:
                LogInfo("Auto-join désactivé");
                SendRequest(new FightAutoJoinActivationRequest
                            {
                                Ebrm = 0
                            },
                            FightAutoJoinActivationRequest.TypeUrl,
                            true);
                return true;
            case FightAutoReadyDeactivatedEvent _:
                LogInfo("Auto-ready désactivé");
                SendRequest(new FightAutoReadyActivationRequest(), FightAutoReadyActivationRequest.TypeUrl, true);
                return true;
            default:
                return false;
        }
    }

    private void HandleChatChannelMessage(ChatChannelMessageEvent chatChannelMessageEvent)
    {
        LogInfo("Message reçu sur le canal {Channel} : {Message}",
                chatChannelMessageEvent.Channel,
                chatChannelMessageEvent.Content);

        if (chatChannelMessageEvent.Channel is Channel.Private or Channel.Guild)
        {
            LogMpDiscord(
                $"({chatChannelMessageEvent.Channel}) {chatChannelMessageEvent.SenderName}: {chatChannelMessageEvent.Content}");
        }
    }

    private void HandlePartyJoin(PartyJoinEvent partyJoinEvent)
    {
        var party = PartyRepository.Instance.GetOrCreatePartyManager(ServerId)
                                   .CreateOrAddMember(partyJoinEvent.PartyId, PlayerId);

        party.Leader = partyJoinEvent.LeaderId;
        party.Members.Clear();
        foreach (var member in partyJoinEvent.Members)
        {
            party.Members.Add(member.Id);
        }

        if (party.Leader != PlayerId)
        {
            SendRequest(new FightAutoReadyActivationRequest(), FightAutoReadyActivationRequest.TypeUrl, true);
            SendRequest(new FightAutoJoinActivationRequest
                        {
                            Ebrm = 0
                        },
                        FightAutoJoinActivationRequest.TypeUrl,
                        true);
            SendRequest(new AutoFollowActivationRequest(), AutoFollowActivationRequest.TypeUrl, true);
        }

        Party = party;
        Client.DoWork();
    }

    private void HandlePartyInvitation(PartyInvitationEvent partyInvitationEvent)
    {
        LogInfo("Invitation en groupe de {CharacterId}", partyInvitationEvent.FromPlayerName);
        if (!BotManager.Instance.IsBotName(partyInvitationEvent.FromPlayerName))
        {
            return;
        }

        SendRequest(new PartyInvitationAcceptRequest
                    {
                        PartyId = partyInvitationEvent.PartyId,
                    },
                    PartyInvitationAcceptRequest.TypeUrl);
    }

    private void HandlePartyMemberInFight(PartyMemberInFightEvent partyMemberInFightEvent)
    {
        LogInfo("Membre du groupe en combat");
        if (Map != null && Map.Id == partyMemberInFightEvent.StandardFightMap.MapId)
        {
            FightIdToJoin = partyMemberInFightEvent.FightId;
            FightMemberToJoin = partyMemberInFightEvent.MemberId;
            FightMapIdToJoin = partyMemberInFightEvent.StandardFightMap.MapId;

            _ = SendRequestWithDelay(new FightJoinRequest
                                     {
                                         FighterId = partyMemberInFightEvent.MemberId,
                                         FightId = partyMemberInFightEvent.FightId
                                     },
                                     FightJoinRequest.TypeUrl,
                                     2000);
            return;
        }

        Map?.GoToMap(partyMemberInFightEvent.StandardFightMap.MapId);
    }
}
