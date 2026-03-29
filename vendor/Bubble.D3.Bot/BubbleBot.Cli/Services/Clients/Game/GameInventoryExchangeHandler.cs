using System.Collections.Concurrent;
using System.Text;
using Bubble.Shared.Protocol;
using BubbleBot.Cli.Logging;
using BubbleBot.Cli.Models;
using BubbleBot.Cli.Repository;
using Discord;

namespace BubbleBot.Cli.Services.Clients.Game;

internal sealed class GameInventoryExchangeHandler : GameClientServiceBase, IGameMessageHandler
{
    private readonly GameWorkflowService _workflowService;
    private readonly GameSessionService _sessionService;

    public GameInventoryExchangeHandler(BotGameClientContext    context,
                                        ClientTransportService  transportService,
                                        GameNotificationService notificationService,
                                        GameWorkflowService     workflowService,
                                        GameSessionService      sessionService)
        : base(context, transportService, notificationService)
    {
        _workflowService = workflowService;
        _sessionService = sessionService;
    }

    public bool TryHandle(IProtoMessage message)
    {
        switch (message)
        {
            case InventoryContentEvent inventoryContentEvent:
                HandleInventoryContent(inventoryContentEvent);
                return true;
            case ObjectAddedEvent objectAddedEvent:
                HandleObjectAdded(objectAddedEvent);
                return true;
            case ObjectDeletedEvent objectDeletedEvent:
                LogInfo("Objet supprimé de l'inventaire {ObjectId}", objectDeletedEvent.ObjectUid);
                Inventory.Items.TryRemove(objectDeletedEvent.ObjectUid, out _);
                return true;
            case ObjectQuantityEvent objectQuantityEvent:
                HandleObjectQuantity(objectQuantityEvent);
                return true;
            case KamasUpdateEvent kamasUpdateEvent:
                Inventory.Kamas = kamasUpdateEvent.Quantity;
                return true;
            case ObjectAveragePricesEvent _:
                LogInfo("Prix moyen des objets chargé");
                return true;
            case ExchangeBidHouseOfflineSoldItemsEvent exchangeBidHouseOfflineSoldItemsEvent:
                HandleOfflineSoldItems(exchangeBidHouseOfflineSoldItemsEvent);
                return true;
            case ExchangeErrorEvent exchangeErrorEvent:
                HandleExchangeError(exchangeErrorEvent);
                return true;
            case ExchangeRequestedTradeEvent exchangeRequestedTradeEvent:
                HandleExchangeRequestedTrade(exchangeRequestedTradeEvent);
                return true;
            case ExchangeStartedWithPodsEvent _:
                HandleExchangeStarted();
                return true;
            case ExchangeObjectsAddedEvent _:
                ObjectsInExchange++;
                return true;
            case ExchangeReadyEvent exchangeReadyEvent:
                HandleExchangeReady(exchangeReadyEvent);
                return true;
            case ExchangeLeaveEvent _:
                HandleExchangeLeave();
                return true;
            case ExchangeBidBuyerStartedEvent _:
                _workflowService.OnBidHouseEquipementOpened();
                return true;
            case ExchangeTypesItemsExchangerDescriptionForUserEvent exchangeTypesItemsExchangerDescriptionForUserEvent:
                _workflowService.OnBidHouseItemFound(exchangeTypesItemsExchangerDescriptionForUserEvent);
                return true;
            default:
                return false;
        }
    }

    private void HandleInventoryContent(InventoryContentEvent inventoryContentEvent)
    {
        LogInfo("Inventaire chargé");

        Inventory.Kamas = inventoryContentEvent.Kamas;
        if (Inventory.KamasBase == 0)
        {
            Inventory.KamasBase = inventoryContentEvent.Kamas;
        }

        if (!Inventory.Items.IsEmpty)
        {
            var oldInventory = Inventory.Items;
            Inventory.Items = new ConcurrentDictionary<int, ObjectItemInventoryWrapper>();

            foreach (var obj in inventoryContentEvent.Objects)
            {
                Inventory.Items[obj.Item.Uid] = new ObjectItemInventoryWrapper(obj);

                var oldObj = oldInventory.FirstOrDefault(x => x.Value.Item.Item.Gid == obj.Item.Gid).Value;
                if (oldObj != null)
                {
                    Inventory.Items[obj.Item.Uid].BaseQuantity = obj.Item.Quantity;
                }
            }

            return;
        }

        Inventory.Items = new ConcurrentDictionary<int, ObjectItemInventoryWrapper>(
            inventoryContentEvent.Objects.ToDictionary(x => x.Item.Uid, x => new ObjectItemInventoryWrapper(x)));
    }

    private void HandleObjectAdded(ObjectAddedEvent objectAddedEvent)
    {
        LogInfo("Objet ajouté à l'inventaire {ObjectId}", objectAddedEvent.Object);
        Inventory.Items[objectAddedEvent.Object.Item.Uid] = new ObjectItemInventoryWrapper(objectAddedEvent.Object);

        var obj = Inventory.Items[objectAddedEvent.Object.Item.Uid];
        if (obj.Template?.Usable != true)
        {
            return;
        }

        ItemsToUse.Add(obj);
        if (!Client.RecomputeAutoOpen())
        {
            return;
        }

        SendRequest(new ObjectUseRequest
                    {
                        ObjectUid = objectAddedEvent.Object.Item.Uid
                    },
                    ObjectUseRequest.TypeUrl);

        LogDiscord($"Ouverture de {obj.Template.Name}", true);
    }

