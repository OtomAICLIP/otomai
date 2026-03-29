using System.IO.Pipelines;
using System.Runtime.CompilerServices;

namespace Bubble.Core.Network.Framing.Abstractions.Extensions;

public static class PipeReaderExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool TryAdvanceTo(this PipeReader r, SequencePosition consumed)
    {
        // Suppress exceptions if the pipe has already been completed
        try
        {
            r.AdvanceTo(consumed, consumed);
            return true;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}