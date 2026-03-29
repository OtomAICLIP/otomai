namespace OtomAI.Protocol.Shared;

public class IpcStatus
{
    public ServerStatus ServerStatus { get; set; } = ServerStatus.Starting;
    public int PlayersCount { get; set; }
}
