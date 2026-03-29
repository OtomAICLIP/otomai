using Bubble.Core.Datacenter.Datacenter.Effects;
using Bubble.DamageCalculation.Customs;
using Bubble.DamageCalculation.FighterManagement;
using Bubble.DamageCalculation.SpellManagement;
using Bubble.DamageCalculation.Tools;

namespace Bubble.DamageCalculation.DamageManagement;

/// <summary>
/// This class define all the possible output of an effect.
/// </summary>
public class EffectOutput
{
    public bool Unknown { get; set; }
    public long ThrowedBy { get; set; }
    public bool ThroughPortal { get; set; } 
    public SummonInfos? Summon { get; set; }
    public int StatValue { get; set; }
    public int StatId { get; set; } = -1;
    public Interval? Shield { get; set; }
    public int RangeLoss { get; set; }
    public int RangeGain { get; set; }
    public int NewStateId { get; set; } = -1;
    public bool AreadyHadStateId { get; set; }
    public MovementInfos? Movement { get; set; }
    public int LostStateId { get; set; } = -1;
    public bool IsSummoning { get; set; }
    public bool IsPushed { get; set; }
    public bool IsPulled { get; set; }
    public long FighterId { get; set; }
    public bool Dispell { get; set; }
    public bool Death { get; set; }
    public DamageRange? DamageRange { get; set; }
    public long CarriedBy { get; set; }
    public bool AttemptedApTheft { get; set; }
    public bool AttemptedAmTheft { get; set; }
    public bool AreMaxLifePointsAffected { get; set; }
    public bool AreLifePointsAffected { get; set; }
    public bool AreErodedLifePointsAffected { get; set; }
    public int ApStolen { get; set; }
    public int AmStolen { get; set; }
    public int Dodged { get; set; }
    public int ApLost { get; set; }
    public int AmLost { get; set; }
    public ActionId ActionId { get; set; }
    public HaxeBuff? BuffAdded { get; set; }
    public HaxeBuff? BuffRemoved { get; set; }
    public HaxeBuff? BuffUpdated { get; set; }
    public HaxeBuff? BuffTriggered { get; set; }
    public int ReduceBuffDuration { get; set; } = 0;
    public bool IsLifeAffected => AreLifePointsAffected || AreMaxLifePointsAffected || AreErodedLifePointsAffected;
    public int SpellIdModified { get; set; } = -1;
    public int SpellModificationId { get; set; } = -1;
    public int SpellModificationValue { get; set; } = -1;
    public SpellExecutionInfos? SpellExecutionInfos { get; set; }
    public int SpellExecutionId { get; set; } = -1;
    public int SpellExecutionLevel { get; set; } = -1;
    public int MarkTriggered { get; set; } = -1;
    public int ApGain { get; set; }
    public int AmGain { get; set; }
    public long? ControlledBy { get; set; }
    public bool? NoMoreControlled { get; set; }
    public bool CanBeControlled { get; set; }
    public int CooldownValue { get; set; }
    public int CooldownSpellId { get; set; } = -1;
    public int InvisibilityDetectedAtCell { get; set; } = -1;
    public bool? InvisibilityState { get; set; }
    public bool IsKill { get; set; }
    public long SourceId { get; set; }
    public bool PassCurrentTurn { get; set; }
    public bool LookUpdate { get; set; }
    public Mark? MarkAdded { get; set; }

    public EffectOutput(long fighterId, long sourceId, ActionId actionId)
    {
        FighterId = fighterId;
        SourceId  = sourceId;
        ActionId  = actionId;
    }

    public static EffectOutput FromMovement(long fighterId,
        long sourceId, 
        ActionId actionId, 
        int position, 
        int direction,
        HaxeFighter? swappedWith = null,
        bool fromPandawa = false,
        bool invalid = false)
    {
        return new EffectOutput(fighterId, sourceId, actionId)
        {
            Movement = new MovementInfos(position, direction, swappedWith, fromPandawa, invalid),
        };
    }

    public static EffectOutput FromDamageRange(long fighterId, long sourceId, ActionId actionId, DamageRange damageRange)
    {
        return new EffectOutput(fighterId, sourceId, actionId)
        {
            DamageRange = damageRange,
        };
    }

    public static EffectOutput FromApLost(long fighterId, long sourceId, ActionId actionId, int apStolen)
    {
        return new EffectOutput(fighterId, sourceId, actionId)
        {
            ApLost = apStolen,
        };
    }

