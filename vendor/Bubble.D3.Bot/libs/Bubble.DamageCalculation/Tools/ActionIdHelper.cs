using Bubble.Core.Datacenter.Datacenter.Effects;


namespace Bubble.DamageCalculation.Tools;

public static class ActionIdHelper
{
    /// <summary>
    /// Determines if the action is based on the caster's life.
    /// </summary>
    /// <param name="actionId">The action ID to check.</param>
    /// <returns>Returns true if the action is based on the caster's life, otherwise false.</returns>
    public static bool IsBasedOnCasterLife(ActionId actionId)
    {
        return IsBasedOnCasterLifePercent(actionId) || IsBasedOnCasterLifeMidlife(actionId) ||
               IsBasedOnCasterLifeMissing(actionId) || IsBasedOnCasterLifeMissingMaxLife(actionId);
    }

    /// <summary>
    /// Determines whether the input integer parameter represents a caster life percent.
    /// </summary>
    /// <param name="actionId">The input actionId to be checked.</param>
    /// <returns>True if the input parameter is an action based of the caster life percent, false otherwise.</returns>
    public static bool IsBasedOnCasterLifePercent(ActionId actionId)
    {
        return actionId is ActionId.CharacterLifePointsLostBasedOnCasterLifeFromWater or
                           ActionId.CharacterLifePointsLostBasedOnCasterLifeFromEarth or
                           ActionId.CharacterLifePointsLostBasedOnCasterLifeFromAir or
                           ActionId.CharacterLifePointsLostBasedOnCasterLifeFromFire or
                           ActionId.CharacterLifePointsLostBasedOnCasterLife or
                           ActionId.CharacterDispatchLifePointsPercent or
                           ActionId.CharacterLifePointsLostBasedOnCasterLifeNotReduced;
    }

    /// <summary>
    /// Determines whether the input integer parameter represents a caster life missing value.
    /// </summary>
    /// <param name="actionId">The input actionId to be checked.</param>
    /// <returns>True if the input parameter is an action based of the caster missing life, false otherwise.</returns>
    public static bool IsBasedOnCasterLifeMissing(ActionId actionId)
    {
        return actionId is ActionId.CharacterLifePointsLostBasedOnCasterLifeMissingFromWater or
                           ActionId.CharacterLifePointsLostBasedOnCasterLifeMissingFromEarth or
                           ActionId.CharacterLifePointsLostBasedOnCasterLifeMissingFromAir or
                           ActionId.CharacterLifePointsLostBasedOnCasterLifeMissingFromFire or
                           ActionId.CharacterLifePointsLostBasedOnCasterLifeMissing;
    }

    /// <summary>
    /// Determines whether the input integer parameter represents a combination of caster life missing and maximum life values.
    /// </summary>
    /// <param name="actionId">The input actionId to be checked.</param>
    /// <returns>True if the input parameter is an action based of the caster missing max life, false otherwise.</returns>
    public static bool IsBasedOnCasterLifeMissingMaxLife(ActionId actionId)
    {
        return actionId is ActionId.CharacterLifePointsLostBasedOnCasterMissingMaxLife or
                           ActionId.CharacterLifePointsLostBasedOnCasterMissingMaxLifeAir or
                           ActionId.CharacterLifePointsLostBasedOnCasterMissingMaxLifeFire or
                           ActionId.CharacterLifePointsLostBasedOnCasterMissingMaxLifeWater or
                           ActionId.CharacterLifePointsLostBasedOnCasterMissingMaxLifeEarth;
    }

    /// <summary>
    /// Determines whether the input integer parameter represents a caster life midlife value.
    /// </summary>
    /// <param name="actionId">The input actionId to be checked.</param>
    /// <returns>True if the input parameter is an action based of the caster missing middle life, false otherwise.</returns>
    public static bool IsBasedOnCasterLifeMidlife(ActionId actionId)
    {
        return actionId == ActionId.CharacterLifePointsLostBasedOnCasterLifeMidlife;
    }

    /// <summary>
    /// Determines whether the input integer parameter represents a splash damage or splash heal value.
    /// </summary>
    /// <param name="actionId">The input actionId to be checked.</param>
    /// <returns>True if the input parameter is a valid splash damage or splash heal value, false otherwise.</returns>
    public static bool IsSplash(ActionId actionId)
    {
        if (!IsSplashDamage(actionId))
        {
            return IsSplashHeal(actionId);
        }

        return true;
    }

    /// <summary>
    /// Determines whether the input integer parameter represents a splash damage value.
    /// </summary>
    /// <param name="actionId">The input actionId to be checked.</param>
    /// <returns>True if the input parameter is a slash damage, false otherwise.</returns>
    public static bool IsSplashDamage(ActionId actionId)
    {
        return IsSplashFinalDamage(actionId) || IsSplashRawDamage(actionId);
    }

    /// <summary>
    /// Determines whether the input integer parameter represents a splash final damage value.
    /// </summary>
    /// <param name="actionId">The input actionId to be checked.</param>
    /// <returns>True if the input parameter is a final slash damage, false otherwise.</returns>
    public static bool IsSplashFinalDamage(ActionId actionId)
    {
        return actionId is ActionId.FightSplashFinalTakenDamage or
                           ActionId.FightSplashFinalTakenDamageNeutral or
                           ActionId.FightSplashFinalTakenDamageAir or
                           ActionId.FightSplashFinalTakenDamageFire or
                           ActionId.FightSplashFinalTakenDamageWater or
                           ActionId.FightSplashFinalTakenDamageEarth;
    }

    /// <summary>
    /// Determines whether the input integer parameter represents a splash raw damage value.
    /// </summary>
    /// <param name="actionId">The input actionId to be checked.</param>
    /// <returns>True if the input parameter is a raw slash damage, false otherwise.</returns>
    public static bool IsSplashRawDamage(ActionId actionId)
    {
        return actionId is ActionId.FightSplashRawTakenDamage or
                           ActionId.FightSplashRawTakenDamageNeutral or
                           ActionId.FightSplashRawTakenDamageAir or
                           ActionId.FightSplashRawTakenDamageFire or
                           ActionId.FightSplashRawTakenDamageWater or
                           ActionId.FightSplashRawTakenDamageEarth;
    }

    /// <summary>
    /// Determines if the action is a splash heal.
    /// </summary>
    /// <param name="actionId">The action ID to check.</param>
    /// <returns>Returns true if the action is a splash heal, otherwise false.</returns>
    public static bool IsSplashHeal(ActionId actionId)
    {
        return actionId is ActionId.FightSplashHeal or ActionId.FightCasterSplashHeal;
    }

    /// <summary>
    /// Determines whether the input action ID represents an action based on movement points.
    /// </summary>
    /// <param name="actionId">The input action ID to be checked.</param>
    /// <returns>True if the input parameter is one of the specified values (1012, 1013, 1014, 1015, 1016), false otherwise.</returns>
    public static bool IsBasedOnMovementPoints(ActionId actionId)
    {
        return actionId is ActionId.CharacterLifePointsLostBasedOnMovementPoints or
                           ActionId.CharacterLifePointsLostBasedOnMovementPointsFromAir or
                           ActionId.CharacterLifePointsLostBasedOnMovementPointsFromWater or
                           ActionId.CharacterLifePointsLostBasedOnMovementPointsFromFire or
                           ActionId.CharacterLifePointsLostBasedOnMovementPointsFromEarth;
    }
    
    /// <summary>
    /// Determines whether the input action ID represents an action based on action points.
    /// </summary>
    /// <param name="actionId">The input action ID to be checked.</param>
    /// <returns>True if the input parameter is one of the specified values (1012, 1013, 1014, 1015, 1016), false otherwise.</returns>
    public static bool IsBasedOnActionsPoints(ActionId actionId)
    {
        return actionId is ActionId.CharacterManaUseKillLifeAir or
                           ActionId.CharacterManaUseKillLifeWater or
                           ActionId.CharacterManaUseKillLifeFire or
                           ActionId.CharacterManaUseKillLifeEarth or 
                           ActionId.CharacterManaUseKillLifeNeutral;
    }
    
