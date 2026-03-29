namespace OtomAI.Bot.Services.Clients.Contracts;

/// <summary>
/// Fight context interface. Mirrors Bubble.D3.Bot's IFightClientContext.
/// </summary>
public interface IFightClientContext
{
    bool InFight { get; }
    int FightTurn { get; }
    bool IsMyTurn { get; }
    int ActionPoints { get; }
    int MovementPoints { get; }
    int CurrentCellId { get; }
}
