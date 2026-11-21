using Android.App;
using Android.Runtime;
using Android.Util;
using Electron2D;
using IOPath = System.IO.Path;

namespace PermissionsAndroid;

[Application]
public sealed class GameApplication(nint handle, JniHandleOwnership ownership) : Application(handle, ownership)
{
    private readonly Activities _activities = new();
    private int? _vibratorProbeThread;
    private bool _vibratorLookupObserved;

    public override Java.Lang.Object? GetSystemService(string? name)
    {
        if (_vibratorProbeThread == Environment.CurrentManagedThreadId &&
            name == (OperatingSystem.IsAndroidVersionAtLeast(31) ? VibratorManagerService : VibratorService))
        {
            _vibratorLookupObserved = true;
            return null;
        }

        return base.GetSystemService(name);
    }

    public override async void OnCreate()
    {
        base.OnCreate();
        RegisterActivityLifecycleCallbacks(_activities);
        var directory = FilesDir!.AbsolutePath;
        var mode = "unset";
        try
        {
            mode = File.ReadAllText(IOPath.Combine(directory, "mode")).Trim();
            if (mode.StartsWith("vibrate-", StringComparison.Ordinal))
            {
                ProjectSettings.Set(ProjectSettings.RenderingMethod, "compatibility");
                ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
                var window = new VibrationWindow(mode, CheckUnavailableVibrator, line =>
                {
                    Log.Info("HandheldVibration", line);
                    File.WriteAllText(IOPath.Combine(directory, "result"), line);
                });
                await Engine.RunAsync(window);
                return;
            }

            await PermissionResultContract.RunAsync(mode, permissions =>
            {
                var activity = _activities.Current ?? throw new InvalidOperationException("No native game activity");
                activity.RunOnUiThread(() => activity.RequestPermissions(permissions, 0x42D1));
            }, line =>
            {
                Log.Info("PermissionResult", line);
                if (line.StartsWith("RESULT ", StringComparison.Ordinal)) File.WriteAllText(IOPath.Combine(directory, "result"), line);
            });
        }
        catch (Exception error)
        {
            var result = $"RESULT {mode} FAIL {error}\n";
            File.WriteAllText(IOPath.Combine(directory, "result"), result);
            Log.Error("PermissionResult", result);
        }
        finally { UnregisterActivityLifecycleCallbacks(_activities); }
    }

    private void CheckUnavailableVibrator()
    {
        _vibratorProbeThread = Environment.CurrentManagedThreadId;
        try
        {
            Input.VibrateHandheld(173, .5f);
        }
        finally
        {
            _vibratorProbeThread = null;
        }

        if (!_vibratorLookupObserved)
        {
            throw new InvalidOperationException("The public call did not query the unavailable vibrator service.");
        }
    }

    private sealed class VibrationWindow(string mode, Action checkUnavailableVibrator, Action<string> report) : Window
    {
        private bool _requested;

        protected override void OnProcess(double delta)
        {
            base.OnProcess(delta);
            if (_requested || !DisplayServer.WindowIsFocused())
            {
                return;
            }

            _requested = true;
            switch (mode)
            {
                case "vibrate-default":
                    Input.VibrateHandheld();
                    break;
                case "vibrate-half":
                    Input.VibrateHandheld(173, .5f);
                    break;
                case "vibrate-min":
                    Input.VibrateHandheld(1, float.Epsilon);
                    break;
                case "vibrate-max":
                    Input.VibrateHandheld(219, 1f);
                    break;
                case "vibrate-silent":
                    Input.VibrateHandheld(0);
                    Input.VibrateHandheld(173, 0f);
                    break;
                case "vibrate-unavailable":
                    checkUnavailableVibrator();
                    break;
                default:
                    throw new ArgumentException($"Unknown vibration scenario: {mode}", nameof(mode));
            }

            report($"RESULT {mode} RETURNED API={(int)Android.OS.Build.VERSION.SdkInt}");
        }
    }

    private sealed class Activities : Java.Lang.Object, IActivityLifecycleCallbacks
    {
        private Activity? _current;
        internal Activity? Current => Volatile.Read(ref _current);
        public void OnActivityCreated(Activity activity, Android.OS.Bundle? savedInstanceState) => Volatile.Write(ref _current, activity);
        public void OnActivityDestroyed(Activity activity) => Interlocked.CompareExchange(ref _current, null, activity);
        public void OnActivityPaused(Activity activity) { }
        public void OnActivityResumed(Activity activity) { }
        public void OnActivitySaveInstanceState(Activity activity, Android.OS.Bundle outState) { }
        public void OnActivityStarted(Activity activity) { }
        public void OnActivityStopped(Activity activity) { }
    }
}
