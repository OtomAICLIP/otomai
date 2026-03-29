using Bubble.DamageCalculation;
using BubbleBot.Cli.Repository.Maps;

namespace BubbleBot.Cli.Services.Fight.Zones;

public class Square : ZRectangle
{
    public Square(uint minRadius, uint size, bool isDiagonalFree, Map map) : base(minRadius, size, isDiagonalFree, map)
    {
        Shape = isDiagonalFree ? SpellShape.W : SpellShape.X;
    }

    public uint Length => Width;
}