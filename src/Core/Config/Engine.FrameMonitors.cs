using System.Diagnostics;
using System.Globalization;

namespace Electron2D;

public sealed partial class Engine
{
    private (long ProcessStart, long Physics, long Navigation) _pendingFrameTiming;
    private bool _printFPS;
    private int _fpsWarmupWindows;

    private void ConfigureFrameRateDiagnostics(bool windowed)
    {
        var printFPS = false;
        int? maxFPS = null;
        var arguments = Environment.GetCommandLineArgs();
        for (var index = 1; index < arguments.Length; index++)
        {
            var argument = arguments[index];
            if (argument is "--" or "++") break;
            if (argument == "--print-fps") printFPS = true;
            if (!windowed || argument != "--max-fps") continue;
            if (++index == arguments.Length ||
                !int.TryParse(arguments[index], NumberStyles.None, CultureInfo.InvariantCulture, out var limit))
                throw new ArgumentException("--max-fps requires a nonnegative integer.");
            maxFPS = limit;
        }

        if (windowed)
            Volatile.Write(ref _windowMaxFps, maxFPS ?? ProjectSettings.GetWithOverride(ProjectSettings.MaxFPS));
        _printFPS = printFPS || !IsEditorHint && ProjectSettings.GetWithOverride(ProjectSettings.PrintFPS);
        _fpsWarmupWindows = 3;
    }

    private void PrintFrameRateSample(double framesPerSecond)
    {
        if (_fpsWarmupWindows > 0)
        {
            _fpsWarmupWindows--;
            return;
        }
        var role = IsEditorHint ? "Editor" : "Project";
        RuntimeDiagnostics.Instance.ReportMessage(
            FormattableString.Invariant($"{role} FPS: {framesPerSecond:0.##} ({1000d / framesPerSecond:0.00} mspf)"));
    }

    private void CompleteFrameTiming(double elapsed) => Performance.Service.ObserveFrame(elapsed,
        Stopwatch.GetTimestamp() - _pendingFrameTiming.ProcessStart,
        _pendingFrameTiming.Physics, _pendingFrameTiming.Navigation);
}
