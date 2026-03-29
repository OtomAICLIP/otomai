using System.Diagnostics.CodeAnalysis;
using Bubble.Core.Datacenter.Datacenter.Effects;
using Bubble.Core.Datacenter.Datacenter.Monster;
using Bubble.DamageCalculation;
using Bubble.DamageCalculation.Customs;
using Bubble.DamageCalculation.FighterManagement;
using Bubble.DamageCalculation.FighterManagement.FighterStats;
using Bubble.DamageCalculation.SpellManagement;
using BubbleBot.Cli.Repository.Maps;
using BubbleBot.Cli.Utils;

namespace BubbleBot.Cli.Services.Fight;

public class FightActor : IFighterData, IStatsOwner
{
    public long Id { get; set; }
    public StatsFields Stats { get; set; }
    public FightActor(Monsters? template, FightInfo? fight)
    {
        Template = template;
        Stats = new StatsFields(this);
        Fight = fight;
    }
    
    public string Name { get; set; } = string.Empty;
    
    public int Breed { get; private set; }
    public FightInfo? Fight { get; }
    
    public List<int> PreviousPositions { get; set; } = new();
    public int GameBeginPosition { get; set; }
    public int TurnBeginPosition { get; set; }
    public int BeforeLastSpellPosition { get; set; } = -1;
    public bool IsForcedDead { get; set; }
    public int Team { get; set; }
    public IList<FightActor> Summons { get; } = new List<FightActor>();
    public IList<HaxeBuff> Buffs { get; set; } = new List<HaxeBuff>();

    public IList<HaxeBuff> ActiveBuffs =>
        Buffs.Where(x => x.IsApplied && SpellManager.IsInstantaneousSpellEffect(x.Effect)).ToList();
    public IList<HaxeSpellState> States => ActiveBuffs.Where(x => x.SpellState != null && x.IsState())
                                                      .Select(x => x.SpellState!)
                                                      .ToList();

    public void Update(ActorPositionInformation actorPositionInformation)
    {
        Id = actorPositionInformation.ActorId;
        TurnBeginPosition = actorPositionInformation.Disposition.CellId;
        CellId = actorPositionInformation.Disposition.CellId;

        if(actorPositionInformation.ActorInformationValue.Fighter == null)
        {
            return;
        }

        if (actorPositionInformation.ActorInformationValue.Fighter.NamedFighter != null)
        {
            Name = actorPositionInformation.ActorInformationValue.Fighter.NamedFighter.Name;
        }
        
        GameBeginPosition = actorPositionInformation.ActorInformationValue.Fighter.SpawnInformation.Position.Disposition.CellId;
        
        if (actorPositionInformation.ActorInformationValue.Fighter.AiFighter != null)
        {
            Breed = actorPositionInformation.ActorInformationValue.Fighter.AiFighter.MonsterFighterInformation.MonsterGid;
        }
        else if (actorPositionInformation.ActorInformationValue.Fighter.NamedFighter != null)
        {
            Breed = actorPositionInformation.ActorInformationValue.Fighter.NamedFighter.CharacterInformation.BreedId;
        }
        else
        {
            Breed = 0;
        }
        
        CarriedFighterId = actorPositionInformation.Disposition.CarryingCharacterId;
        BeforeLastSpellPosition = actorPositionInformation.Disposition.CellId;
        PreviousPositions = actorPositionInformation.ActorInformationValue.Fighter.PreviousPositions;
        if (!IsForcedDead)
        {
            IsForcedDead = !actorPositionInformation.ActorInformationValue.Fighter.SpawnInformation.Alive;
        }

        Team = (int)actorPositionInformation.ActorInformationValue.Fighter.SpawnInformation.Team;

        UpdateStats(actorPositionInformation.ActorInformationValue.Fighter.Stats);
    }
    
    public void UpdateStats(FightCharacteristics stats)
    {
        Characteristics = stats;
        
        foreach (var stat in stats.Characteristics)
        {
            AddOrUpdateStat(stat);
        }   
    }

    public void AddOrUpdateStat(CharacterCharacteristic characteristic)
    {
        if (characteristic.Value != null)
        {
            AddOrUpdateStat(characteristic.CharacteristicId, characteristic.Value);
        }
        else if (characteristic.Detailed != null)
        {
            AddOrUpdateStat(characteristic.CharacteristicId, characteristic.Detailed);
        }
        else if (characteristic.Usable != null)
        {
            AddOrUpdateStat(characteristic.CharacteristicId, characteristic.Usable);
        }
    }

