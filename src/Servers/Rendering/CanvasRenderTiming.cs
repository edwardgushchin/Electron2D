using System.Diagnostics;

namespace Electron2D;

internal abstract class CanvasRenderTiming : IDisposable
{
    private long _cpuTicks;
    private bool _cpuPending;
    internal double CPU { get; private set; }
    internal double GPU { get; private set; }
    internal void BeginFrame() { Collect(true); _cpuTicks = 0; _cpuPending = false; }
    internal void AddCPU(long ticks) => _cpuTicks += ticks;
    internal void EndFrame() { _cpuPending = true; Collect(false); }
    internal void Collect(bool wait)
    {
        if (!TryCollect(wait, out var nanoseconds)) return;
        if (_cpuPending) { CPU = _cpuTicks * 1000d / Stopwatch.Frequency; GPU = nanoseconds / 1000000d; _cpuPending = false; }
    }
    internal void Abort() { _cpuPending = false; Discard(); }
    protected abstract bool TryCollect(bool wait, out ulong nanoseconds);
    protected abstract void Discard();
    public abstract void Dispose();
}
