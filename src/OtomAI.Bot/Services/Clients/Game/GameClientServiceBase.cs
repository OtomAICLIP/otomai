using OtomAI.Bot.Client;
using OtomAI.Bot.Client.Context;

namespace OtomAI.Bot.Services.Clients.Game;

/// <summary>
/// Abstract base class for game services providing property delegates to BotGameClient state.
/// Mirrors Bubble.D3.Bot's GameClientServiceBase with 60+ property delegates.
/// Avoids passing the full client to every method - services access state via base properties.
/// </summary>
public abstract class GameClientServiceBase
{
    protected BotGameClient Client { get; }
    protected BotGameClientContext Context => Client.Context;
    protected GameRuntimeState State => Context.RuntimeState;

    // Common state delegates
    protected long CharacterId => State.CharacterId;
    protected string CharacterName => State.CharacterName;
    protected int Level => State.Level;
    protected long CurrentMapId => State.CurrentMapId;
    protected int CurrentCellId => State.CurrentCellId;
    protected bool InFight => State.InFight;
    protected int ActionPoints => State.ActionPoints;
    protected int MovementPoints => State.MovementPoints;
    protected int Kamas => State.Kamas;
    protected bool IsMoving => State.IsMoving;
    protected int ServerId => Context.ServerId;
    protected bool InTreasureHunt => State.InTreasureHunt;
    protected bool InHavenBag => State.InHavenBag;

    protected GameClientServiceBase(BotGameClient client)
    {
        Client = client;
    }
}
