using Bubble.Core.Datacenter.Datacenter.Effects;
using Bubble.DamageCalculation.Tools;

namespace Bubble.DamageCalculation.SpellManagement;

public class HaxeSpell
{
    public static readonly HaxeSpell Empty = new(0,
                                            string.Empty,
                                                 Array.Empty<HaxeSpellEffect>(),
                                                 Array.Empty<HaxeSpellEffect>(),
                                                 0,
                                                 false,
                                                 false,
                                                 0, 0, 0, false,
                                                 false, false, false, 0,
                                                 false);
                                                              
    public string Name { get; set; }
    public bool NeedsVisibleEntity { get; set; }
    public bool NeedFreeTrapCell { get; }
    public bool NeedsTakenCell { get; set; }
    public bool NeedsFreeCell { get; set; }
    public int MinimaleRange { get; set; }
    public int MaximaleRange { get; set; }
    public int MaxEffectsStack { get; set; }
    public int Level { get; set; }
    public bool IsWeapon { get; set; }
    public bool IsTrap { get; set; }
    public bool IsGlyph { get; set; }
    public bool IsRune { get; set; }
    public int Id { get; set; }
    public int CriticalHitProbability { get; set; }
    public bool CanBeReflected { get; set; } = true;
    public bool CanAlwaysTriggerSpells { get; set; }
    public int CriticalHitBonus { get; set; }
    public bool CastTestLos { get; set; }

    private readonly IList<HaxeSpellEffect> _effects;
    private readonly IList<HaxeSpellEffect> _criticalEffects;


    public HaxeSpell(int id, string name, IList<HaxeSpellEffect> effects, IList<HaxeSpellEffect> criticalEffects, int level,
                     bool canAlwaysTriggerSpells, bool isWeapon, int minimaleRange, int maximaleRange,
                     int criticalHitProbability, bool needsFreeCell, bool needsTakenCell, bool needsVisibleEntity,
                     bool needFreeTrapCell, int maxEffectsStack, bool castTestLos)
    {
        Id                     = id;
        Name                   = name;
        _effects               = effects;
        _criticalEffects       = criticalEffects;
        Level                  = level;
        CanAlwaysTriggerSpells = canAlwaysTriggerSpells;
        IsWeapon               = isWeapon;
        MinimaleRange          = minimaleRange;
        MaximaleRange          = maximaleRange;
        CriticalHitProbability = criticalHitProbability;
        NeedsFreeCell          = needsFreeCell;
        NeedsTakenCell         = needsTakenCell;
        NeedsVisibleEntity     = needsVisibleEntity;
        NeedFreeTrapCell       = needFreeTrapCell;
        MaxEffectsStack        = maxEffectsStack;
        CastTestLos            = castTestLos;
        
        // hotfix cause idk this is shit
        if (Id == 13480) // Croisement roublard
        {
            var masks = _effects[0].Masks.ToList();
            masks.Add("e2501");
            var masksArr = masks.ToArray();
            Array.Sort(masksArr, HaxeSpellEffect.SortMasks);
            
            _effects[0].Masks = masksArr;
        }
    }

    public bool IsImmediateDamageInflicted(bool isCritical)
    {
        if (IsWeapon)
        {
            return true;
        }

        var effects = isCritical ? _criticalEffects : _effects;
        if (effects.Count == 0)
        {
            return false;
        }

        return effects.Any(effect => SpellManager.IsInstantaneousSpellEffect(effect) && ActionIdHelper.IsDamageInflicted(effect.ActionId));
    }

    public bool HasAtLeastOneRandomEffect()
    {
        return _effects.Any(effect => effect.RandomWeight > 0) ||
               _criticalEffects.Any(effect => effect.RandomWeight > 0);
    }

    public IList<HaxeSpellEffect> GetEffects()
    {
        return _effects;
    }

    public HaxeSpellEffect? GetEffectById(int id)
    {
        return _effects.FirstOrDefault(effect => effect.Id == id);
    }

    public IList<HaxeSpellEffect> GetCriticalEffects()
    {
        return _criticalEffects;
    }

    public HaxeSpellEffect? GetEffectByActionId(ActionId actionId)
    {
        return _effects.FirstOrDefault(effect => effect.ActionId == actionId);
    }

    public HaxeSpellEffect? GetCriticalEffectById(int id)
    {
        return _criticalEffects.FirstOrDefault(effect => effect.Id == id);
    }

    public HaxeSpellEffect? GetCriticalEffectByActionId(ActionId actionId)
    {
        return _criticalEffects.FirstOrDefault(effect => effect.ActionId == actionId);
    }



}