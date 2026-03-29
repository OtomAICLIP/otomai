using System.Numerics;
using System.Security.Cryptography;

namespace OtomAI.Core.Crypto;

/// <summary>
/// Diffie-Hellman key exchange for server verification.
/// Mirrors Bubble.D3.Bot's DH challenge-response flow:
/// Server sends p, g, publicKey -> Client computes shared secret.
/// </summary>
public sealed class DiffieHellmanHelper
{
    private BigInteger _privateKey;
    private BigInteger _p;
    private BigInteger _g;

    public byte[] PublicKey { get; private set; } = [];

    public void Initialize(byte[] p, byte[] g)
    {
        _p = new BigInteger(p, isUnsigned: true, isBigEndian: true);
        _g = new BigInteger(g, isUnsigned: true, isBigEndian: true);

        var privBytes = RandomNumberGenerator.GetBytes(32);
        _privateKey = new BigInteger(privBytes, isUnsigned: true, isBigEndian: true);

        var pub = BigInteger.ModPow(_g, _privateKey, _p);
        PublicKey = pub.ToByteArray(isUnsigned: true, isBigEndian: true);
    }

    public byte[] ComputeSharedSecret(byte[] serverPublicKey)
    {
        var serverPub = new BigInteger(serverPublicKey, isUnsigned: true, isBigEndian: true);
        var shared = BigInteger.ModPow(serverPub, _privateKey, _p);
        return shared.ToByteArray(isUnsigned: true, isBigEndian: true);
    }
}
