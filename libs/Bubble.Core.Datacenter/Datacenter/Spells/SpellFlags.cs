namespace Bubble.Core.Datacenter.Datacenter.Spells;

[Flags]
public enum SpellFlags
{
    VerboseCast = 1,
    BypassSummoningLimit = 2,
    CanAlwaysTriggerSpells = 4,
    HideCastConditions = 8
}