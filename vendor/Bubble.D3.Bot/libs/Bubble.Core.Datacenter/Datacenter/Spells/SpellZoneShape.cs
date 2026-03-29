namespace Bubble.Core.Datacenter.Datacenter.Spells;

public enum SpellZoneShape : byte
{
    None,
    Point = 80,
    Circle = 67,
    OutsideCircle = 73,
    OutsideComplexCircle = 90,
    Ring = 79,
    HalfCircle = 85,
    Line = 76,
    DiagonalLine = 47,
    LineFromCaster = 108,
    PerpendicularLine = 84,
    DiagonalPerpendicularLine = 45,
    Cross = 88,
    CrossWithoutCenter = 81,
    DiagonalCross = 43,
    DiagonalCrossWithoutCenter = 35,
    Star = 42,
    Checkerboard = 68,
    Cone = 86,
    Boomerang = 66,
    Square = 71,
    SquareWithoutDiagonals = 87,
    Rectangle = 82,
    Fork = 70,
    WholeMap = 97,
    WholeMapWithTheDead = 65,
    Custom = 59,
    Empty = 32
}