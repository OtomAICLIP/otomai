using Bubble.Shared.Protocol;
using BubbleBot.Cli.Models;
using BubbleBot.Cli.Repository.Maps;
using BubbleBot.Cli.Services.Fight;
using BubbleBot.Cli.Services.Parties;

namespace BubbleBot.Cli.Services.Clients.Contracts;

public interface IFightClientContext : IClientLogger, ISpellCasterContext
{
    List<SpellWrapper> Spells { get; }
    long PlayerId { get; }
    CharacterInfo Info { get; set; }
    Map? Map { get; set; }
    bool IsInFight { get; set; }
    bool AutoPass { get; set; }
    PartyInfo? Party { get; }
    TrajetSettings? Trajet { get; }

    Task SendRequestWithDelay(IProtoMessage             message,
                              string                    typeUrl,
                              int                       delay,
                              Predicate<IProtoMessage>? predicate = null);

    void SendRequest(IProtoMessage message, string typeUrl, bool setUid = false);
    void SetIsAgainstBot(string fighterName);
    void SetWithBot(string fighterName);
}
