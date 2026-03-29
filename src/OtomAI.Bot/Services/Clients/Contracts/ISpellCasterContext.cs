namespace OtomAI.Bot.Services.Clients.Contracts;

/// <summary>
/// Spell casting context. Mirrors Bubble.D3.Bot's ISpellCasterContext.
/// </summary>
public interface ISpellCasterContext
{
    Task CastSpellAsync(int spellId, int targetCellId, CancellationToken ct = default);
}
