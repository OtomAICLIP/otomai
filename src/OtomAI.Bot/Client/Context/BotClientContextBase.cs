using OtomAI.Core;
using OtomAI.Core.Crypto;

namespace OtomAI.Bot.Client.Context;

/// <summary>
/// Base context shared by all client types (login, game, koli).
/// Mirrors Bubble.D3.Bot's BotClientContextBase:
/// - Circular buffer for network framing
/// - DH crypto state
/// - Sync primitives
/// - UID generation
/// </summary>
public abstract class BotClientContextBase
{
    public CircularBuffer Buffer { get; } = new(65536);
    public DiffieHellmanHelper DiffieHellman { get; } = new();
    public Lock SyncRoot { get; } = new();
    public CancellationTokenSource Cts { get; set; } = new();

    private int _nextUid;
    public int NextUid() => Interlocked.Increment(ref _nextUid);

    public bool IsVerified { get; set; }
    public byte[]? SharedSecret { get; set; }
}
