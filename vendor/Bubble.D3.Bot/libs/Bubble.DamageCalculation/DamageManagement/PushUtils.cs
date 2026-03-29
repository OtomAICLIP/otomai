using Bubble.Core.Datacenter.Datacenter.Effects;
using Bubble.DamageCalculation.Customs;
using Bubble.DamageCalculation.FighterManagement;
using Bubble.DamageCalculation.SpellManagement;
using Bubble.DamageCalculation.Tools;

namespace Bubble.DamageCalculation.DamageManagement;

public static class PushUtils
{
    public const bool AllowMarkPreview = true;

    /// <summary>
    /// This method returns the opposite direction of the push direction. 
    /// </summary>
    /// <param name="sourceX">An integer representing the x-coordinate of the source location</param>
    /// <param name="sourceY">An integer representing the y-coordinate of the source location</param>
    /// <param name="destinationX">An integer representing the x-coordinate of the destination location</param>
    /// <param name="checkDiagonal">A boolean representing whether to check for diagonal direction, with a default value of true</param>
    /// <returns>The opposite direction of the push direction</returns>
    public static int GetPullDirection(int sourceX, int sourceY, int destinationX, bool checkDiagonal = true)
    {
        var pushDirection = GetPushDirection(sourceX, sourceY, destinationX, checkDiagonal);
        if (pushDirection == -1)
        {
            return pushDirection;
        }

        return MapDirection.GetOppositeDirection(pushDirection);
    }

    /// <summary>
    /// This method returns the push direction based on the source and destination locations. 
    /// </summary>
    /// <param name="sourceX">An integer representing the x-coordinate of the source location</param>
    /// <param name="sourceY">An integer representing the y-coordinate of the source location</param>
    /// <param name="destinationX">An integer representing the x-coordinate of the destination location</param>
    /// <param name="checkDiagonal">A boolean representing whether to check for diagonal direction, with a default value of true</param>
    /// <returns>The push direction based on the source and destination locations</returns>
    public static int GetPushDirection(int sourceX, int sourceY, int destinationX, bool checkDiagonal = true)
    {
        int direction;
        if (destinationX == sourceY && (destinationX == sourceX || !checkDiagonal))
        {
            return -1;
        }

        var intermediatePoint = sourceY == destinationX ? sourceX : sourceY;
        if (MapTools.IsInDiag(intermediatePoint, destinationX))
        {
            direction = MapTools.GetLookDirection4DiagExact(intermediatePoint, destinationX);
        }
        else
        {
            direction = MapTools.GetLookDirection4(intermediatePoint, destinationX);
        }

        return direction;
    }

    /// <summary>
    /// This method calculates the damage range of a collision between two fighters.
    /// </summary>
    /// <param name="fightContext">An instance of the FightContext class</param>
    /// <param name="attacker">An instance of the HaxeFighter class representing the attacker</param>
    /// <param name="defender">An instance of the HaxeFighter class representing the defender</param>
    /// <param name="power">An integer representing the collision power</param>
    /// <param name="resistance">An integer representing the collision resistance</param>
    /// <returns>The damage range of a collision between two fighters</returns>
    public static DamageRange GetCollisionDamage(FightContext fightContext, HaxeFighter attacker, HaxeFighter defender,
                                                 int power, int resistance)
    {
        int attackerLevel;
        if (attacker.Data.IsSummon() && attacker.Data.GetSummonerId() != 0)
        {
            attackerLevel = attacker.GetSummoner(fightContext)!.Level;
        }
        else
        {
            attackerLevel = attacker.Level;
        }

        var attackerStrength = attacker.Data.GetCharacteristicValue(StatId.PushDamageBonus);
        var defenderStrength = defender.Data.GetCharacteristicValue(StatId.PushDamageReduction);
        attackerStrength -= defenderStrength;
        var damage = power * ((int)Math.Floor(attackerLevel / 2.0) + 32 + attackerStrength) /
                     (4 * (int)Math.Pow(2, resistance));
        var damageRange = new DamageRange(damage, damage);
        damageRange.MinimizeBy(0);
        damageRange.IsCollision = true;
        if (attacker.IsPacifist())
        {
            damageRange.SetToZero();
        }

        return damageRange;
    }

