using System.Diagnostics;

namespace Electron2D;

public sealed partial class Performance
{
    private double _processTime, _physicsTime, _navigationTime, _timingElapsed;
    private long _processPeak, _physicsPeak, _navigationPeak;

    internal void ResetFrameTiming()
    {
        _timingElapsed = 0;
        _processPeak = _physicsPeak = _navigationPeak = 0;
        AtomicFloatingPoint.Write(ref _processTime, 0);
        AtomicFloatingPoint.Write(ref _physicsTime, 0);
        AtomicFloatingPoint.Write(ref _navigationTime, 0);
    }

    internal void ObserveFrame(double elapsed, long processTicks, long physicsTicks, long navigationTicks)
    {
        _processPeak = Math.Max(_processPeak, processTicks);
        _physicsPeak = Math.Max(_physicsPeak, physicsTicks);
        _navigationPeak = Math.Max(_navigationPeak, navigationTicks);
        _timingElapsed += elapsed;
        if (_timingElapsed < 1d - 1e-12d) return;
        AtomicFloatingPoint.Write(ref _processTime, _processPeak / (double)Stopwatch.Frequency);
        AtomicFloatingPoint.Write(ref _physicsTime, _physicsPeak / (double)Stopwatch.Frequency);
        AtomicFloatingPoint.Write(ref _navigationTime, _navigationPeak / (double)Stopwatch.Frequency);
        _processPeak = _physicsPeak = _navigationPeak = 0;
        _timingElapsed %= 1d;
        if (_timingElapsed > 1d - 1e-12d) _timingElapsed = 0;
    }
}
