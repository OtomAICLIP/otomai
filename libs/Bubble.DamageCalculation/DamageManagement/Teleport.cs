using System.Drawing;
using Bubble.Core.Datacenter.Datacenter.Effects;
using Bubble.DamageCalculation.FighterManagement;
using Bubble.DamageCalculation.SpellManagement;
using Bubble.DamageCalculation.Tools;

namespace Bubble.DamageCalculation.DamageManagement;

public static class Teleport
{
    public const int SameDirection = -1;

    public const int OppositeDirection = -2;

    /// <summary>
    /// Teleports a fighter and updates the position of other affected fighters.
    /// </summary>
    /// <param name="fightContext">The current fight context.</param>
    /// <param name="runningEffect">The running effect of the teleportation.</param>
    /// <param name="fighter">The fighter to be teleported.</param>
    /// <param name="updateMarks">Whether to update marks on the affected cells.</param>
    /// <returns>A list of EffectOutput representing the updated positions of the fighters.</returns>
    public static List<EffectOutput> TeleportFighter(FightContext fightContext, RunningEffect runningEffect,
        HaxeFighter fighter, bool updateMarks)
    {
        var effectOutputs = new List<EffectOutput>();
        var actionId      = runningEffect.GetSpellEffect().ActionId;

        bool isExchange;
        int  lookDirection;

        HaxeFighter  casterOrFighter;
        HaxeFighter? affectedFighter = null;

        if (actionId is ActionId.CharacterTeleportOnSameMap or ActionId.FightTeleswapMirror)
        {
            casterOrFighter = runningEffect.GetCaster();
            isExchange      = true;
        }
        else
        {
            isExchange      = ActionIdHelper.IsExchange(actionId);
            casterOrFighter = isExchange ? runningEffect.GetCaster() : fighter;
        }

        if (actionId is ActionId.FightRollbackTurnBeginPosition or ActionId.FightRollbackPreviousPosition)
        {
            lookDirection = -1;
        }
        else if (actionId is ActionId.CharacterTeleportOnSameMap or ActionId.FightTeleswapMirror)
        {
            lookDirection =
                MapTools.GetLookDirection4(casterOrFighter.GetCurrentPositionCell(), fightContext.TargetedCell);
        }
        else
        {
            lookDirection = isExchange ? -1 : -2;
        }

        // maybe we will not keep that but it works with double death
        if (isExchange && (casterOrFighter.GetCurrentPositionCell() == fightContext.TargetedCell))
        {
            casterOrFighter = fighter;
        }

        var teleportedCell =
            GetTeleportedPosition(fightContext, runningEffect, casterOrFighter, fightContext.TargetedCell);
        
        if (teleportedCell == casterOrFighter.GetCurrentPositionCell() && ActionIdHelper.IsExchange(actionId))
        {
            teleportedCell = fighter.GetCurrentPositionCell();
        }
        
        if (teleportedCell == casterOrFighter.GetCurrentPositionCell() && !isExchange)
        {
            if(affectedFighter == casterOrFighter)
            {
                return [];
            }
            
            effectOutputs.Add(EffectOutput.FromMovement(casterOrFighter.Id, 
                runningEffect.Caster.Id,
                runningEffect.SpellEffect.ActionId,
                teleportedCell, 
                lookDirection, 
                affectedFighter, 
                invalid: true));

            return effectOutputs;
        }

        if (fightContext.Map.IsCellWalkable(teleportedCell))
        {
            affectedFighter = fightContext.GetFighterFromCell(teleportedCell, true);
            if (affectedFighter != null && affectedFighter.IsAlive())
            {
                if (actionId == ActionId.CharacterExchangePlacesForce ||
                    affectedFighter.CanSwitchPosition(casterOrFighter, actionId, false) &&
                    casterOrFighter.CanSwitchPosition(affectedFighter, actionId, isExchange))
                {
                    if(affectedFighter == casterOrFighter)
                    {
                        return [];
                    }
                    
                    effectOutputs.Add(EffectOutput.FromMovement(affectedFighter.Id,
                        runningEffect.Caster.Id,
                        runningEffect.SpellEffect.ActionId,
                        casterOrFighter.GetCurrentPositionCell(), 
                        -1,
                        casterOrFighter));
                    
                    affectedFighter.SetCurrentPositionCell(casterOrFighter.GetCurrentPositionCell());
                }
                else
                {
                    return new List<EffectOutput>();
                }
            }
            else
            {
                affectedFighter = null;
            }

            if (casterOrFighter.HasState(8))
            {
                ReleaseFighter(fightContext, casterOrFighter.GetCarrier(fightContext));
            }

            casterOrFighter.SetCurrentPositionCell(teleportedCell);
        }
        else
        {
            teleportedCell = -1;
        }

        if(actionId != ActionId.CharacterExchangePlaces && actionId != ActionId.CharacterExchangePlacesForce)
        {
            if (casterOrFighter.GetCurrentPositionCell() == teleportedCell)
            {
                DamageCalculator.ExecuteMarks(fightContext,
                    runningEffect,
                    casterOrFighter,
                    casterOrFighter.GetCurrentPositionCell(),
                    updateMarks,
                    fromDrag: true);
            }
        }
        
        if (affectedFighter != null)
        {
            DamageCalculator.ExecuteMarks(fightContext,
                runningEffect, 
                affectedFighter,
                affectedFighter.GetCurrentPositionCell(),
                updateMarks,
                fromDrag: true);
        }

        if(affectedFighter == casterOrFighter)
        {
            return [];
        }
        
        effectOutputs.Add(EffectOutput.FromMovement(casterOrFighter.Id,
            runningEffect.Caster.Id,
            runningEffect.SpellEffect.ActionId,
            teleportedCell,
            lookDirection, 
            affectedFighter));

        return effectOutputs;
    }

