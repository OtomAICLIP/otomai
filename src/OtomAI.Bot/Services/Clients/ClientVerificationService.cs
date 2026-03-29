using OtomAI.Bot.Client.Context;
using Serilog;

namespace OtomAI.Bot.Services.Clients;

/// <summary>
/// DH challenge-response server verification.
/// Mirrors Bubble.D3.Bot's ClientVerificationService:
/// Server sends DH params (p, g, publicKey) -> client computes shared secret.
/// </summary>
public sealed class ClientVerificationService
{
    private readonly BotClientContextBase _context;

    public ClientVerificationService(BotClientContextBase context)
    {
        _context = context;
    }

    /// <summary>
    /// Handle server verification challenge: compute DH shared secret and respond.
    /// </summary>
    public void HandleServerVerification(byte[] p, byte[] g, byte[] serverPublicKey)
    {
        _context.DiffieHellman.Initialize(p, g);
        _context.SharedSecret = _context.DiffieHellman.ComputeSharedSecret(serverPublicKey);
        _context.IsVerified = true;
        Log.Information("Server verification complete (DH key exchange)");
    }

    /// <summary>
    /// Get client public key to send back to server.
    /// </summary>
    public byte[] GetClientPublicKey() => _context.DiffieHellman.PublicKey;
}
