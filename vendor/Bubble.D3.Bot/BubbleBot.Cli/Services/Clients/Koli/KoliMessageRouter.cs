using Bubble.Shared.Protocol;

namespace BubbleBot.Cli.Services.Clients.Koli;

internal sealed class KoliMessageRouter : KoliClientServiceBase, IClientMessageRouter
{
    private readonly KoliWorkflowService _workflowService;
    private readonly ClientVerificationService _verificationService;

    public KoliMessageRouter(BotKoliClient          owner,
                             BotKoliClientContext   context,
                             ClientTransportService transportService,
                             KoliNotificationService notificationService,
                             KoliWorkflowService    workflowService,
                             ClientVerificationService verificationService)
        : base(owner, context, transportService, notificationService)
    {
        _workflowService = workflowService;
        _verificationService = verificationService;
    }

    public void OnMessageReceived(IProtoMessage? message, string? typeFullName)
    {
        LogInfo("RCV: {Event}", typeFullName);

        if (typeFullName == nameof(MapMovementConfirmResponse))
        {
            Map?.OnMapMovementConfirmResponse();
            return;
        }

        if (message == null)
        {
            return;
        }

        switch (message)
        {
            case AuthenticationTicketAcceptedEvent _:
                SendRequest(new CharacterListRequest(), CharacterListRequest.TypeUrl);
                SendRequest(new BakApiTokenRequest(), BakApiTokenRequest.TypeUrl);
                BotManager.Instance.Clients[BotId] = BotController;
                break;
            case AuthenticationTicketRefusedEvent _:
                LogError("Authentication ticket refused");
                break;
            case CharacterSelectionEvent characterSelectionEvent:
                if (characterSelectionEvent.SuccessValue == null)
                {
                    LogError("Character selection failed");
                    return;
                }

                Info.Information = characterSelectionEvent.SuccessValue.Character.CharacterBasicInformationValue;
                BotManager.Instance.UpdateConsoleTitle();
                break;
            case CharacterLoadingCompleteEvent _:
                SendRequest(new ContextCreationRequest(), ContextCreationRequest.TypeUrl);

                if (GameClient?.Map == null && GameClient?.LastMapCurrentEvent != null)
                {
                    SendRequest(new ContextReadyRequest
                                {
                                    MapId = GameClient.LastMapCurrentEvent.MapId
                                },
                                ContextReadyRequest.TypeUrl);
                }

                break;
            case ContextCreationEvent creationEvent:
                if (!Settings.IsBank)
                {
                    SendRequest(new PlayerStatusUpdateRequest
                                {
                                    Status = new CharacterStatus
                                    {
                                        StatusValue = CharacterStatus.Status.StatusSolo
                                    }
                                },
                                PlayerStatusUpdateRequest.TypeUrl);
                }

                if (creationEvent.Context == ContextCreationEvent.GameContext.Fight)
                {
                    NeedNextContext = true;
                }

                break;
            case SequenceNumberEvent _:
                SendRequest(new SequenceNumberRequest
                            {
                                Number = SequenceNumber++
                            },
                            SequenceNumberRequest.TypeUrl);
                break;
            case MapCurrentEvent mapCurrentEvent:
                LogInfo("On demande des infos sur la carte {MapId}", mapCurrentEvent.MapId);

                SendRequest(new MapInformationRequest
                            {
                                MapId = (int)mapCurrentEvent.MapId
                            },
                            MapInformationRequest.TypeUrl);

                if (NeedNextContext || GameClient?.Map == null)
                {
                    SendRequest(new ContextReadyRequest
                                {
                                    MapId = mapCurrentEvent.MapId
                                },
                                ContextReadyRequest.TypeUrl);
                }

                break;
            case FightSynchronizeEvent fightSynchronizeEvent:
                LogInfo("On synchronize un combat en cours");
                FightInfo?.OnSynchronizeEvent(fightSynchronizeEvent);
                break;
            case FightJoinRunningEvent _:
                LogInfo("On rejoint un combat en cours");
                break;
            case FightMapInformationEvent fightMapInformationEvent:
                _workflowService.OnFightMapInformation(fightMapInformationEvent);
                break;
            case MapComplementaryInformationEvent mapComplementaryInformationEvent:
                _workflowService.OnNewMap(mapComplementaryInformationEvent);
                break;
            case CharacterForceSelectionEvent characterForceSelectionEvent:
                _workflowService.OnCharacterForceSelection(characterForceSelectionEvent);
                break;
            case ServerVerificationEvent _:
                _verificationService.OnServerVerificationEvent();
                break;
            case ServerChallengeEvent serverChallengeEvent:
                _verificationService.OnServerChallengeEvent(serverChallengeEvent.Value);
                break;
            case FightPlacementPossiblePositionsEvent fightPlacementPossiblePositionsEvent:
                IsInFight = true;
                FightInfo?.OnPlacementPossiblePositionsEvent(fightPlacementPossiblePositionsEvent);
                break;
            case FightFighterShowEvent fightFighterShowEvent:
                FightInfo?.OnFightFighterShowEvent(fightFighterShowEvent);
                break;
            case FightFighterRefreshEvent fightFighterRefreshEvent:
                FightInfo?.OnFightFighterRefreshEvent(fightFighterRefreshEvent);
                break;
            case FightRefreshCharacterStatsEvent fightRefreshCharacterStatsEvent:
                FightInfo?.OnFightRefreshCharacterStatsEvent(fightRefreshCharacterStatsEvent);
                break;
            case FightEndEvent fightEndEvent:
                _workflowService.OnFightEnded(fightEndEvent);
                break;
            case GameActionFightEvent actionFight:
                FightInfo?.OnFightAction(actionFight);
                break;
            case SurrenderVoteStartEvent _:
                SendRequest(new SurrenderVoteCastRequest
                            {
                                Vote = true,
                                Vote2 = true
                            },
                            SurrenderVoteCastRequest.TypeUrl);
                break;
            case MapMovementEvent mapMovementEvent:
                if (Map == null)
                {
                    return;
                }

                Map.OnMapMovementEvent(mapMovementEvent);

                if (FightInfo != null && IsInFight)
                {
                    FightInfo.OnFightMovement(mapMovementEvent);
                }

                break;
            case MapMovementRefusedEvent mapMovementRefusedEvent:
                LogWarning("Déplacement refusé vers la cellule {CellX},{CellY}",
                           mapMovementRefusedEvent.CellX,
                           mapMovementRefusedEvent.CellY);

                Map?.OnMapMovementRefused(mapMovementRefusedEvent);
                FightInfo?.OnMapMovementRefused(mapMovementRefusedEvent);
                break;
            case FightTurnEvent fightTurnEvent:
                FightInfo?.OnFightTurnEvent(fightTurnEvent);
                break;
            case SequenceStartEvent sequenceStartEvent:
                FightInfo?.OnSequenceStartEvent(sequenceStartEvent);
                break;
            case SequenceEndEvent sequenceEndEvent:
                FightInfo?.OnSequenceEndEvent(sequenceEndEvent);
                break;
            case FightIsTurnReadyEvent fightIsTurnReadyEvent:
                FightInfo?.OnFightIsTurnReadyEvent(fightIsTurnReadyEvent);
                break;
            case BasicLatencyStatsRequest _:
                SendRequest(new BasicLatencyStatsRequest
                            {
                                Latency = Random.Shared.Next(500, 650)
                            },
                            BasicLatencyStatsRequest.TypeUrl,
                            LastRequestUid);
                break;
            case ServerSessionReadyEvent _:
                SendRequest(new ClientIdRequest
                            {
                                Id = Hwid
                            },
                            ClientIdRequest.TypeUrl);
                break;
            case TextInformationEvent textInformationEvent:
                if (textInformationEvent.MessageId is 170 or 171 or 172 or 173 or 174 or 175)
                {
                    FightInfo?.OnErrorCast();
                }

                break;
        }
    }
}