    private void HandleObjectQuantity(ObjectQuantityEvent objectQuantityEvent)
    {
        LogInfo(
            $"Quantité d'objet mise à jour pour {objectQuantityEvent.Object.ObjectUid} : {objectQuantityEvent.Object.Quantity}");

        if (!Inventory.Items.TryGetValue(objectQuantityEvent.Object.ObjectUid, out var obj))
        {
            return;
        }

        LogDiscord($"Quantité d'objet mise à jour pour {obj.Template?.Name} : {objectQuantityEvent.Object.Quantity}",
                   true);
        obj.Item.Item.Quantity = objectQuantityEvent.Object.Quantity;

        if (obj.Template?.Usable == true)
        {
            ItemsToUse.Add(obj);
        }

        if (obj.Template == null || obj.Template.TypeId == 175 || !obj.Template.IsDestructible)
        {
            return;
        }

        if (Trajet != null && Trajet.ItemsToKeep.Contains(obj.Template.Id))
        {
            LogInfo("Objet à garder " + obj.Template.Name);
            return;
        }

        ItemsToDestroy.Add(obj);
    }

    private void HandleOfflineSoldItems(ExchangeBidHouseOfflineSoldItemsEvent exchangeBidHouseOfflineSoldItemsEvent)
    {
        LogInfo("Ventes lors de la dernière connexion:");

        var soldItems = new StringBuilder();
        soldItems.AppendLine("Ventes lors de la dernière connexion:");
        foreach (var item in exchangeBidHouseOfflineSoldItemsEvent.BidHouseItems)
        {
            var template = ItemRepository.Instance.GetItem((ushort)item.ObjectGid);
            soldItems.AppendLine(
                $"Vente de x{item.Quantity} {template?.Name} pour {FormatKamas(item.PriceDateEffect.Price, true)} kamas");
        }

        LogDiscordVente(soldItems.ToString(), Color.Gold);
    }

    private void HandleExchangeError(ExchangeErrorEvent exchangeErrorEvent)
    {
        LogInfo("Erreur d'échange, on réessaye dans 20 secondes ({ErrorType})", exchangeErrorEvent.ErrorType);

        _ = Task.Run(async () =>
        {
            LogInfo("Réessai de l'échange");
            await Task.Delay(Random.Shared.Next(2000, 5000));
            _sessionService.ExchangeToBankCharacter();
        });
    }

    private void HandleExchangeRequestedTrade(ExchangeRequestedTradeEvent exchangeRequestedTradeEvent)
    {
        LogInfo("Echange demandé par {CharacterId}", exchangeRequestedTradeEvent.SourceId);
        if (_settings.IsBank)
        {
            LogInfo("On accepte l'échange");
            SendRequest(new ExchangeAcceptRequest(), ExchangeAcceptRequest.TypeUrl);
            ObjectsInExchange = 0;
            return;
        }

        SendRequest(new DialogLeaveEvent
                    {
                        DialogType = DialogType.DialogExchange
                    },
                    ExchangeAcceptRequest.TypeUrl);
    }

    private void HandleExchangeStarted()
    {
        ObjectsInExchange = 0;
        if (_settings.IsBank)
        {
            return;
        }

        LogInfo("Echange démarré avec le joueur banque, on envoie les roses");

        foreach (var item in Inventory.Items)
        {
            if (item.Value.Item.Item.Gid == 15263)
            {
                LastRosesAmount = item.Value.Item.Item.Quantity;
                LogInfo("Envoi de {Quantity} Roses des Sables", LastRosesAmount);
            }

            if (item.Value.Template?.TypeId == 175 || item.Value.Item.Item.Gid == 15263)
            {
                LogInfo("Envoi de x{Quantity} {ItemName}",
                        item.Value.Item.Item.Quantity,
                        item.Value.Template?.Name);
                SendRequest(new ExchangeObjectMoveRequest
                            {
                                ObjectUid = item.Value.Item.Item.Uid,
                                Quantity = item.Value.Item.Item.Quantity,
                            },
                            ExchangeObjectMoveRequest.TypeUrl);
                LogInfo("Envoyé");

                ObjectsInExchange++;
                Thread.Sleep(200);
            }
            else
            {
                LogInfo("On ne prend pas en compte l'objet {ItemName}", item.Value.Template?.Name);
            }
        }

        LogInfo("On accepte l'échange");
        _ = SendRequestWithDelay(new ExchangeReadyRequest
                                 {
                                     Ready = true,
                                     Step = ObjectsInExchange,
                                     Efvb = null
                                 },
                                 ExchangeReadyRequest.TypeUrl,
                                 2000);
    }

    private void HandleExchangeReady(ExchangeReadyEvent exchangeReadyEvent)
    {
        LogInfo("Echange prêt pour {CharacterId}, {State}",
                exchangeReadyEvent.CharacterId,
                exchangeReadyEvent.Ready);

        if (exchangeReadyEvent.CharacterId == _characterId)
        {
            return;
        }

        _ = SendRequestWithDelay(new ExchangeReadyRequest
                                 {
                                     Ready = true,
                                     Step = ObjectsInExchange,
                                     Efvb = null
                                 },
                                 ExchangeReadyRequest.TypeUrl,
                                 500);
    }

    private void HandleExchangeLeave()
    {
        if (NeedEmptyToBank && !_settings.IsBank)
        {
            LogMpDiscord($"**x{LastRosesAmount}** Roses des Sables envoyée au joueur banque");
            Client.PlanifyDisconnect();
            return;
        }

        if (_settings.IsBank)
        {
            LogMpDiscord("Roses des Sables récupérée ");
        }
    }
}
