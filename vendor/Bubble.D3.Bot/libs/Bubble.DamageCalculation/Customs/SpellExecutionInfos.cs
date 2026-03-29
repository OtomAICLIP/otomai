using Bubble.Core.Datacenter.Datacenter.Effects;
using Bubble.DamageCalculation.FighterManagement;
using Bubble.DamageCalculation.SpellManagement;

namespace Bubble.DamageCalculation.Customs;

public record SpellExecutionInfos(FightContext Context, HaxeFighter? Caster, HaxeSpell Spell, bool IsCritical,
                                  ActionId ActionId)
{
    public override string ToString()
    {
        return $"{{ Context = {Context}, Caster = {Caster}, Spell = {Spell}, IsCritical = {IsCritical} }}";
    }
}