using Bubble.Core.Network.Framing.Abstractions.Metadata;

namespace Bubble.Core.Network.Framing.Protobuf;

public sealed record ProtobufMetadata(int Length) : IFrameMetadata;