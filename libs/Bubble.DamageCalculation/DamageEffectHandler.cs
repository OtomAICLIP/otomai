using Bubble.Core.Datacenter.Datacenter.Effects;
using Bubble.DamageCalculation.Customs;
using Bubble.DamageCalculation.DamageManagement;
using Bubble.DamageCalculation.FighterManagement;
using Bubble.DamageCalculation.SpellManagement;
using Bubble.DamageCalculation.Tools;

namespace Bubble.DamageCalculation;

public static class DamageEffectHandler
{
    public static void HandleForceRuneTrigger(FightContext fightContext, RunningEffect runningEffect, bool isTriggered,
        HaxeSpellEffect effect, HaxeFighter caster)
    {
        if (effect.Zone.Shape == 'P')
        {
            // Rune
            var runeMarks =
                fightContext.Map.GetMarkInteractingWithCell(fightContext.TargetedCell, true, GameActionMarkType.Rune);

            foreach (var rune in runeMarks)
            {
                if (rune.CasterId == caster.Id)
                {
                    DamageCalculator.ExecuteMarkSpell(null, rune, runningEffect, fightContext, isTriggered);
                    rune.IsDeleted = true;
                }
            }
        }
        else
        {
            var runeMarks = fightContext.Map.GetMarks(true, GameActionMarkType.Rune, caster.TeamId);

            foreach (var rune in runeMarks)
            {
                if (rune.CasterId == caster.Id &&
                    effect.Zone.IsCellInZone!(rune.MainCell, fightContext.TargetedCell,
                        caster.GetCurrentPositionCell()))
                {
                    DamageCalculator.ExecuteMarkSpell(null, rune, runningEffect, fightContext, isTriggered);
                    rune.IsDeleted = true;
                }
            }
        }
    }

    public static void AddEffectMarkTrigger(FightContext fightContext, Mark mark)
    {
        foreach (var fighter in fightContext.Fighters)
        {
            if (mark.Cells.Contains(fighter.GetCurrentPositionCell()))
            {
                if (fighter.PendingEffects.Any(x => x.MarkTriggered == mark.MarkId))
                {
                    continue;
                }

                fighter.PendingEffects.Add(EffectOutput.FromMarkTrigger(fighter.Id, mark.CasterId,
                    mark.GetActionTrigger(), mark.MarkId));
            }
        }
    }

    public static void HandleForceGlyphTrigger(FightContext fightContext, RunningEffect runningEffect, bool isTriggered,
        HaxeSpellEffect effect, HaxeFighter caster)
    {
        if (effect.Zone.Shape == 'P')
        {
            var marks = fightContext.Map.GetMarkInteractingWithCell(fightContext.TargetedCell, true,
                GameActionMarkType.Glyph);

            foreach (var mark in marks)
            {
                if (mark.Aura)
                    continue;
                
                if (mark.CasterId == caster.Id && (effect.Param3 == 0 || mark.FromSpell?.Id == effect.Param3))
                {
                    AddEffectMarkTrigger(fightContext, mark);
                    DamageCalculator.ExecuteMarkSpell(null, mark, runningEffect, fightContext, isTriggered);
                }
            }
        }
        else
        {
            var marks = fightContext.Map.GetMarks(false, GameActionMarkType.Glyph, caster.TeamId);

            foreach (var mark in marks)
            {
                if (mark.CasterId == caster.Id &&
                    effect.Zone.IsCellInZone!(mark.MainCell, fightContext.TargetedCell,
                        caster.GetCurrentPositionCell()) && (effect.Param3 == 0 || mark.FromSpell?.Id == effect.Param3))
                {
                    AddEffectMarkTrigger(fightContext, mark);
                    DamageCalculator.ExecuteMarkSpell(null, mark, runningEffect, fightContext, isTriggered);
                }
            }
        }
    }

    public static void HandleForceTrapTrigger(FightContext fightContext, RunningEffect runningEffect, bool isTriggered,
        HaxeSpellEffect effect, HaxeFighter caster)
    {
        var marks = fightContext.Map
                                .GetMarks(false, GameActionMarkType.Trap)
                                .Where(x => !x.Aura);

        foreach (var mark in marks)
        {
            if (mark.CasterId == caster.Id &&
                effect.Zone.IsCellInZone!(mark.MainCell, fightContext.TargetedCell, caster.GetCurrentPositionCell()))
            {
                DamageCalculator.ExecuteMarkSpell(null, mark, runningEffect, fightContext, isTriggered);
            }
        }
    }

    public static void HandleAddTrap(FightContext fightContext, RunningEffect runningEffect)
    {
        if (!fightContext.Map.IsCellWalkable(fightContext.TargetedCell))
        {
            return;
        }

        // check if someone is already on the cell
        var mark = fightContext.Map.GetMarkOnCenter(fightContext.TargetedCell, false, GameActionMarkType.Trap);

        if (mark != null && runningEffect.GetSpell().NeedFreeTrapCell)
        {
            return;
        }

        var spell = DamageCalculator.DataInterface.CreateSpellFromId(runningEffect.SpellEffect.Param1,
            runningEffect.SpellEffect.Param2);

        mark = Mark.CreateMark(fightContext, fightContext.GetFreeMarkId(), GameActionMarkType.Trap,
            runningEffect.GetCaster().TeamId,
            fightContext.TargetedCell, runningEffect.GetCaster(), false, false, spell, runningEffect, true);

        if (fightContext.IsSimulation)
        {
            runningEffect.GetCaster().AddPendingEffects(EffectOutput.FromMarkAdded(mark.CasterId, mark.MarkId, mark.GetActionTrigger(), mark));
            return;
        }

        fightContext.Map.AddMark(mark);
    }

