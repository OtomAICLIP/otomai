using System.Text.Json;
using OtomAI.Bot.Models;
using Serilog;

namespace OtomAI.Bot.Services;

/// <summary>
/// Account loading from config files.
/// Mirrors Bubble.D3.Bot's AccountService: reads Zaap settings + accounts.json.
/// </summary>
public sealed class AccountService
{
    public List<AccountSettings> LoadAccounts(string path = "accounts.json")
    {
        if (!File.Exists(path))
        {
            Log.Warning("Accounts file not found: {Path}", path);
            return [];
        }

        var json = File.ReadAllText(path);
        var accounts = JsonSerializer.Deserialize<List<AccountSettings>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        Log.Information("Loaded {Count} accounts from {Path}", accounts.Count, path);
        return accounts;
    }
}
