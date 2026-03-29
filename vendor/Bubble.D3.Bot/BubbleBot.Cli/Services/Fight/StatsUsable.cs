using Bubble.DamageCalculation;

namespace BubbleBot.Cli.Services.Fight;

public class StatsUsable : StatsField
{
    public long? LimitEquipped { get; }

    public StatsUsable(IStatsOwner owner, StatId characteristic, long valueBase)
        : base(owner, characteristic, valueBase) { }

    public StatsUsable(IStatsOwner owner, StatId characteristic, long valueBase, long? limit,
                       long?       limitEquipped)
        : base(owner, characteristic, valueBase, limit, true)
    {
        LimitEquipped = limitEquipped;
    }

    public short Used { get; set; }

    public long TotalMax => base.Total;

    public override long Total =>
        (Equipped > LimitEquipped ? LimitEquipped.Value : Equipped) + Base + Additional + Context;

    public int UsedByCaster { get; set; }
    public int AvailableAtRoundStart { get; set; }

    public void Use(short use)
    {
        Used += use;
    }

    public void ResetUsed()
    {
        Used = 0;
        UsedByCaster = 0;
    }


    public override StatsField Reset(IStatsOwner owner)
    {
        var clone = new StatsUsable(owner, Characteristic, Base, Limit, LimitEquipped)
        {
            Additional = Additional,
            Context = 0,
            Equipped = Equipped,
            Given = Given,
        };

        return clone;
    }
}