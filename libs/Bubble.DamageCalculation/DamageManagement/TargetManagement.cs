using Bubble.Core.Datacenter.Datacenter.Effects;
using Bubble.DamageCalculation.Customs;
using Bubble.DamageCalculation.FighterManagement;
using Bubble.DamageCalculation.SpellManagement;
using Bubble.DamageCalculation.Tools;

namespace Bubble.DamageCalculation.DamageManagement;

public static class TargetManagement
{
    /// <summary>
    /// Get the targets for a given spell effect in a fight context.
    /// </summary>
    /// <param name="fightContext">The fight context.</param>
    /// <param name="caster">The caster of the spell.</param>
    /// <param name="spell">The spell being cast.</param>
    /// <param name="effect">The spell effect to check for targets.</param>
    /// <param name="triggeringFighter">The fighter triggering the effect.</param>
    /// <param name="isFromDeath"></param>
    /// <param name="runningEffect"></param>
    /// <param name="forceTarget"></param>
    /// <returns>An object containing the targeted fighters, additional targets, and a flag indicating if the effect is used.</returns>
    public static TargetResult GetTargets(FightContext fightContext,
        HaxeFighter caster, 
        HaxeSpell spell,
        HaxeSpellEffect effect, 
        HaxeFighter? triggeringFighter,
        bool isFromDeath = false,
        RunningEffect? runningEffect = null,
        HaxeFighter? forceTarget = null,
        HaxeFighter? additionalTarget = null)
    {
        List<HaxeFighter> targetedFighters;

        var usingPortal = fightContext.UsingPortal();
        int index;

        var masks = effect.Masks.ToArray();
        
        for (index = 0; index < effect.Masks.Length; index++)
        {
            var mask = effect.Masks[index];

            if (mask[0] == '*')
            {
                if (!SpellManager.TargetPassMaskExclusion(caster, caster, triggeringFighter, fightContext, mask,
                    masks, usingPortal, true))
                {
                    return new TargetResult()
                    {
                        TargetedFighters  = null,
                        AdditionalTargets = null,
                        IsUsed            = false,
                    };
                }
            }
            else
            {
                break;
            }
        }

        if (effect.ActionId == ActionId.CasterExecuteSpellOnCell) // ActionCasterExecuteSpellOnCell
        {
            return new TargetResult
            {
                TargetedFighters  = new List<HaxeFighter>(),
                AdditionalTargets = new List<HaxeFighter>(),
                IsUsed            = true,
            };
        }

        var minRange = spell.MinimaleRange;
        var maxRange = spell.MaximaleRange;

        if (effect.ActionId is ActionId.CharacterPushUpTo)
        {
            minRange = 1;
            maxRange = 1;
        }

        if (effect.ActionId is ActionId.CharacterPushUpTo or ActionId.CharacterPullUpTo) // ActionCharacterPushUpTo || ActionCharacterPullUpTo
        {
            var startPoint = MapTools.GetCellCoordById(caster.GetCurrentPositionCell())!.Value;
            var endPoint   = MapTools.GetCellCoordById(fightContext.TargetedCell)!.Value;

            var startX = startPoint.X;
            var startY = startPoint.Y;
            var endX   = endPoint.X;
            var endY   = endPoint.Y;

            var direction = MapTools.GetLookDirection8ExactByCoord(startX, startY, endX, endY);

            targetedFighters = fightContext.GetFightersUpTo(caster.GetCurrentPositionCell(), direction, minRange, maxRange, 1);
        }
        else if (effect.ActionId is ActionId.CharacterTeleportOnSameMap && effect.RawZone[0] != 'P')
        {
            return new TargetResult
            {
                TargetedFighters  = new List<HaxeFighter> { caster },
                AdditionalTargets = new List<HaxeFighter>(),
                IsUsed            = true,
            };
        }
        else if (ActionIdHelper.IsSummonWithoutTarget(effect.ActionId) && effect.Zone.Shape == 'C')
        {
            targetedFighters = new List<HaxeFighter>();
        }
        else
        {
            var originCell = usingPortal ? fightContext.Map.GetOutputPortalCell(fightContext.InputPortalCellId) : caster.BeforeLastSpellPosition;

            targetedFighters = fightContext.GetFightersFromZone(effect.Zone,
                fightContext.TargetedCell,
                originCell,
                isFromDeath,
                caster,
                effect.RawZone,
                runningEffect,
                forceTarget,
                additionalTarget,
                fromAppearing: effect.Masks.Contains("U") || effect.Masks.Contains("u"));
        }

        if (fightContext.FromGlyphAuraSet)
        {
            var mark  = fightContext.FromGlyphAura;
            
            if (mark != null)
            {
                targetedFighters = targetedFighters.Where(fighter => !mark.TriggeredFighters.Contains(fighter.Id)).ToList();
            }
        }

        var additionalTargets = GetOutOfAreaTarget(fightContext, caster, effect, triggeringFighter, targetedFighters);
        
        if (additionalTargets.Count > 0)
        {
            targetedFighters = targetedFighters.Concat(additionalTargets).ToList();
        }

        targetedFighters = targetedFighters.Where(fighter => SpellManager.IsSelectedByMask(caster,
                                               effect.Masks.Skip(index).ToArray(),
                                               fighter,
                                               triggeringFighter, 
                                               fightContext))
                                           .ToList();

        return new TargetResult
        {
            TargetedFighters  = targetedFighters,
            AdditionalTargets = additionalTargets,
            IsUsed            = true,
        };
    }

