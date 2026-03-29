using System.Collections.Concurrent;
using BubbleBot.Cli.Services.Parties;

namespace BubbleBot.Cli.Repository;

public class PartyManager
{
    public ConcurrentDictionary<int, PartyInfo> Parties { get; } = new();
    public HashSet<long> AvailablePlayers { get; } = new();
    
    public PartyInfo? GetParty(int partyId)
    {
        return Parties.TryGetValue(partyId, out var party) ? party : null;
    }
    
    public void AddParty(PartyInfo party)
    {
        Parties.TryAdd(party.Id, party);
    }
    
    public PartyInfo CreateOrAddMember(int partyId, long member)
    {
        if (Parties.TryGetValue(partyId, out var party))
        {
            party.Members.Add(member);
        }
        else
        {
            AddParty(new PartyInfo()
            {
                Id = partyId,
                Leader = member,
                Members = [member]
            });
        }
        
        AvailablePlayers.Remove(member);
        return Parties[partyId];
    }

    public long GetAvailablePlayerId()
    {
        // randomly take one
        return Random.Shared.GetItems(AvailablePlayers.ToArray(), 1).FirstOrDefault();
    }
}