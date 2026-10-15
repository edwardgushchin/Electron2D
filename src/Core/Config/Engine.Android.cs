#if ANDROID
using Android.App;
using Electron2D.Platform.Android;

namespace Electron2D;

internal static class AndroidWindowHost
{
    private static readonly object Gate = new();
    private static Window? _window;
    private static TaskCompletionSource<int>? _completion;
    private static bool _started;
    private static string? _moviePath;
    private static int _movieFPS;
    private static bool _movieDisableVSync;

    internal static Task<int> RunAsync(Window window, string? moviePath = null, int movieFPS = 60, bool movieDisableVSync = false)
    {
        lock (Gate)
        {
            if (_window is not null)
                throw new InvalidOperationException("An Android window lifecycle is already pending or active.");
            var completion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            Engine.Service.ReserveWindowRun();
            _window = window; _moviePath = moviePath; _movieFPS = movieFPS; _movieDisableVSync = movieDisableVSync;
            _completion = completion;
            return _completion.Task;
        }
    }

    internal static void CancelPending()
    {
        Window window;
        TaskCompletionSource<int> completion;
        lock (Gate)
        {
            if (_started || _window is null) return;
            window = _window;
            completion = _completion!;
            _window = null;
            _completion = null;
            _moviePath = null; _movieFPS = 0; _movieDisableVSync = false;
        }
        try { Engine.Service.CancelReservedWindowCore(window); completion.TrySetCanceled(); }
        catch (Exception error) { completion.TrySetException(error); }
    }

    internal static void Run()
    {
        Window window;
        TaskCompletionSource<int> completion;
        lock (Gate)
        {
            window = _window ?? throw new InvalidOperationException("Call Engine.RunAsync from Application.OnCreate before launching GameActivity.");
            if (_started) throw new InvalidOperationException("The Android window run already started.");
            completion = _completion!;
            _started = true;
        }
        var code = 0;
        Exception? failure = null;
        try { code = Engine.Service.RunReservedWindowCore(window, _moviePath, _movieFPS, _movieDisableVSync); }
        catch (Exception error) { failure = error; }
        finally { lock (Gate) { _window = null; _completion = null; _started = false; _moviePath = null; _movieFPS = 0; _movieDisableVSync = false; } }
        if (failure is null) completion.TrySetResult(code);
        else completion.TrySetException(failure);
    }
}

[Activity(Name = "org.electron2d.GameActivity")]
internal sealed class AndroidGameActivity : SDLActivity
{
    protected override string[] GetLibraries() => ["SDL3"];

    protected override void OnDestroy()
    {
        AndroidWindowHost.CancelPending();
        base.OnDestroy();
    }

    protected override void Main()
    {
        try { AndroidWindowHost.Run(); }
        finally { RunOnUiThread(Finish); }
    }
}
#endif
