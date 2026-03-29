namespace Bubble.Core.Datacenter.Datacenter.World;

[Flags]
public enum MapPositionFlags : uint
{
    CapabilityAllowChallenge = 1U,
    CapabilityAllowAggression = 2U,
    CapabilityAllowTeleportTo = 4U,
    CapabilityAllowTeleportFrom = 8U,
    CapabilityAllowExchangesBetweenPlayers = 16U,
    CapabilityAllowHumanVendor = 32U,
    CapabilityAllowCollector = 64U,
    CapabilityAllowSoulCapture = 128U,
    CapabilityAllowSoulSummon = 256U,
    CapabilityAllowTavernRegen = 512U,
    CapabilityAllowTombMode = 1024U,
    CapabilityAllowTeleportEverywhere = 2048U,
    CapabilityAllowFightChallenges = 4096U,
    CapabilityAllowMonsterRespawn = 8192U,
    CapabilityAllowMonsterFight = 16384U,
    CapabilityAllowMount = 32768U,
    CapabilityAllowObjectDisposal = 65536U,
    CapabilityAllowUnderwater = 131072U,
    CapabilityAllowPvp1V1 = 262144U,
    CapabilityAllowPvp3V3 = 524288U,
    CapabilityAllowMonsterAggression = 1048576U,
    AllCapabilitiesMask = 2097151U,
    Outdoor = 2097152U,
    ShowNameOnFingerpost = 4194304U,
    HasPriorityOnWorldmap = 8388608U,
    AllowPrism = 16777216U,
    IsTransition = 33554432U,
    MapHasTemplate = 67108864U,
    HasPublicPaddock = 134217728U
}