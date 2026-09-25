using Android.App;
using Android.Content.PM;
using Android.Util;
using Electron2D;
using Org.Libsdl.App;

namespace Electron2DAndroidProbe;

[Activity(Label = "Electron2D probe", MainLauncher = true, Exported = true,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize |
        ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.Navigation)]
public sealed class MainActivity : SDLActivity
{
    protected override string[] GetLibraries() => ["SDL3"];

    protected override void Main()
    {
        try
        {
            var window = new Window { Size = new(640, 360) };
            window.AddChild(new ProbeNode { ProcessEnabled = true });
            Engine.Instance.MaxFPS = 60;
            Log.Info("Electron2DProbe", "RUN");
            var exitCode = Engine.Instance.Run(window);
            Log.Info("Electron2DProbe", $"DONE {exitCode}");
        }
        catch (Exception error)
        {
            Log.Error("Electron2DProbe", error.ToString());
        }
    }

    private sealed class ProbeNode : Entity
    {
        private int _frames;
        protected override void OnDraw()
        {
            DrawRect(new(0, 0, 640, 360), Colors.Red);
            Log.Info("Electron2DProbe", "DRAW");
        }
        protected override void OnProcess(double delta)
        {
            if (++_frames == 900)
                Tree!.Quit();
        }
    }
}
