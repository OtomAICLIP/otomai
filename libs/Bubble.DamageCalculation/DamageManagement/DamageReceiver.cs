using Bubble.Core.Datacenter.Datacenter.Effects;
using Bubble.DamageCalculation.Customs;
using Bubble.DamageCalculation.FighterManagement;
using Bubble.DamageCalculation.SpellManagement;
using Bubble.DamageCalculation.Tools;

namespace Bubble.DamageCalculation.DamageManagement;

public static class DamageReceiver
{
    public const double LifeStealMultiplicator = 0.5;

    /// <summary>
    /// Calculates the damage or heal for a fighter and returns an array of effect outputs.
    /// </summary>
    /// <param name="fightContext">The context of the fight.</param>
    /// <param name="runningEffect">The running effect of the damage or heal.</param>
    /// <param name="damageRange">The range of damage or heal values.</param>
    /// <param name="targetFighter">The target fighter receiving the damage or heal.</param>
    /// <param name="isMelee">Indicates whether the damage or heal is melee.</param>
    /// <param name="ignoreDamageReduction">Indicates whether to ignore damage reduction.</param>
    /// <returns>An array of effect outputs.</returns>
    public static List<EffectOutput> ReceiveDamageOrHeal(FightContext fightContext, RunningEffect runningEffect,
                                                         DamageRange damageRange, HaxeFighter targetFighter,
                                                         bool isMelee = false, bool ignoreDamageReduction = false)
    {
        var copiedDamageRange = (DamageRange)damageRange.Copy();
        
        if (targetFighter.UnderMaximizeRollEffect())
        {
            copiedDamageRange.MinimizeBy(copiedDamageRange.Max);
        }
        
        if (ActionIdHelper.IsBasedOnActionsPoints(runningEffect.SpellEffect.ActionId) && !fightContext.IsSimulation)
        {
            if (targetFighter.Data.GetUsedPa() <= 0)
            {
                copiedDamageRange.SetToZero();
            }
            else
            {
                copiedDamageRange.Multiply(targetFighter.Data.GetUsedPa());
            }
        }
        else if(ActionIdHelper.IsBasedOnMovementPointsUsed(runningEffect.SpellEffect.ActionId) && !fightContext.IsSimulation)
        {
            if (targetFighter.Data.GetUsedPm() <= 0)
            {
                copiedDamageRange.SetToZero();
            }
            else
            {
                copiedDamageRange.Multiply(targetFighter.Data.GetUsedPm());
            }
        }
        
        var healOnDamageRatio = targetFighter.GetHealOnDamageRatio(runningEffect, isMelee);

        if (healOnDamageRatio > 0 && !copiedDamageRange.IsHeal && !targetFighter.IsInvulnerableTo(runningEffect, isMelee, copiedDamageRange.ElemId))
        {
            copiedDamageRange.IsHeal         = true;
            copiedDamageRange.IsShieldDamage = false;
            copiedDamageRange.Multiply(healOnDamageRatio * 0.01);
            runningEffect.HasDamageToHeal = true;
        }

        var effectOutputs = copiedDamageRange.IsHeal
            ? [ExecuteLifePointsWin(runningEffect, copiedDamageRange, targetFighter),]
            : ReceiveDamage(fightContext, runningEffect, copiedDamageRange, targetFighter, isMelee, ignoreDamageReduction);
        
        var distinctFighterIds = effectOutputs.Select(effectOutput => effectOutput.FighterId);

        foreach (var currentFighterId in distinctFighterIds)
        {
            var currentDamageRange = new DamageRange(0, 0);

            foreach (var currentEffectOutput in effectOutputs)
            {
                bool isEffective;
                if (currentEffectOutput.FighterId == currentFighterId && currentEffectOutput.DamageRange != null &&
                    !currentEffectOutput.DamageRange.IsInvulnerable)
                {
                    var tempDamageRange = currentEffectOutput.DamageRange;
                    isEffective = tempDamageRange.Min != 0 || tempDamageRange.Max != 0;
                }
                else
                {
                    isEffective = false;
                }

                if (isEffective && currentEffectOutput.DamageRange is { IsShieldDamage: false, })
                {
                    if (currentEffectOutput.DamageRange.IsHeal)
                    {
                        currentDamageRange.SubInterval(currentEffectOutput.DamageRange);
                    }
                    else
                    {
                        currentDamageRange.AddInterval(currentEffectOutput.ComputeLifeDamage());
                    }
                }
            }

            if (targetFighter.Data.HasGod())
            {
                currentDamageRange.SetToZero();
            }
            
            if (fightContext.GetFighterById(currentFighterId)?.GetPendingLifePoints().Max - currentDamageRange.Min <= 0)
            {
                if(!targetFighter.Data.HasGod())
                {
                    effectOutputs = effectOutputs.Concat([EffectOutput.DeathOf(currentFighterId, runningEffect.Caster.Id, runningEffect.SpellEffect.ActionId, false)]).ToList();
                }
                
            }
        }

        return effectOutputs;
    }