    private void AddOrUpdateStat(int characteristicId, CharacterCharacteristicValue value)
    {
        if(Stats.Fields.TryGetValue((StatId)characteristicId, out var stat))
        {
            stat.Base = value.Total;
            return;
        }

        Stats.Fields.Add((StatId)characteristicId, new StatsField(this, (StatId)characteristicId, value.Total));
    }
    
    private void AddOrUpdateStat(int characteristicId, CharacterCharacteristicDetailed value)
    {
        if(!Stats.Fields.TryGetValue((StatId)characteristicId, out var stat))
        {
            stat = new StatsField(this, (StatId)characteristicId, value.Base);
            Stats.Fields.Add((StatId)characteristicId, stat);
        }
        
        stat.Base = value.Base;
        stat.Additional = value.Additional;
        stat.Context = value.ContextModification;
        stat.Equipped = value.ObjectsAndMountBonus;
        stat.Given = value.Temporary + value.AlignmentGiftBonus;
    }
    
    private void AddOrUpdateStat(int characteristicId, CharacterCharacteristicDetailedUsable value)
    {
        if(!Stats.Fields.TryGetValue((StatId)characteristicId, out var stat))
        {
            stat = new StatsUsable(this, (StatId)characteristicId, value.Base);
            Stats.Fields.Add((StatId)characteristicId, stat);
        }
        
        if(stat is not StatsUsable usable)
        {
            usable = new StatsUsable(this, (StatId)characteristicId, value.Base);
            Stats.Fields[(StatId)characteristicId] = usable;
        }
        
        usable.Base = value.Base;
        usable.Additional = value.Additional;
        usable.Context = value.ContextModification;
        usable.Equipped = value.ObjectsAndMountBonus;
        usable.Given = value.Temporary + value.AlignmentGiftBonus;
        usable.Used = (short)value.Used;
    }
    

    public long SummonerId => Characteristics.Summoner;

    public bool Summoned => Characteristics.Summoned;

    public Monsters? Template { get; }

    public FightCharacteristics Characteristics { get; set; } = new()
    {
        Characteristics = new List<CharacterCharacteristic>(),
        Summoner = 0,
        Summoned = false,
        InvisibilityState = Jdc.JdcDpbe
    };

    public bool IsInvisible => Characteristics.InvisibilityState == Jdc.JdcDpbe;
    public bool FightStarted => true;
    public int CellId { get; set; }
    public int LastCellId { get; set; }
    public FighterTranslator FighterTranslator { get; set; }
    public long CarriedFighterId { get; set; }
    public List<SpellWrapper> Spells { get; set; } = new();
    public FightActor? LastAttacked { get; set; }

    public void SetFighterTranslator()
    {
        // long actorId, int level, int breed, PlayerType playerType, int teamId, bool isStaticElement, HaxeBuff[] buffs, IFighterData data
        FighterTranslator = new FighterTranslator(Id, 
                                                  200,
                                                  Breed, 
                                                  Breed > 20 ? PlayerType.Monster : PlayerType.Human,
                                                  Team, 
                                                  false, 
                                                  [], 
                                                  this);
        if (IsForcedDead)
        {
            FighterTranslator.IsDead = true;
        }
    }

    public bool UseSummonSlot()
    {
        return Template?.UseSummonSlot ?? false;
    }

    public int ResolveDodge()
    {
        return -1;
    }

    public bool IsSummon()
    {
        return Summoned;
    }

    public int GetUsedPm()
    {
        return Stats.Mp.Used;
    }

    public bool HasGod()
    {
        return false;
    }

    public int GetTurnBeginPosition()
    {
        return TurnBeginPosition;
    }

    public long GetSummonerId()
    {
        return SummonerId;
    }

    public IStatsField? GetStat(int statId)
    {
        return Stats.GetStat(statId);
    }
    
    public IStatsField? GetStat(StatId statId)
    {
        return Stats.GetStat((int)statId);
    }
    
    public int GetStartedPositionCell()
    {
        return BeforeLastSpellPosition;
    }

    public int GetPreviousPosition()
    {
        // only 2 previous positions are stored
        if (PreviousPositions.Count < 2)
        {
            return GameBeginPosition;
        }
        
        /*var previousPosition = MovementHistory.PopPreviousPosition(2);

        if (previousPosition == null)
        {
            return GameBeginPosition;
        }*/

        // On prend l'avant dernière position
        return PreviousPositions[^2];
    }

    public int GetMaxHealthPoints()
    {
        return Stats.GetMaxHealthPoints();
    }

