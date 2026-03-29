using Bubble.Core.Datacenter.Datacenter.Effects;
using Bubble.DamageCalculation.Customs;
using Bubble.DamageCalculation.FighterManagement;
using Bubble.DamageCalculation.SpellManagement;
using Bubble.DamageCalculation.Tools;

namespace Bubble.DamageCalculation.DamageManagement;

public static class DamageSender
{
    private static readonly double[] MidlifeDamagePercent;

    static DamageSender()
    {
        MidlifeDamagePercent = new double[52];
        for (var i = 0; i < 52; i++)
        {
            var value = (1 + Math.Cos(2 * Math.PI * i * 0.01)) * 0.5;
            MidlifeDamagePercent[i] = value * value / 4;
        }
    }

    /// <summary>
    /// Calculates the total damage for a given context, effect, and an optional flag to use the critical hit value.
    /// </summary>
    /// <param name="fightContext">The context of the fight.</param>
    /// <param name="runningEffect">The effect being executed.</param>
    /// <param name="isPreview">Optional flag to know it's its in preview. Default is false.</param>
    /// <returns>A DamageRange representing the total damage.</returns>
    public static DamageRange GetTotalDamage(FightContext fightContext, RunningEffect runningEffect, bool isPreview = false)
    {
        var caster      = runningEffect.GetCaster();
        var spellEffect = runningEffect.GetSpellEffect();
        var damageRange = DamageRange.FromSpellEffect(spellEffect, fightContext.IsSimulation);

        if (ActionIdHelper.IsBasedOnActionsPoints(spellEffect.ActionId) || ActionIdHelper.IsBasedOnMovementPointsUsed(spellEffect.ActionId))
        {
            damageRange.Min = damageRange.Max;
        }

        var firstEffect = runningEffect.GetFirstParentEffect() ?? runningEffect;
        if (firstEffect.Caster.Data.HasGod())
        {
            damageRange.Min = 666000;
            damageRange.Max = 666000;
        }
        
        if (damageRange.ElemId == 6)
        {
            damageRange.ElemId = caster.GetBestElement();
        }
        else if (damageRange.ElemId == 7)
        {
            damageRange.ElemId = caster.GetWorstElement();
        }

        if (caster.IsUnlucky())
        {
            damageRange.MaximizeBy(damageRange.Min);
        }

        //damageRange.Add(caster.Data.GetItemSpellBaseDamageModification(runningEffect.GetSpell().Id));

        if (ActionIdHelper.IsBasedOnCasterLife(spellEffect.ActionId))
        {
            damageRange = GetDamageBasedOnCasterLife(runningEffect, damageRange);
        }

        if (ActionIdHelper.IsSplash(spellEffect.ActionId))
        {
            var target = spellEffect.ActionId == ActionId.FightCasterSplashHeal
                ? fightContext.GetFighterFromCell(fightContext.TargetedCell)!
                : runningEffect.GetCaster();

            if (target == null)
            {
                return damageRange;
            }
            
            damageRange = GetSplashDamage(runningEffect, damageRange, target);
        }

        if (ActionIdHelper.IsBoostable(spellEffect.ActionId))
        {
            damageRange = GetBoostableDamage(runningEffect, damageRange);
        }

        if (ActionIdHelper.IsBasedOnMovementPoints(spellEffect.ActionId))
        {
            var movementPoints = caster.Data.GetCharacteristicValue(StatId.MovementPoints);
            if (movementPoints <= 0)
            {
                damageRange.SetToZero();
            }
            else
            {
                damageRange.Multiply(movementPoints / (movementPoints + (double)caster.Data.GetUsedPm()));
            }
        }

        damageRange.Add(caster.Data.GetDamageHealEquipmentSpellMod(runningEffect.GetSpell().Id, runningEffect.SpellEffect.ActionId));
        damageRange.IsHeal = ActionIdHelper.IsHeal(spellEffect.ActionId);
        damageRange.FromBuff = runningEffect.IsTriggered;
        
        return damageRange;
    }