    /// <summary>
    /// Determines whether the input action ID represents an action based on action points.
    /// </summary>
    /// <param name="actionId">The input action ID to be checked.</param>
    /// <returns>True if the input parameter is one of the specified values (1012, 1013, 1014, 1015, 1016), false otherwise.</returns>
    public static bool IsBasedOnMovementPointsUsed(ActionId actionId)
    {
        return actionId is ActionId.CharacterMovementUseKillLifeAir or
                           ActionId.CharacterMovementUseKillLifeWater or
                           ActionId.CharacterMovementUseKillLifeFire or
                           ActionId.CharacterMovementUseKillLifeEarth or 
                           ActionId.CharacterMovementUseKillLifeNeutral;
    }

    /// <summary>
    /// Determines whether the input action ID represents an action based on target life percent.
    /// </summary>
    /// <param name="actionId">The input action ID to be checked.</param>
    /// <returns>True if the input parameter is one of the specified values (1048, 1067, 1068, 1069, 1070, 1071), false otherwise.</returns>
    public static bool IsBasedOnTargetLifePercent(ActionId actionId)
    {
        return actionId is ActionId.CharacterLifePointsMalusPercent or
                           ActionId.CharacterLifePointsLostBasedOnTargetLifeFromAir or
                           ActionId.CharacterLifePointsLostBasedOnTargetLifeFromWater or
                           ActionId.CharacterLifePointsLostBasedOnTargetLifeFromFire or
                           ActionId.CharacterLifePointsLostBasedOnTargetLifeFromEarth or
                           ActionId.CharacterLifePointsLostBasedOnTargetLife;
    }

    /// <summary>
    /// Determines whether the input action ID represents an action that affects the target's max life.
    /// </summary>
    /// <param name="actionId">The input action ID to be checked.</param>
    /// <returns>True if the input parameter is equal to 2845, false otherwise.</returns>
    public static bool IsTargetMaxLifeAffected(ActionId actionId)
    {
        return actionId is ActionId.CharacterLifePointsWinZobal or
                           ActionId.CharacterDeboostVitality or
                           ActionId.CharacterDeboostVitalityPercentStatic or
                           ActionId.CharacterBoostVitality or
                           ActionId.CharacterBoostVitalityPercentStatic or
                           ActionId.CharacterGainVitality or
                           ActionId.CharacterStealVitality or
                           ActionId.CharacterBoostVitalityPercent or
                           ActionId.CharacterDeboostVitalityPercent;
    }

    /// <summary>
    /// Determines whether the input action ID represents an action based on target life.
    /// </summary>
    /// <param name="actionId">The input action ID to be checked.</param>
    /// <returns>True if the input parameter satisfies any of the following conditions:
    /// 1. Is based on target life percent (see IsBasedOnTargetLifePercent function).
    /// 2. Is based on target max life (see IsBasedOnTargetMaxLife function).
    /// 3. Is based on target life missing max life (see IsBasedOnTargetLifeMissingMaxLife function).
    /// False otherwise.</returns>
    public static bool IsBasedOnTargetLife(ActionId actionId)
    {
        if (!IsBasedOnTargetLifePercent(actionId) && !IsBasedOnTargetMaxLife(actionId))
        {
            return IsBasedOnTargetLifeMissingMaxLife(actionId);
        }

        return true;
    }

    /// <summary>
    /// Determines whether the input action ID represents an action based on target max life.
    /// </summary>
    /// <param name="actionId">The input action ID to be checked.</param>
    /// <returns>True if the input parameter is equal to 1109, false otherwise.</returns>
    public static bool IsBasedOnTargetMaxLife(ActionId actionId)
    {
        return actionId == ActionId.FightLifePointsWinPercent;
    }

    /// <summary>
    /// Determines whether the input action ID represents an action based on target life missing max life.
    /// </summary>
    /// <param name="actionId">The input action ID to be checked.</param>
    /// <returns>True if the input parameter is one of the specified values (1092, 1093, 1094, 1095, 1096), false otherwise.</returns>
    public static bool IsBasedOnTargetLifeMissingMaxLife(ActionId actionId)
    {
        return actionId is ActionId.CharacterLifePointsLostBasedOnTargetMissingMaxLife or
                           ActionId.CharacterLifePointsLostBasedOnTargetMissingMaxLifeAir or
                           ActionId.CharacterLifePointsLostBasedOnTargetMissingMaxLifeFire or
                           ActionId.CharacterLifePointsLostBasedOnTargetMissingMaxLifeWater or
                           ActionId.CharacterLifePointsLostBasedOnTargetMissingMaxLifeEarth;
    }

    /// <summary>
    /// Determines whether the input actionId can be boosted.
    /// </summary>
    /// <param name="actionId">The input actionId to be checked.</param>
    /// <returns>True if the input actionId is boostable, false otherwise.</returns>
    public static bool IsBoostable(ActionId actionId)
    {
        if (actionId is ActionId.CharacterLifePointsLostFromPush or
                        ActionId.CharacterLifePointsStealWithoutBoost or
                        ActionId.CharacterLifePointsLostNoBoost or
                        ActionId.CharacterLifePointsLostNoBoostFromEarth or
                        ActionId.CharacterLifePointsLostNoBoostFromAir or
                        ActionId.CharacterLifePointsLostNoBoostFromWater or
                        ActionId.CharacterLifePointsLostNoBoostFromFire)
        {
            return false;
        }

        var isBasedOnCasterLife = IsBasedOnCasterLife(actionId);
        var isBasedOnTargetLife = IsBasedOnTargetLife(actionId);
        var isSplash            = IsSplash(actionId);

        return !isBasedOnCasterLife && !isBasedOnTargetLife && !isSplash;
    }

    /// <summary>
    /// Determines whether the input actionId is a life steal effect.
    /// </summary>
    /// <param name="actionId">The input actionId to be checked.</param>
    /// <returns>True if the input actionId is a life steal effect, false otherwise.</returns>
    public static bool IsLifeSteal(ActionId actionId)
    {
        return actionId is ActionId.CharacterLifePointsSteal or
                           ActionId.CharacterLifePointsStealFromBestElement or
                           ActionId.CharacterLifePointsStealFromWorstElement or
                           ActionId.CharacterLifePointsStealWithoutBoost or
                           ActionId.CharacterLifePointsStealFromEarth or
                           ActionId.CharacterLifePointsStealFromFire or
                           ActionId.CharacterLifePointsStealFromWater or
                           ActionId.CharacterLifePointsStealFromAir;
    }

    /// <summary>
    /// Determines whether the input integer parameter represents a heal action.
    /// </summary>
    /// <param name="actionId">The input actionId to be checked.</param>
    /// <returns>True if the input parameter is one of the specified values, false otherwise.</returns>
    public static bool IsHeal(ActionId actionId)
    {
        return actionId is ActionId.CharacterLifePointsWinWithoutElement or
                           ActionId.CharacterDispatchLifePointsPercent or
                           ActionId.CharacterLifePointsWinFromFire or
                           ActionId.CharacterLifePointsWinWithoutBoost or
                           ActionId.CharacterLifePointsWinNoBoost or
                           ActionId.CharacterHealAttackers or
                           ActionId.CharacterLifePointsWinZobal or
                           ActionId.FightLifePointsWinPercent or
                           ActionId.FightSplashHeal or
                           ActionId.FightCasterSplashHeal or
                           ActionId.CharacterLifePointsWinFromWater or
                           ActionId.CharacterLifePointsWinFromEarth or
                           ActionId.CharacterLifePointsWinFromAir or
                           ActionId.CharacterLifePointsWinFromNeutral or
                           ActionId.CharacterLifePointsWinFromBestElement;
    }