    /// <summary>
    /// Pulls a target fighter towards the caster or another fighter based on the given parameters.
    /// </summary>
    /// <param name="fightContext">The fight context in which the pull occurs.</param>
    /// <param name="runningEffect">The running effect that caused the pull.</param>
    /// <param name="target">The target fighter to be pulled.</param>
    /// <param name="distance">The distance to pull the target.</param>
    /// <param name="ignoreBlocking">Whether to ignore blocking when pulling the target.</param>
    /// <param name="ignoreDamageReduction">Whether to ignore damage reduction when applying damage.</param>
    /// <returns>An array of EffectOutput the results of the pull action.</returns>
    public static List<EffectOutput> Pull(FightContext fightContext, RunningEffect runningEffect, HaxeFighter target,
                                          int distance, bool ignoreBlocking, bool ignoreDamageReduction)
    {
        var caster      = runningEffect.GetCaster();
        var usingPortal = fightContext.UsingPortal();
        var isGetPulled =
            runningEffect.GetSpellEffect().ActionId == ActionId.CharacterGetPulled; // ActionCharacterGetPulled

        if (isGetPulled)
        {
            (caster, target) = (target, caster);
        }

        int startPosition;
        if (usingPortal)
        {
            startPosition = isGetPulled
                ? fightContext.InputPortalCellId
                : fightContext.Map.GetOutputPortalCell(fightContext.InputPortalCellId);
        }
        else
        {
            startPosition = caster.GetCurrentPositionCell();
        }

        if (fightContext.MarkMainCell != -1)
        {
            startPosition = fightContext.MarkMainCell;
        }

        var targetPosition = fightContext.InMovement
            ? target.GetCurrentPositionCell()
            : target.GetBeforeLastSpellPosition();

        if (fightContext.MarkExecutionCell != -1)
        {
            targetPosition = target.BeforeMarkPosition;
        }
        
        var pullDirection = GetPullDirection(startPosition, 
            fightContext.TargetedCell, 
            targetPosition,
            !fightContext.InMovement);
        
        return Drag(fightContext, runningEffect, target, distance, pullDirection, ignoreBlocking, false,
                    ignoreDamageReduction, true);
    }

    /// <summary>
    /// This method pushes a fighter in a specified direction. 
    /// </summary>
    /// <param name="fightContext">An instance of the FightContext class</param>
    /// <param name="runningEffect">An instance of the RunningEffect class</param>
    /// <param name="pushedFighter">An instance of the HaxeFighter class representing the fighter to be pushed</param>
    /// <param name="pushPower">An integer representing the push power</param>
    /// <param name="isForced">A boolean representing whether the push is forced or not</param>
    /// <param name="ignoreMovementCost">A boolean representing whether to ignore movement cost or not</param>
    /// <param name="ignoreLineOfSight">A boolean representing whether to ignore line of sight or not</param>
    /// <returns>An array of EffectOutput</returns>
    public static List<EffectOutput> Push(FightContext fightContext, RunningEffect runningEffect,
                                          HaxeFighter pushedFighter, int pushPower, bool isForced,
                                          bool ignoreMovementCost, bool ignoreLineOfSight)
    {
        var caster      = runningEffect.GetCaster();
        var usingPortal = fightContext.UsingPortal();
        var isGetPushed =
            runningEffect.GetSpellEffect().ActionId == ActionId.CharacterGetPushed; // ActionCharacterGetPushed

        if (isGetPushed)
        {
            (caster, pushedFighter) = (pushedFighter, caster);
        }

        int pushSource;
        if (usingPortal)
        {
            pushSource = isGetPushed
                ? fightContext.InputPortalCellId
                : fightContext.Map.GetOutputPortalCell(fightContext.InputPortalCellId);
        }
        else
        {
            pushSource = caster.GetBeforeLastSpellPosition();
        }

        if (fightContext.MarkMainCell != -1)
        {
            pushSource = fightContext.MarkMainCell;
        }

        var currentPosition = fightContext.InMovement
            ? pushedFighter.GetCurrentPositionCell()
            : pushedFighter.GetBeforeLastSpellPosition();
        
        if (fightContext.MarkExecutionCell != -1)
        {
            currentPosition = pushedFighter.BeforeMarkPosition;
        }
        
        var pushDirection = GetPushDirection(pushSource, fightContext.TargetedCell, currentPosition,
                                             !fightContext.InMovement);
        
        return Drag(fightContext, runningEffect, pushedFighter, pushPower, pushDirection, isForced,
                    ignoreMovementCost, ignoreLineOfSight, false);
    }