    public static void HandleAddRune(FightContext fightContext, RunningEffect runningEffect)
    {
        if (!fightContext.Map.IsCellWalkable(fightContext.TargetedCell))
        {
            return;
        }

        var mark = fightContext.Map.GetMarkOnCenter(fightContext.TargetedCell, false, GameActionMarkType.Rune);

        if (mark != null)
        {
            mark.IsDeleted = true;
        }

        var spell = DamageCalculator.DataInterface.CreateSpellFromId(runningEffect.SpellEffect.Param1,
            runningEffect.SpellEffect.Param2);

        var newMark = Mark.CreateMark(fightContext,
            fightContext.GetFreeMarkId(),
            GameActionMarkType.Rune,
            runningEffect.GetCaster().TeamId,
            fightContext.TargetedCell,
            runningEffect.GetCaster(),
            false,
            false,
            spell,
            runningEffect,
            true);

        if (fightContext.IsSimulation)
        {
            runningEffect.GetCaster().AddPendingEffects(EffectOutput.FromMarkAdded(newMark.CasterId, newMark.MarkId, newMark.GetActionTrigger(), newMark));
            return;
        }

        fightContext.Map.AddMark(newMark);
    }

    public static void HandleAddPortal(FightContext fightContext, RunningEffect runningEffect)
    {
        if (fightContext.IsSimulation)
        {
            return;
        }

        if (!fightContext.Map.IsCellWalkable(fightContext.TargetedCell))
        {
            return;
        }

        var mark = fightContext.Map.GetMarkOnCenter(fightContext.TargetedCell, false);

        if (mark != null)
        {
            mark.IsDeleted = true;
        }

        var spell = DamageCalculator.DataInterface.CreateSpellFromId((int)SpellId.Teleportail, 1);

        var newMark = Mark.CreateMark(fightContext,
            fightContext.GetFreeMarkId(),
            GameActionMarkType.Portal,
            runningEffect.GetCaster().TeamId,
            fightContext.TargetedCell,
            runningEffect.GetCaster(),
            false,
            false,
            spell,
            runningEffect,
            true);

        var marks = fightContext.Map.GetMarks(false, GameActionMarkType.Portal, runningEffect.GetCaster().TeamId);

        if (marks.Count >= 4) // max portals is 4
        {
            var firstPortal = marks.FirstOrDefault(x => x is { IsDeleted: false, });

            if (firstPortal != null)
            {
                firstPortal.IsDeleted = true;
            }
        }

        if (marks.Count == 0)
        {
            newMark.Active = false;
        }

        fightContext.Map.AddMark(newMark);
        RedefinePortals(fightContext);
    }

    public static void RemoveBombWall(FightContext fightContext, HaxeFighter bomb)
    {
        if (fightContext.IsSimulation)
        {
            return;
        }

        if (!bomb.IsBomb())
        {
            return;
        }

        var marks = fightContext.Map.GetMarks(false, GameActionMarkType.Wall, bomb.TeamId);
        
        // check the marks that are linked to the bomb
        
        var possibleLinkedBombs = fightContext.Fighters
                                              .Where(x => x.IsBomb() && x.IsLinkedBomb(bomb) && bomb.Breed == x.Breed)
                                              .ToArray();
        
        var bombsInZone = fightContext.GetFightersFromZone(DamageCalculator.WallZone, bomb.GetCurrentPositionCell(), bomb.GetCurrentPositionCell());
        
        bombsInZone = bombsInZone.Where(x => x.IsBomb() && possibleLinkedBombs.Contains(bomb) && x != bomb && bomb.Breed == x.Breed)
                                 .ToList();

        // we have to remove every marks between this and other bombs
        foreach (var possibleBomb in bombsInZone)
        {
            var markCells = DamageCalculator.WallZoneLine.GetCells!(bomb.GetCurrentPositionCell(),
                possibleBomb.GetCurrentPositionCell());
            
            foreach (var cell in markCells)
            {
                var fighterOnCell = fightContext.GetFighterFromCell(cell);

                if (fighterOnCell == possibleBomb)
                {
                    break;
                }

                if (fighterOnCell != null && fighterOnCell.IsBomb() && fighterOnCell.Breed == bomb.Breed)
                {
                    break;
                }

                var mark = marks.FirstOrDefault(x => x.MainCell == cell && x.CasterId == bomb.GetSummoner(fightContext)!.Id);

                if (mark != null)
                {
                    mark.Active = false;
                }
            }
        }
    }
    
    public static int GetFreeMarkId(List<int> excludeIds)
    {
        var newId    = 1;
        var isUnique = false;

        while (!isUnique)
        {
            isUnique = true;
            var allMarks = excludeIds;

            if (allMarks.All(mark => newId != mark))
            {
                continue;
            }

            newId++;
            isUnique = false;
        }

        return newId;
    }

