using System.Collections;
using Bubble.Core.Datacenter.Datacenter.Effects;
using Bubble.DamageCalculation.Customs;
using Bubble.DamageCalculation.DamageManagement;
using Bubble.DamageCalculation.FighterManagement;
using Bubble.DamageCalculation.SpellManagement;
using Bubble.DamageCalculation.Tools;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Bubble.DamageCalculation;

public static class DamageCalculator
{
    public static IDamageCalculationInterface DataInterface = null!;
    public static ILogger? Logger = null!;

    public const int MaxLoopIterations = 500;
    public static readonly SpellZone WallZone = SpellZone.FromRawZone("X7");
    public static readonly SpellZone WallZoneLine = SpellZone.FromRawZone("l1,7");

    private const bool IsDebug = false;

    public static List<string> DebugLogs = new();

    public static List<HaxeFighter> DamageComputation(FightContext fightContext, HaxeFighter fighter, HaxeSpell spell, bool isTriggered = false, bool forceCritical = false, bool debugMode = false)
    {
        fightContext.Map.ResetEffectCastCount();
        
        var targetedFighter = fightContext.GetFighterFromCell(fightContext.TargetedCell);

        if (spell.NeedsFreeCell && targetedFighter != null && targetedFighter.IsAlive() ||
            spell.NeedsTakenCell && (targetedFighter == null || !targetedFighter.IsAlive()) ||
            spell.NeedsVisibleEntity &&
            (targetedFighter == null || !targetedFighter.IsAlive() ||
             targetedFighter.IsInvisible()))
        {
            return new List<HaxeFighter>();
        }

        if (fighter.IsInvisible() && !spell.IsImmediateDamageInflicted(false))
        {
            var stateChangeOutput = EffectOutput.FromInvisibilityDetectedAtCell(fighter.Id, fighter.Id, ActionId.DecorsRevealUnvisible, fighter.GetCurrentPositionCell());
            fighter.PendingEffects.Add(stateChangeOutput);
        }
        else if (fighter.PlayerType != PlayerType.Monster && fighter.IsInvisible() && spell.IsImmediateDamageInflicted(false))
        {
            DispelInvisibility(fightContext, fighter, spell, isTriggered);
        }

        ExecuteSpell(fightContext, fighter, spell, forceCritical, null, isTriggered);
      
        foreach (var tempFighter in fightContext.Fighters)
        {
            tempFighter.SavePendingEffects();
        }

        return fightContext.GetAffectedFighters();
    }

    public static void DispelInvisibility(FightContext fightContext, HaxeFighter fighter, HaxeSpell spell, bool isTriggered)
    {
        var invisibilityStateEffect = EffectOutput.FromInvisiblityStateChanged(fighter.Id, fighter.Id, ActionId.DecorsRevealUnvisible, false);
        fighter.PendingEffects.Add(invisibilityStateEffect);
        var effectOutputs = fighter.RemoveState((int)SpellStateId.Invisible);

        foreach (var effectOutput in effectOutputs)
        {
            fighter.PendingEffects.Add(effectOutput);
        }

        TriggerHandler(new[]
        {
            invisibilityStateEffect,
        }, new RunningEffect(fightContext, fighter, spell, HaxeSpellEffect.Empty), fightContext, isTriggered);

        fighter.RemoveState((int)SpellStateId.Invisible);
    }

    /// <summary>
    /// Executes a spell based on the given parameters and applies its effects to the target(s).
    /// </summary>
    /// <param name="context">The fight context containing information about the current fight.</param>
    /// <param name="caster">The fighter casting the spell.</param>
    /// <param name="spell">The spell being cast.</param>
    /// <param name="isCritical">Indicates whether the spell is a critical hit or not.</param>
    /// <param name="parentEffect">The parent running effect, if any.</param>
    /// <param name="isTriggered">Indicates whether it's in preview mode.</param>
    /// <param name="isPreview">Indicates whether it's preview.</param>
    /// <param name="forceTarget"></param>
    /// <param name="additionalTarget"></param>
    public static IList<HaxeFighter> ExecuteSpell(FightContext context, 
        HaxeFighter caster, 
        HaxeSpell spell, 
        bool isCritical, 
        RunningEffect? parentEffect,
        bool isTriggered = false, 
        bool isPreview = false,
        HaxeFighter? forceTarget = null,
        HaxeFighter? additionalTarget = null)
    {
        if (parentEffect != null && !ShouldComputeTarget(parentEffect, caster))
        {
            return Array.Empty<HaxeFighter>();
        }

        if (parentEffect == null && (spell.CriticalHitProbability == 0 || spell.GetCriticalEffects().Count == 0))
        {
            isCritical = false;
        }

        var isCriticalEffect = isCritical && spell.GetCriticalEffects().Count > 0;

        if (isCriticalEffect && parentEffect != null && !ActionIdHelper.IsCriticalFlagInherited(parentEffect.GetSpellEffect().ActionId))
        {
            isCriticalEffect = false;
        }
        
        var targetedFighters = new List<HaxeFighter>();
        var effectsList      = (isCriticalEffect ? spell.GetCriticalEffects() : spell.GetEffects()).ToList();
        var isFromDeath      = parentEffect != null && AnyParentIsTriggeredByDeath(parentEffect);

        var targetedFightersIntMap = 
            GenerateTargets(context, 
                caster, 
                spell,
                parentEffect, 
                forceTarget, 
                additionalTarget, 
                effectsList, 
                isFromDeath,
                targetedFighters, 
                out var additionalTargetsIntMap);
        
        var lastRandomIndex = effectsList.FindLastIndex(effect => effect.RandomGroup > 0);
        var effectsBeforeRandom = lastRandomIndex == -1 ? effectsList : effectsList.GetRange(0, lastRandomIndex + 1);
        var effectsAfterRandom = lastRandomIndex == -1 ? [] : effectsList.GetRange(lastRandomIndex + 1, effectsList.Count - lastRandomIndex - 1);
        
        foreach (var effect in effectsBeforeRandom.Where(effect => effect.RandomWeight <= 0))
        {
            targetedFightersIntMap = 
                DamageCalculator.ExecuteEffect(context,
                    caster,
                    spell,
                    isCritical,
                    parentEffect,
                    isTriggered,
                    isPreview, 
                    forceTarget,
                    additionalTarget,
                    effect,
                    targetedFightersIntMap,
                    effectsList,
                    isFromDeath, 
                    targetedFighters,
                    ref additionalTargetsIntMap);
        }

        if (!spell.HasAtLeastOneRandomEffect() || effectsList.Count <= 0)
        {
            return targetedFighters.DistinctBy(x => x.Id).ToArray();
        }

        var randomGroups       = RandomGroup.CreateGroups(effectsList);
        var randomGroupEffects = RandomGroup.SelectRandomGroup(randomGroups);

        foreach (var effect in randomGroupEffects)
        {
            try
            {
                ExecuteEffect(context, 
                    caster, 
                    spell, 
                    isCritical,
                    parentEffect,
                    isTriggered,
                    isPreview,
                    effect,
                    targetedFightersIntMap,
                    additionalTargetsIntMap);
            }
            catch (Exception e)
            {
                Log.Error(e, "Error while executing effect {EffectId} for spell {SpellId}", effect.Id, spell.Id);
            }
        }

        foreach (var effect in effectsAfterRandom.Where(effect => effect.RandomWeight <= 0))
        {
            targetedFightersIntMap = 
                DamageCalculator.ExecuteEffect(context,
                    caster,
                    spell,
                    isCritical,
                    parentEffect,
                    isTriggered,
                    isPreview, 
                    forceTarget,
                    additionalTarget,
                    effect,
                    targetedFightersIntMap,
                    effectsList,
                    isFromDeath, 
                    targetedFighters,
                    ref additionalTargetsIntMap);
        }
        
        return targetedFighters.DistinctBy(x => x.Id).ToArray();
    }

    private static Dictionary<int, IList<HaxeFighter>> ExecuteEffect(
        FightContext context,
        HaxeFighter caster,
        HaxeSpell spell,
        bool isCritical,
        RunningEffect? parentEffect,
        bool isTriggered,
        bool isPreview,
        HaxeFighter? forceTarget,
        HaxeFighter? additionalTarget,
        HaxeSpellEffect effect,
        Dictionary<int, IList<HaxeFighter>> targetedFightersIntMap,
        List<HaxeSpellEffect> effectsList,
        bool isFromDeath,
        List<HaxeFighter> targetedFighters,
        ref Dictionary<int, IList<HaxeFighter>> additionalTargetsIntMap)
    {

        DamageCalculator.ExecuteEffect(context,
            caster,
            spell,
            isCritical, 
            parentEffect,
            isTriggered, 
            isPreview,
            effect,
            targetedFightersIntMap, 
            additionalTargetsIntMap);

        if(ActionIdHelper.IsRevive(effect.ActionId))
        {
            targetedFightersIntMap = DamageCalculator.GenerateTargets(context, 
                caster, 
                spell,
                parentEffect, 
                forceTarget, 
                additionalTarget, 
                effectsList, 
                isFromDeath,
                targetedFighters, 
                out additionalTargetsIntMap);
        }
        return targetedFightersIntMap;
    }

