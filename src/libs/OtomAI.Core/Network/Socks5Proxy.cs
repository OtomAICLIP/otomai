using System.Net;
using System.Net.Sockets;
using System.Text;
using Serilog;

namespace OtomAI.Core.Network;

/// <summary>
/// SOCKS5 proxy connector for routing game traffic through residential IPs.
/// </summary>
public static class Socks5Proxy
{
    public static async Task<TcpClient> ConnectAsync(
        string proxyHost, int proxyPort,
        string targetHost, int targetPort,
        string? username = null, string? password = null,
        CancellationToken ct = default)
    {
        var tcp = new TcpClient();
        await tcp.ConnectAsync(proxyHost, proxyPort, ct);
        var stream = tcp.GetStream();

        // Auth method negotiation
        bool hasAuth = username is not null && password is not null;
        byte[] greeting = hasAuth
            ? [0x05, 0x02, 0x00, 0x02] // NoAuth + UserPass
            : [0x05, 0x01, 0x00];      // NoAuth only
        await stream.WriteAsync(greeting, ct);

        var response = new byte[2];
        await stream.ReadExactlyAsync(response, ct);

        if (response[0] != 0x05)
            throw new InvalidOperationException("Invalid SOCKS5 response");

        if (response[1] == 0x02 && hasAuth)
        {
            await AuthenticateAsync(stream, username!, password!, ct);
        }
        else if (response[1] != 0x00)
        {
            throw new InvalidOperationException($"SOCKS5 auth method {response[1]} not supported");
        }

        // Connect request
        var hostBytes = Encoding.ASCII.GetBytes(targetHost);
        var request = new byte[7 + hostBytes.Length];
        request[0] = 0x05; // Version
        request[1] = 0x01; // Connect
        request[2] = 0x00; // Reserved
        request[3] = 0x03; // Domain name
        request[4] = (byte)hostBytes.Length;
        hostBytes.CopyTo(request, 5);
        request[^2] = (byte)(targetPort >> 8);
        request[^1] = (byte)(targetPort & 0xFF);
        await stream.WriteAsync(request, ct);

        var reply = new byte[10];
        await stream.ReadExactlyAsync(reply.AsMemory(0, 4), ct);
        if (reply[1] != 0x00)
            throw new InvalidOperationException($"SOCKS5 connect failed: status {reply[1]}");

        // Read remaining address bytes based on type
        int addrLen = reply[3] switch
        {
            0x01 => 4,  // IPv4
            0x04 => 16, // IPv6
            0x03 => throw new NotSupportedException("Domain reply not expected"),
            _ => throw new InvalidOperationException($"Unknown address type {reply[3]}")
        };
        await stream.ReadExactlyAsync(new byte[addrLen + 2], ct); // addr + port

        Log.Debug("SOCKS5 tunnel established to {Host}:{Port} via {Proxy}:{ProxyPort}",
            targetHost, targetPort, proxyHost, proxyPort);

        return tcp;
    }

    private static async Task AuthenticateAsync(
        NetworkStream stream, string username, string password, CancellationToken ct)
    {
        var userBytes = Encoding.ASCII.GetBytes(username);
        var passBytes = Encoding.ASCII.GetBytes(password);
        var auth = new byte[3 + userBytes.Length + passBytes.Length];
        auth[0] = 0x01; // Sub-negotiation version
        auth[1] = (byte)userBytes.Length;
        userBytes.CopyTo(auth, 2);
        auth[2 + userBytes.Length] = (byte)passBytes.Length;
        passBytes.CopyTo(auth, 3 + userBytes.Length);
        await stream.WriteAsync(auth, ct);

        var response = new byte[2];
        await stream.ReadExactlyAsync(response, ct);
        if (response[1] != 0x00)
            throw new InvalidOperationException("SOCKS5 authentication failed");
    }
}