    public static void RedefineBombWall(FightContext fightContext)
    {
        if (fightContext.IsSimulation)
        {
            return;
        }

        var marks      = new List<Mark>();

        foreach (var bomb in fightContext.Fighters.Where(x => x.IsBomb()).ToArray())
        {
            if (bomb.IsCarried())
            {
                continue;
            }
            
            var summoner = bomb.GetSummoner(fightContext)!;

            var possibleLinkedBombs = fightContext.Fighters
                                                  .Where(x => x.IsBomb() && x.IsLinkedBomb(bomb) && bomb.Breed == x.Breed)
                                                  .ToArray();

            if (possibleLinkedBombs.Length == 0)
            {
                break;
            }

            var bombsInZone = fightContext.GetFightersFromZone(DamageCalculator.WallZone, bomb.GetCurrentPositionCell(),
                bomb.GetCurrentPositionCell());

            bombsInZone = bombsInZone.Where(x =>
                x.IsBomb() && possibleLinkedBombs.Contains(bomb) && x != bomb && bomb.Breed == x.Breed).ToList();

            bombsInZone.Sort((a, b) => TargetManagement.ComparePositions(bomb.GetCurrentPositionCell(),
                false, a.GetCurrentPositionCell(), b.GetCurrentPositionCell()));

            var markIds = fightContext.Map.GetMarks(false, GameActionMarkType.Wall, bomb.TeamId)
                                      .Select(x => x.MarkId)
                                      .ToList();
            
            foreach (var possibleBomb in bombsInZone)
            {
                var markCells = DamageCalculator.WallZoneLine.GetCells!(bomb.GetCurrentPositionCell(),
                    possibleBomb.GetCurrentPositionCell());
                
                if (possibleBomb.IsCarried())
                {
                    continue;
                }
                
                foreach (var cell in markCells)
                {
                    var fighterOnCell = fightContext.GetFighterFromCell(cell);

                    if (fighterOnCell == possibleBomb)
                    {
                        break;
                    }

                    if (fighterOnCell != null && fighterOnCell.IsBomb() && fighterOnCell.Breed == bomb.Breed)
                    {
                        break;
                    }

                    if (marks.Any(x => x.MainCell == cell && x.CasterId == summoner.Id && !x.IsDeleted))
                    {
                        continue;
                    }

                    var spell = DamageCalculator.DataInterface.GetBombWallSpellFromFighter(bomb);
                    
                    var newMark = Mark.CreateMark(fightContext,
                        GetFreeMarkId(markIds),
                        GameActionMarkType.Wall,
                        bomb.TeamId,
                        cell,
                        summoner,
                        false,
                        false,
                        spell,
                        null,
                        true);
                    
                    markIds.Add(newMark.MarkId);
                    newMark.IsNew = true;

                    var marksOnCell = fightContext.Map.GetMarkInteractingWithCell(cell, false, GameActionMarkType.Wall);
                    var markOnCell = marksOnCell.FirstOrDefault(x =>
                        x.MainCell == cell && x.CasterId == summoner.Id && x.AssociatedSpell?.Id == spell?.Id);

                    if (markOnCell != null)
                    {
                        newMark.IsNew     = false;
                        newMark.MarkId    = markOnCell.MarkId;
                        newMark.Active = true;
                    }

                    marks.Add(newMark);
                }
            }
        }

        foreach (var mark in fightContext.Map.GetMarks(false, GameActionMarkType.Wall).Where(x => !x.IsDeleted))
        {
            if (!marks.Any(x => x.MarkId == mark.MarkId))
            {
                mark.IsDeleted = true;
            }
            else
            {
                marks.Remove(marks.First(x => x.MarkId == mark.MarkId));
            }
        }

        foreach (var mark in marks)
        {
            fightContext.Map.AddMark(mark);
            if (mark.IsNew)
            {
                DamageCalculator.ExecuteWallOnEveryFighter(mark, null, fightContext, false);
            }
        }
    }

    public static void RedefinePortals(FightContext fightContext)
    {
        if (fightContext.IsSimulation)
        {
            return;
        }

        foreach (var mark in fightContext.Map.GetMarks(false, GameActionMarkType.Portal).Where(x => !x.IsDeleted))
        {
            var fighterOnCell =
                fightContext.Fighters.FirstOrDefault(x => x.IsAlive() && x.GetCurrentPositionCell() == mark.MainCell);

            if (fighterOnCell != null || mark.Used)
            {
                mark.Active         = false;
                mark.IsStateUpdated = true;
                continue;
            }

            var portalsChain = PortalUtils.GetPortalChainFromPortals(mark,
                fightContext.Map.GetMarks(false, GameActionMarkType.Portal, (int)mark.TeamId)
                            .Where(x => x is { IsDeleted: false, Used: false, })
                            .ToArray());

            // we need to be sure that the portal chain is not broken
            // so we have to remove the portals that have someone on it
            portalsChain.RemoveAll(x =>
                fightContext.Fighters.Any(
                    fighter => fighter.IsAlive() && fighter.GetCurrentPositionCell() == x.MainCell));

            if (portalsChain.Count == 0)
            {
                mark.Active         = false;
                mark.IsStateUpdated = true;
                continue;
            }


            mark.IsStateUpdated = true;
            mark.Active         = true;
        }
    }

    public static void HandleAddGlyph(FightContext fightContext, RunningEffect runningEffect)
    {
        if (!fightContext.Map.IsCellWalkable(fightContext.TargetedCell))
        {
            return;
        }

        var spell = DamageCalculator.DataInterface.CreateSpellFromId(runningEffect.SpellEffect.Param1,
            runningEffect.SpellEffect.Param2);

        var mark = Mark.CreateMark(fightContext,
            fightContext.GetFreeMarkId(),
            GameActionMarkType.Glyph,
            runningEffect.GetCaster().TeamId,
            fightContext.TargetedCell,
            runningEffect.GetCaster(),
            false,
            runningEffect.SpellEffect.ActionId == ActionId.FightAddGlyphCastingSpellEndturn,
            spell,
            runningEffect,
            true,
            runningEffect.SpellEffect.ActionId == ActionId.FightAddGlyphCastingSpellImmediate);

        if (fightContext.IsSimulation)
        {
            runningEffect.GetCaster().AddPendingEffects(EffectOutput.FromMarkAdded(mark.CasterId, mark.MarkId, mark.GetActionTrigger(), mark));
            return;
        }

        if (runningEffect.SpellEffect.ActionId == ActionId.FightAddGlyphCastingSpellImmediate)
        {
            //DamageCalculator.ExecuteGlyphOnEveryFighter(mark, null, fightContext, false);
        }

        fightContext.Map.AddMark(mark);
    }

    public static void HandleAddGlyphAura(FightContext fightContext, RunningEffect runningEffect)
    {
        if (!fightContext.Map.IsCellWalkable(fightContext.TargetedCell))
        {
            return;
        }

        var spell = DamageCalculator.DataInterface.CreateSpellFromId(runningEffect.SpellEffect.Param1,
            runningEffect.SpellEffect.Param2);

        var mark = Mark.CreateMark(fightContext,
            fightContext.GetFreeMarkId(),
            GameActionMarkType.Glyph,
            runningEffect.GetCaster().TeamId,
            fightContext.TargetedCell,
            runningEffect.GetCaster(),
            true,
            false,
            spell,
            runningEffect,
            true);
        

        if (fightContext.IsSimulation)
        {
            runningEffect.GetCaster().AddPendingEffects(EffectOutput.FromMarkAdded(mark.CasterId, mark.MarkId, mark.GetActionTrigger(), mark));
            return;
        }

        DamageCalculator.ExecuteGlyphOnEveryFighter(mark, null, fightContext, false);

        fightContext.Map.AddMark(mark);
    }

