namespace OtomAI.Bot.Repository;

/// <summary>
/// Party tracking. Mirrors Bubble.D3.Bot's PartyManager + PartyRepository.
/// </summary>
public sealed class PartyRepository
{
    private readonly Dictionary<long, PartyInfo> _parties = [];

    public void AddOrUpdate(PartyInfo party) => _parties[party.Id] = party;
    public void Remove(long partyId) => _parties.Remove(partyId);
    public PartyInfo? Get(long partyId) => _parties.GetValueOrDefault(partyId);
    public IEnumerable<PartyInfo> GetAll() => _parties.Values;
}

public sealed class PartyInfo
{
    public long Id { get; set; }
    public string Name { get; set; } = "";
    public long LeaderId { get; set; }
    public List<PartyMember> Members { get; set; } = [];
}

public sealed class PartyMember
{
    public long CharacterId { get; set; }
    public string Name { get; set; } = "";
    public int Level { get; set; }
    public int BreedId { get; set; }
    public long MapId { get; set; }
    public int LifePercent { get; set; }
}
