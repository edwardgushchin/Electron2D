using System.Diagnostics;
using SDL3;

namespace Electron2D;

internal sealed partial class GPUCanvasBackend
{
    internal enum FrameProfileBoundary { Begin, Canvas, Presentation, End }

    private GPUTimestampQueries?[]? _frameProfileQueries;
    private bool[]? _frameProfileReady;
    private bool _frameProfileEnabled;
    private int _frameProfileSamples;
    private long _frameProfileStart;
    private double _frameProfileSetup, _frameProfileCanvas, _frameProfilePresentation;

    internal void EnableFrameProfile()
    {
        var queries = new GPUTimestampQueries?[FrameQueueSize];
        var ready = new bool[queries.Length];
        try
        {
            for (var index = 0; index < queries.Length; index++)
                queries[index] = new GPUTimestampQueries(Device, _device, (uint)FrameProfileBoundary.End + 1);
        }
        catch (Exception error) when (error is NotSupportedException or InvalidOperationException)
        {
            foreach (var query in queries) query?.Dispose();
            ReportFrameProfileFailure(error);
            return;
        }

        _frameProfileQueries = queries;
        _frameProfileReady = ready;
        _frameProfileEnabled = true;
        _frameProfileStart = Stopwatch.GetTimestamp();
    }

    internal void MarkFrameProfile(FrameProfileBoundary boundary)
    {
        if (!_frameProfileEnabled) return;
        try
        {
            var queries = _frameProfileQueries![_nextFrameSlot]!;
            var command = SDL.AcquireGPUCommandBuffer(Device);
            if (command == 0) throw Failure("acquire GPU profile command");
            try
            {
                if (boundary == FrameProfileBoundary.Begin) queries.Reset(command);
                queries.Write(command, (uint)boundary);
                var submitted = command;
                command = 0;
                if (boundary == FrameProfileBoundary.End) SubmitFencedFrameWork(submitted);
                else Check(SDL.SubmitGPUCommandBuffer(submitted), "submit GPU profile command");
            }
            finally
            {
                if (command != 0) SDL.CancelGPUCommandBuffer(command);
            }

            if (boundary == FrameProfileBoundary.End) _frameProfileReady![_nextFrameSlot] = true;
        }
        catch (InvalidOperationException error) { ReportFrameProfileFailure(error); }
    }

    private void CollectFrameProfile()
    {
        if (!_frameProfileEnabled || !_frameProfileReady![_nextFrameSlot]) return;
        _frameProfileReady[_nextFrameSlot] = false;
        Span<ulong> timestamps = stackalloc ulong[(int)FrameProfileBoundary.End + 1];
        try { _frameProfileQueries![_nextFrameSlot]!.Read(timestamps); }
        catch (InvalidOperationException error) { ReportFrameProfileFailure(error); return; }

        // A narrow hardware counter can wrap between markers. Skip that sample.
        if (timestamps[0] > timestamps[1] || timestamps[1] > timestamps[2] || timestamps[2] > timestamps[3]) return;

        _frameProfileSetup += timestamps[1] - timestamps[0];
        _frameProfileCanvas += timestamps[2] - timestamps[1];
        _frameProfilePresentation += timestamps[3] - timestamps[2];
        _frameProfileSamples++;
        var now = Stopwatch.GetTimestamp();
        if (Stopwatch.GetElapsedTime(_frameProfileStart, now) < TimeSpan.FromSeconds(1)) return;

        var total = (timestamps[3] - timestamps[0]) / 1_000_000d;
        var scale = _frameProfileSamples * 1_000_000d;
        RuntimeDiagnostics.Instance.ReportMessage(FormattableString.Invariant(
            $"GPU profile (ms): frame {total:0.000}, average setup {_frameProfileSetup / scale:0.000}, canvas {_frameProfileCanvas / scale:0.000}, presentation {_frameProfilePresentation / scale:0.000} ({_frameProfileSamples} frames)"));
        _frameProfileStart = now;
        _frameProfileSamples = 0;
        _frameProfileSetup = _frameProfileCanvas = _frameProfilePresentation = 0;
    }

    private void ReportFrameProfileFailure(Exception error)
    {
        _frameProfileEnabled = false;
        RuntimeDiagnostics.Instance.ReportMessage($"GPU profile unavailable on {Driver}: {error.Message}");
    }

    private void ReleaseFrameProfile()
    {
        if (_frameProfileQueries is null) return;
        foreach (var query in _frameProfileQueries) query?.Dispose();
    }
}