    private static Dictionary<int, IList<HaxeFighter>> GenerateTargets(FightContext context,
        HaxeFighter caster,
        HaxeSpell spell,
        RunningEffect? parentEffect,
        HaxeFighter? forceTarget,
        HaxeFighter? additionalTarget,
        List<HaxeSpellEffect> effectsList,
        bool isFromDeath, 
        List<HaxeFighter> targetedFighters,
        out Dictionary<int, IList<HaxeFighter>> additionalTargetsIntMap)
    {
        var targetedFightersIntMap  = new Dictionary<int, IList<HaxeFighter>>();
        additionalTargetsIntMap = new Dictionary<int, IList<HaxeFighter>>();
        
        for (var effectIndex = 0; effectIndex < effectsList.Count;)
        {
            var spellEffect = effectsList[effectIndex];

            var triggeringFighter = parentEffect?.TriggeringFighter;
            var targets =
                TargetManagement.GetTargets(context, 
                    caster,
                    spell,
                    spellEffect, 
                    triggeringFighter, 
                    isFromDeath, 
                    forceTarget: forceTarget, 
                    additionalTarget: additionalTarget);

            if (targets.IsUsed)
            {
                /*if (additionalTarget != null && (targets.TargetedFighters == null ||
                                                 !targets.TargetedFighters.Contains(additionalTarget)))
                {
                    targets.TargetedFighters ??= new List<HaxeFighter>();
                    targets.TargetedFighters.Add(additionalTarget);
                }*/
                
                targetedFightersIntMap[spellEffect.Id]  = targets.TargetedFighters ?? new List<HaxeFighter>();
                additionalTargetsIntMap[spellEffect.Id] = targets.AdditionalTargets ?? new List<HaxeFighter>();
                effectIndex++;

                if (targets.TargetedFighters != null)
                {
                    targetedFighters.AddRange(targets.TargetedFighters);
                }
            }
            else
            {
                effectsList.RemoveAt(effectIndex);
            }
        }

        return targetedFightersIntMap;
    }

    private static void ExecuteEffect(FightContext context, 
        HaxeFighter caster, 
        HaxeSpell spell,
        bool isCritical,
        RunningEffect? parentEffect, 
        bool isTriggered, 
        bool isPreview, 
        HaxeSpellEffect effect,
        Dictionary<int, IList<HaxeFighter>> targetedFightersIntMap,
        Dictionary<int, IList<HaxeFighter>> additionalTargetsIntMap,
        bool forceRefresh = false)
    {
        if (forceRefresh || effect.Masks.Contains("U") || effect.Masks.Contains("u") || effect.Masks.Contains("T") || effect.Masks.Contains("W") || effect.Masks.Contains("V") || effect.Masks.Contains("v"))
        {
            var target = TargetManagement.GetTargets(context, caster, spell, effect, parentEffect?.TriggeringFighter);
            targetedFightersIntMap[effect.Id]  = target.TargetedFighters ?? new List<HaxeFighter>();
            additionalTargetsIntMap[effect.Id] = target.AdditionalTargets ?? new List<HaxeFighter>();
            
            // target = TargetManagement.GetTargets(context, caster, spell, effect, parentEffect?.TriggeringFighter);
        }

        var currentEffect = new RunningEffect(context, caster, spell, effect);
        currentEffect.SetParentEffect(parentEffect);
        currentEffect.ForceCritical = isCritical || effect.IsCritical;
        
        ComputeEffect(context, currentEffect, isTriggered, targetedFightersIntMap[effect.Id], additionalTargetsIntMap[effect.Id], isPreview);
    }

    /// <summary>
    /// Computes the effects of a spell on target fighters.
    /// </summary>
    /// <param name="fightContext">The context of the current fight.</param>
    /// <param name="runningEffect">The running effect to process.</param>
    /// <param name="isTriggered">Flag indicating if the effect is triggered.</param>
    /// <param name="targetList">List of targets for the effect.</param>
    /// <param name="additionalTargets">List of additional targets for the effect.</param>
    /// <param name="isPreview">Flag indicating if this is a preview computation (optional, default is false).</param>
    public static IList<EffectOutput> ComputeEffect(FightContext fightContext, RunningEffect runningEffect, bool isTriggered, IList<HaxeFighter>? targetList, IList<HaxeFighter>? additionalTargets, bool isPreview = false)
    {
        if (runningEffect.Spell.Id is 14337 or 3868 or 6301)
        {
            return new List<EffectOutput>();
        }
        
        if (fightContext.Map.GetEffectCastCount() > MaxLoopIterations)
        {
            Log.Error("Too many effects cast in a single turn ({Spell})", runningEffect.Spell.Id);
            return new List<EffectOutput>();
        }
        
        fightContext.Map.IncrementEffectCastCount();
        
        var effect = runningEffect.GetSpellEffect();
        effect.ResetUseCount();

        var caster = runningEffect.GetCaster();

        HaxeFighter? summon = null;

        runningEffect.ForceCritical |= effect.IsCritical;

        // when the caster is different from the original caster, we reset the input portal cell id
        if (caster.Id != fightContext.OriginalCaster.Id && !(caster.Data.IsSummon() && caster.Data.GetSummonerId() == fightContext.OriginalCaster.Id))
        {
            fightContext.InputPortalCellId = -1;
        }

        DamageRange? currentDamageRange = null;

        var resultOutput = new List<EffectOutput>();

        // spell initialization
        if (SpellManager.IsInstantaneousSpellEffect(effect) || runningEffect.IsTriggered)
        {
            switch (effect.ActionId)
            {
                case ActionId.FightUsePortal:
                    DamageEffectHandler.HandleUsePortal(fightContext, caster);
                    return resultOutput;
                case ActionId.ForceRuneTrigger:
                    DamageEffectHandler.HandleForceRuneTrigger(fightContext, runningEffect, isTriggered, effect, caster);
                    return resultOutput;
                case ActionId.FightDisablePortal:
                    DamageEffectHandler.HandleDisablePortal(fightContext, runningEffect, caster);
                    return resultOutput;
                case ActionId.ForceGlyphTrigger:
                    DamageEffectHandler.HandleForceGlyphTrigger(fightContext, runningEffect, isTriggered, effect, caster);
                    return resultOutput;
                case ActionId.ForceTrapTrigger:
                    DamageEffectHandler.HandleForceTrapTrigger(fightContext, runningEffect, isTriggered, effect, caster);
                    return resultOutput;
                case ActionId.FightAddRuneCastingSpell:
                    DamageEffectHandler.HandleAddRune(fightContext, runningEffect);
                    return resultOutput;
                case ActionId.FightAddPortal:
                    DamageEffectHandler.HandleAddPortal(fightContext, runningEffect);
                    return resultOutput;
                case ActionId.FightAddGlyphCastingSpell:
                case ActionId.FightAddGlyphCastingSpellImmediate:
                case ActionId.FightAddGlyphCastingSpellEndturn:
                    DamageEffectHandler.HandleAddGlyph(fightContext, runningEffect);
                    return resultOutput;
                case ActionId.FightAddGlyphAura:
                    DamageEffectHandler.HandleAddGlyphAura(fightContext, runningEffect);
                    return resultOutput;
                case ActionId.FightAddTrapCastingSpell:
                    DamageEffectHandler.HandleAddTrap(fightContext, runningEffect);
                    break;
                case ActionId.CasterExecuteSpellOnCell:
                    if (!DamageEffectHandler.HandleCasterExecuteSpellOnCell(fightContext, runningEffect, caster, isTriggered))
                    {
                        return resultOutput;
                    }

                    break;
                case ActionId.DecorsRevealUnvisible:
                    DamageEffectHandler.HandleRevealUnvisible(fightContext, runningEffect, caster);
                    break;
            }

            if (ActionIdHelper.IsSummonWithoutTarget(effect.ActionId))
            {
                summon = DamageEffectHandler.HandleSummoningWithoutTarget(fightContext, targetList, effect, caster, runningEffect.Spell);
            }
            else
            {
                currentDamageRange = DamageEffectHandler.GenerateDefaultDamage(fightContext, runningEffect, isTriggered, caster, effect);
            }
        }

        if (targetList == null)
        {
            return resultOutput;
        }

        if (targetList.Count > 0 && effect.ActionId == ActionId.CharacterDispatchLifePointsPercent && currentDamageRange != null)
        {
            resultOutput = DamageEffectHandler.HandleDispatchLifePointsPercent(fightContext, runningEffect, isTriggered, isPreview, caster, currentDamageRange, summon);
        }

        var targets = targetList.ToArray();

        Array.Sort(targets, (param1, param2) => TargetManagement.ComparePositions(fightContext.TargetedCell,
            ActionIdHelper.IsPush(effect.ActionId),
            param1.GetCurrentPositionCell(),
            param2.GetCurrentPositionCell()));

        foreach (var target in targets)
        {
            if (!HandleTarget(fightContext, runningEffect, isTriggered, additionalTargets, isPreview, target, effect, summon, caster, currentDamageRange, out var effects))
            {
                continue;
            }

            resultOutput.AddRange(effects);
        }
        
        return resultOutput;
    }
    
