using Electron2D;
using Filter = Electron2D.TextureFilter;
using Repeat = Electron2D.TextureRepeat;

internal static partial class RenderingRuntimeTests
{
    private static bool VerifyCanvasSampling(string backend, string? fixture = null)
    {
        using var image = Image.CreateFromData(2, 1, false, Image.Format.Rgba8, new byte[] { 255, 0, 0, 255, 0, 0, 255, 255 });
        using var texture = ImageTexture.CreateFromImage(image);
        using var shader = fixture is null ? null : LoadShader(fixture);
        using var material = shader is null ? null : new ShaderMaterial { Shader = shader };
        var window = new Window { Size = new(96, 80) };
        var parent = new Entity(); window.AddChild(parent);
        var inherited = new Sprite { Name = "inherited", Texture = texture, Centered = false, Position = new(4, 4), Scale = new(8, 8), Material = material };
        var nearest = new Sprite { Name = "nearest", Texture = texture, Centered = false, Position = new(4, 20), Scale = new(8, 8), TextureFilter = Filter.Nearest, Material = material };
        var neutral = new Node { Name = "neutral" };
        var independent = new Sprite { Texture = texture, Centered = false, Position = new(4, 36), Scale = new(8, 8), Material = material };
        parent.AddChild(inherited); parent.AddChild(nearest); parent.AddChild(neutral); neutral.AddChild(independent);
        var frames = 0; var software = false;
        window.AddChild(new CanvasNode
        {
            ReadyAction = n =>
            {
                var server = RenderingServer.Instance!;
                server.SetDefaultClearColor(Colors.Black);
                software = server.GetCurrentRenderingDriverName() == "software";
                if (software) window.CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest;
                server.FramePostDraw += () =>
                {
                    using var frame = server.Readback();
                    try
                    {
                        switch (++frames)
                        {
                            case 1:
                                if (software) { Pixel(frame, 11, 6, Colors.Red); Pixel(frame, 11, 38, Colors.Red); }
                                else { Mixed(frame, 11, 6); Mixed(frame, 11, 38); }
                                Pixel(frame, 11, 22, Colors.Red);
                                parent.TextureFilter = Filter.Nearest;
                                break;
                            case 2:
                                Pixel(frame, 11, 6, Colors.Red); Pixel(frame, 11, 22, Colors.Red);
                                if (software) Pixel(frame, 11, 38, Colors.Red); else Mixed(frame, 11, 38);
                                window.CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest;
                                parent.TextureFilter = Filter.ParentNode;
                                break;
                            case 3:
                                Pixel(frame, 11, 6, Colors.Red); Pixel(frame, 11, 38, Colors.Red);
                                inherited.RegionEnabled = true; inherited.RegionRect = new(-2, 0, 6, 1);
                                parent.TextureRepeat = Repeat.Enabled;
                                break;
                            case 4:
                                Pixel(frame, 40, 6, Colors.Red); Pixel(frame, 48, 6, Colors.Blue);
                                if (backend == "gpu") parent.TextureRepeat = Repeat.Mirror;
                                else window.CanvasItemDefaultTextureRepeat = Viewport.DefaultCanvasItemTextureRepeat.Enabled;
                                break;
                            case 5:
                                Pixel(frame, 40, 6, backend == "gpu" ? Colors.Blue : Colors.Red);
                                image.Fill(Colors.Green); texture.Update(image);
                                break;
                            case 6:
                                Pixel(frame, 40, 6, Colors.Green); Pixel(frame, 11, 22, Colors.Green); Pixel(frame, 11, 38, Colors.Green);
                                n.Tree!.Quit();
                                break;
                        }
                    }
                    catch (Exception e) { throw new InvalidOperationException($"Sampling {backend}/{fixture ?? "default"} frame {frames}: {e.Message}", e); }
                };
            }
        });
        Engine.Instance.Run(window); Released(window);
        Check(frames == 6, "Sampling state changes reach successive native frames.");
        Console.WriteLine($"Canvas sampling pixel checks passed: {backend}/{fixture ?? "default"}.");

        return software;

        static void Mixed(Image frame, int x, int y)
        {
            var color = frame.GetPixel(x, y);
            Check(color.R is > 0.25f and < 0.75f && color.B is > 0.25f and < 0.75f && color.G < 0.02f,
                $"Linear filtering must blend red and blue at ({x}, {y}), got {color}.");
        }
    }

