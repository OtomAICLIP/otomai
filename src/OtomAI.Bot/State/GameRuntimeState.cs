namespace OtomAI.Bot.State;

/// <summary>
/// Centralized game state container.
/// Inspired by Bubble.D3.Bot's GameRuntimeState (85+ properties).
/// Add fields as protocol messages are decoded.
/// </summary>
public sealed class GameRuntimeState
{
    // Character
    public long CharacterId { get; set; }
    public string CharacterName { get; set; } = "";
    public int Level { get; set; }
    public long CurrentMapId { get; set; }
    public int CurrentCellId { get; set; }

    // Resources
    public int Kamas { get; set; }
    public int ActionPoints { get; set; }
    public int MovementPoints { get; set; }

    // Combat
    public bool InFight { get; set; }
    public int FightTurn { get; set; }

    // Server
    public int ServerId { get; set; }
    public string ServerName { get; set; } = "";
}
