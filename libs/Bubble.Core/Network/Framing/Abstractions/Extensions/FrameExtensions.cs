using System.Runtime.CompilerServices;
using Bubble.Core.Network.Framing.Abstractions.Metadata;

namespace Bubble.Core.Network.Framing.Abstractions.Extensions;

public static class FrameExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Frame<T> AsTyped<T>(this Frame frame)
        where T : class, IFrameMetadata
    {
        return new Frame<T>(frame.Payload, frame.Metadata as T ?? throw new ArgumentException(SocketsStrings.InvalidFrameType, nameof(frame)));
    }
}