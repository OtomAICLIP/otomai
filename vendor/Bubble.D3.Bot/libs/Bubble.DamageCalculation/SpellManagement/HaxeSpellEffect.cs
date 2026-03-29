using System.Text;
using Bubble.Core.Datacenter.Datacenter.Effects;
using Bubble.DamageCalculation.Customs;
using Bubble.DamageCalculation.Tools;

namespace Bubble.DamageCalculation.SpellManagement;

public class HaxeSpellEffect
{
    public const int InvalidActionId = -1;

    public static readonly HaxeSpellEffect Empty = new(0, 1, 0, ActionId.Noop, 0, 0, 0, 0, false, "I", "", "", 0,
                                                       0, false, 0, 0,
                                                       true, 1, 0);


    public SpellZone Zone { get; set; }
    public string[] Triggers { get; set; }
    public string RawZone { get; set; }
    public double RandomWeight { get; set; }
    public int RandomGroup { get; set; }
    public int Param3 { get; set; }
    public int Param2 { get; set; }
    public int Param1 { get; set; }
    public int Order { get; set; }
    public string[] Masks { get; set; }
    public int Level { get; set; }
    public bool IsDispellable { get; set; }
    public bool IsCritical { get; set; }
    public int Id { get; set; }
    public int Duration { get; set; }
    public int Delay { get; set; }
    public int Category { get; set; }
    public ActionId ActionId { get; set; }
    public int TurnDuration { get; set; }

    public bool UseMinMax { get; set; }
    public int DispellableType { get; set; }
    public int ElementId { get; set; }


    private int _useCount = 0;

    public HaxeSpellEffect(int id, int level, int order, ActionId actionId, int param1, int param2, int param3,
        int duration, bool isCritical, string triggers, string rawZone, string masks, double randomWeight,
                           int randomGroup, bool isDispellable, int delay, int category, bool useMinMax,
                           int dispellableType, int elementId)
    {
        _useCount     = 0;
        Id            = id;
        Level         = level;
        Order         = order;
        ActionId      = actionId;
        Param1        = param1;
        Param2        = param2;
        Param3        = param3;
        Duration      = duration;
        IsCritical    = isCritical;
        IsDispellable = isDispellable;
        Triggers      = SpellManager.SplitTriggers(triggers).ToArray();
        RawZone       = rawZone;
        Masks         = SpellManager.SplitMasks(masks).ToArray();
        Array.Sort(Masks, SortMasks);
        RandomWeight    = randomWeight;
        RandomGroup     = randomGroup;
        Delay           = delay;
        Category        = category;
        Zone            = SpellZone.FromRawZone(rawZone);
        UseMinMax       = useMinMax;
        DispellableType = dispellableType;
        ElementId       = elementId;

        if (Duration == -1)
        {
            Duration = -1000;
        }
        
        TurnDuration = duration + delay;
    }

    public static int SortMasks(string mask1, string mask2)
    {
        const string maskCharacters = "*bBeEfFzZKoOPpTWUvVrRQq";

        if (maskCharacters.Contains(mask1[0]) && maskCharacters.Contains(mask2[0]))
        {
            if (mask1[0] == '*' && mask2[0] != '*')
            {
                return -1;
            }

            if (mask2[0] == '*' && mask1[0] != '*')
            {
                return 1;
            }

            return 0;
        }

        if (maskCharacters.Contains(mask1[0]))
        {
            return -1;
        }

        if (maskCharacters.Contains(mask2[0]))
        {
            return 1;
        }

        return 0;
    }

    public void ResetUseCount()
    {
        _useCount = 0;
    }

    public void RegisterUse()
    {
        if (!ActionIdHelper.SpellExecutionHasGlobalLimitation(ActionId))
        {
            return;
        }

        ++_useCount;
    }

    public bool IsRandom()
    {
        return RandomWeight > 0;
    }

    public bool IsAoe()
    {
        return Zone.Radius >= 1;
    }

    public bool HasReachedMaxUseLimit()
    {
        if (!ActionIdHelper.SpellExecutionHasGlobalLimitation(ActionId))
        {
            return false;
        }

        return _useCount >= Param3;
    }


    public int GetRandomRoll()
    {
        var minRoll   = GetEffectMinRoll();
        var maxRoll   = GetEffectMaxRoll();
        var number    = Random.Shared.NextDouble();
        var newNumber = number * (maxRoll - minRoll);
        return (int)Math.Floor(minRoll + newNumber + 0.5);
    }

    public int GetMinRoll()
    {
        return Param1 + Param3;
    }

    public int GetMaxRoll()
    {
        return Param1 * Param2 + Param3;
    }

    public int GetEffectMinRoll()
    {
        if (Param1 == 0 && Param2 == 0)
        {
            return Param3;
        }

        return Param1;
    }

    public int GetEffectMaxRoll()
    {
        return Param2 != 0 ? Param2 : GetEffectMinRoll();
    }
    
    public int GetElement()
    {
        return ElementsHelper.GetElementFromActionId(ActionId);
    }

    public Interval GetDamageInterval()
    {
        return new Interval(GetMinRoll(), GetMaxRoll());
    }

    public HaxeSpellEffect Clone()
    {
        var clone = new HaxeSpellEffect(Id, Level, Order, ActionId, Param1, Param2, Param3, Duration, IsCritical,
                                        "", RawZone, "", RandomWeight, RandomGroup, IsDispellable, Delay, Category,
                                        UseMinMax, DispellableType, ElementId)
        {
            Triggers = Triggers,
            Masks    = Masks,
        };

        return clone;
    }

    public void SetZoneFrom(SpellZoneDescr effZoneDescription)
    {
        var constructRawZone = new StringBuilder();
        constructRawZone.Append(effZoneDescription.Shape);
        constructRawZone.Append(effZoneDescription.Param1);
        
        Zone = SpellZone.FromRawZone(constructRawZone.ToString());
    }
}