using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bubble.Core.Services;
using Bubble.Shared;
using BubbleBot.Cli.Models;
using Serilog;

namespace BubbleBot.Cli.Services;


public class AccountService : Singleton<AccountService>
{
    public List<AnkamaAccount> Accounts { get; set; } = new();
    public List<SaharachAccount> SaharachAccounts { get; set; } = new();

    public void LoadAccounts()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var accountsPath = Path.Combine(appDataPath, "zaap", "Settings");

        if (!File.Exists(accountsPath))
        {
            return;
        }

        var json = File.ReadAllText(accountsPath);
        var settings = JsonSerializer.Deserialize<SettingsFile>(json);

        if (settings == null)
        {
            return;
        }

        Accounts = settings.UserAccounts;

        var hwidList = HardwareService.GenerateRandomHwidAddresses();
        
        var saharachAccounts = GetSaharachAccounts();

        var index = 0;
        
        foreach (var account in Accounts)
        {
            var saharachAccount = saharachAccounts.FirstOrDefault(x => x.Id == account.Id);

            if (saharachAccount != null)
            {
                saharachAccount.Infos = account;
                if (!string.IsNullOrEmpty(account.Proxy))
                {
                    saharachAccount.Proxy = account.Proxy;
                }
                continue;
            }

            saharachAccount = new SaharachAccount
            {
                Id = account.Id,
                Username = account.Login,
                Infos = account,
                HardwareId = hwidList[index],
                Proxy = account.Proxy
            };

            index++;

            saharachAccounts.Add(saharachAccount);
        }

        saharachAccounts = saharachAccounts
                           .Where(x => Accounts.Any(u => u.Id == x.Id))
                           .ToList();

        SaharachAccounts = saharachAccounts;

        SaveSaharachAccounts();
    }

    private static List<SaharachAccount> GetSaharachAccounts()
    {
        var accountsPath = Path.Combine("accounts.json");

        if (!File.Exists(accountsPath))
        {
            return [];
        }

        var json = File.ReadAllText(accountsPath);

        return JsonSerializer.Deserialize<List<SaharachAccount>>(json)!;
    }

    public void SaveSaharachAccounts()
    {
        var accountsPath = "accounts.json";

        var json = JsonSerializer.Serialize(SaharachAccounts,
                                            new JsonSerializerOptions
                                            {
                                                WriteIndented = true,
                                            });

        if (!File.Exists(accountsPath))
        {
            Log.Logger.Information("Fichier accounts.json n'existe pas, création du fichier.");
        }

        File.WriteAllText(accountsPath, json);
    }

    public SaharachAccount? GetAccount(int id)
    {
        return SaharachAccounts.FirstOrDefault(x => x.Infos.Id == id);
    }

    public SaharachAccount? GetAccount(string username)
    {
        return SaharachAccounts.FirstOrDefault(x => x.Infos.Login == username);
    }
}
