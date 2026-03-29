using System.Globalization;
using Bubble.DamageCalculation.Customs;
using Bubble.DamageCalculation.FighterManagement;

namespace Bubble.DamageCalculation.SpellManagement;

public static class SpellManager
{
    private const string ExclusiveMasksList = "*bBeEfFzZKoOPpTWuUvVrRQq";

    /// <summary>
    /// Splits a string into a list of substrings based on space and comma delimiters.
    /// </summary>
    /// <param name="input">The input string to be split.</param>
    /// <returns>An IList containing the split substrings.</returns>
    public static IList<string> SplitMasks(string input)
    {
        var substrings   = new List<string>();
        var currentIndex = 0;

        while (currentIndex < input.Length)
        {
            while (currentIndex < input.Length && (input[currentIndex] == ' ' || input[currentIndex] == ','))
            {
                currentIndex++;
            }

            var startPosition = currentIndex;

            while (currentIndex < input.Length && input[currentIndex] != ',')
            {
                currentIndex++;
            }

            if (currentIndex != startPosition)
            {
                substrings.Add(input.Substring(startPosition, currentIndex - startPosition));
            }
        }

        return substrings;
    }

    /// <summary>
    /// Splits a string into a list of substrings based on space and pipe delimiters.
    /// </summary>
    /// <param name="input">The input string to be split.</param>
    /// <returns>An IList containing the split substrings.</returns>
    public static IList<string> SplitTriggers(string? input)
    {
        if (input == null)
        {
            return new List<string>();
        }

        var substrings   = new List<string>();
        var currentIndex = 0;

        while (currentIndex < input.Length)
        {
            while (currentIndex < input.Length && (input[currentIndex] == ' ' || input[currentIndex] == '|'))
            {
                currentIndex++;
            }

            var startPosition = currentIndex;

            while (currentIndex < input.Length && input[currentIndex] != '|')
            {
                currentIndex++;
            }

            if (currentIndex != startPosition)
            {
                substrings.Add(input.Substring(startPosition, currentIndex - startPosition));
            }
        }

        return substrings;
    }

    /// <summary>
    /// Determines if a target is selected by a mask.
    /// </summary>
    /// <param name="fighter">The fighter to be evaluated.</param>
    /// <param name="maskList">The list of masks to evaluate the fighter against.</param>
    /// <param name="target">The main target of the spell.</param>
    /// <param name="referenceTarget">The reference target, usually the main target of the spell.</param>
    /// <param name="fightContext">The fight context</param>
    /// <returns>Returns true if the target is selected by the mask, false otherwise.</returns>
    public static bool IsSelectedByMask(HaxeFighter fighter, IList<string> maskList, HaxeFighter? target,
                                        HaxeFighter? referenceTarget, FightContext fightContext)
    {
        if (maskList.Count == 0)
        {
            return true;
        }

        if (target == null)
        {
            return false;
        }

        return IsIncludedByMask(fighter, maskList, target) &&
               PassMaskExclusion(fighter, maskList, target, referenceTarget, fightContext);
    }
    
