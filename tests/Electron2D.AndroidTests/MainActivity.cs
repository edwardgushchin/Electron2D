using Android.App;
using Android.Content.PM;
using Android.Util;
using Org.Libsdl.App;
using System.Runtime.InteropServices;

namespace Electron2DAndroidTests;

[Activity(Name = "org.electron2d.tests.MainActivity", Label = "Electron2D tests", MainLauncher = true, Exported = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize |
        ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.Navigation)]
public sealed class MainActivity : SDLActivity
{
    protected override string[] GetLibraries() => ["SDL3"];

    protected override void Main()
    {
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
            Log.Info("Electron2DTests", "CHECK managed contracts");
            ContractChecks.Run(message => Log.Info("Electron2DTests", message));
            Log.Info("Electron2DTests", "CHECK native font precision");
            NativeFontPrecisionTests.Run(FontTestFixtures.OpenSans, FontTestFixtures.Arabic);
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
