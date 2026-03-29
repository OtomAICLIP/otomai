// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Bubble.Core.Network.Internal.MemoryPool;

internal static class MemoryPoolThrowHelper
{
    private static string GetArgumentName(ExceptionArgument argument)
    {
        Debug.Assert(Enum.IsDefined(argument), "The enum value is not defined, please check the ExceptionArgument Enum.");

        return argument.ToString();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ArgumentOutOfRangeException GetArgumentOutOfRangeException(int sourceLength, int offset)
    {
        return (uint)offset > (uint)sourceLength
            ?
            // Offset is negative or less than array length
            new ArgumentOutOfRangeException(GetArgumentName(ExceptionArgument.offset))
            :
            // The third parameter (not passed) length must be out of range
            new ArgumentOutOfRangeException(GetArgumentName(ExceptionArgument.length));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ArgumentOutOfRangeException GetArgumentOutOfRangeException_BufferRequestTooLarge(int maxSize)
    {
        return new ArgumentOutOfRangeException(GetArgumentName(ExceptionArgument.size), string.Format(SocketsStrings.CannotAllocateMoreThan, maxSize));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ObjectDisposedException GetObjectDisposedException(ExceptionArgument argument)
    {
        return new ObjectDisposedException(GetArgumentName(argument));
    }

    public static void ThrowArgumentOutOfRangeException(int sourceLength, int offset)
    {
        throw GetArgumentOutOfRangeException(sourceLength, offset);
    }

    public static void ThrowArgumentOutOfRangeException_BufferRequestTooLarge(int maxSize)
    {
        throw GetArgumentOutOfRangeException_BufferRequestTooLarge(maxSize);
    }

    public static void ThrowInvalidOperationException_DoubleDispose()
    {
        throw new InvalidOperationException("Object is being disposed twice");
    }

    public static void ThrowObjectDisposedException(ExceptionArgument argument)
    {
        throw GetObjectDisposedException(argument);
    }

    internal enum ExceptionArgument
    {
        size,
        offset,
        length,
        MemoryPoolBlock,
        MemoryPool
    }
}