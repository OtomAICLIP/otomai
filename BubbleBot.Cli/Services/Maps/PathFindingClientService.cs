using System.Drawing;
using System.Runtime.InteropServices;
using Bubble.Core.Collections;
using Bubble.Core.Services;
using BubbleBot.Cli.Repository.Maps;
using BubbleBot.Cli.Services.Fight;
using BubbleBot.Cli.Utils;

namespace BubbleBot.Cli.Services.Maps;



public class PathFindingClientService : Singleton<PathFindingClientService>
{
    private const int SearchLimit = 500;
    private const int EstimateHeuristic = 1;

    private static readonly Direction[] Directions =
    [
        Direction.SouthWest,
        Direction.NorthWest,
        Direction.NorthEast,
        Direction.SouthEast,
        Direction.South,
        Direction.West,
        Direction.North,
        Direction.East
    ];

    public ClientMovementPath FindClientPath(MapData  map, short startCellId, short endCellId, bool allowDiag = true,
                                             bool allowThroughEntity = true, bool avoidObstacles = true)
    {
        if (startCellId == endCellId)
            return new ClientMovementPath();

        var distanceToEnd = MapTools.GetDistance(startCellId, endCellId);
        var start = MapTools.GetCellCoordById(startCellId)!;
        var endX = MapTools.GetCellIdXCoord(endCellId);
        var endY = MapTools.GetCellIdYCoord(endCellId);
        short endCellAuxId = -1;

        var costOfCell = new double[MapTools.MapCountCell];
        var openListWeights = new double[MapTools.MapCountCell];
        var parentOfCell = new int[MapTools.MapCountCell];
        var isCellClosed = new bool[MapTools.MapCountCell];
        var isEntityOnCell = new bool[MapTools.MapCountCell];
        var openList = new List<int>(40);

        for (var cellId = 0; cellId < MapTools.MapCountCell; cellId++)
        {
            parentOfCell[cellId] = -1;
            isCellClosed[cellId] = false;
            isEntityOnCell[cellId] = false;
        }

        openList.Clear();
        costOfCell[startCellId] = 0;
        openList.Add(startCellId);

        while (openList.Count > 0 && isCellClosed[endCellId] == false)
        {
            var minimum = 99999999d;
            var smallestCostIndex = 0;

            for (var i = 0; i < openList.Count; i++)
            {
                var cost = openListWeights[openList[i]];
                if (cost <= minimum)
                {
                    minimum = cost;
                    smallestCostIndex = i;
                }
            }

            var parentId = openList[smallestCostIndex];
            var parentX = MapTools.GetCellIdXCoord(parentId);
            var parentY = MapTools.GetCellIdYCoord(parentId);

            openList.RemoveAt(smallestCostIndex);
            isCellClosed[parentId] = true;
            var y = parentY - 1;

            while (y <= parentY + 1)
            {
                var x = parentX - 1;
                while (x <= parentX + 1)
                {
                    var cellId = MapTools.GetCellIdByCoord(x, y);
                    if (cellId != MapTools.InvalidCellId &&
                        isCellClosed[cellId] == false &&
                        cellId != parentId &&
                        map.PointMov(false, x, y, allowThroughEntity, parentId) &&
                        (y == parentY ||
                         x == parentX ||
                         allowDiag &&
                         (map.PointMov(false, parentX, y, allowThroughEntity, parentId) ||
                          map.PointMov(false, x, parentY, allowThroughEntity, parentId))))
                    {
                        double pointWeight;

                        if (cellId == endCellId)
                        {
                            pointWeight = 1;
                        }
                        else
                        {
                            var speed = map.Cells[(short)cellId].Speed;

                            if (allowThroughEntity)
                            {
                                if (isEntityOnCell[cellId])
                                {
                                    pointWeight = 20;
                                }
                                else
                                {
                                    if (speed >= 0)
                                    {
                                        pointWeight = 6 - speed;
                                    }
                                    else
                                    {
                                        pointWeight = 12 + Math.Abs(speed);
                                    }
                                }
                            }
                            else
                            {
                                pointWeight = 1;

                                if (isEntityOnCell[cellId])
                                {
                                    pointWeight += 0.3;
                                }

                                if (MapTools.IsValidCoord(x + 1, y) &&
                                    isEntityOnCell[MapTools.GetCellIdByCoord(x + 1, y)])
                                {
                                    pointWeight += 0.3;
                                }

                                if (MapTools.IsValidCoord(x, y + 1) &&
                                    isEntityOnCell[MapTools.GetCellIdByCoord(x, y + 1)])
                                {
                                    pointWeight += 0.3;
                                }

                                if (MapTools.IsValidCoord(x - 1, y) &&
                                    isEntityOnCell[MapTools.GetCellIdByCoord(x - 1, y)])
                                {
                                    pointWeight += 0.3;
                                }

                                if (MapTools.IsValidCoord(x, y - 1) &&
                                    isEntityOnCell[MapTools.GetCellIdByCoord(x, y - 1)])
                                {
                                    pointWeight += 0.3;
                                }
                            }
                        }

                        var movementCost = costOfCell[parentId] +
                                           (y == parentY || x == parentX ? 10 : 15) * pointWeight;

                        if (allowThroughEntity)
                        {
                            var cellOnEndColumn = x + y == endX + endY;
                            var cellOnStartColumn = x + y == start.Value.X + start.Value.Y;
                            var cellOnEndLine = x - y == endX - endY;
                            var cellOnStartLine = x - y == start.Value.X - start.Value.Y;

                            if (!cellOnEndColumn && !cellOnEndLine || !cellOnStartColumn && !cellOnStartLine)
                            {
                                movementCost += MapTools.GetDistance(cellId, endCellId);
                                movementCost += MapTools.GetDistance(cellId, startCellId);
                            }

                            if (x == endX || y == endY)
                            {
                                movementCost -= 3;
                            }

                            if (cellOnEndColumn ||
                                cellOnEndLine ||
                                x + y == parentX + parentY ||
                                x - y == parentX - parentY)
                            {
                                movementCost -= 2;
                            }

                            if (x == start.Value.X || y == start.Value.Y)
                            {
                                movementCost -= 3;
                            }

                            if (cellOnStartColumn || cellOnStartLine)
                            {
                                movementCost -= 2;
                            }

                            var distanceTmpToEnd = MapTools.GetDistance(cellId, endCellId);
                            if (distanceTmpToEnd < distanceToEnd)
                            {
                                endCellAuxId = (short)cellId;
                                distanceToEnd = distanceTmpToEnd;
                            }
                        }

                        if (parentOfCell[cellId] == MapTools.InvalidCellId || movementCost < costOfCell[cellId])
                        {
                            parentOfCell[cellId] = parentId;
                            costOfCell[cellId] = movementCost;
                            var heuristic = 10 * Math.Sqrt((endY - y) * (endY - y) + (endX - x) * (endX - x));
                            openListWeights[cellId] = heuristic + movementCost;

                            if (!openList.Contains(cellId))
                            {
                                openList.Add(cellId);
                            }
                        }
                    }

                    x++;
                }

                y++;
            }
        }

        var movPath = new ClientMovementPath
        {
            Start = map.GetCell(startCellId)!,
        };

        if (parentOfCell[endCellId] == MapTools.InvalidCellId)
        {
            endCellId = endCellAuxId;
        }
        
        if(endCellId == MapTools.InvalidCellId)
            return new ClientMovementPath();

        movPath.End = map.GetCell(endCellId)!;

        var cursor = endCellId;

        while (cursor != startCellId)
        {
            if (allowDiag)
            {
                var parent = parentOfCell[cursor];
                var grandParent = parent == MapTools.InvalidCellId ? MapTools.InvalidCellId : parentOfCell[parent];
                var grandGrandParent = grandParent == MapTools.InvalidCellId
                    ? MapTools.InvalidCellId
                    : parentOfCell[grandParent];

                var kX = MapPoint.GetPoint(cursor)!.X;
                var kY = MapPoint.GetPoint(cursor)!.Y;

                if (grandParent != MapTools.InvalidCellId && MapTools.GetDistance(cursor, grandParent) == 1)
                {
                    if (map.PointMov(false, kX, kY, allowThroughEntity, grandParent))
                    {
                        parentOfCell[cursor] = grandParent;
                    }
                }
                else
                {
                    if (grandGrandParent != MapTools.InvalidCellId &&
                        MapTools.GetDistance(cursor, grandGrandParent) == 2)
                    {
                        var nextX = MapPoint.GetPoint(grandGrandParent)!.X;
                        var nextY = MapPoint.GetPoint(grandGrandParent)!.Y;
                        var interX = kX + MathUtils.Round((nextX - kX) / 2d);
                        var interY = kY + MathUtils.Round((nextY - kY) / 2d);

                        if (map.PointMov(false, interX, interY, allowThroughEntity, cursor) &&
                            map.PointWeight(interX, interY) < 2)
                        {
                            parentOfCell[cursor] = MapTools.GetCellIdByCoord(interX, interY);
                        }
                    }
                    else
                    {
                        if (grandParent != MapTools.InvalidCellId && MapTools.GetDistance(cursor, grandParent) == 2)
                        {
                            var nextX = MapTools.GetCellIdXCoord(grandParent);
                            var nextY = MapTools.GetCellIdYCoord(grandParent);
                            var interX = MapTools.GetCellIdXCoord(parent);
                            var interY = MapTools.GetCellIdYCoord(parent);

                            if (kX + kY == nextX + nextY &&
                                kX - kY != interX - interY &&
                                !map.IsChangeZone(MapTools.GetCellIdByCoord(kX, kY),
                                                  MapTools.GetCellIdByCoord(interX, interY)) &&
                                !map.IsChangeZone(MapTools.GetCellIdByCoord(interX, interY),
                                                  MapTools.GetCellIdByCoord(nextX, nextY)))
                            {
                                parentOfCell[cursor] = grandParent;
                            }
                            else if (kX - kY == nextX - nextY &&
                                     kX - kY != interX - interY &&
                                     !map.IsChangeZone(MapTools.GetCellIdByCoord(kX, kY),
                                                       MapTools.GetCellIdByCoord(interX, interY)) &&
                                     !map.IsChangeZone(MapTools.GetCellIdByCoord(interX, interY),
                                                       MapTools.GetCellIdByCoord(nextX, nextY)))
                            {
                                parentOfCell[cursor] = grandParent;
                            }
                            else if (kX == nextX &&
                                     kX != interX &&
                                     map.PointWeight(kX, interY) < 2 &&
                                     map.PointMov(false, kX, interY, allowThroughEntity, cursor))
                            {
                                parentOfCell[cursor] = MapTools.GetCellIdByCoord(kX, interY);
                            }
                            else if (kY == nextY &&
                                     kY != interY &&
                                     map.PointWeight(interX, kY) < 2 &&
                                     map.PointMov(false, interX, kY, allowThroughEntity, cursor))

                            {
                                parentOfCell[cursor] = MapTools.GetCellIdByCoord(interX, kY);
                            }
                        }
                    }
                }
            }

            movPath.AddPoint(new PathElement(MapPoint.GetPoint(parentOfCell[cursor])!,
                                             (uint)MapTools.GetLookDirection8Exact(parentOfCell[cursor], cursor)));
            cursor = (short)parentOfCell[cursor];
        }

        movPath.Path.Reverse();
        return movPath;
    }
    