    public static EffectOutput FromAmLost(long fighterId, long sourceId, ActionId actionId, int amStolen)
    {
        return new EffectOutput(fighterId, sourceId, actionId)
        {
            AmLost = amStolen,
        };
    }

    public static EffectOutput FromApTheft(long fighterId, long sourceId, ActionId actionId, int apStolen, int dodged)
    {
        return new EffectOutput(fighterId, sourceId, actionId)
        {
            AttemptedApTheft = true,
            ApStolen         = apStolen,
            Dodged           = dodged,
        };
    }

    public static EffectOutput FromAmTheft(long fighterId, long sourceId, ActionId actionId, int amStolen, int dodged)
    {
        return new EffectOutput(fighterId, sourceId, actionId)
        {
            AttemptedAmTheft = true,
            AmStolen         = amStolen,
            Dodged           = dodged,
        };
    }

    public static EffectOutput FromStateChange(long fighterId, long sourceId, ActionId actionId, int stateId, bool add, bool alreadyHad = false)
    {
        if (add)
        {
            return new EffectOutput(fighterId, sourceId, actionId)
            {
                NewStateId = stateId,
                AreadyHadStateId = alreadyHad,
            };
        }

        return new EffectOutput(fighterId, sourceId, actionId)
        {
            LostStateId = stateId,
        };
    }

    public static EffectOutput FromStatUpdate(long fighterId, long sourceId, ActionId actionId, int statId, int statValue)
    {
        return new EffectOutput(fighterId, sourceId, actionId)
        {
            StatId    = statId,
            StatValue = statValue,
        };
    }


    public static EffectOutput DeathOf(long fighterId, long sourceId, ActionId actionId, bool isKill)
    {
        return new EffectOutput(fighterId, sourceId, actionId)
        {
            Death  = true,
            IsKill = isKill,
        };
    }


    public static EffectOutput FromDispell(long fighterId, long sourceId, ActionId actionId)
    {
        return new EffectOutput(fighterId, sourceId, actionId)
        {
            Dispell = true,
        };
    }

    public static EffectOutput FromSummon(long fighterId, long sourceId, ActionId actionId, int position, int direction, int lookId = 0)
    {
        return new EffectOutput(fighterId, sourceId, actionId)
        {
            Summon = new SummonInfos(position, direction, lookId),
        };
    }

    public static EffectOutput FromSummoning(long fighterId, long sourceId, ActionId actionId, bool canBeControlled)
    {
        return new EffectOutput(fighterId, sourceId, actionId)
        {
            IsSummoning     = true,
            CanBeControlled = canBeControlled,
        };
    }


    public static EffectOutput FromAffectedLifePoints(long fighterId, long sourceId, ActionId actionId)
    {
        return new EffectOutput(fighterId, sourceId, actionId)
        {
            AreLifePointsAffected = true,
        };
    }

    public static EffectOutput FromAffectedMaxLifePoints(long fighterId, long sourceId, ActionId actionId)
    {
        return new EffectOutput(fighterId, sourceId, actionId)
        {
            AreMaxLifePointsAffected = true,
        };
    }

    public static EffectOutput FromBuffAdded(long fighterId, long sourceId, ActionId actionId, HaxeBuff buff)
    {
        return new EffectOutput(fighterId, sourceId, actionId)
        {
            BuffAdded = buff,
        };
    }

    public static EffectOutput FromBuffRemoved(long fighterId, long sourceId, ActionId actionId, HaxeBuff buff)
    {
        return new EffectOutput(fighterId, sourceId, actionId)
        {
            BuffRemoved = buff,
        };
    }

    public static EffectOutput FromBuffUpdated(long fighterId, long sourceId, ActionId actionId, HaxeBuff buff)
    {
        return new EffectOutput(fighterId, sourceId, actionId)
        {
            BuffUpdated = buff,
        };
    }

    public static EffectOutput FromBuffDurationUpdated(long fighterId, long sourceId, ActionId actionId, int delta)
    {
        return new EffectOutput(fighterId, sourceId, actionId)
        {
            ReduceBuffDuration = delta,
        };
    }

    public static EffectOutput FromAffectedErodedLifePoints(long fighterId, long sourceId, ActionId actionId)
    {
        return new EffectOutput(fighterId, sourceId, actionId)
        {
            AreErodedLifePointsAffected = true,
        };
    }

