using Bubble.DamageCalculation;
using Bubble.DamageCalculation.FighterManagement.FighterStats;

namespace BubbleBot.Cli.Services.Fight;

public class StatsFields
{
    private static readonly StatsFormulasHandler FormulasChanceDependant =
        owner => (int)(owner.Stats[StatId.Chance] / 10d);

    private static readonly StatsFormulasHandler FormulasWisdomDependant =
        owner => (int)(owner.Stats[StatId.Wisdom] / 10d);

    private static readonly StatsFormulasHandler FormulasAgilityDependant =
        owner => (int)(owner.Stats[StatId.Agility] / 10d);

    public delegate int StatsFormulasHandler(IStatsOwner target);
    
    private IStatsOwner Owner { get; }

    public IDictionary<StatId, StatsField> Fields { get; set; }

    
    public StatsFields(IStatsOwner owner)
    {
        Owner  = owner;
        Fields = new Dictionary<StatId, StatsField>();
    }
    
    public StatsFields(IStatsOwner owner, IDictionary<StatId, StatsField> fields)
    {
        Owner  = owner;
        Fields = fields;
    }

    public StatsField this[StatId id]
        => Fields.TryGetValue(id, out var value) ? value : throw new KeyNotFoundException();

    public StatsField LifePoints => this[StatId.LifePoints];
    public StatsField Vitality => this[StatId.Vitality];
    public StatsField CurPermanentDamage => this[StatId.CurPermanentDamage];

    public StatsField StatsPoints => this[StatId.StatsPoints];
    public StatsUsable Ap => (this[StatId.ActionPoints] as StatsUsable)!;
    public StatsUsable Mp => (this[StatId.MovementPoints] as StatsUsable)!;
    public StatsField CurLife => this[StatId.CurLife];

    public IStatsField? GetStat(int statId)
    {
        return Fields.TryGetValue((StatId)statId, out var stat) ? stat : null;
    }
    public IStatsField? GetStat(StatId statId)
    {
        return Fields.TryGetValue(statId, out var stat) ? stat : null;
    }
    

    public int GetStatTotalValue(StatId statId)
    {
        if (Fields.TryGetValue(statId, out var stat))
        {
            return (int)stat.Total;
        }

        return 0;
    }

    /// <summary>
    /// Calculates the maximum health points of a character based on their vitality and life points stats.
    /// </summary>
    /// <returns>
    /// A double representing the maximum health points of the character.
    /// </returns>
    public int GetMaxHealthPoints()
    {
        var vitalityStat = GetStat(StatId.Vitality);

        if (vitalityStat == null)
        {
            return 0;
        }

        var effectiveVitality = Math.Max(0, vitalityStat.Base + vitalityStat.Equipped + vitalityStat.Additional + vitalityStat.Given) + vitalityStat.Context;

        return (int)(GetStatTotalValue(StatId.LifePoints) + effectiveVitality - GetStatTotalValue(StatId.CurPermanentDamage));
    }

    /// <summary>
    /// Calculates the maximum health points of a character based on their vitality and life points stats.
    /// </summary>
    /// <returns>
    /// A double representing the maximum health points of the character.
    /// </returns>
    public int GetMaxHealthPointsBase()
    {
        var vitalityStat = GetStat(StatId.Vitality);

        if (vitalityStat == null)
        {
            return 0;
        }

        var effectiveVitality =
            Math.Max(0, vitalityStat.Base + vitalityStat.Equipped + vitalityStat.Additional + vitalityStat.Given);

        return (int)(GetStatTotalValue(StatId.LifePoints) + effectiveVitality);
    }   
    
    public int GetCharacteristicValue(int characteristicId)
    {
        return (int)(Fields.TryGetValue((StatId)characteristicId, out var stat) ? stat.Total : 0);
    }


    public int GetHealthPoints()
    {
        return GetMaxHealthPoints() + GetCharacteristicValue((int)StatId.CurLife) + GetCharacteristicValue((int)StatId.CurPermanentDamage);
    }

    public StatsFields Clone()
    {
        var fields = new Dictionary<StatId, StatsField>();

        foreach (var field in Fields)
        {
            fields[field.Key] = field.Value.Clone();
        }

        return new StatsFields(Owner, fields);
    }

    public void Reset()
    {
        foreach (var field in Fields.ToArray())
        {
            Fields[field.Key] = field.Value.Reset(Owner);
        }
    }
}