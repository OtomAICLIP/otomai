using Bubble.Core.Datacenter.Attributes;

namespace Bubble.Core.Datacenter.Datacenter.Effects;

[DatacenterObject("Core.DataCenter.Metadata.Effect", "Effects", "Ankama.Dofus.Core.DataCenter", nameof(Id))]
public partial class Effects : IDofusRootObject
{
    public static string FileName => "data_assets_effectsroot.asset.bundle";
    
    public required int Id { get; set; }
    
    [DatacenterPropertyText]
    public required int DescriptionId { get; set; }
    
    public required int IconId { get; set; }
    
    public required int Characteristic { get; set; }
    
    public required int Category { get; set; }
    
    public required string CharacteristicOperator { get; set; }
    
    public required bool ShowInTooltip { get; set; }
    
    public required bool UseDice { get; set; }
    
    public required bool ForceMinMax { get; set; }
    
    public required bool Boost { get; set; }
    
    public required bool Active { get; set; }
    
    public required int OppositeId { get; set; }
    
    public required string TheoreticalDescriptionId { get; set; }
    
    public required int TheoreticalPattern { get; set; }
    
    public required bool ShowInSet { get; set; }
    
    public required sbyte BonusType { get; set; }
    
    public required bool UseInFight { get; set; }
    
    public required int EffectPriority { get; set; }
    
    public required float EffectPowerRate { get; set; }
    
    public required int ElementId { get; set; }
    
    public required bool IsInPercent { get; set; }
    
    public required bool HideValueInTooltip { get; set; }
    
    public required int TextIconReferenceId { get; set; }
    public required int EffectTriggerDuration { get; set; }
    
    public required List<int> ActionFiltersIds { get; set; } 
}