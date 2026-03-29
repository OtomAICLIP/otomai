namespace OtomAI.Bot.Services.Clients.Contracts;

/// <summary>
/// Client-specific logging interface. Mirrors Bubble.D3.Bot's IClientLogger.
/// </summary>
public interface IClientLogger
{
    void LogInfo(string message, params object[] args);
    void LogWarning(string message, params object[] args);
    void LogError(string message, params object[] args);
}