    public ClientMovementPath FindClientPathInFight(MapData  map, short startCellId, short endCellId, long fighterId, FightInfo fightInfo)
    {
        if (startCellId == endCellId)
            return new ClientMovementPath();

        var allowDiag = false;
        var allowThroughEntity = false;

        var distanceToEnd = MapTools.GetDistance(startCellId, endCellId);
        var start = MapTools.GetCellCoordById(startCellId)!;
        var endX = MapTools.GetCellIdXCoord(endCellId);
        var endY = MapTools.GetCellIdYCoord(endCellId);
        short endCellAuxId = -1;

        var costOfCell = new double[MapTools.MapCountCell];
        var openListWeights = new double[MapTools.MapCountCell];
        var parentOfCell = new int[MapTools.MapCountCell];
        var isCellClosed = new bool[MapTools.MapCountCell];
        var isEntityOnCell = new bool[MapTools.MapCountCell];
        var openList = new List<int>(40);

        for (var cellId = 0; cellId < MapTools.MapCountCell; cellId++)
        {
            parentOfCell[cellId] = -1;
            isCellClosed[cellId] = false;
            isEntityOnCell[cellId] = false;
        }

        openList.Clear();
        costOfCell[startCellId] = 0;
        openList.Add(startCellId);

        while (openList.Count > 0 && isCellClosed[endCellId] == false)
        {
            var minimum = 99999999d;
            var smallestCostIndex = 0;

            for (var i = 0; i < openList.Count; i++)
            {
                var cost = openListWeights[openList[i]];
                if (cost <= minimum)
                {
                    minimum = cost;
                    smallestCostIndex = i;
                }
            }

            var parentId = openList[smallestCostIndex];
            var parentX = MapTools.GetCellIdXCoord(parentId);
            var parentY = MapTools.GetCellIdYCoord(parentId);

            openList.RemoveAt(smallestCostIndex);
            isCellClosed[parentId] = true;
            var y = parentY - 1;

            while (y <= parentY + 1)
            {
                var x = parentX - 1;
                while (x <= parentX + 1)
                {
                    var cellId = MapTools.GetCellIdByCoord(x, y);
                    if (cellId != MapTools.InvalidCellId &&
                        isCellClosed[cellId] == false &&
                        cellId != parentId &&
                        map.PointMov(false, x, y, fightInfo, fighterId, allowThroughEntity, parentId) &&
                        (y == parentY ||
                         x == parentX ||
                         allowDiag &&
                         (map.PointMov(false, parentX, y, fightInfo, fighterId, allowThroughEntity, parentId) ||
                          map.PointMov(false, x, parentY, fightInfo, fighterId, allowThroughEntity, parentId))))
                    {
                        double pointWeight;

                        if (cellId == endCellId)
                        {
                            pointWeight = 1;
                        }
                        else
                        {
                            var speed = map.Cells[(short)cellId].Speed;

                            if (allowThroughEntity)
                            {
                                if (isEntityOnCell[cellId])
                                {
                                    pointWeight = 20;
                                }
                                else
                                {
                                    if (speed >= 0)
                                    {
                                        pointWeight = 6 - speed;
                                    }
                                    else
                                    {
                                        pointWeight = 12 + Math.Abs(speed);
                                    }
                                }
                            }
                            else
                            {
                                pointWeight = 1;

                                if (isEntityOnCell[cellId])
                                {
                                    pointWeight += 0.3;
                                }

                                if (MapTools.IsValidCoord(x + 1, y) &&
                                    isEntityOnCell[MapTools.GetCellIdByCoord(x + 1, y)])
                                {
                                    pointWeight += 0.3;
                                }

                                if (MapTools.IsValidCoord(x, y + 1) &&
                                    isEntityOnCell[MapTools.GetCellIdByCoord(x, y + 1)])
                                {
                                    pointWeight += 0.3;
                                }

                                if (MapTools.IsValidCoord(x - 1, y) &&
                                    isEntityOnCell[MapTools.GetCellIdByCoord(x - 1, y)])
                                {
                                    pointWeight += 0.3;
                                }

                                if (MapTools.IsValidCoord(x, y - 1) &&
                                    isEntityOnCell[MapTools.GetCellIdByCoord(x, y - 1)])
                                {
                                    pointWeight += 0.3;
                                }
                            }
                        }

                        var movementCost = costOfCell[parentId] +
                                           (y == parentY || x == parentX ? 10 : 15) * pointWeight;

                        if (allowThroughEntity)
                        {
                            var cellOnEndColumn = x + y == endX + endY;
                            var cellOnStartColumn = x + y == start.Value.X + start.Value.Y;
                            var cellOnEndLine = x - y == endX - endY;
                            var cellOnStartLine = x - y == start.Value.X - start.Value.Y;

                            if (!cellOnEndColumn && !cellOnEndLine || !cellOnStartColumn && !cellOnStartLine)
                            {
                                movementCost += MapTools.GetDistance(cellId, endCellId);
                                movementCost += MapTools.GetDistance(cellId, startCellId);
                            }

                            if (x == endX || y == endY)
                            {
                                movementCost -= 3;
                            }

                            if (cellOnEndColumn ||
                                cellOnEndLine ||
                                x + y == parentX + parentY ||
                                x - y == parentX - parentY)
                            {
                                movementCost -= 2;
                            }

                            if (x == start.Value.X || y == start.Value.Y)
                            {
                                movementCost -= 3;
                            }

                            if (cellOnStartColumn || cellOnStartLine)
                            {
                                movementCost -= 2;
                            }

                            var distanceTmpToEnd = MapTools.GetDistance(cellId, endCellId);
                            if (distanceTmpToEnd < distanceToEnd)
                            {
                                endCellAuxId = (short)cellId;
                                distanceToEnd = distanceTmpToEnd;
                            }
                        }

                        if (parentOfCell[cellId] == MapTools.InvalidCellId || movementCost < costOfCell[cellId])
                        {
                            parentOfCell[cellId] = parentId;
                            costOfCell[cellId] = movementCost;
                            var heuristic = 10 * Math.Sqrt((endY - y) * (endY - y) + (endX - x) * (endX - x));
                            openListWeights[cellId] = heuristic + movementCost;

                            if (!openList.Contains(cellId))
                            {
                                openList.Add(cellId);
                            }
                        }
                    }

                    x++;
                }

                y++;
            }
        }

        var movPath = new ClientMovementPath
        {
            Start = map.GetCell(startCellId)!,
        };

        if (parentOfCell[endCellId] == MapTools.InvalidCellId)
        {
            endCellId = endCellAuxId;
        }
        
        if(endCellId == MapTools.InvalidCellId)
            return new ClientMovementPath();

        movPath.End = map.GetCell(endCellId)!;

        var cursor = endCellId;

        while (cursor != startCellId)
        {
            if (allowDiag)
            {
                var parent = parentOfCell[cursor];
                var grandParent = parent == MapTools.InvalidCellId ? MapTools.InvalidCellId : parentOfCell[parent];
                var grandGrandParent = grandParent == MapTools.InvalidCellId
                    ? MapTools.InvalidCellId
                    : parentOfCell[grandParent];

                var kX = MapPoint.GetPoint(cursor)!.X;
                var kY = MapPoint.GetPoint(cursor)!.Y;

                if (grandParent != MapTools.InvalidCellId && MapTools.GetDistance(cursor, grandParent) == 1)
                {
                    if (map.PointMov(false, kX, kY, allowThroughEntity, grandParent))
                    {
                        parentOfCell[cursor] = grandParent;
                    }
                }
                else
                {
                    if (grandGrandParent != MapTools.InvalidCellId &&
                        MapTools.GetDistance(cursor, grandGrandParent) == 2)
                    {
                        var nextX = MapPoint.GetPoint(grandGrandParent)!.X;
                        var nextY = MapPoint.GetPoint(grandGrandParent)!.Y;
                        var interX = kX + MathUtils.Round((nextX - kX) / 2d);
                        var interY = kY + MathUtils.Round((nextY - kY) / 2d);

                        if (map.PointMov(false, interX, interY, allowThroughEntity, cursor) &&
                            map.PointWeight(interX, interY) < 2)
                        {
                            parentOfCell[cursor] = MapTools.GetCellIdByCoord(interX, interY);
                        }
                    }
                    else
                    {
                        if (grandParent != MapTools.InvalidCellId && MapTools.GetDistance(cursor, grandParent) == 2)
                        {
                            var nextX = MapTools.GetCellIdXCoord(grandParent);
                            var nextY = MapTools.GetCellIdYCoord(grandParent);
                            var interX = MapTools.GetCellIdXCoord(parent);
                            var interY = MapTools.GetCellIdYCoord(parent);

                            if (kX + kY == nextX + nextY &&
                                kX - kY != interX - interY &&
                                !map.IsChangeZone(MapTools.GetCellIdByCoord(kX, kY),
                                                  MapTools.GetCellIdByCoord(interX, interY)) &&
                                !map.IsChangeZone(MapTools.GetCellIdByCoord(interX, interY),
                                                  MapTools.GetCellIdByCoord(nextX, nextY)))
                            {
                                parentOfCell[cursor] = grandParent;
                            }
                            else if (kX - kY == nextX - nextY &&
                                     kX - kY != interX - interY &&
                                     !map.IsChangeZone(MapTools.GetCellIdByCoord(kX, kY),
                                                       MapTools.GetCellIdByCoord(interX, interY)) &&
                                     !map.IsChangeZone(MapTools.GetCellIdByCoord(interX, interY),
                                                       MapTools.GetCellIdByCoord(nextX, nextY)))
                            {
                                parentOfCell[cursor] = grandParent;
                            }
                            else if (kX == nextX &&
                                     kX != interX &&
                                     map.PointWeight(kX, interY) < 2 &&
                                     map.PointMov(false, kX, interY, allowThroughEntity, cursor))
                            {
                                parentOfCell[cursor] = MapTools.GetCellIdByCoord(kX, interY);
                            }
                            else if (kY == nextY &&
                                     kY != interY &&
                                     map.PointWeight(interX, kY) < 2 &&
                                     map.PointMov(false, interX, kY, allowThroughEntity, cursor))

                            {
                                parentOfCell[cursor] = MapTools.GetCellIdByCoord(interX, kY);
                            }
                        }
                    }
                }
            }

            movPath.AddPoint(new PathElement(MapPoint.GetPoint(parentOfCell[cursor])!,
                                             (uint)MapTools.GetLookDirection8Exact(parentOfCell[cursor], cursor)));
            cursor = (short)parentOfCell[cursor];
        }

        movPath.Path.Reverse();
        return movPath;
    }


