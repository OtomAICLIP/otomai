using System.Drawing;
using Bubble.Core.Datacenter.Datacenter.Effects;
using Bubble.DamageCalculation.Customs;
using Bubble.DamageCalculation.DamageManagement;
using Bubble.DamageCalculation.FighterManagement;

namespace Bubble.DamageCalculation.SpellManagement;

public class Mark
{
    public uint TeamId { get; set; }

    public GameActionMarkType MarkType { get; set; }
    public int MarkId { get; set; }
    public int MainCell { get; set; }
    public IList<int> Cells { get; set; }
    public IList<int> DisplayCells { get; set; }

    public long CasterId { get; set; }
    public HaxeSpell? AssociatedSpell { get; set; }
    public bool Aura { get; set; }
    public bool EndTrigger { get; set; }
    public bool Active { get; set; }
    public bool Used { get; set; }
    public bool IsImmediate { get; set; }
    public HaxeSpell? FromSpell { get; set; }
    //public HaxeSpell? ParentSpell { get; set; }

    public RunningEffect? FromEffect { get; set; }

    public bool IsNew { get; set; }
    public GameActionFightInvisibilityStateEnum Visibility { get; set; }
    public bool IsDeleted { get; set; }
    public bool WasJustSpawned { get; set; }
    public bool IsUpdated { get; set; }
    public bool IsStateUpdated { get; set; }
    public long DisabledUntilThisFighterPlay { get; set; }
    public List<long> TriggeredFighters { get; set; }
    public int Duration { get; set; }
    public int PortalBonus { get; private set; }
    public bool? LastActive { get; set; }

    private int _color;
    
    public Mark()
    {
        AssociatedSpell = null;
        MarkType        = 0;

        Cells    = Array.Empty<int>();
        Duration = -1;

        TriggeredFighters = new List<long>();
        DisplayCells      = new List<int>();
    }

    public bool DecrementDuration()
    {
        if (Duration is -1 or <= -100)
        {
            return false;
        }

        return --Duration <= 0;
    }
    
    public bool StopDrag()
    {
        switch (MarkType)
        {
            case GameActionMarkType.Trap:
            case GameActionMarkType.Wall:
                return true;
            default:
                return false;
        }
    }

    public static Mark CreateMark(FightContext context, int markId, GameActionMarkType markType, int markTeamId, int markMainCell,
        HaxeFighter markCaster, bool aura, bool endTrigger, HaxeSpell? associatedSpell, RunningEffect? fromEffect,
        bool active, bool isImmediate = false)
    {
        var parentEffect = fromEffect?.GetParentEffect();
        var parentSpell  = fromEffect?.GetSpell();

        if (parentEffect != null)
        {
            parentSpell = parentEffect.GetSpell();
        }
        
        var mark = new Mark
        {
            MarkId          = markId,
            MarkType        = markType,
            Aura            = aura,
            EndTrigger      = endTrigger,
            TeamId          = (uint)markTeamId,
            MainCell        = markMainCell,
            CasterId        = markCaster.Id,
            AssociatedSpell = associatedSpell,
            FromEffect      = fromEffect,
            FromSpell       = parentSpell ?? associatedSpell,
            //ParentSpell     = parentSpell,
            Active            = active,
            IsNew             = true,
            Visibility = markType == GameActionMarkType.Trap
                ? GameActionFightInvisibilityStateEnum.Invisible
                : GameActionFightInvisibilityStateEnum.Visible,
            TriggeredFighters = [],
            Duration          = fromEffect?.SpellEffect.Duration ?? -1,
            Cells = fromEffect?.SpellEffect.Zone.GetCells!(markMainCell, markMainCell) ?? 
                    new List<int>
                    {
                        markMainCell,
                    },
            IsImmediate = isImmediate,
            WasJustSpawned = true,
        };

        if (mark.Cells.Count > 0 && !mark.Cells.Contains(markMainCell) && aura && fromEffect?.SpellEffect.Zone is not { Shape: 'C', })
        {
            mark.MainCell = mark.Cells[0];
        }
            
        mark.DisplayCells = fromEffect?.SpellEffect.Zone is { Shape: 'C', MinRadius: 0, }
            ? new List<int>
            {
                markMainCell,
            }
            : mark.Cells.Where(context.IsCellWalkable).ToArray();

        if (fromEffect != null)
        {
            mark.PortalBonus = fromEffect.SpellEffect.Param3;
        }

        TryGetMarkColor(fromEffect, mark, parentSpell);

        if (mark.MarkType == GameActionMarkType.Trap)
        {
            mark.Visibility = GameActionFightInvisibilityStateEnum.Invisible;
        }

        mark.AdaptSpellToType(markType);

        markCaster.PendingEffects.Add(EffectOutput.FromMarkAdded(markCaster.Id, markCaster.Id, fromEffect?.SpellEffect.ActionId ?? 0, mark));
        return mark;
    }

