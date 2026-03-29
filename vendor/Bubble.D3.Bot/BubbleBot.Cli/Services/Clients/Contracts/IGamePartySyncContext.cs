using BubbleBot.Cli.Services.Parties;

namespace BubbleBot.Cli.Services.Clients.Contracts;

public interface IGamePartySyncContext
{
    string BotId { get; }
    long PlayerId { get; }
    PartyInfo? Party { get; }
    void SynchronizeLeaderPosition();
}
