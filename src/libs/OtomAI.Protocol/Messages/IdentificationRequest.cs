using OtomAI.Protocol.Dispatch;
using ProtoBuf;

namespace OtomAI.Protocol.Messages;

/// <summary>
/// Login request sent after TCP connection.
/// TypeUrl: "iwv" (from Bubble.D3.Bot analysis).
/// </summary>
[ProtoContract]
public sealed class IdentificationRequest : IProtoMessage
{
    public static string TypeUrl => "iwv";

    [ProtoMember(1)] public string TicketKey { get; set; } = "";
    [ProtoMember(2)] public string Language { get; set; } = "en";
}

/// <summary>
/// Keep-alive ping. TypeUrl: "iws".
/// </summary>
[ProtoContract]
public sealed class PingRequest : IProtoMessage
{
    public static string TypeUrl => "iws";

    [ProtoMember(1)] public bool Quiet { get; set; }
}
