using Bubble.Core.Datacenter.Attributes;

namespace Bubble.Core.Datacenter.Datacenter.Notifications;

[DatacenterObject("Core.DataCenter.Metadata.Notification", "Notifications", "Ankama.Dofus.Core.DataCenter", nameof(Id))]
public sealed partial class Notifications : IDofusRootObject
{
    public static string FileName => "data_assets_notificationsroot.asset.bundle";

    public required int Id { get; set; }
    
    public required string TitleId { get; set; } = string.Empty;
    public required string MessageId { get; set; } = string.Empty;
    
    public required int IconId { get; set; }
    public required int TypeId { get; set; }
    
    public required string Trigger { get; set; } = string.Empty;
    public required bool CantBeClosed { get; set; }
}