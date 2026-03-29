namespace Bubble.DamageCalculation;

public enum SpellModifierTypeEnum
{
    InvalidModification = 0,

    Rangeable = 1,

    Damage = 2,

    BaseDamage = 3,

    HealBonus = 4,

    ApCost = 5,

    CastInterval = 6,

    CriticalHitBonus = 7,

    CastLine = 8,

    Los = 9,

    MaxCastPerTurn = 10,

    MaxCastPerTarget = 11,

    RangeMax = 12,

    RangeMin = 13,

    OccupiedCell = 14,

    FreeCell = 15,

    VisibleTarget = 16,

    PortalFreeCell = 17,

    PortalProjection = 18
}
