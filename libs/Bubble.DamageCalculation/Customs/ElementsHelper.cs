using Bubble.Core.Datacenter.Datacenter.Effects;

namespace Bubble.DamageCalculation.Customs;

public class ElementsHelper
{
    /// <summary>
    /// Gets the element associated with the given action ID.
    /// </summary>
    /// <param name="actionId">The action ID to get the element from.</param>
    /// <returns>The element associated with the given action ID.</returns>
    public static int GetElementFromActionId(ActionId actionId)
    {
        switch (actionId)
        {
            case ActionId.CharacterLifePointsWinWithoutElement:
                return 5; // None
            case ActionId.CharacterLifePointsStealWithoutBoost:
            case ActionId.CharacterLifePointsLostBasedOnCasterLife:
            case ActionId.CharacterLifePointsSteal:
            case ActionId.CharacterLifePointsLost:
            case ActionId.CharacterLifePointsWinWithoutBoost:
            case ActionId.CharacterLifePointsLostNoBoost:
            case ActionId.CharacterLifePointsLostBasedOnCasterLifeMissing:
            case ActionId.CharacterLifePointsLostBasedOnCasterLifeNotReduced:
            case ActionId.CharacterLifePointsLostBasedOnCasterLifeMidlife:
            case ActionId.CharacterLifePointsLostBasedOnMovementPoints:
            case ActionId.CharacterLifePointsLostBasedOnTargetLife:
            case ActionId.CharacterLifePointsLostBasedOnTargetMissingMaxLife:
            case ActionId.FightSplashRawTakenDamageNeutral:
            case ActionId.FightSplashFinalTakenDamageNeutral:
            case ActionId.CharacterLifePointsWinFromNeutral:
                return 0; // Neutral
            case ActionId.CharacterLifePointsLostBasedOnCasterLifeFromAir:
            case ActionId.CharacterLifePointsStealFromAir:
            case ActionId.CharacterLifePointsLostFromAir:
            case ActionId.CharacterLifePointsLostBasedOnCasterLifeMissingFromAir:
            case ActionId.CharacterLifePointsLostBasedOnMovementPointsFromAir:
            case ActionId.CharacterLifePointsLostNoBoostFromAir:
            case ActionId.CharacterLifePointsLostBasedOnTargetLifeFromAir:
            case ActionId.CharacterLifePointsLostBasedOnTargetMissingMaxLifeAir:
            case ActionId.FightSplashRawTakenDamageAir:
            case ActionId.FightSplashFinalTakenDamageAir:
            case ActionId.CharacterLifePointsWinFromAir:
                return 4; // Air
            case ActionId.CharacterLifePointsLostBasedOnCasterLifeFromWater:
            case ActionId.CharacterLifePointsStealFromWater:
            case ActionId.CharacterLifePointsLostFromWater:
            case ActionId.CharacterLifePointsLostBasedOnCasterLifeMissingFromWater:
            case ActionId.CharacterLifePointsLostBasedOnMovementPointsFromWater:
            case ActionId.CharacterLifePointsLostNoBoostFromWater:
            case ActionId.CharacterLifePointsLostBasedOnTargetLifeFromWater:
            case ActionId.CharacterLifePointsLostBasedOnTargetMissingMaxLifeWater:
            case ActionId.FightSplashRawTakenDamageWater:
            case ActionId.FightSplashFinalTakenDamageWater:
            case ActionId.CharacterLifePointsWinFromWater:
                return 3; // Water
            case ActionId.CharacterLifePointsLostBasedOnCasterLifeFromFire:
            case ActionId.CharacterLifePointsStealFromFire:
            case ActionId.CharacterLifePointsLostFromFire:
            case ActionId.CharacterLifePointsWinFromFire:
            case ActionId.CharacterLifePointsLostBasedOnCasterLifeMissingFromFire:
            case ActionId.CharacterLifePointsLostBasedOnMovementPointsFromFire:
            case ActionId.CharacterLifePointsWinZobal:
            case ActionId.CharacterLifePointsLostNoBoostFromFire:
            case ActionId.CharacterLifePointsLostBasedOnTargetLifeFromFire:
            case ActionId.CharacterLifePointsLostBasedOnTargetMissingMaxLifeFire:
            case ActionId.FightSplashRawTakenDamageFire:
            case ActionId.FightSplashFinalTakenDamageFire:
                return 2; // Fire
            case ActionId.CharacterLifePointsLostBasedOnCasterLifeFromEarth:
            case ActionId.CharacterLifePointsStealFromEarth:
            case ActionId.CharacterLifePointsLostFromEarth:
            case ActionId.CharacterLifePointsLostBasedOnCasterLifeMissingFromEarth:
            case ActionId.CharacterLifePointsLostBasedOnMovementPointsFromEarth:
            case ActionId.CharacterLifePointsLostNoBoostFromEarth:
            case ActionId.CharacterLifePointsLostBasedOnTargetLifeFromEarth:
            case ActionId.CharacterLifePointsLostBasedOnTargetMissingMaxLifeEarth:
            case ActionId.FightSplashRawTakenDamageEarth:
            case ActionId.FightSplashFinalTakenDamageEarth:
            case ActionId.CharacterLifePointsWinFromEarth:
                return 1; // Earth
            case ActionId.CharacterLifePointsLostFromBestElement:
            case ActionId.CharacterLifePointsStealFromBestElement:
            case ActionId.FightSplashRawTakenDamageBestElement:
            case ActionId.FightSplashFinalTakenDamageBestElement:
            case ActionId.CharacterLifePointsWinFromBestElement:
                return 6; // Best
            case ActionId.CharacterLifePointsLostFromWorstElement:
            case ActionId.CharacterLifePointsStealFromWorstElement:
            case ActionId.FightSplashFinalTakenDamageWorstElement:
                return 7; // Worst
            default:
                return -1; // Undefined
        }
    }
}