using Android.App;
using Android.Runtime;
using Android.Util;
using Electron2D;
using IOPath = System.IO.Path;

namespace SystemBackAndroid;

[Application]
public sealed class GameApplication(nint handle, JniHandleOwnership ownership) : Application(handle, ownership)
{
    public override async void OnCreate()
    {
        base.OnCreate();
        var directory = FilesDir!.AbsolutePath;
        var mode = "unset";
        try
        {
            mode = File.ReadAllText(IOPath.Combine(directory, "mode")).Trim();
            Reject<PlatformNotSupportedException>(() => OS.ReadBufferFromSTDIN());
            Reject<PlatformNotSupportedException>(() => OS.ReadStringFromSTDIN());
            Reject<ArgumentOutOfRangeException>(() => OS.ReadBufferFromSTDIN(0));
            Reject<ArgumentOutOfRangeException>(() => OS.ReadStringFromSTDIN(1));
            await SystemBackContract.RunAsync(mode, line =>
            {
                Log.Info("SystemBack", line);
                if (line.StartsWith("RESULT ", StringComparison.Ordinal)) File.WriteAllText(IOPath.Combine(directory, "result"), line);
            });
        }
        catch (Exception error)
        {
            var result = $"RESULT {mode} FAIL {error}\n";
            File.WriteAllText(IOPath.Combine(directory, "result"), result); Log.Error("SystemBack", result);
        }
    }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
