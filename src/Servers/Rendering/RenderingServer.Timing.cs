using System.Diagnostics;

namespace Electron2D;

public sealed partial class RenderingServer
{
    private CanvasRenderTiming? _cpuRenderTiming;
    private long _cpuRenderStart;
    private double _frameSetupCPU;
    private CanvasRenderTiming? ViewportTiming(Viewport viewport) => _serverViewports.GetValueOrDefault(viewport)?.Timing;
    private void PauseRenderCPU()
    { if (_cpuRenderTiming is { } timing) timing.AddCPU(Stopwatch.GetTimestamp() - _cpuRenderStart); }
    private void ViewportSetMeasureRenderTimeCore(RID rid, bool enable)
    {
        var state = ChangeViewport(rid, out _);
        if (enable) state.Timing ??= _backend.CreateRenderTiming();
        else { var old = state.Timing; state.Timing = null; old?.Dispose(); }
    }
    private double ViewportMeasuredRenderTime(RID rid, bool GPU)
    {
        var timing = ViewportTiming(ServerViewport(rid));
        if (timing is null) return 0;
        timing.Collect(false); return GPU ? timing.GPU : timing.CPU;
    }
    private void ReleaseRenderTimings(ref List<Exception>? errors)
    {
        foreach (var state in _serverViewports.Values)
        {
            var timing = state.Timing; state.Timing = null;
            try { timing?.Dispose(); } catch (Exception error) { (errors ??= []).Add(error); }
        }
    }
    /// <summary>Enables native CPU/GPU duration measurements for a viewport's rendering work.</summary>
    /// <param name="viewport">A live scene or caller-owned viewport.</param><param name="enable">Whether to retain native elapsed queries.</param>
    /// <remarks>Results are milliseconds from completed viewport draws. Child viewport CPU work, process scripts,
    /// frame caps and presentation waits are excluded. Disabled and not-yet-completed measurements read zero.
    /// Enabling allocates cold query storage. The current native adapters are Vulkan and Linux GL timestamp contexts;
    /// unsupported backends reject. Disable/release retires pending native queries on the owner.</remarks>
    /// <exception cref="ArgumentException">The viewport identity is invalid.</exception>
    /// <exception cref="InvalidOperationException">Owner, capture/submission or native state is invalid.</exception>
    /// <exception cref="NotSupportedException">The active backend has no native elapsed-query adapter.</exception>
    public static void ViewportSetMeasureRenderTime(RID viewport, bool enable) => RequireService().ViewportSetMeasureRenderTimeCore(viewport, enable);
    /// <summary>Returns a viewport's last completed CPU render duration in milliseconds.</summary>
    /// <param name="viewport">A live scene or caller-owned viewport.</param>
    /// <returns>CPU rendering time, excluding dependent viewport execution; zero when disabled or not yet completed.</returns>
    /// <exception cref="ArgumentException">The viewport identity is invalid.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable, off-owner or native queries fail.</exception>
    public static double ViewportGetMeasuredRenderTimeCPU(RID viewport) => RequireService().ViewportMeasuredRenderTime(viewport, false);
    /// <summary>Returns a viewport's last completed native GPU render duration in milliseconds.</summary>
    /// <param name="viewport">A live scene or caller-owned viewport.</param>
    /// <returns>Native elapsed device time, independent of CPU clock epoch; zero when disabled or not yet completed.</returns>
    /// <exception cref="ArgumentException">The viewport identity is invalid.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable, off-owner or native queries fail.</exception>
    public static double ViewportGetMeasuredRenderTimeGPU(RID viewport) => RequireService().ViewportMeasuredRenderTime(viewport, true);
    /// <summary>Returns the last frame's common CPU rendering preparation time in milliseconds.</summary>
    /// <returns>Common preparation outside individual viewport draws, process callbacks and presentation waits; zero before a frame completes.</returns>
    /// <exception cref="InvalidOperationException">The renderer is unavailable or the caller is off-owner.</exception>
    public static double GetFrameSetupTimeCPU() { var service = RequireService(); service.EnsureOwner(); return service._frameSetupCPU; }
}
