using Electron2D;
using static TextureArrayTests;

internal static partial class CanvasHDRTests
{
    internal static void RunSources()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var source = Image.CreateEmpty(4, 4, true, Image.Format.Rgba8); source.Fill(new(128f / 255, 128f / 255, 128f / 255, 1)); source.GenerateMIPMaps();
        using var encoded = ImageTexture.CreateFromImage(source);
        using var unorm = new TextureRD(); using var srgb = new TextureRD();
        using var glsl = Load("HDRCanvasGLSL"); using var hlsl = Load("HDRCanvasHLSL");
        using var a = new ShaderMaterial { Shader = glsl }; using var b = new ShaderMaterial { Shader = hlsl };
        foreach (var material in new[] { a, b })
        { material.SetShaderParameter("values", encoded); material.SetShaderParameter("lod", 9f); material.SetShaderParameter("multiplier", .5f); material.SetShaderParameter("dataWeight", 0f); }
        var root = new Window { Size = new(192, 96), UseHDR2D = true };
        var low = new SubViewport { Size = new(16, 16), RenderTargetUpdateMode = ViewportUpdateMode.Always };
        var high = new SubViewport { Size = new(16, 16), UseHDR2D = true, RenderTargetUpdateMode = ViewportUpdateMode.Always };
        foreach (var view in new[] { low, high }) view.AddChild(new Sprite { Texture = encoded, Centered = false, Scale = new(4, 4), TextureFilter = TextureFilter.Nearest });
        root.AddChild(low); root.AddChild(high); using var lowTexture = low.GetTexture(); using var highTexture = high.GetTexture();
        root.AddChild(new Sprite { Texture = lowTexture, Centered = false, Position = new(80, 4) });
        root.AddChild(new Sprite { Texture = highTexture, Centered = false, Position = new(104, 4) });
        root.AddChild(new Sprite { Texture = encoded, Centered = false, Position = new(4, 4), Scale = new(4, 4), TextureFilter = TextureFilter.NearestWithMipmaps });
        root.AddChild(new Sprite { Texture = unorm, Centered = false, Position = new(28, 4), Scale = new(4, 4), TextureFilter = TextureFilter.NearestWithMipmaps, Material = a });
        root.AddChild(new Sprite { Texture = srgb, Centered = false, Position = new(52, 4), Scale = new(4, 4), TextureFilter = TextureFilter.NearestWithMipmaps, Material = b });
        using var output = root.GetTexture(); RenderingDevice device = null!; RID u = default, s = default; var frame = 0;
        root.Ready += _ =>
        {
            device = RenderingServer.GetRenderingDevice()!;
            using var format = new RDTextureFormat { Width = 4, Height = 4, MIPMaps = 2, Format = RenderingDevice.DataFormat.R8G8B8A8UNORM, UsageBits = RenderingDevice.TextureUsageBits.Sampling | RenderingDevice.TextureUsageBits.CPURead | RenderingDevice.TextureUsageBits.CanCopyTo };
            using var view = new RDTextureView(); var data = new byte[80];
            for (var i = 0; i < data.Length; i += 4) { data[i] = data[i + 1] = data[i + 2] = i < 64 ? (byte)128 : (byte)64; data[i + 3] = 255; }
            u = device.TextureCreate(format, view, [data]); unorm.TextureRDRID = u;
            format.Format = RenderingDevice.DataFormat.R8G8B8A8SRGB; s = device.TextureCreate(format, view, [data]); srgb.TextureRDRID = s;
            RenderingServer.FramePostDraw += () =>
            {
                using var pixels = output.GetImage()!;
                Pixel(pixels, 8, 8, Linear(128f / 255));
                var expected = frame == 0 ? Linear(64f / 255) * .5f : 128f / 255;
                Pixel(pixels, 32, 8, expected); Pixel(pixels, 56, 8, expected);
                Pixel(pixels, 84, 8, Linear(128f / 255)); Pixel(pixels, 108, 8, Linear(128f / 255));
                using var lowPixels = lowTexture.GetImage()!; using var highPixels = highTexture.GetImage()!;
                Pixel(lowPixels, 4, 4, 128f / 255); Pixel(highPixels, 4, 4, high.UseHDR2D ? Linear(128f / 255) : 128f / 255);
                if (++frame == 1)
                {
                    foreach (var material in new[] { a, b }) { material.SetShaderParameter("multiplier", 0f); material.SetShaderParameter("dataWeight", 1f); }
                    high.UseHDR2D = false; high.Size = new(20, 16);
                }
                else { device.FreeRID(u); device.FreeRID(s); root.Tree!.Quit(); }
            };
        };
        Check(Engine.Run(root) == 0 && frame == 2 && device.IsDisposed, "HDR color source lifetime.");
        Console.WriteLine("HDR sources: full/partial mip chains and high-LOD clamping, native sRGB without double transfer, UNORM derived view, GLSL/HLSL canvas versus numeric data samplers/uniforms, independent/shared viewport colors and resize passed.");
    }
}