    /// <summary>
    /// Determines whether a fighter is included by the mask based on the given list of masks and fighters.
    /// </summary>
    /// <param name="fighter1">The first fighter to be checked.</param>
    /// <param name="masks">The list of masks.</param>
    /// <param name="fighter2">The second fighter to be checked.</param>
    /// <returns>True if the fighter is included by the mask; otherwise, false.</returns>
    public static bool IsIncludedByMask(HaxeFighter fighter1, IList<string> masks, HaxeFighter fighter2)
    {
        var sameFighter = fighter2.Id == fighter1.Id;

        if (sameFighter)
        {
            return masks.Contains("c", StringComparer.Ordinal) ||
                   masks.Contains("C", StringComparer.Ordinal) ||
                   masks.Contains("a", StringComparer.Ordinal);
        }

        var sameTeam = fighter1.TeamId == fighter2.TeamId;
        var isSummon = fighter2.Data.IsSummon();

        foreach (var mask in masks)
        {
            switch (mask)
            {
                case "A":
                    if (!sameTeam)
                    {
                        return true;
                    }

                    break;
                case "D":
                    if (!sameTeam && fighter2.PlayerType == PlayerType.Sidekick)
                    {
                        return true;
                    }

                    break;
                case "H":
                    if (!sameTeam && fighter2.PlayerType == PlayerType.Human && !isSummon)
                    {
                        return true;
                    }

                    break;
                case "I":
                    if (!sameTeam && fighter2.PlayerType != PlayerType.Sidekick && isSummon &&
                        !fighter2.IsStaticElement)
                    {
                        return true;
                    }

                    break;
                case "J":
                    if (!sameTeam && fighter2.PlayerType != PlayerType.Sidekick && isSummon)
                    {
                        return true;
                    }

                    break;
                case "L":
                    if (!sameTeam && (fighter2.PlayerType == PlayerType.Human && !isSummon || fighter2.PlayerType == PlayerType.Sidekick))
                    {
                        return true;
                    }

                    break;
                case "M":
                    if (!sameTeam && fighter2.PlayerType != PlayerType.Human && !isSummon && !fighter2.IsStaticElement)
                    {
                        return true;
                    }

                    break;
                case "S":
                    if (!sameTeam && fighter2.PlayerType != PlayerType.Sidekick && isSummon && fighter2.IsStaticElement)
                    {
                        return true;
                    }

                    break;
                case "a":
                case "g":
                    if (sameTeam)
                    {
                        return true;
                    }

                    break;
                case "d":
                    if (sameTeam && fighter2.PlayerType == PlayerType.Sidekick)
                    {
                        return true;
                    }

                    break;
                case "h":
                    if (sameTeam && fighter2.PlayerType == PlayerType.Human && !isSummon)
                    {
                        return true;
                    }

                    break;
                case "i":
                    if (sameTeam && fighter2.PlayerType != PlayerType.Sidekick && isSummon && !fighter2.IsStaticElement)
                    {
                        return true;
                    }

                    break;
                case "j":
                    if (sameTeam && fighter2.PlayerType != PlayerType.Sidekick && isSummon)
                    {
                        return true;
                    }
                    break;
                case "l":
                    if (sameTeam && (fighter2.PlayerType == PlayerType.Human && !isSummon || fighter2.PlayerType == PlayerType.Sidekick))
                    {
                        return true;
                    }
                    break;
                case "m":
                    if (sameTeam && fighter2.PlayerType != PlayerType.Human && !isSummon && !fighter2.IsStaticElement)
                    {
                        return true;
                    }

                    break;
                case "s":
                    if (sameTeam && fighter2.PlayerType != PlayerType.Sidekick && isSummon && fighter2.IsStaticElement)
                    {
                        return true;
                    }

                    break;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if a fighter passes mask exclusion based on the given list of exclusion masks.
    /// </summary>
    /// <param name="fighter1">The first fighter to be checked.</param>
    /// <param name="exclusionMasks">The list of exclusion masks.</param>
    /// <param name="fighter2">The second fighter to be checked.</param>
    /// <param name="fighter3">The third fighter to be checked.</param>
    /// <param name="fightContext">The fight context.</param>
    /// <returns>True if the fighter passes mask exclusion; otherwise, false.</returns>
    public static bool PassMaskExclusion(HaxeFighter fighter1, IList<string> exclusionMasks, HaxeFighter fighter2,
                                         HaxeFighter? fighter3, FightContext fightContext)
    {
        var currentIndex  = 0;
        var isUsingPortal = fightContext.UsingPortal();

        while (currentIndex < exclusionMasks.Count)
        {
            var currentMask = exclusionMasks[currentIndex];
            currentIndex++;

            if (!ExclusiveMasksList.Contains(currentMask[0], StringComparison.Ordinal))
            {
                continue;
            }

            HaxeFighter targetFighter;
            bool        isCaster;
            if (currentMask[0] == '*')
            {
                targetFighter = fighter1;
                isCaster      = true;
            }
            else
            {
                targetFighter = fighter2;
                isCaster      = false;
            }

            if (!TargetPassMaskExclusion(fighter1, targetFighter, fighter3, fightContext,
                                         currentMask, exclusionMasks, isUsingPortal, isCaster))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Determines if a target should be excluded from the target list based on a given mask.
    /// </summary>
    /// <param name="caster">The caster of the spell.</param>
    /// <param name="target">The target to be evaluated.</param>
    /// <param name="referenceTarget">The reference target, usually the main target of the spell.</param>
    /// <param name="fightContext">The fight context.</param>
    /// <param name="mask">The mask used to evaluate the target.</param>
    /// <param name="maskList">The list of masks for the spell.</param>
    /// <param name="isUsingPortal">Indicates if the cast came from a portal.</param>
    /// <param name="isCaster">Indicates if the character is the first in the mask.</param>
    /// <returns>Returns true if the target should be excluded, false otherwise.</returns>
    public static bool TargetPassMaskExclusion(HaxeFighter caster, HaxeFighter target, HaxeFighter? referenceTarget,
                                               FightContext fightContext, string mask, IList<string> maskList,
                                               bool isUsingPortal, bool isCaster)
    {
        int maskValue;
        var startIndex = isCaster ? 1 : 0;

        switch (mask.Length)
        {
            case 0:
            case 1:
                maskValue = 0;
                break;
            default:
                int.TryParse(mask[(startIndex + 1)..], CultureInfo.InvariantCulture, out maskValue);
                break;
        }

        var maskCharacter   = mask[startIndex];
        var exclusionResult = EvaluateExclusionByMaskCharacter(caster, target, referenceTarget, fightContext, maskCharacter, maskValue, isUsingPortal);

        if (!MaskIsOneOfCondition(mask))
        {
            return exclusionResult;
        }

        var nextMask = maskList.IndexOf(mask) + 1;
        
        if (exclusionResult)
        {
            for (var i = nextMask; i < maskList.Count; i++)
            {
                if (maskList[i].Length > startIndex && maskList[i][startIndex] == mask[startIndex])
                {
                    maskList[i] = " ";
                }
            }
        }
        else
        {
            for (var i = nextMask; i < maskList.Count; i++)
            {
                if (maskList[i].Length > startIndex && maskList[i][startIndex] == mask[startIndex])
                {
                    exclusionResult = true;
                    break;
                }
            }
        }
        
        return exclusionResult;
    }

    /// <summary>
    /// Evaluates if a target should be excluded based on a given mask character and its associated value.
    /// </summary>
    /// <param name="caster">The caster of the spell.</param>
    /// <param name="target">The target to be evaluated.</param>
    /// <param name="referenceTarget">The reference target, usually the main target of the spell.</param>
    /// <param name="fightContext">The fight context.</param>
    /// <param name="maskCharacter">The mask character used to evaluate the target.</param>
    /// <param name="maskValue">The value associated with the mask character.</param>
    /// <param name="isUsingPortal">Indicates if the cast came from a portal.</param>
    /// <returns>Returns true if the target should be excluded, false otherwise.</returns>
    private static bool EvaluateExclusionByMaskCharacter(HaxeFighter caster, HaxeFighter target,
                                                         HaxeFighter? referenceTarget,
                                                         FightContext fightContext, char maskCharacter, int maskValue,
                                                         bool isUsingPortal)
    {
        switch (maskCharacter)
        {
            case 'B':
                return target.PlayerType == PlayerType.Human && target.Breed == maskValue;
            case 'b':
                return target.PlayerType != PlayerType.Human || target.Breed != maskValue;
            case 'E':
                return target.HasState(maskValue);
            case 'e':
                return !target.HasState(maskValue);
            case 'F':
                return target.PlayerType != PlayerType.Human && target.Breed == maskValue;
            case 'f':
                return target.PlayerType == PlayerType.Human || target.Breed != maskValue;
            case 'K':
                return target.HasState(8) &&
                       caster.GetCarried(fightContext) == target ||
                       target.PendingEffects.Any(x => x.ThrowedBy == caster.Id);
            case 'P':
                return target.Id == caster.Id || target.Data.IsSummon() && target.Data.GetSummonerId() == caster.Id ||
                       target.Data.IsSummon() && caster.Data.GetSummonerId() == target.Data.GetSummonerId() ||
                       caster.Data.IsSummon() && caster.Data.GetSummonerId() == target.Id;
            case 'p':
                return !(target.Id == caster.Id || target.Data.IsSummon() && target.Data.GetSummonerId() == caster.Id ||
                         target.Data.IsSummon() && caster.Data.GetSummonerId() == target.Data.GetSummonerId() ||
                         caster.Data.IsSummon() && caster.Data.GetSummonerId() == target.Id);
            case 'Q':
                return fightContext.GetFighterCurrentSummonCount(target) >= target.Data.GetCharacteristicValue(StatId.MaxSummonedCreaturesBoost);
            case 'q':
                return fightContext.GetFighterCurrentSummonCount(target) < target.Data.GetCharacteristicValue(StatId.MaxSummonedCreaturesBoost);
            case 'R':
                return isUsingPortal;
            case 'r':
                return !isUsingPortal;
            case 'T':
                return target.WasTelefraggedThisTurn();
            case 'U':
                return target.IsAppearing();
            case 'u':
                return target.IsAppearing();
            case 'V':
                var pendingLifePoints = target.GetPendingLifePoints().Min;
                var maxLifePoints     = target.Data.GetMaxHealthPoints();
                var r                 = pendingLifePoints / (double)maxLifePoints * 100d;
                return r < maskValue;
            case 'v':
                pendingLifePoints = target.GetPendingLifePoints().Min;
                maxLifePoints     = target.Data.GetMaxHealthPoints();
                var rv = pendingLifePoints / (double)maxLifePoints * 100d;
                return rv >= maskValue;
            case 'W':
                return target.WasTeleportedInInvalidCellThisTurn(fightContext);
            case 'Z':
                return target.PlayerType == PlayerType.Sidekick && target.Breed == maskValue;
            case 'z':
                return target.PlayerType != PlayerType.Sidekick || target.Breed != maskValue;
            case 'o':
            case 'O':
                return referenceTarget != null && target.Id == referenceTarget.Id;
            case 'h':
                return true;
            case 'l':
                return target.PlayerType == PlayerType.Human && !target.Data.IsSummon() || target.PlayerType == PlayerType.Sidekick;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the input string's first character (or second if the first is an asterisk) is one of the specified conditions.
    /// </summary>
    /// <param name="input">The input string to check.</param>
    /// <returns>True if the condition is met; otherwise, false.</returns>
    public static bool MaskIsOneOfCondition(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return false;
        }

        var firstRelevantChar = input[0] == '*' ? input[1] : input[0];

        return firstRelevantChar is 'B' or 'F' or 'Z';
    }

    public static bool IsInstantaneousSpellEffect(HaxeSpellEffect spellEffect)
    {
        return spellEffect.Triggers.Contains("I") && spellEffect.Delay <= 0;
    }
}