    /// <summary>
    /// This method pulls a fighter to the caster's position. 
    /// </summary>
    /// <param name="fightContext">An instance of the FightContext class</param>
    /// <param name="runningEffect">An instance of the RunningEffect class</param>
    /// <param name="pulledFighter">An instance of the HaxeFighter class representing the fighter to be pulled</param>
    /// <param name="ignoreMovementCost">A boolean representing whether to ignore movement cost or not</param>
    /// <param name="ignoreLineOfSight">A boolean representing whether to ignore line of sight or not</param>
    /// <returns>An array of EffectOutput</returns>
    public static List<EffectOutput> PullTo(FightContext fightContext, RunningEffect runningEffect,
                                            HaxeFighter pulledFighter,
                                            bool ignoreMovementCost, bool ignoreLineOfSight)
    {
        var distance    = MapTools.GetDistance(fightContext.TargetedCell, pulledFighter.GetBeforeLastSpellPosition());
        var copyContext = fightContext.Copy();
        copyContext.TargetedCell = runningEffect.GetCaster().GetBeforeLastSpellPosition();
        return Pull(copyContext, runningEffect, pulledFighter, distance, ignoreMovementCost,
                    ignoreLineOfSight);
    }

    /// <summary>
    /// This method pushes a fighter to the targeted cell. 
    /// </summary>
    /// <param name="fightContext">An instance of the FightContext class</param>
    /// <param name="runningEffect">An instance of the RunningEffect class</param>
    /// <param name="pushedFighter">An instance of the HaxeFighter class representing the fighter to be pushed</param>
    /// <param name="isForced">A boolean representing whether the push is forced or not</param>
    /// <param name="ignoreMovementCost">A boolean representing whether to ignore movement cost or not</param>
    /// <param name="ignoreLineOfSight">A boolean representing whether to ignore line of sight or not</param>
    /// <returns>An array of EffectOutput</returns>
    public static List<EffectOutput> PushTo(FightContext fightContext, RunningEffect runningEffect,
                                            HaxeFighter pushedFighter,
                                            bool isForced, bool ignoreMovementCost, bool ignoreLineOfSight)
    {
        var distance    = MapTools.GetDistance(fightContext.TargetedCell, pushedFighter.GetBeforeLastSpellPosition());
        var copyContext = fightContext.Copy();
        copyContext.TargetedCell = runningEffect.GetCaster().GetBeforeLastSpellPosition();
        return Push(copyContext, runningEffect, pushedFighter, distance, isForced, ignoreMovementCost,
                    ignoreLineOfSight);
    }

