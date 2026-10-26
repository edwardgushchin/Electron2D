using Android.App;
using Android.Runtime;
using Android.Util;
using IOPath = System.IO.Path;

namespace PermissionsAndroid;

[Application]
public sealed class GameApplication(nint handle, JniHandleOwnership ownership) : Application(handle, ownership)
{
    private readonly Activities _activities = new();
    public override async void OnCreate()
    {
        base.OnCreate();
        RegisterActivityLifecycleCallbacks(_activities);
        var directory = FilesDir!.AbsolutePath;
        var mode = "unset";
        try
        {
            mode = File.ReadAllText(IOPath.Combine(directory, "mode")).Trim();
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
