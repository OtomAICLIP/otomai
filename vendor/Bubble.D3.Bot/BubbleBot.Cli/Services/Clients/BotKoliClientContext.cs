using BubbleBot.Cli.Models;

namespace BubbleBot.Cli.Services.Clients;

internal sealed class BotKoliClientContext : BotClientContextBase
{
    public BotKoliClientContext(string          botId,
                                string          hwid,
                                int             serverId,
                                string          serverName,
                                BotClient       botController,
                                SaharachAccount account,
                                string          token,
                                BotSettings     settings)
        : base(botId, hwid, serverId, serverName)
    {
        BotController = botController;
        Account = account;
        Token = token;
        Settings = settings;
    }

    public BotClient BotController { get; }
    public SaharachAccount Account { get; set; }
    public string Token { get; }
    public BotSettings Settings { get; }
    public KoliRuntimeState State { get; } = new();
}
