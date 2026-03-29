using System.Collections.Concurrent;
using Bubble.Core.Datacenter.Datacenter.Items;
using BubbleBot.Cli.Repository;

namespace BubbleBot.Cli.Services;

public class Inventory
{
    public ConcurrentDictionary<int, ObjectItemInventoryWrapper> Items { get; set; } = new();
    public long Kamas { get; set; }
    public long KamasBase { get; set; }
    
    public BotGameClient Client { get; }
    
    public Inventory(BotGameClient client)
    {
        Client = client;
    }
}

public class ObjectItemInventoryWrapper
{
    public ObjectItemInventory Item { get; }
    public Items? Template { get; }
    public int BaseQuantity { get; set; }

    public ObjectItemInventoryWrapper(ObjectItemInventory item)
    {
        Item = item;
        BaseQuantity = item.Item.Quantity;
        Template = ItemRepository.Instance.GetItem((ushort)item.Item.Gid);
    }
}
