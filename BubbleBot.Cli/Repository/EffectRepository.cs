using System.Collections.Frozen;
using Bubble.Core.Datacenter;
using Bubble.Core.Datacenter.Datacenter.Effects;
using Bubble.Core.Services;

namespace BubbleBot.Cli.Repository;

public class EffectRepository : Singleton<EffectRepository>
{
    private Dictionary<int, Effects> _effects;

    public EffectRepository()
    {
        _effects = new Dictionary<int, Effects>();
    }

    public void Initialize()
    {
        _effects = (DatacenterService.Load<Effects>()).Values.ToDictionary(x => x.Id);
    }
    
    public Effects? GetEffect(int id)
    {
        return _effects.TryGetValue(id, out var effect) ? effect : null;
    }
    
    public int GetEffectCategory(ActionId actionId)
    {
        if (!_effects.TryGetValue((int)actionId, out var effect))
        {
            return -1;
        }

        return effect.Category;
    }
}