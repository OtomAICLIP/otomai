using System.Numerics;
using Bubble.Core;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Security;

namespace BubbleBot.Cli.Services.Clients;

internal abstract class BotClientContextBase
{
    protected BotClientContextBase(string botId, string hwid, int serverId, string serverName)
    {
        BotId = botId;
        Hwid = hwid;
        ServerId = serverId;
        ServerName = serverName;
        Cvld = new BigInteger(DHStandardGroups.rfc2409_768.P.ToByteArray());
        Cvle = new BigInteger(DHStandardGroups.rfc2409_768.Q.ToByteArray());
        Cvlf = new BigInteger(DHStandardGroups.rfc2409_768.G.ToByteArray());
    }

    public string BotId { get; }
    public string Hwid { get; }
    public int ServerId { get; }
    public string ServerName { get; }

    public int Uid { get; set; }
    public long CharacterId { get; set; }
    public long LastMessageSent { get; set; }
    public int LastRequestUid { get; set; }
    public int SequenceNumber { get; set; } = 1;

    public CircularBuffer Buffer { get; } = new(8192);
    public Lock SyncRoot { get; } = new();
    public SecureRandom SecureRandom { get; } = new();

    public bool IsWaitingForAnotherPacket { get; set; }
    public int? ExpectedLength { get; set; }
    public int ExpectedLengthSize { get; set; }

    public BigInteger Cvld { get; }
    public BigInteger Cvlc { get; }
    public BigInteger Cvlf { get; }
    public BigInteger Cvle { get; }
    public BigInteger? Cvlg { get; set; }
    public BigInteger Cvlh { get; set; }
}
