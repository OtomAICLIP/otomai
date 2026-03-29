using Bubble.Core.Datacenter.Datacenter.Effects;
using Bubble.DamageCalculation.Customs;
using Bubble.DamageCalculation.FighterManagement;
using Bubble.DamageCalculation.SpellManagement;
using Bubble.DamageCalculation.Tools;

namespace Bubble.DamageCalculation;

public class FightContext
{
    public IList<HaxeFighter> LastKilledDefenders { get; }
    public IList<HaxeFighter> LastKilledChallengers { get; }
    public bool InMovement { get; set; }
    public IList<HaxeFighter> TempFighters { get; set; }
    public int GameTurn { get; }
    public IMapInfo Map { get; }
    public int TargetedCell { get; set; }
    public int MarkExecutionCell { get; set; } = -1;
    public int MarkMainCell { get; set; } = -1;
    public HaxeFighter OriginalCaster { get; }
    public IList<int> TriggeredMarks { get; set; }
    public int InputPortalCellId { get; set; } = -1;
    public IList<HaxeFighter> Fighters { get; }
    public List<FighterCellLight> FighterInitialPositions { get; }

    public bool DebugMode { get; private set; }
    public bool IsSimulation { get; set; }
    public bool FromStartingSpell { get; set; }
    public bool FromGlyphAuraSet { get; set; }
    public bool FromGlyphAuraDispell { get; set; }
    public Mark? FromGlyphAura { get; set; }

    public int LastMarkId { get; set; }

    public FightContext(int gameTurn,
        IMapInfo mapInfo,
        int targetedCell,
        HaxeFighter originalCaster,
        IList<HaxeFighter>? fighters = null,
        List<FighterCellLight>? fightersInitialPositions = null,
        int inputPortalCellId = -1,
        bool debugMode = false)
    {
        LastKilledDefenders   = new List<HaxeFighter>();
        LastKilledChallengers = new List<HaxeFighter>();

        InMovement   = false;
        TempFighters = new List<HaxeFighter>();

        GameTurn          = gameTurn;
        Map               = mapInfo;
        TargetedCell      = targetedCell;
        OriginalCaster    = originalCaster;
        TriggeredMarks    = new List<int>();
        InputPortalCellId = inputPortalCellId;
        var marks = Map.GetMarks(false);
        
        LastMarkId        = marks.Count == 0 ? 0 : marks.Max(x => x.MarkId);
        
        if (fighters == null)
        {
            Fighters = new List<HaxeFighter>();
        }
        else
        {
            Fighters = fighters;
        }

        if (fightersInitialPositions == null)
        {
            FighterInitialPositions = mapInfo.GetFightersInitialPositions();

            foreach (var fighter in Fighters)
            {
                RemoveFighterCells(fighter.Id);
            }
        }
        else
        {
            FighterInitialPositions = fightersInitialPositions;
        }

        DebugMode = debugMode;
    }


    public void ResetTriggers()
    {
        foreach (var fighter in Fighters.SelectMany(x => x.Buffs))
        {
            fighter.ResetTriggeredOn();
        }
    }

    /// <summary>
    /// Determines if a portal is currently being used.
    /// </summary>
    /// <returns>Returns true if a portal is being used, otherwise false.</returns>
    public bool UsingPortal()
    {
        return InputPortalCellId != -1;
    }

    /// <summary>
    /// Removes the initial position of the fighter with the specified id from the FightersInitialPositions list.
    /// </summary>
    /// <param name="fighterId">The id of the fighter whose initial position should be removed.</param>
    public void RemoveFighterCells(long fighterId)
    {
        FighterInitialPositions.RemoveAll(initialPosition => initialPosition.Id == fighterId);
    }

    /// <summary>
    /// Determines if the given cell is empty and available for movement.
    /// </summary>
    /// <param name="cell">The cell id to check for emptiness and movement availability.</param>
    /// <returns>Returns true if the cell is empty and available for movement, otherwise false.</returns>
    public bool IsCellEmptyForMovement(int cell)
    {
        if (Fighters.Any(x => x.IsAlive() && x.GetCurrentPositionCell() == cell))
        {
            return false;
        }

        return Map.IsCellWalkable(cell);
    }

