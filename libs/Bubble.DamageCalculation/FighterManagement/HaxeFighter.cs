using Bubble.Core.Datacenter.Datacenter.Effects;
using Bubble.DamageCalculation.Customs;
using Bubble.DamageCalculation.DamageManagement;
using Bubble.DamageCalculation.FighterManagement.FighterStats;
using Bubble.DamageCalculation.SpellManagement;
using Bubble.DamageCalculation.Tools;
using Enumerable = System.Linq.Enumerable;

namespace Bubble.DamageCalculation.FighterManagement;

public class HaxeFighter
{
    private const int MaxResistHuman = 50;
    private const int MaxResistMonster = 100;
    
    public static readonly int[] BombBreedId = { 3112, 3113, 3114, 5161, };
    public static readonly int[] SteamerTurretBreedId = { 3287, 3288, 3289, 5143, 5141, 5142, };
    
    private int _currentPosition = -1;
    private int _pendingPreviousPosition = -1;
    public int BeforeLastSpellPosition { get; set; } = -1;
    public int BeforeMarkPosition { get; set; } = -1;
    
    private HaxeFighter? _carriedFighter;
    private HaxeLinkedList<HaxeBuff> _pendingDispelledBuffs;
    public HaxeLinkedList<HaxeBuff> Buffs { get; private set; }
    
    // Only used for bombs combos
    public List<HaxeBuff> BuffsToReapply { get; private set; } = new();
    
    private HaxeLinkedListNode<HaxeBuff>? _pendingBuffHead;
    
    
    public HaxeLinkedList<EffectOutput>? TotalEffects { get; set; }
    public DamageRange? LastTheoreticalDamageRange { get; private set; }
    public DamageRange? LastRawDamageTaken { get; set; }
    public DamageRange? LastTheoreticalRawDamageTaken { get; set; }
    public HaxeLinkedList<EffectOutput> PendingEffects { get; private set; } = new();

    public bool IsStaticElement { get; private set; }
    public int TeamId { get; }
    public PlayerType PlayerType { get; }
    public int Level { get; }
    public int Breed { get; }
    public long Id { get; }
    public IFighterData Data { get; }
    public short Grade { get; set; }

    private HaxeFighterSave? _save;

    public bool IsDead { get; set; }
    public bool IsSimulation { get; set; }


    public HaxeFighter(long id, int level, int breed, PlayerType playerType, int teamId, bool isStaticElement,
                       IList<HaxeBuff> buffs, IFighterData data, bool isSummonCastPreviewed = false)
    {
        Id                 = id;
        Level              = level;
        Breed              = breed;
        PlayerType         = playerType;
        IsStaticElement    = isStaticElement;
        Data               = data;
        TeamId             = teamId;

        Buffs                  = new HaxeLinkedList<HaxeBuff>();
        _pendingDispelledBuffs = new HaxeLinkedList<HaxeBuff>();

        TotalEffects               = new HaxeLinkedList<EffectOutput>();
        LastRawDamageTaken         = null;
        LastTheoreticalDamageRange = null;
        PendingEffects             = new HaxeLinkedList<EffectOutput>();

        foreach (var buff in buffs)
        {
            Buffs.Add(buff.Clone());
        }
    }

