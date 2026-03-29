// See https://aka.ms/new-console-template for more information

using System.Collections.Concurrent;
using System.Text.Json;
using Bubble.Core.Datacenter;
using Bubble.Core.Network.Proxy;
using Bubble.Shared;
using BubbleBot.Cli;
using BubbleBot.Cli.Models;
using BubbleBot.Cli.Repository;
using BubbleBot.Cli.Repository.Maps;
using BubbleBot.Cli.Services;
using BubbleBot.Cli.Services.Fight;
using BubbleBot.Cli.Services.Maps.World;
using BubbleBot.Cli.Services.TreasureHunts;
using BubbleBot.Cli.Services.TreasureHunts.Models;
using Serilog;
using Serilog.Sinks.FastConsole;
using Spectre.Console;

Log.Logger = new LoggerConfiguration()
             .Enrich.FromLogContext()
             .MinimumLevel.Debug()
             .WriteTo.File("logs/bot-logs.log", rollingInterval: RollingInterval.Day)
             .CreateLogger();

var logger2 = new LoggerConfiguration()
              .Enrich.FromLogContext()
              .MinimumLevel.Debug()
              .WriteTo.Console()
              .WriteTo.File("logs/bot-stats.log", rollingInterval: RollingInterval.Day)
              .CreateLogger();

DatacenterService.Initialize(await GetDofusPathAsync() ??
                             throw new Exception("Impossible de trouver le chemin de Dofus"));

logger2.Information("Chargement des comptes");
AccountService.Instance.LoadAccounts();

if (!AccountService.Instance.SaharachAccounts.Any(x => x.ToLoad))
{
    logger2.Error("Il n'y à aucun compte à charger");
    return;
}

logger2.Information("Initialisation des repositories");
logger2.Information("Initialisation des items");
ItemRepository.Instance.Initialize();
logger2.Information("Initialisation des serveurs");
ServerRepository.Instance.Initialize();
logger2.Information("Initialisation des protos");
ProtoService.Instance.Initialize();
logger2.Information("Initialisation des maps");
MapRepository.Instance.Initialize();
logger2.Information("Initialisation des sorts");
SpellRepository.Instance.Initialize();
logger2.Information("Initialisation des effets");
EffectRepository.Instance.Initialize();
logger2.Information("Initialisation des indices de chasses");
await CluesSolver.Instance.Initialize();
logger2.Information("Initialisation du WorldPathFinder");
WorldPathFinderService.Instance.Initialize();
DamageCalculationTranslator.Instance.Initialize();
AllianceRepository.Instance.Initialize();

logger2.Information("Connexion des comptes");
_ = Task.Run(async () =>
{
    foreach (var account in AccountService.Instance.SaharachAccounts.Where(x => x.ToLoad))
    {
        var retiesCount = 0;

        while (retiesCount < 6)
        {
            try
            {
                Log.Logger.Information("Chargement du compte {account} using proxy {Proxy}",
                                       account.Username,
                                       account.Proxy);
                await BotManager.Instance.AddBot(account.Username);
                BotManager.Instance.AccountsErrors.Remove(account.Username);
                break;
            }
            catch (Exception e)
            {
                Log.Logger.Error(e, "Error while adding bot {account}", account.Username);
                BotManager.Instance.AccountsErrors.Add(account.Username);
                retiesCount++;
            }
        }
    }
});


var reconnectingQueue = new ConcurrentQueue<string>();