    /// <summary>
    /// Drags a fighter in a given direction and applies relevant effects.
    /// </summary>
    /// <param name="fightContext">The current fight context.</param>
    /// <param name="runningEffect">The running effect related to the drag.</param>
    /// <param name="fighter">The fighter being dragged.</param>
    /// <param name="distance">The distance to drag the fighter.</param>
    /// <param name="direction">The direction to drag the fighter in.</param>
    /// <param name="skipInitialChecks">Whether to skip initial checks related to dragging.</param>
    /// <param name="applyCollisionDamage">Whether to apply collision damage after the drag.</param>
    /// <param name="processTriggers">Whether to process triggers during the drag.</param>
    /// <param name="isPull">Whether the drag is considered a pull rather than a push.</param>
    /// <returns>A list of effect outputs resulting from the drag.</returns>
    public static List<EffectOutput> Drag(FightContext fightContext, RunningEffect runningEffect,
        HaxeFighter fighter, int distance, int direction, bool skipInitialChecks, bool applyCollisionDamage,
        bool processTriggers, bool isPull)
    {
        var  isStopped = false;
        bool isDragged;

        DragResult? dragResult;

        if (!skipInitialChecks && (fighter.HasStateEffect(3) || !fighter.Data.CanBreedBePushed() || fighter.HasStateEffect(0)) || direction == -1)
        {
            return new List<EffectOutput>();
        }

        if (MapDirection.IsCardinal(direction))
        {
            distance = (int)Math.Ceiling(distance / 2.0);
        }

        var initialCell = fighter.GetCurrentPositionCell();
        
        var movedThroughPortal = false;
        var effectOutputs      = new List<EffectOutput>();

        do
        {
            dragResult = GetDragCellDest(fightContext, fighter, direction, distance);
            
            if (dragResult.Cell != fighter.GetCurrentPositionCell())
            {
                isStopped          = true;
                movedThroughPortal = dragResult.StopReason == DragResults.Portal;
                var carriedFighter = fighter.GetCarried(fightContext);
                if (carriedFighter != null)
                {
                    var updatedFightContext = fightContext.Copy();
                    updatedFightContext.TargetedCell = fighter.GetCurrentPositionCell();
                    effectOutputs.AddRange(Teleport.ThrowFighter(updatedFightContext, fighter, runningEffect, processTriggers));
                }
            }

            isDragged = ApplyDrag(fightContext, fighter, dragResult.Cell, processTriggers, out var cell);
            dragResult.Cell = cell;
            
            distance  = dragResult.RemainingForce;
        } while (isDragged && dragResult.StopReason == DragResults.Portal && distance > 0);

        if (isStopped)
        {
            var output = new EffectOutput(fighter.Id, runningEffect.Caster.Id, runningEffect.SpellEffect.ActionId)
            {
                Movement      = new MovementInfos(dragResult.Cell, -1),
                ThroughPortal = movedThroughPortal,
            };

            if (isPull)
            {
                output.IsPulled = true;
            }
            else
            {
                output.IsPushed = true;
            }

            effectOutputs.Add(output);
            fighter.PendingEffects.Add(output);
        }

        if (applyCollisionDamage)
        {
            ApplyCollisionDamage(fightContext, runningEffect, fighter, dragResult, direction, processTriggers);
        }

        if (isDragged)
        {
            if (fightContext.MarkExecutionCell == -1 && dragResult.Cell != -1)
            { 
                fighter.BeforeMarkPosition = dragResult.Cell;
            }

            if (fightContext.MarkExecutionCell == -1)
            {
                DamageCalculator.ExecuteMarks(fightContext, runningEffect, fighter, dragResult.Cell, processTriggers, fromDrag: true);
            }
            
            if (initialCell != fighter.GetCurrentPositionCell())
            {
                fightContext.Map.ReactivePortalAfterMove(fighter.GetCurrentPositionCell());
                DamageEffectHandler.RedefinePortals(fightContext);
            }
        }

        return effectOutputs;
    }

    /// <summary>
    /// Gets the destination cell after dragging a fighter and the drag result.
    /// </summary>
    /// <param name="fightContext">The current fight context.</param>
    /// <param name="fighter">The fighter being dragged.</param>
    /// <param name="direction">The direction to drag the fighter in.</param>
    /// <param name="distance">The distance to drag the fighter.</param>
    /// <returns>A DragResult object containing remaining force, destination cell, and stopping reason.</returns>
    public static DragResult GetDragCellDest(FightContext fightContext, HaxeFighter fighter, int direction,
                                             int distance)
    {
        var currentCell     = fighter.GetCurrentPositionCell();
        var iteration       = 0;
        var hasPortal       = false;
        var hasActiveObject = false;

        if (fighter.IsBomb())
        {
            DamageEffectHandler.RemoveBombWall(fightContext, fighter);
        }

        for (var remainingForce = 0; remainingForce < distance; remainingForce++)
        {
            iteration++;

            var currentPosition = currentCell;
            currentCell = MapTools.GetNextCellByDirection(currentCell, direction);

            if (IsPathBlocked(fightContext, currentPosition, currentCell, direction))
            {
                return new DragResult()
                {
                    RemainingForce = distance - (iteration - 1),
                    Cell           = currentPosition,
                    StopReason     = DragResults.Collision,
                };
            }

            var marksInteracting = fightContext.Map.GetMarkInteractingWithCell(currentCell, true);

            foreach (var currentMark in marksInteracting)
            {
                if (currentMark.MarkType == 0 || !currentMark.Active || currentMark.IsDeleted)
                {
                    continue;
                }

                if (!hasPortal && (currentMark.StopDrag() || currentMark.MarkType == GameActionMarkType.Portal))
                {
                    hasPortal = true;
                }

                if (!hasActiveObject && currentMark.MarkType != GameActionMarkType.Portal && currentMark.StopDrag())
                {
                    hasActiveObject = true;
                }

                if (hasPortal && hasActiveObject)
                {
                    break;
                }
            }

            if (hasPortal)
            {
                var stopReason = hasActiveObject ? DragResults.ActiveObject : DragResults.Portal;

                return new DragResult()
                {
                    RemainingForce = distance - iteration,
                    Cell           = currentCell,
                    StopReason     = stopReason,
                };
            }
        }

        return new DragResult()
        {
            RemainingForce = 0,
            Cell           = currentCell,
            StopReason     = DragResults.Complete,
        };
    }

