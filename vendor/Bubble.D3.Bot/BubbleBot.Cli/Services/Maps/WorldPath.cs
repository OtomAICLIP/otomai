using Serilog;

namespace BubbleBot.Cli.Services.Maps;

public class WorldPath
{
    private long _wantToGoOnMapId;
    
    public int WantToGoOnCellId { get; set; }

    public long WantToGoOnMapId
    {
        get => _wantToGoOnMapId;
        set
        {
            _wantToGoOnMapId = value;
            WantToGoOnMapRealId = value;
        }
    }
    public long WantToGoOnMapRealId { get; set; }
    public bool ChangeMapAfterOnWantedCell { get; set; }
    public int WantToUseInteractiveId { get; set; }
    public int WantToUseInteractiveSkillId { get; set; }
    public long WantToAttackMonster { get; set; }

    public void Reset()
    {
        WantToGoOnCellId = -1;
        WantToGoOnMapId = -1;
        WantToGoOnMapRealId = -1;
        ChangeMapAfterOnWantedCell = false;
        WantToUseInteractiveId = -1;
        WantToUseInteractiveSkillId = -1;
        WantToAttackMonster = 0;
    }
}