using System.Text.Json;
using System.Xml;
using BubbleBot.FrigostAdapter.Models;

var accounts = File.ReadAllLines("accounts.txt");

var accountsList = new List<(string username, string password)>();

foreach (var account in accounts)
{
    var split = account.Split(':');
    accountsList.Add((split[0], split[1]));
}

var proxies = File.ReadAllLines("proxies.txt");

var proxiesList = new List<(string ip, int port)>();

foreach (var proxy in proxies)
{
    var prox = proxy;
    if(proxy.StartsWith("socks5://"))
    {
        prox = proxy.Replace("socks5://", "");
    }
    
    var split = prox.Split(':');
    proxiesList.Add((split[0], int.Parse(split[1])));
}

var settingsList = new List<Settings>();

var randomNumber = Random.Shared.Next(10000, 99999);
foreach (var account in accountsList)
{
    // take random proxy
    var proxy = proxiesList[new Random().Next(0, proxiesList.Count)];
    
    settingsList.Add(new Settings
    {
        Icon = ":/app.ico",
        Alias = randomNumber.ToString(),
        Account = account.username,
        Password = account.password,
        Network = "",
        Proxy = "ON",
        ProxyIp = proxy.ip,
        ProxyPort = proxy.port.ToString(),
        ProxyUsername = "test",
        ProxyPassword = "test",
        ProxyType = "SOCKS5",
        ConfortSettings = new ConfortSettings
        {
            TeamNumber = 2,
            LockFps = false,
            LockFpsValue = 60,
            AutoAcceptExchange = true,
            AutoAcceptParty = true,
            AutoAcceptDungeon = true,
            AutoAcceptDelay = new MinMax()
            {
                Min = 500,
                Max = 1000
            },
            AutoSwitchExchange = 1,
            HidePlayers = false,
            HidePlayersExceptMine = false,
            HideMonsters = false,
            HideNpc = false,
            AutoSwitch = false,
            AutoSwitchButton = 481053114416,
            AutoSwitchNext = false,
            AutoSwitchNextButton = 167520501780,
            AutoSwitchPrevious = false,
            AutoSwitchPreviousButton = 158930567186,
            AutoFollow = true,
            AutoFollowButton = 1,
            AutoFollowDelay = new MinMax
            {
                Min = 300,
                Max = 600
            },
            AutoClick = true,
            AutoClickButton = 2,
            AutoInvite = true,
            AutoInviteButton = 305076895815,
            ToggleFightBot = false,
            ToggleFightBotButton = 300781928518,
            ToggleLosCalculator = false,
            ToggleLosCalculatorButton = 326551732300,
            SpeedAnimation = true,
            SpeedAnimationMultiplier = 8d,
            ToggleTactical = 0,
            DisableFightPopup = false,
            DisableLevelUpPopup = false,
            AutoSwitchTurn = true,
            AutoSwitchFightEnd = false,
            AutoPassTurn = false,
            AutoPassTurnDelay = new MinMax
            {
                Min = 0,
                Max = 0
            },
            AutoReadyType = 0,
            AutoReadyDelay = new MinMax
            {
                Min = 0,
                Max = 0
            },
            AutoJoin = false,
            AutoJoinDelay = new MinMax(),
            BlockSpectators = false,
            BlockSpectatorsDelay = new MinMax(),
            BlockFightAccess = false,
            BlockFightAccessDelay = new MinMax(),
            AutoMyTurnLosCalculator = false,
            DisableByClickLosCalculator = false,
            CountHarebourgSimulator = false,
            CountHarebourgSimulatorTargetButton = 0,
            CountHarebourgSimulatorMoveButton = 0
        },
        BotSettings = new BotSettings
        {
            FightSettings = new FightSettings
            {
                Enabled = false,
                KickOthers = false,
                KickOthersDelay = new MinMax(),
                Placement = 0,
                PlacementDelay = new MinMax(),
                PlayTurnAfter = new MinMax(),
                ResumeTurnAfter = new MinMax(),
                PassTurnAfter = new MinMax(),
                Casters = new List<object>()
            }
        }
    });
}

File.WriteAllText("accounts.json", JsonSerializer.Serialize(settingsList, new JsonSerializerOptions
{
    WriteIndented = true
}));