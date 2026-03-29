using Bubble.Core.Datacenter.Datacenter.Effects;
using Bubble.DamageCalculation.Customs;
using Bubble.DamageCalculation.DamageManagement;
using Bubble.DamageCalculation.SpellManagement;
using Bubble.DamageCalculation.Tools;

namespace Bubble.DamageCalculation.FighterManagement;

public class HaxeBuff
{
    private static readonly string[] DamageTriggers =
    {
        "D", "DN", "DE", "DF", "DW",
        "DA", "DG", "DT", "DI", "DBA",
        "DBE", "DM", "DR", "DCAC", "DS",
        "PD", "PMD", "PPD", "DV",
    };

    public uint Id { get; set; }
    public HaxeSpellEffect Effect { get; set; }
    public HaxeSpell Spell { get; set; }
    public RunningEffect RunningEffect { get; }
    public int TriggerCount { get; set; }
    public int StartTriggerCount { get; set; }
    public HaxeSpellState? SpellState { get; set; }
    public HaxeFighter Caster { get; set; }
    public long CasterId => Caster.Id;
    public HaxeFighter Target { get; set; }
    public List<long> HasBeenTriggeredOn { get; }
    public ActionId DisplayActionId { get; set; }
    public int SpellModificationId { get; set; }
    public bool IsApplied { get; set; }
    public long AliveSource { get; set; }
    public int TargetedCell { get; set; } = -1;
    public bool ApplyOnNextCasterTurn { get; set; }
    public bool DecrementOnNextCasterTurn { get; set; }
    public bool IsGlobalTurn { get; set; }

    public int LastTriggeredTurn { get; set; } = -1;
    public int AddedAtTurn { get; set; } = -1;
    public bool IsActive => Effect.Delay <= 0 && !ApplyOnNextCasterTurn;
    public bool IsDelayed { get; }
    public string SpellName { get; } = "";
    public RunningEffect? FirstParentEffect { get; set; }
    
    public HaxeBuff(HaxeFighter caster,
        HaxeFighter target, 
        HaxeSpell spell, 
        HaxeSpellEffect effect,
        RunningEffect runningEffect,
        int startTriggerCount = 0)
    {
        Caster          = caster;
        Target          = target;
        Spell           = spell;
        RunningEffect   = runningEffect;
        Effect          = effect.Clone();
        DisplayActionId = effect.ActionId;
        AddedAtTurn     = caster.Data.GetCurrentTurn();
        IsDelayed       = effect.Delay > 0;
        
        if (Effect.Duration == -1)
        {
            Effect.Duration = -1000;
        }

        FirstParentEffect = runningEffect.GetFirstParentEffect();
        HasBeenTriggeredOn = new List<long>();
        TriggerCount       = StartTriggerCount = startTriggerCount;
        
        SpellModificationId = ActionIdHelper.GetSpellModificationIdFromActionId(effect.ActionId);
        
        if (effect.ActionId == ActionId.FightSetState || 
            effect.ActionId == ActionId.FightUnsetState || 
            effect.ActionId == ActionId.FightDisableState)
        {
            SpellState = DamageCalculator.DataInterface.CreateStateFromId(effect.Param3);
        }

        if(ActionIdHelper.IsSpellExecution(effect.ActionId))
        {
            SpellName = DamageCalculator.DataInterface.CreateSpellFromId(effect.Param1, effect.Param2)?.Name ?? "Unknown";
        }

        AliveSource = caster.Id; // caster.Data.GetCurrentFighter(); 
       
        var triggeredEffect = runningEffect.GetLastTriggeredEffect();
        var parentEffect = runningEffect.GetFirstParentEffect();
     
        if(parentEffect != null)
        {
            AliveSource = parentEffect.Caster.Id;
        }
        
        if (runningEffect.TriggeringFighter != null && triggeredEffect != null && triggeredEffect.Caster.IsAlive())
        {
            AliveSource = triggeredEffect.Caster.Id;
        }
        
        if (runningEffect.TriggeringFighter != null && runningEffect.SpellEffect.Triggers.Any(x => x.StartsWith("D")))
        {
            AliveSource = runningEffect.TriggeringFighter.Id;
        }
        
        // Kimbo, Glyphe Paire et Impaire
        if(effect.Param3 is 29 or 30 && effect.ActionId == ActionId.FightSetState && runningEffect.IsTriggered)
        {
            // we get the kimbo summon
            var summon = caster.Data.GetSummonIds().FirstOrDefault();
            
            if(summon != 0)
            {
                AliveSource = summon;
                Effect.TurnDuration++;
            }
            else
            {
                AliveSource = caster.Data.GetCurrentFighter();
            }
        }

        if(runningEffect.Spell.IsGlyph && runningEffect.Spell.Id == 3487)
        {
            AliveSource = caster.Data.GetCurrentFighter();
        }

        if (!caster.Data.FightStarted && (!effect.Triggers.Contains("TB") && !effect.Triggers.Contains("TE")))
        {
            IsGlobalTurn = true;
        }
        
        if(!caster.Data.FightStarted && (effect.Triggers.Contains("TB") || effect.Triggers.Contains("TE")))
        {
            AddedAtTurn = 1;
        }

        if ((caster.Data.NobodyHasPlayed()) && !IsState() && (effect.Triggers.Contains("TB") ||
                                                              effect.Triggers.Contains("TE") || 
                                                              effect.Triggers.Contains("I")))
        {
             ApplyOnNextCasterTurn = false;
        }

        if (caster.IsPlaying() &&
            /*runningEffect.IsTriggered &&*/
            /*Effect.Triggers.Contains("TE") This cause Libration (Sacrieur) to be applied twice ||*/
            Effect.Triggers.Contains("TB") &&
            Effect.TurnDuration > 0)
        {
            Effect.TurnDuration++;
        }
        /*else
        {
            if (!target.Data.HasPlayedThisTurn() && effect.TurnDuration == 1)
            {
                //Effect.Duration++;
                Effect.TurnDuration++;
            }
            if (!target.Data.HasPlayedThisTurn() && effect.Delay == 1)
            {
                Effect.Delay++;
            }
        }*/
    }