    private static bool HandleTarget(FightContext fightContext, RunningEffect runningEffect, bool isTriggered, IList<HaxeFighter>? additionalTargets,
        bool isPreview, HaxeFighter target, HaxeSpellEffect effect, HaxeFighter? summon, HaxeFighter caster, DamageRange? currentDamageRange,
        out List<EffectOutput> resultOutput)
    {
        resultOutput = new List<EffectOutput>();

        if (ActionIdHelper.IsHealBonus(effect.ActionId) || ActionIdHelper.IsHealMalus(effect.ActionId))
        {
            currentDamageRange = DamageSender.GetTotalHealBonus(runningEffect, target);
        }
        
        if (!ShouldComputeTarget(runningEffect, target))
        {
            return true;
        }

        var isMelee = runningEffect.GetCaster() != target && MapTools.AreCellsAdjacent(runningEffect.GetCaster().GetCurrentPositionCell(), target.GetCurrentPositionCell());

        if (effect.Delay > 0 || !SpellManager.IsInstantaneousSpellEffect(effect) && !runningEffect.IsTriggered)
        {
            if (!target.IsAlive())
            {
                return true;
            }

            resultOutput = DamageEffectHandler.HandleDelayedCast(fightContext, runningEffect, isTriggered, isPreview, target, summon);
            return true;
        }

        if (effect.ActionId == ActionId.SummonBomb && summon == null || ActionIdHelper.IsSpellExecution(effect.ActionId))
        {
            if (!HandleSpellExecution(fightContext, runningEffect, caster, target, isTriggered))
            {
                return false;
            }

            return true;
        }

        var targetDamage = InitializeTargetDamage(fightContext, runningEffect, additionalTargets, effect, caster, target, currentDamageRange);

        if (effect.ActionId == ActionId.CharacterPassCurrentTurn)
        {
            resultOutput = new List<EffectOutput>()
            {
                EffectOutput.FromPassCurrentTurn(target.Id, caster.Id, effect.ActionId),
            };
        }
        else if (effect.ActionId == ActionId.CharacterDispellSpell)
        {
            resultOutput = DamageEffectHandler.HandleDispellSpell(target, caster, effect);
        }
        else if (effect.ActionId == ActionId.CharacterActivateBomb)
        {
            DamageEffectHandler.HandleBombActivation(fightContext, runningEffect, target);
        }
        else if (effect.ActionId == ActionId.CharacterMultiplyReceivedHeal)
        {
            DamageEffectHandler.HandleMultiplyReceivedHeal(runningEffect, effect);
        }
        else if (effect.ActionId is ActionId.CharacterAddSpellCooldown
                                    or ActionId.CharacterRemoveSpellCooldown
                                    or ActionId.CharacterSetSpellCooldown)
        {
            resultOutput = new List<EffectOutput>
            {
                EffectOutput.FromCooldown(target.Id, caster.Id, effect.ActionId, effect.Param1, effect.Param3),
            };
        }
        else if (effect.ActionId == ActionId.DecorsRevealUnvisible)
        {
            var invisibilityStateEffect = EffectOutput.FromInvisiblityStateChanged(target.Id, caster.Id, effect.ActionId, false);

            var list = target.RemoveState((int)SpellStateId.Invisible);

            resultOutput = new List<EffectOutput>
            {
                invisibilityStateEffect,
            };

            resultOutput.AddRange(list);
        }
        else if (ActionIdHelper.IsTargetMarkDispell(effect.ActionId))
        {
            DamageEffectHandler.HandleMarkDispell(fightContext, effect, caster);
            return false;
        }
        else if (effect.ActionId is ActionId.CharacterDeboostActionPointsDodgeable or ActionId.CharacterDeboostMovementPointsDodgeable)
        {
            resultOutput = DamageEffectHandler.HandleDodgeableApAm(effect, runningEffect, target, caster);
        }
        else if (ActionIdHelper.IsStatModifier(effect.ActionId))
        {
            resultOutput = DamageEffectHandler.HandleStatModifier(runningEffect, target, effect, targetDamage);
        }
        else if (ActionIdHelper.IsStatBoost(effect.ActionId))
        {
            DamageEffectHandler.HandleStatBoost(fightContext, runningEffect, effect, target, caster);
        }
        else if (ActionIdHelper.IsStatGain(effect.ActionId))
        {
            resultOutput = DamageEffectHandler.HandleStatGain(effect, target, caster);
        }
        else if (ActionIdHelper.IsTeleport(effect.ActionId))
        {
            resultOutput = Teleport.TeleportFighter(fightContext, runningEffect, target, isTriggered);
        }
        else if (effect.ActionId == ActionId.CarryCharacter)
        {
            resultOutput = Teleport.CarryFighter(fightContext, runningEffect, target);
        }
        else if (effect.ActionId == ActionId.ThrowCarriedCharacter)
        {
            resultOutput = Teleport.ThrowFighter(fightContext, runningEffect.GetCaster(), runningEffect, isTriggered);
        }
        else if (effect.ActionId == ActionId.CharacterKill)
        {
            resultOutput =
            [
                EffectOutput.DeathOf(target.Id, caster.Id, effect.ActionId, true),
            ];
        }
        else if (effect.ActionId == ActionId.CharacterPushUpTo)
        {
            resultOutput = PushUtils.PushTo(fightContext, runningEffect, target, false, false, isTriggered);
        }
        else if (effect.ActionId == ActionId.CharacterHealAttackers)
        {
            resultOutput = DamageEffectHandler.HandleHealAttackers(runningEffect, effect);
        }
        else if (effect.ActionId == ActionId.FightSetState)
        {
            resultOutput = DamageEffectHandler.HandleSetState(runningEffect, target, effect);
        }
        else if (effect.ActionId == ActionId.FightUnsetState)
        {
            resultOutput = DamageEffectHandler.HandleUnsetState(target, runningEffect, caster);
        }
        else if (effect.ActionId == ActionId.CharacterPullUpTo)
        {
            resultOutput = PushUtils.PullTo(fightContext, runningEffect, target, false, isTriggered);
        }
        else if (effect.ActionId == ActionId.CharacterShortenActiveEffectsDuration)
        {
            resultOutput = DamageEffectHandler.HandleShortenActiveEffectsDuration(target, effect, caster);
        }
        else if (ActionIdHelper.IsSummon(effect.ActionId))
        {
            resultOutput = DamageEffectHandler.HandleSummon(fightContext, runningEffect, ref summon, resultOutput, caster, target);
        }
        else if (effect.ActionId == ActionId.CharacterSpellReflector)
        {
            resultOutput = DamageEffectHandler.HandleSpellReflector(fightContext, runningEffect, isTriggered, effect, target, isMelee);
        }
        else if (ActionIdHelper.IsHeal(effect.ActionId))
        {
            resultOutput = DamageReceiver.ReceiveDamageOrHeal(fightContext, runningEffect, targetDamage!, target, /* not sure*/ isTriggered);
        }
        else if (ActionIdHelper.IsDamage(effect.Category, effect.ActionId))
        {
            resultOutput = DamageReceiver.ReceiveDamageOrHeal(fightContext, runningEffect, targetDamage!, target, isMelee, isTriggered);
        }
        else if (ActionIdHelper.IsPush(effect.ActionId))
        {
            resultOutput = PushUtils.Push(fightContext, runningEffect, target, effect.Param1, ActionIdHelper.IsForcedDrag(effect.ActionId), ActionIdHelper.AllowCollisionDamage(effect.ActionId), isTriggered);
        }
        else if (ActionIdHelper.IsPull(effect.ActionId))
        {
            resultOutput = PushUtils.Pull(fightContext, runningEffect, target, effect.Param1, ActionIdHelper.IsForcedDrag(effect.ActionId), isTriggered);
        }

        HandleAffectedTarget(fightContext, runningEffect, target, resultOutput, isPreview, isTriggered);
        return true;
    }

    private static object HandleTargetMarkDispell()
    {
        throw new NotImplementedException();
    }

    private static bool ShouldComputeTarget(RunningEffect runningEffect, HaxeFighter fighter)
    {
        if (runningEffect.GetSpellEffect().Triggers.Contains("X") || fighter.IsAlive() ||
            runningEffect.GetSpellEffect().RawZone.Length > 0 && runningEffect.GetSpellEffect().RawZone[0] == 'A' ||
            ActionIdHelper.IsRevive(runningEffect.SpellEffect.ActionId))
        {
            return true;
        }

        return AnyParentIsTriggeredByDeath(runningEffect);
    }

    private static bool AnyParentIsTriggeredByDeath(RunningEffect? runningEffect)
    {
        while (runningEffect != null)
        {
            if (runningEffect.GetSpellEffect().Triggers.Contains("X"))
            {
                return true;
            }
            runningEffect = runningEffect.GetParentEffect();
        }
        return false;
    }

    private static DamageRange? InitializeTargetDamage(FightContext fightContext, RunningEffect runningEffect,
        IList<HaxeFighter>? additionalTargets, HaxeSpellEffect effect,
        HaxeFighter caster, HaxeFighter target, DamageRange? damageRange)
    {
        DamageRange? damage = null;

        if (effect.ActionId == ActionId.CharacterLifePointsLostFromPush) // CollisionDamage
        {
            damage = PushUtils.GetCollisionDamage(fightContext, caster, target,
                effect.Param1, effect.Param2);
        }
        else if (ActionIdHelper.IsBasedOnTargetLife(effect.ActionId))
        {
            damage = DamageReceiver.GetDamageBasedOnTargetLife(runningEffect.GetSpellEffect(), target,
                (DamageRange)damageRange!.Copy());
        }
        else if (damageRange != null)
        {
            damage = (DamageRange)damageRange.Copy();
        }

        if (!ActionIdHelper.IsFakeDamage(effect.ActionId) &&
            effect.ActionId != ActionId.CharacterLifePointsLostFromPush && damage != null &&
            damage is not { Min: 0, Max: 0, })
        {
            DamageEffectHandler.HandleAoeMalus(fightContext, additionalTargets, target, effect, caster, damage);
        }

        return damage;
    }