    /// <summary>
    /// Determines whether the input integer parameter represents a shield action.
    /// </summary>
    /// <param name="actionId">The input actionId to be checked.</param>
    /// <returns>True if the input parameter is one of the specified values, false otherwise.</returns>
    public static bool IsShield(ActionId actionId)
    {
        return actionId is ActionId.CharacterBoostShieldBasedOnCasterLevel or
                           ActionId.CharacterBoostShieldBasedOnCasterLife or
                           ActionId.CharacterBoostShield;
    }

    /// <summary>
    /// Determines whether the input integer parameter represents a heal bonus action.
    /// </summary>
    /// <param name="actionId">The input actionId to be checked.</param>
    /// <returns>True if the input parameter is one of the specified values, false otherwise.</returns>
    public static bool IsHealBonus(ActionId actionId)
    {
        return actionId is ActionId.CharacterBoostVitalityPercentStatic or
                           ActionId.CharacterBoostVitalityPercent;
    }
    /// <summary>
    /// Determines whether the input integer parameter represents a deboost heal bonus action.
    /// </summary>
    /// <param name="actionId">The input actionId to be checked.</param>
    /// <returns>True if the input parameter is one of the specified values, false otherwise.</returns>
    public static bool IsHealMalus(ActionId actionId)
    {
        return actionId is ActionId.CharacterDeboostVitalityPercentStatic or
                           ActionId.CharacterDeboostVitalityPercent;
    }

    /// <summary>
    /// Determines whether the input integer parameter represents a target mark dispel action.
    /// </summary>
    /// <param name="actionId">The input actionId to be checked.</param>
    /// <returns>True if the input parameter is one of the specified values, false otherwise.</returns>
    public static bool IsTargetMarkDispell(ActionId actionId)
    {
        return actionId is ActionId.DispelGlyphsOfTarget or ActionId.DispelTrapsOfTarget
                                                               or ActionId.DispelRunesOfTarget;
    }

    /// <summary>
    /// Determines whether the input integer parameter represents a target mark dispel action.
    /// </summary>
    /// <param name="actionId">The input actionId to be checked.</param>
    /// <returns>True if the input parameter is one of the specified values, false otherwise.</returns>
    public static bool IsMark(ActionId actionId)
    {
        return actionId is ActionId.FightAddGlyphAura or ActionId.FightAddGlyphCastingSpell
                                                            or ActionId.FightAddGlyphCastingSpellEndturn
                                                            or ActionId.FightAddGlyphCastingSpellImmediate
                                                            or ActionId.ForceRuneTrigger
                                                            or ActionId.FightAddTrapCastingSpell;
    }

    /// <summary>
    /// Determines whether the input integer parameter represents a shield action.
    /// </summary>
    /// <param name="actionId">The input actionId to be checked.</param>
    /// <returns>True if the input parameter is one of the specified values, false otherwise.</returns>
    public static bool IsDodgeable(ActionId actionId)
    {
        return actionId is ActionId.CharacterDeboostActionPointsDodgeable or
                           ActionId.CharacterDeboostMovementPointsDodgeable;
    }

    /// <summary>
    /// Determines whether the input integer parameter represents a shield action.
    /// </summary>
    /// <param name="actionId">The input actionId to be checked.</param>
    /// <returns>True if the input parameter is one of the specified values, false otherwise.</returns>
    public static int DodgeableToStatId(ActionId actionId)
    {
        return actionId switch
               {
                   ActionId.CharacterDeboostActionPointsDodgeable   => (int)StatId.DodgePaLostProbability,
                   ActionId.CharacterDeboostMovementPointsDodgeable => (int)StatId.DodgePmLostProbability,
                   _                                                      => -1,
               };
    }

    /// <summary>
    /// Determines whether the input integer parameter represents a stat boost action.
    /// </summary>
    /// <param name="actionId">The input actionId to be checked.</param>
    /// <returns>True if the input parameter is one of the specified values, false otherwise.</returns>
    public static bool IsStatGain(ActionId actionId)
    {
        return actionId is ActionId.CharacterActionPointsWin or
                           ActionId.CharacterMovementPointsWin;
    }

    /// <summary>
    /// Determines whether the input integer parameter represents a stat boost action.
    /// </summary>
    /// <param name="actionId">The input actionId to be checked.</param>
    /// <returns>True if the input parameter is one of the specified values, false otherwise.</returns>
    public static StatId StatGainToStatId(ActionId actionId)
    {
        return actionId switch
               {
                   ActionId.CharacterActionPointsWin   => StatId.ActionPoints,
                   ActionId.CharacterMovementPointsWin => StatId.MovementPoints,
                   _                                         => StatId.Unknown,
               };
    }

    /// <summary>
    /// Determines whether the input integer parameter represents a stat boost action.
    /// </summary>
    /// <param name="actionId">The input actionId to be checked.</param>
    /// <returns>True if the input parameter is one of the specified values, false otherwise.</returns>
    public static bool IsStatBoost(ActionId actionId)
    {
        return StatBoostToBuffActionId(actionId) != 0 || StatBoostToDebuffActionId(actionId) != 0;
    }

    /// <summary>
    /// Returns the name of the stat boosted by the input action.
    /// </summary>
    /// <param name="actionId">The input actionId to get the name of the boosted stat from.</param>
    /// <returns>The name of the stat boosted by the input action, or null if the action does not boost any stat.</returns>
    public static string? StatBoostToStatName(ActionId actionId)
    {
        return actionId switch
               {
                   ActionId.CharacterStealChance       => "chance",
                   ActionId.CharacterStealAgility      => "agility",
                   ActionId.CharacterStealIntelligence => "intelligence",
                   ActionId.CharacterStealWisdom       => "wisdom",
                   ActionId.CharacterStealStrength     => "strength",
                   _                                         => null,
               };
    }

    /// <summary>
    /// Returns the buff actionId associated with the input stat boost actionId.
    /// </summary>
    /// <param name="actionId">The input actionId to get the associated buff actionId from.</param>
    /// <returns>The buff actionId associated with the input stat boost actionId, or 0 if the action does not have an associated buff actionId.</returns>
    public static ActionId StatBoostToBuffActionId(ActionId actionId)
    {
        return actionId switch
               {
                   ActionId.CharacterStealChance       => ActionId.CharacterBoostChance,
                   ActionId.CharacterStealAgility      => ActionId.CharacterBoostAgility,
                   ActionId.CharacterStealIntelligence => ActionId.CharacterBoostIntelligence,
                   ActionId.CharacterStealWisdom       => ActionId.CharacterBoostWisdom,
                   ActionId.CharacterStealStrength     => ActionId.CharacterBoostStrength,

                   // custom
                   ActionId.CharacterStealRange          => ActionId.CharacterBoostRange,
                   ActionId.CharacterActionPointsSteal   => ActionId.CharacterBoostActionPoints,
                   ActionId.CharacterMovementPointsSteal => ActionId.CharacterBoostMovementPoints,
                   _                                           => 0,
               };
    }