    public static bool HandleCasterExecuteSpellOnCell(FightContext fightContext, RunningEffect runningEffect,
        HaxeFighter caster, bool isTriggered)
    {
        return DamageCalculator.HandleSpellExecution(fightContext, runningEffect, caster, null, isTriggered);
    }

    public static DamageRange GenerateDefaultDamage(FightContext fightContext, RunningEffect runningEffect,
        bool isTriggered, HaxeFighter caster, HaxeSpellEffect effect)
    {
        var mustOverrideCaster = caster.PlayerType == PlayerType.Monster && caster.Data.IsSummon() &&
                                 HaxeFighter.BombBreedId.Contains(caster.Breed) ||
                                 caster.PlayerType == PlayerType.Monster && caster.Data.IsSummon() &&
                                 HaxeFighter.SteamerTurretBreedId.Contains(caster.Breed);

        if (mustOverrideCaster)
        {
            runningEffect.OverrideCaster(runningEffect.GetCaster().GetSummoner(fightContext)!);
        }

        DamageRange damage;

        if (ActionIdHelper.IsDamage(effect.Category, effect.ActionId) || ActionIdHelper.IsHeal(effect.ActionId))
        {
            damage = DamageSender.GetTotalDamage(fightContext, runningEffect, isTriggered);
        }
        else if (ActionIdHelper.IsShield(effect.ActionId))
        {
            damage = DamageSender.GetTotalShield(runningEffect);
        }
        else
        {
            damage = DamageRange.Zero;
        }

        damage.Probability = runningEffect.Probability;

        if (caster != runningEffect.GetCaster())
        {
            runningEffect.OverrideCaster(caster);
        }

        return damage;
    }

    public static HaxeFighter? HandleSummoningWithoutTarget(FightContext fightContext, IList<HaxeFighter>? targetList,
        HaxeSpellEffect effect, HaxeFighter caster, HaxeSpell spell)
    {
        if (effect.ActionId != ActionId.SummonBomb)
        {
            if (targetList != null)
            {
                targetList.Clear();
            }
            else
            {
                targetList = new List<HaxeFighter>();
            }
        }

        HaxeFighter? summon = null;
        var targetedCell = fightContext.TargetedCell;
        
        var fighterOnTargetedCell = fightContext.GetFighterFromCell(targetedCell);
        
        var summonAvailable = Math.Max(0,
            caster.Data.GetCharacteristicValue(StatId.MaxSummonedCreaturesBoost) -
            fightContext.GetFighterCurrentSummonCount(caster));

        var tempTargets = new List<HaxeFighter>();

        if (fightContext.Map.IsCellWalkable(targetedCell))
        {
            if (effect.ActionId == ActionId.CharacterAddIllusionMirror || effect.Zone.Radius > 1 || effect.Zone.Shape == ';' || effect.Zone.Shape == 'T')
            {
                var possibleCells = new List<int>();

                if (effect.ActionId == ActionId.CharacterAddIllusionMirror)
                {
                    if (fighterOnTargetedCell != null && fighterOnTargetedCell.IsAlive())
                    {
                        return null;
                    }

                    var casterCoord = MapTools.GetCellCoordById(caster.GetCurrentPositionCell())!.Value;
                    var distance    = MapTools.GetDistance(caster.GetCurrentPositionCell(), targetedCell);

                    var possiblePoints = new[]
                    {
                        casterCoord with { X = casterCoord.X + distance, },
                        casterCoord with { X = casterCoord.X - distance, },
                        casterCoord with { Y = casterCoord.Y + distance, },
                        casterCoord with { Y = casterCoord.Y - distance, },
                    };

                    possibleCells.AddRange(possiblePoints.Select(point => MapTools.GetCellIdByCoord(point.X, point.Y)));
                }
                else if (effect.Zone.Radius is 63)
                {
                    var cells = effect.Zone.GetCells!(targetedCell, caster.GetCurrentPositionCell());
                    possibleCells.AddRange(cells.ToArray());
                }
                else if (effect.Zone.Radius > 1 || effect.Zone.Shape == ';' || effect.Zone.Shape == 'T')
                {
                    possibleCells.AddRange(effect.Zone.GetCells!(targetedCell, caster.GetCurrentPositionCell()));
                }

                var possibleCellsArr = possibleCells.ToArray();
                //Random.Shared.Shuffle(possibleCellsArr);

                Array.Sort(possibleCellsArr,
                    (a, b) =>
                        TargetManagement.ComparePositions(caster.GetCurrentPositionCell(), false, 
                            a,
                            b));

                var spawnCapabilities = effect.Param3;

                if (spawnCapabilities == 0)
                {
                    spawnCapabilities = 1;
                }
                
                foreach (var cell in possibleCellsArr)
                {
                    if (!fightContext.Map.IsCellWalkable(cell) || fightContext.GetFighterFromCell(cell) != null)
                    {
                        continue;
                    }
                    
                    if (summonAvailable <= 0 || spawnCapabilities <= 0)
                    {
                        break;
                    }
                    
                    summon = DamageCalculator.Summon(effect, fightContext, caster, spell, cell);
                    
                    if (summon == null)
                    {
                        continue;
                    }
                    
                    tempTargets.Add(summon);
                    spawnCapabilities--;

                    if (summon.Data.UseSummonSlot())
                    {
                        summonAvailable--;
                        
                        if (summonAvailable <= 0)
                        {
                            break;
                        }
                    }
                }
            }
            else
            {
                if (fighterOnTargetedCell != null && fighterOnTargetedCell.IsAlive())
                {
                    return null;
                }

                summon = DamageCalculator.Summon(effect, fightContext, caster, spell, targetedCell);
                if (summon != null)
                {
                    if (!summon.Data.UseSummonSlot() || summonAvailable > 0)
                    {
                        tempTargets.Add(summon);
                    }
                }
            }
        }

        if (targetList == null)
        {
            targetList = new List<HaxeFighter>();
        }

        foreach (var fighter in tempTargets)
        {
            targetList.Add(fighter);
        }

        return summon;
    }

