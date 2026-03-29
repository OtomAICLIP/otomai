using System.Drawing;

namespace Bubble.DamageCalculation.Tools;

public class LinkedCellManager
{
    public static List<uint> GetLinks(int startPoint, List<int>? checkPoints)
    {
        if (checkPoints == null || checkPoints.Count == 1 && startPoint == checkPoints[0])
        {
            return new List<uint> { (uint)startPoint, };
        }

        var pointsList = checkPoints.Where(x => x != startPoint).ToList();

        var res     = new List<uint>();
        var current = startPoint;
        var maxTry  = pointsList.Count + 1;

        while (pointsList.Count > 0 || maxTry > 0)
        {
            maxTry--;
            res.Add((uint)current);
            var index = pointsList.IndexOf(current);

            if (index != -1)
            {
                pointsList.RemoveAt(index);
            }

            var next = GetClosestPortal(current, pointsList);

            if (next == null)
            {
                break;
            }

            current = next.Value;
        }

        if (res.Count < 2)
        {
            return new List<uint> { (uint)startPoint, };
        }

        return res;
    }

    public static int? GetClosestPortal(int refMapPoint, List<int> portals)
    {
        var closests = new List<int>();
        var bestDist = 63;

        foreach (var portal in portals)
        {
            var dist = MapTools.GetDistance(refMapPoint, portal);
            if (dist < bestDist)
            {
                closests.Clear();
                closests.Add(portal);
                bestDist = (int)dist;
            }
            else
            {
                if (dist == bestDist)
                {
                    closests.Add(portal);
                }
            }
        }

        if (closests.Count == 0)
        {
            return null;
        }

        if (closests.Count == 1)
        {
            return closests[0];
        }

        return GetBestNextPortal(refMapPoint, closests);
    }

    public static int GetBestNextPortal(int refCell, List<int> closests)
    {
        if (closests.Count < 2)
        {
            throw new Exception("closests should have a size of 2");
        }

        var refCoord = MapTools.GetCellCoordById(refCell)!.Value;
        var nudge    = MapTools.GetCellCoordById(refCell)!.Value;

        closests.Sort((o1, o2) =>
        {
            var o1Coord = MapTools.GetCellCoordById(o1)!.Value;
            var o2Coord = MapTools.GetCellCoordById(o2)!.Value;

            var res = MathUtils.GetPositiveOrientedAngle(refCoord, nudge, o1Coord) - MathUtils.GetPositiveOrientedAngle(refCoord, nudge, o2Coord);

            return res > 0 ? 1 : res < 0 ? -1 : 0;
        });

        var res = GetBestPortalWhenRefIsNotInsideClosests(refCell, closests);

        if (res != null)
        {
            return res.Value;
        }

        return closests[0];
    }

    public static int? GetBestPortalWhenRefIsNotInsideClosests(int refCell, List<int> sortedClosests)
    {
        if (sortedClosests.Count < 2)
        {
            return null;
        }

        var refCoord = MapTools.GetCellCoordById(refCell)!.Value;

        var prev      = sortedClosests[^1];
        var prevCoord = MapTools.GetCellCoordById(prev)!.Value;

        foreach (var portal in sortedClosests)
        {
            var portalCoord = MapTools.GetCellCoordById(portal)!.Value;

            switch (MathUtils.CompareAngles(refCoord, prevCoord, new Point(portalCoord.X, portalCoord.Y)))
            {
                case CompareAngle.Unlike:
                    if (sortedClosests.Count <= 2)
                    {
                        return null;
                    }

                    break;
                case CompareAngle.Counterclockwise:
                    return prev;
            }

            prev = portal;
        }

        return null;
    }
}