    /// <summary>
    /// Processes the received damage for a fighter and returns an array of effect outputs.
    /// </summary>
    /// <param name="fightContext">The context of the fight.</param>
    /// <param name="runningEffect">The running effect of the damage.</param>
    /// <param name="damageRange">The range of damage values.</param>
    /// <param name="targetFighter">The target fighter receiving the damage.</param>
    /// <param name="isCritical">Indicates whether the damage is critical.</param>
    /// <param name="ignoreDamageReduction">Indicates whether to ignore damage reduction.</param>
    /// <returns>An array of effect outputs.</returns>
    public static List<EffectOutput> ReceiveDamage(FightContext fightContext, RunningEffect runningEffect,
                                                   DamageRange damageRange, HaxeFighter targetFighter,
                                                   bool isCritical = false, bool ignoreDamageReduction = false)
    {
        var effectOutputs = new List<EffectOutput>();
        var dodgeResult   = ExecuteDodge(fightContext, runningEffect, targetFighter, isCritical, ignoreDamageReduction);

        if (dodgeResult.Done)
        {
            return dodgeResult.Damage;
        }
        
        if (!damageRange.IsCollision)
        {
            targetFighter.LastTheoreticalRawDamageTaken = (DamageRange)damageRange.Copy();

            if (!runningEffect.GetCaster().IsPacifist())
            {
                targetFighter.LastRawDamageTaken = targetFighter.LastTheoreticalRawDamageTaken;
            }
            else
            {
                damageRange = targetFighter.LastRawDamageTaken = DamageRange.Zero;
            }

            damageRange.Add(-GetFlatResistance(runningEffect, targetFighter, damageRange.ElemId));

            var reduced = targetFighter.GetDamageReductor(runningEffect, damageRange, isCritical);
            damageRange.TotalReduced = reduced.Min;

            damageRange.SubInterval(reduced);

            if (damageRange.Max > 0 && runningEffect.GetSpell().CanBeReflected && !runningEffect.IsReflect && runningEffect.GetCaster() != targetFighter)
            {
                var reflectedDamageRange = ReflectDamage(fightContext, runningEffect, damageRange, targetFighter);
                
                if (reflectedDamageRange != null)
                {
                    var reflectedRunningEffect = runningEffect.Copy();
                    reflectedRunningEffect.IsReflect = true;
                    effectOutputs.AddRange(ReceiveDamageOrHeal(fightContext, reflectedRunningEffect, reflectedDamageRange, runningEffect.GetCaster(), isCritical));
                }
            }

            damageRange.Multiply(1 - targetFighter.GetElementMainResist(damageRange.ElemId) / 100d);
            damageRange.MinimizeBy(0);

            var splitDamageOutputs = GetSplitDamage(fightContext, runningEffect, damageRange, targetFighter, ignoreDamageReduction);
            
            if (splitDamageOutputs != null)
            {
                return splitDamageOutputs;
            }
        }

        var damageOutput = ApplyDamage(runningEffect, targetFighter, damageRange, isCritical, ignoreDamageReduction);

        if (ActionIdHelper.IsLifeSteal(runningEffect.GetSpellEffect().ActionId) && runningEffect.GetCaster() != targetFighter)
        {
            var lifeStealEffectOutput = GetLifeStealEffect(runningEffect, damageOutput.ComputeLifeDamage(), runningEffect.GetCaster());
            
            if (lifeStealEffectOutput != null)
            {
                effectOutputs.Add(lifeStealEffectOutput);
            }
        }

        if (!damageRange.IsCollision)
        {
            damageOutput.FighterId = ChangeTargetIfSacrifice(fightContext, runningEffect, damageOutput, targetFighter, isCritical);
        }

        effectOutputs.Add(damageOutput);

        return effectOutputs.ToList();
    }