    /// <summary>
    /// Returns the debuff action ID for a given stat boost action ID.
    /// </summary>
    /// <param name="actionId">The action ID representing the stat boost.</param>
    /// <returns>The action ID of the corresponding stat debuff, or -1 if the input is not a valid stat boost action ID.</returns>
    public static ActionId StatBoostToDebuffActionId(ActionId actionId)
    {
        return actionId switch
               {
                   ActionId.CharacterStealChance       => ActionId.CharacterDeboostChance,
                   ActionId.CharacterStealAgility      => ActionId.CharacterDeboostAgility,
                   ActionId.CharacterStealIntelligence => ActionId.CharacterDeboostIntelligence,
                   ActionId.CharacterStealWisdom       => ActionId.CharacterDeboostWisdom,
                   ActionId.CharacterStealStrength     => ActionId.CharacterDeboostStrength,

                   // custom
                   ActionId.CharacterStealRange        => ActionId.CharacterDeboostRange,
                   ActionId.CharacterActionPointsSteal => ActionId.CharacterDeboostActionPointsDodgeable,
                   ActionId.CharacterMovementPointsSteal =>
                       ActionId.CharacterDeboostMovementPointsDodgeable,
                   _ => 0,
               };
    }

    /// <summary>
    /// Determines whether the input parameters represent a damage action.
    /// </summary>
    /// <param name="categoryId">The category ID of the action.</param>
    /// <param name="actionId">The action ID to check.</param>
    /// <returns>True if the input parameters represent a damage action, false otherwise.</returns>
    public static bool IsDamage(int categoryId, ActionId actionId)
    {
        return categoryId == 2 &&
               actionId != ActionId.CharacterMovementPointsLost &&
               actionId != ActionId.CharacterActionPointsLost;
    }

