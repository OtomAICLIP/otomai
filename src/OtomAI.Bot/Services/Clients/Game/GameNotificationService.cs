using OtomAI.Bot.Client;
using Serilog;

namespace OtomAI.Bot.Services.Clients.Game;

/// <summary>
/// Discord logging, kamas/inventory reporting.
/// Mirrors Bubble.D3.Bot's GameNotificationService.
/// </summary>
public sealed class GameNotificationService : GameClientServiceBase
{
    public GameNotificationService(BotGameClient client) : base(client) { }

    public void NotifyLevelUp(int newLevel)
    {
        Log.Information("{Name} leveled up to {Level}!", CharacterName, newLevel);
        // TODO: Send Discord webhook if configured
    }

    public void NotifyFightResult(bool won, int xpGained, int kamasGained)
    {
        Log.Information("{Name} fight {Result}: +{XP} XP, +{Kamas} kamas",
            CharacterName, won ? "won" : "lost", xpGained, kamasGained);
    }

    public void NotifyItemDrop(string itemName, int quantity)
    {
        Log.Information("{Name} dropped {Qty}x {Item}", CharacterName, quantity, itemName);
    }

    public void NotifyError(string message)
    {
        Log.Error("{Name}: {Message}", CharacterName, message);
    }
}
