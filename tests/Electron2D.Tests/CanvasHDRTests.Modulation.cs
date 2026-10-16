using Electron2D;
using static TextureArrayTests;

internal static partial class CanvasHDRTests
{
    internal static void RunModulation(string backend = "gpu", bool HDR = true)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        var root = new Window { Size = new(192, 96), UseHDR2D = HDR };
        var modulator = new CanvasModulate { Color = new(.5f, .5f, .5f, .8f) }; root.AddChild(modulator);
        Entity Box(Color color, Vector2 size)
        { var box = new Entity(); box.Draw += item => item.DrawRect(new(Vector2.Zero, size), color); return box; }
        var ordinary = Box(new(.5f, .5f, .5f, 1), new(16, 16)); ordinary.Position = new(4, 4); root.AddChild(ordinary);
        var group = new CanvasGroup { Position = new(28, 4), FitMargin = 0, ClearMargin = 0, SelfModulate = new(.5f, .5f, .5f, .5f) };
        group.AddChild(Box(new(.5f, .5f, .5f, 1), new(16, 16))); root.AddChild(group);
        var mask = Box(new(.5f, .5f, .5f, .5f), new(16, 16)); mask.Position = new(52, 4); mask.ClipChildren = ClipChildrenMode.Only;
        mask.AddChild(Box(new(.5f, .5f, .5f, 1), new(8, 8))); root.AddChild(mask);
        var drawn = Box(new(.5f, .5f, .5f, .5f), new(16, 16)); drawn.Position = new(76, 4); drawn.ClipChildren = ClipChildrenMode.AndDraw;
        drawn.AddChild(Box(new(.5f, .5f, .5f, 1), new(8, 8))); root.AddChild(drawn);
        using var texture = root.GetTexture(); var frame = 0; long before = 0, allocated = 0;
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            root.Tree!.ProcessFrameStarted += _ => before = GC.GetAllocatedBytesForCurrentThread();
            RenderingServer.FramePreDraw += () => { modulator.Color = new((frame & 1) == 0 ? .5f : .25f, .5f, .5f, .8f); ordinary.QueueRedraw(); };
            RenderingServer.FramePostDraw += () =>
            {
                if (frame >= 24) allocated += GC.GetAllocatedBytesForCurrentThread() - before;
                if (frame == 0 || frame == 87)
                {
                    using var pixels = texture.GetImage()!;
                    var baseColor = (HDR ? Linear(.5f) * Linear(modulator.Color.R) : .5f * modulator.Color.R) * .8f;
                    var tolerance = HDR ? .0003f : .006f;
                    Pixel(pixels, 8, 8, baseColor, tolerance);
                    Pixel(pixels, 32, 8, baseColor * (HDR ? Linear(.5f) : .5f) * .5f, tolerance);
                    Pixel(pixels, 56, 8, baseColor * .5f, tolerance);
                    Pixel(pixels, 80, 8, baseColor * .55f, tolerance);
                    Pixel(pixels, 88, 16, baseColor * .25f, tolerance);
                }
                if (++frame == 88) { Check(allocated == 0, $"Modulated HDR={HDR}/{backend} replay allocated {allocated} owner-thread bytes."); root.Tree!.Quit(); }
            };
        };
        Check(Engine.Run(root) == 0 && frame == 88, "Modulated canvas host.");
        Console.WriteLine($"Canvas modulation HDR={HDR}/{backend}: separate command/canvas transfer, unshaded built-in group/Only-mask, shared AndDraw geometry; 24 warm + 64 active frames, {allocated} bytes.");
    }
}