    /// <summary>
    /// Calculates the teleported position of a fighter based on the action and targeted cell.
    /// </summary>
    /// <param name="fightContext">The current fight context.</param>
    /// <param name="runningEffect">The running effect of the teleportation.</param>
    /// <param name="fighter">The fighter to be teleported.</param>
    /// <param name="targetCell">The targeted cell for teleportation.</param>
    /// <returns>The cell ID of the teleported position.</returns>
    public static int GetTeleportedPosition(FightContext fightContext, RunningEffect runningEffect, HaxeFighter fighter,
        int targetCell)
    {
        var actionId = runningEffect.GetSpellEffect().ActionId;

        var currentPosition = fighter.GetCurrentPositionCell();
        var initialPosition = currentPosition;

        if (fighter.IsAlive() && fighter.CanTeleport(actionId, false, initialPosition))
        {
            Point? cellCoord = null;

            bool isExchange;
            if (actionId == ActionId.CharacterTeleportOnSameMap)
            {
                isExchange = ActionIdHelper.IsExchange(actionId);
                if (isExchange)
                {
                    currentPosition = targetCell;
                }
                else
                {
                    if (fightContext.IsCellEmptyForMovement(targetCell))
                    {
                        currentPosition = targetCell;
                    }
                    else if (runningEffect.GetSpellEffect().RawZone[0] != 'P')
                    {
                        var spellZone = runningEffect.GetSpellEffect().Zone;
                        var zoneCells = spellZone.GetCells!(targetCell, currentPosition);

                        zoneCells = PortalUtils.GetPortalChainFromPortalCells(fightContext.TargetedCell, zoneCells,
                            ActionIdHelper.IsPush(runningEffect.GetSpellEffect().ActionId));

                        foreach (var currentCell in zoneCells)
                        {
                            if (fightContext.IsCellEmptyForMovement(currentCell))
                            {
                                currentPosition = currentCell;
                                break;
                            }
                        }
                    }
                }
            }
            else if (actionId == ActionId.CharacterTeleportToFightStartPos)
            {
                currentPosition = fighter.Data.GetFightStartPosition();
            }
            else if (actionId == ActionId.FightRollbackTurnBeginPosition)
            {
                currentPosition = fighter.Data.GetTurnBeginPosition();
            }
            else if (actionId == ActionId.FightRollbackPreviousPosition)
            {
                currentPosition = fighter.GetPendingPreviousPosition();
            }
            else if (actionId == ActionId.FightTeleswap)
            {
                currentPosition = runningEffect.GetCaster().GetCurrentPositionCell();
            }
            else if (actionId is ActionId.FightTeleswapMirror or ActionId.FightTeleswapMirrorImpactPoint)
            {
                cellCoord = MapTools.GetCellCoordById(targetCell);
            }
            else if (actionId is ActionId.FightTeleswapMirrorCaster)
            {
                cellCoord = MapTools.GetCellCoordById(runningEffect.GetCaster().GetCurrentPositionCell());
            }
            else
            {
                isExchange = ActionIdHelper.IsExchange(actionId);
                if (isExchange)
                {
                    currentPosition = targetCell;
                }
                else
                {
                    cellCoord = null;
                }
            }

            if (cellCoord != null)
            {
                var currentCoord = MapTools.GetCellCoordById(currentPosition)!.Value;
                var targetCoord  = cellCoord.Value;
                var newCoord = new Point(targetCoord.X + (targetCoord.X - currentCoord.X),
                    targetCoord.Y + (targetCoord.Y - currentCoord.Y));
                currentPosition = MapTools.GetCellIdByCoord(newCoord.X, newCoord.Y);
            }
        }

        if (currentPosition != initialPosition)
        {
            var otherFighter = fightContext.GetFighterFromCell(currentPosition);
            if (otherFighter != null && (fighter.HasState(3) || otherFighter.HasState(3)))
            {
                currentPosition = initialPosition;
            }
        }

        return currentPosition;
    }

