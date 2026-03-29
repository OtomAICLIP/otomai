using Bubble.Core.Datacenter.Datacenter.Spells;

namespace Bubble.DamageCalculation.SpellManagement;

public class HaxeSpellState
{
    public uint Id { get; }
    public IList<int> StateEffects { get; }

    public SpellStates Template { get; }

    public HaxeSpellState(uint id, IList<int> stateEffects, SpellStates template)
    {
        Id           = id;
        StateEffects = stateEffects;
        Template     = template;
    }

    public bool HasEffect(int effectId)
    {
        return StateEffects.Contains(effectId);
    }
}