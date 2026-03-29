using Bubble.DamageCalculation.FighterManagement;
using Bubble.DamageCalculation.SpellManagement;

namespace Bubble.DamageCalculation;

public interface IDamageCalculationInterface
{
    public HaxeFighter SummonDouble(HaxeFighter caster, bool isIllusion);
    public HaxeFighter SummonMonster(HaxeFighter caster, int id, int? grade = null);
    bool SummonTakesSlot(int id);
    public HaxeSpell? GetStartingSpell(HaxeFighter fighter, int? gradeId = null);
    public HaxeSpell? GetLinkedExplosionSpellFromFighter(HaxeFighter bomb);
    public HaxeSpell? GetBombExplosionSpellFromFighter(HaxeFighter bomb);
    public HaxeSpell GetBombCastOnFighterSpell(int bombId, int level);
    public HaxeSpell? GetBombWallSpellFromFighter(HaxeFighter bomb);
    public HaxeSpellState CreateStateFromId(int stateId);
    public HaxeSpell? CreateSpellFromId(int spellId, int level);
}