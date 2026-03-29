using Bubble.Core.Datacenter.Datacenter.Effects;
using Bubble.DamageCalculation.Customs;
using Bubble.DamageCalculation.SpellManagement;
using Bubble.DamageCalculation.Tools;

namespace Bubble.DamageCalculation.DamageManagement;

public class DamageRange : Interval
{
    public static readonly DamageRange Zero = new(0, 0);

    public bool IsCollision { get; set; }
    public bool IsInvulnerable { get; set; }
    public int Group { get; set; }
    public bool IsCritical { get; set; }
    public bool IsHeal { get; set; }
    public bool IsShieldDamage { get; set; }
    public int ElemId { get; set; }
    public double Probability { get; set; }
    public bool FromBuff { get; set; }

    public DamageRange(int min, int max) : base(min, max)
    {
    }

    public static DamageRange FromSpellEffect(HaxeSpellEffect spellEffect, bool preview = false)
    {
        var dm = new DamageRange(!preview ? spellEffect.GetRandomRoll() : spellEffect.GetEffectMinRoll(), spellEffect.GetEffectMaxRoll())
        {
            IsCollision = spellEffect.ActionId == ActionId.CharacterLifePointsLostFromPush, // 80
            ElemId      = ElementsHelper.GetElementFromActionId(spellEffect.ActionId),
            IsCritical  = spellEffect.IsCritical,
            Probability = Math.Round(spellEffect.RandomWeight, 2),
            Group       = spellEffect.RandomGroup,
        };

        return dm;
    }

    public override Interval Copy()
    {
        return new DamageRange(Min, Max)
        {
            IsCollision    = IsCollision,
            ElemId         = ElemId,
            Probability    = Probability,
            Group          = Group,
            IsInvulnerable = IsInvulnerable,
            IsHeal         = IsHeal,
            IsShieldDamage = IsShieldDamage,
            IsCritical     = IsCritical,
            FromBuff = FromBuff
        };
    }
}