    /// <summary>
    /// Apply combo bonus to the caster of a summon, storing pending buffs and removing them from the summon.
    /// </summary>
    /// <param name="fighter">The HaxeFighter representing the summon</param>
    /// <param name="fightContext">The FightContext containing information about the ongoing fight</param>
    public static void ApplyComboBonusToCaster(HaxeFighter fighter, FightContext fightContext)
    {
        foreach (var buff in fighter.Buffs)
        {
            if (buff.Effect.ActionId == ActionId.BombComboBonus)
            {
                fighter.GetSummoner(fightContext)!.StorePendingBuff(buff);
            }
        }

        fighter.RemoveBuffByActionId(ActionId.BombComboBonus);
    }

    /// <summary>
    /// Get the out of area targets based on the masks and action ids provided.
    /// </summary>
    /// <param name="fightContext">The FightContext containing information about the ongoing fight</param>
    /// <param name="caster">The HaxeFighter representing the caster</param>
    /// <param name="spellEffect">The HaxeSpellEffect representing the spell effect</param>
    /// <param name="triggeringFighter">(Nullable) The HaxeFighter representing the triggering fighter</param>
    /// <param name="targets">An array of HaxeFighters representing the potential targets</param>
    /// <returns>An array of HaxeFighters that are considered out of area targets</returns>
    public static IList<HaxeFighter> GetOutOfAreaTarget(FightContext fightContext, HaxeFighter caster,
                                                        HaxeSpellEffect spellEffect, HaxeFighter? triggeringFighter,
                                                        IList<HaxeFighter> targets)
    {
        IList<string> masks = spellEffect.Masks;

        var outOfAreaTargets = new List<HaxeFighter>();

        if (targets.IndexOf(caster) == -1 &&
            (masks.Contains("C") || spellEffect.ActionId == ActionId.CharacterTeleportOnSameMap) &&
            spellEffect.ActionId != ActionId.CharacterSummonDeadAllyInFight)
        {
            outOfAreaTargets.Add(caster);
        }

        if (masks.Contains("O") && triggeringFighter != null && targets.IndexOf(triggeringFighter) == -1)
        {
            outOfAreaTargets.Add(triggeringFighter);
        }

        var carriedFighter = caster.GetCarried(fightContext);

        if (carriedFighter != null)
        {
            if ((masks.Contains("K") || spellEffect.ActionId == ActionId.ThrowCarriedCharacter) && targets.IndexOf(carriedFighter) == -1 &&
                !outOfAreaTargets.Contains(carriedFighter))
            {
                outOfAreaTargets.Add(carriedFighter);
            }
        }

        return outOfAreaTargets;
    }
    public static int GetDistance(int cellId1, int cellId2)
    {
        var xCoord1 = GetCellIdXCoord(cellId1);
        var yCoord1 = GetCellIdYCoord(cellId1);

        var xCoord2 = GetCellIdXCoord(cellId2);
        var yCoord2 = GetCellIdYCoord(cellId2);

        return (int)Math.Floor((double)Math.Abs(xCoord2 - xCoord1) + Math.Abs(yCoord2 - yCoord1));
    }
    public static int GetCellIdXCoord(int cellId)
    {
        var floorDivision     = (int)Math.Floor((double)cellId / 15);
        var halfFloorDivision = (int)Math.Floor((double)(floorDivision + 1) / 2);
        var xCoord            = cellId - floorDivision * 15;
        return halfFloorDivision + xCoord;
    }
    public static int GetCellIdYCoord(int cellId)
    {
        var floorDivision     = (int)Math.Floor((double)cellId / 15);
        var halfFloorDivision = (int)Math.Floor((double)(floorDivision + 1) / 2);
        var diff              = floorDivision - halfFloorDivision;
        var yCoord            = cellId - floorDivision * 15;
        return yCoord - diff;
    }

