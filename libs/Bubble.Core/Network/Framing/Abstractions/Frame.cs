using System.Buffers;
using Bubble.Core.Network.Framing.Abstractions.Metadata;

namespace Bubble.Core.Network.Framing.Abstractions;

public readonly struct Frame
{
    public static readonly Frame Empty = new(ReadOnlySequence<byte>.Empty, default!);

    public Frame(ReadOnlyMemory<byte> payload, IFrameMetadata metadata)
    {
        Payload = new ReadOnlySequence<byte>(payload);
        Metadata = metadata;
    }

    public Frame(ReadOnlySequence<byte> payload, IFrameMetadata metadata)
    {
        Payload = payload;
        Metadata = metadata;
    }

    public ReadOnlySequence<byte> Payload { get; }

    public IFrameMetadata Metadata { get; }

    public bool IsPayloadEmpty()
    {
        return Metadata.Length is 0 && Payload.IsEmpty;
    }

    public bool IsEmptyFrame()
    {
        return Metadata == default! && Payload.IsEmpty;
    }
}

public readonly struct Frame<TMetadata>
    where TMetadata : class, IFrameMetadata
{
    public static readonly Frame<TMetadata> Empty = new(ReadOnlySequence<byte>.Empty, default!);

    public Frame(ReadOnlyMemory<byte> payload, TMetadata metadata)
    {
        Payload = new ReadOnlySequence<byte>(payload);
        Metadata = metadata;
    }

    public Frame(ReadOnlySequence<byte> payload, TMetadata metadata)
    {
        Payload = payload;
        Metadata = metadata;
    }

    public ReadOnlySequence<byte> Payload { get; }

    public TMetadata Metadata { get; }

    public bool IsPayloadEmpty()
    {
        return Metadata.Length is 0 && Payload.IsEmpty;
    }

    public bool IsEmptyFrame()
    {
        return Metadata == default! && Payload.IsEmpty;
    }
}