    /// <summary>
    /// Calculates the total shield value for a given effect.
    /// </summary>
    /// <param name="runningEffect">The effect being executed.</param>
    /// <returns>A DamageRange representing the total shield value.</returns>
    public static DamageRange GetTotalShield(RunningEffect runningEffect)
    {
        var caster      = runningEffect.GetCaster();
        var spellEffect = runningEffect.GetSpellEffect();
        var shieldRange = new DamageRange(0, 0)
        {
            IsHeal         = true,
            IsShieldDamage = true,
            IsCritical     = spellEffect.IsCritical,
        };

        var      actionId = spellEffect.ActionId;
        Interval damageInterval;
        Interval casterInterval;

        if (actionId == ActionId.CharacterBoostShield)
        {
            shieldRange.AddInterval(spellEffect.GetDamageInterval());
        }
        else if (actionId == ActionId.CharacterBoostShieldBasedOnCasterLife)
        {
            damageInterval = spellEffect.GetDamageInterval();
            casterInterval = new Interval(caster.Data.GetMaxHealthPoints(), caster.Data.GetMaxHealthPoints());
            shieldRange.AddInterval(casterInterval);
            shieldRange.MultiplyInterval(damageInterval).Multiply(0.01);
        }
        else if (actionId == ActionId.CharacterBoostShieldBasedOnCasterLevel)
        {
            damageInterval     = spellEffect.GetDamageInterval();
            casterInterval     = new Interval(caster.Level, caster.Level).MultiplyInterval(damageInterval);
            casterInterval.Min = MathUtils.Round(casterInterval.Min * 0.01);
            casterInterval.Max = MathUtils.Round(casterInterval.Max * 0.01);
            shieldRange.AddInterval(casterInterval);
        }

        return shieldRange;
    }

    /// <summary>
    /// Calculates the total heal bonus for a given effect.
    /// </summary>
    /// <param name="runningEffect">The effect being executed.</param>
    /// <param name="target"></param>
    /// <returns>A DamageRange representing the total shield value.</returns>
    public static DamageRange GetTotalHealBonus(RunningEffect runningEffect, HaxeFighter target)
    {
        var spellEffect = runningEffect.GetSpellEffect();
        var healRange = new DamageRange(0, 0)
        {
            IsHeal         = true,
            IsShieldDamage = true,
            IsCritical     = spellEffect.IsCritical,
        };

        var actionId = spellEffect.ActionId;

        if (actionId is ActionId.CharacterBoostVitalityPercent or ActionId.CharacterDeboostVitalityPercent)
        {
            var damageInterval = spellEffect.GetDamageInterval();
            var maxHealth      = target.Data.GetMaxHealthPoints();

            var casterInterval = new Interval(maxHealth, maxHealth);
            healRange.AddInterval(casterInterval);
            healRange.MultiplyInterval(damageInterval).Multiply(0.01);
        }
        else if (actionId is ActionId.CharacterBoostVitalityPercentStatic or ActionId.CharacterDeboostVitalityPercentStatic)
        {
            var damageInterval = spellEffect.GetDamageInterval();
            var maxHealth      = target.Data.GetMaxHealthPointsWithoutContext();
            
            var casterInterval = new Interval(maxHealth, maxHealth);
            healRange.AddInterval(casterInterval);
            healRange.MultiplyInterval(damageInterval).Multiply(0.01);
        }

        if (actionId is ActionId.CharacterDeboostVitalityPercentStatic or ActionId.CharacterDeboostVitalityPercent)
        {
            // invert the number
            healRange.Multiply(-1);
        }

        return healRange;
    }

    /// <summary>
    /// Calculates the damage based on the caster's life for a given effect and an initial damage range.
    /// </summary>
    /// <param name="runningEffect">The effect being executed.</param>
    /// <param name="initialDamage">The initial damage range before considering caster's life.</param>
    /// <returns>A DamageRange representing the damage based on the caster's life.</returns>
    public static DamageRange GetDamageBasedOnCasterLife(RunningEffect runningEffect, DamageRange initialDamage)
    {
        var      caster      = runningEffect.GetCaster();
        var      spellEffect = runningEffect.GetSpellEffect();
        Interval lifeInterval;

        if (ActionIdHelper.IsBasedOnCasterLifePercent(spellEffect.ActionId))
        {
            lifeInterval = caster.GetPendingLifePoints();
            initialDamage.MultiplyInterval(lifeInterval);
            initialDamage.Multiply(0.01);
        }
        else if (ActionIdHelper.IsBasedOnCasterLifeMissing(spellEffect.ActionId))
        {
            lifeInterval = caster.GetPendingMissingLifePoints();
            initialDamage.MultiplyInterval(lifeInterval);
            initialDamage.Multiply(0.01);
        }
        else if (ActionIdHelper.IsBasedOnCasterLifeMissingMaxLife(spellEffect.ActionId))
        {
            var baseCharacteristicValue = caster.Data.GetCharacteristicValue(StatId.CurPermanentDamage);
            if (baseCharacteristicValue >= 0)
            {
                initialDamage.Multiply(baseCharacteristicValue / 100d);
            }
        }
        else if (ActionIdHelper.IsBasedOnCasterLifeMidlife(spellEffect.ActionId))
        {
            lifeInterval = caster.GetPendingLifePoints();
            var midLifeInterval = new Interval(0, 0)
            {
                Min = 100 * lifeInterval.Min / caster.Data.GetMaxHealthPoints() - 50,
                Max = 100 * lifeInterval.Max / caster.Data.GetMaxHealthPoints() - 50,
            };

            midLifeInterval.Abs().MinimizeBy(0).MaximizeBy(50);

            initialDamage.Min *= (int)(caster.Data.GetCharacteristicValue(StatId.LifePoints) *
                MidlifeDamagePercent[midLifeInterval.Min] / 100d);
            initialDamage.Max *= (int)(caster.Data.GetCharacteristicValue(StatId.LifePoints) *
                MidlifeDamagePercent[midLifeInterval.Max] / 100d);
        }

        return initialDamage;
    }

