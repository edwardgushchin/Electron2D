using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;

internal static partial class RenderingDeviceTexelTests
{
    internal static void Canvas(string language)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var texture = new TextureRD(); var root = new Window { Size = new(64, 64) }; RenderingDevice device = null!; Action<bool> draw = null!; var frames = 0; long before = 0;
        root.Ready += _ =>
        {
            device = RenderingServer.GetRenderingDevice()!;
            var producer = GraphicsProducer(device, language == "GLSL" ? ShaderLanguage.GLSL : ShaderLanguage.HLSL, false);
            texture.TextureRDRID = producer.Target; draw = producer.Draw;
            root.AddChild(new Sprite { Texture = texture, Centered = false, Position = new(8, 8), Scale = new(4, 4) });
            RenderingServer.FramePreDraw += Generate; RenderingServer.FramePostDraw += Captured;
        };
        Engine.Run(root);
        Check(root.IsDisposed && device.IsDisposed && RenderingServer.GetRenderingDevice() is null, "Renderer shutdown retires texel programs and their borrowed canvas output.");
        Console.WriteLine(language + ": changing texel vertex/fragment output reached retained canvas; 32 full owner frames=0 managed bytes.");
        void Generate() { if (frames >= 8) before = GC.GetAllocatedBytesForCurrentThread(); draw(frames % 2 == 0); }
        void Captured()
        {
            if (frames >= 8) Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared texel graphics and ordinary canvas submission allocate zero managed owner bytes.");
            if (frames is 0 or 39)
            {
                using var pixels = root.GetTexture().GetImage()!; var pixel = pixels.GetPixel(9, 9); var green = frames % 2 == 0;
                Check(Math.Abs(pixel.R - (green ? 0 : 1)) < .01f && Math.Abs(pixel.G - (green ? 1 : 0)) < .01f && pixel.B < .01f, "Retained canvas samples the current typed-buffer-produced image.");
            }
            if (++frames != 40) return; RenderingServer.FramePreDraw -= Generate; RenderingServer.FramePostDraw -= Captured; root.Tree!.Quit();
        }
    }
}