    /// <summary>
    /// Throws a fighter that is being carried to a targeted cell.
    /// </summary>
    /// <param name="fightContext">The fight context.</param>
    /// <param name="carrier">The carrier HaxeFighter.</param>
    /// <param name="runningEffect">The effect causing the throw.</param>
    /// <param name="applyMarks">Indicates whether to apply marks on the target cell.</param>
    /// <returns>An array of EffectOutputs representing the throw's effects.</returns>
    public static List<EffectOutput> ThrowFighter(FightContext fightContext, HaxeFighter carrier,
        RunningEffect runningEffect, bool applyMarks)
    {
        var carriedFighter = carrier.GetCarried(fightContext);
        if (carriedFighter == null)
        {
            return new List<EffectOutput>();
        }

        RemoveCarrierState(fightContext, carrier);
        RemoveCarriedState(fightContext, carriedFighter);

        carriedFighter.SetCurrentPositionCell(fightContext.TargetedCell);

        var lookDirection = MapTools.GetLookDirection4(carrier.GetCurrentPositionCell(), fightContext.TargetedCell);
        var movementEffect = EffectOutput.FromMovement(carriedFighter.Id, runningEffect.Caster.Id,
            runningEffect.SpellEffect.ActionId, fightContext.TargetedCell, lookDirection);
        movementEffect.ThrowedBy = carrier.Id;
        DamageCalculator.ExecuteMarks(fightContext, runningEffect, carriedFighter, fightContext.TargetedCell,
            applyMarks, fromDrag: true);

        return new List<EffectOutput> { movementEffect, };
    }

    /// <summary>
    /// Releases a carried fighter.
    /// </summary>
    /// <param name="fightContext">The fight context.</param>
    /// <param name="carrier">The carrier HaxeFighter.</param>
    /// <returns>An array of EffectOutputs representing the release's effects.</returns>
    public static IList<EffectOutput> ReleaseFighter(FightContext fightContext, HaxeFighter? carrier)
    {
        if (carrier == null)
        {
            return new List<EffectOutput>();
        }

        var carriedFighter = carrier.GetCarried(fightContext);

        if (carriedFighter == null)
        {
            return new List<EffectOutput>();
        }

        BreakCarrierLink(carrier);

        RemoveCarrierState(fightContext, carrier);
        RemoveCarriedState(fightContext, carriedFighter);

        var movementEffect = EffectOutput.FromMovement(carriedFighter.Id, carrier.Id,
            ActionId.CharacterTeleportOnSameMap, carrier.GetBeforeLastSpellPosition(), -1, null, true);
        movementEffect.ThrowedBy = carrier.Id;

        return new List<EffectOutput>()
        {
            movementEffect,
        };
    }

    private static void RemoveCarriedState(FightContext fightContext, HaxeFighter carriedFighter)
    {
        var removeCarriedEffect = new HaxeSpellEffect(0, 0, 0, ActionId.FightUnsetState, 8, 0, 0,
            0, false, "I", "P", "A,a", 0d, 0, false, 0, 0, false, 0, -1);
        var carriedRunningEffect =
            new RunningEffect(fightContext, carriedFighter, HaxeSpell.Empty, removeCarriedEffect);

        DamageCalculator.ComputeEffect(fightContext, carriedRunningEffect, false, new List<HaxeFighter>()
        {
            carriedFighter,
        }, null);
    }