    /// <summary>
    /// Compares two positions relative to a reference position, taking into account the direction and distances.
    /// </summary>
    /// <param name="referencePosition">The cell id of the reference position</param>
    /// <param name="otherWise">True if the comparison is clockwise, false otherwise</param>
    /// <param name="position1">The cell id of the first position to compare</param>
    /// <param name="position2">The cell id of the second position to compare</param>
    /// <returns>An integer representing the result of the comparison (-1, 0, or 1)</returns>
    public static int ComparePositions(int referencePosition, bool otherWise, int position1, int position2)
    {
        var distanceA  = MapTools.GetDistance(position1, referencePosition);
        var distanceB  = MapTools.GetDistance(position2, referencePosition);
        
        if (distanceA != distanceB)
        {
            return (distanceB - distanceA) * (otherWise ? 1 : -1);
        }

        var targetCellCoord = MapTools.GetCellCoordById(referencePosition);
        var aCoord          = MapTools.GetCellCoordById(position1);
        var bCoord          = MapTools.GetCellCoordById(position2);

        if (targetCellCoord == null || aCoord == null || bCoord == null)
        {
            return 0;
        }

        var directionA = MapTools.GetLookDirection8ByCoord(targetCellCoord.Value, aCoord.Value);
        var directionB = MapTools.GetLookDirection8ByCoord(targetCellCoord.Value, bCoord.Value);

        if (position1 == position2)
        {
            return 0;
        }
            
        if (directionA == directionB)
        {
            directionB = 0;
            if (directionA is 0 or 7 or 6 or 5)
            {
                directionA = position1 < position2 ? -1 : 1;
            }
            else
            {
                directionA = position1 < position2 ? 1 : -1;
            }
        }
        else
        {
            directionA = (directionA + 1) % 8;
            directionB = (directionB + 1) % 8;
        }

        return (directionB - directionA) * (otherWise ? 1 : -1);
    }

    /// <summary>
    /// Get a list of bombs that are about to explode.
    /// </summary>
    /// <param name="fighter">The HaxeFighter instance representing the bomb.</param>
    /// <param name="fightContext">The FightContext instance representing the fight.</param>
    /// <param name="runningEffect">The RunningEffect instance representing the effect.</param>
    /// <param name="processedBombs">An optional list of already processed bombs.</param>
    /// <returns>Returns a list of HaxeFighter instances representing the bombs about to explode.</returns>
    public static IList<HaxeFighter> GetBombsAboutToExplode(HaxeFighter fighter, FightContext fightContext, RunningEffect runningEffect, IList<HaxeFighter>? processedBombs = null)
    {
        if (fighter.PlayerType != PlayerType.Monster || !fighter.Data.IsSummon() || !HaxeFighter.BombBreedId.Contains(fighter.Breed))
        {
            return new List<HaxeFighter>();
        }

        var bombs = processedBombs ?? new List<HaxeFighter>();

        if (bombs.Contains(fighter))
        {
            return bombs;
        }
        

        var explosionSpell = DamageCalculator.DataInterface.GetLinkedExplosionSpellFromFighter(fighter);

        if (explosionSpell == null)
        {
            return bombs;
        }

        ApplyComboBonusToCaster(fighter, fightContext);
        
        bombs.Add(fighter);

        var originalTargetedCell = fightContext.TargetedCell;

        foreach (var effect in explosionSpell.GetEffects())
        {
            if (effect.ActionId != ActionId.CharacterActivateBomb)
            {
                continue;
            }

            var newEffect = new RunningEffect(fightContext, fighter, explosionSpell, effect)
            {
                ParentEffect  = runningEffect,
                ForceCritical = runningEffect.ForceCritical,
            };

            var targetManagementResult = GetTargets(fightContext, fighter, explosionSpell, effect, null);

            if (targetManagementResult.TargetedFighters == null)
            {
                return bombs;
            }

            foreach (var bomb in targetManagementResult.TargetedFighters)
            {
                if (bomb != fighter && !runningEffect.IsTargetingAnAncestor(bomb) && bomb.IsLinkedBomb(fighter))
                {
                    fightContext.TargetedCell = bomb.GetCurrentPositionCell();
                    bombs                     = GetBombsAboutToExplode(bomb, fightContext, newEffect, bombs);
                }
            }

            fightContext.TargetedCell = originalTargetedCell;
        }

        return bombs;
    }
}