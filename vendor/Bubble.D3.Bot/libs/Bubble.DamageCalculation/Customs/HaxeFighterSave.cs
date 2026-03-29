using Bubble.DamageCalculation.DamageManagement;
using Bubble.DamageCalculation.FighterManagement;

namespace Bubble.DamageCalculation.Customs;

public class HaxeFighterSave
{
    public required long Id { get; set; }
    public required IList<EffectOutput> Outputs { get; set; }
    public required HaxeLinkedList<HaxeBuff> Buffs { get; set; }
    public required int Cell { get; set; }
    public required int PendingPreviousPosition { get; set; }
}