    /// <summary>
    /// Calculates the permanent damage for a given damage range and target fighter.
    /// </summary>
    /// <param name="damageRange">The range of damage values.</param>
    /// <param name="targetFighter">The target fighter receiving the damage.</param>
    /// <returns>A DamageRange representing the permanent damage.</returns>
    public static DamageRange GetPermanentDamage(DamageRange damageRange, HaxeFighter targetFighter)
    {
        if (damageRange.IsHeal)
        {
            return DamageRange.Zero;
        }

        var permanentDamageRatio =
            (int)Math.Floor((double)Math.Max(0,
                Math.Min(targetFighter.Data.GetCharacteristicValue(StatId.PermanentDamagePercent), 50))) / 100d;

        var calculatedDamageRange = new DamageRange((int)Math.Floor(damageRange.Min * permanentDamageRatio),
                                                    (int)Math.Floor(damageRange.Max * permanentDamageRatio));

        var remainingHealthPoints = targetFighter.Data.GetHealthPoints() - 1;
        return new DamageRange((int)Math.Floor(Math.Min((double)calculatedDamageRange.Min, remainingHealthPoints)),
                               (int)Math.Floor(Math.Min((double)calculatedDamageRange.Max, remainingHealthPoints)));
    }

    /// <summary>
    /// Calculates the damage based on target's life for a given spell effect, target fighter, and damage range.
    /// </summary>
    /// <param name="spellEffect">The HaxeSpellEffect instance.</param>
    /// <param name="targetFighter">The target fighter receiving the damage.</param>
    /// <param name="damageRange">The range of damage values.</param>
    /// <returns>A DamageRange representing the damage based on the target's life.</returns>
    public static DamageRange GetDamageBasedOnTargetLife(HaxeSpellEffect spellEffect, HaxeFighter targetFighter,
                                                         DamageRange damageRange)
    {
        if (ActionIdHelper.IsBasedOnTargetMaxLife(spellEffect.ActionId))
        {
            damageRange.Multiply(targetFighter.Data.GetMaxHealthPoints() / 100d);
        }
        else if (ActionIdHelper.IsBasedOnTargetLifePercent(spellEffect.ActionId))
        {
            var lifePointsInterval = targetFighter.GetPendingLifePoints();
            damageRange.MultiplyInterval(lifePointsInterval);
            damageRange.Multiply(0.01);
            
            spellEffect.ActionId = ActionId.CharacterLifePointsMalus;
        }
        else if (ActionIdHelper.IsBasedOnTargetLifeMissingMaxLife(spellEffect.ActionId))
        {
            damageRange.Multiply(targetFighter.Data.GetCharacteristicValue(StatId.PermanentDamagePercent) / 100d);
        }

        return damageRange;
    }

    /// <summary>
    /// Changes the target of an effect if the target has an active Sacrifice effect.
    /// </summary>
    /// <param name="fightContext">The FightContext instance.</param>
    /// <param name="runningEffect">The RunningEffect instance.</param>
    /// <param name="effectOutput">The EffectOutput instance.</param>
    /// <param name="targetFighter">The target HaxeFighter instance.</param>
    /// <param name="isCritical">Indicates whether the effect is critical.</param>
    /// <returns>A number representing the new target's ID if there's an active Sacrifice effect, otherwise the original target's ID.</returns>
    public static long ChangeTargetIfSacrifice(FightContext fightContext, RunningEffect runningEffect,
                                               EffectOutput effectOutput, HaxeFighter   targetFighter, bool isCritical)
    {
        if (ActionIdHelper.IsHeal(runningEffect.GetSpellEffect().ActionId))
        {
            return targetFighter.Id;
        }
        
        var sacrificedFighterIds = targetFighter.GetAllSacrificed();

        if (runningEffect.GetSpellEffect().ActionId == ActionId.CharacterLifePointsLostFromPush ||
            targetFighter.IsInvulnerableTo(runningEffect, isCritical, effectOutput.DamageRange?.ElemId))
        {
            return targetFighter.Id;
        }

        foreach (var fighterId in sacrificedFighterIds)
        {
            foreach (var currentBuff in targetFighter.Buffs)
            {
                if (currentBuff.Effect.ActionId != ActionId.CharacterSacrify ||
                    !currentBuff.ShouldBeTriggeredOnTarget(effectOutput, runningEffect, targetFighter, isCritical,
                                                           fightContext) || currentBuff.CasterId != fighterId)
                {
                    continue;
                }

                var currentFighter = fightContext.GetFighterById(fighterId);

                if (currentFighter != null && currentFighter.IsAlive())
                {
                    return currentFighter.Id;
                }
            }
        }

        return targetFighter.Id;
    }

