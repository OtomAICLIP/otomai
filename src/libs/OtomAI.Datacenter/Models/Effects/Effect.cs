using OtomAI.Datacenter.Attributes;

namespace OtomAI.Datacenter.Models.Effects;

/// <summary>
/// Effect definitions. Mirrors Bubble.Core.Datacenter's Effects model set.
/// </summary>
[DatacenterObject("Effects")]
public sealed class Effect
{
    public int Id { get; set; }
    public string Description { get; set; } = "";
    public int Category { get; set; }
    public bool IsBoost { get; set; }
    public bool Active { get; set; }
    public int Characteristic { get; set; }
    public bool ShowInTooltip { get; set; }
    public int ElementId { get; set; }
}