    /// <summary>
    /// Calculates the splash damage for a given effect, damage range, and target fighter.
    /// </summary>
    /// <param name="runningEffect">The effect being executed.</param>
    /// <param name="initialDamage">The initial damage range before considering splash damage.</param>
    /// <param name="targetFighter">The target fighter affected by the splash damage.</param>
    /// <returns>A DamageRange representing the splash damage.</returns>
    public static DamageRange GetSplashDamage(RunningEffect runningEffect, DamageRange initialDamage,
                                              HaxeFighter targetFighter)
    {
        var spellEffect = runningEffect.GetSpellEffect();
        var finalDamage = DamageRange.Zero;

        if (ActionIdHelper.IsSplash(spellEffect.ActionId))
        {
            if (ActionIdHelper.IsSplashFinalDamage(spellEffect.ActionId) ||
                ActionIdHelper.IsSplashHeal(spellEffect.ActionId))
            {
                foreach (var output in targetFighter.PendingEffects)
                {
                    var damage = output.DamageRange;
                    if (damage != null && !damage.IsHeal && !damage.IsInvulnerable && !damage.IsCollision &&
                        (damage.Min != 0 || damage.Max != 0))
                    {
                        finalDamage = damage;
                    }
                }
            }
            else if (ActionIdHelper.IsSplashRawDamage(spellEffect.ActionId) &&
                     targetFighter.LastRawDamageTaken != null && targetFighter.LastTheoreticalRawDamageTaken != null)
            {
                finalDamage = targetFighter.IsPacifist() ? targetFighter.LastRawDamageTaken : targetFighter.LastTheoreticalRawDamageTaken;
            }

            finalDamage = (DamageRange)finalDamage.Copy();
            finalDamage.MultiplyInterval(spellEffect.GetDamageInterval());
            finalDamage.Multiply(0.01);
            initialDamage.Min    = finalDamage.Min;
            initialDamage.Max    = finalDamage.Max;
            initialDamage.ElemId = finalDamage.ElemId;

            if (spellEffect.ActionId == ActionId.FightSplashFinalTakenDamage)
            {
                runningEffect.GetSpellEffect().ActionId =
                    ActionIdHelper.GetSplashFinalTakenDamageElement(initialDamage.ElemId);
            }
            else if (spellEffect.ActionId == ActionId.FightSplashRawTakenDamage)
            {
                runningEffect.GetSpellEffect().ActionId =
                    ActionIdHelper.GetSplashRawTakenDamageElement(initialDamage.ElemId);
            }
        }

        return initialDamage;
    }

    /// <summary>
    /// Calculates the boostable damage for a given effect and damage range.
    /// </summary>
    /// <param name="runningEffect">The effect being executed.</param>
    /// <param name="initialDamage">The initial damage range before considering boostable damage.</param>
    /// <returns>A DamageRange representing the boostable damage.</returns>
    public static DamageRange GetBoostableDamage(RunningEffect runningEffect, DamageRange initialDamage)
    {
        initialDamage.Add(BaseDamageBonus(runningEffect));
        initialDamage.Multiply(ApplyDamageBonus(runningEffect));
        initialDamage.Add(FlatDamageBonus(runningEffect));

        if (runningEffect.GetSpell().IsWeapon)
        {
            initialDamage.Multiply(
                (100 + runningEffect.GetCaster().Data.GetCharacteristicValue(StatId.WeaponDamagesPercent)) * 0.01);
        }

        initialDamage.Multiply(BombComboBonus(runningEffect));

        return initialDamage;
    }

