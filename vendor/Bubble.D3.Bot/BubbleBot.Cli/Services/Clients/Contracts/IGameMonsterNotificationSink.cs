namespace BubbleBot.Cli.Services.Clients.Contracts;

public interface IGameMonsterNotificationSink
{
    void LogArchiDiscord(string message);
}
