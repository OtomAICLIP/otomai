using System.Runtime.InteropServices;

namespace Bubble.DamageCalculation.Customs;

[StructLayout(LayoutKind.Sequential)]
public struct FighterCellLight
{
    public readonly long Id;
    public readonly short CellId;
    
    public FighterCellLight(long id, short cellId)
    {
        Id     = id;
        CellId = cellId;
    }
}