using Android.App;
using Android.Content.PM;
using Android.Util;
using Org.Libsdl.App;
using System.Runtime.InteropServices;

namespace Electron2DAndroidTests;

[Activity(Name = "org.electron2d.tests.MainActivity")]
public sealed class MainActivity : SDLActivity
{
    protected override string[] GetLibraries() => ["SDL3"];

    protected override void Main()
    {
        var run = Intent?.GetStringExtra("run") ?? "manual";
        try
        {
            using var activity = PackageManager!.GetActivityInfo(ComponentName!, PackageInfoFlags.MetaData)!;
            // AssetsPaths is the sign bit even on Android versions before its public SDK constant.
            if ((int)activity.ConfigChanges >= 0)
                throw new InvalidOperationException("The installed SDL activity must handle resource overlay changes without recreation.");
            using var launcher = PackageManager!.GetLaunchIntentForPackage(PackageName!);
            if (!activity.Exported || launcher?.Component?.ClassName != ComponentName!.ClassName)
                throw new InvalidOperationException("The installed SDL activity must be an exported launcher entry point.");
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
            Log.Info("Electron2DTests", "CHECK managed contracts");
            ContractChecks.Run(message => Log.Info("Electron2DTests", message));
            Log.Info("Electron2DTests", "CHECK native font precision");
            var latin = FontTestFixtures.OpenSans; var arabic = FontTestFixtures.Arabic;
            Log.Info("Electron2DTests", "CHECK native font fixtures ready");
            NativeFontPrecisionTests.Run(latin, arabic, message => Log.Info("Electron2DTests", message));
            // Match the desktop headless PCM profile; emulator hardware output is a separate gate.
            SDL3.SDL.SetHint("SDL_AUDIODRIVER", "dummy");
            Log.Info("Electron2DTests", "CHECK native TLS/audio lifecycle");
            NativeChecks.Run();
            Log.Info("Electron2DTests", $"RESULT {run} PASS");
        }
        catch (Exception error)
        {
            Log.Error("Electron2DTests", $"RESULT {run} FAIL {error}");
        }
        finally { RunOnUiThread(Finish); }
    }
}
