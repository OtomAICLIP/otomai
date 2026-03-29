using Bubble.DamageCalculation;
using Bubble.DamageCalculation.Customs;
using Bubble.DamageCalculation.FighterManagement;
using Bubble.DamageCalculation.SpellManagement;

namespace BubbleBot.Cli.Services.Fight;

public class FightSimulation : IMapInfo
{
    public FightInfo FightInfo { get; }
    public IList<Mark> Marks { get; }

    public FightSimulation(FightInfo fightInfo)
    {
        FightInfo = fightInfo;
        Marks = new List<Mark>();
    }
    
    public bool IsCellWalkable(int cellId)
    {
        return FightInfo.Map.Data.IsCellWalkable(cellId);
    }
    
    public int GetOutputPortal(Mark mark, out Mark? usedPortal)
    {
        usedPortal = null;
        return -1;
    }
    
    public IList<Mark> GetMarks(bool activeOnly, GameActionMarkType? markType = null, int? teamId = null)
    {
        return Marks.Where(x => !activeOnly || x.Active).ToList();
    }
    public Mark? GetMarkOnCenter(int cellId, bool activeOnly, GameActionMarkType? markType = null)
    {
        return Marks.FirstOrDefault(x => x.MainCell == cellId && (!activeOnly || x.Active));
    }
    public IList<Mark> GetMarkInteractingWithCell(int cellId, bool activeOnly, GameActionMarkType? markType = null)
    {
        return Marks.Where(x => x.Cells.Contains(cellId) && (!activeOnly || x.Active)).ToList();
    }
    public HaxeFighter? GetLastKilledAlly(int teamId)
    {
        return FightInfo.GetLastKilledAlly(teamId);
    }
    public List<FighterCellLight> GetFightersInitialPositions()
    {
        return FightInfo.GetFightersInitialPositions();
    }
    public HaxeFighter? GetFighterById(long fighterId)
    {
        return FightInfo.GetFighterById(fighterId);
    }
    public IList<long> GetEveryFighterId()
    {
        return FightInfo.GetEveryFighterId();
    }
    public long GetCarriedFighterIdBy(HaxeFighter fighter)
    {
        return FightInfo.GetCarriedFighterIdBy(fighter);
    }
    public void DispellIllusionOnCell(int cellId)
    {
        return;
    }
    public void AddMark(Mark mark)
    {
        return;
    }
    public int GetFreeId()
    {
        return FightInfo.GetFreeId();
    }
    
    public int GetOutputPortalCell(int portalCellId)
    {
        return FightInfo.GetOutputPortalCell(portalCellId);
    }
    public void ReactivePortalAfterMove(int cell)
    {
        return;
    }
    public bool IsFightEnded()
    {
        return FightInfo.IsFightEnded();
    }
    
    public int EffectCastCount { get; set; }
    
    public void ResetEffectCastCount()
    {
        EffectCastCount = 0;
    }

    public void IncrementEffectCastCount()
    {
        EffectCastCount++;
    }

    public int GetEffectCastCount()
    {
        return EffectCastCount;
    }

    public void RemoveDeadFighter(long fighterId)
    {
        return;
    }

    public void RemoveDeadFighter(int fighterId)
    {
        return;
    }

}