using Bubble.Core.Services;
using Bubble.DamageCalculation;
using BubbleBot.Cli.Repository.Maps;
using BubbleBot.Cli.Services.Fight.Zones;

namespace BubbleBot.Cli.Services.Fight;

public class FightLosDetectorService  : Singleton<FightLosDetectorService>
{
    public IList<Cell> GetRangeCells(SpellWrapper spell, int origin, bool isPreview)
    {
        if(spell.Caster.FightInfo == null)
        {
            return new List<Cell>();
        }
        
        var range    = spell.GetMaxRange();

        if (spell.Caster.IsPlayerBreed() && isPreview)
        {
            range = Math.Min(7, range);
        }
        
        var minRange = spell.GetMinRange();
        var shape    = spell.GetSpellShape();

        var castInLine     = spell.GetCastInLine() || shape == SpellShape.l;
        var castInDiagonal = spell.GetCastInDiagonal();
        var castTestLos    = spell.GetCastTestLos();

        return GetRangeCells(spell.Caster.FightInfo.Map,
                             spell.Caster.FightInfo,
                             origin, 
                             range,
                             minRange, 
                             castInLine,
                             castInDiagonal,
                             castTestLos);
    }
    
    public IList<Cell> GetRangeCells(Map map, FightInfo fight, int origin, int range, int minRange, bool castInLine,
        bool castInDiagonal, bool castTestLos)
    {
        DisplayZone zone;

        if (range < minRange)
        {
            range = minRange;
        }

        range = (int)Math.Min(range, MapConstants.Width * MapConstants.Height);

        if (range < 0)
        {
            range = 0;
        }

        if (castInLine && castInDiagonal)
        {
            zone = new Cross(SpellShape.Unknown, (uint)minRange, (uint)range, map, false, true);
        }
        else if (castInLine)
        {
            zone = new Cross(SpellShape.Unknown, (uint)minRange, (uint)range, map);
        }
        else if (castInDiagonal)
        {
            zone = new Cross(SpellShape.Unknown, (uint)minRange, (uint)range, map, true);
        }
        else
        {
            zone = new Lozenge(SpellShape.Unknown, (uint)minRange, (uint)range, map);
        }

        var untargetableCells = new List<Cell>();
        var allCells          = zone.GetCells((uint)origin);


        Custom losZone;

        if (!castTestLos)
        {
            losZone = new Custom(allCells.ToArray(), map);
        }
        else
        {
            losZone = new Custom(GetCell(fight, allCells, MapPoint.GetPoint(origin)!), map);

            var noLosRangeCell = zone.GetCells((uint)origin);
            var losRangeCell   = losZone.GetCells((uint)origin).ToArray();

            untargetableCells.AddRange(noLosRangeCell.Where(cell => !losRangeCell.Contains(cell)));
        }

        //var portalUsableCells = new List<Cell>();
        //var cells             = new List<Cell>();

        /*if (mpWithPortals.Count < 2)
        {
            return losZone.GetCells((uint)origin).Where(cell => !untargetableCells.Contains(cell)).ToList();
        }*/

        return losZone.GetCells((uint)origin).Where(cell => !untargetableCells.Contains(cell)).ToList();
    }

    public IList<Cell> GetCell(FightInfo fight, IEnumerable<Cell> range, MapPoint refPosition)
    {
        var rangeArr = range as Cell[] ?? range.ToArray();
        var orderedCell = rangeArr.Select(r => MapPoint.GetPoint(r.Id)!)
                                  .Select(mp => (mp, refPosition.ManhattanDistanceTo(mp)))
                                  .ToList();

        // Sort on distance
        orderedCell.Sort((a, b) => a.Item2.CompareTo(b.Item2));
        orderedCell.Reverse();

        var tested = new Dictionary<string, bool>(StringComparer.Ordinal);

        var result = new List<short>();

        foreach (var (mp, _) in orderedCell)
        {
            var key = $"{mp.X}_{mp.Y}";

            if (tested.ContainsKey(key) && refPosition.X + refPosition.X != mp.X + mp.Y &&
                refPosition.X - refPosition.Y != mp.X - mp.Y)
            {
                continue;
            }

            var line = MapTools.GetCellsCoordBetween(refPosition.CellId, mp.CellId);

            if (line.Count == 0)
            {
                result.Add((short)mp.CellId);
            }
            else
            {
                var los          = true;
                var currentPoint = string.Empty;

                for (var j = 0; j < line.Count; j++)
                {
                    currentPoint = $"{line[j].X}_{line[j].Y}";

                    if (!MapPoint.IsInMap(line[j].X, line[j].Y))
                    {
                        continue;
                    }

                    var cellId2 = (short)MapTools.GetCellIdByCoord(line[j].X, line[j].Y);

                    if (j > 0 && fight.HasEntity((short)MapTools.GetCellIdByCoord(line[j - 1].X, line[j - 1].Y),
                        true))
                    {
                        los = false;
                    }
                    else if (line[j].X + line[j].Y == refPosition.X + refPosition.Y ||
                             line[j].X - line[j].Y == refPosition.X - refPosition.Y)
                    {
                        los = los && fight.PointLos(cellId2);
                    }
                    else if (!tested.TryGetValue(currentPoint, out var currentLos))
                    {
                        los = los && fight.PointLos(cellId2);
                    }
                    else
                    {
                        los = los && currentLos;
                    }
                }

                tested[currentPoint] = los;
            }
        }

        foreach (var r in rangeArr)
        {
            var mp = MapPoint.GetPoint(r.Id)!;

            if (tested.TryGetValue(mp.X + "_" + mp.Y, out var los) && los)
            {
                result.Add((short)r.Id);
            }
        }

        return result.Select(r => fight.Map.Data.GetCell(r)!).ToList();
    }

}