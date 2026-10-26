#if ANDROID
using Android.App;
using Electron2D.Platform.Android;
using System.Runtime.Versioning;

namespace Electron2D;

internal static class AndroidWindowHost
{
    private static readonly object Gate = new();
    private static Window? _window;
    private static TaskCompletionSource<int>? _completion;
    private static bool _started;
    private static string? _moviePath;
    private static int _movieFPS;
    private static Action<Window, ulong>? _beforeFrame;
    private static bool _movieDisableVSync;
    private static WeakReference<AndroidGameActivity>? _activity;

    internal static void AttachActivity(AndroidGameActivity activity)
    {
        lock (Gate) _activity = new(activity);
    }

    internal static bool DetachActivity(AndroidGameActivity activity)
    {
        lock (Gate)
        {
            if (_activity is null || !_activity.TryGetTarget(out var current) || !ReferenceEquals(current, activity)) return false;
            _activity = null;
            return true;
        }
    }

    internal static void PostPermissionResults(AndroidGameActivity activity, string[] permissions, Android.Content.PM.Permission[] results)
    {
        MainLoop? loop;
        lock (Gate)
        {
            if (!_started || activity.IsFinishing || activity.IsDestroyed || _activity is null ||
                !_activity.TryGetTarget(out var current) || !ReferenceEquals(current, activity)) return;
            loop = _window?.Tree;
            if (!ReferenceEquals(loop, Engine.Service.MainLoopCore)) return;
        }
        if (loop is null) return;
        foreach (var permission in permissions) if (string.IsNullOrEmpty(permission)) return;
        Span<bool> granted = permissions.Length <= 64 ? stackalloc bool[permissions.Length] : new bool[permissions.Length];
        for (var i = 0; i < granted.Length; i++) granted[i] = i < results.Length && results[i] == Android.Content.PM.Permission.Granted;
        loop.PostPermissionResults(permissions, granted);
    }

    internal static Task<int> RunAsync(Window window, string? moviePath = null, int movieFPS = 60, bool movieDisableVSync = false, Action<Window, ulong>? beforeFrame = null)
    {
        lock (Gate)
        {
            if (_window is not null)
                throw new InvalidOperationException("An Android window lifecycle is already pending or active.");
            var completion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            Engine.Service.ReserveWindowRun();
            _window = window; _moviePath = moviePath; _movieFPS = movieFPS; _movieDisableVSync = movieDisableVSync; _beforeFrame = beforeFrame;
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
            _moviePath = null; _movieFPS = 0; _movieDisableVSync = false; _beforeFrame = null;
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
        try { code = Engine.Service.RunReservedWindowCore(window, _moviePath, _movieFPS, _movieDisableVSync, _beforeFrame); }
        catch (Exception error) { failure = error; }
        finally { lock (Gate) { _window = null; _completion = null; _started = false; _moviePath = null; _movieFPS = 0; _movieDisableVSync = false; _beforeFrame = null; } }
        if (failure is null) completion.TrySetResult(code);
        else completion.TrySetException(failure);
    }
}

[Activity(Name = "org.electron2d.GameActivity", EnableOnBackInvokedCallback = true)]
internal sealed class AndroidGameActivity : SDLActivity
{
    private Android.Window.IOnBackInvokedCallback? _backCallback;

    protected override string[] GetLibraries() => ["SDL3"];

    protected override void OnCreate(Android.OS.Bundle? savedInstanceState)
    {
        AndroidWindowHost.AttachActivity(this);
        base.OnCreate(savedInstanceState);
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            _backCallback = new BackCallback(this);
            OnBackInvokedDispatcher!.RegisterOnBackInvokedCallback(Android.Window.IOnBackInvokedDispatcher.PriorityDefault, _backCallback);
        }
    }

    public override void OnBackPressed() => RequestGoBack();

    [SupportedOSPlatform("android23.0")]
    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        AndroidWindowHost.PostPermissionResults(this, permissions, grantResults);
    }

    public override bool DispatchKeyEvent(Android.Views.KeyEvent? e)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(33) && e?.KeyCode == Android.Views.Keycode.Back &&
            !e.IsFromSource(Android.Views.InputSourceType.Mouse))
        {
            // SDL consumes legacy navigation before Activity.OnBackPressed can observe it.
            if (e.Action == Android.Views.KeyEventActions.Up && !e.IsCanceled) OnBackPressed();
            return true;
        }
        return base.DispatchKeyEvent(e);
    }

    private void RequestGoBack()
    {
        if (!IsFinishing && !IsDestroyed && !DisplayServer.TryQueueSystemGoBack()) Finish();
    }

    [SupportedOSPlatform("android33.0")]
    private sealed class BackCallback(AndroidGameActivity owner) : Java.Lang.Object, Android.Window.IOnBackInvokedCallback
    {
        public void OnBackInvoked() => owner.RequestGoBack();
    }

    protected override void OnDestroy()
    {
        try
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(33) && _backCallback is not null)
            {
                try { OnBackInvokedDispatcher!.UnregisterOnBackInvokedCallback(_backCallback); }
                finally { _backCallback.Dispose(); _backCallback = null; }
            }
        }
        finally
        {
            try { if (AndroidWindowHost.DetachActivity(this)) AndroidWindowHost.CancelPending(); }
            finally { base.OnDestroy(); }
        }
    }

    protected override void Main()
    {
        try { AndroidWindowHost.Run(); }
        finally { RunOnUiThread(Finish); }
    }
}
#endif
