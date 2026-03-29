// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Collections.Concurrent;

namespace Bubble.Core.Network.Internal.MemoryPool;

internal sealed class PinnedBlockMemoryPool : MemoryPool<byte>
{
    private const int _blockSize = 4096;

    private const int AnySize = -1;

    private readonly ConcurrentQueue<MemoryPoolBlock> _blocks;
    private readonly object _disposeSync;

    private bool _isDisposed;

    public override int MaxBufferSize =>
        _blockSize;

    public static int BlockSize =>
        _blockSize;

    public PinnedBlockMemoryPool()
    {
        _blocks = [];
        _disposeSync = new object();
    }

    protected override void Dispose(bool disposing)
    {
        if (_isDisposed)
            return;

        lock (_disposeSync)
        {
            _isDisposed = true;

            if (disposing)
                // Discard blocks in pool
                while (_blocks.TryDequeue(out _))
                {
                }
        }
    }

    public override IMemoryOwner<byte> Rent(int size = AnySize)
    {
        if (size > _blockSize)
            MemoryPoolThrowHelper.ThrowArgumentOutOfRangeException_BufferRequestTooLarge(_blockSize);

        if (_isDisposed)
            MemoryPoolThrowHelper.ThrowObjectDisposedException(MemoryPoolThrowHelper.ExceptionArgument.MemoryPool);

        return _blocks.TryDequeue(out var block)
            ?
            // block successfully taken from the stack - return it
            block
            : new MemoryPoolBlock(this, BlockSize);
    }

    internal void Return(MemoryPoolBlock block)
    {
        #if BLOCK_LEASE_TRACKING
            Debug.Assert(block.Pool == this, "Returned block was not leased from this pool");
            Debug.Assert(block.IsLeased, $"Block being returned to pool twice: {block.Leaser}{Environment.NewLine}");
            block.IsLeased = false;
        #endif

        if (!_isDisposed)
            _blocks.Enqueue(block);
    }
}