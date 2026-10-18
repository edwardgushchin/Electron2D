using System.Diagnostics;

namespace Electron2D;

public sealed partial class Engine
{
    private (long ProcessStart, long Physics, long Navigation) _pendingFrameTiming;

    private void CompleteFrameTiming(double elapsed) => Performance.Service.ObserveFrame(elapsed,
        Stopwatch.GetTimestamp() - _pendingFrameTiming.ProcessStart,
        _pendingFrameTiming.Physics, _pendingFrameTiming.Navigation);
}
