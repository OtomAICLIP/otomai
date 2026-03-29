using System.Buffers;

namespace Bubble.Core;

public sealed class CircularBuffer
{
    private byte[] _buffer;
    private int _head;
    private int _size;
    private int _tail;

    public int Position =>
        _head;
    
    public int Capacity =>
        _buffer.Length;

    public int Size =>
        _size;

    public bool IsEmpty =>
        _size == 0;

    public bool IsFull =>
        _size == _buffer.Length;
    
    public byte[] Data =>
        _buffer;

    public int BaseCapacity { get; }
    public CircularBuffer(int capacity)
    {
        BaseCapacity = capacity;
        _buffer = new byte[capacity];
        _head = 0;
        _tail = 0;
        _size = 0;
        
        // clear the 10 first bytes of the buffer to avoid reading the var32 incorrectly
        for (var i = 0; i < 10; i++)
        {
            _buffer[i] = 0;
        }
    }

    public void Clear()
    {
        _head = 0;
        _tail = 0;
        _size = 0;
        
        // clear the 10 first bytes of the buffer to avoid reading the var32 incorrectly
        for (var i = 0; i < 10; i++)
        {
            _buffer[i] = 0;
        }
        
        // If the buffer is bigger than the original size, we recreate it
        if (_buffer.Length > BaseCapacity)
        {
            _buffer = new byte[BaseCapacity];
        }
    }

    public byte Peek()
    {
        if (IsEmpty)
            throw new OutOfMemoryException("Buffer is empty.");

        return _buffer[_head];
    }

    public int Read(Memory<byte> data)
    {
        if (IsEmpty)
            return 0;

        var bytesToRead = Math.Min(data.Length, _size);
        var bytesToEnd = Math.Min(bytesToRead, _buffer.Length - _head);

        _buffer.AsMemory(_head, bytesToEnd).CopyTo(data);

        if (bytesToRead > bytesToEnd)
            _buffer.AsMemory(0, bytesToRead - bytesToEnd).CopyTo(data[bytesToEnd..]);

        _head = (_head + bytesToRead) % _buffer.Length;
        _size -= bytesToRead;

        return bytesToRead;
    }

    public int Read(int count, out ReadOnlySequence<byte> sequence)
    {
        sequence = default;

        if (IsEmpty)
            return 0;

        var bytesToRead = Math.Min(count, _size);
        var buffer = new byte[bytesToRead];
        var bytesToEnd = Math.Min(bytesToRead, _buffer.Length - _head);

        Array.Copy(_buffer, _head, buffer, 0, bytesToEnd);

        if (bytesToRead > bytesToEnd)
            Array.Copy(_buffer, 0, buffer, bytesToEnd, bytesToRead - bytesToEnd);

        sequence = new ReadOnlySequence<byte>(buffer);
        _head = (_head + bytesToRead) % _buffer.Length;
        _size -= bytesToRead;

        return bytesToRead;
    }

    public int Read(byte[] data, int offset, int count)
    {
        if (IsEmpty)
            return 0;

        var bytesToRead = Math.Min(count, _size);
        var bytesToEnd = Math.Min(bytesToRead, _buffer.Length - _head);
        Array.Copy(_buffer, _head, data, offset, bytesToEnd);

        if (bytesToRead > bytesToEnd) Array.Copy(_buffer, 0, data, offset + bytesToEnd, bytesToRead - bytesToEnd);

        _head = (_head + bytesToRead) % _buffer.Length;
        _size -= bytesToRead;

        return bytesToRead;
    }

    public int ReadVarInt32()
    {
        var result = 0;
        var shift = 0;
        byte b;
        do
        {
            b = Peek();
            _ = Read(new byte[1], 0, 1);
            result |= (b & 0x7F) << shift;
            shift += 7;
        }
        while ((b & 0x80) != 0);

        return result;
    }

    public void Write(ref ReadOnlySequence<byte> sequence)
    {
        foreach (var memory in sequence) Write(memory);
    }

    public void Write(ReadOnlyMemory<byte> data)
    {
        Write(data.ToArray(), 0, data.Length);
    }

    public void Write(byte[] data, int offset, int count)
    {
        if (count > _buffer.Length - _size)
        {
            // we have to expand it
            var newBuffer = new byte[Math.Max(_buffer.Length * 2, _buffer.Length + count)];
            var newHead = 0;
            var newTail = 0;
            var newSize = 0;
            
            if (_head < _tail)
            {
                Array.Copy(_buffer, _head, newBuffer, 0, _size);
            }
            else
            {
                var bytesToTheEnd = Math.Min(_size, _buffer.Length - _head);
                Array.Copy(_buffer, _head, newBuffer, 0, bytesToTheEnd);

                if (_size > bytesToTheEnd)
                    Array.Copy(_buffer, 0, newBuffer, bytesToTheEnd, _size - bytesToTheEnd);
            }

            newTail = _size;

            newHead = 0;
            newSize = _size;
            _buffer = newBuffer;
            _head = newHead;
            _tail = newTail;
            _size = newSize;
        }
        
        var bytesToEnd = Math.Min(count, _buffer.Length - _tail);

        Array.Copy(data, offset, _buffer, _tail, bytesToEnd);

        if (count > bytesToEnd)
            Array.Copy(data, offset + bytesToEnd, _buffer, 0, count - bytesToEnd);

        _tail = (_tail + count) % _buffer.Length;
        _size += count;
    }

    public bool Seek(int length)
    {
        // we rearrange the buffer to start at the expected length
        if (_tail != length)
        {
            var newBuffer = new byte[_buffer.Length];
            var newHead = 0;
            var newTail = 0;
            var newSize = 0;

            if (_head < length)
            {
                var bytesToEnd = Math.Min(_size, _buffer.Length - _head);
                Array.Copy(_buffer, _head, newBuffer, 0, bytesToEnd);

                if (_size > bytesToEnd)
                    Array.Copy(_buffer, 0, newBuffer, bytesToEnd, _size - bytesToEnd);

                newTail = _size;
            }
            else
            {
                Array.Copy(_buffer, _head, newBuffer, 0, _size);
                newTail = _size;
            }

            newHead = 0;
            newSize = _size;
            newBuffer.AsMemory().Slice(0, newSize).CopyTo(_buffer.AsMemory());
            _head = newHead;
            _tail = newTail;
            _size = newSize;
            
            return true;
        }
        
        return false;
    }
}