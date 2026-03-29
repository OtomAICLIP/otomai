using System.Text;
using System.Text.Json.Serialization;
using ProtoBuf;

namespace Bubble.Shared.Protocol;

[ProtoContract]
public sealed class Any
{
    [ProtoMember(1)]
    public required string TypeUrl { get; set; }

    [ProtoMember(2)]
    [JsonIgnore]
    public required byte[] Value { get; set; }
}