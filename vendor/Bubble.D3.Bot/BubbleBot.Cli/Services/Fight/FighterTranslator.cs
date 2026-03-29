using Bubble.DamageCalculation.Customs;
using Bubble.DamageCalculation.FighterManagement;

namespace BubbleBot.Cli.Services.Fight;

public class FighterTranslator : HaxeFighter
{
    public FighterTranslator(long actorId, int level, int breed, PlayerType playerType, int teamId, bool isStaticElement, HaxeBuff[] buffs, IFighterData data) :
        base(actorId,
             level,
             breed,
             playerType,
             teamId,
             isStaticElement,
             buffs,
             data)
    {
    }

    public FighterTranslator(HaxeFighter haxeFighter) :
        base(haxeFighter.Id,
             Math.Min(200, haxeFighter.Level),
             haxeFighter.Breed,
             haxeFighter.PlayerType,
             haxeFighter.TeamId,
             haxeFighter.IsStaticElement,
             haxeFighter.Buffs.ToArray(),
             haxeFighter.Data.Clone())
    {
        BeforeLastSpellPosition = haxeFighter.BeforeLastSpellPosition;
        IsDead                  = haxeFighter.IsDead;
    }

    public FighterTranslator Clone()
    {
        return new FighterTranslator(this);
    }
}