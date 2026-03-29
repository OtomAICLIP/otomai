using System.Drawing;

namespace BubbleBot.Cli.Utils;


public enum CompareAngle
{
    Like,
    Unlike,
    Clockwise,
    Counterclockwise,
}
public static class MathUtils
{
    public static double GetPositiveOrientedAngle(Point point1, Point point2, Point point3)
    {
        return CompareAngles(point1, point2, point3) switch
               {
                   CompareAngle.Like             => 0,
                   CompareAngle.Unlike           => Math.PI,
                   CompareAngle.Clockwise        => GetAngle(point1, point2, point3),
                   CompareAngle.Counterclockwise => 2 * Math.PI - GetAngle(point1, point2, point3),
                   _                             => 0,
               };
    }

    public static CompareAngle CompareAngles(Point point1, Point point2, Point point3)
    {
        var vector1     = ComputeVector(point1, point2);
        var vector2     = ComputeVector(point1, point3);
        var determinant = GetDeterminant(vector1, vector2);

        if (determinant != 0)
        {
            if (determinant > 0)
            {
                return CompareAngle.Clockwise;
            }

            return CompareAngle.Counterclockwise;
        }

        if (vector1.X >= 0 == vector2.X >= 0 && vector1.Y >= 0 == vector2.Y >= 0)
        {
            return CompareAngle.Like;
        }

        return CompareAngle.Unlike;
    }

    public static Point ComputeVector(Point point1, Point point2)
    {
        return new Point(point2.X - point1.X, point2.Y - point1.Y);
    }

    public static int GetDeterminant(Point point1, Point point2)
    {
        return point1.X * point2.Y - point1.Y * point2.X;
    }

    public static double GetDistanceBetweenPoints(Point point1, Point point2)
    {
        return Math.Sqrt(Math.Pow(point1.X - point2.X, 2) + Math.Pow((double)point1.Y - point2.Y, 2));
    }

    public static double GetAngle(Point point1, Point point2, Point point3)
    {
        var p1 = GetDistanceBetweenPoints(point2, point3);
        var p2 = GetDistanceBetweenPoints(point1, point2);
        var p3 = GetDistanceBetweenPoints(point1, point3);

        return Math.Acos((p2 * p2 + p3 * p3 - p1 * p1) / (2 * p2 * p3));
    }

    /// <summary>
    /// Rounds a given number to the specified number of decimal places.
    /// </summary>
    /// <param name="value">The number to be rounded.</param>
    /// <param name="precision">The number of decimal places to round to.</param>
    /// <returns>A rounded number with the specified number of decimal places.</returns>
    public static double RoundWithPrecision(double value, double precision)
    {
        value *= Math.Pow(10, precision);
        return Round(value) / Math.Pow(10, precision);
    }

    /// <summary>
    /// Rounds a given number with the default math behavior.
    /// </summary>
    /// <param name="value">The number to be rounded.</param>
    /// <returns>A rounded number.</returns>
    public static int Round(double value)
    {
        return (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }
    
}