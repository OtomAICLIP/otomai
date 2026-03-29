using System.Runtime.InteropServices;

namespace BubbleBot.Cli.Services.Maps;

[StructLayout(LayoutKind.Explicit)]
public readonly struct MapObjectPosition
{
    public MapObjectPosition(ulong position) => Position = position;

    public MapObjectPosition(uint mapId, short cellId, byte orientation)
    {
        MapId       = mapId;
        CellId      = cellId;
        Orientation = orientation;
    }

    [FieldOffset(0)] public readonly uint MapId;
    [FieldOffset(4)] public readonly short CellId;
    [FieldOffset(6)] public readonly byte Orientation;

    [FieldOffset(0)] public readonly ulong Position;

    public ulong ToPosition() => Position;
}