_ = Task.Run(async () =>
{
    while (true)
    {
        try
        {
            foreach (var error in BotManager.Instance.AccountsErrors)
            {
                if (reconnectingQueue.Contains(error))
                {
                    continue;
                }

                reconnectingQueue.Enqueue(error);
            }

            foreach (var client in BotManager.Instance.GetClients().OrderBy(x => x.Key))
            {
                var bot = client.Value.GameClient;

                if (bot == null && DateTime.UtcNow - client.Value.LastReconnect > TimeSpan.FromMinutes(1))
                {
                    reconnectingQueue.Enqueue(client.Key);
                    continue;
                }

                if (bot == null)
                {
                    continue;
                }

                if (bot.IsConnected)
                {
                    if (bot.Map == null)
                    {
                        bot.MapNullCount++;

                        if (bot.MapNullCount > 5)
                        {
                            bot.DisconnectAsync();
                            bot.IsStopped = true;
                        }
                    }
                    
                    if(bot.LastMessageReceived != DateTime.MinValue &&
                       DateTime.UtcNow - bot.LastMessageReceived > TimeSpan.FromSeconds(35))
                    {            
                        logger2.Information("Bot {bot} is disconnected because of inactivity 4", bot.BotId);
                        bot.DisconnectAsync();
                        bot.IsStopped = true;
                        continue;
                    }

                    if (!bot.IsBank && !bot.ZaapMode && !bot.IsKoli && bot.Trajet == null)
                    {
                        if (!bot.NeedEmptyToBank &&
                            bot.LastFlagRequest != DateTime.MinValue &&
                            DateTime.UtcNow - bot.LastFlagRequest > TimeSpan.FromMinutes(5) &&
                            DateTime.UtcNow - bot.LastReconnect > TimeSpan.FromMinutes(5))
                        {
                            logger2.Information("Bot {bot} is disconnected because of inactivity", bot.BotId);
                            // ça fait 5min qu'on est fou rien donc il faudrait commencer à faire un truc hein
                            bot.TreasureHuntData?.GiveUp(GiveUpReason.MapNotFound);
                            bot.DisconnectAsync();
                        }
                    }

                    if (!bot.IsInFight && bot.Trajet != null)
                    {
                        if (bot.LastMapChange != DateTime.MinValue &&
                            DateTime.UtcNow - bot.LastMapChange > TimeSpan.FromSeconds(120))
                        { 
                            if(!bot.IsWaitingPartyMembers && !bot.IsTrajetReady)
                            {              
                                logger2.Information("Bot {bot} is disconnected because of inactivity 2", bot.BotId);
                                bot.DisconnectAsync();
                                continue;
                            }
                        }

                        if (bot.LastMapChange != DateTime.MinValue &&
                            DateTime.UtcNow - bot.LastMapChange > TimeSpan.FromSeconds(15))
                        {
                            bot.DoWork();
                            continue;
                        }
                    }

                    if (!bot.IsInFight && bot.IsKoli)
                    {
                        if (BotGameClient.DebugAutoFight)
                            continue;

                        bot.DoKoliMode();
                        if ((bot.KoliFightDones == 0 ||
                             bot.LastFight != DateTime.MinValue &&
                             DateTime.UtcNow - bot.LastFight > TimeSpan.FromMinutes(10)) &&
                            DateTime.UtcNow - bot.LastReconnect > TimeSpan.FromMinutes(10))
                        {
                            if (bot.IsInFight)
                                continue;
                            
                            logger2.Information("Bot {bot} is disconnected because of inactivity 3", bot.BotId); 
                            bot.DisconnectAsync();
                            continue;
                        }

                        // si l'heure actuel == xxH05 // xxH10 // xxH25 // xxH30 // xxH35 // xxH40
                        if (DateTime.UtcNow.Minute % 5 == 0 && DateTime.UtcNow.Second < 10)
                        {
                            if (bot.IsInFight)
                                continue;

                            if (bot.IsKoliReady)
                            {
                                bot.KoliRegister();
                            }
                        }
                    }
                }

                if (!bot.IsConnected && !bot.IsConnecting && !bot.IsDisconnectionPlanned)
                {
                    reconnectingQueue.Enqueue(bot.BotId);
                }
            }
        }
        catch (Exception e)
        {
            logger2.Error(e, "Error in bot manager loop");
        }

        await Task.Delay(10000);
    }
});

var reconnectingHistory = new ConcurrentDictionary<string, DateTime>();

_ = Task.Run(async () =>
{
    while (true)
    {
        try
        {
            while (reconnectingQueue.TryDequeue(out var botName))
            {
                if (reconnectingHistory.TryGetValue(botName, out var lastReconnect) &&
                    DateTime.UtcNow - lastReconnect < TimeSpan.FromMinutes(1))
                {
                    continue;
                }

                reconnectingHistory[botName] = DateTime.UtcNow;

                if (BotManager.Instance.AccountsErrors.Contains(botName))
                {
                    try
                    {
                        await BotManager.Instance.AddBot(botName);
                        BotManager.Instance.AccountsErrors.Remove(botName);
                        continue;
                    }
                    catch
                    {
                        // ignored
                        continue;
                    }
                }

                if (BotManager.Instance.Clients.TryGetValue(botName, out var v) &&
                    DateTime.UtcNow - v.LastReconnect < TimeSpan.FromMinutes(1))
                {
                    continue;
                }

                if (BotManager.Instance.GetGameClients().TryGetValue(botName, out var bot))
                {
                    if (bot.IsConnected || bot.IsConnecting || bot.IsDisconnectionPlanned)
                    {
                        continue;
                    }

                    await BotManager.Instance.AddBot(botName);
                }
                else
                {
                    await BotManager.Instance.AddBot(botName);
                }
            }
        }
        catch (Exception e)
        {
            logger2.Error(e, "Error in bot reconnection loop");
        }

        await Task.Delay(1000);
    }
});

