using Bubble.Core.Datacenter;
using Bubble.Core.Datacenter.Datacenter;
using Bubble.Core.Datacenter.Datacenter.Items;
using Bubble.Core.Services;

namespace BubbleBot.Cli.Repository;

public class ItemRepository : Singleton<ItemRepository>
{
    private Dictionary<ushort, Items> _items;

    public void Initialize()
    {
        _items = (DatacenterService.Load<Items>()).Values.ToDictionary(x => x.Id);
    }
    
    public Items? GetItem(ushort id)
    {
        return _items.TryGetValue(id, out var item) ? item : null;
    }
}