    private static void VerifySamplingCapabilities(string backend, bool software)
    {
        using var image = Image.CreateEmpty(4, 4, true, Image.Format.Rgba8);
        var data = image.GetData();
        for (var i = 0; i < data.Length; i += 4)
        {
            data[i + (i < image.GetMipmapOffset(1) ? 0 : i < image.GetMipmapOffset(2) ? 1 : 2)] = 255;
            data[i + 3] = 255;
        }
        using var levels = Image.CreateFromData(4, 4, true, Image.Format.Rgba8, data);
        using var texture = ImageTexture.CreateFromImage(levels);
        if (backend != "gpu")
        {
            foreach (var filter in software ? new[] { Filter.NearestWithMipmaps, Filter.LinearWithMipmapsAnisotropic, Filter.Nearest, Filter.Linear }
                : new[] { Filter.NearestWithMipmaps, Filter.LinearWithMipmapsAnisotropic, Filter.Nearest })
            {
                var rejected = new Window(); var completed = false;
                rejected.AddChild(new CanvasNode
                {
                    TextureFilter = filter,
                    TextureRepeat = filter == Filter.Nearest ? Repeat.Mirror : Repeat.Disabled,
                    DrawAction = n => n.DrawTexture(texture, Vector2.Zero),
                    ReadyAction = _ => RenderingServer.Instance!.FramePostDraw += () => completed = true,
                });
                Reject<NotSupportedException>(() => Engine.Instance.Run(rejected)); Released(rejected);
                Check(!completed, "Unsupported sampling fails before a frame is reported complete.");
            }
            Console.WriteLine("Compatibility sampling limits explicitly rejected.");
            return;
        }
        var settings = ProjectSettings.Instance;
        var original = settings.Get(ProjectSettings.UseNearestMipmapFilter);
        try
        {
            foreach (var nearestMip in new[] { false, true })
            {
                settings.Set(ProjectSettings.UseNearestMipmapFilter, nearestMip);
                var window = new Window { Size = new(80, 64) };
                var node = new CanvasNode
                {
                    TextureFilter = Filter.Nearest,
                    DrawAction = n =>
                    {
                        n.DrawTextureRect(texture, new Rect2(4, 4, 2, 2), false);
                        n.DrawTextureRect(texture, new Rect2(12, 4, 3, 3), false);
                    },
                };
                var frames = 0;
                node.ReadyAction = n => RenderingServer.Instance!.FramePostDraw += () =>
                {
                    using var frame = RenderingServer.Instance.Readback();
                    var filter = node.TextureFilter;
                    if (filter is Filter.Nearest or Filter.Linear)
                    {
                        Pixel(frame, 4, 4, Colors.Red); Pixel(frame, 13, 5, Colors.Red);
                    }
                    else
                    {
                        Pixel(frame, 4, 4, Colors.Green);
                        if (filter is Filter.NearestWithMipmaps or Filter.LinearWithMipmaps)
                        {
                            var mixed = frame.GetPixel(13, 5);
                            if (nearestMip) Pixel(frame, 13, 5, Colors.Red);
                            else Check(mixed.R is > 0.2f and < 0.9f && mixed.G is > 0.1f and < 0.8f, "Linear mip interpolation samples both levels.");
                        }
                    }
                    if (++frames == 6) n.Tree!.Quit(); else node.TextureFilter = (Filter)(frames + 1);
                };
                window.AddChild(node); Engine.Instance.Run(window); Released(window);
                Check(frames == 6, "All six concrete GPU filter modes execute with both mip interpolation settings.");
            }
        }
        finally { settings.Set(ProjectSettings.UseNearestMipmapFilter, original); }
        VerifyCanvasAnisotropy();
        Console.WriteLine("GPU mipmap and anisotropy checks passed.");
    }

    private static void VerifyCanvasAnisotropy()
    {
        using var image = Image.CreateEmpty(64, 64, false, Image.Format.Rgba8);
        for (var y = 0; y < 64; y++)
            for (var x = 0; x < 64; x++) image.SetPixel(x, y, x % 4 < 2 ? Colors.Red : Colors.Blue);
        image.GenerateMipmaps();
        using var texture = ImageTexture.CreateFromImage(image);
        var window = new Window { Size = new(96, 64), AnisotropicFilteringLevel = Viewport.AnisotropicFiltering.Disabled };
        var frames = 0; var initial = 0f; var final = 0f;
        window.AddChild(new CanvasNode
        {
            TextureFilter = Filter.LinearWithMipmapsAnisotropic,
            DrawAction = n => n.DrawTextureRect(texture, new Rect2(4, 4, 64, 4), false),
            ReadyAction = n => RenderingServer.Instance!.FramePostDraw += () =>
            {
                using var frame = RenderingServer.Instance.Readback();
                var contrast = 0f;
                for (var x = 8; x < 60; x++) contrast += System.MathF.Abs(frame.GetPixel(x, 5).R - frame.GetPixel(x, 5).B);
                if (frames == 0) initial = contrast;
                if (++frames == 5) { final = contrast; n.Tree!.Quit(); }
                else window.AnisotropicFilteringLevel = (Viewport.AnisotropicFiltering)frames;
            }
        });
        Engine.Instance.Run(window); Released(window);
        Check(frames == 5 && final > initial + 5, $"Anisotropy must preserve stretched stripe contrast: {initial} -> {final}.");
    }
}
