using Bubble.Core.Datacenter.Datacenter.World;
using BubbleBot.Cli.Services.Fight;

namespace BubbleBot.Cli.Repository.Maps;


public class MapData
{
    public long BottomMapId { get; }
    public long LeftMapId { get; }
    public long RightMapId { get; }
    public long TopMapId { get; }
    public long TopMapIdServer { get; set; }
    public long BottomMapIdServer { get; set; }
    public long LeftMapIdServer { get; set; }
    public long RightMapIdServer { get; set; }

    public int PosX => _positions.PosX;
    public int PosY => _positions.PosY;
    public int WorldMap { get; set; }
    public int SubAreaId { get; set; }
    public bool HasPriorityOnWorldmap { get; set; }
    public bool Outdoor { get; set; }

    private readonly MapPositions _positions;
    public MapPositions Positions => _positions;

    public List<Cell> Cells { get; }
    public long Id { get; }

    private List<InteractiveElementData> _interactiveElements;
    private Dictionary<long, ActorPositionInformation> _actors = new();
    private readonly Cell[] _freeCells;

    public int InternalX { get;  }
    public int InternalY { get;  }

    public MapData(MapPositions                 positions,
               List<Cell>                   cells,
               List<InteractiveElementData> interactiveElements,
               long                         bottomMapId,
               long                         leftMapId,
               long                         rightMapId,
               long                         topMapId,
               long                         bottomMapIdServer,
               long                         leftMapIdServer,
               long                         rightMapIdServer,
               long                         topMapIdServer)
    {
        Id = positions.Id;
        BottomMapId = bottomMapId;
        LeftMapId = leftMapId;
        RightMapId = rightMapId;
        TopMapId = topMapId;
        
        BottomMapIdServer = bottomMapIdServer;
        LeftMapIdServer = leftMapIdServer;
        RightMapIdServer = rightMapIdServer;
        TopMapIdServer = topMapIdServer;

        _positions = positions;
        Cells = cells;
        _interactiveElements = interactiveElements;

        WorldMap = positions.WorldMap;
        SubAreaId = positions.SubAreaId;
        HasPriorityOnWorldmap = positions.HasPriorityOnWorldmap;
        Outdoor = positions.Outdoor;
        
        _freeCells = Cells
                          .Where(x => IsCellWalkable(x) && !x.FarmCell)
                          .OrderBy(x => MapPoint.Middle.ManhattanDistanceTo(MapPoint.Points[x.Id]))
                          .ToArray();

        InternalX = DecodeIdToX(Id);
        InternalY = DecodeIdToY(Id);
    }

    public bool CanUseHavenBag()
    {
        return Positions.CapabilityAllowTeleportFrom && Positions.CapabilityAllowTeleportTo;
    }
    
    public static bool CanUseHavenBag(MapPositions positions)
    {
        return positions.CapabilityAllowTeleportFrom && positions.CapabilityAllowTeleportTo;
    }
    /// <summary>
    /// Décodage de l'ID de map pour obtenir la position X interne dans le monde
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static int DecodeIdToX(long id)
    {
        var x = (id >> 9) & 0x0FF;
        if (((id >> 17) & 1) == 1)
            x *= -1;
        return
            (int)x; // For now, no way to go over an int
    }

    /// <summary>
    /// Décodage de l'ID de map pour obtenir la position Y interne dans le monde
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static int DecodeIdToY(long id)
    {
        var y = id & 0x0FF;
        if (((id >> 8) & 1) == 1)
            y *= -1;
        return (int)y; // For now, no way to go over an int
    }
    public bool PointMov(bool isInFight, int x, int y, bool allowThroughEntity = true, int previousCell = -1)
    {
        if (!MapPoint.IsInMap(x, y))
        {
            return false;
        }

        var cell = GetCell((short)MapPoint.GetPoint(x, y)!.CellId);

        var mov = cell!.Mov;

        if (isInFight && !cell.NonWalkableDuringFight)
        {
            mov = false;
        }

        if (mov && previousCell != -1 && previousCell != cell.Id)
        {
            var previousCellData = Cells[(short)previousCell];
            var dif = Math.Abs(Math.Abs(cell.Floor) - Math.Abs(previousCellData.Floor));

            if (previousCellData.MoveZone != cell.MoveZone && dif > 0 ||
                previousCellData.MoveZone == cell.MoveZone && cell.MoveZone == 0 && dif > 110)
            {
                mov = false;
            }
        }

        return mov;
    }
    public bool PointMov(bool isInFight, int x, int y, FightInfo fightInfo, long fighterId, bool allowThroughEntity = true, int previousCell = -1)
    {
        if (!MapPoint.IsInMap(x, y))
        {
            return false;
        }

        var cell = GetCell((short)MapPoint.GetPoint(x, y)!.CellId);

        var mov = cell!.Mov;

        if (isInFight && !cell.NonWalkableDuringFight)
        {
            mov = false;
        }

        if (mov && previousCell != -1 && previousCell != cell.Id)
        {
            var previousCellData = Cells[(short)previousCell];
            var dif = Math.Abs(Math.Abs(cell.Floor) - Math.Abs(previousCellData.Floor));

            if (previousCellData.MoveZone != cell.MoveZone && dif > 0 ||
                previousCellData.MoveZone == cell.MoveZone && cell.MoveZone == 0 && dif > 110)
            {
                mov = false;
            }
        }
        
        var fightCell = fightInfo.GetFighterAtCell(cell.Id);
        
        if (fightCell != null && fighterId != fightCell.Id)
        {
            mov = false;
        }

        return mov;
    }