    /// <summary>
    /// Handles the execution of a spell.
    /// </summary>
    /// <param name="fightContext">The FightContext instance representing the fight.</param>
    /// <param name="runningEffect">The RunningEffect instance representing the effect.</param>
    /// <param name="caster">The HaxeFighter instance representing the caster of the spell.</param>
    /// <param name="target">The HaxeFighter instance representing the target of the spell.</param>
    /// <param name="isTriggered">A boolean indicating whether the spell is triggered.</param>
    /// <returns>Returns a boolean indicating whether the spell was executed successfully.</returns>
    public static bool HandleSpellExecution(FightContext fightContext, RunningEffect runningEffect, HaxeFighter caster,
        HaxeFighter? target, bool isTriggered)
    {
        try
        {
            var spellEffect = runningEffect.GetSpellEffect();

            if (spellEffect.HasReachedMaxUseLimit())
            {
                return false;
            }

            spellEffect.RegisterUse();

            var fighterPositions = new Dictionary<HaxeFighter, int>();

            foreach (var fighter in fightContext.Fighters)
            {
                fighterPositions[fighter] = fighter.GetBeforeLastSpellPosition();
                fighter.SavePositionBeforeSpellExecution();
            }

            var executionResult = SolveSpellExecution(fightContext, runningEffect, target);

            if (executionResult != null)
            {
                executionResult.Caster?.PendingEffects.Add(
                    EffectOutput.FromSpellExecution(caster.Id,
                                                    caster.Id,
                                                    runningEffect.SpellEffect.ActionId,
                                                    executionResult));

                ExecuteSpell(executionResult.Context,
                             executionResult.Caster!,
                             executionResult.Spell,
                             executionResult.IsCritical,
                             runningEffect,
                             isTriggered /*!caster.Data.IsAlly()*/);

                // add new fighters to current context
                AddFightContextTempFighters(fightContext, executionResult.Context);

                executionResult.Caster?.PendingEffects.Add(
                    EffectOutput.FromSpellExecutionEnd(caster.Id,
                                                       caster.Id,
                                                       runningEffect.SpellEffect.ActionId,
                                                       executionResult.Spell.Id,
                                                       (short)executionResult.Spell.Level));
            }

            foreach (var entry in fighterPositions)
            {
                entry.Key.SetBeforeLastSpellPosition(entry.Value);
            }


            return true;
        }
        catch (Exception e)
        {
            Log.Error(e, "Error while handling spell execution");
            return false;
        }
    }


    /// <summary>
    /// Handles the effects on the affected target.
    /// </summary>
    /// <param name="fightContext">The FightContext instance representing the fight.</param>
    /// <param name="runningEffect">The RunningEffect instance representing the effect.</param>
    /// <param name="affectedFighter">The HaxeFighter instance representing the affected fighter.</param>
    /// <param name="effectOutputs">An array of EffectOutput instances representing the effect outputs.</param>
    /// <param name="unknown">A boolean indicating if the damage range is unknown.</param>
    /// <param name="oppositeTeam">A boolean indicating if the targets are on the opposite team.</param>
    public static void HandleAffectedTarget(FightContext fightContext, RunningEffect runningEffect,
        HaxeFighter affectedFighter, List<EffectOutput> effectOutputs, bool unknown, bool oppositeTeam)
    {
        var spellEffect        = runningEffect.GetSpellEffect();
        var hasSummonedFighter = false;

        if (ActionIdHelper.IsTargetMaxLifeAffected(spellEffect.ActionId))
        {
            effectOutputs.Add(EffectOutput.FromAffectedMaxLifePoints(affectedFighter.Id, runningEffect.Caster.Id, runningEffect.SpellEffect.ActionId));
        }

        if (spellEffect.ActionId is ActionId.CharacterLifePointsMalusPercent or ActionId.CharacterLifePointsMalus)
        {
            effectOutputs.Add(EffectOutput.FromAffectedLifePoints(affectedFighter.Id, runningEffect.Caster.Id, runningEffect.SpellEffect.ActionId));
        }

        if(fightContext.MarkExecutionCell == -1)
            affectedFighter.SavePositionBeforeMarkExecution();

        var summonedFighters = new List<HaxeFighter>();
        
        foreach (var output in effectOutputs)
        {
            if (output.DamageRange != null && unknown)
            {
                output.Unknown = true;
            }

            var fighter  = fightContext.GetFighterById(output.FighterId)!;
            var isDamage = output.DamageRange != null && !(output.DamageRange.Min == 0 && output.DamageRange.Max == 0);

            if (isDamage && output.DamageRange is { IsHeal: false, })
            {
                output.AreLifePointsAffected = true;
                var permanentDamage = DamageReceiver.GetPermanentDamage(output.DamageRange, fighter);
                output.AreErodedLifePointsAffected = permanentDamage is not { Min: 0, Max: 0, };
            }

            if (output.AreErodedLifePointsAffected)
            {
                output.AreMaxLifePointsAffected = true;
            }

            if (output.Death)
            {
                fightContext.AddLastKilledAlly(fighter);
            }

            if (output.Summon != null)
            {
                hasSummonedFighter = true;
                summonedFighters.Add(fighter);
            }

            if (!fighter.PendingEffects.Contains(output))
            {
                fighter.PendingEffects.Add(output);
            }
        }
        
        TriggerHandler(effectOutputs, runningEffect, fightContext, oppositeTeam);
        DamageEffectHandler.RedefinePortals(fightContext);

        if (!hasSummonedFighter)
        {
            return;
        }

        foreach (var summon in summonedFighters)
        {
            var startingSpell = DataInterface.GetStartingSpell(summon, spellEffect.Param2);
            if (startingSpell != null)
            {
                var targetedCell = fightContext.TargetedCell;
                
                fightContext.TargetedCell = summon.GetCurrentPositionCell();
                ExecuteSpell(fightContext, summon, startingSpell, runningEffect.ForceCritical, null, oppositeTeam,
                    unknown);
                fightContext.TargetedCell = targetedCell;
            }
        }
    }

    /// <summary>
    /// Determine if the summon takes a slot based on the spell effect.
    /// </summary>
    /// <param name="spellEffect">The HaxeSpellEffect instance representing the spell effect.</param>
    /// <param name="fightContext">The FightContext instance representing the fight.</param>
    /// <param name="caster">The HaxeFighter instance representing the caster of the spell.</param>
    /// <returns>A boolean indicating if the summon takes a slot.</returns>
    public static bool SummonTakesSlot(HaxeSpellEffect spellEffect, FightContext fightContext, HaxeFighter caster)
    {
        if (ActionIdHelper.IsSummonWithSlot(spellEffect.ActionId))
        {
            return true;
        }

        return spellEffect.ActionId == ActionId.SummonCreature && DataInterface.SummonTakesSlot(spellEffect.Param1);
    }

