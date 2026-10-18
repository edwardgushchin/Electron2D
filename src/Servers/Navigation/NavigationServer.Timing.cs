using System.Diagnostics;

namespace Electron2D;

public sealed partial class NavigationServer
{
    [ThreadStatic] private static long _threadProcessingTicks;
    [ThreadStatic] private static long _processingStarted;
    [ThreadStatic] private static int _processingTimingDepth;

    internal static long ThreadProcessingTicks => _threadProcessingTicks;

    private static void BeginProcessingTiming()
    {
        if (_processingTimingDepth++ == 0) _processingStarted = Stopwatch.GetTimestamp();
    }

    private static void EndProcessingTiming()
    {
        if (--_processingTimingDepth == 0)
            _threadProcessingTicks += Stopwatch.GetTimestamp() - _processingStarted;
    }
}
