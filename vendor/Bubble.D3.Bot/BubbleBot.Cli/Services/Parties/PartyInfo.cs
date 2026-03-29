namespace BubbleBot.Cli.Services.Parties;

public class PartyInfo
{
    public required int Id { get; set; }

    public required long Leader { get; set; }
    public required List<long> Members { get; set; } = new();
}