    public static List<EffectOutput> HandleDelayedCast(FightContext fightContext, RunningEffect runningEffect,
        bool isTriggered, bool isPreview, HaxeFighter target, HaxeFighter? summon)
    {
        List<EffectOutput> resultOutput = new();

        var buff = HaxeBuff.FromRunningEffect(target, runningEffect);
        buff.TargetedCell = fightContext.TargetedCell;

        var effectOutput = target.StorePendingBuff(buff);
        if (effectOutput != null)
        {
            resultOutput.Add(effectOutput);
        }

        resultOutput.Add(EffectOutput.FromBuffAdded(target.Id, runningEffect.Caster.Id,
            runningEffect.SpellEffect.ActionId, buff));
        DamageCalculator.HandleAffectedTarget(fightContext, runningEffect, target, resultOutput, isPreview,
            isTriggered);
        return resultOutput;
    }

    public static List<EffectOutput> HandleDispatchLifePointsPercent(FightContext fightContext,
        RunningEffect runningEffect, bool isTriggered, bool isPreview, HaxeFighter caster,
        DamageRange currentDamageRange, HaxeFighter? summon)
    {
        var damageBasedOnTargetLife = DamageReceiver.GetDamageBasedOnTargetLife(runningEffect.GetSpellEffect(), caster,
            (DamageRange)currentDamageRange.Copy());

        damageBasedOnTargetLife.IsHeal = false;
        var resultOutput =
            DamageReceiver.ReceiveDamageOrHeal(fightContext, runningEffect, damageBasedOnTargetLife, caster);

        DamageCalculator.HandleAffectedTarget(fightContext, runningEffect, caster, resultOutput, isPreview,
            isTriggered);
        return resultOutput;
    }

    public static void HandleAoeMalus(FightContext fightContext, IList<HaxeFighter>? additionalTargets,
        HaxeFighter target, HaxeSpellEffect effect, HaxeFighter caster,
        DamageRange currentDamageAdditional)
    {
        var efficiency = 1d;

        if ((additionalTargets == null || !additionalTargets.Contains(target)) &&
            ActionIdHelper.AllowAoeMalus(effect.ActionId))
        {
            var malus = DamageCalculator.GetAoeMalus(effect, fightContext.TargetedCell, caster, target);

            efficiency *= malus;
        }

        if (fightContext.UsingPortal())
        {
            efficiency *= 1 + fightContext.GetPortalBonus(effect.ActionId) * 0.01;
        }

        currentDamageAdditional.Multiply(efficiency);
    }

    public static List<EffectOutput> HandleDispellSpell(HaxeFighter target, HaxeFighter caster, HaxeSpellEffect effect)
    {
        return target.RemoveBuffBySpellId(caster, effect.Param3, effect.Param1);
    }

    public static void HandleBombActivation(FightContext fightContext, RunningEffect runningEffect, HaxeFighter target)
    {
        var bombs = TargetManagement.GetBombsAboutToExplode(target, fightContext, runningEffect);

        foreach (var bomb in bombs)
        {
            var bombExplosionSpell = DamageCalculator.DataInterface.GetBombExplosionSpellFromFighter(bomb);
            if (bombExplosionSpell == null)
            {
                continue;
            }

            var newContext = fightContext.Copy();
            newContext.TargetedCell = bomb.GetCurrentPositionCell();
            DamageCalculator.ExecuteSpell(newContext, bomb, bombExplosionSpell, runningEffect.ForceCritical,
                runningEffect);

            bomb.RemoveBuffByActionId(ActionId.BombComboBonus);
        }

        runningEffect.Caster.RemoveBuffByActionId(ActionId.BombComboBonus);
    }

    public static void HandleMultiplyReceivedHeal(RunningEffect runningEffect, HaxeSpellEffect effect)
    {
        var value                  = effect.Param1 * 0.01;
        var triggeringOutputDamage = runningEffect.TriggeringOutput?.DamageRange;
        triggeringOutputDamage?.Multiply(value);
    }

    public static void HandleMarkDispell(FightContext fightContext, HaxeSpellEffect effect, HaxeFighter caster)
    {
        if (fightContext.IsSimulation)
        {
            return;
        }

        var markType = effect.ActionId switch
                       {
                           ActionId.DispelGlyphsOfTarget => GameActionMarkType.Glyph,
                           ActionId.DispelTrapsOfTarget  => GameActionMarkType.Trap,
                           ActionId.DispelRunesOfTarget  => GameActionMarkType.Rune,
                           _                                   => GameActionMarkType.None,
                       };

        var marks = fightContext.Map.GetMarks(false, markType, caster.TeamId);
        foreach (var mark in marks)
        {
            if (mark.CasterId == caster.Id && (effect.Param1 == 0 || effect.Param1 == mark.AssociatedSpell?.Id))
            {
                mark.Active    = false;
                mark.IsDeleted = true;
            }
        }
    }