    private static HaxeSpellEffect RemoveCarrierState(FightContext fightContext, HaxeFighter carrier)
    {
        var removeCarrierEffect = new HaxeSpellEffect(0, 0, 0, ActionId.FightUnsetState, 3, 0, 0,
            0, false, "I", "P", "A,a", 0d, 0, false, 0, 0, false, 0, -1);
        var carrierRunningEffect = new RunningEffect(fightContext, carrier, HaxeSpell.Empty, removeCarrierEffect);

        DamageCalculator.ComputeEffect(fightContext, carrierRunningEffect, false, new List<HaxeFighter>()
        {
            carrier,
        }, null);

        BreakCarrierLink(carrier);

        return removeCarrierEffect;
    }


    private static void AddCarriedState(FightContext fightContext, HaxeFighter carriedFighter, HaxeSpell spell)
    {
        var removeCarriedEffect = new HaxeSpellEffect(0, 0, 0, ActionId.FightSetState, 0, 0, 8,
            -1000, false, "I", "P", "A,a", 0d, 0, false, 0, 0, false, 0, -1);
        var carriedRunningEffect = new RunningEffect(fightContext, carriedFighter, spell, removeCarriedEffect);

        DamageCalculator.ComputeEffect(fightContext, carriedRunningEffect, false, new List<HaxeFighter>()
        {
            carriedFighter,
        }, null);
    }

    private static HaxeSpellEffect AddCarrierState(FightContext fightContext, HaxeFighter carrier, HaxeSpell spell)
    {
        var removeCarrierEffect = new HaxeSpellEffect(0, 0, 0, ActionId.FightSetState, 0, 0, 3,
            -1000, false, "I", "P", "A,a", 0d, 0, false, 0, 0, false, 0, -1);
        var carrierRunningEffect = new RunningEffect(fightContext, carrier, spell, removeCarrierEffect);

        DamageCalculator.ComputeEffect(fightContext, carrierRunningEffect, false, new List<HaxeFighter>()
        {
            carrier,
        }, null);
        return removeCarrierEffect;
    }

    /// <summary>
    /// Breaks the link between the carrier and the carried fighter.
    /// </summary>
    /// <param name="carrier">The carrier HaxeFighter.</param>
    public static void BreakCarrierLink(HaxeFighter carrier)
    {
        carrier.CarryFighter(null);
    }

    /// <summary>
    /// Carries a fighter in the context of a fight.
    /// </summary>
    /// <param name="fightContext">The fight context where the action is happening.</param>
    /// <param name="runningEffect">The running effect that triggered this action.</param>
    /// <param name="targetFighter">The fighter to be carried.</param>
    /// <returns>An array of EffectOutput containing the results of the action.</returns>
    public static List<EffectOutput> CarryFighter(FightContext fightContext, RunningEffect runningEffect,
        HaxeFighter targetFighter)
    {
        if (targetFighter.HasStateEffect(3) || !targetFighter.Data.CanBreedBeCarried() ||
            targetFighter.HasStateEffect(4))
        {
            return new List<EffectOutput>();
        }

        runningEffect.GetCaster().CarryFighter(targetFighter);

        targetFighter.SetCurrentPositionCell(runningEffect.GetCaster().GetCurrentPositionCell());

        AddCarriedState(fightContext, targetFighter, runningEffect.GetSpell());
        AddCarrierState(fightContext, runningEffect.GetCaster(), runningEffect.GetSpell());

        var direction = MapTools.GetLookDirection4(runningEffect.GetCaster().GetCurrentPositionCell(), fightContext.TargetedCell);
        var movementOutput = EffectOutput.FromMovement(targetFighter.Id, runningEffect.Caster.Id,
            runningEffect.SpellEffect.ActionId, runningEffect.GetCaster().GetCurrentPositionCell(), direction, null,
            true);
        movementOutput.CarriedBy = runningEffect.GetCaster().Id;

        var outputs = new List<EffectOutput>
        {
            movementOutput,
        };

        if (runningEffect.GetCaster().IsInvisible())
        {
            DamageCalculator.DispelInvisibility(fightContext, runningEffect.GetCaster(), runningEffect.GetSpell(), false);
        }

        if (targetFighter.IsInvisible())
        {
            DamageCalculator.DispelInvisibility(fightContext, targetFighter, runningEffect.GetSpell(), false);
        }

        return outputs;
    }
}