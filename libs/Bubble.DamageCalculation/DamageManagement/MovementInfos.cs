using Bubble.DamageCalculation.FighterManagement;

namespace Bubble.DamageCalculation.DamageManagement;

public class MovementInfos
{
    public MovementInfos(int newPos, int direction, HaxeFighter? swappedWith = null, bool fromPandawa = false, bool wasInvalid = false)
    {
        NewPosition = newPos;
        Direction   = direction;
        SwappedWith = swappedWith;
        FromPandawa = fromPandawa;
        WasInvalid  = wasInvalid;
    }

    public bool WasInvalid { get; set; }
    public HaxeFighter? SwappedWith { get; }
    public int NewPosition { get; }
    public int Direction { get; }
    public bool FromPandawa { get; set; }
}