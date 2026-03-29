using Bubble.Core.Datacenter.Datacenter.Effects;
using Bubble.Core.Datacenter.Datacenter.Spells;
using Bubble.DamageCalculation;
using Bubble.DamageCalculation.Customs;
using Bubble.DamageCalculation.SpellManagement;
using BubbleBot.Cli.Repository.Maps;
using BubbleBot.Cli.Services.Clients.Contracts;
using BubbleBot.Cli.Services.Fight.Zones;

namespace BubbleBot.Cli.Services.Fight;

public class SpellWrapper
{
    public ISpellCasterContext Caster { get; set; }
    public Spells Spell { get; }
    public SpellLevels SpellLevel { get; }
    public bool Available { get; }
    public bool IsWeapon { get; }
    public int Id => Spell.Id;

    public SpellWrapper(ISpellCasterContext caster, Spells spell, SpellLevels spellLevel, bool available)
    {
        Caster = caster;
        Spell = spell;
        SpellLevel = spellLevel;
        Available = available;
        IsWeapon = spell.Id == 0;
    }
    public int LastCastTurn { get; set; }

    public bool GetRangeCanBeBoosted()
    {
        var rangeCanBeBoosted = SpellLevel.RangeCanBeBoosted;

        return rangeCanBeBoosted;
    }


    public int GetMaxRange()
    {
        var rangeCanBeBoosted = GetRangeCanBeBoosted();
        var finalRange = (int)SpellLevel.Range;

        if (Caster.Fighter == null)
            return finalRange;

        if (rangeCanBeBoosted)
        {
            finalRange += (int)Caster.Fighter.Stats[StatId.Range].Total;
        }

        if (finalRange < GetMinRange())
        {
            finalRange = GetMinRange();
        }

        return finalRange;
    }

    public int GetMinRange()
    {
        var finalRange = SpellLevel.MinRange;
        return finalRange;
    }

    public SpellShape GetSpellShape()
    {
        var zone = GetPreferredPreviewZone();

        if (zone == null)
        {
            return SpellShape.Unknown;
        }

        return zone.Shape;
    }

    public DisplayZone? GetPreferredPreviewZone(bool isWholeMapShapeIgnored = false,
                                                bool isInfiniteSizeIgnored  = true,
                                                bool isTooltipFilter        = false,
                                                int  outputPortalCell       = -1)
    {
        DisplayZone? biggestZone = null;

        uint lastSurface = 0;

        foreach (var effect in SpellLevel.Effects)
        {
            var zone = effect.ZoneDescription;

            if (zone.Shape == (int)SpellShape.Unknown || isTooltipFilter && !effect.VisibleInTooltip)
            {
                continue;
            }

            var currentZone = GetZone((SpellShape)zone.Shape,
                                      zone.Param1,
                                      zone.Param2,
                                      isWholeMapShapeIgnored,
                                      zone.IsStopAtTarget,
                                      Spell.Id == 0,
                                      outputPortalCell);
            
            if(currentZone == null)
                continue;

            var currentSurface = currentZone.Surface;
            if ((!isInfiniteSizeIgnored || !currentZone.IsInfinite) && currentSurface > lastSurface)
            {
                biggestZone = currentZone;
                lastSurface = currentSurface;
            }
        }

        return biggestZone;
    }

    public DisplayZone? GetZone(SpellShape shape, uint size, uint alternativeSize, bool isWholeMapShapeIgnored = false,
                                bool       isZoneStopAtTarget = false, bool isWeapon = false, int entityCellId = -1)
    {
        if(Caster.Fighter?.Fight == null)
            return null;
        
        if (Caster.Fighter == null)
            return null;
        
        switch (shape)
        {
            case SpellShape.X:
                return new Cross(shape,
                                 alternativeSize,
                                 isWeapon || size > 0 ? size : alternativeSize > 0 ? alternativeSize : size,
                                 Caster.Fighter.Fight.Map!);
            case SpellShape.L:
                return new Line(shape, 0, size, Caster.Fighter.Fight.Map);
            case SpellShape.l:
                return new Line(shape,
                                alternativeSize,
                                size,
                                Caster.Fighter.Fight.Map,
                                true,
                                isZoneStopAtTarget,
                                (uint)(entityCellId != -1 ? entityCellId : Caster.Fighter.CellId));
            case SpellShape.T:
                return new Cross(shape, 0, size, Caster.Fighter.Fight.Map);
            case SpellShape.D:
                return new Cross(shape, 0, size, Caster.Fighter.Fight.Map);
            case SpellShape.C:
                return new Lozenge(shape, alternativeSize, size, Caster.Fighter.Fight.Map);
            case SpellShape.O:
                return new Lozenge(shape, size, size, Caster.Fighter.Fight.Map);
            case SpellShape.Q:
                return new Cross(shape,
                                 alternativeSize > 0 ? alternativeSize : 1,
                                 size > 0 ? size : 1,
                                 Caster.Fighter.Fight.Map);
            case SpellShape.V:
                return new Cone(0, size, Caster.Fighter.Fight.Map);
            case SpellShape.W:
                return new Square(0, size, true, Caster.Fighter.Fight.Map);
            case SpellShape.Plus:
                return new Cross(shape, 0, size > 0 ? size : 1, Caster.Fighter.Fight.Map, true);
            case SpellShape.Sharp:
                return new Cross(shape, alternativeSize, size, Caster.Fighter.Fight.Map, true);
            case SpellShape.Slash:
                return new Line(shape, 0, size, Caster.Fighter.Fight.Map);
            case SpellShape.Star:
                return new Cross(shape, 0, size, Caster.Fighter.Fight.Map, false, true);
            case SpellShape.Minus:
                return new Cross(shape, 0, size, Caster.Fighter.Fight.Map, true);
            case SpellShape.G:
                return new Square(0, size, false, Caster.Fighter.Fight.Map);
            case SpellShape.I:
                return new Lozenge(shape, size, 63, Caster.Fighter.Fight.Map);
            case SpellShape.U:
                return new HalfLozenge(0, size, Caster.Fighter.Fight.Map);
            case SpellShape.A:
            case SpellShape.a:
                if (!isWholeMapShapeIgnored)
                {
                    return new Lozenge(shape, 0, 63, Caster.Fighter.Fight.Map);
                }

                return new Cross(shape, 0, size, Caster.Fighter.Fight.Map);
            case SpellShape.R:
                return new Rectangle(alternativeSize, size, Caster.Fighter.Fight.Map);
            case SpellShape.F:
                return new Fork(size, Caster.Fighter.Fight.Map);
            case SpellShape.P:
            default:
                return new Cross(shape, 0, 0, Caster.Fighter.Fight.Map);
        }
    }

