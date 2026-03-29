namespace OtomAI.Bot.Fight;

/// <summary>
/// Entity in a fight (player, monster, summon).
/// Mirrors Bubble.D3.Bot's FightActor.
/// </summary>
public sealed class FightActor
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public int CellId { get; set; }
    public int TeamId { get; set; }
    public bool IsAlly { get; set; }
    public bool IsAlive { get; set; } = true;
    public bool IsSummon { get; set; }

    // Stats
    public int LifePoints { get; set; }
    public int MaxLifePoints { get; set; }
    public int ActionPoints { get; set; }
    public int MovementPoints { get; set; }
    public int Level { get; set; }
    public int BreedId { get; set; }

    // Resistances
    public int EarthResistance { get; set; }
    public int FireResistance { get; set; }
    public int WaterResistance { get; set; }
    public int AirResistance { get; set; }
    public int NeutralResistance { get; set; }

    public double LifePercent => MaxLifePoints > 0 ? (double)LifePoints / MaxLifePoints : 0;
}
