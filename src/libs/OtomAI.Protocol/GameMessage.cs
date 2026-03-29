using ProtoBuf;

namespace OtomAI.Protocol;

/// <summary>
/// Top-level envelope for all Dofus 3.0 messages.
/// Mirrors the Bubble.D3.Bot GameMessage structure:
///   oneof Content { Event=1, Request=2, Response=4 }
/// </summary>
[ProtoContract]
public sealed class GameMessage
{
    [ProtoMember(1)] public GameEvent? Event { get; set; }
    [ProtoMember(2)] public GameRequest? Request { get; set; }
    [ProtoMember(4)] public GameResponse? Response { get; set; }

    public bool IsEvent => Event is not null;
    public bool IsRequest => Request is not null;
    public bool IsResponse => Response is not null;
}

[ProtoContract]
public sealed class GameEvent
{
    [ProtoMember(1)] public int Uid { get; set; } = -1;
    [ProtoMember(2)] public ProtobufAny? Content { get; set; }
}

[ProtoContract]
public sealed class GameRequest
{
    [ProtoMember(1)] public int Uid { get; set; }
    [ProtoMember(2)] public ProtobufAny? Content { get; set; }
}

[ProtoContract]
public sealed class GameResponse
{
    [ProtoMember(1)] public int Uid { get; set; }
    [ProtoMember(2)] public ProtobufAny? Content { get; set; }
}

/// <summary>
/// Protobuf Any wrapper. TypeUrl uses short codes like "type.ankama.com/iwv".
/// </summary>
[ProtoContract]
public sealed class ProtobufAny
{
    [ProtoMember(1)] public string TypeUrl { get; set; } = "";
    [ProtoMember(2)] public byte[] Value { get; set; } = [];

    public string ShortCode => TypeUrl.StartsWith("type.ankama.com/")
        ? TypeUrl["type.ankama.com/".Length..]
        : TypeUrl;
}