    public bool GetCastInLine()
    {
        return SpellLevel.CastInLine;
    }

    public bool GetCastInDiagonal()
    {
        return SpellLevel.CastInDiagonal;
    }

    public bool GetCastTestLos()
    {
        return SpellLevel.CastTestLos;
    }

    public int GetApCost()
    {
        return SpellLevel.ApCost;
    }

    public bool GetNeedTakenCell()
    {
        var needTakenCell = SpellLevel.NeedTakenCell;

        return needTakenCell;
    }

    public bool GetNeedFreeCell()
    {
        var needFreeCell = SpellLevel.NeedFreeCell;

        return needFreeCell;
    }
    public bool GetPortalProjectionForbidden()
    {
        var needPortalProjectionForbidden = SpellLevel.PortalProjectionForbidden;

        return needPortalProjectionForbidden;
    }
    public (int InputPortalCellId, int TargetedCell, IList<Mark> PortalsUsed)? GetTargetedCell(Cell targetedCell)
    {
        if (GetPortalProjectionForbidden())
        {
            return null;
        }

        var marks = Caster.Fighter!.Fight!.GetMarkInteractingWithCell(targetedCell.Id, true, GameActionMarkType.Portal);

        if (marks.Count == 0)
        {
            return null;
        }

        var portal = marks[0];

        if (SpellLevel.Effects.Any(x => x.EffectId == (int)ActionId.FightDisablePortal))
        {
            return null;
        }

        if (!portal.Active || portal.Used)
        {
            return null;
        }

        var output = Caster.Fighter.Fight.GetOutputPortals(portal, out var usedPortals);

        if (output == -1 || usedPortals.Count == 0)
        {
            return null;
        }

        var exitPoint    = new MapPoint((short)output);
        var targetPortal = new MapPoint(targetedCell);
        var fighterPoint = new MapPoint((short)Caster.Fighter!.CellId);

        var symmetricalTargetX = targetPortal.X - fighterPoint.X + exitPoint.X;
        var symmetricalTargetY = targetPortal.Y - fighterPoint.Y + exitPoint.Y;

        if (!MapPoint.IsInMap(symmetricalTargetX, symmetricalTargetY))
        {
            return null;
        }

        var newCellId = MapTools.GetCellIdByCoord(symmetricalTargetX, symmetricalTargetY);

        var inputPortalCellId = portal.MainCell;

        return (inputPortalCellId, newCellId, usedPortals);
    }

    public int GetMaxCastPerTurn()
    {
        return SpellLevel.MaxCastPerTurn;
    }
    
    public int GetMaxCastPerTarget()
    {
        return SpellLevel.MaxCastPerTarget;
    }
    
    public int GetCastInterval()
    {
        var minCastInterval = SpellLevel.MinCastInterval;

        return minCastInterval;
    }

    public int GetCooldown()
    {
        var interval = GetCastInterval();
        
        if (interval == 63)
        {
            return -63;
        }

        try
        {

            var initialCooldown = Caster.FightInfo!.Turn - SpellLevel.InitialCooldown;
            if (initialCooldown <= 0 && SpellLevel.InitialCooldown != 0)
            {
                return initialCooldown;
            }

            if (LastCastTurn <= 0)
            {
                return 0;
            }

            var cooldown = Caster.FightInfo!.Turn - (LastCastTurn + interval);

            if (cooldown <= 0)
            {
                return cooldown;
            }

            return 0;
        }
        catch
        {
            return -1;
        }
    }

    public void Reset()
    {
        LastCastTurn = 0;
    }

}