    public static List<EffectOutput> HandleStatModifier(RunningEffect runningEffect, HaxeFighter target,
        HaxeSpellEffect effect, DamageRange? currentDamageAdditional)
    {
        List<EffectOutput> resultOutput = new();

        var buff = HaxeBuff.FromRunningEffect(target, runningEffect);

        if (runningEffect.IsTriggered)
        {
            buff.Effect          = buff.Effect.Clone();
            buff.Effect.Triggers = new[] { "I", };
        }

        if (ActionIdHelper.IsShield(effect.ActionId))
        {
            buff.DisplayActionId = ActionId.CharacterBoostShield;
            buff.Effect.Param1   = currentDamageAdditional!.Min;

            resultOutput.Add(EffectOutput.FromDamageRange(target.Id, runningEffect.Caster.Id, effect.ActionId,
                currentDamageAdditional));
        }
        else if (ActionIdHelper.IsHealBonus(effect.ActionId))
        {
            buff.DisplayActionId = ActionId.CharacterBoostVitality;
            buff.Effect.Param1   = currentDamageAdditional!.Min;
        }
        else if (ActionIdHelper.IsHealMalus(effect.ActionId))
        {
            buff.DisplayActionId = ActionId.CharacterDeboostVitality;
            buff.Effect.Param1   = -currentDamageAdditional!.Min;
        }
        else if (ActionIdHelper.IsFakeDamage(effect.ActionId))
        {
            buff.DisplayActionId = ActionId.CharacterLifePointsMalus;
            buff.Effect.Param1   = currentDamageAdditional!.Min;
        }
        else if (effect.ActionId == ActionId.CharacterBoostThreshold)
        {
            var lifePoints = (int)(buff.Effect.Param2 / 100d * target.GetMaxLifeWithoutContext());
            buff.Effect.Param1 = lifePoints;
            //buff.Effect.Param3 = lifePoints;
        }
        else if (effect.ActionId is ActionId.CharacterActionPointsLost)
        {
            // resultOutput.Add(EffectOutput.FromApLost(target.Id, runningEffect.Caster.Id, effect.ActionId, effect.GetDamageInterval().Min));
        }
        else if (effect.ActionId is ActionId.FightDisableState)
        {
            if (buff.Effect.TurnDuration <= -100)
            {
                buff.Effect.Duration     = 1;
                buff.Effect.TurnDuration = 1;
            }
        }
        else if (effect.ActionId is ActionId.CharacterBoostRange or ActionId.CharacterDeboostRange)
        {
            var effectOutputRange = new EffectOutput(target.Id, runningEffect.Caster.Id, effect.ActionId);
            var value             = effect.GetDamageInterval().Min;

            if (effect.ActionId is ActionId.CharacterDeboostRange)
            {
                effectOutputRange.RangeLoss = value;
            }
            else
            {
                effectOutputRange.RangeGain = value;
            }

            resultOutput.Add(effectOutputRange);
        }

        var effectOutput = target.StorePendingBuff(buff);
        if (effectOutput != null)
        {
            resultOutput.Add(effectOutput);
        }

        resultOutput.Add(EffectOutput.FromBuffAdded(target.Id, runningEffect.Caster.Id, effect.ActionId, buff));

        var lf = resultOutput.FirstOrDefault(x => x.ActionId == ActionId.CharacterLifePointsMalus);
        var offsetPdv = lf?.StatValue ?? 0;
        
        if (currentDamageAdditional != null && !currentDamageAdditional.IsHeal && !currentDamageAdditional.IsShieldDamage &&
            (target.GetPendingLifePoints().Max - offsetPdv) - currentDamageAdditional.Min <= 0)
        {
            if(!target.Data.HasGod())
            {
                resultOutput = resultOutput.Concat([EffectOutput.DeathOf(target.Id, runningEffect.Caster.Id, runningEffect.SpellEffect.ActionId, false)]).ToList();
            }
                
        }
        
        return resultOutput;
    }

    public static void HandleStatBoost(FightContext fightContext, RunningEffect runningEffect, HaxeSpellEffect effect,
        HaxeFighter target, HaxeFighter caster)
    {
        var statBoostToActionId   = ActionIdHelper.StatBoostToBuffActionId(effect.ActionId);
        var statDeBoostToActionId = ActionIdHelper.StatBoostToDebuffActionId(effect.ActionId);

        if (statBoostToActionId == 0 || statDeBoostToActionId == 0)
        {
            return;
        }

        var targetRunningEffect = runningEffect.Copy();
        targetRunningEffect.SpellEffect.ActionId = statDeBoostToActionId;

        var effects = DamageCalculator.ComputeEffect(fightContext,
            targetRunningEffect,
            false,
            new List<HaxeFighter>
            {
                target,
            },
            null);

        var effectLost = effects.LastOrDefault(x => x.ActionId == statDeBoostToActionId && x.StatValue < 0);
        var value = 0;
        
        if (statDeBoostToActionId == ActionId.CharacterDeboostMovementPointsDodgeable)
        {
            effectLost = effects.LastOrDefault(x => x.ActionId == statDeBoostToActionId && x.AmStolen > 0);
            if(effectLost != null)
            {
                value = effectLost.AmStolen;
            }
        }
        else if (statDeBoostToActionId == ActionId.CharacterDeboostActionPointsDodgeable)
        {
            effectLost = effects.LastOrDefault(x => x.ActionId == statDeBoostToActionId && x.ApStolen > 0);
            if(effectLost != null)
            {
                value = effectLost.ApStolen;
            }
        }
        else
        {
            // if for some reason the target was not deboosted then we don't boost the caster because it's a not steal
            // or it has been dodged
            if (effectLost is not { StatValue: < 0, })
            {
                return;
            }
            
            value = effectLost.StatValue;
        }
        
        if (effectLost == null)
        {
            return;
        }
        
        var casterRunningEffect = runningEffect.Copy();
        casterRunningEffect.SpellEffect.ActionId = statBoostToActionId;
        casterRunningEffect.SpellEffect.Param1 = Math.Abs(value);
        
        DamageCalculator.ComputeEffect(fightContext, casterRunningEffect, false,
            new List<HaxeFighter>
            {
                caster,
            }, null);
    }

    public static List<EffectOutput> HandleStatGain(HaxeSpellEffect effect, HaxeFighter target, HaxeFighter caster)
    {
        List<EffectOutput> resultOutput = new();

        switch (effect.ActionId)
        {
            case ActionId.CharacterActionPointsWin:
                resultOutput.Add(EffectOutput.FromApGain(target.Id, caster.Id, effect.ActionId,
                    effect.GetDamageInterval().Min));
                break;
            case ActionId.CharacterMovementPointsWin:
                resultOutput.Add(EffectOutput.FromAmGain(target.Id, caster.Id, effect.ActionId,
                    effect.GetDamageInterval().Min));
                break;
        }

        return resultOutput;
    }

