using Bubble.Core.Extensions;

namespace Bubble.Shared.Extensions;

public static class IProtoMessageExtensions
{
    public static string GetName<T>(this T _)
        where T : class, IProtoMessage
    {
        return typeof (T).Name;
    }
}