namespace OtomAI.Core;

/// <summary>
/// Circular byte buffer for network frame reassembly.
/// Mirrors Bubble.Core's CircularBuffer: wraps around a fixed-size array,
/// supporting efficient append/consume without repeated allocation.
/// </summary>
public sealed class CircularBuffer
{
    private byte[] _buffer;
    private int _head;
    private int _tail;

    public CircularBuffer(int capacity = 65536)
    {
        _buffer = new byte[capacity];
    }

    public int Length
    {
        get
        {
            int len = _tail - _head;
            return len >= 0 ? len : len + _buffer.Length;
        }
    }

    public int Capacity => _buffer.Length;
    public int FreeSpace => _buffer.Length - Length - 1;

    public void Write(ReadOnlySpan<byte> data)
    {
        if (data.Length > FreeSpace)
            Grow(Length + data.Length);

        int firstChunk = Math.Min(data.Length, _buffer.Length - _tail);
        data[..firstChunk].CopyTo(_buffer.AsSpan(_tail));
        if (firstChunk < data.Length)
            data[firstChunk..].CopyTo(_buffer);

        _tail = (_tail + firstChunk + (data.Length - firstChunk)) % _buffer.Length;
    }

    public int Read(Span<byte> destination)
    {
        int toRead = Math.Min(destination.Length, Length);
        if (toRead == 0) return 0;

        int firstChunk = Math.Min(toRead, _buffer.Length - _head);
        _buffer.AsSpan(_head, firstChunk).CopyTo(destination);
        if (firstChunk < toRead)
            _buffer.AsSpan(0, toRead - firstChunk).CopyTo(destination[firstChunk..]);

        _head = (_head + toRead) % _buffer.Length;
        return toRead;
    }

    public byte Peek(int offset)
    {
        if (offset >= Length) throw new IndexOutOfRangeException();
        return _buffer[(_head + offset) % _buffer.Length];
    }

    public void Discard(int count)
    {
        count = Math.Min(count, Length);
        _head = (_head + count) % _buffer.Length;
    }

    public void Clear()
    {
        _head = 0;
        _tail = 0;
    }

    private void Grow(int minCapacity)
    {
        int newCap = Math.Max(_buffer.Length * 2, minCapacity + 1);
        var newBuf = new byte[newCap];
        int len = Length;
        Read(newBuf);
        _buffer = newBuf;
        _head = 0;
        _tail = len;
    }
}