    public DamageRange ComputeShieldDamage()
    {
        if (DamageRange == null || Shield == null)
        {
            return DamageRange.Zero;
        }

        var damageRange = (DamageRange)DamageRange.Copy();
        damageRange.MinimizeByInterval(damageRange);
        damageRange.IsShieldDamage = true;
        return damageRange;
    }

    public DamageRange ComputeLifeDamage()
    {
        if (DamageRange == null)
        {
            return DamageRange.Zero;
        }

        var damageRange = (DamageRange)DamageRange.Copy();

        if (Shield != null)
        {
            damageRange.SubInterval(Shield);
            damageRange.MinimizeBy(0);
        }

        return damageRange;
    }

    public static EffectOutput FromSpellModificationUpdate(long id, long sourceId, ActionId actionId, int spellId, int spellModificationId, int value)
    {
        return new EffectOutput(id, sourceId, actionId)
        {
            SpellIdModified        = spellId,
            SpellModificationId    = spellModificationId,
            SpellModificationValue = value,
        };
    }

    public static EffectOutput FromSpellExecution(long casterId, long sourceId, ActionId actionId, SpellExecutionInfos executionResult)
    {
        return new EffectOutput(casterId, sourceId, actionId)
        {
            SpellExecutionInfos = executionResult,
        };
    }

    public static EffectOutput FromSpellExecutionEnd(long casterId, long sourceId, ActionId actionId, int spellId, short level)
    {
        return new EffectOutput(casterId, sourceId, actionId)
        {
            SpellExecutionId    = spellId,
            SpellExecutionLevel = level,
        };
    }

    public static EffectOutput FromMarkTrigger(long fighterId, long sourceId, ActionId actionId, int markId)
    {
        return new EffectOutput(fighterId, sourceId, actionId)
        {
            MarkTriggered = markId,
        };
    }
    
    public static EffectOutput FromApGain(long fighterId, long sourceId, ActionId actionId, int apGain)
    {
        return new EffectOutput(fighterId, sourceId, actionId)
        {
            ApGain = apGain,
        };
    }

    public static EffectOutput FromAmGain(long fighterId, long sourceId, ActionId actionId, int amGain)
    {
        return new EffectOutput(fighterId, sourceId, actionId)
        {
            AmGain = amGain,
        };
    }


    public static EffectOutput FromControlEntity(long fighterId, long sourceId, ActionId actionId, long casterId)
    {
        return new EffectOutput(fighterId, sourceId, actionId)
        {
            ControlledBy = casterId,
        };
    }


    public static EffectOutput FromNoControlEntity(long fighterId, long sourceId, ActionId actionId)
    {
        return new EffectOutput(fighterId, sourceId, actionId)
        {
            NoMoreControlled = true,
        };
    }

    public static EffectOutput FromCooldown(long id, long sourceId, ActionId actionId, int spellId, int cooldown)
    {
        return new EffectOutput(id, sourceId, actionId)
        {
            ActionId        = actionId,
            CooldownSpellId = spellId,
            CooldownValue   = cooldown,
        };
    }
    
    public static EffectOutput FromInvisibilityDetectedAtCell(long fighterId, long sourceId, ActionId actionId, int cellId)
    {
        return new EffectOutput(fighterId, sourceId, actionId)
        {
            InvisibilityDetectedAtCell = cellId,
        };
    }

    public static EffectOutput FromInvisiblityStateChanged(long fighterId, long sourceId, ActionId actionId, bool newState)
    {
        return new EffectOutput(fighterId, sourceId, actionId)
        {
            InvisibilityState = newState,
        };
    }

    public static EffectOutput FromPassCurrentTurn(long fighterId, long sourceId, ActionId actionId)
    {
        return new EffectOutput(fighterId, sourceId, actionId)
        {
            PassCurrentTurn = true,
        };
    }

    public static EffectOutput FromLookUpdate(long fighterId, long casterId, ActionId action)
    {
        return new EffectOutput(fighterId, casterId, action)
        {
            LookUpdate = true,
        };
    }

    public static EffectOutput FromBuffTriggered(long fighterId, long sourceId, ActionId actionId, HaxeBuff buff)
    {
        return new EffectOutput(fighterId, sourceId, actionId)
        {
            BuffTriggered = buff,
        };
    }

    public static EffectOutput FromMarkAdded(long fighterId, long sourceId, ActionId actionId, Mark mark)
    {
        return new EffectOutput(fighterId, sourceId, actionId)
        {
            MarkAdded = mark,
        };
    }

}