    public MovementPath FindPath(MapData map, short startCell, short endCell, bool diagonal, int movementPoints = -1,
                                 FightInfo? fight = null)
    {
        return FindPath(map, MapPoint.GetPoint(startCell)!, MapPoint.GetPoint(endCell)!, diagonal, movementPoints, fight);
    }

    public MovementPath FindPath(MapData    map,
                                 MapPoint   startPoint,
                                 MapPoint   endPoint,
                                 bool       diagonal,
                                 int        movementPoints = -1,
                                 FightInfo? fight          = null)
    {
        var success = false;

        var matrix = new PathNode[MapPoint.MapSize + 1];
        var openList = new PriorityQueueB<short>(new ComparePfNodeMatrix(matrix));
        var closedList = new List<PathNode>();

        var location = startPoint.CellId;

        var counter = 0;

        if (movementPoints == 0)
        {
            return MovementPath.GetEmptyPath(map, map.Cells[startPoint.CellId]);
        }

        matrix[location].Cell = location;
        matrix[location].Parent = -1;
        matrix[location].G = 0;
        matrix[location].F = EstimateHeuristic;
        matrix[location].Status = NodeState.Open;

        openList.Push((short)location);
        while (openList.Count > 0)
        {
            location = openList.Pop();
            var locationPoint = new MapPoint((short)location);

            if (matrix[location].Status == NodeState.Closed)
            {
                continue;
            }

            if (location == endPoint.CellId)
            {
                matrix[location].Status = NodeState.Closed;
                success = true;
                break;
            }

            if (counter > SearchLimit)
            {
                return MovementPath.GetEmptyPath(map, map.Cells[startPoint.CellId]);
            }

            for (var i = 0; i < (diagonal ? 8 : 4); i++)
            {
                var newLocationPoint = locationPoint.GetNearestCellInDirection(Directions[i]);

                if (newLocationPoint == null)
                {
                    continue;
                }

                var newLocation = newLocationPoint.CellId;

                if (newLocation < 0 || newLocation >= MapPoint.MapSize)
                {
                    continue;
                }

                if (!MapPoint.IsInMap(newLocationPoint.X, newLocationPoint.Y))
                {
                    continue;
                }

                if (fight != null)
                {
                    if (!map.IsCellWalkableFight(newLocation))
                    {
                        continue;
                    }
                }
                else
                {
                    if (!map.IsCellWalkable(newLocation))
                    {
                        continue;
                    }
                }

                if (fight != null && !fight.IsCellFree(newLocation) && newLocation != endPoint.CellId)
                {
                    continue;
                }
                
                var newG = matrix[location].G + 1;

                if (matrix[newLocation].Status is NodeState.Open or NodeState.Closed && matrix[newLocation].G <= newG)
                {
                    continue;
                }

                matrix[newLocation].Cell = newLocation;
                matrix[newLocation].Parent = (short)location;
                matrix[newLocation].G = newG;
                matrix[newLocation].H = GetHeuristic(newLocationPoint, endPoint);
                matrix[newLocation].F = newG + matrix[newLocation].H;

                openList.Push((short)newLocation);
                matrix[newLocation].Status = NodeState.Open;
            }

            counter++;
            matrix[location].Status = NodeState.Closed;
        }

        if (success)
        {
            var node = matrix[endPoint.CellId];

            while (node.Parent != -1)
            {
                closedList.Add(node);
                node = matrix[node.Parent];
            }

            closedList.Add(node);
        }

        closedList.Reverse();

        if (movementPoints > 0 && closedList.Count + 1 > movementPoints)
        {
            return new(map, closedList.Take(movementPoints + 1).Select(entry => map.Cells[entry.Cell]));
        }

        return new(map, closedList.Select(entry => map.Cells[entry.Cell]));
    }

