using BubbleBot.Cli.Services.Fight;

namespace BubbleBot.Cli.Services.Clients.Contracts;

public interface ISpellCasterContext
{
    FightActor? Fighter { get; set; }
    FightInfo? FightInfo { get; set; }
    bool IsPlayerBreed();
}