    public bool IsCellWalkable(int cell)
    {
        return Map.IsCellWalkable(cell);
    }

    /// <summary>
    /// Calculates the portal bonus based on the provided action id.
    /// </summary>
    /// <param name="actionId">The action id to determine if the portal bonus should be calculated.</param>
    /// <returns>Returns the portal bonus as an integer value.</returns>
    public int GetPortalBonus(ActionId actionId)
    {
        if (!ActionIdHelper.IsPortalBonus(actionId))
        {
            return 0;
        }

        var activePortals = Map.GetMarks(true, GameActionMarkType.Portal).Where(x => x.Active).ToList();

        if (activePortals.Count == 0)
        {
            return 0;
        }

        var portalBonus = 0;
        var inputPortal = activePortals.Find(x => x.MainCell == InputPortalCellId);

        if (inputPortal == null)
        {
            return 0;
        }

        Mark? nextPortal    = null;
        var   currentPortal = inputPortal;

        activePortals = activePortals.Where(x => x.TeamId == currentPortal.TeamId).ToList();
        activePortals.Remove(currentPortal);
        activePortals = PortalUtils.GetPortalChainFromPortals(currentPortal, activePortals);

        var multiplier = 0;

        while (true)
        {
            var distance = -1;

            if (activePortals.Count > 0)
            {
                nextPortal = activePortals[0];
                distance   = MapTools.GetDistance(nextPortal.MainCell, currentPortal.MainCell);
            }

            if (nextPortal == null)
            {
                break;
            }

            var maxBonus = 0;
            var effects  = nextPortal.FromSpell!.GetEffects();

            foreach (var effect in effects)
            {
                if (effect.ActionId == ActionId.FightAddPortal)
                {
                    maxBonus   = Math.Max(maxBonus, effect.Param3);
                    multiplier = Math.Max(multiplier, effect.Param1);
                }
            }

            portalBonus   += distance;
            currentPortal =  nextPortal;
            activePortals.Remove(currentPortal);

            if (activePortals.Count <= 0)
            {
                return maxBonus + (portalBonus * multiplier);
            }
        }

        throw new Exception("There is no nearest Portal");
    }

    /// <summary>
    /// Retrieves the last killed ally of the specified team.
    /// </summary>
    /// <param name="teamId">The id of the team (0 for challengers, 1 for defenders).</param>
    /// <returns>Returns the last killed ally as a HaxeFighter, or null if none found.</returns>
    public HaxeFighter? GetLastKilledAlly(int teamId)
    {
        if (teamId == 0 && LastKilledChallengers.Count > 0)
        {
            return LastKilledChallengers[0];
        }

        return Map.GetLastKilledAlly(teamId);
    }
    
    public void RemoveDeadFighter(long fighterId)
    {
        Map.RemoveDeadFighter(fighterId);
    }

    /// <summary>
    /// Generates a unique identifier for a new fighter.
    /// </summary>
    /// <returns>Returns a unique identifier as a number.</returns>
    public int GetFreeId()
    {
        return Map.GetFreeId();
    }

    /// <summary>
    /// Retrieves fighters in the specified direction up to a certain distance or a limited number of fighters.
    /// </summary>
    /// <param name="cellId">The starting cell id.</param>
    /// <param name="direction">The direction to search for fighters.</param>
    /// <param name="distanceToIgnore">The distance to ignore before starting to search for fighters.</param>
    /// <param name="maxDistance">The maximum distance to search for fighters.</param>
    /// <param name="maxFighters">The maximum number of fighters to return.</param>
    /// <returns>Returns an array of fighters found in the specified direction up to the given constraints.</returns>
    public List<HaxeFighter> GetFightersUpTo(int cellId, int direction, int distanceToIgnore, int maxDistance,
        int maxFighters)
    {
        HaxeFighter? foundFighter = null;

        var fighters = new List<HaxeFighter>();

        do
        {
            cellId = MapTools.GetNextCellByDirection(cellId, direction);
            distanceToIgnore--;
            maxDistance--;

            if (distanceToIgnore > 0)
            {
                continue;
            }

            foundFighter = GetFighterFromCell(cellId);
            if (foundFighter == null)
            {
                continue;
            }

            fighters.Add(foundFighter);
            maxFighters--;
        } while (foundFighter == null && MapTools.IsValidCellId(cellId) && maxDistance > 0 && maxFighters > 0);

        return fighters;
    }