    /// <summary>
    /// Applies damage to a target fighter, taking into account the target's invulnerability and any damage multipliers.
    /// </summary>
    /// <param name="runningEffect">The RunningEffect instance.</param>
    /// <param name="targetFighter">The target HaxeFighter instance.</param>
    /// <param name="damageRange">The DamageRange instance representing the damage to be applied.</param>
    /// <param name="isCritical">Indicates whether the effect is critical.</param>
    /// <param name="isCollision">Indicates whether the damage is a collision damage.</param>
    /// <returns>An EffectOutput instance representing the applied damage and any additional effects.</returns>
    public static EffectOutput ApplyDamage(RunningEffect runningEffect, HaxeFighter targetFighter,
                                           DamageRange damageRange, bool isCritical, bool isCollision)
    {
        if (targetFighter.IsInvulnerableTo(runningEffect, isCritical, damageRange.ElemId))
        {
            damageRange.IsInvulnerable = true;
        }

        damageRange.MinimizeBy(0);
        damageRange = ApplyDealtMultiplier(runningEffect, damageRange, targetFighter, isCritical);

        if (!damageRange.IsHeal && !damageRange.IsShieldDamage)
        {
            if (runningEffect.Caster.IsPacifist())
            {
                damageRange.SetToZero();
            }

            var minLife = targetFighter.GetMinimumHealthPoints();
            var curLife = targetFighter.GetPendingLifePoints().Min + targetFighter.GetPendingShield().Min;
            
            if (curLife - damageRange.Min < minLife)
            {
                damageRange.Min = curLife - minLife;
            }
        }

        var damageOutput = EffectOutput.FromDamageRange(targetFighter.Id, runningEffect.Caster.Id, runningEffect.SpellEffect.ActionId, damageRange);
        if (!ActionIdHelper.IsFakeDamage(runningEffect.GetSpellEffect().ActionId))
        {
            damageOutput.Shield = targetFighter.GetPendingShield();
        }
        


        return damageOutput;
    }

    /// <summary>
    /// Applies damage multipliers to the damage range based on various factors, such as caster's and target's characteristics and properties.
    /// </summary>
    /// <param name="runningEffect">The RunningEffect instance.</param>
    /// <param name="damageRange">The DamageRange instance representing the damage to be multiplied.</param>
    /// <param name="targetFighter">The target HaxeFighter instance.</param>
    /// <param name="isMelee">Indicates whether the effect is melee.</param>
    /// <returns>A DamageRange instance representing the multiplied damage.</returns>
    public static DamageRange ApplyDealtMultiplier(RunningEffect runningEffect, DamageRange damageRange,
                                                   HaxeFighter targetFighter, bool isMelee = false)
    {
        var caster = runningEffect.GetCaster();

        if (ActionIdHelper.IsBoostable(runningEffect.GetSpellEffect().ActionId))
        {
            damageRange.Multiply(caster.GetCurrentDealtDamageMultiplierCategory(runningEffect.GetSpell().IsWeapon) / 100d);
            damageRange.Multiply(caster.GetCurrentDealtDamageMultiplierMelee(isMelee) / 100d);
            damageRange.Multiply(targetFighter.GetCurrentReceivedDamageMultiplierCategory(runningEffect.GetSpell().IsWeapon) / 100d);
            damageRange.Multiply(targetFighter.GetCurrentReceivedDamageMultiplierMelee(isMelee) / 100d);
            damageRange.Multiply(caster.Data.GetCharacteristicValue(StatId.DealtDamageMultiplier) / 100d);
        }

        damageRange.Multiply(targetFighter.GetDamageMultiplicator(runningEffect, isMelee, damageRange.IsCollision) / 100d);
        return damageRange;
    }

