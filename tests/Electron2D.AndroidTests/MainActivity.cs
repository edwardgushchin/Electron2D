using Android.App;
using Android.OS;
using Android.Util;
using System.Runtime.InteropServices;

namespace Electron2DAndroidTests;

[Activity(Name = "org.electron2d.tests.MainActivity", Label = "Electron2D tests", MainLauncher = true, Exported = true)]
public sealed class MainActivity : Activity
{
    protected override void OnCreate(Bundle? state)
    {
        base.OnCreate(state);
        var run = Intent?.GetStringExtra("run") ?? "manual";
        try
        {
            var expected = Intent?.GetStringExtra("rid") switch
            {
                "android-arm" => Architecture.Arm,
                "android-arm64" => Architecture.Arm64,
                "android-x86" => Architecture.X86,
                "android-x64" => Architecture.X64,
                null => RuntimeInformation.ProcessArchitecture,
                _ => throw new ArgumentException("Unknown test RID.")
            };
            if (RuntimeInformation.ProcessArchitecture != expected)
                throw new InvalidOperationException("The app did not execute the requested runtime architecture.");
            ContractChecks.Run();
            Log.Info("Electron2DTests", $"RESULT {run} PASS");
        }
        catch (Exception error)
        {
            Log.Error("Electron2DTests", $"RESULT {run} FAIL {error}");
        }
        finally { Finish(); }
    }
}