    private static void TryGetMarkColor(RunningEffect? fromEffect, Mark mark, HaxeSpell? parentSpell)
    {
        if (mark.MarkType == GameActionMarkType.Portal)
        {
            mark._color = mark.TeamId == 1 ? Color.Blue.ToArgb() & 0xFFFFFF : Color.Red.ToArgb() & 0xFFFFFF;
            return;
        }

        mark._color = GetMarkColorBySpell(mark.AssociatedSpell, fromEffect?.SpellEffect);

        if (mark._color == 0)
        {
            mark._color = GetMarkColorBySpell(parentSpell, fromEffect?.SpellEffect);
        }

        if (mark._color != 0)
        {
            return;
        }

        if (fromEffect != null && fromEffect.SpellEffect.Param3 > 0)
        {
            mark._color = fromEffect.SpellEffect.Param3;
            return;
        }

        var effects = mark.AssociatedSpell?.GetEffects();

        if (effects == null)
        {
            return;
        }

        foreach (var effect in effects)
        {
            var elementId = effect.ElementId;

            if (elementId == -1)
            {
                elementId = effect.GetElement();
            }

            mark._color = GetMarkColorByElement(elementId);

            if (mark._color != 0)
            {
                break;
            }

            if (effect.ActionId == ActionId.CharacterActionPointsLost)
            {
                mark._color = 16777215;
                break;
            }
        }
    }

    public void SetMarkType(GameActionMarkType markType)
    {
        MarkType = markType;

        AdaptSpellToType(markType);
    }

    public void SetAssociatedSpell(HaxeSpell spell)
    {
        AssociatedSpell = spell;

        AdaptSpellToType(MarkType);
    }


    private void AdaptSpellToType(GameActionMarkType markType)
    {
        if (markType == 0 || AssociatedSpell == null)
        {
            return;
        }

        switch (markType)
        {
            case GameActionMarkType.Glyph:
                AssociatedSpell.IsGlyph = true;
                AssociatedSpell.IsTrap  = false;
                AssociatedSpell.IsRune  = false;
                break;
            case GameActionMarkType.Trap:
                AssociatedSpell.IsTrap  = true;
                AssociatedSpell.IsGlyph = false;
                AssociatedSpell.IsRune  = false;
                break;
            case GameActionMarkType.Rune:
                AssociatedSpell.IsRune  = true;
                AssociatedSpell.IsTrap  = false;
                AssociatedSpell.IsGlyph = false;
                break;
        }
    }

    public bool CanSee(int fromTeamId)
    {
        // spectator 
        if(fromTeamId == -1)
        {
            return Visibility == GameActionFightInvisibilityStateEnum.Visible;
        }
        
        return Visibility == GameActionFightInvisibilityStateEnum.Visible || fromTeamId == TeamId;
    }

    private static int GetMarkColorBySpell(HaxeSpell? spell, HaxeSpellEffect? fromEffectSpellEffect)
    {
        if (spell == null)
        {
            return 0;
        }

        // forgelance lance mark center
        if (fromEffectSpellEffect != null && spell.Id is 23262 or 24391)
        {
            if (fromEffectSpellEffect.Zone.Radius == 1)
            {
                return 5718180;
            }
        }

        return spell.Id switch
               {
                   // Sram
                   12906 => 12128795,
                   12910 => 3222918,
                   12930 => 4149784,
                   12914 => 9895830,
                   12916 => 1798857,
                   12918 => 9895830,
                   12942 => 12128795,
                   12920 => 5911580,
                   12921 => 5911580,
                   12931 => 1798857,
                   12950 => 1798857,
                   12941 => 12128795,
                   12948 => 5911580,
                   14314 => 3222918,

                   // Eniripsa
                   13201 => 15407341,
                   13211 => 15407341,
                   13197 => 15407341,
                   13236 => 15407341,

                   // Sacrieur
                   14000 => 13243184,
                   14001 => 13243184,
                   
                   // Ouginak
                   13801 => 3222918,

                   // Forgelance
                   23262 => 4337427,
                   24391 => 4337427,
                   23846 => 3222918,

                   // Osamodas
                   12696 => 11827225,

                   // Feca
                   12985 => 13243184,
                   12987 => 10902314,
                   12988 => 3222918,
                   12990 => 292238,
                   12991 => 5718180,
                   12992 => 3524728,
                   13025 => 13243184,
                   13042 => 12422470,
                   13021 => 10902314,
                   13030 => 554600,
                   13023 => 292238,
                   13024 => 5718180,
                   13013 => 3524728,

                   // Huppermage
                   13685 => 3524728,
                   13687 => 13243184,
                   13673 => 292238,
                   13686 => 10902314,
                   13719 => 3222918,

                   // Eliotrope
                   14574 => 16711680,
                   14573 => 16711680,
                   _     => 0,
               };
    }

    public static int GetMarkColorByElement(int? elementId)
    {
        return elementId switch
               {
                   0 or 1 => 10902314,
                   2      => 13243184,
                   3      => 292238,
                   4      => 3524728,
                   _      => 0,
               };
    }

    public ActionId GetActionTrigger()
    {
        switch (MarkType)
        {
            case GameActionMarkType.Glyph:
                return ActionId.ForceGlyphTrigger;
            case GameActionMarkType.Trap:
                return ActionId.ForceTrapTrigger;
            case GameActionMarkType.Rune:
                return ActionId.ForceRuneTrigger;
            default:
                return ActionId.ForceGlyphTrigger;
        }
    }

    public void Use()
    {
        Used           = true;
        Active         = false;
        IsStateUpdated = true;
        IsUpdated      = true;
    }

    public Mark Clone()
    {
        return new Mark
        {
            MarkId          = MarkId,
            MarkType        = MarkType,
            MainCell        = MainCell,
            CasterId        = CasterId,
            TeamId          = TeamId,
            AssociatedSpell = AssociatedSpell,
            FromSpell       = FromSpell,
            FromEffect      = FromEffect,
            DisplayCells    = DisplayCells,
            _color          = _color,
            Active          = Active,
            Used            = Used,
            IsStateUpdated  = IsStateUpdated,
            IsUpdated       = IsUpdated,
        };
    }
}