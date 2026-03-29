namespace Bubble.Shared.Protocol;

public interface IProtoMessage
{
    public static virtual string TypeUrl { get; } = string.Empty;
}