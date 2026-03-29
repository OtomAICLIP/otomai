namespace Bubble.DamageCalculation.FighterManagement.FighterStats;

public class HaxeDetailedStat : HaxeStat
{
    public int ObjectsAndMountBonus { get; private set; }
    public int Additional { get; private set; }
    public int BaseValue { get; private set; }
    public int AlignGiftBonus { get; private set; }
    public int ContextModif { get; private set; }

    public HaxeDetailedStat(int id, int baseValue, int additional, int objectsAndMountBonus, int alignGiftBonus,
                            int contextModif) : base(id)
    {
        SetBaseValue(baseValue);
        SetAdditional(additional);
        SetObjectsAndMountBonus(objectsAndMountBonus);
        SetAlignGiftBonus(alignGiftBonus);
        SetContextModif(contextModif);
    }

    public override void UpdateStatWithValue(int value, bool positive)
    {
        var isPositive = positive ? 1 : -1;
        var realValue  = Math.Floor((double)value * isPositive);

        ContextModif += (int)realValue;

        SetContextModif(ContextModif);
    }

    public int SetObjectsAndMountBonus(int value)
    {
        ObjectsAndMountBonus = value;
        UpdateTotal();
        return ObjectsAndMountBonus;
    }

    public int SetAdditional(int value)
    {
        Additional = value;
        UpdateTotal();
        return Additional;
    }

    public int SetBaseValue(int value)
    {
        BaseValue = value;
        UpdateTotal();
        return BaseValue;
    }

    public int SetAlignGiftBonus(int value)
    {
        AlignGiftBonus = value;
        UpdateTotal();
        return AlignGiftBonus;
    }

    public int SetContextModif(int value)
    {
        ContextModif = value;
        UpdateTotal();
        return ContextModif;
    }

    private void UpdateTotal()
    {
        Total = BaseValue + Additional + ObjectsAndMountBonus + AlignGiftBonus + ContextModif;
    }
}