namespace Bubble.DamageCalculation.FighterManagement.FighterStats;

public class HaxeSimpleStat : HaxeStat
{
    public HaxeSimpleStat(int id, int total) : base(id)
    {
        Total = total;
    }

    public override void UpdateStatWithValue(int value, bool positive)
    {
        var modificator = positive ? 1 : -1;
        value =  (int)Math.Floor((double)value * modificator);
        Total += value;
    }
}