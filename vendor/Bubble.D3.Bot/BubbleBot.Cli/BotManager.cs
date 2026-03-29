using System.Collections.Concurrent;
using System.Net;
using Bubble.Core.Network.Proxy;
using Bubble.Core.Services;
using BubbleBot.Cli.Services;
using Serilog;

namespace BubbleBot.Cli;

public class BotManager : Singleton<BotManager>
{
    public const bool NoLog = false;
    
    public readonly ConcurrentDictionary<string, BotClient> Clients = new();
    public readonly ConcurrentDictionary<string, BotGameClient> GameClients = new();
    public readonly HashSet<string> AccountsErrors = new();

    public HashSet<string> GetBotNames()
    {
        return [..GameClients.Select(x => $"{x.Key} ({x.Value.Info.Name})" +
                                          $" ({x.Value.TreasureHuntData.ChassesSuccess}/{x.Value.TreasureHuntData.ChassesDones})").ToArray()];
    }
    
    public bool IsBotName(string name)
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                                "BubbleBot",
                                $"{name}.txt");
        if (File.Exists(path))
        {
            return true;
        }
        
        return GameClients.Any(x => x.Value.Info.Name == name);
    }
    
    public ConcurrentDictionary<string, BotClient> GetClients()
    {
        return Clients;
    }
    
    public ConcurrentDictionary<string, BotGameClient> GetGameClients()
    {
        return GameClients;
    }
    
    public void UpdateConsoleTitle()
    {
        Console.Title = $@"BubbleBot - {Clients.Count} clients - {string.Join(" / ", GetBotNames())}";
    }
    
    public BotGameClient? GetBotByCharacterId(long characterId)
    {
        return GameClients.Values.FirstOrDefault(x => x.PlayerId == characterId);
    }
    
    public async Task AddBot(string username, BotSettings? settings = null)
    {
        if (Clients.TryGetValue(username, out var v) && DateTime.UtcNow - v.LastReconnect < TimeSpan.FromMinutes(1))
        {
            return;
        }

        AccountService.Instance.LoadAccounts();

        var ankamaHost = GetIpFromHost("dofus2-co-production.ankama-games.com");
        const int ankamaPort = 443;

        var account = AccountService.Instance.GetAccount(username);
        
        if(account == null || !account.ToLoad)
        {
            Log.Logger.Error("Account {id} not found", username);
            return;
        }
        
        var proxyIp = string.Empty;
        var proxyPort = 0;
        var proxyUsername = string.Empty;
        var proxyPassword = string.Empty;
        
        if (!string.IsNullOrEmpty(account.Proxy))
        {
            var proxyIpPort = account.Proxy.Split("://")[1].Split("@")[0];
            var proxyUserPw = account.Proxy.Split("://")[1].Split("@")[1];

            proxyIp = proxyIpPort.Split(":")[0];
            proxyPort = int.Parse(proxyIpPort.Split(":")[1]);
            
            // maybe there is no user and password
            if (proxyUserPw.Contains(':'))
            {
                proxyUsername = proxyUserPw.Split(":")[0];
                proxyPassword = proxyUserPw.Split(":")[1];
            }
            
            // if proxy is a domain we need to resolve it, we count the number of . to know
            // if it's a domain or an ip
            if (proxyIp.Count(x => x == '.') < 3)
            {
                proxyIp = GetIpFromHost(proxyIp);
            }
        }

        if (File.Exists("accounts.txt"))
        {
            var accounts = (await File.ReadAllLinesAsync("accounts.txt"));
            var accountIndex = accounts.Any(x => x.StartsWith(username, StringComparison.InvariantCultureIgnoreCase));

            if (!accountIndex)
            {
                Log.Logger.Error("Account {id} not found in account.txt", username);
                account.ToLoad = false;
                return;
            }
        }

        var (key, _, _, webProxy) =
            await AnkamaService.Instance.ConnectAsync(account.Username, proxyIp, proxyPort, proxyUsername, proxyPassword);
        
        if (settings == null)
        {
            settings = new BotSettings
            {
                Id = username,
                Account = account,
                ApiKey = key,
                Address = ankamaHost,
                Port = ankamaPort,
                ServerId = account.Server,
                Proxy = string.IsNullOrEmpty(account.Proxy) ? null 
                    : new Socks5Options(proxyIp, 
                                        proxyPort,
                                        ankamaHost,
                                        ankamaPort,
                                        proxyUsername, 
                                        proxyPassword),
                IsBank = account.IsBank,
                IsKoli = account.IsKoli,
                WebProxy = webProxy,
            };
        }
        else
        {
            settings.ApiKey = key;
        }

        var botClient = new BotClient(settings);

        if (Clients.TryGetValue(username, out var value))
        {
            value.Disconnect();
            value.Dispose();
            botClient.LastReconnect = value.LastReconnect;
        }
        
        botClient.ConnectAsync();
        Clients[username] = botClient;
    }

    public void AddGameBotClient(BotGameClient client)
    {
        if(GameClients.TryGetValue(client.BotId, out var value))
        { 
            client.TreasureHuntData.ChassesDones = value.TreasureHuntData.ChassesDones;
            client.TreasureHuntData.ChassesSuccess = value.TreasureHuntData.ChassesSuccess;
            client.ConnectedAt = value.ConnectedAt;
            client.Inventory = value.Inventory;
            client.LastFlagRequest = value.LastFlagRequest;
            client.ZaapMode = value.ZaapMode;
            client.CurrentZaapIndex = value.CurrentZaapIndex;
            client.LastZaapTaken = value.LastZaapTaken;
            value.IsStopped = true;
            value.Disconnect();
        }
        
        GameClients[client.BotId] = client;
        UpdateConsoleTitle();
    }

    public static string GetIpFromHost(string host)
    {
        var ips = Dns.GetHostAddresses(host);
        return ips[0].ToString();
    }
    
}