    /// <summary>
    /// Retrieves fighters from a given SpellZone, starting cell, and target cell.
    /// </summary>
    /// <param name="spellZone">The SpellZone instance to search for fighters in.</param>
    /// <param name="startingCell">The starting cell id.</param>
    /// <param name="targetCell">The target cell id.</param>
    /// <param name="isFromDeath"></param>
    /// <param name="caster"></param>
    /// <param name="rawZone"></param>
    /// <param name="runningEffect"></param>
    /// <param name="forceTarget"></param>
    /// <returns>Returns an array of fighters found in the specified SpellZone.</returns>
    public List<HaxeFighter> GetFightersFromZone(SpellZone spellZone,
        int startingCell,
        int targetCell,
        bool isFromDeath = false, 
        HaxeFighter? caster = null,
        string? rawZone = null, 
        RunningEffect? runningEffect = null, 
        HaxeFighter? forceTarget = null,
        HaxeFighter? additionalTarget = null,
        bool fromAppearing = false)
    {
        if (!MapTools.IsValidCellId(startingCell) || !MapTools.IsValidCellId(targetCell))
        {
            return new List<HaxeFighter>();
        }

        var fightersInZone = new List<HaxeFighter>();

        bool IsValidFighter(HaxeFighter fighter, int shape)
        {
            if (shape == 'A' || fighter.IsAlive(isFromDeath) && (!fighter.HasState(8) || shape == 'a'))
            {
                return MapTools.IsValidCellId(fighter.GetBeforeLastSpellPosition());
            }

            return false;
        }

        List<int>? rangeCellIds = null;
        var        castTestLos  = false;
        
        if (rawZone != null && caster != null)
        {
            castTestLos = spellZone.Shape == 'C' && rawZone.EndsWith("10,4,1");
            
            if (castTestLos)
            {
                rangeCellIds = caster.Data
                                     .GetCellIdsInRange(startingCell, spellZone.Radius, spellZone.MinRadius)
                                     .ToList();
            }
        }
        
        if (spellZone.Shape == 'P' && 
            spellZone.MinRadius == 0 && 
            spellZone.Radius == 0 &&
            caster != null &&
            //caster.PendingEffects.Any(x => x.InvisibilityState == true) &&
            caster.PendingEffects.Any(x => x.IsSummoning) &&
            Fighters.Any(y => y.PendingEffects.Any(x => x.Summon != null))&&
            fromAppearing)
        {
            var summonedFighter = Fighters.FirstOrDefault(x => x.PendingEffects.Any(y => y.Summon != null));
            
            if (summonedFighter != null)
            {
                fightersInZone.Add(summonedFighter);
                forceTarget = summonedFighter;
            }
        }

        foreach (var fighter in Fighters)
        {
            if (IsValidFighter(fighter, spellZone.Shape) &&
                (spellZone.IsCellInZone!(fighter.GetBeforeLastSpellPosition(), startingCell, targetCell) || additionalTarget == fighter))
            {
                if (castTestLos && rangeCellIds != null)
                {
                    if (!rangeCellIds.Contains(fighter.GetBeforeLastSpellPosition()))
                        continue;
                }
                
                fightersInZone.Add(fighter);
            }
        }
        

        foreach (var currentPosition in FighterInitialPositions.ToArray())
        {
            if (!MapTools.IsValidCellId(currentPosition.CellId) ||
                !spellZone.IsCellInZone!(currentPosition.CellId, startingCell, targetCell))
            {
                continue;
            }

            if (castTestLos && rangeCellIds != null)
            {
                if (!rangeCellIds.Contains(currentPosition.CellId))
                    continue;
            }

            var foundFighter = CreateFighterById(currentPosition.Id);
            if (foundFighter != null && IsValidFighter(foundFighter, spellZone.Shape))
            {
                fightersInZone.Add(foundFighter);
            }
        }

        if (forceTarget != null)
        {
            return fightersInZone.Where(x => x.Id == forceTarget.Id).ToList();
        }
        
        return fightersInZone;
    }

