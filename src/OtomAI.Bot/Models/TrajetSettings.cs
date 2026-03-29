namespace OtomAI.Bot.Models;

/// <summary>
/// Route/trajet configuration. Mirrors Bubble.D3.Bot's TrajetSettings.
/// </summary>
public sealed class TrajetSettings
{
    public string Name { get; set; } = "";
    public List<TrajetStep> Steps { get; set; } = [];
    public int MinMonsterLevel { get; set; }
    public int MaxMonsterLevel { get; set; }
    public int MinGroupSize { get; set; } = 1;
    public int MaxGroupSize { get; set; } = 8;
    public bool FightOnPath { get; set; } = true;
}

public sealed class TrajetStep
{
    public long MapId { get; set; }
    public int CellId { get; set; }
    public string? Action { get; set; } // "fight", "gather", "bank", "zaap"
    public int Direction { get; set; }
}