    /// <summary>
    /// Summons a fighter based on the given spell effect and fight context.
    /// </summary>
    /// <param name="spellEffect">The HaxeSpellEffect instance representing the spell effect.</param>
    /// <param name="fightContext">The FightContext instance representing the fight.</param>
    /// <param name="caster">The HaxeFighter instance representing the caster of the spell.</param>
    /// <param name="spell"></param>
    /// <param name="cellId">The cell id for the summon (optional, defaults to -1).</param>
    /// <returns>The summoned HaxeFighter instance or null if summoning failed.</returns>
    public static HaxeFighter? Summon(HaxeSpellEffect spellEffect, FightContext fightContext, HaxeFighter caster, HaxeSpell spell, int cellId = -1)
    {
        HaxeFighter? summonedFighter;

        if (!MapTools.IsValidCellId(cellId))
        {
            cellId = fightContext.TargetedCell;
        }

        var fighterAtCell = fightContext.GetFighterFromCell(cellId);
        
        if (!fightContext.Map.IsCellWalkable(cellId) || fighterAtCell != null && fighterAtCell.IsAlive())
        {
            return null;
        }

        var actionId         = spellEffect.ActionId;
        var isRevive         = ActionIdHelper.IsRevive(actionId);
        var isFakeRevive = false;

        switch (actionId)
        {
            case ActionId.CharacterAddDoubleUseSummonSlot:
            case ActionId.CharacterAddIllusionMirror:
            case ActionId.CharacterAddDoubleNoSummonSlot:
                if (caster.Data.IsSummon())
                {
                    return null;
                }
                
                summonedFighter = DataInterface.SummonDouble(caster, actionId == ActionId.CharacterAddIllusionMirror);
                break;
            default:
                if (isRevive)
                {
                    summonedFighter = fightContext.GetLastKilledAlly(caster.TeamId);

                    if (!fightContext.IsSimulation && summonedFighter != null && caster.GetSummoner(fightContext) == null)
                    {
                        fightContext.RemoveDeadFighter(summonedFighter.Id);
                        
                        if (spellEffect.ActionId == ActionId.CharacterSummonDeadAllyAsSummonInFight)
                        {
                            /**/
                                                        
                            // just invoke a new monster with the same template and grade

                            if (summonedFighter.Breed > 20)
                            {
                                summonedFighter = DataInterface.SummonMonster(caster, summonedFighter.Breed, summonedFighter.Grade);
                                isRevive = false;
                                isFakeRevive = true;

                                if (caster.Breed == (int)MonsterId.Sylargh)
                                {
                                    summonedFighter.Data.SetZombieLife();
                                }
                            }
                            else
                            {
                                // if there is someone on the cell we don't revive, but we apply "Zombi" to the cell
                                if (fighterAtCell != null)
                                {
                                    summonedFighter.Data.SetSummoner(caster.Id);
                                    summonedFighter.Data.ResetStats();
                                    summonedFighter.IsDead = false;
                                    isFakeRevive = true;
                                }
                            }
                        }
                        else if (spellEffect.ActionId == ActionId.CharacterSummonDeadAllyInFight)
                        {
                            fighterAtCell = fightContext.GetFighterFromCell(summonedFighter.GetCurrentPositionCell());
                            
                            if (fighterAtCell != null)
                            {
                                summonedFighter.SetCurrentPositionCell(cellId);
                                fighterAtCell = fightContext.GetFighterFromCell(summonedFighter.GetCurrentPositionCell());
                            }
                            
                            if (fighterAtCell == null)
                            {
                                var curPermDamage = summonedFighter.Data.GetPermanentDamage();
                                summonedFighter.Data.SetSummoner(-1);
                                summonedFighter.Data.ResetStats();
                                summonedFighter.Data.SetPermanentDamage(curPermDamage);
                                summonedFighter.Data.ResetCurLife();
                                summonedFighter.Data.SetHalfLife();
                                
                                var zombi = summonedFighter.Buffs.FirstOrDefault(buff => buff.Effect.ActionId == ActionId.FightSetState && buff.Effect.Param3 == (int)SpellStateId.Zombi);
                                if (zombi != null)
                                {
                                    summonedFighter.Buffs.Remove(zombi);
                                }
                                summonedFighter.IsDead = false;
                                isFakeRevive = true;
                            }
                            else
                            {
                                summonedFighter = null;
                            }
                        }
                    }
                }
                else
                {
                    summonedFighter = DataInterface.SummonMonster(caster, spellEffect.Param1, spellEffect.Param2);
                }

                break;
        }

        if (summonedFighter == null)
        {
            return null;
        }
        
        var existingFighter  = fightContext.GetFighterById(summonedFighter.Id);

        if (!isRevive)
        {
            if (fightContext.GetFighterById(summonedFighter.Id) != null)
            {
                summonedFighter.Data.OverrideId(fightContext.GetFreeId());
            }
        }
        
        if (isRevive && existingFighter == null)
        {
            return null;
        }

        if (!isRevive)
        {
            summonedFighter.SetCurrentPositionCell(cellId);
            summonedFighter.BeforeLastSpellPosition = cellId;
            summonedFighter.BeforeMarkPosition = cellId;
        }
        
        summonedFighter.IsSimulation           = fightContext.IsSimulation;
        
        if (fightContext.GetFighterById(summonedFighter.Id) == null)
        {
            fightContext.TempFighters.Add(summonedFighter);
            fightContext.Fighters.Add(summonedFighter);
        }

        if (spellEffect.ActionId is ActionId.SummonSlave or ActionId.FightKillAndSummonSlave && caster.PlayerType == PlayerType.Human)
        {
            summonedFighter.PendingEffects.Add(EffectOutput.FromControlEntity(summonedFighter.Id, caster.Id, spellEffect.ActionId, caster.Id));
        }

        if (isFakeRevive ||isRevive)
        {
            DamageCalculator.ExecuteZombi(fightContext, caster, spell, summonedFighter);
        }

        return summonedFighter;
    }

    private static void ExecuteZombi(FightContext fightContext, HaxeFighter caster, HaxeSpell spell, HaxeFighter summonedFighter)
    {

        var se = new HaxeSpellEffect(0,
            0,
            0,
            ActionId.FightSetState,
            0, 
            0, 
            (int)SpellStateId.Zombi, -1000, 
            false, 
            "I", 
            "A", 
            "A,a", 
            0d, 
            0, 
            false,
            0, 
            0,
            false, 0, 0);

        DamageCalculator.ExecuteEffect(fightContext,
            caster,
            spell,
            false,
            null,
            false,
            false,
            se,
            new Dictionary<int, IList<HaxeFighter>>()
            {
                { 0, new List<HaxeFighter>() { summonedFighter } }
            },
            new Dictionary<int, IList<HaxeFighter>>()
            {
                { 0, new List<HaxeFighter>() { summonedFighter } }
            });
    }

    /// <summary>
    /// Handles the triggers for an array of effect outputs and updates the affected fighters.
    /// </summary>
    /// <param name="trigger">The trigger to handle.</param>
    /// <param name="fightContext">The FightContext instance representing the fight.</param>
    /// <param name="target">The HaxeFighter instance representing the target of the spell.</param>
    /// <param name="isTriggered">A boolean indicating whether is triggered.</param>
    public static void TriggerHandler(string trigger, FightContext fightContext, HaxeFighter target, bool isTriggered)
    {
        void TriggerEffectsOnBuffs(HaxeLinkedList<HaxeBuff> buffs, bool isOnCaster, Func<HaxeBuff, bool> shouldTrigger, HaxeFighter affectedFighter)
        {
            var currentBuffNode = buffs.Head;

            while (currentBuffNode != null)
            {
                var tempBuffNode = currentBuffNode;
                currentBuffNode = currentBuffNode.Next;

                var currentBuff = tempBuffNode.Item;

                TriggerBuff(fightContext, target, isTriggered, currentBuff, affectedFighter, isOnCaster, shouldTrigger);
            }
        }

        TriggerEffectsOnBuffs(target.Buffs.Copy(), false, 
            buff =>
            {
                if (trigger.Contains("CMPARR"))
                    return buff.Effect.Triggers.Contains(trigger);
                
                if(trigger.Contains("TB") || trigger.Contains("TE"))
                {
                    return buff.LastTriggeredTurn != fightContext.GameTurn && 
                           buff.Effect.Triggers.Contains(trigger);
                }
                    
                return buff.Effect.Triggers.Contains(trigger);

            }, target);
    }

    public static void TriggerBuff(FightContext fightContext, HaxeFighter target, bool isTriggered, HaxeBuff currentBuff,
        HaxeFighter affectedFighter, bool isOnCaster, Func<HaxeBuff, bool> shouldTrigger)
    {
        fightContext.Map.ResetEffectCastCount();

        HaxeFighter triggerFighter;
        if (currentBuff.Effect.ActionId is ActionId.TargetExecuteSpell or
                                           ActionId.TargetExecuteSpellWithAnimation ||
            currentBuff.Effect.Triggers.Contains("K") || currentBuff.Effect.Triggers.Contains("KWW") ||
            currentBuff.Effect.Triggers.Contains("KWS") || currentBuff.Effect.Triggers.Contains("PO") ||
            currentBuff.Effect.Triggers.Contains("CH"))
        {
            triggerFighter = affectedFighter;
        }
        else
        {
            triggerFighter = target;
        }

        if (currentBuff.Effect.Triggers.Length == 0 ||
            currentBuff.Effect.Triggers is ["I",] ||
            ActionIdHelper.IsSpellExecution(currentBuff.Effect.ActionId) && currentBuff.Effect.Param3 > 0 &&
            currentBuff.GetTriggerCount() >= currentBuff.Effect.Param3 ||
            isOnCaster && currentBuff.HasBeenTriggeredOn.Contains(triggerFighter.Id))
        {
            return;
        }

        if (!currentBuff.IsActive)
        {
            return;
        }

        if (!shouldTrigger(currentBuff))
        {
            return;
        }

        /*if (currentBuff.Effect.ActionId == ActionId.FightLifePointsWinPercent && currentBuff.Effect.Triggers.Contains("TE"))
        {
            // we have to check the masks
            if(!SpellManager.IsSelectedByMask(target, currentBuff.Effect.Masks, target, target, fightContext))
            {
                return;
            }
        }*/

        HaxeSpellEffect? spellEffect;
        if (currentBuff.Effect.IsCritical)
        {
            spellEffect            = currentBuff.Effect.Clone();
            spellEffect.IsCritical = false;
        }
        else
        {
            spellEffect = currentBuff.Effect;
        }
        
        if (!shouldTrigger(currentBuff))
        {
            return;
        }

        var newRunningEffect = new RunningEffect(fightContext, fightContext.GetFighterById(currentBuff.CasterId)!, currentBuff.Spell, spellEffect);

        newRunningEffect.TriggeredByEffectSetting(new RunningEffect(fightContext, target, HaxeSpell.Empty, spellEffect));
        newRunningEffect.TriggeringOutput = null;
        newRunningEffect.IsTriggered      = true;
        newRunningEffect.ForceCritical    = false;

        var copiedFightContext = fightContext.Copy();
        copiedFightContext.TargetedCell = triggerFighter.GetBeforeLastSpellPosition();

        if (isOnCaster)
        {
            currentBuff.HasBeenTriggeredOn.Add(triggerFighter.Id);
        }

        currentBuff.IncrementTriggerCount();
        currentBuff.LastTriggeredTurn = fightContext.GameTurn;
        triggerFighter.PendingEffects.Add(EffectOutput.FromBuffTriggered(triggerFighter.Id, triggerFighter.Id, ActionId.K, currentBuff));
                
        ComputeEffect(copiedFightContext, newRunningEffect, isTriggered, new List<HaxeFighter> { triggerFighter, }, null);
        AddFightContextTempFighters(fightContext, copiedFightContext);
    }

