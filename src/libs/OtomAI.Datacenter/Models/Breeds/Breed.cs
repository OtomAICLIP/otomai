using OtomAI.Datacenter.Attributes;

namespace OtomAI.Datacenter.Models.Breeds;

/// <summary>
/// Character breed/class definition. Mirrors Bubble.Core.Datacenter's Breed model.
/// </summary>
[DatacenterObject("Breeds")]
public sealed class Breed
{
    public int Id { get; set; }
    public string ShortName { get; set; } = "";
    public string LongName { get; set; } = "";
    public string Description { get; set; } = "";
    public int MaleColors { get; set; }
    public int FemaleColors { get; set; }
    public int[] SpellsId { get; set; } = [];
    public BreedRole[] Roles { get; set; } = [];

    // Stat growth curves
    public int[][] StatsPointsForStrength { get; set; } = [];
    public int[][] StatsPointsForIntelligence { get; set; } = [];
    public int[][] StatsPointsForChance { get; set; } = [];
    public int[][] StatsPointsForAgility { get; set; } = [];
    public int[][] StatsPointsForVitality { get; set; } = [];
    public int[][] StatsPointsForWisdom { get; set; } = [];
}

public sealed class BreedRole
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
}
