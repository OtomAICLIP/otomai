using Bubble.DamageCalculation.DamageManagement;
using Bubble.DamageCalculation.FighterManagement;

namespace Bubble.DamageCalculation.SpellManagement;

public class RunningEffect
{
    public FightContext? Context { get; }
    public HaxeSpellEffect SpellEffect { get; set; }
    public HaxeFighter Caster { get; set; }
    public HaxeSpell Spell { get; set; }
    public bool IsTriggered { get; set; }
    public EffectOutput? TriggeringOutput { get; set; }
    public HaxeFighter? TriggeringFighter { get; set; }
    public double Probability { get; set; }
    public bool IsReflect { get; set; }
    public bool HasDamageToHeal { get; set; }
    public bool ForceCritical { get; set; }

    public bool CanAlwaysTriggerSpells { get; set; }

    public RunningEffect? ParentEffect { get; set; }

    public RunningEffect(FightContext? context, HaxeFighter caster, HaxeSpell spell, HaxeSpellEffect spellEffect, double probability = 0, bool canAlwaysTriggerSpells = true)
    {
        Context                = context;
        Caster                 = caster;
        SpellEffect            = spellEffect.Clone();
        Probability            = probability;
        CanAlwaysTriggerSpells = canAlwaysTriggerSpells;
        Spell                  = spell;
    }

    public void TriggeredByEffectSetting(RunningEffect runningEffect)
    {
        ParentEffect      = runningEffect;
        TriggeringFighter = runningEffect.Caster;
    }

    public void SetParentEffect(RunningEffect? parentEffect)
    {
        ParentEffect = parentEffect;

        if (TriggeringFighter == null && ParentEffect != null)
        {
            TriggeringFighter = ParentEffect.TriggeringFighter;
        }
    }

    public void OverrideSpellEffect(HaxeSpellEffect spellEffect)
    {
        SpellEffect = spellEffect;
    }

    public void OverrideCaster(HaxeFighter caster)
    {
        Caster = caster;
    }

    public bool IsTargetingAnAncestor(HaxeFighter haxeFighter)
    {
        var effect = this;
        while (effect != null)
        {
            if (effect.Caster.Id == haxeFighter.Id)
            {
                return true;
            }

            effect = effect.ParentEffect;
        }

        return false;
    }

    public HaxeFighter GetCaster()
    {
        return Caster;
    }

    public HaxeSpell GetSpell()
    {
        return Spell;
    }

    public HaxeSpellEffect GetSpellEffect()
    {
        return SpellEffect;
    }

    public RunningEffect? GetParentEffect()
    {
        return ParentEffect;
    }

    public RunningEffect? GetFirstParentEffect()
    {
        var effect = this;
        while (effect.ParentEffect != null)
        {
            effect = effect.ParentEffect;
        }

        return effect;
    }
    
    public RunningEffect? GetLastTriggeredEffect()
    {
        var effect = this;
        if(IsTriggered)
        {
            return effect;
        }
        
        while (effect.ParentEffect != null)
        {
            effect = effect.ParentEffect;
            
            if (effect.IsTriggered)
            {
                return effect;
            }
        }

        return effect;
    }

    public RunningEffect Copy()
    {
        var re = new RunningEffect(Context, Caster, Spell, SpellEffect, Probability, CanAlwaysTriggerSpells)
        {
            IsTriggered       = IsTriggered,
            IsReflect         = IsReflect,
            TriggeringFighter = TriggeringFighter,
            TriggeringOutput  = TriggeringOutput,
            ForceCritical     = ForceCritical,
        };
        re.SetParentEffect(this);

        return re;
    }
}