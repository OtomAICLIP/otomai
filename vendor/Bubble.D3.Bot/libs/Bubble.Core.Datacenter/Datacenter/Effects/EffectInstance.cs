using Bubble.Core.Datacenter.Attributes;

namespace Bubble.Core.Datacenter.Datacenter.Effects;

[Flags]
public enum EffectInstanceFlags : byte
{
    VisibleInTooltip = 1,
    VisibleInBuffUi = 2,
    VisibleInFightLog = 4,
    VisibleOnTerrain = 8,
    ForClientOnly = 16,
    Trigger = 32
}

[DatacenterObject("Core.DataCenter.Metadata.Effect.Instance", "EffectInstance", "Ankama.Dofus.Core.DataCenter", "0")]
public partial class EffectInstance : IDofusSubObject
{
    public bool VisibleInTooltip => (Flags & (ushort)EffectInstanceFlags.VisibleInTooltip) != 0;
    public bool VisibleInBuffUi => (Flags & (ushort)EffectInstanceFlags.VisibleInBuffUi) != 0;
    public bool VisibleInFightLog => (Flags & (ushort)EffectInstanceFlags.VisibleInFightLog) != 0;
    public bool VisibleOnTerrain => (Flags & (ushort)EffectInstanceFlags.VisibleOnTerrain) != 0;
    public bool ForClientOnly => (Flags & (ushort)EffectInstanceFlags.ForClientOnly) != 0;
    public bool Trigger => (Flags & (ushort)EffectInstanceFlags.Trigger) != 0;

    public required byte Flags { get; set; }

    public required int EffectUid { get; set; }

    public required short BaseEffectId { get; set; }

    public required ushort EffectId { get; set; }

    public required int Order { get; set; }

    public required short TargetId { get; set; }

    public required string TargetMask { get; set; }

    public required sbyte Duration { get; set; }

    public required float Random { get; set; }

    public required short Group { get; set; }

    public required short Modificator { get; set; }

    public required bool Dispellable { get; set; }

    public required byte Delay { get; set; }

    public required string Triggers { get; set; }

    public required sbyte EffectElement { get; set; }

    public required short SpellId { get; set; }

    public required int EffectTriggerDuration { get; set; }
    public required SpellZoneDescr ZoneDescription { get; set; }
}

[DatacenterObject("Core.DataCenter.Metadata.Effect.Instance", "EffectInstanceInteger", "Ankama.Dofus.Core.DataCenter", "0")]
public partial class EffectInstanceInteger : EffectInstance
{
    public required int Value { get; set; }
}

[DatacenterObject("Core.DataCenter.Metadata.Effect.Instance", "EffectInstanceDice", "Ankama.Dofus.Core.DataCenter", "0")]
public sealed partial class EffectInstanceDice : EffectInstanceInteger
{
    public required int DiceNum { get; set; }
    public required int DiceSide { get; set; }
    public required byte DisplayZero { get; set; }
}