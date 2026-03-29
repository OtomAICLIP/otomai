namespace Bubble.DamageCalculation.SpellManagement;

public class RandomGroup
{
    public double Weight { get; set; }
    public List<HaxeSpellEffect> Effects { get; set; }

    /// <summary>
    /// Initializes a new instance of the RandomGroup class.
    /// </summary>
    /// <param name="effects">A list of HaxeSpellEffect objects.</param>
    public RandomGroup(List<HaxeSpellEffect> effects)
    {
        Effects = effects;
        Weight  = 0;
        foreach (var effect in effects)
        {
            Weight += effect.RandomWeight;
        }
    }
    
    /// <summary>
    /// Calculates the total weight of the RandomGroups in the given dictionary.
    /// </summary>
    /// <param name="randomGroups">A dictionary containing RandomGroup objects.</param>
    /// <returns>Total weight of the RandomGroups.</returns>
    public static double TotalWeight(Dictionary<int, RandomGroup> randomGroups)
    {
        return randomGroups.Values.Sum(randomGroup => randomGroup.Weight);
    }

    /// <summary>
    /// Creates groups based on the given list of HaxeSpellEffect objects.
    /// </summary>
    /// <param name="effects">A list of HaxeSpellEffect objects.</param>
    /// <returns>A dictionary containing RandomGroup objects.</returns>
    public static Dictionary<int, RandomGroup> CreateGroups(List<HaxeSpellEffect> effects)
    {
        var groupDictionary = new Dictionary<int, RandomGroup>();
        var groupId         = 0;

        foreach (var effect in effects.Where(effect => effect.RandomWeight > 0))
        {
            if (effect.RandomGroup == 0)
            {
                effect.RandomGroup = --groupId;
            }

            var groupKey = effect.RandomGroup;
            if (groupDictionary.TryGetValue(groupKey, out var group))
            {
                group.Effects.Add(effect);
                group.Weight += effect.RandomWeight;
            }
            else
            {
                var newGroup = new RandomGroup(new List<HaxeSpellEffect> { effect, });
                groupDictionary[groupKey] = newGroup;
            }
        }

        return groupDictionary;
    }
    
    /// <summary>
    /// Selects a random group from the given Group dictionary.
    /// </summary>
    /// <param name="groupDictionary">A dictionary containing RandomGroup objects.</param>
    /// <returns>A list of HaxeSpellEffect objects from the selected random group.</returns>
    public static IList<HaxeSpellEffect> SelectRandomGroup(Dictionary<int, RandomGroup> groupDictionary)
    {
        var targetWeight = TotalWeight(groupDictionary) * Random.Shared.NextDouble();

        RandomGroup? selectedGroup = null;

        foreach (var randomGroup in groupDictionary.Values)
        {
            targetWeight -= randomGroup.Weight;
            if (targetWeight > 0)
            {
                continue;
            }

            selectedGroup = randomGroup;
            break;
        }

        return selectedGroup?.Effects ?? new List<HaxeSpellEffect>();
    }

    /// <summary>
    /// Adds an effect to the group and updates the group's weight.
    /// </summary>
    /// <param name="effect">The HaxeSpellEffect object to add.</param>
    public void AddEffect(HaxeSpellEffect effect)
    {
        Effects.Add(effect);
        Weight += effect.RandomWeight;
    }
}