    /// <summary>
    /// Applies the drag effect to a fighter, changing its position to the destination cell, and interacts with portals and dispels illusions on its path.
    /// </summary>
    /// <param name="fightContext">The fight context in which the dragging takes place.</param>
    /// <param name="fighter">The fighter being dragged.</param>
    /// <param name="destinationCell">The destination cell for the dragged fighter.</param>
    /// <param name="skipDispelling">Indicates whether dispelling illusions should be skipped while dragging.</param>
    /// <returns>True if the drag is successful, false otherwise.</returns>
    public static bool ApplyDrag(FightContext fightContext, HaxeFighter fighter, int destinationCell, bool skipDispelling, out int destination)
    {
        destination = destinationCell;
        
        if (destinationCell == fighter.GetCurrentPositionCell())
        {
            return false;
        }

        if (!skipDispelling)
        {
            var cellsOnLargeWay = MapTools.GetCellsIdOnLargeWay(fighter.GetCurrentPositionCell(), destinationCell);

            foreach (var cell in cellsOnLargeWay)
            {
                fightContext.Map.DispellIllusionOnCell(cell);
            }
        }

        fighter.SetCurrentPositionCell(destinationCell);

        if (!fighter.Data.CanBreedUsePortals() || fighter.HasStateEffect(17))
        {
            return true;
        }

        var interactingMarks = fightContext.Map.GetMarkInteractingWithCell(destinationCell, true, GameActionMarkType.Portal).Where(x => x.Active).ToArray();
        if (interactingMarks.Length > 0 && (interactingMarks[0].TeamId == fighter.TeamId || fightContext.GameTurn != 1))
        {
            var entry = interactingMarks[0];

            var outputPortalCell = fightContext.Map.GetOutputPortal(entry, out var usedPortal);
            if (!MapTools.IsValidCellId(outputPortalCell) || usedPortal == null)
            {
                return true;
            }

            fighter.SetCurrentPositionCell(outputPortalCell);
            destination = outputPortalCell;
            
            entry.Use();
            usedPortal.Use();

            DamageEffectHandler.RedefinePortals(fightContext);
        }

        return true;
    }

    /// <summary>
    /// Applies collision damage to a target and collateral targets based on the given parameters.
    /// </summary>
    /// <param name="fightContext">The fight context in which the collision occurs.</param>
    /// <param name="runningEffect">The running effect that caused the collision.</param>
    /// <param name="target">The primary target of the collision.</param>
    /// <param name="collisionData">An object containing data about the collision.</param>
    /// <param name="direction">The direction of the collision.</param>
    /// <param name="ignoreDamageReduction">Whether to ignore damage reduction when applying damage.</param>
    public static void ApplyCollisionDamage(FightContext fightContext, RunningEffect runningEffect,
                                            HaxeFighter target,
                                            DragResult collisionData, int direction, bool ignoreDamageReduction)
    {
        if (!target.IsAlive() || collisionData.RemainingForce <= 0 ||
            collisionData.StopReason == DragResults.ActiveObject)
        {
            return;
        }

        if (MapDirection.IsCardinal(direction))
        {
            collisionData.RemainingForce *= 2;
        }

        var caster = runningEffect.GetCaster();

        if (caster.PlayerType == PlayerType.Monster && caster.Data.IsSummon() && HaxeFighter.BombBreedId.Contains(caster.Breed) ||
            caster.PlayerType == PlayerType.Monster && caster.Data.IsSummon() && HaxeFighter.SteamerTurretBreedId.Contains(caster.Breed))
        {
            caster = caster.GetSummoner(fightContext);
        }

        ApplyCollisionDamageOnTarget(fightContext, runningEffect, target, collisionData.RemainingForce, 0, ignoreDamageReduction);
        var collateralTargets = GetCollateralTargets(fightContext, collisionData.Cell, direction, collisionData.RemainingForce);
        var collateralCount   = 1;

        foreach (var currentFighter in collateralTargets)
        {
            ApplyCollisionDamageOnTarget(fightContext, runningEffect, currentFighter, collisionData.RemainingForce, collateralCount, ignoreDamageReduction);
            collateralCount++;
        }
    }

