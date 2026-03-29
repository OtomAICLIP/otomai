namespace Bubble.DamageCalculation.DamageManagement;

public class SummonInfos
{
    public int Position { get; }
    public int Direction { get; }
    public int LookId { get; }

    public SummonInfos(int position, int direction, int lookId = 0)
    {
        Position  = position;
        Direction = direction;
        LookId    = lookId;
    }
}