    /// <summary>
    /// Determines if a fighter was teleported to an invalid cell during the current turn.
    /// </summary>
    /// <param name="fightContext">The FightContext instance representing the current fight context.</param>
    /// <returns>True if a fighter was teleported to an invalid cell, otherwise false.</returns>
    public bool WasTeleportedInInvalidCellThisTurn(FightContext fightContext)
    {
        foreach (var effectOutput in PendingEffects)
        {
            if (effectOutput.Movement != null && 
                !fightContext.Map.IsCellWalkable(effectOutput.Movement.NewPosition))
            {
                return true;
            }
            
            if (effectOutput.Movement != null && 
                effectOutput.Movement.WasInvalid)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Determines if a fighter was telefragged during the current turn.
    /// </summary>
    /// <returns>True if a fighter was telefragged, otherwise false.</returns>
    public bool WasTelefraggedThisTurn()
    {
        foreach (var effectOutput in PendingEffects)
        {
            if (effectOutput.Movement != null && effectOutput.Movement.SwappedWith != null)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Updates a stat with a percentage value.
    /// </summary>
    /// <param name="stat">The stat to be updated.</param>
    /// <param name="percent">The percentage value to apply.</param>
    /// <param name="increase">If true, the stat will be increased by the percentage value. If false, it will be decreased.</param>
    /// <returns>The updated stat value.</returns>
    public int UpdateStatWithPercentValue(HaxeStat stat, int percent, bool increase)
    {
        var sign         = increase ? 1 : -1;
        var currentValue = stat.Total;
        var delta        = (int)Math.Floor((double)sign * percent);
        return (int)Math.Floor((double)currentValue + delta);
    }

    /// <summary>
    /// Updates a stat with a flat value.
    /// </summary>
    /// <param name="stat">The stat to be updated.</param>
    /// <param name="value">The flat value to apply.</param>
    /// <param name="increase">If true, the stat will be increased by the flat value. If false, it will be decreased.</param>
    /// <returns>The updated stat value.</returns>
    public int UpdateStatFromFlatValue(HaxeStat stat, int value, bool increase)
    {
        var sign         = increase ? 1 : -1;
        var isLinearBuff = ActionIdHelper.IsLinearBuffActionIds(stat.Id);
        var currentValue = stat.Total;
        int newValue;

        if (isLinearBuff)
        {
            var delta = value * sign;
            newValue = currentValue + delta;
        }
        else
        {
            var percentChange = (int)Math.Floor(100 * (1 + sign * value * 0.01)) - 100;
            if (currentValue == 0)
            {
                newValue = percentChange;
            }
            else
            {
                newValue = (int)Math.Floor(currentValue * (1 + percentChange * 0.01));
            }
        }

        return newValue;
    }

    /// <summary>
    /// Updates a stat from a buff.
    /// </summary>
    /// <param name="buff">The buff to apply.</param>
    /// <param name="apply">If true, the buff will be applied. If false, it will be removed.</param>
    /// <returns>An EffectOutput instance containing the updated stat information or null if the effect is not instantaneous or the stat is not found.</returns>
    public EffectOutput? UpdateStatFromBuff(HaxeBuff buff, bool apply)
    {
        var spellEffect  = buff.Effect;
        var effectOutput = UpdateStat(buff.DisplayActionId, apply, spellEffect, buff.CasterId);
        return effectOutput;
    }

    public EffectOutput? UpdateStat(ActionId actionId, bool apply, HaxeSpellEffect spellEffect, long buffCasterId)
    {
        if (!SpellManager.IsInstantaneousSpellEffect(spellEffect))
        {
            return null;
        }

        var statId = ActionIdHelper.GetStatIdFromStatActionId(spellEffect.ActionId);

        if (statId == -1)
        {
            return null;
        }

        var stat = Data.GetStat(statId);

        if (stat == null)
        {
            return null;
        }

        UpdateStatFromEffect(stat, spellEffect, apply);

        if (ActionIdHelper.IsShield(spellEffect.ActionId))
        {
            stat.Context = Math.Max(0, stat.Context);
        }

        var effectOutput = EffectOutput.FromStatUpdate(Id, buffCasterId, actionId, (int)stat.Characteristic, (int)stat.Context);
        return effectOutput;
    }

    private void UpdateStatFromEffect(IStatsField field, HaxeSpellEffect effect, bool positive)
    {
        if (ActionIdHelper.IsFlatStatBoostActionId(effect.ActionId) ||
            ActionIdHelper.IsPercentStatBoostActionId(effect.ActionId))
        {
            UpdateStatWithValue(field, effect.GetMinRoll(), positive);
        }
    }

    private void UpdateStatWithValue(IStatsField field, int value, bool positive)
    {
        var isPositive = positive ? 1 : -1;
        var realValue  = Math.Floor((double)value * isPositive);

        field.Context += (int)realValue;
    }

    public EffectOutput? UpdateSpellModificationFromBuff(HaxeBuff buff, int spellModificationId, int value = -999999)
    {
        var spellEffect = buff.Effect;
        value = value == -999999 ? spellEffect.Param3 : value;

        if (buff.RunningEffect.Context == null || !buff.RunningEffect.Context.IsSimulation)
        {
            Data.AddSpellModification(spellEffect.Param1, spellModificationId, (short)value, buff.Effect.ActionId);
        }

        return EffectOutput.FromSpellModificationUpdate(Id, buff.CasterId, buff.Effect.ActionId, spellEffect.Param1, spellModificationId, value);
    }
    

    /// <summary>
    /// Checks if the fighter is under the effect of the maximize roll buff.
    /// </summary>
    /// <returns>True if the fighter is under the effect of the maximize roll buff, otherwise false.</returns>
    public bool UnderMaximizeRollEffect()
    {
        return Buffs.Any(currentBuff => currentBuff.IsApplied && currentBuff.Effect.ActionId == ActionId.CharacterMaximizeRoll);
    }
    
    /// <summary>
    /// Stores a pending buff, ensures the maximum stack of effects is not exceeded, and returns the corresponding effect output.
    /// </summary>
    /// <param name="haxeBuff">The HaxeBuff object representing the buff to be stored.</param>
    /// <returns>An EffectOutput object representing the stored buff effect.</returns>
    public EffectOutput? StorePendingBuff(HaxeBuff haxeBuff)
    {
        var existingBuffsCount       = 0;
        var pendingBuffsCount        = 0;
        var isPendingBuffHeadReached = false;
        
        //if (haxeBuff.Spell.MaxEffectsStack == -1)
        if (haxeBuff.Spell.MaxEffectsStack <= 0)
        {
            return AddPendingBuff(haxeBuff);
        }

        bool BuffComparer(HaxeBuff buff1, HaxeBuff buff2)
        {      
            if (buff1.Effect.Delay == 0 && buff2.Effect.Delay > 0)
            {
                return false;
            }

            if (buff1.Effect.Id == buff2.Effect.Id)
            {
                if(buff1.Effect.Triggers.Length != buff2.Effect.Triggers.Length)
                {
                    return false;
                }
                
                if(buff1.Effect.Triggers.Length > 0 && buff2.Effect.Triggers.Length > 0 && buff1.Effect.Triggers[0] != buff2.Effect.Triggers[0])
                {
                    return false;
                }
                
                return true;
            }

            if (buff1.Spell.Id != buff2.Spell.Id || buff1.Effect.ActionId != buff2.Effect.ActionId)
            {
                return false;
            }

            // This is homemade, but it seems logic that two effects that are different but in the same spell level should coexist
            /*if (buff1.Spell.Id == buff2.Spell.Id && buff1.Effect.Level == buff2.Effect.Level
                                                 && buff1.Effect.Order != buff2.Effect.Order)
            {
                return false;
            }*/
            
            if(buff1.Effect.Triggers.Length != buff2.Effect.Triggers.Length)
            {
                return false;
            }
                
            if(buff1.Effect.Triggers.Length > 0 && buff2.Effect.Triggers.Length > 0 && buff1.Effect.Triggers[0] != buff2.Effect.Triggers[0])
            {
                return false;
            }

            if(!(buff1.Effect.Order == buff2.Effect.Order && buff1.Effect.Level != buff2.Effect.Level))
            {
                return buff1.Effect.IsCritical != buff2.Effect.IsCritical;
            }

            return true;

            
            if (buff1.Effect.Delay == 0 && buff2.Effect.Delay > 0)
            {
                return false;
            }

            if (buff1.Effect.Id == buff2.Effect.Id)
            {
                return true;
            }

            if (buff1.Spell.Id != buff2.Spell.Id || buff1.Effect.ActionId != buff2.Effect.ActionId)
            {
                return false;
            }

            /*if (buff1.Effect.ActionId == ActionId.FightSetState)
            {
                if(buff1.Effect.Param3 != buff2.Effect.Param3)
                {
                    return false;
                }
            }*/

            // if (buff1.Effect.Order == buff2.Effect.Order && buff1.Effect.Level != buff2.Effect.Level)
            if (buff1.Effect.Order != buff2.Effect.Order && buff1.Effect.Level == buff2.Effect.Level)
            {
                return buff1.Effect.IsCritical != buff2.Effect.IsCritical;
            }

            return true;

        }

        foreach (var buff in Buffs)
        {
            if (buff == _pendingBuffHead?.Item)
            {
                isPendingBuffHeadReached = true;
            }

            if (!BuffComparer(haxeBuff, buff))
            {
                continue;
            }

            if (!isPendingBuffHeadReached)
            {
                existingBuffsCount++;
            }
            else
            {
                pendingBuffsCount++;
            }
        }

        if (existingBuffsCount + pendingBuffsCount < haxeBuff.Spell.MaxEffectsStack)
        {
            return AddPendingBuff(haxeBuff);
        }

        isPendingBuffHeadReached = false;
        var node = Buffs.Head;

        while (node != null)
        {
            var buff = node;
            node = node.Next;

            if (buff.Item == _pendingBuffHead?.Item)
            {
                isPendingBuffHeadReached = true;
            }

            if (!BuffComparer(buff.Item, haxeBuff))
            {
                continue;
            }

            if (!isPendingBuffHeadReached)
            {
                _pendingDispelledBuffs.Add(buff.Item);
            }
            
            var eff = SafeRemoveBuff(buff);
            if (eff != null)
            {
                PendingEffects.Add(eff);
            }
            if (!BuffComparer(buff.Item, haxeBuff))
            {
                continue;
            }

            PendingEffects.Add(EffectOutput.FromBuffRemoved(Id, buff.Item.CasterId, buff.Item.Effect.ActionId, buff.Item));
            pendingBuffsCount--;
            
            if (pendingBuffsCount < haxeBuff.Spell.MaxEffectsStack)
            {
                break;
            }
        }

        return AddPendingBuff(haxeBuff);
    }

    public void SetBeforeLastSpellPosition(int cellId)
    {
        BeforeLastSpellPosition = cellId;
    }

    public void SavePositionBeforeSpellExecution()
    {
        SetBeforeLastSpellPosition(GetCurrentPositionCell());
    }
    
    public void SavePositionBeforeMarkExecution()
    {
        BeforeMarkPosition = GetCurrentPositionCell();
    }

    public void SavePendingEffects()
    {
        if (TotalEffects != null && PendingEffects != null)
        {
            TotalEffects = TotalEffects.Concat(PendingEffects);
        }
        else
        {
            TotalEffects = PendingEffects;
        }

        PendingEffects = new HaxeLinkedList<EffectOutput>();
    }

    public HaxeFighterSave Save()
    {
        _save = new HaxeFighterSave
        {
            Id                      = Id,
            Outputs                 = PendingEffects.Select(x => x).ToArray(),
            Buffs                   = Buffs.Copy(),
            Cell                    = GetCurrentPositionCell(),
            PendingPreviousPosition = _pendingPreviousPosition,
        };

        return _save;
    }

    public bool Load(HaxeFighterSave? save = null)
    {
        if (save == null)
        {
            if (_save != null)
            {
                return Load(_save);
            }

            return false;
        }

        if (Id == save.Id)
        {
            PendingEffects = ConvertToLinkedList(save.Outputs);
            Buffs          = save.Buffs.Copy();
            SetCurrentPositionCell(save.Cell);
            _pendingPreviousPosition = save.PendingPreviousPosition;
            return true;
        }

        return false;
    }

    private EffectOutput? SafeRemoveBuff(HaxeLinkedListNode<HaxeBuff> buff)
    {
        Buffs.Remove(buff);

        if (buff.Item.IsApplied)
        {
            if (DisableBuff(buff, out var safeRemoveBuff))
            {
                return safeRemoveBuff;
            }
        }

        if (buff == _pendingBuffHead)
        {
            _pendingBuffHead = buff.Next;
        }

        return null;
    }


    public void ResetToInitialState()
    {
        LastRawDamageTaken = null;

        SetCurrentPositionCell(-1);
        SetBeforeLastSpellPosition(-1);
        _pendingPreviousPosition = -1;

        FlushPendingBuffs();

        PendingEffects = new HaxeLinkedList<EffectOutput>();

        foreach (var buff in Buffs)
        {
            buff.TriggerCount = buff.StartTriggerCount;
        }
    }

    public List<EffectOutput> RemoveState(int stateId)
    {
        var effectOutputs = new List<EffectOutput>();

        var buff    = Buffs.Head;
        var removed = false;

        EffectOutput? effectOutput = null;

        while (buff != null)
        {
            var current = buff;
            buff = buff.Next;

            if (current.Item.Effect.ActionId != ActionId.FightSetState || current.Item.Effect.GetMinRoll() != stateId)
            {
                continue;
            }

            if (current != _pendingBuffHead)
            {
                _pendingDispelledBuffs.Add(current.Item);
            }

            var output = SafeRemoveBuff(current);

            if (output != null)
            {
                effectOutput = output;
                PendingEffects.Add(output);
            }

            var removedBuff = current.Item;
            
            if (effectOutput != null)
            {
                effectOutputs.Add(effectOutput);
            }

            if (!removed)
            {
                effectOutputs.Add(EffectOutput.FromStateChange(Id, Id, ActionId.FightUnsetState, removedBuff.Effect.GetMinRoll(), false));
            }

            effectOutputs.Add(EffectOutput.FromBuffRemoved(Id, Id, ActionId.FightUnsetState, removedBuff));
            removed = true;
        }

        return effectOutputs;
    }

    public List<EffectOutput> RemoveBuffBySpellId(HaxeFighter caster, int spellId, int level = -1)
    {
        var buff          = Buffs.Head;
        var effectOutputs = new List<EffectOutput>();

        while (buff != null)
        {
            var current = buff;
            buff = buff.Next;
            
            // apply only for zobal for now
            if(current.Item.FirstParentEffect != null && current.Item.FirstParentEffect.Spell.Id == spellId && current.Item.Caster.Breed == 14)
            {         
                if (level > 1 && current.Item.Spell.Level != level)
                {
                    continue;
                }
                
                effectOutputs.AddRange(RemoveBuff(caster, current));
                continue;
            }

            if (current.Item.Spell.Id != spellId)
            {
                continue;
            }

            if (level > 1 && current.Item.Spell.Level != level)
            {
                continue;
            }

            if (current != _pendingBuffHead)
            {
                _pendingDispelledBuffs.Add(current.Item);
            }

            effectOutputs.AddRange(RemoveBuff(caster, current));
        }

        return effectOutputs;
    }

    public HaxeLinkedListNode<HaxeBuff>? GetBuffById(int buffId)
    {
        var buff = Buffs.Head;

        while (buff != null)
        {
            var current = buff;
            buff = buff.Next;

            if (current.Item.Id != buffId)
            {
                continue;
            }

            if (current != _pendingBuffHead)
            {
                _pendingDispelledBuffs.Add(current.Item);
            }

            return current;
        }

        return null;
    }
    
    public void RemoveBuffByActionId(ActionId actionId)
    {
        var buff = Buffs.Head;

        while (buff != null)
        {
            var current = buff;
            buff = buff.Next;

            if (current.Item.Effect.ActionId != actionId)
            {
                continue;
            }

            if (current != _pendingBuffHead)
            {
                _pendingDispelledBuffs.Add(current.Item);
            }

            foreach (var item in RemoveBuff(this, current))
            {
                PendingEffects.Add(item);
            }
        }
    }

    public List<EffectOutput> ReduceBuffDurations(HaxeFighter caster, int duration)
    {
        var num           = 0;
        var effectOutputs = new List<EffectOutput>();
        
        var buff         = Buffs.Head;
        while (buff != null)
        {
            var current = buff;
            buff = buff.Next;
            
            if (current.Item.Effect.Delay > 0)
            {
                continue;
            }
            
            if (current.Item.Effect.Duration < 0)
            {
                continue;
            }

            if (current.Item.Effect.Duration > 0 && current.Item.Effect.Duration <= duration && current.Item.Effect.IsDispellable)
            {
                if (current != _pendingBuffHead)
                {
                    _pendingDispelledBuffs.Add(current.Item);
                }

                effectOutputs.AddRange(RemoveBuff(caster, current));
                num++;
            }
            else
            {
                current.Item.Effect.Duration -= duration;
            }
        }

        if (num > 0)
        {
            effectOutputs.Add(EffectOutput.FromDispell(Id, caster.Id, ActionId.CharacterShortenActiveEffectsDuration));
        }

        effectOutputs.Add(EffectOutput.FromBuffDurationUpdated(Id, caster.Id, ActionId.CharacterShortenActiveEffectsDuration, duration));
        return effectOutputs;
    }

    public IList<EffectOutput> RemoveBuff(HaxeFighter caster, HaxeLinkedListNode<HaxeBuff> current)
    {
        var effectOutputs = new List<EffectOutput>();
        
        
        var eff = SafeRemoveBuff(current);

        if (eff != null)
        {
            effectOutputs.Add(eff);
        }

        if (current.Item.IsState())
        {
            effectOutputs.Add(EffectOutput.FromStateChange(Id, caster.Id, ActionId.FightUnsetState, current.Item.Effect.GetMinRoll(), false));
        }

        effectOutputs.Add(EffectOutput.FromBuffRemoved(Id, caster.Id, ActionId.FightUnsetState, current.Item));

        return effectOutputs;
    }

    public bool IsUnlucky()
    {
        return Buffs.Any(buff => buff.IsApplied && buff.Effect.ActionId == ActionId.CharacterUnlucky);
    }

    public bool IsSwitchTeleport(ActionId actionId)
    {
        return actionId is ActionId.FightTeleswapMirror or
                           ActionId.FightTeleswapMirrorCaster or
                           ActionId.FightTeleswapMirrorImpactPoint or
                           ActionId.CharacterTeleportToFightStartPos or
                           ActionId.FightRollbackTurnBeginPosition or
                           ActionId.FightRollbackPreviousPosition or
                           ActionId.CharacterExchangePlaces;
    }

    public bool IsSteamerTurret()
    {
        if (PlayerType == PlayerType.Monster && Data.IsSummon())
        {
            return SteamerTurretBreedId.Contains(Breed);
        }

        return false;
    }

    public bool IsPacifist()
    {
        return HasStateEffect(6);
    }

    public bool IsLinkedBomb(HaxeFighter? fighter)
    {
        if (fighter is { PlayerType: PlayerType.Monster, } && Data.IsSummon() && BombBreedId.Contains(Breed))
        {
            if (fighter.Data.GetSummonerId() != Data.GetSummonerId())
            {
                return fighter.Data.GetSummonerId() == Id;
            }

            return true;
        }

        return false;
    }

    public bool IsInvulnerableWeapon()
    {
        return HasStateEffect(28);
    }

    public bool IsInvulnerableWater()
    {
        return HasStateEffect(23);
    }

    public bool IsInvulnerableTo(RunningEffect runningEffect, bool isMelee = false, int? elementId = null)
    {
        var spellEffect = runningEffect.SpellEffect;
        var caster      = runningEffect.Caster;

        if (IsInvulnerable() ||
            IsInvulnerableCritical() && spellEffect.IsCritical ||
            IsInvulnerableWeapon() && runningEffect.Spell.IsWeapon ||
            IsInvulnerableSummon() && caster.Data.IsSummon() ||
            IsInvulnerablePush() && spellEffect.ActionId == ActionId.CharacterLifePointsLostFromPush ||
            IsInvulnerableNeutral() && elementId == 0 ||
            IsInvulnerableEarth() && elementId == 1 ||
            IsInvulnerableWater() && elementId == 3 ||
            IsInvulnerableFire() && elementId == 2 ||
            IsInvulnerableAir() && elementId == 4 ||
            IsInvulnerableMelee() && isMelee ||
            IsInvulnerableRange() && !isMelee)
        {
            return true;
        }

        return false;
    }

    public bool IsInvulnerableSummon()
    {
        return HasStateEffect(31);
    }

    public bool IsInvulnerableRange()
    {
        return HasStateEffect(20);
    }

    public bool IsInvulnerablePush()
    {
        return HasStateEffect(26);
    }

    public bool IsInvulnerableNeutral()
    {
        return HasStateEffect(25);
    }

    public bool IsInvulnerableMelee()
    {
        return HasStateEffect(19);
    }

    public bool IsInvulnerableFire()
    {
        return HasStateEffect(21);
    }

    public bool IsInvulnerableEarth()
    {
        return HasStateEffect(24);
    }

    public bool IsInvulnerableCritical()
    {
        return HasStateEffect(27);
    }

    public bool IsInvulnerableAir()
    {
        return HasStateEffect(22);
    }

    public bool IsInvulnerable()
    {
        return HasStateEffect(7);
    }

    public bool IsInvisible()
    {
        return HasState((int)SpellStateId.Invisible);
    }

    public bool IsIncurable()
    {
        return HasStateEffect(5);
    }

    public bool IsCarrying()
    {
        return HasStateEffect(3);
    }

    public bool IsCarried()
    {
        return HasStateEffect(8);
    }

    public bool IsBomb()
    {
        if (PlayerType == PlayerType.Monster && Data.IsSummon())
        {
            return BombBreedId.Contains(Breed);
        }

        return false;
    }

    public bool IsAppearing()
    {
        return PendingEffects.Any(x => x.Summon != null);
    }

    public bool IsAlive(bool removePendingEffects = false)
    {
        if (IsDead)
        {
            return false;
        }

        var isDead = false;

        if (!removePendingEffects)
        {
            foreach (var effect in PendingEffects)
            {
                if (effect.Death)
                {
                    isDead = true;
                }
                else if (effect.Summon != null)
                {
                    isDead = false;
                }
            }
        }

        if (!isDead)
        {
            return Data.GetHealthPoints() > 0;
        }

        return false;
    }

    public bool HasStateEffect(int actionId)
    {
        var state = Buffs.Where(buff => buff.SpellState != null && buff.SpellState.StateEffects.Contains(actionId) &&
                                   SpellManager.IsInstantaneousSpellEffect(buff.Effect) && buff.Effect.Delay <= 0)
                    .FirstOrDefault(buff => HasState(buff.Effect.Param3));

        if (state == null)
        {
            return false;
        }

        var disabledState = Buffs.FirstOrDefault(buff => buff.Effect.ActionId == ActionId.FightDisableState
                                                         && buff.Effect.Param3 == state.Effect.Param3
                                                         && SpellManager.IsInstantaneousSpellEffect(buff.Effect) && buff.Effect.Delay <= 0);
        
        return disabledState == null;
    }
    
    public int GetMinimumHealthPoints()
    {
        var min = 0;

        foreach (var buff in Buffs)
        {
            if (buff.Effect.ActionId == ActionId.CharacterBoostThreshold && buff.Effect.Delay <= 0)
            {
                if(buff.Effect.Param1 > min)
                {
                    min = buff.Effect.Param1;
                }
            }
        }
        
        return min;
    }

    public bool HasState(int stateId)
    {
        var hasState      = false;
        var hasStateUnset = false;

        foreach (var buff in Buffs)
        {
            if (buff.Effect.Delay > 0)
            {
                continue;
            }
            
            if (buff.Effect.ActionId == ActionId.FightSetState && buff.Effect.GetMinRoll() == stateId && SpellManager.IsInstantaneousSpellEffect(buff.Effect))
            {
                hasState = true;
            }

            if (buff.Effect.ActionId == ActionId.FightDisableState && buff.Effect.GetMinRoll() == stateId && SpellManager.IsInstantaneousSpellEffect(buff.Effect))
            {
                hasStateUnset = true;
                break;
            }
        }

        return hasState && !hasStateUnset;
    }

    /// <summary>
    /// Gets the worst element based on main stat and flat damage bonus.
    /// The default is Strengh.
    /// </summary>
    /// <returns>The worst element.</returns>
    public int GetWorstElement()
    {
        int[] elements = { 0, 2, 3, 4, };

        var worstElement = 1;

        foreach (var currentElement in elements)
        {
            // if current main stat << main stat
            // OR
            // current main stat == main stat and current flat damage bonus < flat damage bonus
            if (GetElementMainStat(currentElement) < GetElementMainStat(worstElement) ||
                GetElementMainStat(currentElement) == GetElementMainStat(worstElement) &&
                GetElementFlatDamageBonus(currentElement) < GetElementFlatDamageBonus(worstElement))
            {
                worstElement = currentElement;
            }
        }

        return worstElement;
    }

    public HaxeFighter? GetSummoner(FightContext fightContext)
    {
        return fightContext.GetFighterById(Data.GetSummonerId());
    }

    public int GetDamageHealContextSpellMod(int spellId)
    {
        return Buffs.Where(buff => buff.IsApplied && buff.Effect.ActionId == ActionId.BoostSpellDmg && buff.Effect.Param1 == spellId)
                    .Sum(buff => buff.Effect.Param3);
    }

    public int GetSpellBaseDamageModification(int spellId)
    {
        return Buffs.Where(buff => buff.IsApplied && buff.Effect.ActionId == ActionId.BoostSpellBaseDmg && buff.Effect.Param1 == spellId)
                    .Sum(buff => buff.Effect.Param3);
    }

    public IList<List<HaxeFighter>> GetSharingDamageTargets(FightContext fightContext)
    {
        var results = new List<List<HaxeFighter>>();

        foreach (var buff in Buffs)
        {
            if (buff.Effect.ActionId != ActionId.CharacterShareDamages)
            {
                continue;
            }

            var fighters = fightContext.GetEveryFighter();

            var targets = fighters.Where(fighter => fighter.Id != Id && fighter.IsAlive())
                                  .Where(fighter => Enumerable.Any(fighter.Buffs,
                                      targetBuff =>
                                          targetBuff.Effect.ActionId == ActionId.CharacterShareDamages &&
                                          targetBuff.Effect.Delay <= 0 &&
                                          buff.Spell.Id == targetBuff.Spell.Id &&
                                          buff.CasterId == targetBuff.CasterId))
                                  .ToList();

            targets.Add(this);
            results.Add(targets);
        }

        return results;
    }

    public Interval GetPendingShield()
    {
        var shieldInterval = new Interval(Data.GetCharacteristicValue(StatId.Shield), Data.GetCharacteristicValue(StatId.Shield));
        var damages        = PendingEffects.Select(x => x.ComputeLifeDamage()).Concat(PendingEffects.Select(x => x.ComputeShieldDamage()));


        foreach (var damage in damages)
        {
            if (damage.IsHeal && damage.IsShieldDamage)
            {
                shieldInterval.AddInterval(damage);
            }
            else if (!damage.IsHeal && damage.IsShieldDamage)
            {
                shieldInterval.SubInterval(damage);
            }
        }

        shieldInterval.MinimizeBy(0);
        return shieldInterval;
    }

    public int GetPendingPreviousPosition()
    {
        var previousPosition = Data.GetPreviousPosition();
        if (previousPosition != -1)
        {
            return previousPosition;
        }

        return GetCurrentPositionCell();
    }

    public Interval GetPendingMissingLifePoints()
    {
        var damageInterval = new Interval(Data.GetMaxHealthPoints() - Data.GetHealthPoints(), Data.GetMaxHealthPoints() - Data.GetHealthPoints());

        foreach (var effect in PendingEffects)
        {
            if (effect.DamageRange == null)
            {
                continue;
            }

            if (effect.DamageRange.IsHeal && !effect.DamageRange.IsShieldDamage)
            {
                damageInterval.SubInterval(effect.DamageRange);
            }
            else if (!effect.DamageRange.IsHeal && !effect.DamageRange.IsShieldDamage && !effect.DamageRange.IsInvulnerable)
            {
                damageInterval.AddInterval(effect.DamageRange);
            }
        }

        damageInterval.MinimizeBy(0);
        damageInterval.MaximizeBy(Data.GetMaxHealthPoints());
        return damageInterval;
    }

    public Interval GetPendingMaxLifePoints()
    {
        var maxLifePoints   = Data.GetMaxHealthPoints();
        var maxLifeInterval = new Interval(maxLifePoints, maxLifePoints);

        foreach (var effect in PendingEffects)
        {
            if (effect.DamageRange == null)
            {
                continue;
            }

            if (!effect.DamageRange.IsHeal && !effect.DamageRange.IsShieldDamage && !effect.DamageRange.IsInvulnerable)
            {
                maxLifeInterval.SubInterval(DamageReceiver.GetPermanentDamage(effect.ComputeLifeDamage(), this));
            }
        }

        maxLifeInterval.MinimizeBy(0);
        maxLifeInterval.MaximizeBy(maxLifePoints);
        return maxLifeInterval;
    }

    public Interval GetPendingLifePoints()
    {
        var healthPointInterval = new Interval(Data.GetHealthPoints(), Data.GetHealthPoints());

        foreach (var effect in PendingEffects)
        {
            if (effect.DamageRange == null)
            {
                continue;
            }

            if (effect.DamageRange.IsHeal && !effect.DamageRange.IsShieldDamage)
            {
                healthPointInterval.AddInterval(effect.DamageRange);
            }
            else if (!effect.DamageRange.IsHeal && !effect.DamageRange.IsShieldDamage && !effect.DamageRange.IsInvulnerable)
            {
                healthPointInterval.SubInterval(effect.ComputeLifeDamage());
            }
        }

        healthPointInterval.MinimizeBy(0);
        healthPointInterval.MaximizeBy(Data.GetMaxHealthPoints());
        return healthPointInterval;
    }

    public int GetMaxLife()
    {
        return Data.GetMaxHealthPoints();
    }
    public int GetMaxLifeWithoutContext()
    {
        return Data.GetMaxHealthPointsWithoutContext();
    }
    public int GetLifePoint()
    {
        return Data.GetHealthPoints();
    }

    public int GetHealOnDamageRatio(RunningEffect runningEffect, bool isMelee)
    {
        foreach (var buff in Buffs)
        {
            if (buff.Effect.ActionId == ActionId.CharacterGiveLifeWithRatio &&
                (SpellManager.IsInstantaneousSpellEffect(buff.Effect) ||
                 buff.ShouldBeTriggeredOnTargetDamage(runningEffect, this, isMelee, false)))
            {
                return buff.Effect.Param1;
            }
        }

        return 0;
    }

    public int GetElementMainStat(int elementId)
    {
        return elementId switch
               {
                   0 or 1 => Data.GetCharacteristicValue(StatId.Strength),
                   2      => Data.GetCharacteristicValue(StatId.Intelligence),
                   3      => Data.GetCharacteristicValue(StatId.Chance),
                   4      => Data.GetCharacteristicValue(StatId.Agility),
                   5      => GetElementMainStat(GetBestElement()),
                   6      => GetElementMainStat(GetWorstElement()),
                   _      => 0,
               };
    }

    public int GetElementMainResist(int elementId)
    {
        var min = PlayerType == PlayerType.Human ? MaxResistHuman : MaxResistMonster;

        var value = elementId switch
                    {
                        0 => Data.GetCharacteristicValue(StatId.NeutralElementResistPercent),
                        1 => Data.GetCharacteristicValue(StatId.EarthElementResistPercent),
                        2 => Data.GetCharacteristicValue(StatId.FireElementResistPercent),
                        3 => Data.GetCharacteristicValue(StatId.WaterElementResistPercent),
                        4 => Data.GetCharacteristicValue(StatId.AirElementResistPercent),
                        _ => 0,
                    };

        value += Data.GetCharacteristicValue(StatId.ResistPercent);
        return Math.Min(value, min);
    }

    public int GetElementMainReduction(int elementId)
    {
        return elementId switch
               {
                   0 => Data.GetCharacteristicValue(StatId.NeutralElementReduction),
                   1 => Data.GetCharacteristicValue(StatId.EarthElementReduction),
                   2 => Data.GetCharacteristicValue(StatId.FireElementReduction),
                   3 => Data.GetCharacteristicValue(StatId.WaterElementReduction),
                   4 => Data.GetCharacteristicValue(StatId.AirElementReduction),
                   _ => 0,
               };
    }

    public int GetElementFlatDamageBonus(int elementId)
    {
        return elementId switch
               {
                   0 => Data.GetCharacteristicValue(StatId.NeutralDamageBonus),
                   1 => Data.GetCharacteristicValue(StatId.EarthDamageBonus),
                   2 => Data.GetCharacteristicValue(StatId.FireDamageBonus),
                   3 => Data.GetCharacteristicValue(StatId.WaterDamageBonus),
                   4 => Data.GetCharacteristicValue(StatId.AirDamageBonus),
                   6 => GetElementFlatDamageBonus(GetBestElement()),
                   7 => GetElementFlatDamageBonus(GetWorstElement()),
                   _ => 0,
               };
    }

    public HaxeLinkedList<EffectOutput>? GetEffectsDeltaFromSave()
    {
        if (_save != null)
        {
            if (_save.Outputs.Count == 0)
            {
                return PendingEffects;
            }

            var lastIndexPosition = -1;
            for (var i = 0; i < _save.Outputs.Count; i++)
            {
                lastIndexPosition = i;
            }

            return ConvertToLinkedList(PendingEffects.Skip(lastIndexPosition).ToList());
        }

        return null;
    }

    public DamageRange GetDynamicalDamageReflect()
    {
        var damageRange = new DamageRange(0, 0);

        foreach (var buff in Buffs)
        {
            if (!buff.IsActive)
            {
                continue;
            }

            if (buff.Effect.ActionId == ActionId.CharacterReflectorUnboosted)
            {
                damageRange.AddInterval(buff.Effect.GetDamageInterval());
            }
            else if (buff.Effect.ActionId == ActionId.CharacterLifeLostReflector)
            {
                var damage = buff.Effect.GetDamageInterval().Multiply(Level / 20d + 1);
                damage.AddInterval(damage);
            }
        }

        return damageRange;
    }

    public Interval GetDamageReductor(RunningEffect runningEffect, DamageRange damageRange, bool isMelee)
    {
        var damageReductor = new Interval(0, 0);

        if (!ActionIdHelper.CanTriggerDamageMultiplier(runningEffect.SpellEffect.ActionId))
        {
            return damageReductor;
        }

        foreach (var buff in Buffs)
        {
            if (buff.Effect.ActionId == ActionId.CharacterLifeLostCasterModerator && buff.Effect.Delay <= 0 &&
                (SpellManager.IsInstantaneousSpellEffect(buff.Effect) || buff.ShouldBeTriggeredOnTargetDamage(runningEffect, this, isMelee, damageRange.IsCollision)))
            {
                damageReductor.AddInterval(buff.Effect.GetDamageInterval().Multiply(Level / 20d + 1));
            }
        }

        return damageReductor;
    }

    public int GetDamageMultiplicator(RunningEffect runningEffect, bool isMelee, bool isCollision)
    {
        var damageMultiplicator = 100;

        if (!ActionIdHelper.CanTriggerDamageMultiplier(runningEffect.SpellEffect.ActionId))
        {
            return damageMultiplicator;
        }

        foreach (var buff in Buffs)
        {
            if (buff.Effect.ActionId == ActionId.CharacterMultiplyReceivedDamage && buff.Effect.Delay <= 0 &&
                (SpellManager.IsInstantaneousSpellEffect(buff.Effect) || buff.ShouldBeTriggeredOnTargetDamage(runningEffect, this, isMelee, isCollision)))
            {
                damageMultiplicator = (int)(damageMultiplicator * buff.Effect.Param1 * 0.01d);
            }
        }

        return damageMultiplicator;
    }

    public IList<DamageRange> GetDamageEffects()
    {
        return TotalEffects.Where(x => x.DamageRange != null)
                           .Select(x => x.DamageRange)
                           .ToList()!;
    }

    public int GetCurrentReceivedDamageMultiplierMelee(bool isMelee)
    {
        var value = Data.GetCharacteristicValue(isMelee
            ? StatId.ReceivedDamageMultiplierMelee
            : StatId.DealtDamageMultiplierSpells);
        return Math.Max(0, value);
    }

    public int GetCurrentReceivedDamageMultiplierCategory(bool isMelee)
    {
        var value = Data.GetCharacteristicValue(isMelee ? StatId.ReceivedDamageMultiplierWeapon : StatId.ReceivedDamageMultiplierSpells);
        return Math.Max(0, value);
    }

    /// <summary>
    /// Sets the current position cell of the fighter and updates the carried fighter's position if the fighter has the "carrying" state.
    /// </summary>
    /// <param name="cellId">The new cell id for the fighter's position.</param>
    public void SetCurrentPositionCell(int cellId)
    {
        _pendingPreviousPosition = GetCurrentPositionCell();
        _currentPosition         = cellId;
        if (HasState(3) && _carriedFighter != null)
        {
            _carriedFighter.SetCurrentPositionCell(cellId);
        }
    }

    public int GetCurrentDealtDamageMultiplierMelee(bool isMelee)
    {
        var value = Data.GetCharacteristicValue(isMelee
            ? StatId.DealtDamageMultiplierMelee
            : StatId.DealtDamageMultiplierDistance);
        return Math.Max(0, value);
    }

    public int GetCurrentDealtDamageMultiplierCategory(bool isMelee)
    {
        var value = Data.GetCharacteristicValue(isMelee
            ? StatId.DealtDamageMultiplierWeapon
            : StatId.DealtDamageMultiplierSpells);
        return Math.Max(0, value);
    }

    public HaxeFighter? GetCarrier(FightContext fightContext)
    {
        var currentPosition = GetCurrentPositionCell();
        foreach (var fighter in fightContext.Fighters)
        {
            if (fighter.GetCurrentPositionCell() == currentPosition && fighter.GetCarried(fightContext) == this)
            {
                return fighter;
            }
        }

        return null;
    }

    public HaxeFighter? GetCarried(FightContext fightContext)
    {
        if (_carriedFighter == null)
        {
            var carriedFighter = fightContext.GetCarriedFighterBy(this);
            if (carriedFighter != null && carriedFighter.HasState(8))
            {
                _carriedFighter = carriedFighter;
            }
        }

        return _carriedFighter;
    }

    public int GetCurrentPositionCell()
    {
        if (MapTools.IsValidCellId(_currentPosition))
        {
            return _currentPosition;
        }

        return Data.GetStartedPositionCell();
    }

    /// <summary>
    /// Gets the best element based on main stat and flat damage bonus.
    /// The default is Strengh.
    /// </summary>
    /// <returns>The worst element.</returns>
    public int GetBestElement()
    {
        int[] elements = { 0, 2, 3, 4, };

        var worstElement = 1;

        foreach (var currentElement in elements)
        {
            // if current main stat << main stat
            // OR
            // current main stat == main stat and current flat damage bonus < flat damage bonus
            if (GetElementMainStat(currentElement) > GetElementMainStat(worstElement) ||
                GetElementMainStat(currentElement) == GetElementMainStat(worstElement) &&
                GetElementFlatDamageBonus(currentElement) > GetElementFlatDamageBonus(worstElement))
            {
                worstElement = currentElement;
            }
        }

        return worstElement;
    }


    public int GetBeforeLastSpellPosition()
    {
        if (BeforeLastSpellPosition == -1)
        {
            return Data.GetStartedPositionCell();
        }

        return BeforeLastSpellPosition;
    }

    public IList<long> GetAllSacrificed()
    {
        return Buffs.Where(buff => buff.IsApplied && buff.Effect.ActionId == ActionId.CharacterSacrify)
                    .Select(buff => buff.CasterId)
                    .ToList();
    }

    public void FlushPendingBuffs()
    {
        if (_pendingBuffHead != null)
        {
            if (_pendingBuffHead.Previous != null)
            {
                _pendingBuffHead.Previous.Next = null;

                if (_pendingBuffHead == Buffs.Tail)
                {
                    Buffs.Tail = _pendingBuffHead.Previous;
                }
            }
            else if (_pendingBuffHead == Buffs.Head)
            {
                Buffs.Clear();
            }

            _pendingBuffHead = null;
        }

        if (_pendingDispelledBuffs.Head != null)
        {
            Buffs                  = _pendingDispelledBuffs.Append(Buffs);
            _pendingDispelledBuffs = new HaxeLinkedList<HaxeBuff>();
        }
    }


    
    public HaxeFighter Copy(FightContext fightContext)
    {
        var newId    = fightContext.GetFreeId();
        var newBuffs = Buffs.ToList();

        return new HaxeFighter(newId, Level, Breed, PlayerType, TeamId, IsStaticElement, newBuffs, Data);
    }

    public List<DamageRange> ComputeSeparatedPendingDamage()
    {
        return PendingEffects.Select(x => x.ComputeLifeDamage())
                             .Concat(PendingEffects.Select(x => x.ComputeShieldDamage()))
                             .ToList();
    }

    public void CarryFighter(HaxeFighter? fighter)
    {
        _carriedFighter = fighter;
    }

    public bool CanUsePortal()
    {
        return Data.CanBreedUsePortals() && !HasStateEffect(17);
    }

    public bool CanTeleport(ActionId actionId = ActionId.InvalidAction, bool isSwitchPosOnTarget = true, int? param3 = null)
    {
        if (!HasStateEffect(3) && (!HasStateEffect(18) || !IsSwitchTeleport(actionId)))
        {
            if (isSwitchPosOnTarget)
            {
                return Data.CanBreedSwitchPosOnTarget();
            }

            if (!Data.CanBreedSwitchPos())
            {
                return ActionIdHelper.CanTeleportOverBreedSwitchPos(actionId);
            }

            return true;
        }

        return false;
    }

    public bool CanSwitchPosition(HaxeFighter switcher, ActionId actionId = ActionId.InvalidAction, bool param3 = true)
    {
        if (HasState(3) || switcher.HasState(3))
        {
            return false;
        }

        if (CanTeleport(actionId, param3, switcher.GetCurrentPositionCell()))
        {
            return !HasState(3);
        }

        return false;
    }

    public bool CanBePushed()
    {
        if (!HasStateEffect(3) && Data.CanBreedBePushed())
        {
            return !HasStateEffect(3);
        }

        return false;
    }

    public bool CanBeMoved()
    {
        return !HasStateEffect(3);
    }

    public bool CanBeCarried()
    {
        if (!HasStateEffect(3) && Data.CanBreedBeCarried())
        {
            return !HasStateEffect(4);
        }

        return false;
    }

    public void AddTotalEffects(List<EffectOutput> effects)
    {
        if (TotalEffects != null)
        {
            TotalEffects.Append(effects);
        }
        else
        {
            TotalEffects = ConvertToLinkedList(effects);
        }
    }

    private HaxeLinkedList<T> ConvertToLinkedList<T>(IList<T> list)
    {
        var newList = new HaxeLinkedList<T>();

        foreach (var item in list)
        {
            newList.Add(item);
        }

        return newList;
    }
    

    public void AddPendingEffects(EffectOutput effectOutput)
    {
        PendingEffects.Add(effectOutput);
    }

    public void AddPendingBuffs(HaxeLinkedList<HaxeBuff> buffs)
    {
        foreach (var buff in buffs)
        {
            bool apply;

            if (ActionIdHelper.IsBuff(buff.Effect.ActionId) && !ActionIdHelper.IsShield(buff.Effect.ActionId))
            {
                apply = true;
            }
            else
            {
                if (!ActionIdHelper.IsDebuff(buff.Effect.ActionId) || ActionIdHelper.IsShield(buff.Effect.ActionId))
                {
                    continue;
                }

                apply = false;
            }

            UpdateStatFromBuff(buff, apply);
        }


        if (_pendingBuffHead == null)
        {
            _pendingBuffHead = buffs.Head;
        }

        Buffs = Buffs.Append(buffs);
    }

    public EffectOutput? AddPendingBuff(HaxeBuff buff)
    {
        if (_pendingBuffHead == null)
        {
            _pendingBuffHead = Buffs.Add(buff);
        }
        else
        {
            Buffs.Add(buff);
        }

        if (!buff.IsActive)
        {
            return null;
        }

        return ActivateBuff(buff);
    }

    public EffectOutput? ActivateBuff(HaxeBuff buff)
    {
        buff.IsApplied = true;

        if (buff.Effect.ActionId == ActionId.ControlEntity)
        {
            return EffectOutput.FromControlEntity(Id, buff.CasterId, buff.Effect.ActionId, buff.CasterId);
        }

        if (buff.Effect.ActionId == ActionId.CharacterMakeInvisible && !Data.IsInvisible)
        {
            return EffectOutput.FromInvisiblityStateChanged(Id, buff.CasterId, buff.Effect.ActionId, true);
        }

        if (ActivateSpellBuff(buff, out var effectOutput))
        {
            return effectOutput;
        }
        
        if (ActivateLookBuff(buff, out effectOutput))
        {
            return effectOutput;
        }
        
        return ActivateStatBuff(buff);
    }


    private bool ActivateLookBuff(HaxeBuff buff, out EffectOutput? effectOutput)
    {
        effectOutput = null;

        if (!ActionIdHelper.IsLookChange(buff.Effect.ActionId) && !ActionIdHelper.IsScaleChange(buff.Effect.ActionId))
        {
            return false;
        }

        effectOutput = EffectOutput.FromLookUpdate(Id, buff.CasterId, buff.Effect.ActionId);
        return true;
    }
    
    private bool ActivateSpellBuff(HaxeBuff buff, out EffectOutput? effectOutput)
    {
        effectOutput = null;

        var spellBoostId = ActionIdHelper.GetSpellModificationIdFromActionId(buff.Effect.ActionId);
        
        if (spellBoostId == -1)
        {
            return false;
        }

        effectOutput = UpdateSpellModificationFromBuff(buff, spellBoostId);
        return true;
    }

    private EffectOutput? ActivateStatBuff(HaxeBuff buff)
    {
        bool apply;

        if (ActionIdHelper.IsBuff(buff.Effect.ActionId) /* && !ActionIdHelper.IsShield(buff.Effect.ActionId)*/)
        {
            apply = true;
        }
        else
        {
            if (!ActionIdHelper.IsDebuff(buff.Effect.ActionId) /* || ActionIdHelper.IsShield(buff.Effect.ActionId)*/)
            {
                return null;
            }

            apply = false;
        }

        return UpdateStatFromBuff(buff, apply);
    }

    private bool DisableBuff(HaxeLinkedListNode<HaxeBuff> buff, out EffectOutput? output)
    {
        output = null;

        if (buff.Item.GetActionId() == ActionId.ControlEntity)
        {
            output = EffectOutput.FromNoControlEntity(Id, buff.Item.CasterId, buff.Item.Effect.ActionId);
        }
        else if (buff.Item.GetActionId() == ActionId.CharacterMakeInvisible && Data.IsInvisible)
        {
            output = EffectOutput.FromInvisiblityStateChanged(Id, buff.Item.CasterId, buff.Item.Effect.ActionId, false);
        }
        else if (DisableLookBuff(buff.Item, out var effectOutput))
        {
            output = effectOutput;
        }
        else if (DisableSpellBuff(buff.Item, out effectOutput))
        {
            output = effectOutput;
        }
        else if (DisableStatModifier(buff.Item, out effectOutput))
        {
            output = effectOutput;
        }

        return output != null;
    }
    
    private bool DisableLookBuff(HaxeBuff buff, out EffectOutput? effectOutput)
    {
        effectOutput = null;

        if (!ActionIdHelper.IsLookChange(buff.Effect.ActionId) && !ActionIdHelper.IsScaleChange(buff.Effect.ActionId))
        {
            return false;
        }

        effectOutput = EffectOutput.FromLookUpdate(Id, buff.CasterId, buff.Effect.ActionId);
        return true;
    }

    private bool DisableStatModifier(HaxeBuff buff, out EffectOutput? effectOutput)
    {
        effectOutput = null;
        if (!ActionIdHelper.IsStatModifier(buff.Effect.ActionId))
        {
            return false;
        }

        effectOutput = UpdateStatFromBuff(buff, !ActionIdHelper.IsBuff(buff.Effect.ActionId));
        return true;
    }

    private bool DisableSpellBuff(HaxeBuff buff, out EffectOutput? effectOutput)
    {
        effectOutput = null;

        var spellBoostId = ActionIdHelper.GetSpellModificationIdFromActionId(buff.Effect.ActionId);
        if (spellBoostId == -1)
        {
            return false;
        }
        
        effectOutput = UpdateSpellModificationFromBuff(buff, spellBoostId, -buff.Effect.Param3);
        return true;
    }

    public bool IsPlaying()
    {
        return Data.IsPlaying();
    }

    public bool UseSummonerTurnAndIsPlaying()
    {
        return Data.UseSummonerTurnAndIsPlaying();
    }

    public bool IsAllyWith(HaxeFighter haxeFighter)
    {
        return TeamId == haxeFighter.TeamId;
    }
}