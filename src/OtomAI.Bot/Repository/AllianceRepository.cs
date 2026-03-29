namespace OtomAI.Bot.Repository;

/// <summary>
/// Alliance data repository (singleton). Mirrors Bubble.D3.Bot's AllianceRepository.
/// </summary>
public sealed class AllianceRepository
{
    private static readonly Lazy<AllianceRepository> _instance = new(() => new AllianceRepository());
    public static AllianceRepository Instance => _instance.Value;

    private readonly Dictionary<int, AllianceRecord> _alliances = [];
    private AllianceRepository() { }

    public void Register(AllianceRecord alliance) => _alliances[alliance.Id] = alliance;
    public AllianceRecord? Get(int id) => _alliances.GetValueOrDefault(id);
}

public sealed class AllianceRecord
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Tag { get; set; } = "";
}