    /// <summary>
    /// Retrieves a fighter from a specific cell.
    /// </summary>
    /// <param name="cellId">The cell id to search for a fighter.</param>
    /// <param name="useCurrentPosition">Whether to use the current position cell or the position before the last spell. Default value is false.</param>
    /// <returns>Returns a HaxeFighter instance if found, otherwise returns null.</returns>
    public HaxeFighter? GetFighterFromCell(int cellId, bool useCurrentPosition = false)
    {
        foreach (var fighter in Fighters.Where(x => x.IsAlive()))
        {
            if (useCurrentPosition && fighter.GetCurrentPositionCell() == cellId ||
                !useCurrentPosition && fighter.GetBeforeLastSpellPosition() == cellId)
            {
                return fighter;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the current summon count of a fighter.
    /// </summary>
    /// <param name="fighter">The HaxeFighter instance for which the summon count is to be retrieved.</param>
    /// <returns>Returns the current summon count of the given fighter.</returns>
    public int GetFighterCurrentSummonCount(HaxeFighter fighter)
    {
        var summonCount = 0;

        foreach (var f in Fighters)
        {
            if (f.Id != fighter.Id && f.IsAlive() && f.Data.IsSummon() && f.Data.UseSummonSlot() &&
                f.Data.GetSummonerId() == fighter.Id && !f.IsStaticElement)
            {
                summonCount++;
            }
        }

        return summonCount;
    }

    /// <summary>
    /// Retrieves a fighter by their id.
    /// </summary>
    /// <param name="id">The id of the fighter to be retrieved.</param>
    /// <returns>Returns a HaxeFighter instance if found, otherwise creates a new HaxeFighter with the given id.</returns>
    public HaxeFighter? GetFighterById(long id)
    {
        foreach (var fighter in Fighters)
        {
            if (fighter.Id == id)
            {
                return fighter;
            }
        }

        return CreateFighterById(id);
    }

    /// <summary>
    /// Retrieves every fighter in the current map.
    /// </summary>
    /// <returns>Returns an array containing all fighters in the map.</returns>
    public List<HaxeFighter> GetEveryFighter()
    {
        var fighterList = new List<HaxeFighter>();

        foreach (var fighterId in Map.GetEveryFighterId())
        {
            fighterList.Add(GetFighterById(fighterId)!);
        }

        return fighterList;
    }

    /// <summary>
    /// Gets the carried fighter by a specific fighter.
    /// </summary>
    /// <param name="fighter">The HaxeFighter instance carrying another fighter.</param>
    /// <returns>Returns the carried HaxeFighter instance, or null if there is no carried fighter.</returns>
    public HaxeFighter? GetCarriedFighterBy(HaxeFighter fighter)
    {
        var carriedFighterId = Map.GetCarriedFighterIdBy(fighter);

        if (carriedFighterId != 0)
        {
            return GetFighterById(carriedFighterId);
        }

        return null;
    }

    /// <summary>
    /// Retrieves an array of fighters that are affected by any effect.
    /// </summary>
    /// <returns>Returns an array of HaxeFighter instances that are affected by any effect.</returns>
    public List<HaxeFighter> GetAffectedFighters()
    {
        var affectedFighters = new List<HaxeFighter>();

        foreach (var fighter in Fighters)
        {
            fighter.SavePendingEffects();

            if (fighter.TotalEffects == null)
            {
                continue;
            }

            foreach (var effect in fighter.TotalEffects)
            {
                if (effect.DamageRange == null &&
                    effect.Movement == null &&
                    effect is
                    {
                        AttemptedApTheft          : false,
                        AttemptedAmTheft          : false,
                        LostStateId               : -1,
                        NewStateId                : -1,
                        ApStolen                  : 0,
                        AmStolen                  : 0,
                        RangeLoss                 : 0,
                        RangeGain                 : 0,
                        Summon                    : null,
                        StatId                    : -1,
                        IsSummoning               : false,
                        Dispell                   : false,
                        Death                     : false,
                        BuffAdded                 : null,
                        BuffRemoved               : null,
                        BuffUpdated               : null,
                        SpellIdModified           : -1,
                        SpellExecutionInfos       : null,
                        MarkTriggered             : -1,
                        ControlledBy              : null,
                        NoMoreControlled          : null,
                        CooldownSpellId           : -1,
                        InvisibilityDetectedAtCell: -1,
                        InvisibilityState         : null,
                        PassCurrentTurn           : false,
                        LookUpdate                : false,
                        MarkAdded                 : null
                    })
                {
                    continue;
                }

                affectedFighters.Add(fighter);
                break;
            }
        }

        return affectedFighters;
    }

    /// <summary>
    /// Creates a HaxeFighter instance with the given fighter ID.
    /// </summary>
    /// <param name="fighterId">The ID of the fighter to create.</param>
    /// <returns>Returns a HaxeFighter instance if found, otherwise returns null.</returns>
    public HaxeFighter? CreateFighterById(long fighterId)
    {
        var fighter = Map.GetFighterById(fighterId);
        if (fighter != null)
        {
            Fighters.Add(fighter);
            RemoveFighterCells(fighter.Id);
        }

        return fighter;
    }

    /// <summary>
    /// Creates a deep copy of the current FightContext instance.
    /// </summary>
    /// <returns>Returns a new FightContext instance with the same properties as the original one.</returns>
    public FightContext Copy()
    {
        var clonedContext = new FightContext(GameTurn, Map, TargetedCell, OriginalCaster,
            new List<HaxeFighter>(Fighters),
            new List<FighterCellLight>(FighterInitialPositions))
        {
            TriggeredMarks    = TriggeredMarks,
            TempFighters      = TempFighters,
            DebugMode         = DebugMode,
            InputPortalCellId = InputPortalCellId,
            IsSimulation      = IsSimulation
        };

        return clonedContext;
    }

    /// <summary>
    /// Adds the last killed ally to the appropriate list based on the team and player type.
    /// </summary>
    /// <param name="fighter">The HaxeFighter instance representing the killed ally.</param>
    public void AddLastKilledAlly(HaxeFighter fighter)
    {
        var targetList  = fighter.TeamId == 0 ? LastKilledChallengers : LastKilledDefenders;
        var insertIndex = 0;

        if (fighter.PlayerType == PlayerType.Human)
        {
            insertIndex = 0;
        }
        else
        {
            while (insertIndex < targetList.Count && targetList[insertIndex].PlayerType == PlayerType.Human)
            {
                insertIndex++;
            }

            if (fighter.PlayerType != PlayerType.Sidekick)
            {
                while (insertIndex < targetList.Count && targetList[insertIndex].PlayerType == PlayerType.Sidekick)
                {
                    insertIndex++;
                }
            }
        }

        if (fighter.TeamId == 0)
        {
            LastKilledChallengers.Insert(insertIndex, fighter);
        }
        else
        {
            LastKilledDefenders.Insert(insertIndex, fighter);
        }
    }

    public void SaveEffects()
    {
        foreach (var fighter in Fighters)
        {
            fighter.SavePendingEffects();
        }
    }

    public int GetFreeMarkId()
    {
        var newId    = LastMarkId + 1;
        var isUnique = false;

        while (!isUnique)
        {
            isUnique = true;
            var allMarks = Map.GetMarks(false);

            if (allMarks.All(mark => newId != mark.MarkId))
            {
                continue;
            }

            newId++;
            LastMarkId++;
            isUnique = false;
        }

        LastMarkId = newId;
        return newId;
    }
}