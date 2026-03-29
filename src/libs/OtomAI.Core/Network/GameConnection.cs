using System.Buffers;
using System.Net;
using System.Net.Sockets;
using Serilog;

namespace OtomAI.Core.Network;

/// <summary>
/// Raw TCP connection to a Dofus 3.0 game server.
/// Frames: [VarInt32 length][Protobuf payload]
/// </summary>
public sealed class GameConnection : IAsyncDisposable
{
    private readonly TcpClient _tcp = new();
    private NetworkStream? _stream;
    private readonly byte[] _readBuffer = new byte[8192];
    private readonly MemoryStream _frameBuffer = new();
    private CancellationTokenSource? _cts;

    public bool IsConnected => _tcp.Connected;
    public event Func<ReadOnlyMemory<byte>, Task>? OnMessage;
    public event Action<Exception>? OnDisconnected;

    public async Task ConnectAsync(string host, int port, CancellationToken ct = default)
    {
        await _tcp.ConnectAsync(host, port, ct);
        _stream = _tcp.GetStream();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _ = ReadLoopAsync(_cts.Token);
        Log.Information("Connected to {Host}:{Port}", host, port);
    }

    public async Task SendAsync(ReadOnlyMemory<byte> payload, CancellationToken ct = default)
    {
        if (_stream is null) throw new InvalidOperationException("Not connected");

        // Frame: VarInt32 length prefix + payload
        var lenBytes = EncodeVarInt32(payload.Length);
        await _stream.WriteAsync(lenBytes, ct);
        await _stream.WriteAsync(payload, ct);
        await _stream.FlushAsync(ct);
    }

    private async Task ReadLoopAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested && _stream is not null)
            {
                int bytesRead = await _stream.ReadAsync(_readBuffer, ct);
                if (bytesRead == 0) break;

                _frameBuffer.Write(_readBuffer, 0, bytesRead);
                await ProcessFramesAsync();
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            OnDisconnected?.Invoke(ex);
        }
    }

    private async Task ProcessFramesAsync()
    {
        _frameBuffer.Position = 0;
        while (_frameBuffer.Position < _frameBuffer.Length)
        {
            long startPos = _frameBuffer.Position;

            if (!TryReadVarInt32(_frameBuffer, out int frameLen))
            {
                _frameBuffer.Position = startPos;
                break;
            }

            long remaining = _frameBuffer.Length - _frameBuffer.Position;
            if (remaining < frameLen)
            {
                _frameBuffer.Position = startPos;
                break;
            }

            var payload = new byte[frameLen];
            _frameBuffer.ReadExactly(payload);

            if (OnMessage is not null)
                await OnMessage(payload);
        }

        // Compact the buffer
        long unread = _frameBuffer.Length - _frameBuffer.Position;
        if (unread > 0)
        {
            var temp = _frameBuffer.GetBuffer().AsSpan((int)_frameBuffer.Position, (int)unread).ToArray();
            _frameBuffer.SetLength(0);
            _frameBuffer.Write(temp);
        }
        else
        {
            _frameBuffer.SetLength(0);
        }
    }

    private static bool TryReadVarInt32(Stream stream, out int value)
    {
        value = 0;
        int shift = 0;
        while (shift < 35)
        {
            int b = stream.ReadByte();
            if (b < 0) return false;
            value |= (b & 0x7F) << shift;
            if ((b & 0x80) == 0) return true;
            shift += 7;
        }
        return false;
    }

    private static byte[] EncodeVarInt32(int value)
    {
        var buf = new List<byte>(5);
        uint v = (uint)value;
        while (v >= 0x80)
        {
            buf.Add((byte)(v | 0x80));
            v >>= 7;
        }
        buf.Add((byte)v);
        return buf.ToArray();
    }

    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        if (_stream is not null)
            await _stream.DisposeAsync();
        _tcp.Dispose();
        _frameBuffer.Dispose();
    }
}
