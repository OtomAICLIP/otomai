using BubbleBot.Cli.Services.Maps;

namespace BubbleBot.Cli.Services.Fight;

public class AiCellResult
{
    public required int FromCellId { get; set; }
    public required int CellId { get; set; }
    public int InputPortalCellId { get; set; } = -1;

    public bool UsingPortal => InputPortalCellId != -1;

    public required int MpCost { get; set; }
    public required int ApCost { get; set; }
    public required SpellWrapper Spell { get; set; }
    public double Score { get; set; }
    public double DamageOnEnemies { get; set; }
    public double DamageOnAllies { get; set; }
    public int RemainingAp { get; set; }

    public bool CantBeUsed { get; set; }

    public int TargetedCell => InputPortalCellId != -1 ? InputPortalCellId : CellId;

    public AiMovementCellResult? MovementResult { get; set; }
    public int RemainingMp { get; set; }
}

public struct AiMovementCellResult
{
    public AiMovementCellResult(FightInfo fight, short toCellId, MovementPath movementPath, double score)
    {
        Fight   = fight;
        ToCellId     = toCellId;
        MovementPath = movementPath;
        Score        = score;
    }
    
    public required short FromCellId { get; set; }
    public required FightInfo Fight { get; set; } 
    public required short ToCellId { get; set; }
    public int InputPortalCellId { get; set; } = -1;
    public required MovementPath? MovementPath { get; set; }
    public double Score { get; set; }
    public double MalusScore { get; set; }
    
    public MovementPath GetMovementPath()
    {
        if (MovementPath != null && MovementPath.Cells.Length > 0)
        {
            return MovementPath;
        }
        
        return MovementPath = PathFindingClientService.Instance.FindPath(Fight.Map.Data, 
                                                                   FromCellId, 
                                                                   ToCellId,
                                                                   false, 
                                                                   -1,
                                                                   Fight);
    }
    
}