    private static double GetHeuristic(MapPoint pointA, MapPoint pointB)
    {
        var dxy = new Point(Math.Abs(pointB.X - pointA.X), Math.Abs(pointB.Y - pointA.Y));
        var orthogonalValue = Math.Abs(dxy.X - dxy.Y);
        var diagonalValue = Math.Abs((dxy.X + dxy.Y - orthogonalValue) / 2);

        return EstimateHeuristic * (diagonalValue + orthogonalValue + dxy.X + dxy.Y);
    }

    #region Nested type: ComparePfNodeMatrix

    private class ComparePfNodeMatrix : IComparer<short>
    {
        private readonly PathNode[] _matrix;

        public ComparePfNodeMatrix(PathNode[] matrix) => _matrix = matrix;

        #region IComparer<ushort> Members

        public int Compare(short a, short b)
        {
            if (_matrix[a].F > _matrix[b].F)
            {
                return 1;
            }

            if (_matrix[a].F < _matrix[b].F)
            {
                return -1;
            }

            return 0;
        }

        #endregion
    }

    #endregion

    public MapPoint[] FindReachableCells(FightInfo fight, short from, int distance)
    {
        var result   = new List<MapPoint>();
        var matrix   = new PathNode[MapPoint.MapSize + 1];
        var openList = new PriorityQueueB<short>(new ComparePfNodeMatrix(matrix));
        var location = from;
        var counter  = 0;

        if (distance == 0)
        {
            return new[] { new MapPoint(from), };
        }

        matrix[location].Cell   = location;
        matrix[location].Parent = -1;
        matrix[location].G      = 0;
        matrix[location].F      = 0;
        matrix[location].Status = NodeState.Open;

        openList.Push(location);
        
        while (openList.Count > 0)
        {
            location = openList.Pop();
            var locationPoint = new MapPoint(location);

            if (matrix[location].Status == NodeState.Closed)
            {
                continue;
            }

            if (counter > SearchLimit)
            {
                break;
            }
            
            var isLeft = BotGameClient.IsLeftCol(location);
            var isRight = BotGameClient.IsRightCol(location);
            var isTop = BotGameClient.IsTopRow(location);
            var isBottom = BotGameClient.IsBottomRow(location);

            for (var i = 0; i < 4; i++)
            {
                if (isLeft && (Directions[i] == Direction.NorthWest || 
                               Directions[i] == Direction.SouthWest || 
                               Directions[i] == Direction.West))
                {
                    continue;
                }
                
                if (isRight && (Directions[i] == Direction.NorthEast || 
                               Directions[i] == Direction.SouthEast || 
                               Directions[i] == Direction.East))
                {
                    continue;
                }
                
                if (isTop && (Directions[i] == Direction.North || 
                                Directions[i] == Direction.NorthEast || 
                                Directions[i] == Direction.NorthWest))
                {
                    continue;
                }
                
                if (isBottom && (Directions[i] == Direction.South || 
                                 Directions[i] == Direction.SouthEast || 
                                 Directions[i] == Direction.SouthWest))
                {
                    continue;
                }
                
                var newLocationPoint = locationPoint.GetNearestCellInDirection(Directions[i]);

                if (newLocationPoint == null)
                {
                    continue;
                }

                var newLocation = newLocationPoint.CellId;

                if (newLocation < 0 || newLocation >= MapPoint.MapSize)
                {
                    continue;
                }

                if (!MapPoint.IsInMap(newLocationPoint.X, newLocationPoint.Y))
                {
                    continue;
                }
                
                if (!fight.IsCellFree(newLocation))
                {
                    continue;
                }

                var newG = matrix[location].G + 1;

                if (matrix[newLocation].Status is NodeState.Open or NodeState.Closed &&
                    matrix[newLocation].G <= newG)
                {
                    continue;
                }

                matrix[newLocation].Cell   = newLocation;
                matrix[newLocation].Parent = location;
                matrix[newLocation].G      = newG;
                matrix[newLocation].H      = 0;
                matrix[newLocation].F      = newG + matrix[newLocation].H;

                if (!(newG <= distance))
                {
                    continue;
                }

                result.Add(newLocationPoint);
                openList.Push((short)newLocation);
                matrix[newLocation].Status = NodeState.Open;
            }

            counter++;
            matrix[location].Status = NodeState.Closed;
        }

        result.Add(MapPoint.GetPoint(from)!);
        return result.ToArray();
    }


}

[StructLayout(LayoutKind.Auto)]
public struct PathNode
{
    public int Cell;
    public double F;
    public double G;
    public double H;
    public short Parent;
    public NodeState Status;
}

public enum NodeState : byte
{
    None,
    Open,
    Closed,
}