    public int GetMaxHealthPointsWithoutContext()
    {
        return Stats.GetMaxHealthPointsBase();
    }
    public void AddSpellModification(int spellId, int modificationId, short value, ActionId actionId)
    {
        
    }

    public int GetItemSpellDamageModification(int spellId)
    {
        return 0;
    }

    public int GetHealthPoints()
    {
        return Stats.GetHealthPoints();
    }

    public int GetCharacteristicValue(StatId characteristicId)
    {
        return Stats.GetCharacteristicValue((int)characteristicId);
    }

    public bool CanBreedUsePortals()
    {
        return Template?.CanUsePortal ?? false;
    }

    public bool CanBreedSwitchPosOnTarget()
    {
        return Template?.CanSwitchPosOnTarget ?? false;
    }

    public bool CanBreedSwitchPos()
    {
        return Template?.CanSwitchPos ?? false;
    }

    public bool CanBreedBePushed()
    {
        return Template?.CanBePushed ?? false;
    }

    public bool CanBreedBeCarried()
    {
        return Template?.CanBeCarried ?? false;
    }

    public int RollApDodge(int apStolen, HaxeFighter source)
    {
        try
        {
            var value = 0;

            for (var i = 0; i < apStolen && value < Stats.Ap.Total; i++)
            {
                if (RollApLose(source, value))
                {
                    value++;
                }
            }

            return value;
        }
        catch (Exception e)
        {
            return 1;
        }
    }
    
