namespace Bubble.DamageCalculation.Tools;

public class Interval
{
    public Interval(int min, int max)
    {
        Min = min;
        Max = max;
    }

    public int Min { get; set; }
    public int Max { get; set; }

    public int TotalReduced { get; set; }

    public Interval SubInterval(Interval interval)
    {
        Min -= interval.Min;
        Max -= interval.Max;

        return this;
    }

    public Interval AddInterval(Interval interval)
    {
        Min += interval.Min;
        Max += interval.Max;

        return this;
    }

    public Interval Add(int add)
    {
        Min += add;
        Max += add;

        return this;
    }

    public Interval SetToZero()
    {
        Min = 0;
        Max = 0;

        return this;
    }

    public Interval MultiplyInterval(Interval interval)
    {
        Min *= interval.Min;
        Max *= interval.Max;

        return this;
    }

    public Interval Multiply(double number)
    {
        Min = (int)Math.Floor(Min * number);
        Max = (int)Math.Floor(Max * number);

        return this;
    }

    public Interval MinimizeByInterval(Interval interval)
    {
        if (Min < interval.Min)
        {
            Min = interval.Min;
        }

        if (Max < interval.Max)
        {
            Max = interval.Max;
        }

        return this;
    }

    public Interval MinimizeBy(int min)
    {
        if (Min < min)
        {
            Min = min;
        }

        if (Max < min)
        {
            Max = min;
        }

        return this;
    }

    public Interval MaximizeByInterval(Interval interval)
    {
        if (Min > interval.Min)
        {
            Min = interval.Min;
        }

        if (Max > interval.Max)
        {
            Max = interval.Max;
        }

        return this;
    }

    public Interval MaximizeBy(int max)
    {
        if (Min > max)
        {
            Min = max;
        }

        if (Max > max)
        {
            Max = max;
        }

        return this;
    }

    public Interval Abs()
    {
        if (Min < 0)
        {
            Min = -Min;
        }

        if (Max < 0)
        {
            Max = -Max;
        }

        return this;
    }

    public bool IsZero()
    {
        return Min == 0 && Max == 0;
    }

    public Interval IncreaseByPercent(int percent)
    {
        return Multiply((100d + percent) / 100d);
    }

    public Interval DecreaseByPercent(int percent)
    {
        return Multiply((100d - percent) / 100d);
    }

    public virtual Interval Copy()
    {
        return new Interval(Min, Max);
    }
}