    /// <summary>
    /// Checks if the action is a push effect.
    /// </summary>
    /// <param name="actionId">The id of the action to check.</param>
    /// <returns>True if the action is a push effect, false otherwise.</returns>
    public static bool IsPush(ActionId actionId)
    {
        switch (actionId)
        {
            case ActionId.CharacterPush:
            case ActionId.CharacterPushForce:
            case ActionId.CharacterGetPushed:
            case ActionId.FightPushNoDamage:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Checks if the action is a pull effect.
    /// </summary>
    /// <param name="actionId">The id of the action to check.</param>
    /// <returns>True if the action is a pull effect, false otherwise.</returns>
    public static bool IsPull(ActionId actionId)
    {
        switch (actionId)
        {
            case ActionId.CharacterPull:
            case ActionId.CharacterPullForce:
            case ActionId.CharacterGetPulled:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Checks if the action is a forced drag effect.
    /// </summary>
    /// <param name="actionId">The id of the action to check.</param>
    /// <returns>True if the action is a forced drag effect, false otherwise.</returns>
    public static bool IsForcedDrag(ActionId actionId)
    {
        switch (actionId)
        {
            case ActionId.CharacterPushForce:
            case ActionId.CharacterPullForce:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Checks if the action is a drag effect.
    /// </summary>
    /// <param name="actionId">The id of the action to check.</param>
    /// <returns>True if the action is a drag effect, false otherwise.</returns>
    public static bool IsDrag(ActionId actionId)
    {
        return IsPush(actionId) || IsPull(actionId);
    }

    /// <summary>
    /// Check if the action allows collision damage or not.
    /// </summary>
    /// <param name="actionId">The action ID to check.</param>
    /// <returns>True if the action allows collision damage, false otherwise.</returns>
    public static bool AllowCollisionDamage(ActionId actionId)
    {
        switch (actionId)
        {
            case ActionId.CharacterPush:
            case ActionId.CharacterGetPushed:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Check if the action is a summon or not.
    /// </summary>
    /// <param name="actionId">The action ID to check.</param>
    /// <returns>True if the action is a summon, false otherwise.</returns>
    public static bool IsSummon(ActionId actionId)
    {
        var isSummonWithSlot = IsSummonWithSlot(actionId);
        switch (actionId)
        {
            case ActionId.SummonCreature:
                return true;
            case ActionId.CharacterSummonDeadAllyInFight:
            case ActionId.SummonBomb:
            case ActionId.CharacterAddIllusionMirror:
            case ActionId.CharacterAddDoubleNoSummonSlot:
                return true;
            default:
                return isSummonWithSlot;
        }
    }

    /// <summary>
    /// Check if the action is a summon with a slot or not.
    /// </summary>
    /// <param name="actionId">The action ID to check.</param>
    /// <returns>True if the action is a summon with a slot, false otherwise.</returns>
    public static bool IsSummonWithSlot(ActionId actionId)
    {
        switch (actionId)
        {
            case ActionId.CharacterAddDoubleUseSummonSlot:
            case ActionId.FightKillAndSummon:
            case ActionId.SummonSlave:
            case ActionId.CharacterSummonDeadAllyAsSummonInFight:
            case ActionId.FightKillAndSummonSlave:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Checks if the given action id corresponds to a summon that does not require a target.
    /// </summary>
    /// <param name="actionId">The action id to check.</param>
    /// <returns>Returns true if the action id corresponds to a summon that does not require a target, otherwise returns false.</returns>
    public static bool IsSummonWithoutTarget(ActionId actionId)
    {
        switch (actionId)
        {
            case ActionId.CharacterAddDoubleUseSummonSlot:
            case ActionId.SummonCreature:
            case ActionId.CharacterSummonDeadAllyInFight:
            case ActionId.SummonBomb:
            case ActionId.SummonSlave:
            case ActionId.CharacterSummonDeadAllyAsSummonInFight:
            case ActionId.CharacterAddIllusionMirror:
            case ActionId.CharacterAddDoubleNoSummonSlot:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Check if the given action id represents a "kill and summon" action.
    /// </summary>
    /// <param name="actionId">The action id to check.</param>
    /// <returns>True if the action id represents a "kill and summon" action, false otherwise.</returns>
    public static bool IsKillAndSummon(ActionId actionId)
    {
        return actionId is ActionId.FightKillAndSummonSlave or ActionId.FightKillAndSummon;
    }
    
    public static bool IsKill(ActionId actionId)
    {
        return actionId is ActionId.CharacterKill;
    }
    
    /// <summary>
    /// Check if the given action id represents a "revive" action.
    /// </summary>
    /// <param name="actionId">The action id to check.</param>
    /// <returns>True if the action id represents a "revive" action, false otherwise.</returns>
    public static bool IsRevive(ActionId actionId)
    {
        return actionId is ActionId.CharacterSummonDeadAllyInFight
                           or ActionId.CharacterSummonDeadAllyAsSummonInFight;
    }

    /// <summary>
    /// Get the final taken damage element for a splash attack.
    /// </summary>
    /// <param name="elementId">The splash attack element ID.</param>
    /// <returns>The final taken damage element for the splash attack.</returns>
    public static ActionId GetSplashFinalTakenDamageElement(int elementId)
    {
        return elementId switch
               {
                   0 => ActionId.FightSplashFinalTakenDamageNeutral,
                   1 => ActionId.FightSplashFinalTakenDamageEarth,
                   2 => ActionId.FightSplashFinalTakenDamageFire,
                   3 => ActionId.FightSplashFinalTakenDamageWater,
                   4 => ActionId.FightSplashFinalTakenDamageAir,
                   _ => ActionId.FightSplashFinalTakenDamage,
               };
    }

    /// <summary>
    /// Get the raw taken damage element for a splash attack.
    /// </summary>
    /// <param name="elementId">The splash attack element ID.</param>
    /// <returns>The raw taken damage element for the splash attack.</returns>
    public static ActionId GetSplashRawTakenDamageElement(int elementId)
    {
        return elementId switch
               {
                   0 => ActionId.FightSplashRawTakenDamageNeutral,
                   1 => ActionId.FightSplashRawTakenDamageEarth,
                   2 => ActionId.FightSplashRawTakenDamageFire,
                   3 => ActionId.FightSplashRawTakenDamageWater,
                   4 => ActionId.FightSplashRawTakenDamageAir,
                   _ => ActionId.FightSplashRawTakenDamage,
               };
    }

    /// <summary>
    /// Check if an action ID represents a fake damage attack.
    /// </summary>
    /// <param name="actionId">The action ID to check.</param>
    /// <returns>True if the action ID represents a fake damage attack, false otherwise.</returns>
    public static bool IsFakeDamage(ActionId actionId)
    {
        return actionId is ActionId.CharacterDispatchLifePointsPercent or
                           ActionId.CharacterLifePointsMalus or
                           ActionId.CharacterLifePointsMalusPercent;
    }

    /// <summary>
    /// Checks if the given action id represents a spell execution action.
    /// </summary>
    /// <param name="actionId">The action id to check.</param>
    /// <returns>True if the action id represents a spell execution action, false otherwise.</returns>
    public static bool IsSpellExecution(ActionId actionId)
    {
        return actionId is ActionId.CasterExecuteSpell or
                           ActionId.CasterExecuteSpellOnCell or
                           ActionId.CasterExecuteSpellGlobalLimitation or
                           ActionId.SourceExecuteSpellOnSource or
                           ActionId.SourceExecuteSpellOnTarget or
                           ActionId.TargetExecuteSpell or
                           ActionId.TargetExecuteSpellGlobalLimitation or
                           ActionId.TargetExecuteSpellOnCell or
                           ActionId.TargetExecuteSpellOnCellGlobalLimitation or
                           ActionId.TargetExecuteSpellOnSource or
                           ActionId.TargetExecuteSpellOnSourceGlobalLimitation or
                           ActionId.TargetExecuteSpellWithAnimation or
                           ActionId.TargetExecuteSpellWithAnimationGlobalLimitation;
    }

    /// <summary>
    /// Checks if the given action ID corresponds to a teleport action.
    /// </summary>
    /// <param name="actionId">The action ID to check.</param>
    /// <returns>Returns true if the action ID corresponds to a teleport action, otherwise false.</returns>
    public static bool IsTeleport(ActionId actionId)
    {
        return actionId is ActionId.CharacterTeleportOnSameMap or
                           ActionId.FightRollbackTurnBeginPosition or
                           ActionId.FightRollbackPreviousPosition or
                           ActionId.FightTeleswap or
                           ActionId.FightTeleswapMirror or
                           ActionId.FightTeleswapMirrorCaster or
                           ActionId.FightTeleswapMirrorImpactPoint or 
                           ActionId.CharacterTeleportToFightStartPos
               || IsExchange(actionId);
    }

    /// <summary>
    /// Checks if the given action ID corresponds to an exchange action.
    /// </summary>
    /// <param name="actionId">The action ID to check.</param>
    /// <returns>Returns true if the action ID corresponds to an exchange action, otherwise false.</returns>
    public static bool IsExchange(ActionId actionId)
    {
        return actionId is ActionId.CharacterExchangePlaces or ActionId.CharacterExchangePlacesForce;
    }

    /// <summary>
    /// Checks if the given action ID allows teleporting over breed switch positions.
    /// </summary>
    /// <param name="actionId">The action ID to check.</param>
    /// <returns>Returns true if the action ID allows teleporting over breed switch positions, otherwise false.</returns>
    public static bool CanTeleportOverBreedSwitchPos(ActionId actionId)
    {
        return actionId is ActionId.CharacterTeleportOnSameMap or ActionId.CharacterExchangePlacesForce;
    }

    /// <summary>
    /// Determines if the given action ID allows an AoE malus.
    /// </summary>
    /// <param name="actionId">The action ID to check.</param>
    /// <returns>Returns true if the action ID allows an AoE malus, otherwise false.</returns>
    public static bool AllowAoeMalus(ActionId actionId)
    {
        return !IsSplash(actionId) && !IsShield(actionId);
    }

    /// <summary>
    /// Checks if a dealt heal multiplier is applicable for the given action ID.
    /// </summary>
    /// <param name="actionId">The action ID to check.</param>
    /// <returns>Returns true if the dealt heal multiplier is applicable, otherwise false.</returns>
    public static bool IsDealtHealMultiplierAppliable(ActionId actionId)
    {
        return actionId != ActionId.CharacterDispatchLifePointsPercent &&
               actionId != ActionId.CharacterLifePointsWinNoBoost &&
               actionId != ActionId.FightLifePointsWinPercent &&
               actionId != ActionId.FightSplashHeal &&
               actionId != ActionId.FightCasterSplashHeal;
    }

    /// <summary>
    /// Determines if the given action ID corresponds to a portal bonus.
    /// </summary>
    /// <param name="actionId">The action ID to check.</param>
    /// <returns>Returns true if the action ID corresponds to a portal bonus, otherwise false.</returns>
    public static bool IsPortalBonus(ActionId actionId)
    {
        return actionId != ActionId.FightSplashHeal && actionId != ActionId.FightCasterSplashHeal;
    }

    /// <summary>
    /// Checks if the given action ID can trigger a damage multiplier.
    /// </summary>
    /// <param name="actionId">The action ID to check.</param>
    /// <returns>Returns true if the action ID can trigger a damage multiplier, otherwise false.</returns>
    public static bool CanTriggerDamageMultiplier(ActionId actionId)
    {
        return actionId != ActionId.CharacterDispatchLifePointsPercent;
    }

    /// <summary>
    /// Checks if the given action ID can trigger on damage.
    /// </summary>
    /// <param name="actionId">The action ID to check.</param>
    /// <returns>Returns true if the action ID can trigger on damage, otherwise false.</returns>
    public static bool CanTriggerOnDamage(ActionId actionId)
    {
        //return actionId != ActionId.CharacterLifePointsMalusPercent;
        return true;
    }

    /// <summary>
    /// Converts a stat to its corresponding buff percentage action ID.
    /// </summary>
    /// <param name="statId">The stat ID to convert.</param>
    /// <returns>Returns the corresponding buff percentage action ID or -1 if no match found.</returns>
    public static ActionId StatToBuffPercentActionIds(int statId)
    {
        return statId switch
               {
                   1  => ActionId.CharacterBoostActionPointsPercent,
                   10 => ActionId.CharacterBoostStrengthPercent,
                   11 => ActionId.CharacterBoostVitalityPercent,
                   12 => ActionId.CharacterBoostWisdomPercent,
                   13 => ActionId.CharacterBoostChancePercent,
                   14 => ActionId.CharacterBoostAgilityPercent,
                   15 => ActionId.CharacterBoostIntelligencePercent,
                   23 => ActionId.CharacterBoostMovementPointsPercent,
                   _  => ActionId.InvalidAction,
               };
    }

    /// <summary>
    /// Converts a stat to its corresponding debuff percentage action ID.
    /// </summary>
    /// <param name="statId">The stat ID to convert.</param>
    /// <returns>Returns the corresponding debuff percentage action ID or -1 if no match found.</returns>
    public static int StatToDebuffPercentActionIds(int statId)
    {
        return statId switch
               {
                   1   => 2847,
                   10  => 2835,
                   11  => 2845,
                   12  => 2843,
                   13  => 2841,
                   14  => 2837,
                   15  => 2839,
                   23  => 2849,
                   143 => 2972,
                   _   => -1,
               };
    }

    /// <summary>
    /// Determines if the given action ID is a linear buff stat ID.
    /// </summary>
    /// <param name="statId">The stat ID to check.</param>
    /// <returns>Returns false if the stat ID is a linear buff action ID, otherwise true.</returns>
    public static bool IsLinearBuffActionIds(int statId)
    {
        switch (statId)
        {
            case 31:
            case 33:
            case 34:
            case 35:
            case 36:
            case 37:
            case 59:
            case 60:
            case 61:
            case 62:
            case 63:
            case 69:
            case 101:
            case 121:
            case 124:
            case 141:
            case 142:
                return false;
            default:
                return true;
        }
    }

    /// <summary>
    /// Determines if the given action ID is a stat modifier.
    /// </summary>
    /// <param name="actionId">The action ID to check.</param>
    /// <returns>Returns true if the action ID is a stat modifier, otherwise false.</returns>
    public static bool IsStatModifier(ActionId actionId)
    {
        return ActionConstants.StatBuffActionIds.Contains(actionId) ||
               ActionConstants.StatDebuffActionIds.Contains(actionId)
            /*&& !IsShield(actionId)*/;
    }

    /// <summary>
    /// Determines if the given action ID is a buff.
    /// </summary>
    /// <param name="actionId">The action ID to check.</param>
    /// <returns>Returns true if the action ID is a buff, otherwise false.</returns>
    public static bool IsBuff(ActionId actionId)
    {
        return ActionConstants.StatBuffActionIds.Contains(actionId);
    }

    /// <summary>
    /// Determines if the given action ID is a debuff.
    /// </summary>
    /// <param name="actionId">The action ID to check.</param>
    /// <returns>Returns true if the action ID is a debuff, otherwise false.</returns>
    public static bool IsDebuff(ActionId actionId)
    {
        return ActionConstants.StatDebuffActionIds.Contains(actionId);
    }

    /// <summary>
    /// Determines if the given action ID is a percent stat boost action ID.
    /// </summary>
    /// <param name="actionId">The action ID to check.</param>
    /// <returns>Returns true if the action ID is a percent stat boost action ID, otherwise false.</returns>
    public static bool IsPercentStatBoostActionId(ActionId actionId)
    {
        return ActionConstants.PercentStatBoostActionIdToStat.ContainsKey(actionId);
    }

    /// <summary>
    /// Determines if the given action ID is a flat stat boost action ID.
    /// </summary>
    /// <param name="actionId">The action ID to check.</param>
    /// <returns>Returns true if the action ID is a flat stat boost action ID, otherwise false.</returns>
    public static bool IsFlatStatBoostActionId(ActionId actionId)
    {
        return ActionConstants.FlatStatBoostActionIdToStat.ContainsKey(actionId);
    }

    /// <summary>
    /// Retrieves the stat ID from the given stat action ID.
    /// </summary>
    /// <param name="actionId">The stat action ID to look up.</param>
    /// <returns>Returns the stat ID associated with the stat action ID or -1 if no match found.</returns>
    public static int GetStatIdFromStatActionId(ActionId actionId)
    {
        if (IsFlatStatBoostActionId(actionId))
        {
            return ActionConstants.FlatStatBoostActionIdToStat[actionId];
        }

        if (IsPercentStatBoostActionId(actionId))
        {
            return ActionConstants.PercentStatBoostActionIdToStat[actionId];
        }

        if (ActionConstants.ShieldActionIdToStatId.TryGetValue(actionId, out var value))
        {
            return value;
        }

        if (ActionConstants.HealBonusActionIdToStatId.TryGetValue(actionId, out var statHealValue))
        {
            return statHealValue;
        }

        return -1;
    }

    public static bool IsSpellModificationBoost(ActionId actionId)
    {
        return actionId switch
               {
                   ActionId.BoostSpellRangeable         => true,
                   ActionId.BoostSpellDmg               => true,
                   ActionId.BoostSpellBaseDmg           => true,
                   ActionId.BoostSpellHeal              => true,
                   ActionId.BoostSpellApCost            => true,
                   ActionId.BoostSpellCastIntvl         => true,
                   ActionId.BoostSpellCastIntvlSet      => true,
                   ActionId.BoostSpellCastoutline       => true,
                   ActionId.BoostSpellNolineofsight     => true,
                   ActionId.BoostSpellMaxperturn        => true,
                   ActionId.BoostSpellMaxpertarget      => true,
                   ActionId.BoostSpellRangeMax          => true,
                   ActionId.BoostSpellRangeMin          => true,
                   ActionId.BoostFreeCell               => true,
                   ActionId.BoostOccupiedCell           => true,
                   ActionId.SetSpellRangeMax            => true,
                   ActionId.SetSpellRangeMin            => true,
                   ActionId.BoostVisibleTargetOnCellOn  => true,
                   ActionId.BoostVisibleTargetOnCellOff => true,
                   ActionId.BoostPortalProjectionOn     => true,
                   ActionId.BoostPortalProjectionOff    => true,
                   ActionId.DeboostSpellCriticalHit     => false,
                   ActionId.DeboostSpellApCost          => false,
                   ActionId.DeboostSpellRangeMax        => false,
                   ActionId.DeboostOccupiedCell         => false,
                   ActionId.DeboostFreeCell             => false,
                   ActionId.DeboostSpellRangeMin        => false,
                   _                                          => false,
               };
    }

    /// <summary>
    /// Retrieves the spell modification ID from the given stat action ID.
    /// </summary>
    /// <param name="actionId">The stat action ID to look up.</param>
    /// <returns>Returns the stat ID associated with the stat action ID or -1 if no match found.</returns>
    public static int GetSpellModificationIdFromActionId(ActionId actionId)
    {
        return actionId switch
               {
                   ActionId.BoostSpellRangeable         => (int)SpellModifierTypeEnum.Rangeable,
                   ActionId.BoostSpellDmg               => (int)SpellModifierTypeEnum.Damage,
                   ActionId.BoostSpellBaseDmg           => (int)SpellModifierTypeEnum.BaseDamage,
                   ActionId.BoostSpellHeal              => (int)SpellModifierTypeEnum.HealBonus,
                   ActionId.BoostSpellApCost            => (int)SpellModifierTypeEnum.ApCost,
                   ActionId.DeboostSpellApCost          => (int)SpellModifierTypeEnum.ApCost,
                   ActionId.BoostSpellCastIntvl         => (int)SpellModifierTypeEnum.CastInterval,
                   ActionId.BoostSpellCastIntvlSet      => (int)SpellModifierTypeEnum.CastInterval,
                   ActionId.DeboostSpellCriticalHit     => (int)SpellModifierTypeEnum.CriticalHitBonus,
                   ActionId.BoostSpellCastoutline       => (int)SpellModifierTypeEnum.CastLine,
                   ActionId.BoostSpellNolineofsight     => (int)SpellModifierTypeEnum.Los,
                   ActionId.BoostSpellMaxperturn        => (int)SpellModifierTypeEnum.MaxCastPerTurn,
                   ActionId.BoostSpellMaxpertarget      => (int)SpellModifierTypeEnum.MaxCastPerTarget,
                   ActionId.BoostSpellRangeMax          => (int)SpellModifierTypeEnum.RangeMax,
                   ActionId.DeboostSpellRangeMax        => (int)SpellModifierTypeEnum.RangeMax,
                   ActionId.BoostSpellRangeMin          => (int)SpellModifierTypeEnum.RangeMin,
                   ActionId.DeboostSpellRangeMin        => (int)SpellModifierTypeEnum.RangeMin,
                   ActionId.BoostFreeCell               => (int)SpellModifierTypeEnum.FreeCell,
                   ActionId.DeboostFreeCell             => (int)SpellModifierTypeEnum.FreeCell,
                   ActionId.BoostOccupiedCell           => (int)SpellModifierTypeEnum.OccupiedCell,
                   ActionId.DeboostOccupiedCell         => (int)SpellModifierTypeEnum.OccupiedCell,
                   ActionId.SetSpellRangeMax            => (int)SpellModifierTypeEnum.RangeMax,
                   ActionId.SetSpellRangeMin            => (int)SpellModifierTypeEnum.RangeMin,
                   ActionId.BoostVisibleTargetOnCellOn  => (int)SpellModifierTypeEnum.VisibleTarget,
                   ActionId.BoostVisibleTargetOnCellOff => (int)SpellModifierTypeEnum.VisibleTarget,
                   ActionId.BoostPortalProjectionOn     => (int)SpellModifierTypeEnum.PortalProjection,
                   ActionId.BoostPortalProjectionOff    => (int)SpellModifierTypeEnum.PortalProjection,
                   ActionId.BoostSpellCc                => (int)SpellModifierTypeEnum.CriticalHitBonus,
                   _                                          => -1,
               };
    }
    
    public static SpellModifierActionTypeEnum GetSpellModifierActionType(ActionId actionId)
    {
        return actionId switch
               {
                   ActionId.BoostSpellRangeable         => SpellModifierActionTypeEnum.ActionBoost,
                   ActionId.BoostSpellDmg               => SpellModifierActionTypeEnum.ActionBoost,
                   ActionId.BoostSpellBaseDmg           => SpellModifierActionTypeEnum.ActionBoost,
                   ActionId.BoostSpellHeal              => SpellModifierActionTypeEnum.ActionBoost,
                   ActionId.BoostSpellApCost            => SpellModifierActionTypeEnum.ActionBoost,
                   ActionId.BoostSpellCastIntvl         => SpellModifierActionTypeEnum.ActionBoost,
                   ActionId.BoostSpellCastIntvlSet      => SpellModifierActionTypeEnum.ActionSet,
                   ActionId.BoostSpellCastoutline       => SpellModifierActionTypeEnum.ActionSet,
                   ActionId.BoostSpellNolineofsight     => SpellModifierActionTypeEnum.ActionSet,
                   ActionId.BoostSpellMaxperturn        => SpellModifierActionTypeEnum.ActionBoost,
                   ActionId.BoostSpellMaxpertarget      => SpellModifierActionTypeEnum.ActionBoost,
                   ActionId.BoostSpellRangeMax          => SpellModifierActionTypeEnum.ActionBoost,
                   ActionId.BoostSpellRangeMin          => SpellModifierActionTypeEnum.ActionBoost,
                   ActionId.BoostFreeCell               => SpellModifierActionTypeEnum.ActionBoost,
                   ActionId.BoostOccupiedCell           => SpellModifierActionTypeEnum.ActionBoost,
                   ActionId.SetSpellRangeMax            => SpellModifierActionTypeEnum.ActionSet,
                   ActionId.SetSpellRangeMin            => SpellModifierActionTypeEnum.ActionSet,
                   ActionId.BoostVisibleTargetOnCellOn  => SpellModifierActionTypeEnum.ActionBoost,
                   ActionId.BoostVisibleTargetOnCellOff => SpellModifierActionTypeEnum.ActionBoost,
                   ActionId.BoostPortalProjectionOn     => SpellModifierActionTypeEnum.ActionBoost,
                   ActionId.BoostPortalProjectionOff    => SpellModifierActionTypeEnum.ActionBoost,
                   ActionId.BoostSpellCc                => SpellModifierActionTypeEnum.ActionBoost,

                   ActionId.DeboostSpellApCost      => SpellModifierActionTypeEnum.ActionDeboost,
                   ActionId.DeboostSpellCriticalHit => SpellModifierActionTypeEnum.ActionDeboost,
                   ActionId.DeboostSpellRangeMax    => SpellModifierActionTypeEnum.ActionDeboost,
                   ActionId.DeboostSpellRangeMin    => SpellModifierActionTypeEnum.ActionDeboost,
                   ActionId.DeboostFreeCell         => SpellModifierActionTypeEnum.ActionDeboost,
                   ActionId.DeboostOccupiedCell     => SpellModifierActionTypeEnum.ActionDeboost,
                   _                                      => SpellModifierActionTypeEnum.ActionInvalid,
               };
    }


    /// <summary>
    /// Determines if the given action ID updates a stat.
    /// </summary>
    /// <param name="actionId">The action ID to check.</param>
    /// <returns>Returns true if the action ID updates a stat, otherwise false.</returns>
    public static bool IsStatUpdated(ActionId actionId)
    {
        return ActionConstants.FlatStatBoostActionIdToStat.ContainsKey(actionId) ||
               ActionConstants.PercentStatBoostActionIdToStat.ContainsKey(actionId);
    }

    /// <summary>
    /// Determines if the given action ID represents a stat steal.
    /// </summary>
    /// <param name="actionId">The action ID to check.</param>
    /// <returns>Returns true if the action ID represents a stat steal, otherwise false.</returns>
    public static bool IsStatSteal(ActionId actionId)
    {
        return actionId is ActionId.CharacterStealChance or
                           ActionId.CharacterStealVitality or
                           ActionId.CharacterStealAgility or
                           ActionId.CharacterStealIntelligence or
                           ActionId.CharacterStealWisdom or
                           ActionId.CharacterStealStrength;
    }

    /// <summary>
    /// Determines if the spell execution has global limitations for the given action ID.
    /// </summary>
    /// <param name="actionId">The action ID to check.</param>
    /// <returns>Returns true if the spell execution has global limitations, otherwise false.</returns>
    public static bool SpellExecutionHasGlobalLimitation(ActionId actionId)
    {
        return actionId is ActionId.TargetExecuteSpellOnSourceGlobalLimitation or
                           ActionId.CasterExecuteSpellGlobalLimitation or
                           ActionId.TargetExecuteSpellGlobalLimitation or
                           ActionId.TargetExecuteSpellWithAnimationGlobalLimitation or
                           ActionId.TargetExecuteSpellOnCellGlobalLimitation;
    }

    /// <summary>
    /// Determines if the given action ID represents damage inflicted.
    /// </summary>
    /// <param name="actionId">The action ID to check.</param>
    /// <returns>Returns true if the action ID represents damage inflicted, otherwise false.</returns>
    public static bool IsDamageInflicted(ActionId actionId)
    {
        return actionId is ActionId.CharacterLifePointsLost or
                           ActionId.CharacterLifePointsLostNoBoost or
                           ActionId.CharacterLifePointsLostBasedOnCasterLife or
                           ActionId.CharacterLifePointsLostBasedOnTargetLife or
                           ActionId.CharacterLifePointsLostBasedOnTargetMissingMaxLife or
                           ActionId.CharacterLifePointsLostBasedOnCasterMissingMaxLife or
                           ActionId.CharacterLifePointsLostBasedOnCasterLifeReducedByCaster or
                           ActionId.CharacterLifePointsLostBasedOnCasterLifeNotReduced or
                           ActionId.CharacterLifePointsLostBasedOnCasterLifeMidlife or
                           ActionId.CharacterLifePointsLostBasedOnCasterLifeMissing or
                           ActionId.CharacterLifePointsSteal or
                           ActionId.CharacterLifePointsStealWithoutBoost or
                           ActionId.CharacterLifePointsLostBasedOnMovementPoints or
                           ActionId.FightSplashFinalTakenDamage or
                           ActionId.FightSplashRawTakenDamage or
                           ActionId.FightSplashFinalTakenDamageNeutral or
                           ActionId.FightSplashRawTakenDamageNeutral or
                           ActionId.CharacterLifePointsLostFromEarth or
                           ActionId.CharacterLifePointsLostNoBoostFromEarth or
                           ActionId.CharacterLifePointsLostBasedOnCasterLifeFromEarth or
                           ActionId.CharacterLifePointsLostBasedOnTargetLifeFromEarth or
                           ActionId.CharacterLifePointsLostBasedOnCasterLifeMissingFromEarth or
                           ActionId.CharacterLifePointsLostBasedOnTargetMissingMaxLifeEarth or
                           ActionId.CharacterLifePointsLostBasedOnCasterMissingMaxLifeEarth or
                           ActionId.CharacterLifePointsStealFromEarth or
                           ActionId.CharacterLifePointsLostBasedOnMovementPointsFromEarth or
                           ActionId.FightSplashFinalTakenDamageEarth or
                           ActionId.FightSplashRawTakenDamageEarth or
                           ActionId.CharacterLifePointsLostFromAir or
                           ActionId.CharacterLifePointsLostNoBoostFromAir or
                           ActionId.CharacterLifePointsLostBasedOnCasterLifeFromAir or
                           ActionId.CharacterLifePointsLostBasedOnTargetLifeFromAir or
                           ActionId.CharacterLifePointsLostBasedOnCasterLifeMissingFromAir or
                           ActionId.CharacterLifePointsLostBasedOnTargetMissingMaxLifeAir or
                           ActionId.CharacterLifePointsLostBasedOnCasterMissingMaxLifeAir or
                           ActionId.CharacterLifePointsStealFromAir or
                           ActionId.CharacterLifePointsLostBasedOnMovementPointsFromAir or
                           ActionId.FightSplashFinalTakenDamageAir or
                           ActionId.FightSplashRawTakenDamageAir or
                           ActionId.CharacterLifePointsLostFromWater or
                           ActionId.CharacterLifePointsLostNoBoostFromWater or
                           ActionId.CharacterLifePointsLostBasedOnCasterLifeFromWater or
                           ActionId.CharacterLifePointsLostBasedOnTargetLifeFromWater or
                           ActionId.CharacterLifePointsLostBasedOnTargetMissingMaxLifeWater or
                           ActionId.CharacterLifePointsLostBasedOnCasterMissingMaxLifeWater or
                           ActionId.CharacterLifePointsLostBasedOnCasterLifeMissingFromWater or
                           ActionId.CharacterLifePointsStealFromWater or
                           ActionId.CharacterLifePointsLostBasedOnMovementPointsFromWater or
                           ActionId.FightSplashFinalTakenDamageWater or
                           ActionId.FightSplashRawTakenDamageWater or
                           ActionId.CharacterLifePointsLostFromFire or
                           ActionId.CharacterLifePointsLostNoBoostFromFire or
                           ActionId.CharacterLifePointsLostBasedOnCasterLifeFromFire or
                           ActionId.CharacterLifePointsLostBasedOnTargetLifeFromFire or
                           ActionId.CharacterLifePointsLostBasedOnTargetMissingMaxLifeFire or
                           ActionId.CharacterLifePointsLostBasedOnCasterMissingMaxLifeFire or
                           ActionId.CharacterLifePointsLostBasedOnCasterLifeMissingFromFire or
                           ActionId.CharacterLifePointsStealFromFire or
                           ActionId.CharacterLifePointsLostBasedOnMovementPointsFromFire or
                           ActionId.CharacterLifePointsLostFromPush or
                           ActionId.FightSplashFinalTakenDamageFire or
                           ActionId.FightSplashRawTakenDamageFire or
                           ActionId.CharacterLifePointsLostFromBestElement or
                           ActionId.FightSplashFinalTakenDamageBestElement or
                           ActionId.FightSplashRawTakenDamageBestElement or
                           ActionId.CharacterLifePointsStealFromBestElement or
                           ActionId.CharacterLifePointsLostFromWorstElement or
                           ActionId.CharacterLifePointsStealFromWorstElement or
                           ActionId.FightSplashFinalTakenDamageWorstElement;
    }

    /// <summary>
    /// Determines if the given action ID represents a clockwise confusion.
    /// </summary>
    /// <param name="actionId">The action ID to check.</param>
    /// <returns>Returns true if the action ID represents a clockwise confusion, otherwise false.</returns>
    public static bool IsClockwiseConfusion(ActionId actionId)
    {
        return actionId is ActionId.ClockwiseConfusionDegree or
                           ActionId.ClockwiseConfusionPi2 or
                           ActionId.ClockwiseConfusionPi4 or
                           ActionId.CounterClockwiseConfusionDegree or
                           ActionId.CounterClockwiseConfusionPi2 or
                           ActionId.CounterClockwiseConfusionPi4;
    }

    /// <summary>
    /// Determines if the critical flag is inherited for the given action ID.
    /// </summary>
    /// <param name="actionId">The action ID to check.</param>
    /// <returns>Returns true if the critical flag is inherited, otherwise false.</returns>
    public static bool IsCriticalFlagInherited(ActionId actionId)
    {
        return actionId is ActionId.TargetExecuteSpellOnSource or
                           ActionId.TargetExecuteSpellOnSourceGlobalLimitation or
                           ActionId.SourceExecuteSpellOnTarget or
                           ActionId.SourceExecuteSpellOnSource or
                           ActionId.TargetExecuteSpell or
                           ActionId.TargetExecuteSpellGlobalLimitation or
                           ActionId.TargetExecuteSpellWithAnimation or
                           ActionId.TargetExecuteSpellWithAnimationGlobalLimitation or
                           ActionId.CasterExecuteSpell or
                           ActionId.CasterExecuteSpellOnCell or
                           ActionId.CasterExecuteSpellGlobalLimitation or
                           ActionId.TargetExecuteSpellOnCell or
                           ActionId.TargetExecuteSpellOnCellGlobalLimitation or
                           ActionId.FightAddGlyphCastingSpell or
                           ActionId.FightAddGlyphCastingSpellEndturn or
                           ActionId.FightAddGlyphCastingSpellImmediate or
                           ActionId.FightAddGlyphAura or
                           ActionId.FightAddTrapCastingSpell or
                           ActionId.FightAddRuneCastingSpell;
    }

    public static bool IsScaleChange(ActionId actionId)
    {
        return actionId is ActionId.CharacterAddScaleFlat or ActionId.CharacterAddScalePercent;
    }

    public static bool IsLookChange(ActionId actionId)
    {
        return actionId is ActionId.CharacterChangeLook or ActionId.CharacterAddAppearance
                                                              or ActionId.CharacterChangeColor;
    }

    public static int GetBonesByValue(int value, bool driver, out int skinId, out int scale, out bool removeDriver)
    {
        skinId       = -1;
        scale        = -1;
        removeDriver = false;

        switch (value)
        {
            case 667: //Pandawa - Picole
                return driver ? 1084 : 44;
            case 729: //Xelor - Momification
                return driver ? 1068 : 113;
            case 103: //Zobal - Pleutre
            case 106: //Zobal - Pleutre
                skinId = 1449;
                return driver ? -1 : 1576;
            case 102: //Zobal - Psychopathe
            case 105: //Zobal - Psychopathe
                skinId = 1443;
                return driver ? -1 : 1575;
            case 1035: //Steamer - Scaphrandre
                skinId = 1955;
                return -1;
            case 874: //Pandawa - Colère de Zatoïshwan
                scale = driver ? 60 : 80;
                return driver ? 1202 : 453;
            case 1177: //Arbre - Feuillage, Arbre de vie
                scale = 80;
                return 3164;
            case 671:
            case 1171:
                scale = 80;
                return 3166;
            case 1234: //Osamodas - Fusion Dragonnet
                scale        = 150;
                removeDriver = true;
                return 3716;
            case 1235: //Osamodas - Fusion Tofu
                scale        = 130;
                removeDriver = true;
                return 3669;
            case 1236: //Osamodas - Fusion Bouftou
                scale        = 60;
                removeDriver = true;
                return 3670;
            case 1335: //Osamodas - Fusion Crapaud
                scale        = 110;
                removeDriver = true;
                return 4811;
            case 1260: //Ouginak
                scale        = 150;
                removeDriver = true;
                return 3906;
            case 1334: //Sacrieur - Souffrance Positive
                scale        = 145;
                removeDriver = true;
                return 4828;
            case 1298: //Sacrieur - Souffrance Negative
                scale        = 145;
                removeDriver = true;
                return 4115;
            case 1326: //Bambou panda
                removeDriver = true;
                return 4576;

        }
        
        return 1;
    }
}