    private static void AddFightContextTempFighters(FightContext fightContext, FightContext copiedFightContext)
    {
        if (fightContext == copiedFightContext)
        {
            return;
        }

        foreach (var fighter in copiedFightContext.TempFighters.ToList())
        {
            if (!fightContext.Fighters.Contains(fighter))
            {
                fightContext.Fighters.Add(fighter);
                fightContext.TempFighters.Add(fighter);
            }
        }
    }

    /// <summary>
    /// Handles the triggers for an array of effect outputs and updates the affected fighters.
    /// </summary>
    /// <param name="effectOutputs">An array of EffectOutput instances.</param>
    /// <param name="runningEffect">The RunningEffect instance related to the spell.</param>
    /// <param name="fightContext">The FightContext instance representing the fight.</param>
    /// <param name="isTriggered">A boolean indicating whether is triggered.</param>
    public static void TriggerHandler(IList<EffectOutput> effectOutputs, RunningEffect runningEffect,
        FightContext fightContext, bool isTriggered)
    {
        foreach (var output in effectOutputs)
        {
            if (output.DamageRange != null && runningEffect.ForceCritical)
            {
                output.DamageRange.IsCritical = true;
            }

            var affectedFighter  = fightContext.GetFighterById(output.FighterId)!;
            var areCellsAdjacent = MapTools.AreCellsAdjacent(runningEffect.GetCaster().GetCurrentPositionCell(), affectedFighter.GetCurrentPositionCell());

            TriggerEffects(fightContext, runningEffect, affectedFighter, areCellsAdjacent, output, isTriggered);
        }
    }

    /// <summary>
    /// Calculates the area of effect malus for the given spell effect.
    /// </summary>
    /// <param name="spellEffect">The HaxeSpellEffect instance representing the spell effect.</param>
    /// <param name="targetCellId">The target cell id.</param>
    /// <param name="caster">The HaxeFighter instance representing the caster of the spell.</param>
    /// <param name="target">The HaxeFighter instance representing the target of the spell.</param>
    /// <returns>A floating-point number representing the AoE malus.</returns>
    public static float GetAoeMalus(HaxeSpellEffect spellEffect, int targetCellId, HaxeFighter caster, HaxeFighter target)
    {
        var malus = 0;

        if (spellEffect.Zone.Radius >= 1)
        {
            var zone = spellEffect.Zone;
            malus = zone.GetAoeMalus(targetCellId, caster.GetCurrentPositionCell(), target.GetBeforeLastSpellPosition());
        }

        return (100f - malus) / 100f;
    }

    /// <summary>
    /// Solves spell execution by determining the caster, target, and spell to be executed.
    /// </summary>
    /// <param name="fightContext">The context of the current fight.</param>
    /// <param name="runningEffect">The running effect to process.</param>
    /// <param name="targetFighter">The targeted fighter.</param>
    /// <returns>A SpellExecutionInfos containing the new fight context, caster, spell, and whether the spell is critical.</returns>
    public static SpellExecutionInfos? SolveSpellExecution(FightContext fightContext, RunningEffect runningEffect, HaxeFighter? targetFighter)
    {
        HaxeFighter? spellCaster;
        HaxeFighter? spellTarget;

        var caster            = runningEffect.GetCaster();
        var triggeringFighter = runningEffect.IsTriggered ? runningEffect.TriggeringFighter : caster;

        var isTargetCell = false;
        var actionId     = runningEffect.GetSpellEffect().ActionId;

        switch (actionId)
        {
            case ActionId.TargetExecuteSpell:
            case ActionId.TargetExecuteSpellWithAnimation:
            case ActionId.TargetExecuteSpellGlobalLimitation:
            case ActionId.TargetExecuteSpellWithAnimationGlobalLimitation:
                spellCaster = targetFighter;
                spellTarget = targetFighter;
                break;
            case ActionId.SourceExecuteSpellOnTarget:
                spellCaster = triggeringFighter;
                spellTarget = targetFighter;
                break;
            case ActionId.SourceExecuteSpellOnSource:
                spellCaster = triggeringFighter;
                spellTarget = triggeringFighter;
                break;
            case ActionId.TargetExecuteSpellOnSource:
            case ActionId.TargetExecuteSpellOnSourceGlobalLimitation:
                spellCaster = targetFighter;
                spellTarget = triggeringFighter;
                break;
            case ActionId.SummonBomb:
            case ActionId.CasterExecuteSpell:
            case ActionId.CasterExecuteSpellGlobalLimitation:
                spellCaster = caster;
                spellTarget = targetFighter;
                break;
            case ActionId.TargetExecuteSpellOnCell:
            case ActionId.TargetExecuteSpellOnCellGlobalLimitation:
                spellCaster  = targetFighter;
                spellTarget  = targetFighter;
                isTargetCell = true;
                break;
            case ActionId.CasterExecuteSpellOnCell:
                spellCaster  = caster;
                spellTarget  = null;
                isTargetCell = true;
                break;
            default:
                throw new Exception($"ActionId {runningEffect.GetSpellEffect().ActionId} is not a spell execution");
        }

        var spellEffect     = runningEffect.GetSpellEffect();
        var targetCell      = isTargetCell ? fightContext.TargetedCell : spellTarget!.GetBeforeLastSpellPosition();
        var newFightContext = fightContext.Copy();
        newFightContext.TargetedCell = targetCell;
        var isCritical = runningEffect.ParentEffect != null &&
                         runningEffect.GetFirstParentEffect()!.GetSpellEffect().IsCritical;
        var spell = actionId == ActionId.SummonBomb
            ? DataInterface.GetBombCastOnFighterSpell(spellEffect.Param1, spellEffect.Param2)
            : DataInterface.CreateSpellFromId(spellEffect.Param1, spellEffect.Param2);

        if (spell == null)
        {
            return null;
        }

        return new SpellExecutionInfos(newFightContext, spellCaster, spell, isCritical, actionId);
    }

