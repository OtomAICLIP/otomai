using Bubble.Core.Datacenter.Attributes;

namespace Bubble.Core.Datacenter.Datacenter.World;

[DatacenterObject("Core.DataCenter.Metadata.World", "MapPositions", "Ankama.Dofus.Core.DataCenter", nameof(Id))]
public sealed partial class MapPositions : IDofusRootObject
{
    public static string FileName => "data_assets_mappositionsroot.asset.bundle";
    
    public required MapPositionPlaylist PlaylistIds { get; set; }
    
    public required uint Flags { get; set; }
    public required string FightSnapshot { get; set; } = string.Empty;
    public required string RoleplaySnapshot { get; set; } = string.Empty;

    public required uint Id { get; set; }
    public required short PosX { get; set; }
    public required short PosY { get; set; }
    [DatacenterPropertyText]
    public required int NameId { get; set; }
    public required ushort SubAreaId { get; set; }
    public required sbyte WorldMap { get; set; }
    
    public required byte TacticalModeTemplateId { get; set; }
    
    
    
    public bool CapabilityAllowChallenge => (Flags & (uint)MapPositionFlags.CapabilityAllowChallenge) != 0;
    public bool CapabilityAllowAggression => (Flags & (uint)MapPositionFlags.CapabilityAllowAggression) != 0;
    public bool CapabilityAllowTeleportTo => (Flags & (uint)MapPositionFlags.CapabilityAllowTeleportTo) != 0;
    public bool CapabilityAllowTeleportFrom => (Flags & (uint)MapPositionFlags.CapabilityAllowTeleportFrom) != 0;
    public bool CapabilityAllowExchangesBetweenPlayers => (Flags & (uint)MapPositionFlags.CapabilityAllowExchangesBetweenPlayers) != 0;
    public bool CapabilityAllowHumanVendor => (Flags & (uint)MapPositionFlags.CapabilityAllowHumanVendor) != 0;
    public bool CapabilityAllowCollector => (Flags & (uint)MapPositionFlags.CapabilityAllowCollector) != 0;
    public bool CapabilityAllowSoulCapture => (Flags & (uint)MapPositionFlags.CapabilityAllowSoulCapture) != 0;
    public bool CapabilityAllowSoulSummon => (Flags & (uint)MapPositionFlags.CapabilityAllowSoulSummon) != 0;
    public bool CapabilityAllowTavernRegen => (Flags & (uint)MapPositionFlags.CapabilityAllowTavernRegen) != 0;
    public bool CapabilityAllowTombMode => (Flags & (uint)MapPositionFlags.CapabilityAllowTombMode) != 0;
    public bool CapabilityAllowTeleportEverywhere => (Flags & (uint)MapPositionFlags.CapabilityAllowTeleportEverywhere) != 0;
    public bool CapabilityAllowFightChallenges => (Flags & (uint)MapPositionFlags.CapabilityAllowFightChallenges) != 0;
    public bool CapabilityAllowMonsterRespawn => (Flags & (uint)MapPositionFlags.CapabilityAllowMonsterRespawn) != 0;
    public bool CapabilityAllowMonsterFight => (Flags & (uint)MapPositionFlags.CapabilityAllowMonsterFight) != 0;
    public bool CapabilityAllowMount => (Flags & (uint)MapPositionFlags.CapabilityAllowMount) != 0;
    public bool CapabilityAllowObjectDisposal => (Flags & (uint)MapPositionFlags.CapabilityAllowObjectDisposal) != 0;
    public bool CapabilityAllowUnderwater => (Flags & (uint)MapPositionFlags.CapabilityAllowUnderwater) != 0;
    public bool CapabilityAllowPvp1V1 => (Flags & (uint)MapPositionFlags.CapabilityAllowPvp1V1) != 0;
    public bool CapabilityAllowPvp3V3 => (Flags & (uint)MapPositionFlags.CapabilityAllowPvp3V3) != 0;
    public bool CapabilityAllowMonsterAggression => (Flags & (uint)MapPositionFlags.CapabilityAllowMonsterAggression) != 0;
    public bool AllCapabilitiesMask => (Flags & (uint)MapPositionFlags.AllCapabilitiesMask) != 0;
    public bool Outdoor => (Flags & (uint)MapPositionFlags.Outdoor) != 0;
    public bool ShowNameOnFingerpost => (Flags & (uint)MapPositionFlags.ShowNameOnFingerpost) != 0;
    public bool HasPriorityOnWorldmap => (Flags & (uint)MapPositionFlags.HasPriorityOnWorldmap) != 0;
    public bool AllowPrism => (Flags & (uint)MapPositionFlags.AllowPrism) != 0;
    public bool IsTransition => (Flags & (uint)MapPositionFlags.IsTransition) != 0;
    public bool MapHasTemplate => (Flags & (uint)MapPositionFlags.MapHasTemplate) != 0;
    public bool HasPublicPaddock => (Flags & (uint)MapPositionFlags.HasPublicPaddock) != 0;
}