    public void ResetTriggeredOn()
    {
        TriggerCount = StartTriggerCount;
        HasBeenTriggeredOn.Clear();
    }

    public static HaxeBuff FromRunningEffect(HaxeFighter target, RunningEffect runningEffect)
    {
        return new HaxeBuff(runningEffect.GetCaster(), target, runningEffect.GetSpell(),
                            runningEffect.GetSpellEffect(), runningEffect);
    }

    public bool ShouldBeTriggeredOnCaster(EffectOutput triggeringOutput, RunningEffect runningEffect, HaxeFighter target, bool isMelee, FightContext fightContext)
    {
        if (fightContext.UsingPortal() && Effect.Triggers.Contains("PST", StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }
        
        if (!target.IsAlive())
        {
            if (Effect.Triggers.Contains("K", StringComparer.OrdinalIgnoreCase)) // Kill
            {
                return true;
            }

            if (runningEffect.GetSpell().IsWeapon &&
                Effect.Triggers.Contains("KWW", StringComparer.OrdinalIgnoreCase)) // KillWithSpellWeapon
            {
                return true;
            }

            if (!runningEffect.GetSpell().IsWeapon &&
                Effect.Triggers.Contains("KWS", StringComparer.OrdinalIgnoreCase)) // KillWithoutSpellWeapon
            {
                return true;
            }

            return false;
        }

        if (triggeringOutput.Movement != null &&
            fightContext.Map.IsCellWalkable(triggeringOutput.Movement.NewPosition) &&
            Effect.Triggers.Contains("PO", StringComparer.OrdinalIgnoreCase)) // PositionOccupied
        {
            return true;
        }

        if (triggeringOutput.AttemptedApTheft &&
            Effect.Triggers.Contains("CAPA", StringComparer.OrdinalIgnoreCase)) // CasterAttemptedApTheft
        {
            return true;
        }

        if (triggeringOutput.AttemptedAmTheft &&
            Effect.Triggers.Contains("CMPA", StringComparer.OrdinalIgnoreCase)) // CasterAttemptedMpTheft
        {
            return true;
        }

        if (triggeringOutput.InvisibilityState == true &&
            (Effect.Triggers.Contains("ION", StringComparer.OrdinalIgnoreCase) ||
             Effect.Triggers.Contains("CION", StringComparer.OrdinalIgnoreCase))) // InvisibleOn, CasterInvisibleOn
        {
            return true;
        }

        if (triggeringOutput.InvisibilityState == false && 
            (Effect.Triggers.Contains("IOFF", StringComparer.OrdinalIgnoreCase) ||
             Effect.Triggers.Contains("CIOFF", StringComparer.OrdinalIgnoreCase))) // InvisibleOff, CasterInvisibleOff
        {
            return true;
        }

        if (triggeringOutput.Summon != null && Effect.Triggers.Contains("CI", StringComparer.Ordinal))
        {
            return true;
        }
        
        if (triggeringOutput.Dispell && Effect.Triggers.Contains("CDIS", StringComparer.Ordinal)) // CasterDispell
        {
            return true;
        }

        // There is no more trigger to check
        if (triggeringOutput.DamageRange == null)
        {
            return false;
        }

        if (triggeringOutput.DamageRange.IsCritical && Effect.Triggers.Contains("CC", StringComparer.OrdinalIgnoreCase)) // CriticalCast
        {
            return true;
        }

        // There is no more trigger to check
        if (triggeringOutput.DamageRange.Min == 0 && triggeringOutput.DamageRange.Max == 0)
        {
            return false;
        }

        if (fightContext.UsingPortal() && Effect.Triggers.Contains("PDT"))
        {
            return true;    
        }
        
        if (triggeringOutput.DamageRange.IsHeal)
        {
            if (triggeringOutput.FighterId == CasterId &&
                Effect.Triggers.Contains("LPU", StringComparer.OrdinalIgnoreCase)) // LifePointsUpdate
            {
                return true;
            }

            if (!triggeringOutput.DamageRange.IsShieldDamage &&
                Effect.Triggers.Contains("CH", StringComparer.OrdinalIgnoreCase) &&
                runningEffect.SpellEffect.ActionId != ActionId.CharacterHealAttackers &&
                runningEffect.SpellEffect.ActionId != ActionId.CharacterGiveLifeWithRatio &&
                !runningEffect.HasDamageToHeal) // CasterHeal
            {
                return true;
            }

            if (triggeringOutput.DamageRange.IsShieldDamage && Effect.Triggers.Contains("CS", StringComparer.OrdinalIgnoreCase)) // CasterShield
            {
                return true;
            }
        }

        switch (triggeringOutput.DamageRange.ElemId)
        {
            case 0:                                                                    // Neutral
                if (Effect.Triggers.Contains("CDN", StringComparer.OrdinalIgnoreCase)) // CasterDamageNeutral
                {
                    return true;
                }

                break;
            case 1:                                                                    // Earth
                if (Effect.Triggers.Contains("CDE", StringComparer.OrdinalIgnoreCase)) // CasterDamageEarth
                {
                    return true;
                }

                break;
            case 2:                                                                    // Fire
                if (Effect.Triggers.Contains("CDF", StringComparer.OrdinalIgnoreCase)) // CasterDamageFire
                {
                    return true;
                }

                break;
            case 3:                                                                    // Water
                if (Effect.Triggers.Contains("CDW", StringComparer.OrdinalIgnoreCase)) // CasterDamageWater
                {
                    return true;
                }

                break;
            case 4:                                                                    // Air
                if (Effect.Triggers.Contains("CDA", StringComparer.OrdinalIgnoreCase)) // CasterDamageAir
                {
                    return true;
                }

                break;
        }


        if (runningEffect.Caster.TeamId == target.TeamId)
        {
            if (Effect.Triggers.Contains("CDBA", StringComparer.OrdinalIgnoreCase)) // CasterDamageAgainstAlly
            {
                return true;
            }
        }
        else if (Effect.Triggers.Contains("CDBE", StringComparer.OrdinalIgnoreCase)) // CasterDamageAgainstEnemy
        {
            return true;
        }

        if (isMelee)
        {
            if (Effect.Triggers.Contains("CDM", StringComparer.OrdinalIgnoreCase)) // CasterDamageMelee
            {
                return true;
            }
        }
        else if (Effect.Triggers.Contains("CDR", StringComparer.OrdinalIgnoreCase)) // CasterDamageRange
        {
            return true;
        }

        if (runningEffect.GetSpell().IsWeapon)
        {
            if (Effect.Triggers.Contains("CDCAC", StringComparer.OrdinalIgnoreCase)) // CasterDamageCorpsACorps
            {
                return true;
            }
        }
        else if (Effect.Triggers.Contains("CDS", StringComparer.Ordinal)) // CasterDamageSpell
        {
            return true;
        }

        if (!triggeringOutput.DamageRange.IsCollision) // CasterDamageCollision
        {
            return ShouldBeTriggeredOnCasterDamage(runningEffect, runningEffect.Caster, isMelee);
        }

        return false;
    }

    public bool ShouldBeTriggeredOnTargetDamage(RunningEffect runningEffect, HaxeFighter caster, bool isMelee, bool isCollision)
    {
        var spell        = runningEffect.GetSpell();
        var spellEffect  = runningEffect.GetSpellEffect();
        var effectCaster = runningEffect.GetCaster();
        
        if (Effect.Triggers.Contains("D", StringComparer.Ordinal) && !isCollision) // Damage
        {
            return true;
        }

        if (isCollision)
        {
            if (Effect.Triggers.Contains("PD", StringComparer.Ordinal)) // PushedDamage
            {
                return true;
            }

            if (runningEffect.SpellEffect.Param2 == 0 &&
                Effect.Triggers.Contains("PMD", StringComparer.Ordinal)) // PushedMeleeDamage
            {
                return true;
            }

            if (Effect.Triggers.Contains("PPD", StringComparer.Ordinal)) // PushedDamage
            {
                return true;
            }

            // There is no more trigger to check
            return false;
        }

        var element = ElementsHelper.GetElementFromActionId(runningEffect.SpellEffect.ActionId);

        switch (element)
        {
            case 6:
                element = effectCaster.GetBestElement();
                break;
            case 7:
                element = effectCaster.GetWorstElement();
                break;
        }

        switch (element)
        {
            case 0:
                if (Effect.Triggers.Contains("DN", StringComparer.Ordinal)) // DamageByNeutral
                {
                    return true;
                }

                break;
            case 1:
                if (Effect.Triggers.Contains("DE", StringComparer.Ordinal)) // DamageByEarth
                {
                    return true;
                }

                break;
            case 2:
                if (Effect.Triggers.Contains("DF", StringComparer.Ordinal)) // DamageByFire
                {
                    return true;
                }

                break;
            case 3:
                if (Effect.Triggers.Contains("DW", StringComparer.Ordinal)) // DamageByWater
                {
                    return true;
                }

                break;
            case 4:
                if (Effect.Triggers.Contains("DA", StringComparer.Ordinal)) // DamageByAir
                {
                    return true;
                }

                break;
        }

        if (spell.IsGlyph && Effect.Triggers.Contains("DG", StringComparer.Ordinal)) // DamageByGlyph
        {
            return true;
        }

        if (spell.IsTrap && Effect.Triggers.Contains("DT", StringComparer.Ordinal)) // DamageByTrap
        {
            return true;
        }

        if (effectCaster.Data.IsSummon() &&
            Effect.Triggers.Contains("DI", StringComparer.Ordinal)) // DamageBySummon
        {
            return true;
        }

        if (effectCaster.TeamId == caster.TeamId)
        {
            if (Effect.Triggers.Contains("DBA", StringComparer.Ordinal)) // DamageByAlly
            {
                return true;
            }

            if (spellEffect.IsCritical &&
                Effect.Triggers.Contains("DCCBA", StringComparer.Ordinal)) // DamageCriticalByAlly
            {
                return true;
            }
        }
        else
        {
            if (Effect.Triggers.Contains("DBE", StringComparer.Ordinal)) // DamageByEnemy
            {
                return true;
            }

            if (spellEffect.IsCritical &&
                Effect.Triggers.Contains("DCCBE", StringComparer.Ordinal)) // DamageCriticalByEnemy
            {
                return true;
            }
        }

        if (isMelee)
        {
            if (Effect.Triggers.Contains("DM", StringComparer.Ordinal)) // DamageMelee
            {
                return true;
            }
        }
        else if (Effect.Triggers.Contains("DR", StringComparer.Ordinal)) // DamageRange
        {
            return true;
        }

        if (spell.IsWeapon &&
            Effect.Triggers.Contains("DCAC", StringComparer.Ordinal)) // DamageCorpsACorps
        {
            return true;
        }

        if (Effect.Triggers.Contains("DS", StringComparer.Ordinal) &&
            (runningEffect.GetParentEffect() == null || !runningEffect.GetParentEffect()!.IsTriggered)) // DamageSpell
        {
            return true;
        }
        
        return false;
    }

    public bool ShouldBeTriggeredOnTarget(EffectOutput triggeringOutput, RunningEffect triggeringRunningEffect,
                                          HaxeFighter target, bool isMelee, FightContext fightContext)
    {
        if (!target.IsAlive())
        {
            var parentEffect = triggeringRunningEffect.GetParentEffect();

            if (!target.IsAlive())
            {
                
            }
            // 1009 is ActionCharacterActivateBomb
            // Death
            if (triggeringOutput.Death && Effect.Triggers.Contains("X", StringComparer.Ordinal))
            {
                if (parentEffect == null || parentEffect.SpellEffect.ActionId != ActionId.CharacterActivateBomb)
                {
                    if(Caster != target)
                        return !target.IsStaticElement;
                    
                    return true;
                }
            }
        }

        if (triggeringOutput.StatId != -1)
        {
            // 97 is StatId.CurLife
            // 11 is StatId.Vitality
            
            return Effect.Triggers.Contains("LPU", StringComparer.Ordinal) && triggeringOutput.StatId is 0 or 11 && triggeringOutput.StatValue > 0; // LifePointsUpdate
        }

        if ( /*triggeringOutput.NewStateId == (int)SpellStateId.Invisible ||*/
            triggeringOutput.InvisibilityState == true) // 250
        {
            return Effect.Triggers.Contains("ION", StringComparer.Ordinal) || // InvisibleOn
                   Effect.Triggers.Contains("OEION", StringComparer.Ordinal); // OnEndInvisibleOn
        }

        if ( /*triggeringOutput.LostStateId == (int)SpellStateId.Invisible ||*/
            triggeringOutput.InvisibilityState == false) // 250
        {
            return Effect.Triggers.Contains("IOFF", StringComparer.Ordinal) || // InvisibleOn
                   Effect.Triggers.Contains("OEIOFF", StringComparer.Ordinal); // OnEndInvisibleOff
        }

        if (triggeringOutput.IsPushed && Effect.Triggers.Contains("P", StringComparer.Ordinal)) // Push
        {
            return true;
        }

        if (triggeringOutput.IsPulled && Effect.Triggers.Contains("MA", StringComparer.Ordinal)) // Pull
        {
            return true;
        }

        if (triggeringOutput.ThroughPortal && Effect.Triggers.Contains("PT", StringComparer.Ordinal)) // Portal
        {
            return true;
        }

        if (triggeringOutput.Movement != null &&
            fightContext.Map.IsCellWalkable((short)triggeringOutput.Movement.NewPosition) &&
            Effect.Triggers.Contains("M", StringComparer.Ordinal)) // Movement
        {
            return true;
        }
        

        if (triggeringOutput.Movement != null && triggeringOutput.Movement.SwappedWith != null &&
            Effect.Triggers.Contains("MS", StringComparer.Ordinal)) // MovementSwap
        {
            return true;
        }

        if (triggeringOutput.Movement != null && !triggeringOutput.Movement.WasInvalid &&
            (triggeringOutput.Movement.FromPandawa || 
             triggeringOutput.ActionId == ActionId.FightRollbackPreviousPosition ||
             (triggeringOutput.ActionId == ActionId.CharacterTeleportOnSameMap && (triggeringOutput.Movement.SwappedWith == null ||
                                                                                         triggeringOutput.Movement.SwappedWith.Id != triggeringOutput.FighterId))) &&
            Effect.Triggers.Contains("TP", StringComparer.Ordinal)) // TeleportPandawa
        {
            return true;
        }

        if (triggeringOutput.ApStolen > 0 && Effect.Triggers.Contains("APA", StringComparer.Ordinal)) // AP
        {
            return true;
        }

        if (triggeringOutput.AmStolen > 0 && Effect.Triggers.Contains("MPA", StringComparer.Ordinal)) // MP
        {
            return true;
        }

        if (triggeringOutput.RangeLoss > 0 && Effect.Triggers.Contains("R", StringComparer.Ordinal)) // Range
        {
            return true;
        }

        if (triggeringOutput.Dispell && Effect.Triggers.Contains("DIS", StringComparer.Ordinal)) // Dispell
        {
            return true;
        }

        if (triggeringOutput.NewStateId != -1 && !triggeringOutput.AreadyHadStateId && HasTriggerState(triggeringOutput.NewStateId, true))
        {
            return true;
        }

        if (triggeringOutput.LostStateId != -1 && HasTriggerState(triggeringOutput.LostStateId, false))
        {
            return true;
        }

        if (Effect.Triggers.Any(x => x.StartsWith("TR")))
        {
            if (triggeringOutput.DamageRange == null)
            {
                return false;
            }
            
            if (target.IsInvulnerable())
            {
                return false;
            }
            
            foreach (var trigger in Effect.Triggers)
            {
                foreach (var buff in target.Buffs)
                {
                    var expectedSpellId = int.Parse(trigger["TR".Length..]);
                
                    if (buff.Effect.ActionId != ActionId.CharacterBoostThreshold)
                    {
                        continue;
                    }
                    
                    /*if(target.GetLifePoint() <= buff.Effect.Param1)
                    {
                        continue;
                    }*/
                    
                    if (target.GetPendingLifePoints().Min <= buff.Effect.Param1 && buff.Spell.Id == expectedSpellId)
                    {
                        return true;
                    }
                }
            }
        }

        if (triggeringOutput.AreLifePointsAffected &&
            Effect.Triggers.Contains("VA", StringComparer.Ordinal)) // LifePoints
        {
            return true;
        }

        if (triggeringOutput.AreMaxLifePointsAffected &&
            Effect.Triggers.Contains("VM", StringComparer.Ordinal)) // MaxLifePoints
        {
            return true;
        }

        if (triggeringOutput.AreErodedLifePointsAffected &&
            Effect.Triggers.Contains("VE", StringComparer.Ordinal)) // ErodedLifePoints
        {
            return true;
        }

        if (triggeringOutput.IsLifeAffected && Effect.Triggers.Contains("V", StringComparer.Ordinal)) // Life
        {
            return true;
        }
        
        if(Effect.Triggers.Contains("TR", StringComparer.Ordinal))
        {
            foreach (var buff in target.Buffs)
            {
                if (buff.Effect.ActionId != ActionId.CharacterBoostThreshold)
                {
                    continue;
                }

                if (target.GetLifePoint() <= buff.Effect.Param1)
                {
                    return true;
                }
            }
        }
        
        // There is no more trigger to check
        if (triggeringOutput.DamageRange == null)
        {
            return false;
        }

        if (!triggeringOutput.DamageRange.IsCollision)
        {
            if (triggeringOutput.DamageRange.IsShieldDamage && Effect.Triggers.Contains("S", StringComparer.Ordinal)) // Shield
            {
                return true;
            }
            
            // There is no more trigger to check
            if (triggeringOutput.DamageRange.IsHeal && triggeringOutput.DamageRange.IsShieldDamage)
            {
                return false;
            }

            if (triggeringOutput.DamageRange.IsHeal)
            {
                if (triggeringOutput.DamageRange.Min > 0 && Effect.Triggers.Contains("V", StringComparer.Ordinal)) // Life
                {
                    return true;
                }

                if (triggeringOutput.FighterId == CasterId &&
                    triggeringOutput.SourceId != CasterId && 
                    Effect.Triggers.Contains("LPU", StringComparer.Ordinal)) // LifePointsUpdate
                {
                    return true;
                }

                if (Effect.Triggers.Contains("H", StringComparer.Ordinal) &&
                    !ActionIdHelper.IsLifeSteal(triggeringRunningEffect.GetSpellEffect().ActionId)) // Heal
                {
                    return true;
                }

                // There is no more trigger to check
                return false;
            }

            return ShouldBeTriggeredOnTargetDamage(triggeringRunningEffect, target, isMelee, false);
        }

        if (Effect.Triggers.Contains("PD", StringComparer.Ordinal)) // PushedDamage
        {
            return true;
        }

        if (triggeringRunningEffect.GetSpellEffect().Param2 == 0 &&
            Effect.Triggers.Contains("PMD", StringComparer.Ordinal)) // PushedMeleeDamage
        {
            return true;
        }

        if (Effect.Triggers.Contains("PPD", StringComparer.Ordinal)) // PushedD?Damage
        {
            return true;
        }

        return false;
    }

    private bool ShouldBeTriggeredOnCasterDamage(RunningEffect runningEffect, HaxeFighter caster, bool isMelee)
    {
        var spellEffect  = runningEffect.GetSpellEffect();
        var spell        = runningEffect.GetSpell();
        var effectCaster = runningEffect.GetCaster();

        if (Effect.Triggers.Contains("CD", StringComparer.Ordinal)) // CasterDamage
        {
            return true;
        }

        if (spell.IsGlyph && Effect.Triggers.Contains("CDG", StringComparer.Ordinal)) // CasterDamageGlyph
        {
            return true;
        }

        if (spell.IsTrap && Effect.Triggers.Contains("CDT", StringComparer.Ordinal)) // CasterDamageTrap
        {
            return true;
        }

        if (effectCaster.TeamId == caster.TeamId)
        {
            if (Effect.Triggers.Contains("CDBA", StringComparer.Ordinal)) // CasterDamageAgainstAlly
            {
                return true;
            }

            if (spellEffect.IsCritical &&
                Effect.Triggers.Contains("CDCCBA", StringComparer.Ordinal)) // CasterDamageCriticalAgainstAlly
            {
                return true;
            }
        }
        else
        {
            if (Effect.Triggers.Contains("CDBE", StringComparer.Ordinal)) // CasterDamageAgainstEnemy
            {
                return true;
            }

            if (spellEffect.IsCritical &&
                Effect.Triggers.Contains("CDCCBE", StringComparer.Ordinal)) // CasterDamageCriticalAgainstEnemy
            {
                return true;
            }
        }

        return false;
    }

    public void ResetTriggerCount()
    {
        TriggerCount = StartTriggerCount;
    }

    public bool IsTriggeredByParent(RunningEffect runningEffect)
    {
        var re = runningEffect;
        while (re != null)
        {
            if (re.GetSpellEffect().Id == Effect.Id)
            {
                return true;
            }

            re = re.GetParentEffect();
        }

        return false;
    }

    public bool IsState()
    {
        return Effect.ActionId == ActionId.FightSetState;
    }

    public bool IsUnsetState()
    {
        return Effect.ActionId == ActionId.FightUnsetState;
    }
    public bool IsDisableState()
    {
        return Effect.ActionId == ActionId.FightDisableState;
    }
    public void IncrementTriggerCount()
    {
        ++TriggerCount;
    }

    public bool HasTrigger(string trigger)
    {
        return Effect.Triggers.Contains(trigger, StringComparer.Ordinal);
    }

    public bool HasDamageTrigger()
    {
        foreach (var trigger in Effect.Triggers)
        {
            if (DamageTriggers.Contains(trigger))
            {
                return true;
            }
        }

        return false;
    }

    public ActionId GetActionId()
    {
        return Effect.ActionId;
    }

    public RunningEffect CreateBasicRunningEffect(FightContext fightContext)
    {
        HaxeSpellEffect spellEffect;
        if (Effect.IsCritical)
        {
            spellEffect            = Effect.Clone();
            spellEffect.IsCritical = false;
        }
        else
        {
            spellEffect = Effect;
        }

        return new RunningEffect(fightContext, fightContext.GetFighterById(CasterId)!, Spell, spellEffect);
    }

    public bool CanBeTriggeredBy(RunningEffect runningEffect)
    {
        var actionId = runningEffect.GetSpellEffect().ActionId;

        if (!ActionIdHelper.CanTriggerDamageMultiplier(actionId))
        {
            if (Effect.ActionId == ActionId.CharacterMultiplyReceivedDamage)
            {
                return false;
            }

            if (Effect.ActionId == ActionId.CharacterLifeLostCasterModerator)
            {
                return false;
            }
        }

        if (ActionIdHelper.IsSplash(actionId) &&
            Effect.ActionId != ActionId.CharacterMultiplyReceivedDamage &&
            Effect.ActionId != ActionId.FightCasterSplashHeal)
        {
            return false;
        }

        if (ActionIdHelper.IsDamage(runningEffect.GetSpellEffect().Category, actionId) && HasDamageTrigger() &&
            !ActionIdHelper.CanTriggerOnDamage(actionId))
        {
            return false;
        }

        if (IsTriggeredByParent(runningEffect) && !runningEffect.GetSpell().CanAlwaysTriggerSpells)
        {
            return false;
        }

        return true;
    }

    private bool HasTriggerState(int stateId, bool gain)
    {
        foreach (var trigger in Effect.Triggers)
        {
            if (gain)
            {
                if (trigger.StartsWith("EON", StringComparison.Ordinal) && int.Parse(trigger["EON".Length..]) == stateId)
                {
                    return true;
                }
            }
            else
            {
                if (trigger.StartsWith("EOFF", StringComparison.Ordinal) && int.Parse(trigger["EOFF".Length..]) == stateId)
                {
                    return true;
                }
            }
        }

        return false;
    }


    public int GetTriggerCount()
    {
        return TriggerCount;
    }


    public HaxeBuff Clone()
    {
        return new HaxeBuff(Caster, Target, Spell, Effect.Clone(), RunningEffect.Copy());
    }
}