    public static List<EffectOutput> HandleDodgeableApAm(HaxeSpellEffect effect, RunningEffect runningEffect, HaxeFighter target, HaxeFighter caster)
    {
        List<EffectOutput> resultOutput = new();

        var amount = effect.GetDamageInterval().Min;
        switch (effect.ActionId)
        {
            case ActionId.CharacterDeboostActionPointsDodgeable:
                amount = target.Data.RollApDodge(amount, caster);
                break;
            case ActionId.CharacterDeboostMovementPointsDodgeable:
                amount = target.Data.RollAmDodge(amount, caster);
                break;
        }

        var dodged = effect.GetDamageInterval().Min - amount;

        switch (effect.ActionId)
        {
            case ActionId.CharacterDeboostActionPointsDodgeable:
                resultOutput.Add(EffectOutput.FromApTheft(target.Id, caster.Id, effect.ActionId, amount, dodged));
                //resultOutput.Add(EffectOutput.FromApLost(target.Id, caster.Id, effect.ActionId, amount));
                break;
            case ActionId.CharacterDeboostMovementPointsDodgeable:
                resultOutput.Add(EffectOutput.FromAmTheft(target.Id, caster.Id, effect.ActionId, amount, dodged));
                //resultOutput.Add(EffectOutput.FromAmLost(target.Id, caster.Id, effect.ActionId, amount));
                break;
        }
        
        if(amount <= 0)
        {
            return resultOutput;
        }
        
        var newRunningEffect = runningEffect.Copy();
        newRunningEffect.SpellEffect.Param1 = amount;
        var buff = HaxeBuff.FromRunningEffect(target, newRunningEffect);
        
        //resultOutput.Add(EffectOutput.FromStatUpdate(target.Id, caster.Id, effect.ActionId, -1, -amount));
        target.StorePendingBuff(buff);
        resultOutput.Add(EffectOutput.FromBuffAdded(target.Id, runningEffect.Caster.Id, effect.ActionId, buff));

        return resultOutput;
    }

    public static List<EffectOutput> HandleHealAttackers(RunningEffect runningEffect, HaxeSpellEffect effect)
    {
        List<EffectOutput> resultOutput;

        var currentInterval    = effect.GetDamageInterval();
        var computedLifeDamage = runningEffect.TriggeringOutput?.ComputeLifeDamage();

        var parentEffectCaster = runningEffect.GetParentEffect() != null
            ? runningEffect.GetParentEffect()!.GetCaster()
            : null;

        if (computedLifeDamage != null && parentEffectCaster != null &&
            computedLifeDamage is { IsHeal: false, IsInvulnerable: false, } and not { Min: 0, Max: 0, })
        {
            computedLifeDamage.Multiply(currentInterval.Min);
            computedLifeDamage.Multiply(0.01);
            computedLifeDamage.IsHeal         = true;
            computedLifeDamage.IsShieldDamage = false;
            resultOutput =
            [
                EffectOutput.FromDamageRange(parentEffectCaster.Id, runningEffect.Caster.Id, effect.ActionId,
                    computedLifeDamage)
            ];
        }
        else
        {
            resultOutput = [];
        }

        return resultOutput;
    }

    public static List<EffectOutput> HandleSetState(RunningEffect runningEffect, HaxeFighter target,
        HaxeSpellEffect effect)
    {
        var buff = HaxeBuff.FromRunningEffect(target, runningEffect);
        
        if (runningEffect.IsTriggered)
        {
            buff.Effect.Triggers = ["I",];
        }

        var alreadyHasState = target.HasState(effect.GetMinRoll());
        
        target.StorePendingBuff(buff);

        List<EffectOutput> resultOutput = new()
        {
            EffectOutput.FromStateChange(target.Id, runningEffect.Caster.Id, runningEffect.SpellEffect.ActionId,
                effect.Param3, true, alreadyHasState),
            EffectOutput.FromBuffAdded(target.Id, runningEffect.Caster.Id, runningEffect.SpellEffect.ActionId, buff),
        };

        return resultOutput;
    }

    public static List<EffectOutput> HandleUnsetState(HaxeFighter target, RunningEffect runningEffect,
        HaxeFighter caster)
    {
        var effect = runningEffect.SpellEffect;

        return target.RemoveState(effect.GetMinRoll());
    }

    public static List<EffectOutput> HandleShortenActiveEffectsDuration(HaxeFighter target, HaxeSpellEffect effect,
        HaxeFighter caster)
    {
        return target.ReduceBuffDurations(caster, effect.GetMinRoll());
    }

    public static List<EffectOutput> HandleSummon(FightContext fightContext, RunningEffect runningEffect,
        ref HaxeFighter? summon, List<EffectOutput> resultOutput, HaxeFighter caster, HaxeFighter target)
    {
        var effect = runningEffect.SpellEffect;

        if (ActionIdHelper.IsSummonWithoutTarget(effect.ActionId) && summon != null)
        {
            resultOutput = HandleSummonWithoutTarget(fightContext, caster, target, effect.ActionId);
        }
        else if (ActionIdHelper.IsKillAndSummon(effect.ActionId) &&
                 (!DamageCalculator.SummonTakesSlot(effect, fightContext, caster) ||
                  fightContext.GetFighterCurrentSummonCount(caster) - 1 <
                  caster.Data.GetCharacteristicValue(StatId.MaxSummonedCreaturesBoost)))
        {
            resultOutput = HandleKillAndSummon(fightContext, target, effect, caster, ref summon, runningEffect.Spell);
        }

        DamageCalculator.ExecuteMarks(fightContext, runningEffect, target, target.GetCurrentPositionCell(), false, fromDrag: true);

        return resultOutput;
    }

    private static List<EffectOutput> HandleSummonWithoutTarget(FightContext fightContext, HaxeFighter caster,
        HaxeFighter target, ActionId actionId)
    {
        var summonDirection =
            MapTools.GetLookDirection4(caster.GetCurrentPositionCell(), target.GetCurrentPositionCell());

        List<EffectOutput> resultOutput = new()
        {
            EffectOutput.FromSummon(target.Id, caster.Id, actionId, target.GetCurrentPositionCell(), summonDirection,
                -1),
            EffectOutput.FromSummoning(caster.Id, caster.Id, actionId, true),
        };

        // the player is tp into the summon
        if (actionId == ActionId.CharacterAddIllusionMirror &&
            target.GetCurrentPositionCell() == fightContext.TargetedCell)
        {
            resultOutput.Add(EffectOutput.FromInvisiblityStateChanged(caster.Id, caster.Id, actionId, true));
            resultOutput.Add(EffectOutput.FromMovement(caster.Id, caster.Id, actionId, target.GetCurrentPositionCell(),
                summonDirection));
        }

        return resultOutput;
    }

