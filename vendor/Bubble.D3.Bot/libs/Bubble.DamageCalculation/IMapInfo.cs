using Bubble.DamageCalculation.Customs;
using Bubble.DamageCalculation.FighterManagement;
using Bubble.DamageCalculation.SpellManagement;

namespace Bubble.DamageCalculation;

public interface IMapInfo
{
    bool                          IsCellWalkable(int             cellId);
    int                           GetOutputPortal(Mark           mark,       out Mark?           usedPortal);
    IList<Mark>                   GetMarks(bool                  activeOnly, GameActionMarkType? markType = null, int?                teamId   = null);
    Mark?                         GetMarkOnCenter(int            cellId,     bool                activeOnly,      GameActionMarkType? markType = null);
    IList<Mark>                   GetMarkInteractingWithCell(int cellId,     bool                activeOnly,      GameActionMarkType? markType = null);
    public HaxeFighter?           GetLastKilledAlly(int          teamId);
    public List<FighterCellLight> GetFightersInitialPositions();
    public HaxeFighter?           GetFighterById(long fighterId);
    public IList<long>                GetEveryFighterId();
    public long GetCarriedFighterIdBy(HaxeFighter fighter);
    public void DispellIllusionOnCell(int cellId);
    void AddMark(Mark mark);
    int GetFreeId();
    int GetOutputPortalCell(int portalCellId);
    void ReactivePortalAfterMove(int cell);
    bool IsFightEnded();
    void ResetEffectCastCount();
    void IncrementEffectCastCount();
    int GetEffectCastCount();
    void RemoveDeadFighter(long fighterId);
}