    public bool PointMov(int x, int y)
    {
        if (!MapPoint.IsInMap(x, y))
        {
            return false;
        }

        var point = MapPoint.GetPoint(x, y)!;

        var cell = GetCell((short)point.CellId);

        return cell is { Mov: true, NonWalkableDuringFight: false, };
    }

    public Cell? GetCell(short cellId)
    {
        return !IsValidCellId(cellId) ? null : Cells[cellId];
    }
    public Cell? GetCell(int cellId)
    {
        return !IsValidCellId(cellId) ? null : Cells[cellId];
    }
    public Cell? GetCell(uint cellId)
    {
        return !IsValidCellId((int)cellId) ? null : Cells[(int)cellId];
    }
    public bool IsValidCellId(int cellId)
    {
        return cellId >= 0 && cellId < MapConstants.MapSize;
    }

    public double PointWeight(int x, int y, bool allowTroughEntity = true)
    {
        var cellId = MapTools.GetCellIdByCoord(x, y);

        if (!IsValidCellId(cellId))
        {
            return 0d;
        }

        var speed = Cells[(short)cellId].Speed;
        var weight = 0d;

        if (allowTroughEntity)
        {
            if (speed >= 0)
            {
                weight += 5 - speed;
            }
            else
            {
                weight += 11 + Math.Abs(speed);
            }

            /*var entityOnCell = GetActorOnCell(cellId);

            if (entityOnCell != null)
            {
                weight = 20;
            }*/
        }

        else
        {
            if (GetActorOnCell(cellId) != null)
            {
                weight += 0.3;
            }

            if (GetActorOnCell(MapTools.GetCellIdByCoord(x + 1, y)) != null)
            {
                weight += 0.3;
            }

            if (GetActorOnCell(MapTools.GetCellIdByCoord(x, y + 1)) != null)
            {
                weight += 0.3;
            }

            if (GetActorOnCell(MapTools.GetCellIdByCoord(x - 1, y)) != null)
            {
                weight += 0.3;
            }

            if (GetActorOnCell(MapTools.GetCellIdByCoord(x, y - 1)) != null)
            {
                weight += 0.3;
            }
        }

        return weight;
    }

    public ActorPositionInformation? GetActorOnCell(int cellId)
    {
        return _actors.Values.FirstOrDefault(a => a.Disposition.CellId == cellId);
    }

    public bool IsChangeZone(int cellId1, int cellId2)
    {
        var cellData1 = Cells[(short)cellId1];
        var cellData2 = Cells[(short)cellId2];

        var dif = Math.Abs(Math.Abs(cellData1.Floor) - Math.Abs(cellData2.Floor));
        return (cellData1.MoveZone != cellData2.MoveZone && dif == 0);
    }

    public bool IsCellWalkable(Cell cell)
    {
        return cell.Mov;
    }

    public bool IsCellWalkable(int cellId)
    {
        var cell = GetCell((short)cellId);
        return cell is { FarmCell: false, Mov: true, };
    }
    
    public bool IsCellWalkableFight(int cellId)
    {
        var cell = GetCell((short)cellId);
        return cell is { FarmCell: false, NonWalkableDuringFight: false, Mov: true };
    }

    public IList<Cell> GetFreeCells(bool actorFree = false)
    {
        if (!actorFree) return _freeCells;

        return _freeCells.Where(x => _actors.Any(y => y.Value.Disposition.CellId != x.Id)).ToArray();
    }

    public InteractiveElementData? GetInteractive(int elementUid)
    {
        return _interactiveElements.FirstOrDefault(x => x.InteractionId == elementUid);
    }
    


    public int GetFreeContiguousCell(int cellNum, bool movement)
    {
        if (Id == 212599303) // Péniche bonta
        {
            return 434;
        }

        if (Id == 159777807) // Péniche bonta (côté hors bonta)
        {
            return 121;
        }
        
        for (var i = Direction.SouthEast; i <= Direction.NorthEast; i += 1) // Dans le sens horaire
        {
            var cell = MapTools.GetNextCellByDirection(cellNum, (int)i);
            var cellData = GetCell((short)cell);
            
            if (cellData == null)
            {
                continue;
            }
            
            if (MapTools.IsValidCellId(cellNum) && (!movement || cellData.Mov))
            {
                return cell;
            }
        }
        
        return -1;
    }

    public int GetFreeContiguousCell(int cellNum, bool movement, int direction)
    {
        var cell = MapTools.GetNextCellByDirection(cellNum, direction);
        var cellData = GetCell((short)cell);

        if (cellData != null && MapTools.IsValidCellId(cellNum) && (!movement || cellData.Mov))
        {
            return cell;
        }

        for (var i = Direction.SouthEast; i <= Direction.NorthEast; i += 1) // Dans le sens horaire
        {
            cell = MapTools.GetNextCellByDirection(cellNum, (int)i);
            cellData = GetCell((short)cell);

            if (cellData == null)
            {
                continue;
            }

            if (MapTools.IsValidCellId(cellNum) && (!movement || cellData.Mov))
            {
                return cell;
            }
        }

        return -1;
    }
}