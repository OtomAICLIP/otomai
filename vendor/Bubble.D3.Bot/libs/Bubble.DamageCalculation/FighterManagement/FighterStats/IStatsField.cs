
namespace Bubble.DamageCalculation.FighterManagement.FighterStats;

public interface IStatsField
{
    StatId Characteristic { get; }
    long Base { get; set; }
    long Equipped { get; set; }
    long Given { get; set; }
    long Context { get; set; }
    long Additional { get; set; }
    long Total { get; }

    /// <summary>
    ///     TotalSafe can't be lesser than 0
    /// </summary>
    long TotalSafe { get; }

    long TotalWithoutContext { get; }
}