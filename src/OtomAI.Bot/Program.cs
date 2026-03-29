using OtomAI.Bot.Client;
using OtomAI.Bot.Repository;
using OtomAI.Bot.Repository.Maps;
using OtomAI.Bot.Services;
using OtomAI.Bot.TreasureHunts;
using OtomAI.Datacenter;
using OtomAI.Protocol.Services;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File("logs/otomai-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

Log.Information("OtomAI Bot v0.2.0");
Log.Information("Dofus 3.0 bot framework - aligned with Bubble.D3.Bot architecture");

// Initialize data repositories
var dataPath = "Data";
var protoService = new ProtoService();
protoService.LoadMappings(Path.Combine(dataPath, "game_mappings.json"));

MapRepository.Instance.Load(dataPath);
TrajetRepository.Instance.Load(dataPath);

var datacenter = new DatacenterService();
// datacenter.LoadFromAssetBundles(dofusInstallPath); // TODO: detect install path

var cluesSolver = new CluesSolver();
cluesSolver.LoadClues(dataPath);

// Load accounts
var accountService = new AccountService();
var accounts = accountService.LoadAccounts();

// TODO: Start bots via BotManager
// foreach (var account in accounts)
//     await BotManager.Instance.StartBotAsync(account);

Log.Information("Architecture aligned. Systems ready:");
Log.Information("  - Client: BotClient (login) -> BotGameClient (game) -> BotKoliClient (koli)");
Log.Information("  - Services: Transport, Verification, Session, Workflow, Navigation, Travel");
Log.Information("  - Handlers: Session, Fight, Inventory, World, Party, Arena (chain-of-responsibility)");
Log.Information("  - Repositories: Map, Item, Spell, Server, Alliance, Effect, Party, Trajet");
Log.Information("  - Fight AI: FightInfo, FightActor, SpellWrapper, Zones, LoS");
Log.Information("  - World pathfinding: A* on WorldGraph (vertex/edge model)");
Log.Information("  - Datacenter: Breed, Item, Spell, Monster, Job, Effect, WorldGraph models");

Log.Information("Next steps:");
Log.Information("  1. Extract protobuf message definitions from Dofus Unity client");
Log.Information("  2. Wire up message handlers to protocol messages");
Log.Information("  3. Build datacenter loader for Unity asset bundles");
Log.Information("  4. Test connection flow against live servers");

await Task.Delay(100);
Log.CloseAndFlush();
