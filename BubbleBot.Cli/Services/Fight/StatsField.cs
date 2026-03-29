using Bubble.DamageCalculation;
using Bubble.DamageCalculation.FighterManagement.FighterStats;

namespace BubbleBot.Cli.Services.Fight;

public class StatsField : IStatsField
{
    private readonly StatsFields.StatsFormulasHandler? _formulas;
    private readonly bool _limitEquippedOnly;
    private long? _limit;
    private long _valueAdditional;
    protected long ValueBase;
    protected long ValueContext;
    protected long ValueEquipped;
    protected long ValueGiven;

    public StatsField(IStatsOwner                       owner,
                      StatId                            characteristic,
                      long                              valueBase,
                      StatsFields.StatsFormulasHandler? formulas = null)
    {
        ValueBase = valueBase;
        _formulas = formulas;
        Characteristic = characteristic;
        Owner = owner;

        RealBase = valueBase;
    }

    public StatsField(IStatsOwner                       owner,
                      StatId                            characteristic,
                      long                              valueBase,
                      long?                             limit,
                      bool                              limitEquippedOnly = false,
                      StatsFields.StatsFormulasHandler? formulas          = null)
    {
        ValueBase = valueBase;
        _limit = limit;
        _limitEquippedOnly = limitEquippedOnly;
        Characteristic = characteristic;
        Owner = owner;
        _formulas = formulas;

        RealBase = valueBase;
    }

    public IStatsOwner Owner { get; }

    public StatId Characteristic { get; }

    public long RealBase
    {
        get;
        private set;
    }

    public virtual long Base
    {
        get
        {
            if (_formulas != null)
            {
                return _formulas(Owner) + ValueBase;
            }

            return ValueBase;
        }
        set
        {
            RealBase = value;
            ValueBase = value;
            OnModified();
        }
    }

    public virtual long Equipped
    {
        get => ValueEquipped;
        set
        {
            ValueEquipped = value;
            OnModified();
        }
    }

    public virtual long Given
    {
        get => ValueGiven;
        set
        {
            ValueGiven = value;
            OnModified();
        }
    }

    public virtual long Context
    {
        get => ValueContext;
        set
        {
            ValueContext = value;
            OnModified();
        }
    }

    public virtual long Additional
    {
        get => _valueAdditional;
        set
        {
            _valueAdditional = value;
            OnModified();
        }
    }

    public virtual long Total
    {
        get
        {
            var totalNoBoost = Base + Additional + Equipped;

            if (_limitEquippedOnly && totalNoBoost > Limit)
            {
                totalNoBoost = Limit.Value;
            }

            var total = totalNoBoost + Given + Context;

            if (Limit != null && !_limitEquippedOnly && total > Limit.Value)
            {
                total = Limit.Value;
            }

            return total;
        }
    }

    /// <summary>
    ///     TotalSafe can't be lesser than 0
    /// </summary>
    public virtual long TotalSafe
    {
        get
        {
            var total = Total;

            return total > 0 ? total : 0;
        }
    }

    public virtual long TotalWithoutContext => Total - Context;

    protected virtual long? Limit
    {
        get => _limit;
        set
        {
            _limit = value;
            OnModified();
        }
    }

    public event Action<StatsField, long>? Modified;

    protected virtual void OnModified()
    {
        var handler = Modified;
        handler?.Invoke(this, Total);
    }

    public static long operator +(long i1, StatsField s1) => i1 + s1.Total;

    public static long operator +(StatsField s1, StatsField s2) => s1.Total + s2.Total;

    public static long operator -(long i1, StatsField s1) => i1 - s1.Total;

    public static long operator -(StatsField s1, StatsField s2) => s1.Total - s2.Total;

    public static long operator *(long i1, StatsField s1) => i1 * s1.Total;

    public static long operator *(StatsField s1, StatsField s2) => s1.Total * s2.Total;

    public static double operator /(StatsField s1, double d1) => s1.Total / d1;

    public static double operator /(StatsField s1, StatsField s2) => s1.Total / (double)s2.Total;

    public override string ToString() => $"{Total}({Base}+{Additional}+{Equipped}+{Context})";

    public StatsField Clone()
    {
        return CloneAndChangeOwner(Owner);
    }

    public virtual StatsField CloneAndChangeOwner(IStatsOwner owner)
    {
        var clone = new StatsField(owner, Characteristic, Base, Limit, _limitEquippedOnly)
        {
            Additional = Additional,
            Context = Context,
            Equipped = Equipped,
            Given = Given,
        };

        return clone;
    }

    public virtual StatsField Reset(IStatsOwner owner)
    {
        var clone = new StatsField(owner, Characteristic, RealBase, Limit, _limitEquippedOnly, _formulas)
        {
            Additional = Additional,
            Context = 0,
            Equipped = Equipped,
            Given = Given,
        };

        return clone;
    }
}