    /// <summary>
    /// Reflects a portion of the damage taken by the target fighter back to the attacker.
    /// </summary>
    /// <param name="context">The fight context.</param>
    /// <param name="runningEffect">The running effect of the spell being cast.</param>
    /// <param name="damageRange">The range of damage inflicted by the spell.</param>
    /// <param name="targetFighter">The target fighter receiving the damage.</param>
    /// <returns>Returns the DamageRange to be reflected back to the attacker, or null if no reflection occurs.</returns>
    public static DamageRange? ReflectDamage(FightContext context, RunningEffect runningEffect, DamageRange damageRange,
                                             HaxeFighter targetFighter)
    {
        var spellEffect            = runningEffect.GetSpellEffect();
        var characteristicValue    = targetFighter.Data.GetCharacteristicValue(StatId.ReflectDamage);
        var dynamicalDamageReflect = targetFighter.GetDynamicalDamageReflect();
        var reflectionRange        = new DamageRange(characteristicValue, characteristicValue);
        reflectionRange.AddInterval(dynamicalDamageReflect);

        if (reflectionRange.Min == 0 && reflectionRange.Max == 0)
        {
            return null;
        }

        var copiedDamageRange = (DamageRange)damageRange.Copy();
        
        if (ActionIdHelper.IsBoostable(spellEffect.ActionId))
        {
            var casterCharacteristicValue = runningEffect.GetCaster().Data.GetCharacteristicValue(StatId.DealtDamageMultiplier);
            copiedDamageRange.Multiply(1 + (casterCharacteristicValue - 100) / 100.0);
        }

        copiedDamageRange.MaximizeByInterval(reflectionRange);
        var targetMainResist = targetFighter.GetElementMainResist(damageRange.ElemId);
        copiedDamageRange.MaximizeByInterval(copiedDamageRange.Copy().Multiply(1 - targetMainResist / 100.0));
        return copiedDamageRange;
    }

    /// <summary>
    /// Calculates the flat resistance value based on the running effect, target fighter, and element ID.
    /// </summary>
    /// <param name="runningEffect">The running effect instance.</param>
    /// <param name="targetFighter">The target fighter instance.</param>
    /// <param name="elementId">The ID of the element.</param>
    /// <returns>Returns the calculated flat resistance value.</returns>
    public static int GetFlatResistance(RunningEffect runningEffect, HaxeFighter targetFighter, int elementId)
    {
        var isCritical = runningEffect.GetSpellEffect().IsCritical;
        var resistance = 0;

        if (isCritical && !runningEffect.GetSpell().GetEffects().Contains(runningEffect.GetSpellEffect()))
        {
            resistance += targetFighter.Data.GetCharacteristicValue(StatId.CriticalDamageReduction);
        }

        return resistance + targetFighter.GetElementMainReduction(elementId);
    }

    /// <summary>
    /// Calculates the split damage between multiple targets based on the fight context, running effect, damage range, target fighter, and an optional preview flag.
    /// </summary>
    /// <param name="fightContext">The fight context instance.</param>
    /// <param name="runningEffect">The running effect instance.</param>
    /// <param name="damageRange">The damage range instance.</param>
    /// <param name="targetFighter">The target fighter instance.</param>
    /// <param name="isPreview">Optional boolean flag indicating if the split damage calculation is a preview. Defaults to false.</param>
    /// <returns>Returns an array of split damage values.</returns>
    public static List<EffectOutput>? GetSplitDamage(FightContext fightContext, RunningEffect runningEffect,
                                                      DamageRange damageRange, HaxeFighter targetFighter,
                                                      bool isPreview = false)
    {
        if (ActionIdHelper.IsDrag(runningEffect.GetSpellEffect().ActionId) || runningEffect.IsReflect)
        {
            return null;
        }

        var sharingDamageTargets = targetFighter.GetSharingDamageTargets(fightContext);
        if (sharingDamageTargets.Count == 0)
        {
            return null;
        }

        var splitDamageResults = new List<EffectOutput>();
        foreach (var group in sharingDamageTargets)
        {
            if (group.Count == 0)
            {
                continue;
            }

            var currentSplitDamage = (DamageRange)damageRange.Copy();
            currentSplitDamage.Multiply(1d / (group.Count * sharingDamageTargets.Count));

            foreach (var fighter in group)
            {
                var areCellsAdjacent = MapTools.AreCellsAdjacent(runningEffect.GetCaster().GetCurrentPositionCell(), fighter.GetCurrentPositionCell());

                if (fighter != targetFighter)
                {
                    var dodgeResult = ExecuteDodge(fightContext, runningEffect, fighter, areCellsAdjacent, isPreview);
                    
                    if (dodgeResult.Done)
                    {
                        splitDamageResults.AddRange(dodgeResult.Damage);
                    }
                }

                IList<EffectOutput> damageOutput = new[]
                {
                    ApplyDamage(runningEffect, fighter, (DamageRange)currentSplitDamage.Copy(), areCellsAdjacent, isPreview),
                };

                splitDamageResults.AddRange(damageOutput);
            }
        }

        return splitDamageResults;
    }

