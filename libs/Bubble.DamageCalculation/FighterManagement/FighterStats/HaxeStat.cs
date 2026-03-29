using Bubble.DamageCalculation.SpellManagement;
using Bubble.DamageCalculation.Tools;

namespace Bubble.DamageCalculation.FighterManagement.FighterStats;

public abstract class HaxeStat
{
    public int Total { get; protected set; }

    public int Id { get; protected set; }

    protected HaxeStat(int id)
    {
        Total = 0;
        Id    = id;
    }

    public abstract void UpdateStatWithValue(int value, bool positive);


    public void UpdateStatFromEffect(HaxeSpellEffect effect, bool positive)
    {
        if (ActionIdHelper.IsFlatStatBoostActionId(effect.ActionId) ||
            ActionIdHelper.IsPercentStatBoostActionId(effect.ActionId))
        {
            UpdateStatWithValue(effect.GetMinRoll(), positive);
        }
    }
}