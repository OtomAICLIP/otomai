using OtomAI.Datacenter.Attributes;

namespace OtomAI.Datacenter.Models.Monsters;

/// <summary>
/// Monster definitions. Mirrors Bubble.Core.Datacenter's Monster model set.
/// </summary>
[DatacenterObject("Monsters")]
public sealed class Monster
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Race { get; set; }
    public int[] Grades { get; set; } = [];
    public bool IsBoss { get; set; }
    public bool IsMiniBoss { get; set; }
    public bool IsQuestMonster { get; set; }
    public int FavoriteSubAreaId { get; set; }
    public int[] SpellsId { get; set; } = [];
    public int[] DropsId { get; set; } = [];
}

[DatacenterObject("MonsterGrades")]
public sealed class MonsterGrade
{
    public int MonsterId { get; set; }
    public int Grade { get; set; }
    public int Level { get; set; }
    public int LifePoints { get; set; }
    public int ActionPoints { get; set; }
    public int MovementPoints { get; set; }
    public int Strength { get; set; }
    public int Intelligence { get; set; }
    public int Chance { get; set; }
    public int Agility { get; set; }
    public int Wisdom { get; set; }
    public int EarthResistance { get; set; }
    public int FireResistance { get; set; }
    public int WaterResistance { get; set; }
    public int AirResistance { get; set; }
    public int NeutralResistance { get; set; }
}
