using System.Collections.Concurrent;
using Bubble.Core.Datacenter.Datacenter.Effects;
using Bubble.Core.Services;
using Bubble.DamageCalculation;
using Bubble.DamageCalculation.FighterManagement;
using Bubble.DamageCalculation.SpellManagement;
using BubbleBot.Cli.Repository;

namespace BubbleBot.Cli.Services.Fight;

public class DamageCalculationTranslator : Singleton<DamageCalculationTranslator>, IDamageCalculationInterface
{
    private readonly ConcurrentDictionary<int, HaxeSpell> _cacheSpells = new();
    private readonly ConcurrentDictionary<int, HaxeSpellEffect> _cacheSpellEffects = new();
    
    private static readonly Dictionary<int, SpellId> WallsSpells = new()
    {
        { 2, SpellId.MurDeFeu },
        { 3, SpellId.MurDAir },
        { 4, SpellId.MurDEau },
        { 5, SpellId.MurDeTerre },
    };

    public void Initialize()
    {
        DamageCalculator.DataInterface = this;

    }

    public HaxeFighter SummonDouble(HaxeFighter caster, bool isIllusion)
    {
        return caster;
    }
    
    public HaxeFighter SummonMonster(HaxeFighter caster, int id, int? grade = null)
    {
        return caster;
    }
    public bool SummonTakesSlot(int id)
    {
        return false;
    }
    public HaxeSpell? GetStartingSpell(HaxeFighter fighter, int? gradeId = null)
    {
        return null;
    }
    public HaxeSpell? GetLinkedExplosionSpellFromFighter(HaxeFighter bomb)
    {
        return null;
    }
    public HaxeSpell? GetBombExplosionSpellFromFighter(HaxeFighter bomb)
    {
        return null;
    }
    public HaxeSpell GetBombCastOnFighterSpell(int bombId, int level)
    {
        return null!;
    }
    public HaxeSpell? GetBombWallSpellFromFighter(HaxeFighter bomb)
    {
        return null;
    }
    public HaxeSpellState CreateStateFromId(int stateId)
    {
        if (!SpellRepository.Instance.TryGetSpellState((short)stateId, out var state))
        {
            throw new Exception($"SpellState {stateId} not found");
        }

        return new HaxeSpellState((uint)state!.Id, state.EffectsIds.ToArray(), state);
    }
    public HaxeSpell? CreateSpellFromId(int spellId, int level)
    {
        var hash = DamageCalculator.Create32BitHashSpellLevel(spellId, (byte)level);

        if (_cacheSpells.TryGetValue(hash, out var haxeSpell))
        {
            return haxeSpell;
        }

        var spell = SpellRepository.Instance.GetSpell(spellId);
        if(spell == null)
        {
            return null;
        }

        var spellLevel = SpellRepository.Instance.GetSpellLevel(spellId, (short)level);

        if (spellLevel == null)
        {
            return null;
        }

        _cacheSpells[hash] = new HaxeSpell(spellId,
                                           spell.Name,
                                           GetHaxeSpellEffects(spellLevel.Effects, level, false),
                                           GetHaxeSpellEffects(spellLevel.CriticalEffect, level, true),
                                           level,
                                           spell.CanAlwaysTriggerSpells,
                                           spellId == 0,
                                           spellLevel.MinRange,
                                           spellLevel.Range,
                                           spellLevel.CriticalHitProbability,
                                           spellLevel.NeedFreeCell,
                                           spellLevel.NeedTakenCell,
                                           spellLevel.NeedVisibleEntity,
                                           spellLevel.NeedFreeTrapCell,
                                           spellLevel.MaxStack,
                                           spellLevel.CastTestLos);

        return _cacheSpells[hash];
    }
    
    private IList<HaxeSpellEffect> GetHaxeSpellEffects(IEnumerable<EffectInstanceDice> effects, int level,
                                                       bool isCritical)
    {
        var haxeSpellEffects = new List<HaxeSpellEffect>();

        foreach (var eff in effects)
        {
            if (eff.EffectUid > 0 && _cacheSpellEffects.TryGetValue(eff.EffectUid, out var haxeSpellEffect))
            {
                haxeSpellEffects.Add(haxeSpellEffect);
                continue;
            }

            var effect = EffectRepository.Instance.GetEffect(eff.EffectId);
            
            if (effect == null)
            {
                continue;
            }

            if (eff.ForClientOnly)
            {
                continue;
            }

            var spellEffect = new HaxeSpellEffect(eff.EffectUid, 
                                                  level,
                                                  eff.Order, 
                                                  (ActionId)eff.EffectId,
                                                  eff.DiceNum, 
                                                  eff.DiceSide, 
                                                  eff.Value,
                                                  eff.Duration, 
                                                  isCritical,
                                                  string.IsNullOrEmpty(eff.Triggers) ? "I" : eff.Triggers,
                                                  eff.ZoneDescription.Shape + eff.ZoneDescription.Param1.ToString(),
                                                  string.IsNullOrEmpty(eff.TargetMask) ? "A,g" : eff.TargetMask,
                                                  eff.Random,
                                                  eff.Group, 
                                                  eff.Dispellable,
                                                  eff.Delay,
                                                  effect.Category,
                                                  effect.ForceMinMax,
                                                  eff.Dispellable ? 1 : 3,
                                                  eff.EffectElement);
            spellEffect.SetZoneFrom(eff.ZoneDescription);

            _cacheSpellEffects[eff.EffectUid] = spellEffect;

            haxeSpellEffects.Add(spellEffect);
        }

        return haxeSpellEffects;
    }

}