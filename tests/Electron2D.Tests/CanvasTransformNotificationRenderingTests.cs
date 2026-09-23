using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyTransformNotifications(string backend, string? fixture = null)
    {
        using var shader = fixture is null ? null : LoadShader(fixture);
        using var material = shader is null ? null : new ShaderMaterial { Shader = shader };
        var window = new Window { Size = new(256, 128) };
        var camera = new Camera { Position = new(50, 40), LimitEnabled = false }; window.AddChild(camera);
        var item = LayerBox("Item", new(20, 20), Colors.Red, material); window.AddChild(item);
        var frames = 0; var notices = 0;
        window.Ready += _ =>
        {
            var center = window.GetVisibleRect().Size * 0.5f;
            camera.Position = center;
            camera.ForceUpdateTransform();
            camera.TransformChanged += _ => notices++;
            var server = RenderingServer.Instance!; server.SetDefaultClearColor(Colors.Black);
            server.FramePreDraw += () =>
            {
                if (frames == 0)
                {
                    camera.Position = center + new Vector2(10, 0);
                    Check(notices == 0 && camera.GetScreenCenterPosition() == center, "Position changed after frame flush remains queued.");
                }
                if (frames == 2)
                {
                    camera.Position = center + new Vector2(20, 0); camera.ForceUpdateTransform(); camera.ForceUpdateTransform();
                    Check(camera.GetScreenCenterPosition() == center + new Vector2(20, 0), "Force publishes the camera before this submission.");
                }
            };
            server.FramePostDraw += () =>
            {
                using var pixels = server.Readback(); frames++;
                Pixel(pixels, 21, 21, frames == 1 ? Colors.Red : Colors.Black);
                Pixel(pixels, 11, 21, frames == 2 ? Colors.Red : Colors.Black);
                Pixel(pixels, 1, 21, frames >= 3 ? Colors.Red : Colors.Black);
                if (frames == 4)
                {
                    Check(notices == 2 && item.Draws == 1, "Queued and forced camera updates each deliver once and retain item drawing."); window.Tree!.Quit();
                }
            };
        };
        Engine.Instance.Run(window); Released(window); Check(frames == 4, "Four queued/forced camera frames.");
        Console.WriteLine($"Transform notification native camera pixels passed: {backend}/{fixture ?? "default"}.");
    }
}
