namespace Bubble.Core.Datacenter.Datacenter.Spells;

[Flags]
public enum SpellLevelFlags : ushort
{
    CastInLine = 1,
    CastInDiagonal = 2,
    CastTestLos = 4,
    NeedFreeCell = 8,
    NeedTakenCell = 16,
    NeedFreeTrapCell = 32,
    RangeCanBeBoosted = 64,
    HideEffects = 128,
    Hidden = 256,
    PlayAnimation = 512,
    NeedVisibleEntity = 1024,
    NeedCellWithoutPortal = 2048,
    PortalProjectionForbidden = 4096
}