    /// <summary>
    /// Calculates the life steal effect for a given damage range and target fighter.
    /// </summary>
    /// <param name="runningEffect">The running effect instance.</param>
    /// <param name="damageRange">The damage range that the life steal will be based on.</param>
    /// <param name="targetFighter">The target fighter that the life steal will be applied to.</param>
    /// <returns>An EffectOutput instance containing the life steal effect, or null if the effect doesn't apply.</returns>
    public static EffectOutput? GetLifeStealEffect(RunningEffect runningEffect, DamageRange damageRange,
                                                   HaxeFighter targetFighter)
    {
        EffectOutput? lifeStealEffect = null;

        if (damageRange.Min != 0 || damageRange.Max != 0 && !damageRange.IsInvulnerable)
        {
            damageRange.Multiply(LifeStealMultiplicator);
            var lifePointsWinEffect      = ExecuteLifePointsWin(runningEffect, damageRange, targetFighter);
            var lifePointsWinDamageRange = lifePointsWinEffect.DamageRange;

            if (lifePointsWinDamageRange != null &&
                (lifePointsWinDamageRange.Min != 0 || lifePointsWinDamageRange.Max != 0))
            {
                lifeStealEffect = lifePointsWinEffect;
            }
        }

        return lifeStealEffect;
    }

    /// <summary>
    /// Executes the life points win effect (healing) for a given damage range and target fighter.
    /// </summary>
    /// <param name="runningEffect">The running effect instance.</param>
    /// <param name="damageRange">The damage range representing the healing effect.</param>
    /// <param name="targetFighter">The target fighter that the healing will be applied to.</param>
    /// <returns>An EffectOutput instance containing the healing effect.</returns>
    public static EffectOutput ExecuteLifePointsWin(RunningEffect runningEffect, DamageRange damageRange,
                                                    HaxeFighter targetFighter)
    {
        damageRange.IsHeal = true;

        if (!runningEffect.HasDamageToHeal && ActionIdHelper.IsDealtHealMultiplierAppliable(runningEffect.GetSpellEffect().ActionId))
        {
            damageRange.Multiply(runningEffect.GetCaster().Data.GetCharacteristicValue(StatId.DealtHealMultiplier) / 100d);
        }

        if (targetFighter.HasStateEffect(5))
        {
            damageRange.IsInvulnerable = true;
        }
        else
        {
            var lifePointsInterval = targetFighter.GetPendingMaxLifePoints().SubInterval(targetFighter.GetPendingLifePoints());
            damageRange.MaximizeByInterval(lifePointsInterval.MinimizeBy(0));
            damageRange.MinimizeBy(0);
        }

        return EffectOutput.FromDamageRange(targetFighter.Id, runningEffect.Caster.Id, runningEffect.SpellEffect.ActionId, damageRange);
    }

    /// <summary>
    /// Executes the dodge effect for a given target fighter.
    /// </summary>
    /// <param name="fightContext">The fight context instance.</param>
    /// <param name="runningEffect">The running effect instance.</param>
    /// <param name="targetFighter">The target fighter that the dodge will be applied to.</param>
    /// <param name="isMelee">Optional parameter indicating if the effect is melee (default is false).</param>
    /// <param name="isPreview">Optional parameter indicating if the dodge effect is a preview (default is false).</param>
    /// <returns>A DodgeResult instance containing the results of the dodge.</returns>
    public static DodgeResult ExecuteDodge(FightContext fightContext, RunningEffect runningEffect,
                                           HaxeFighter targetFighter, bool isMelee = false, bool isPreview = false)
    {
        var dodgeResult = new DodgeResult();

        if (!isPreview && isMelee && runningEffect.GetCaster() != targetFighter)
        {
            var dodgeDirection = targetFighter.Data.ResolveDodge();
            if (dodgeDirection != -1)
            {
                dodgeResult.Done   = true;
                dodgeResult.Damage = PushUtils.Push(fightContext, runningEffect, targetFighter, dodgeDirection, true, true, isPreview);
                return dodgeResult;
            }
        }

        return dodgeResult;
    }
}