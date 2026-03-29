using Serilog;

namespace OtomAI.Bot.Services;

/// <summary>
/// Ankama HAAPI authentication service (separate from OAuth).
/// Mirrors Bubble.D3.Bot's AnkamaService: token, certificate, API key management.
/// </summary>
public sealed class AnkamaService
{
    private readonly HttpClient _http = new();

    public async Task<string?> GetGameTokenAsync(string apiKey, int gameId = 102, CancellationToken ct = default)
    {
        Log.Debug("Requesting game token via HAAPI");
        // TODO: POST to HAAPI CreateToken endpoint
        // Returns game token used for IdentificationRequest
        await Task.CompletedTask;
        return null;
    }

    public async Task<string?> RefreshApiKeyAsync(string encryptedKeyData, CancellationToken ct = default)
    {
        Log.Debug("Refreshing HAAPI API key");
        // TODO: Decrypt keydata, check expiry, refresh if needed
        await Task.CompletedTask;
        return null;
    }
}