try { ClearVisibleRegion(); } catch (IOException) { /* no console in SSH sessions */ }

var table = new Table()
            .Border(TableBorder.MinimalHeavyHead)
            .Centered()
            .Title("BubbleBot - Stats")
            .AddColumn(new TableColumn("[bold]Status[/]").Centered().Width(16))
            .AddColumn(new TableColumn("[bold]Bot[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]State[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Position[/]").Centered())
            .AddColumn(new TableColumn("[bold]Level[/]").Centered())
            .AddColumn(new TableColumn("[bold]Inventaire[/]").LeftAligned())
            .AddColumn(new TableColumn("[bold]Dernière update[/]").Centered())
            .BorderColor(Color.White)
            .RoundedBorder()
    ;

var lastConsoleSize = 0;
try { lastConsoleSize = Console.WindowWidth + Console.WindowHeight; } catch { }

try
{
await AnsiConsole.Live(table)
                 .AutoClear(false)                    // Do not remove when done
                 .Overflow(VerticalOverflow.Ellipsis) // Show ellipsis when overflowing
                 .StartAsync(async ctx =>
                 {
                     var test = 0;

                     while (true)
                     {
                         if (test % 600 == 0) // It runs every 200ms so this happens every 200ms * 600 = 2 minutes
                         {
                             try { ClearVisibleRegion(); } catch { }
                             test = 0;
                         }

                         try
                         {
                         if (lastConsoleSize != Console.WindowWidth + Console.WindowHeight)
                         {
                             lastConsoleSize = Console.WindowWidth + Console.WindowHeight;
                             try { ClearVisibleRegion(); } catch { }
                         }
                         } catch { }

                         table.Title($"BubbleBot - Stats - {BotManager.Instance.GetClients().Count} clients");

                         var count = table.Rows.Count;
                         for (var i = 0; i < count; i++)
                         {
                             table.RemoveRow(0);
                         }

                         foreach (var error in BotManager.Instance.AccountsErrors)
                         {
                             if (reconnectingQueue.Contains(error))
                             {
                                 table.AddRow(
                                     "Reconnecting",
                                     error,
                                     "?",
                                     "?",
                                     "?",
                                     "?");
                             }
                             else
                             {
                                 table.AddRow(
                                     "[bold red]Error[/]",
                                     error,
                                     "?",
                                     "?",
                                     "?",
                                     "?");
                             }
                         }


                         var totalKamasByServer = new Dictionary<string, long>();
                         var totalRosesByServer = new Dictionary<string, long>();
                         var totalKolizetonsByServer = new Dictionary<string, long>();

                         foreach (var client in BotManager.Instance.GetClients().Values
                                                          .OrderBy(x => x.BotId))
                         {
                             var gameClient = client.GameClient;
                             if (gameClient == null)
                             {
                                 if (reconnectingQueue.Contains(client.BotId))
                                 {
                                     table.AddRow(
                                         "Reconnecting",
                                         client.BotId,
                                         $"?",
                                         "?",
                                         $"?",
                                         "?");
                                     test++;

                                     continue;
                                 }

                                 var statusAuth = client.IsConnected ? "Connexion Auth" : "Offline";

                                 if (client.Banned)
                                 {
                                     statusAuth = "Banned";
                                 }

                                 table.AddRow(
                                     statusAuth,
                                     client.BotId,
                                     $"?",
                                     "?",
                                     $"?",
                                     "?");
                                 test++;

                                 continue;
                             }

                             var status = "[bold red]Offline[/]";

                             if (gameClient.IsDisconnectionPlanned)
                             {
                                 status = "[bold slateblue1]End[/]";
                                 if (gameClient.IsAtDailyLimit)
                                 {
                                     status = "[bold slateblue1]Daily Limit[/]";
                                 }
                             }
                             else if (gameClient.IsConnecting)
                             {
                                 status = "[bold yellow]Connecting[/]";
                             }
                             else if (gameClient.IsConnected)
                             {
                                 status = "[bold green]Online[/]";

                                 if (gameClient.IsInFight)
                                 {
                                     status = "[bold fuchsia]Fighting[/]";
                                 }

                                 if (!gameClient.IsBank && !gameClient.ZaapMode)
                                 {
                                     if (gameClient.NeedEmptyToBank)
                                     {
                                         status = "[bold yellow4_1]Emptying[/]";
                                     }
                                 }
                             }

                             if (reconnectingQueue.Contains(client.BotId))
                             {
                                 status = "Reconnecting";
                             }

                             if (!client.IsConnected &&
                                 !client.IsConnecting &&
                                 !gameClient.IsConnected &&
                                 !gameClient.IsConnecting &&
                                 !gameClient.IsDisconnectionPlanned &&
                                 client.LastReconnect != DateTime.MinValue &&
                                 DateTime.UtcNow - client.LastReconnect < TimeSpan.FromMinutes(1))
                             {
                                 var timeRemaining = TimeSpan.FromMinutes(1) - (DateTime.UtcNow - client.LastReconnect);
                                 // only show seconds
                                 status = "Will reconnect (" + timeRemaining.ToString("ss") + ")";
                             }

                             var clientKey = client.BotId;
                             var subClientKey = "";
                             if (client.BotId.Contains('@'))
                             {
                                 clientKey = client.BotId.Split("@")[0];
                                 // on prend les 4 premiers caractères
                                 if (clientKey.Length > 6)
                                     clientKey = clientKey[..Math.Min(clientKey.Length, 6)] + "...";

                                 if (client.GameClient?.IsBank == true)
                                 {
                                     subClientKey += " ([bold]Banque[/])";
                                 }

                                 if (client.GameClient?.ZaapMode == true)
                                 {
                                     subClientKey += " ([bold]Zaap[/])";
                                 }
                             }

                             var objectifValue =
                                 $"[bold blue]{gameClient.TreasureHuntData.ChassesSuccess}/{gameClient.TreasureHuntData.ChassesDones}[/]";

                             var secondaryObjectifValue =
                                 $"Step: {gameClient.TreasureHuntData.CurrentCheckpoint}/{gameClient.TreasureHuntData.TotalCheckpoint}";

                             if (!gameClient.IsAtDailyLimit &&
                                 gameClient.ZaapMode == false &&
                                 !gameClient.IsBank &&
                                 !gameClient.IsKoli)
                             {
                                 if (gameClient.TreasureHuntData.State == TreasureHuntState.HuntFinished)
                                 {
                                     secondaryObjectifValue = "Fini";
                                 }
                                 else if (gameClient.TreasureHuntData.State == TreasureHuntState.NoHuntActive)
                                 {
                                     secondaryObjectifValue = "Retour Malle";
                                 }
                             }

                             if (gameClient.IsKoli)
                             {
                                 secondaryObjectifValue = "";
                                 if (gameClient.ArenaStatus)
                                 {
                                     objectifValue = "[bold purple]Inscrit[/]";
                                     objectifValue += $" ({gameClient.KoliFightDones} cbts)";
                                 }
                                 else if (gameClient.CooldownTime > 0)
                                 {
                                     var breed = gameClient.Info.Information?.CharacterLook.BreedId ?? 0;
                                     objectifValue =
                                         $"[bold yellow]Koli ban {gameClient.CooldownTime}[/]min ({(BreedId)breed})";
                                 }
                                 else
                                 {
                                     objectifValue = "[bold cyan2]En attente[/]";
                                     secondaryObjectifValue = $"{gameClient.KoliFightDones} cbts";
                                 }


                                 if (gameClient.AgainstBot != string.Empty)
                                 {
                                     secondaryObjectifValue += $" & {gameClient.WithBot} vs {gameClient.AgainstBot}";
                                 }

                                 if (BotGameClient.DebugAutoFight)
                                 {
                                     objectifValue = $"[bold yellow]{gameClient.FightTotalCount} cbts[/]";
                                     secondaryObjectifValue = $"";
                                 }
                             }

                             if (gameClient.IsAtDailyLimit)
                             {
                                 secondaryObjectifValue = "";
                             }
                             
                             if (gameClient.Trajet != null)
                             {
                                 objectifValue =
                                     $"{gameClient.Account.Trajet} - [bold yellow]{gameClient.FightTotalCount} cbts[/]";
                                 var leader = string.Empty;
                                 if (gameClient.Party != null)
                                 {
                                     leader = BotManager.Instance.GetBotByCharacterId(gameClient.Party.Leader)?.Info.Name;
                                 }
                                 secondaryObjectifValue = "P: " + (leader ?? "");
                             }


                             if (!string.IsNullOrEmpty(secondaryObjectifValue))
                                 objectifValue += $" ({secondaryObjectifValue})";

                             var inventory =
                                 $"[bold blue]{BotGameClient.FormatKamas(gameClient.GetRosesAmountInInventory(), false)}[/] RdS - [bold blue]{BotGameClient.FormatKamas(gameClient.Inventory.Kamas, false)}[/] K";

                             if (gameClient.Trajet != null)
                             {
                                 inventory = "";
                                 var itemsToDisplay = gameClient.Trajet.ItemsToKeep;
                                 if (itemsToDisplay.Length == 0)
                                 {
                                     // affiche les kamas
                                     inventory =
                                         $"[bold blue]{BotGameClient.FormatKamas(gameClient.Inventory.Kamas, false)}[/] K";
                                 }
                                 
                                 foreach (var item in gameClient.Inventory.Items.Values)
                                 {
                                     if (itemsToDisplay.Contains((int)item.Item.Item.Gid))
                                     {
                                         inventory +=
                                             $"x{item.Item.Item.Quantity}[bold green] {item.Template.Name}[/] ";
                                     }
                                 }
                             }
                             
                             totalKamasByServer[gameClient.ServerName] =
                                 totalKamasByServer.GetValueOrDefault(gameClient.ServerName) +
                                 gameClient.Inventory.Kamas;

                             totalRosesByServer[gameClient.ServerName] =
                                 totalRosesByServer.GetValueOrDefault(gameClient.ServerName) +
                                 gameClient.GetRosesAmountInInventory();

                             totalKolizetonsByServer[gameClient.ServerName] =
                                 totalKolizetonsByServer.GetValueOrDefault(gameClient.ServerName) +
                                 gameClient.GeKolizetonAmountInInventory();

                             if (gameClient.IsBank)
                             {
                                 objectifValue = inventory;
                                 inventory = "x";
                             }

                             if (gameClient.IsKoli)
                             {
                                 inventory =
                                     $"[bold blue]{BotGameClient.FormatKamas(gameClient.GeKolizetonAmountInInventory(), false)}[/] Klz - [bold blue]{BotGameClient.FormatKamas(gameClient.Inventory.Kamas, false)}[/] K";
                             }

                             if (gameClient.ZaapMode)
                             {
                                 objectifValue =
                                     $"[bold blue]{gameClient.CurrentZaapIndex}/{gameClient.MapsWithZaap.Count}[/] Zaap";
                             }

                             table.AddRow(
                                 status,
                                 $"{clientKey} - [bold]{gameClient.ServerPrefix} {gameClient.Info.Name}[/] {subClientKey}",
                                 objectifValue,
                                 $"[yellow]{gameClient.Map?.Data.PosX},{gameClient.Map?.Data.PosY}[/]",
                                 (gameClient.Info.Information?.Level ?? 0).ToString(),
                                 inventory,
                                 gameClient.LastMapChange.ToString("HH:mm:ss"));
                         }

                         table.AddEmptyRow();

                         foreach (var server in totalKamasByServer)
                         {
                             table.AddRow(
                                 "Total",
                                 server.Key,
                                 "",
                                 "",
                                 "",
                                 $"[bold blue]{BotGameClient.FormatKamas(totalRosesByServer.GetValueOrDefault(server.Key), false)}[/] RdS - " +
                                 $"[bold blue]{BotGameClient.FormatKamas(totalKolizetonsByServer.GetValueOrDefault(server.Key), false)}[/] Klz - " +
                                 $"[bold blue]{BotGameClient.FormatKamas(server.Value, false)}[/] K");
                         }

                         ctx.Refresh();

                         test++;
                         await Task.Delay(200);
                     }
                 });
}
catch (IOException)
{
    // No interactive console (SSH session) — skip dashboard, just wait
    Log.Logger.Information("No interactive console detected, running in headless mode.");
}

await Task.Delay(-1);

static async Task<string?> GetDofusPathAsync()
{
    const string basePath = @"C:\Users\Administrator\Documents\downloader\dofus\dofus3";
    var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    var confPath = Path.Combine(appData, "zaap", "repositories", "production", "dofus", "dofus3", "release.json");

    if (!File.Exists(confPath))
    {
        return basePath;
    }

    var conf = await File.ReadAllTextAsync(confPath);

    try
    {
        var confJson = JsonSerializer.Deserialize<GameRelease>(conf);
        if (string.IsNullOrEmpty(confJson?.Location))
        {
            return basePath;
        }

        return confJson.Location;
    }
    catch
    {
        return basePath;
    }
}

static void ClearVisibleRegion()
{
    Console.Clear();
}