    protected virtual bool RollApLose(HaxeFighter from, int value)
    {
        try
        {
            var fromApAttack = from.Data.GetCharacteristicValue(StatId.ApAttack);
            var apDodgeProbability = GetCharacteristicValue(StatId.DodgePaLostProbability);

            var apAttack = fromApAttack > 1 ? fromApAttack : 1;
            var apDodge = apDodgeProbability > 1 ? apDodgeProbability : 1;
            var prob = (Stats.Ap.Total - value) / (double)Stats.Ap.TotalMax * (apAttack / (double)apDodge) / 2d;

            prob = prob switch
            {
                < 0.10 => 0.10,
                > 0.90 => 0.90,
                _      => prob,
            };

            var rnd = Random.Shared.NextDouble();

            return rnd < prob;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    protected virtual bool RollMpLose(HaxeFighter from, int value)
    {
        try
        {
            var fromMpAttack = from.Data.GetCharacteristicValue(StatId.MpAttack);
            var mpDodgeProbability = GetCharacteristicValue(StatId.DodgePmLostProbability);

            var mpAttack = fromMpAttack > 1 ? fromMpAttack : 1;
            var mpDodge = mpDodgeProbability > 1 ? mpDodgeProbability : 1;
            var prob = (Stats.Mp.Total - value) / (double)Stats.Mp.TotalMax * (mpAttack / (double)mpDodge) / 2d;

            prob = prob switch
            {
                < 0.10 => 0.10,
                > 0.90 => 0.90,
                _      => prob,
            };

            var rnd = Random.Shared.NextDouble();
            return rnd < prob;
        }
        catch (Exception e)
        {
            return false;
        }
    }
    
    public int RollAmDodge(int mpStolen, HaxeFighter source)
    {
        try
        {
            var value = 0;

            for (var i = 0; i < mpStolen && value < Stats.Ap.Total; i++)
            {
                if (RollMpLose(source, value))
                {
                    value++;
                }
            }

            return value;
        }
        catch (Exception e)
        {
            return 1;
        }
    }


    public bool IsPlaying()
    {
        return Fight?.FighterPlaying?.Id == Id;
    }

    public bool UseSummonerTurnAndIsPlaying()
    {
        return false;
    }

    public void OverrideId(int id)
    {
        Id = id;
    }

    public IFighterData Clone()
    {
        var copy = (FightActor)MemberwiseClone();
        copy.Stats = Stats.Clone();
        return copy;
    }

    public bool IsAlive()
    {
        return !IsForcedDead;
    }

    public bool NobodyHasPlayed()
    {
        return false;
    }

    public long GetCurrentFighter()
    {
        return Fight?.FighterPlaying?.Id ?? -1;
    }

    public int GetDamageHealEquipmentSpellMod(int id, ActionId spellEffectActionId)
    {
        return 0;
    }

    public int GetCurrentTurn()
    {
        return 10;
    }

    public IEnumerable<int> GetCellIdsInRange(int origin, int range, int minRange)
    {
        return FightLosDetectorService.Instance.GetRangeCells(Fight!.Map,
                                                              Fight,
                                                              origin,
                                                              range,
                                                              minRange,
                                                              false,
                                                              false,
                                                              true)
                                      .Select(x => (int)x.Id);

    }

    public int GetUsedPa()
    {
        return Stats.Ap.Used;
    }

    public void ResetStats()
    {
        Stats.Reset();
        IsForcedDead = false;
    }

    public void ResetCurLife()
    {
        Stats.CurLife.Base = 0;   
        IsForcedDead = false;
    }

    public int GetPermanentDamage()
    {
        return Stats.GetStatTotalValue(StatId.CurPermanentDamage);
    }

    public void SetPermanentDamage(int curPermDamage)
    {
        Stats.CurLife.Context -= curPermDamage;
        Stats.CurPermanentDamage.Context = curPermDamage;
    }

    public void SetZombieLife()
    {
        // the base life is 20% of the max life
        Stats.CurLife.Base -= Stats.GetMaxHealthPoints() - Stats.GetMaxHealthPoints() / 5;
    }

    public void SetHalfLife()
    {
        // the base life is 20% of the max life
        Stats.CurLife.Base -= Stats.GetMaxHealthPoints() / 2;
    }

    public void SetSummoner(long casterId)
    {
        Characteristics.Summoner = casterId;
    }

    public void SetSummoner(int casterId)
    {
        Characteristics.Summoner = casterId;
    }

    public bool HasPlayedThisTurn()
    {
        return false;
    }

    public int GetFightStartPosition()
    {
        return GameBeginPosition;
    }

    public int[] GetSummonIds()
    {
        return Summons.Select(x => (int)x.Id).ToArray();
    }


    public bool IsEnemyWith(FightActor fighter)
    {
        return fighter.Team != Team;
    }

    public bool IsAllyWith(FightActor fighter)
    {
        return fighter.Team == Team;
    }

    public FightActor[] GetTacklers(Cell cell)
    {
        return Fight!.GetAllFighters<FightActor>(entry => entry.Team != Team &&
                                                         entry.CanTackle(this) &&
                                                         MapPoint.GetPoint(entry.CellId)!.IsAdjacentTo(cell)).ToArray();
    }

    private bool CanTackle(FightActor actor)
    {
        if (!IsEnemyWith(actor))
        {
            return false;
        }

        if (!IsAlive())
        {
            return false;
        }

        if (actor.CellId == CellId)
        {
            return false;
        }

        if (IsInvisible)
        {
            return false;
        }

        if (actor.IsInvisible)
        {
            return false;
        }

        if (HasState((int)SpellStateId.Porte))
        {
            return false;
        }

        if (States.Any(x => x.Template is { CantTackle: true, }))
        {
            return false;
        }

        if (actor.States.Any(x => x.Template is { CantTackle: true, }))
        {
            return false;
        }

        if (HasState((int)SpellStateId.Intacleur))
        {
            return false;
        }

        if (actor.HasState((int)SpellStateId.Intaclable))
        {
            return false;
        }

        return true;

    }


    private bool HasState(int stat)
    {
        return States.Any(x => x.Template.Id == stat);
    }

    private double GetTacklePercent(Cell cell)
    {
        var tacklers = GetTacklers(cell);

        // no tacklers, then no tackle possible
        if (tacklers.Length <= 0)
        {
            return 1d;
        }

        var percentRemaining =
            tacklers.Aggregate(1d, (current, fightActor) => current * GetSingleTacklerPercent(fightActor));

        switch (percentRemaining)
        {
            case < 0:
                percentRemaining = 0d;
                break;
            case > 1:
                percentRemaining = 1;
                break;
        }

        return percentRemaining;
    }

    private double GetSingleTacklerPercent(FightActor tackler)
    {
        var tackleBlock = tackler.Stats[StatId.TackleBlock].TotalSafe;
        var tackleEvade = Stats[StatId.TackleEvade].TotalSafe;

        return Math.Max(0, Math.Min(1, (tackleEvade + 2) / (2d * (tackleBlock + 2))));
    }
    
    public int GetTackledMp(int mp, Cell cell)
    {
        return MathUtils.Round(mp * (1 - GetTacklePercent(cell)));
    }

    public int GetTackledAp(int ap, Cell cell)
    {
        var removedAp = ap * (1 - GetTacklePercent(cell));
        return MathUtils.Round(removedAp);
    }

    public bool IsPlayerBreed()
    {
        return Breed < 20;
    }



}

