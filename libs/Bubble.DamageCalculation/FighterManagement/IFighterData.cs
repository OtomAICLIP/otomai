using Bubble.Core.Datacenter.Datacenter.Effects;
using Bubble.DamageCalculation.FighterManagement.FighterStats;

namespace Bubble.DamageCalculation.FighterManagement;

public interface IFighterData
{
    public bool UseSummonSlot();

    public int ResolveDodge();

    //public void ResetHaxeStats();
    public bool IsSummon();
    public bool IsInvisible { get; }
    bool FightStarted { get; }
    public int          GetUsedPm();
    public bool         HasGod();
    public int          GetTurnBeginPosition();
    public long         GetSummonerId();
    public IStatsField? GetStat(int statId);
    public int          GetStartedPositionCell();
    public int          GetPreviousPosition();
    public int          GetMaxHealthPoints();
    public int          GetMaxHealthPointsWithoutContext();
    public void         AddSpellModification(int           spellId, int modificationId, short value, ActionId actionId);
    public int          GetItemSpellDamageModification(int spellId);
    public int          GetHealthPoints();
    public int          GetCharacteristicValue(StatId characteristicId);
    public bool         CanBreedUsePortals();
    public bool         CanBreedSwitchPosOnTarget();
    public bool         CanBreedSwitchPos();
    public bool         CanBreedBePushed();
    public bool         CanBreedBeCarried();
    int                 RollApDodge(int apStolen, HaxeFighter source);
    int                 RollAmDodge(int mpStolen, HaxeFighter source);
    bool                IsPlaying();
    bool                UseSummonerTurnAndIsPlaying();
    void                OverrideId(int id);
    IFighterData        Clone();
    bool                IsAlive();
    bool                NobodyHasPlayed();
    long                 GetCurrentFighter();
    int                 GetDamageHealEquipmentSpellMod(int id, ActionId spellEffectActionId);
    int                 GetCurrentTurn();
    IEnumerable<int>    GetCellIdsInRange(int origin, int range, int minRange);
    int                 GetUsedPa();
    void                ResetStats();
    void                ResetCurLife();
    int                 GetPermanentDamage();
    void                SetPermanentDamage(int curPermDamage);
    void                SetZombieLife();
    void                SetHalfLife();
    void                SetSummoner(long casterId);
    bool                HasPlayedThisTurn();
    int                 GetFightStartPosition();
    int[]               GetSummonIds();
}