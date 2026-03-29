using Bubble.DamageCalculation.DamageManagement;

namespace Bubble.DamageCalculation.Customs;

public class DodgeResult
{
    public bool Done { get; set; }
    public List<EffectOutput> Damage { get; set; }
}