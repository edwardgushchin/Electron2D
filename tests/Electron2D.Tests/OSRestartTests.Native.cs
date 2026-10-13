using Electron2D;
using SDL3;

internal static partial class OSRestartTests
{
    private sealed class Box : Entity
    {
        protected override void OnDraw() => DrawRect(new(8, 8, 16, 12), Colors.Red);
    }
    internal static void RunNative()
    {
        using var temp = DirAccess.CreateTemp("os-restart-native-");
        Host(temp.GetCurrentDir(), "dotnet", "replace", "gpu");
        Host(temp.GetCurrentDir(), "apphost", "replace", "compatibility-close");
        Console.WriteLine("OS restart completed native GPU/compatibility pixels, teardown and replacement application startup.");
    }
    private static bool RunWindow(string backend)
    {
        var close = backend.EndsWith("-close", StringComparison.Ordinal);
        ProjectSettings.Set(ProjectSettings.RenderingMethod, close ? backend[..^6] : backend);
        var root = new Window { Size = new(64, 48) }; root.AddChild(new Box()); var rendered = false;
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                using var image = RenderingServer.Service!.Readback();
                Check(image.GetPixel(12, 12).R > .95f && image.GetPixel(12, 12).G < .05f, "Each application actually renders its native frame.");
                if (!rendered)
                {
                    rendered = true;
                    if (close)
                    {
                        var native = SDL.GetWindows(out var count); Check(count == 1 && native is { Length: 1 }, "One native root window.");
                        var input = new SDL.Event { Window = new SDL.WindowEvent { Type = SDL.EventType.WindowCloseRequested, WindowID = SDL.GetWindowID(native![0]) } };
                        Check(SDL.PushEvent(ref input), "Native window close is queued.");
                    }
                    else root.Tree!.Quit(23);
                }
            };
        };
        Check(Engine.Run(root) == (close ? 0 : 23) && rendered, "Native application exits from its scene quit after rendering.");
        return root.IsDisposed && !DisplayServer.IsAvailable && !RenderingServer.IsAvailable && Engine.MainLoop is null;
    }
}
