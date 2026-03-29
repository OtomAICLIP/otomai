namespace OtomAI.Bot.Client;

/// <summary>
/// Character display info. Mirrors Bubble.D3.Bot's CharacterInfo.
/// </summary>
public sealed class CharacterInfo
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public int Level { get; set; }
    public int BreedId { get; set; }
    public int CellId { get; set; }
    public int Direction { get; set; }
    public bool IsRiding { get; set; }
}