    /// <summary>
    /// Triggers effects on the target and caster, based on the triggering effect, target, and other provided parameters.
    /// </summary>
    /// <param name="fightContext">The context of the fight.</param>
    /// <param name="triggeringRunningEffect">The running effect that triggers the effects.</param>
    /// <param name="target">The target fighter on which the effects are triggered.</param>
    /// <param name="melee">Whether the effect triggering is in melee range.</param>
    /// <param name="triggeringOutput">The effect output produced by the triggering effect.</param>
    /// <param name="isTriggered">Whether the computation is triggered.</param>
    /// <returns>Returns true if any effect is triggered, false otherwise.</returns>
    public static bool TriggerEffects(FightContext fightContext, RunningEffect triggeringRunningEffect,
        HaxeFighter target, bool melee, EffectOutput triggeringOutput, bool isTriggered)
    {
        var hasTriggerEffect = false;

        void TriggerEffectsOnBuffs(HaxeLinkedList<HaxeBuff> buffs, bool isOnCaster, Func<HaxeBuff, bool> shouldTrigger, HaxeFighter affectedFighter)
        {
            var currentBuffNode = buffs.Head;

            while (currentBuffNode != null)
            {
                var tempBuffNode = currentBuffNode;
                currentBuffNode = currentBuffNode.Next;

                var currentBuff = tempBuffNode.Item;
                
                HaxeFighter triggerFighter;
                if (currentBuff.Effect.ActionId is ActionId.TargetExecuteSpell or
                                                   ActionId.TargetExecuteSpellWithAnimation ||
                    currentBuff.Effect.Triggers.Contains("K") || 
                    currentBuff.Effect.Triggers.Contains("KWW") ||
                    currentBuff.Effect.Triggers.Contains("KWS") || 
                    currentBuff.Effect.Triggers.Contains("PO") ||
                    currentBuff.Effect.Triggers.Contains("CH") ||
                    currentBuff.Effect.Triggers.Contains("CDBA") ||
                    currentBuff.Effect.Triggers.Contains("CDBE") ||
                    currentBuff.Effect.Triggers.Contains("CC") ||
                    currentBuff.Effect.Triggers.Contains("PST") ||
                    currentBuff.Effect.Triggers.Contains("PDT")
                    )
                {
                    triggerFighter = affectedFighter;
                }
                else
                {
                    triggerFighter = target;
                }
                

                if (currentBuff.Effect.Triggers.Length == 0 ||
                    currentBuff.Effect.Triggers is ["I",] ||
                    !currentBuff.CanBeTriggeredBy(triggeringRunningEffect) ||
                    ActionIdHelper.IsSpellExecution(currentBuff.Effect.ActionId) && 
                    currentBuff.Effect.Param3 > 0 &&
                    currentBuff.GetTriggerCount() >= currentBuff.Effect.Param3 ||
                    isOnCaster && currentBuff.HasBeenTriggeredOn.Contains(triggerFighter.Id))
                {
                    continue;
                }

                if (!shouldTrigger(currentBuff))
                {
                    continue;
                }

                // Kimbo
                if (currentBuff.Effect.Param3 is 29 or 30 && currentBuff.Effect.ActionId == ActionId.FightSetState)
                {
                    var summon = fightContext.Fighters.FirstOrDefault(f => f.GetSummoner(fightContext)?.Id == target.Id);
                    
                    if (summon != null)
                    {
                        triggerFighter = summon;
                    }
                }
                
                HaxeSpellEffect? spellEffect;
                if (currentBuff.Effect.IsCritical)
                {
                    spellEffect            = currentBuff.Effect.Clone();
                    spellEffect.IsCritical = false;
                }
                else
                {
                    spellEffect = currentBuff.Effect;
                }

                var newRunningEffect = new RunningEffect(fightContext, fightContext.GetFighterById(currentBuff.CasterId)!, currentBuff.Spell, spellEffect);

                newRunningEffect.TriggeredByEffectSetting(triggeringRunningEffect);
                newRunningEffect.TriggeringOutput = triggeringOutput;
                newRunningEffect.IsTriggered      = true;
                newRunningEffect.ForceCritical    = triggeringRunningEffect.ForceCritical;

                var copiedFightContext = fightContext.Copy();
                copiedFightContext.TargetedCell = triggerFighter.GetBeforeLastSpellPosition();

                if (isOnCaster)
                {
                    currentBuff.HasBeenTriggeredOn.Add(triggerFighter.Id);
                }

                currentBuff.IncrementTriggerCount();
                triggerFighter.PendingEffects.Add(EffectOutput.FromBuffTriggered(triggerFighter.Id, triggerFighter.Id, ActionId.K, currentBuff));

                try
                {
                    ComputeEffect(copiedFightContext, newRunningEffect, isTriggered,
                        new List<HaxeFighter>
                        {
                            triggerFighter,
                        }, null);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                hasTriggerEffect = true;
                AddFightContextTempFighters(fightContext, copiedFightContext);
            }
        }

        var caster = triggeringRunningEffect.GetCaster();
        var targetBuffs = target.Buffs.Copy();
        TriggerEffectsOnBuffs(targetBuffs, false,
            buff => buff.ShouldBeTriggeredOnTarget(triggeringOutput, triggeringRunningEffect, target, melee, fightContext), target);
       
        TriggerEffectsOnBuffs(caster.Buffs.Copy(), true,
            buff => buff.ShouldBeTriggeredOnCaster(triggeringOutput, triggeringRunningEffect, target, melee, fightContext), caster);

        return hasTriggerEffect;
    }

    /// <summary>
    /// Executes the marks interaction for a given cell.
    /// </summary>
    /// <param name="fightContext">The context of the fight.</param>
    /// <param name="runningEffect">The running effect that triggered the interaction.</param>
    /// <param name="fighter">The HaxeFighter that is interacting with the marks.</param>
    /// <param name="cellId">The ID of the cell that contains the marks.</param>
    /// <param name="isTriggered">Indicates whether the interaction is preview.</param>
    /// <param name="markType">Indicate if a specified mark type should be triggered</param>
    /// <param name="onlyAura">Indicate if a specified mark aura should be triggered</param>
    public static bool ExecuteMarks(FightContext fightContext,
        RunningEffect? runningEffect,
        HaxeFighter fighter,
        int cellId,
        bool isTriggered,
        GameActionMarkType? markType = null,
        bool onlyAura = false, 
        bool fromDrag = false, 
        bool onlyImmediate = false,
        int recursivityTries = 0)
    {
        fightContext.MarkMainCell = -1;
        fightContext.MarkExecutionCell = -1;
        
        var anyMarkTriggered = false;
        
        var marks = fightContext.Map.GetMarkInteractingWithCell(cellId, true);
        
        foreach (var mark in marks)
        {
            if (!mark.Active || fightContext.TriggeredMarks.Contains(mark.MarkId))
            {
                continue;
            }

            if (mark.Aura && mark.TriggeredFighters.Contains(fighter.Id))
            {
                continue;
            }

            if (markType != null && mark.MarkType != markType)
            {
                continue;
            }

            if (onlyAura && !mark.Aura)
            {
                continue;
            }

            if (mark.MarkType == GameActionMarkType.Glyph && onlyImmediate)
            {
                if (!mark.IsImmediate)
                    continue;
            }

            if (mark is { MarkType: GameActionMarkType.Glyph, Aura: false, } && fromDrag)
            {
                continue;
            }

            anyMarkTriggered = true;
            switch (mark.MarkType)
            {
                case GameActionMarkType.Glyph:
                case GameActionMarkType.Aura:
                case GameActionMarkType.Trap:
                    ExecuteMarkSpell(fighter, mark, runningEffect, fightContext, isTriggered);
                    break;
                case GameActionMarkType.Wall:
                    ExecuteWallDamage(fighter, mark, runningEffect, fightContext, isTriggered);
                    break;
                case GameActionMarkType.Portal:
                    if (!DamageEffectHandler.UsePortal(fightContext, fighter, mark))
                    {
                        anyMarkTriggered = false;
                        continue;
                    }

                    break;
            }

            if (!mark.Aura)
            {
                DamageEffectHandler.AddEffectMarkTrigger(fightContext, mark);
            }

            if (!mark.TriggeredFighters.Contains(fighter.Id))
            {
                mark.TriggeredFighters.Add(fighter.Id);
            }
        }

        if (fightContext.MarkExecutionCell == -1 && anyMarkTriggered && recursivityTries < 10)
        {
            fighter.BeforeMarkPosition = fighter.GetCurrentPositionCell();

            foreach (var f in fightContext.Fighters.Where(x => x.IsAlive()))
            {       
                f.BeforeMarkPosition = f.GetCurrentPositionCell();

                DamageCalculator.ExecuteMarks(fightContext, runningEffect, f, f.GetCurrentPositionCell(), true, fromDrag: true, recursivityTries: recursivityTries + 1);
            }
        }
        
        return anyMarkTriggered;
    }


    public static void ExecuteGlyphOnEveryFighter(Mark mark, RunningEffect? runningEffect, FightContext fightContext, bool isTriggered)
    {
        foreach (var fighter in fightContext.Fighters.Where(x => x.IsAlive()))
        {
            if (mark.Cells.Contains(fighter.GetCurrentPositionCell()))
            {
                fighter?.SavePositionBeforeSpellExecution();
                fighter?.SavePositionBeforeMarkExecution();

                ExecuteMarkSpell(fighter, mark, runningEffect, fightContext, isTriggered);
            }
        }
    }

    public static void ExecuteWallOnEveryFighter(Mark mark, RunningEffect? runningEffect, FightContext fightContext, bool isTriggered)
    {
        foreach (var fighter in fightContext.Fighters.Where(x => x.IsAlive()))
        {
            if (mark.Cells.Contains(fighter.GetCurrentPositionCell()))
            {
                ExecuteWallDamage(fighter, mark, runningEffect, fightContext, isTriggered);
            }
        }
    }
    /// <summary>
    /// Executes the mark spell interaction for a given fighter and mark.
    /// </summary>
    /// <param name="fighter">The HaxeFighter that is interacting with the mark.</param>
    /// <param name="mark">The Mark object associated with the spell.</param>
    /// <param name="runningEffect">The running effect that triggered the interaction.</param>
    /// <param name="fightContext">The context of the fight.</param>
    /// <param name="isTriggered">Indicates whether the interaction is triggered.</param>
    public static void ExecuteMarkSpell(HaxeFighter? fighter, Mark mark, RunningEffect? runningEffect, FightContext fightContext, bool isTriggered)
    {
        if (!mark.Active || fightContext.TriggeredMarks.Contains(mark.MarkId))
        {
            return;
        }

        if (mark.Aura && (fighter == null || mark.TriggeredFighters.Contains(fighter.Id)))
        {
            return;
        }
        
        fightContext.TriggeredMarks.Add(mark.MarkId);
        var copiedFightContext = fightContext.Copy();
        copiedFightContext.TargetedCell = mark.MainCell;
        copiedFightContext.MarkExecutionCell = mark.MainCell;
        copiedFightContext.MarkMainCell = mark.MainCell;
        //copiedFightContext.InMovement   = true;
        var markCaster = fightContext.GetFighterById(mark.CasterId);

        if (markCaster == null)
        {
            return;
        }
        
        if (mark.AssociatedSpell == null)
        {
            return;
        }
        
        copiedFightContext.FromGlyphAuraSet = mark.MarkType == GameActionMarkType.Glyph && mark.Aura;
        copiedFightContext.FromGlyphAura    = mark;
        
        // maybe remove this later
        if (mark is { MarkType: GameActionMarkType.Glyph, Aura: false, } && fighter != null && mark.AssociatedSpell.GetEffects().Any(x => x.RawZone.StartsWith("P")))
        {
            copiedFightContext.TargetedCell = fighter.GetBeforeLastSpellPosition();
            copiedFightContext.MarkExecutionCell = fighter.GetBeforeLastSpellPosition();
        }
        
        if (!mark.Aura)
        {
            if (fighter == null)
            {
                DamageEffectHandler.AddEffectMarkTrigger(fightContext, mark);
            }
            else
            {
                fighter.PendingEffects.Add(EffectOutput.FromMarkTrigger(fighter.Id, fighter.Id, mark.GetActionTrigger(), mark.MarkId));
            }
        }
        
        if (fighter != null && !mark.Aura && mark.MarkType == GameActionMarkType.Glyph)
        {      
            if (runningEffect != null)
                runningEffect.TriggeringFighter = fighter;

            ExecuteSpell(copiedFightContext,
                markCaster,
                mark.AssociatedSpell,
                runningEffect?.ForceCritical ?? false,
                runningEffect, 
                isTriggered,
                forceTarget: fighter);
        }
        else
        {
            //if (fighter != null && fighter.GetBeforeLastSpellPosition() != fighter.GetCurrentPositionCell())
                //copiedFightContext.TargetedCell = fighter.GetBeforeLastSpellPosition();

            if (fighter != null)
            {
                copiedFightContext.MarkExecutionCell = fighter.GetCurrentPositionCell();
            }
            
            var additionalFighter = mark.MarkType is GameActionMarkType.Trap or GameActionMarkType.Glyph ? fighter : null;
            
            if (runningEffect != null)
                runningEffect.TriggeringFighter = fighter;

            /*if (mark.AssociatedSpell.Id is 12954 or 12927 or 12928 or 12955 or 12952 or 12929 or 28736 or 28737)
            {
                //additionalFighter = null;
                
                if (runningEffect != null)
                    runningEffect.TriggeringFighter = fighter;
                
                //fighter?.SetBeforeLastSpellPosition(fighter.GetCurrentPositionCell());
            }*/
            
            ExecuteSpell(copiedFightContext,
                markCaster,
                mark.AssociatedSpell,
                runningEffect?.ForceCritical ?? false,
                runningEffect, 
                isTriggered,
                additionalTarget : additionalFighter);
        }

        /*if (!mark.Aura)
        {
            DamageEffectHandler.AddEffectMarkTrigger(fightContext, mark);
        }*/

        if (mark.MarkType == GameActionMarkType.Trap)
        {
            mark.Active    = false;
            mark.IsDeleted = true;
        }
        
        if (fighter == null || !mark.Aura)
        {
            return;
        }

        if (!mark.TriggeredFighters.Contains(fighter.Id))
        {
            mark.TriggeredFighters.Add(fighter.Id);
        }
    }

    /// <summary>
    /// Executes wall damage interaction for a given fighter and mark.
    /// </summary>
    /// <param name="fighter">The HaxeFighter that is interacting with the mark.</param>
    /// <param name="mark">The Mark object associated with the wall damage.</param>
    /// <param name="runningEffect">The running effect that triggered the interaction.</param>
    /// <param name="fightContext">The context of the fight.</param>
    /// <param name="critical">Indicates whether the interaction is critical.</param>
    public static void ExecuteWallDamage(HaxeFighter fighter, Mark mark, RunningEffect? runningEffect, FightContext fightContext, bool critical)
    {
        var linkedBombs = GetBombsLinkedToWall(fighter, fightContext);
        //var finalBonus  = 0;
        //var summoner    = linkedBombs.FirstOrDefault()?.GetSummoner(fightContext);
        //var haxeBuff = linkedBombs.FirstOrDefault()?.Buffs.FirstOrDefault(x => x.Effect.ActionId == ActionId.BombComboBonus);
        
        // we get the two closest bomb
        linkedBombs.Sort((fighterA, fighterB) =>
            TargetManagement.ComparePositions(fighter.GetCurrentPositionCell(), false, fighterA.GetCurrentPositionCell(),
                fighterB.GetCurrentPositionCell()));

        var closestBombs = linkedBombs.Take(2);
        var otherBombs = linkedBombs.Skip(2);
        var maxBonus = 0;
        
        var firstBomb = linkedBombs.FirstOrDefault();

        if (firstBomb == null)
        {
            return;
        }
        
        var summoner = firstBomb.GetSummoner(fightContext);
        var buff = firstBomb.Buffs.FirstOrDefault(x => x.Effect.ActionId == ActionId.BombComboBonus);

        foreach (var bomb in closestBombs)
        {
            var buffNode = bomb.Buffs.Head;

            while (buffNode != null)
            {
                var currentNode = buffNode;
                buffNode = buffNode.Next;

                if (currentNode.Item.Effect.ActionId == ActionId.BombComboBonus)
                {
                    //finalBonus += currentNode.Item.Effect.Param1;
                    if(maxBonus < currentNode.Item.Effect.Param1)
                        maxBonus = currentNode.Item.Effect.Param1;
                    
                    //bomb.GetSummoner(fightContext)!.StorePendingBuff(currentNode.Item);
                }
            }
        }
        
        foreach (var bomb in otherBombs)
        {
            var buffNode = bomb.Buffs.Head;

            while (buffNode != null)
            {
                var currentNode = buffNode;
                buffNode = buffNode.Next;

                if (currentNode.Item.Effect.ActionId == ActionId.BombComboBonus)
                {
                    //finalBonus += currentNode.Item.Effect.Param1;
                    maxBonus += currentNode.Item.Effect.Param1 / 2;
                    
                    //bomb.GetSummoner(fightContext)!.StorePendingBuff(currentNode.Item);
                }
            }
        }

        if (summoner != null && buff != null)
        {
            var clonedBuff = buff.Clone();
            clonedBuff.Effect.Param1 = maxBonus;
            
            summoner.AddPendingBuff(clonedBuff);
    
            /*var re           = new RunningEffect(fightContext, summoner, HaxeSpell.Empty, clonedEffect);
            HandleTarget(fightContext, re, false, new List<HaxeFighter>(), false, summoner, clonedEffect, null,
                summoner, new DamageRange(finalBonus, finalBonus), out var effectOutputs);
            
            foreach (var effectOutput in effectOutputs)
                summoner.AddPendingEffects(effectOutput);*/
        }

        fighter?.SavePositionBeforeSpellExecution();
        fighter?.SavePositionBeforeMarkExecution();

        ExecuteMarkSpell(fighter, mark, runningEffect, fightContext, critical);
        
        firstBomb.GetSummoner(fightContext)!.RemoveBuffByActionId(ActionId.BombComboBonus);
    }

    /// <summary>
    /// Returns a list of bombs linked to a wall in relation to the given fighter and fight context.
    /// </summary>
    /// <param name="fighter">The HaxeFighter associated with the wall.</param>
    /// <param name="fightContext">The context of the fight.</param>
    /// <returns>A list of bombs linked to the wall.</returns>
    public static List<HaxeFighter> GetBombsLinkedToWall(HaxeFighter fighter, FightContext fightContext)
    {
        var bombs       = new List<HaxeFighter>();
        var linkedBombs = new List<HaxeFighter>();
        var wall = fightContext.Map.GetMarkInteractingWithCell(fighter.GetCurrentPositionCell(), true,
            GameActionMarkType.Wall);

        if (wall.Count == 0)
            return new List<HaxeFighter>();
        
        var mainMark = wall[0];
        var fighters = fightContext.GetFightersFromZone(WallZone, mainMark.MainCell, mainMark.MainCell);

        fighters.Sort((fighterA, fighterB) =>
            TargetManagement.ComparePositions(mainMark.MainCell, false, fighterA.GetCurrentPositionCell(),
                fighterB.GetCurrentPositionCell()));

        foreach (var currentFighter in fighters.Where(currentFighter =>
            currentFighter.PlayerType == PlayerType.Monster &&
            currentFighter.Data.IsSummon() &&
            HaxeFighter.BombBreedId.Contains(currentFighter.Breed) &&
            currentFighter.Data.GetSummonerId() == mainMark.CasterId))
        {
            if (bombs.Count == 0)
            {
                bombs.Add(currentFighter);
            }
            else
            {
                foreach (var bomb in bombs.ToArray())
                {
                    if (bombs.IndexOf(currentFighter) == -1)
                    {
                        bombs.Add(currentFighter);
                    }

                    if (bomb == currentFighter || currentFighter.Breed != bomb.Breed ||
                        MapTools.GetLookDirection4(currentFighter.GetCurrentPositionCell(),
                            bomb.GetCurrentPositionCell()) == -1 ||
                        MapTools.GetDistance(currentFighter.GetCurrentPositionCell(),
                            bomb.GetCurrentPositionCell()) > 7)
                    {
                        continue;
                    }

                    if (linkedBombs.IndexOf(currentFighter) == -1)
                    {
                        linkedBombs.Add(currentFighter);
                    }

                    if (linkedBombs.IndexOf(bomb) == -1)
                    {
                        linkedBombs.Add(bomb);
                    }

                    if (linkedBombs.Count == 3)
                    {
                        return linkedBombs;
                    }
                }
            }
        }

        if (linkedBombs.Count >= 3 || bombs.Count >= 3)
        {
            return linkedBombs;
        }

        var linkedBombsArr = linkedBombs.TakeWhile(_ => linkedBombs.Count != 3).ToArray();
        foreach (var bomb in linkedBombsArr)
        {
            fighters = fightContext.GetFightersFromZone(WallZone, bomb.GetCurrentPositionCell(),
                bomb.GetCurrentPositionCell());

            foreach (var currentFighter in fighters.Where(currentFighter =>
                currentFighter.Data.GetSummonerId() == mainMark.CasterId &&
                currentFighter.PlayerType == PlayerType.Monster &&
                currentFighter.Data.IsSummon() &&
                HaxeFighter.BombBreedId.Contains(currentFighter.Breed) &&
                linkedBombs.IndexOf(currentFighter) == -1 &&
                currentFighter.Breed == bomb.Breed))
            {
                linkedBombs.Add(currentFighter);
                break;
            }
        }

        return linkedBombs;
    }
    


    /// <summary>
    /// Create a 32 bit hash from Spell Id and Grade
    /// </summary>
    /// <params>
    /// <param name="spellId">The spell id.</param>
    /// <param name="spellGrade">The spell grade.</param>
    /// </params>
    /// <returns>The hash.</returns>
    public static int Create32BitHashSpellLevel(int spellId, byte spellGrade)
    {
        // Shift spellId left by 8 bits to make room for the spellGrade.
        var hash = spellId << 8;

        // Combine the spellId and spellGrade using bitwise OR.
        hash |= spellGrade;

        // To minimize the risk of collisions, you can also apply a simple mix function.
        hash ^= hash << 13 ^ hash >> 17 ^ hash << 5;

        return hash;
    }
}