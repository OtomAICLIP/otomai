// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.Runtime.InteropServices;

namespace Bubble.Core.Network.Internal.MemoryPool;

internal sealed class MemoryPoolBlock : IMemoryOwner<byte>
{
    public PinnedBlockMemoryPool Pool { get; }

    public Memory<byte> Memory { get; }

    internal MemoryPoolBlock(PinnedBlockMemoryPool pool, int length)
    {
        Pool = pool;

        var pinnedArray = GC.AllocateUninitializedArray<byte>(length, true);

        Memory = MemoryMarshal.CreateFromPinnedArray(pinnedArray, 0, pinnedArray.Length);
    }

    public void Dispose()
    {
        Pool.Return(this);
    }
}