    /// <summary>
    /// Calculates the flat damage bonus.
    /// </summary>
    /// <param name="runningEffect">The running effect instance.</param>
    /// <returns>Returns the calculated flat damage bonus as an integer.</returns>
    public static int FlatDamageBonus(RunningEffect runningEffect)
    {
        var caster      = runningEffect.GetCaster();
        var spellEffect = runningEffect.GetSpellEffect();
        var spell       = runningEffect.GetSpell();

        var flatDamageBonus = (ActionIdHelper.IsHeal(spellEffect.ActionId)
                                  ? caster.Data.GetCharacteristicValue(StatId.HealBonus)
                                  : caster.Data.GetCharacteristicValue(StatId.AllDamagesBonus)) +
                              (spell.IsTrap ? caster.Data.GetCharacteristicValue(StatId.TrapDamageBonus) : 0);
        
        if (!ActionIdHelper.IsHeal(spellEffect.ActionId))
        {
            flatDamageBonus += caster.GetElementFlatDamageBonus(ElementsHelper.GetElementFromActionId(spellEffect.ActionId)) +
                               (spellEffect.IsCritical && runningEffect.GetSpell().GetEffects().IndexOf(spellEffect) == -1
                                   ? caster.Data.GetCharacteristicValue(StatId.CriticalDamageBonus)
                                   : 0);

            if (runningEffect.GetSpell().IsWeapon && spellEffect.IsCritical)
            {
                flatDamageBonus += runningEffect.GetSpell().CriticalHitBonus;
            }
        }

        return flatDamageBonus + caster.GetDamageHealContextSpellMod(runningEffect.Spell.Id);
    }

    /// <summary>
    /// Retrieves the base damage bonus.
    /// </summary>
    /// <param name="runningEffect">The running effect instance.</param>
    /// <returns>Returns the base damage bonus as an integer.</returns>
    public static int BaseDamageBonus(RunningEffect runningEffect)
    {
        return runningEffect.GetCaster().GetSpellBaseDamageModification(runningEffect.GetSpell().Id);
    }

    /// <summary>
    /// Calculates the damage bonus.
    /// </summary>
    /// <param name="runningEffect">The running effect instance.</param>
    /// <returns>Returns the calculated damage bonus as a float.</returns>
    public static float GetDamageBonus(RunningEffect runningEffect)
    {
        var caster      = runningEffect.GetCaster();
        var spellEffect = runningEffect.GetSpellEffect();
        var spell       = runningEffect.GetSpell();
        var element     = ElementsHelper.GetElementFromActionId(spellEffect.ActionId);
        if (element == 6)
        {
            element = caster.GetBestElement();
        }
        else if (element == 7)
        {
            element = caster.GetWorstElement();
        }

        float damageBonus = (ActionIdHelper.IsHeal(spellEffect.ActionId)
                                ? 0
                                : caster.Data.GetCharacteristicValue(StatId.DamagesPercent)) +
                            (spell.IsTrap ? caster.Data.GetCharacteristicValue(StatId.TrapDamageBonusPercent) : 0) +
                            (spell.IsGlyph ? caster.Data.GetCharacteristicValue(StatId.GlyphPower) : 0) +
                            (spell.IsRune ? caster.Data.GetCharacteristicValue(StatId.RunePower) : 0) + (spell.IsWeapon
                                ? caster.Data.GetCharacteristicValue(StatId.WeaponPower)
                                : caster.Data.GetCharacteristicValue(StatId.DamagesPercentSpell) +
                                  caster.GetDamageHealContextSpellMod(runningEffect.GetSpell().Id)) +
                            caster.GetElementMainStat(element);
        if (damageBonus < 0)
        {
            damageBonus = 0;
        }

        return damageBonus;
    }

    /// <summary>
    /// Applies the damage bonus.
    /// </summary>
    /// <param name="runningEffect">The running effect instance.</param>
    /// <returns>Returns the applied damage bonus as a float.</returns>
    public static float ApplyDamageBonus(RunningEffect runningEffect)
    {
        return (100 + GetDamageBonus(runningEffect)) * 0.01f;
    }

    /// <summary>
    /// Applies the bomb combo bonus.
    /// </summary>
    /// <param name="runningEffect">The running effect instance.</param>
    /// <returns>Returns the applied damage bonus as a float.</returns>
    public static float BombComboBonus(RunningEffect runningEffect)
    {
        return 1 + runningEffect.Caster.Data.GetCharacteristicValue(StatId.BombComboBonus) / 100f;
    }
}