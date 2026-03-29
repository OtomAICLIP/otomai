namespace Bubble.DamageCalculation.FighterManagement.FighterStats;

public class HaxeUsableStat : HaxeDetailedStat
{
    public int Used { get; private set; }

    public HaxeUsableStat(int id, int baseValue, int additional, int objectsAndMountBonus, int alignGiftBonus,
                          int contextModif, int used)
        : base(id, baseValue, additional, objectsAndMountBonus, alignGiftBonus, contextModif)
    {
        Used = used;
    }
}