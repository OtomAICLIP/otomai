using System.Drawing;
using Bubble.DamageCalculation.SpellManagement;

namespace Bubble.DamageCalculation.Tools;

public static class PortalUtils
{
    /// <summary>
    /// Get the nearest portal cell to the given cellId.
    /// </summary>
    /// <param name="cellId">The cell id to find the nearest portal cell from.</param>
    /// <param name="portalCells">The list of portal cell ids.</param>
    /// <returns>The nearest portal cell id. Returns -1 if the list of portal cells is empty.</returns>
    public static int GetNearestPortalCell(int cellId, IList<int> portalCells)
    {
        var nearestDistance    = 63;
        var nearestPortalCells = new List<int>();

        foreach (var portalCell in portalCells)
        {
            var distance = MapTools.GetDistance(cellId, portalCell);

            if (distance < nearestDistance)
            {
                nearestPortalCells.Clear();
                nearestPortalCells.Add(portalCell);
                nearestDistance = distance;
            }
            else if (distance == nearestDistance)
            {
                nearestPortalCells.Add(portalCell);
            }
        }

        if (nearestPortalCells.Count <= 0)
        {
            return -1;
        }

        if (nearestPortalCells.Count == 1)
        {
            return nearestPortalCells[0];
        }

        return GetNextNearestPortalCell(cellId, nearestPortalCells.ToArray());
    }

    /// <summary>
    /// Get the next nearest portal cell to the given cellId when there are multiple nearest portal cells.
    /// </summary>
    /// <param name="cellId">The cell id to find the next nearest portal cell from.</param>
    /// <param name="nearestPortalCells">The list of nearest portal cell ids.</param>
    /// <returns>The next nearest portal cell id.</returns>
    public static int GetNextNearestPortalCell(int cellId, int[] nearestPortalCells)
    {
        if (nearestPortalCells.Length < 2)
        {
            throw new ArgumentException("nearestPortalCells should have a minimum length of 2");
        }

        var targetedPortalCellCoord = MapTools.GetCellCoordById(cellId)!.Value;
        var nudgeCoord              = targetedPortalCellCoord with { Y = targetedPortalCellCoord.Y + 1, };

        Array.Sort(nearestPortalCells, (param1, param2) =>
        {
            var value =
                MathUtils.GetPositiveOrientedAngle(targetedPortalCellCoord, nudgeCoord,
                                                   MapTools.GetCellCoordById(param1)!.Value) -
                MathUtils.GetPositiveOrientedAngle(targetedPortalCellCoord, nudgeCoord,
                                                   MapTools.GetCellCoordById(param2)!.Value);
            return (int)Math.Floor(value);
        });

        var nextNearestPortal =
            GetNextNearestPortalWhenTargetedPortalCellIsNotContained(targetedPortalCellCoord, nearestPortalCells);

        if (nextNearestPortal != -1)
        {
            return nextNearestPortal;
        }

        return GetNextNearestPortalCellWhenTargetedPortalCellIsContained(targetedPortalCellCoord, nearestPortalCells);
    }

    /// <summary>
    /// Get the next nearest portal when the targeted portal cell is not contained.
    /// </summary>
    /// <param name="targetedPortalCellCoord">The targeted portal cell coordinates.</param>
    /// <param name="sortedPortalCells">The sorted list of portal cells.</param>
    /// <returns>The next nearest portal cell id. Returns -1 if not found.</returns>
    public static int GetNextNearestPortalWhenTargetedPortalCellIsNotContained(Point targetedPortalCellCoord,
                                                                               IList<int> sortedPortalCells)
    {
        int currentPortalCell;
        if (sortedPortalCells.Count < 2)
        {
            return -1;
        }

        var previousPortalCell = sortedPortalCells[^1];

        for (var index = 0; index < sortedPortalCells.Count; previousPortalCell = currentPortalCell)
        {
            currentPortalCell = sortedPortalCells[index];
            index++;

            switch (MathUtils.CompareAngles(targetedPortalCellCoord,
                                            MapTools.GetCellCoordById(previousPortalCell)!.Value,
                                            MapTools.GetCellCoordById(currentPortalCell)!.Value))
            {
                case CompareAngle.Unlike:
                    if (sortedPortalCells.Count <= 2)
                    {
                        return -1;
                    }

                    continue;
                case CompareAngle.Counterclockwise:
                    return previousPortalCell;
            }
        }

        return -1;
    }

    /// <summary>
    /// Get the next nearest portal cell when the targeted portal cell is contained.
    /// </summary>
    /// <param name="targetedPortalCellCoord">The targeted portal cell coordinates.</param>
    /// <param name="sortedPortalCells">The sorted list of portal cells.</param>
    /// <returns>The next nearest portal cell id.</returns>
    public static int GetNextNearestPortalCellWhenTargetedPortalCellIsContained(
        Point targetedPortalCellCoord, IList<int> sortedPortalCells)
    {
        return sortedPortalCells[0];
    }

    /// <summary>
    /// Retrieves the portal chain from a list of portals based on a starting portal.
    /// </summary>
    /// <param name="startPortal">The starting portal to find the portal chain from.</param>
    /// <param name="portals">The list of portals to consider in the chain.</param>
    /// <returns>Returns an array containing the portal chain.</returns>
    public static List<Mark> GetPortalChainFromPortals(Mark startPortal, IList<Mark> portals)
    {
        var startingCell    = startPortal.MainCell;
        var cellToPortalMap = new Dictionary<int, Mark>();
        var portalCells     = new List<int>();

        foreach (var portal in portals)
        {
            cellToPortalMap[portal.MainCell] = portal;
            portalCells.Add(portal.MainCell);
        }

        var portalChainCells = GetPortalChainFromPortalCells(startingCell, portalCells);

        return portalChainCells.Select(cell => cellToPortalMap[cell]).ToList();
    }

    /// <summary>
    /// Retrieves the portal chain from a list of portal cells based on a starting portal cell.
    /// </summary>
    /// <param name="startCell">The starting cell to find the portal chain from.</param>
    /// <param name="portalCells">The list of portal cells to consider in the chain.</param>
    /// <param name="isOrdered">Optional parameter indicating if the chain should be ordered. Default is false.</param>
    /// <returns>Returns a list containing the portal cell chain.</returns>
    public static List<int> GetPortalChainFromPortalCells(int startCell, IList<int> portalCells, bool isOrdered = false)
    {
        var portalChain = new List<int>();
        var currentCell = startCell;
        portalCells = new List<int>(portalCells);
        var totalCells = portalCells.Contains(currentCell) ? portalCells.Count : portalCells.Count + 1;

        for (var i = 0; i < totalCells; i++)
        {
            portalChain.Add(currentCell);
            portalCells.Remove(currentCell);
            var nearestCell = GetNearestPortalCell(currentCell, portalCells);

            if (nearestCell == -1)
            {
                break;
            }

            currentCell = nearestCell;
        }

        if (portalChain.Count < 2)
        {
            return new List<int>();
        }

        portalChain.Remove(startCell);

        if (isOrdered)
        {
            portalChain.Reverse();
        }

        return portalChain;
    }
}