    /// <summary>
    /// Applies collision damage on a single target based on the given parameters.
    /// </summary>
    /// <param name="fightContext">The fight context in which the collision occurs.</param>
    /// <param name="runningEffect">The running effect that caused the collision.</param>
    /// <param name="target">The target to apply collision damage on.</param>
    /// <param name="damage">The amount of damage to apply.</param>
    /// <param name="collateralIndex">The index of the collateral target, starting at 0 for the primary target.</param>
    /// <param name="ignoreDamageReduction">Whether to ignore damage reduction when applying damage.</param>
    public static void ApplyCollisionDamageOnTarget(FightContext fightContext, RunningEffect runningEffect,
                                                    HaxeFighter target, int damage, int collateralIndex,
                                                    bool ignoreDamageReduction)
    {
        var spellEffect = new HaxeSpellEffect(runningEffect.GetSpellEffect().Id, 1, 0,
                                              ActionId.CharacterLifePointsLostFromPush, damage, collateralIndex,
                                              0,
                                              0, runningEffect.GetSpellEffect().IsCritical, "I", "P", "a,A", 0, 0,
                                              true,
                                              0, 2, true, 1, 0);

        var modifiedEffect = runningEffect.Copy();
        modifiedEffect.OverrideSpellEffect(spellEffect);
        DamageCalculator.ComputeEffect(fightContext, modifiedEffect, ignoreDamageReduction, new List<HaxeFighter> { target, }, null);
    }

    /// <summary>
    /// Retrieves the collateral targets that will be affected by the collision.
    /// </summary>
    /// <param name="fightContext">The fight context in which the collision occurs.</param>
    /// <param name="cell">The starting cell of the collision.</param>
    /// <param name="direction">The direction of the collision.</param>
    /// <param name="force">The force of the collision.</param>
    /// <returns>A list of collateral targets affected by the collision.</returns>
    public static List<HaxeFighter> GetCollateralTargets(FightContext fightContext, int cell, int direction,
                                                         int force)
    {
        List<HaxeFighter> collateralTargets = new();
        cell = MapTools.GetNextCellByDirection(cell, direction);
        var fighter = fightContext.GetFighterFromCell(cell, true);

        while (force > 0 && fighter != null && fighter.IsAlive())
        {
            collateralTargets.Add(fighter);
            cell    = MapTools.GetNextCellByDirection(cell, direction);
            fighter = fightContext.GetFighterFromCell(cell);
            force--;
        }

        return collateralTargets;
    }

    /// <summary>
    /// Determines if the path between two cells is blocked.
    /// </summary>
    /// <param name="fightContext">The fight context in which the path is being checked.</param>
    /// <param name="startCell">The starting cell of the path.</param>
    /// <param name="destinationCell">The destination cell of the path.</param>
    /// <param name="movementPoints">The number of movement points available for the path.</param>
    /// <returns>True if the path is blocked, false otherwise.</returns>
    public static bool IsPathBlocked(FightContext fightContext, int startCell, int destinationCell,
                                     int movementPoints)
    {
        if (fightContext.IsCellEmptyForMovement(destinationCell) && MapTools.AdjacentCellsAllowAccess(fightContext, startCell, movementPoints))
        {
            return false;
        }

        return true;
    }
}