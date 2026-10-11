using Electron2D;

internal static partial class LightingTests
{
    private static void NativeCompatibility()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "compatibility");
        using var canvas = new CanvasTexture { TextureFilter = TextureFilter.Nearest };
        var window = new Window { Size = new(16, 16), Title = "Canvas texture compatibility" }; window.AddChild(new Surface { Area = new(0, 0, 16, 16), Texture = canvas });
        var completed = false;
        window.Ready += _ => RenderingServer.FramePostDraw += () => { using var image = window.GetTexture().GetImage()!; Pixel(image, 8, 8, Colors.White); completed = true; window.Tree!.Quit(); };
        Check(Engine.Run(window) == 0 && completed, "Empty diffuse canvas texture uses the white fallback");
        var rejected = new Window { Size = new(16, 16), Title = "Lighting capability validation" }; rejected.AddChild(new Surface { Area = new(0, 0, 16, 16) }); rejected.AddChild(new DirectionalLight());
        Reject<NotSupportedException>(() => Engine.Run(rejected)); Check(rejected.IsDisposed, "Unsupported lighting releases the native host");
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu");
        Console.WriteLine("Canvas texture compatibility pixels and explicit lighting rejection passed.");
    }
}