    private static List<EffectOutput> HandleKillAndSummon(FightContext fightContext, HaxeFighter target,
        HaxeSpellEffect effect, HaxeFighter caster, ref HaxeFighter? summon, HaxeSpell spell)
    {
        var effectOutputs = new List<EffectOutput>()
        {
            EffectOutput.DeathOf(target.Id, caster.Id, effect.ActionId, true),
        };

        target.PendingEffects.Add(effectOutputs[0]);
        summon = DamageCalculator.Summon(effect, fightContext, caster, spell);
        target.PendingEffects.Remove(effectOutputs[0]);

        if (summon != null)
        {
            var direction =
                MapTools.GetLookDirection4(caster.GetCurrentPositionCell(), summon.GetCurrentPositionCell());

            effectOutputs.Add(EffectOutput.FromSummon(summon.Id, caster.Id, effect.ActionId,
                summon.GetCurrentPositionCell(), direction));
            effectOutputs.Add(EffectOutput.FromSummoning(caster.Id, caster.Id, effect.ActionId, true));
        }

        return effectOutputs;
    }

    public static List<EffectOutput> HandleSpellReflector(FightContext fightContext, RunningEffect runningEffect,
        bool isTriggered,
        HaxeSpellEffect effect, HaxeFighter target, bool isMelee)
    {
        List<EffectOutput> resultOutput;

        var triggeringOutput = runningEffect.TriggeringOutput;
        var damageRange      = triggeringOutput?.DamageRange;
        var parentEffect     = runningEffect.GetParentEffect();

        var nCaster     = parentEffect?.GetCaster();
        var nSpellLevel = parentEffect != null ? parentEffect.GetSpell().Level : 1;
        var nIsWeapon   = parentEffect != null && parentEffect.GetSpell().IsWeapon;

        if (nCaster != null && parentEffect != null && damageRange != null && triggeringOutput != null &&
            triggeringOutput.DamageRange is { IsCollision: false, } &&
            nSpellLevel <= effect.Param2 && !nIsWeapon)
        {
            target.PendingEffects.Remove(triggeringOutput);
            resultOutput = DamageReceiver.ReceiveDamageOrHeal(fightContext, parentEffect,
                triggeringOutput.DamageRange,
                nCaster, /* not sure */
                isMelee,
                isTriggered);
        }
        else
        {
            resultOutput = new List<EffectOutput>();
        }

        return resultOutput;
    }


    public static void HandleRevealUnvisible(FightContext fightContext, RunningEffect runningEffect, HaxeFighter caster)
    {
        var cellsEffect =
            runningEffect.SpellEffect.Zone.GetCells!(caster.GetCurrentPositionCell(), fightContext.TargetedCell);

        foreach (var cell in cellsEffect)
        {
            var marks = fightContext.Map.GetMarkInteractingWithCell(cell, false).Where(x =>
                x.Visibility == GameActionFightInvisibilityStateEnum.Invisible && x.TeamId != caster.TeamId &&
                !x.IsDeleted);

            foreach (var mark in marks)
            {
                mark.IsUpdated  = true;
                mark.Visibility = GameActionFightInvisibilityStateEnum.Visible;
            }
        }
    }


    public static void HandleDisablePortal(FightContext fightContext, RunningEffect runningEffect, HaxeFighter caster)
    {
        if (runningEffect.SpellEffect.Zone.Shape == 'P')
        {
            var mark = fightContext.Map.GetMarkInteractingWithCell(fightContext.TargetedCell, false)
                                   .FirstOrDefault(x =>
                                       x.MarkType == GameActionMarkType.Portal && x.TeamId == caster.TeamId &&
                                       !x.IsDeleted);

            if (mark == null)
            {
                return;
            }

            mark.Use();
            mark.DisabledUntilThisFighterPlay = caster.Id;
        }
        else
        {
            var marks = fightContext.Map.GetMarks(false, GameActionMarkType.Portal, caster.TeamId);

            foreach (var mark in marks)
            {
                mark.Use();
                mark.DisabledUntilThisFighterPlay = caster.Id;
            }
        }

        RedefinePortals(fightContext);
    }


    public static void HandleUsePortal(FightContext fightContext, HaxeFighter caster)
    {
        var mark = fightContext.Map.GetMarkInteractingWithCell(fightContext.TargetedCell, false)
                               .FirstOrDefault(x =>
                                   x.MarkType == GameActionMarkType.Portal && x.TeamId == caster.TeamId &&
                                   !x.IsDeleted);

        if (mark == null)
        {
            return;
        }

        var fighterOnCell = fightContext.GetFighterFromCell(mark.MainCell);

        if (fighterOnCell != null)
        {
            UsePortal(fightContext, fighterOnCell, mark);
        }
    }

    public static bool UsePortal(FightContext fightContext, HaxeFighter fighter, Mark mark)
    {
        var outputPortalCell = fightContext.Map.GetOutputPortal(mark, out var usedPortal);

        if (outputPortalCell == -1 || usedPortal == null)
        {
            return false;
        }

        var outputMarks =
            fightContext.Map.GetMarkInteractingWithCell(outputPortalCell, true, GameActionMarkType.Portal);

        if (outputMarks.Count == 0)
        {
            return false;
        }

        if (!fighter.CanUsePortal())
        {
            return false;
        }

        fightContext.TriggeredMarks.Add(usedPortal.MarkId);
        fightContext.TriggeredMarks.Add(mark.MarkId);

        mark.Use();
        usedPortal.Use();

        fighter.SetCurrentPositionCell(outputPortalCell);
        fighter.PendingEffects.Add(EffectOutput.FromMovement(fighter.Id, mark.CasterId,
            ActionId.CharacterTeleportOnSameMap, outputPortalCell, -1));

        RedefinePortals(fightContext);
        return true;
    }
}