using Electron2D;
using static TextureArrayTests;

internal static class IntegerCanvasTests
{
    private sealed class Patch(Texture? texture = null, RID rid = default) : Entity
    {
        protected override void OnDraw()
        {
            if (rid.IsValid()) RenderingServer.CanvasItemAddTextureRect(GetCanvasItem(), new(0, 0, 16, 16), rid);
            else if (texture is not null) DrawTextureRect(texture, new(0, 0, 16, 16), false);
            else DrawRect(new(0, 0, 16, 16), Colors.White);
        }
    }
    private const RenderingDevice.TextureUsageBits Roles = RenderingDevice.TextureUsageBits.Sampling | RenderingDevice.TextureUsageBits.CPURead | RenderingDevice.TextureUsageBits.CanCopyTo;
    private static Color Bits(int r, int g, int b, int a = 1) => new(BitConverter.Int32BitsToSingle(r), BitConverter.Int32BitsToSingle(g), BitConverter.Int32BitsToSingle(b), BitConverter.Int32BitsToSingle(a));
    internal static void Run()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var unsigned = new TextureRD(); using var signed = new TextureRD(); using var layers = new TextureArrayRD(); using var atlas = new AtlasTexture { Atlas = unsigned, Region = new(0, 0, 4, 4) };
        using var glsl = Load("IntegerCanvasGLSL"); using var hlsl = Load("IntegerCanvasHLSL");
        using var first = new ShaderMaterial { Shader = glsl }; using var second = new ShaderMaterial { Shader = hlsl };
        var root = new Window { Size = new(256, 128), CanvasItemDefaultTextureFilter = Viewport.DefaultCanvasItemTextureFilter.Nearest };
        var frames = 0; long before = 0, allocated = 0; RenderingDevice device = null!; RID u = default, s = default, a = default, custom = default, proxy = default;
        root.Ready += _ =>
        {
            device = RenderingServer.GetRenderingDevice()!; RenderingServer.SetDefaultClearColor(Colors.Black);
            using var format = new RDTextureFormat { Width = 4, Height = 4, Format = RenderingDevice.DataFormat.R32G32B32A32UINT, UsageBits = Roles }; using var view = new RDTextureView();
            u = device.TextureCreate(format, view); unsigned.TextureRDRID = u;
            format.Format = RenderingDevice.DataFormat.R32G32B32A32SINT; s = device.TextureCreate(format, view); signed.TextureRDRID = s;
            format.TextureType = RenderingDevice.TextureType.TextureArray; format.ArrayLayers = 2; format.Mipmaps = 3; a = device.TextureCreate(format, view); layers.TextureRDRID = a;
            format.TextureType = RenderingDevice.TextureType.Texture; format.ArrayLayers = 1; format.Mipmaps = 1; format.Format = RenderingDevice.DataFormat.R8G8B8A8UINT; custom = device.TextureCreate(format, view); using var values = new TextureRD { TextureRDRID = custom };
            // The shader retains its own borrowed resource reference after this callback.
            var retainedValues = (TextureRD)values.Duplicate(); glsl.SetDefaultTextureParameter("values", retainedValues); hlsl.SetDefaultTextureParameter("values", retainedValues);
            glsl.SetDefaultLayeredTextureParameter("layers", layers); hlsl.SetDefaultLayeredTextureParameter("layers", layers);
            Reject<ArgumentException>(() => first.SetShaderParameter("values", signed)); Reject<ArgumentException>(() => glsl.SetDefaultTextureParameter("values", signed));
            using var white = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); using var cpu = ImageTexture.CreateFromImage(white); Reject<ArgumentException>(() => first.SetShaderParameter("values", cpu));
            proxy = RenderingServer.TextureProxyCreate(RenderingServer.TextureRDCreate(u));
            root.AddChild(new Patch(unsigned) { Name = "uint", Position = new(4, 4) }); root.AddChild(new Patch(signed) { Name = "sint", Position = new(36, 4) }); root.AddChild(new Patch(atlas) { Name = "atlas", Position = new(68, 4) }); root.AddChild(new Patch(rid: proxy) { Name = "proxy", Position = new(100, 4) });
            root.AddChild(new Patch { Name = "glsl", Position = new(132, 4), Material = first }); root.AddChild(new Patch { Name = "hlsl", Position = new(164, 4), Material = second });
            using (var reload = Load("IntegerCanvasGLSL"))
            {
                reload.SetDefaultTextureParameter("values", retainedValues); reload.SetDefaultLayeredTextureParameter("layers", layers);
                using var migration = new ShaderMaterial { Shader = reload }; migration.SetShaderParameter("values", retainedValues); migration.SetShaderLayeredParameter("layers", layers);
                using var floatProgram = Load("FloatCanvas"); reload.SetSPIRV(floatProgram.GetSPIRV());
                Check(reload.GetDefaultTextureParameter("values") is null && reload.GetDefaultLayeredTextureParameter("layers") is null && migration.GetShaderParameter("values") is null && migration.GetShaderLayeredParameter("layers") is null, "Scalar reload drops incompatible defaults and overrides atomically.");
            }
            ColdChecks(device, format, view);
            root.Tree!.ProcessFrameStarted += _ => before = GC.GetAllocatedBytesForCurrentThread();
            RenderingServer.FramePreDraw += () =>
            {
                device.TextureClear(u, frames % 2 == 0 ? Bits(1, 0, 0) : Bits(0, 0, 1), 0, 1, 0, 1);
                device.TextureClear(s, frames % 2 == 0 ? Bits(0, 1, 0) : Bits(1, 0, 0), 0, 1, 0, 1);
                device.TextureClear(a, Bits(-10, 0, 0, 0), 0, 3, 0, 1); device.TextureClear(a, Bits(0, -10, 0, 0), 0, 3, 1, 1);
                device.TextureClear(custom, Bits(128, 128, 0, 255), 0, 1, 0, 1);
                first.SetShaderParameter("selectedLayer", (float)(frames % 2)); second.SetShaderParameter("selectedLayer", (float)((frames + 1) % 2));
            };
            RenderingServer.FramePostDraw += () =>
            {
                var bytes = GC.GetAllocatedBytesForCurrentThread() - before; if (frames >= 24) allocated += bytes;
                if (frames < 3 || frames == 55)
                {
                    using var image = root.GetTexture().GetImage()!; var expected = frames % 2 == 0 ? Colors.Red : Colors.Blue;
                    Pixel(image, 12, expected); Pixel(image, 44, frames % 2 == 0 ? Colors.Green : Colors.Red); Pixel(image, 76, expected); Pixel(image, 108, expected);
                    Pixel(image, 140, frames % 2 == 0 ? new(.4f, .5f, 0, 1) : new(.5f, .4f, 0, 1)); Pixel(image, 172, frames % 2 == 0 ? new(.5f, .4f, 0, 1) : new(.4f, .5f, 0, 1));
                }
                frames++; if (frames != 56) return;
                Check(allocated == 0, $"Integer native prepared frames allocated {allocated} managed bytes.");
                using (var image = unsigned.GetImage()!) Check(image.GetPixel(0, 0) == Colors.Blue, "UInt snapshot keeps raw component values.");
                using (var image = layers.GetLayerData(0)!) Check(image.HasMipmaps && image.GetPixel(0, 0).R == -10, "Signed array snapshot preserves negative values and mip chain.");
                first.Shader = null; second.Shader = null; retainedValues.Dispose(); root.Tree!.Quit();
            };
        };
        Engine.Run(root); Check(device.IsDisposed && root.IsDisposed && frames == 56, "Integer renderer shutdown.");
        var filtered = new Window { Size = new(256, 128) }; RenderingDevice rejectedDevice = null!; using var filteredTexture = new TextureRD();
        filtered.Ready += _ => { rejectedDevice = RenderingServer.GetRenderingDevice()!; using var format = new RDTextureFormat { Format = RenderingDevice.DataFormat.R32UINT, UsageBits = Roles }; using var view = new RDTextureView(); filteredTexture.TextureRDRID = rejectedDevice.TextureCreate(format, view); filtered.AddChild(new Patch(filteredTexture)); };
        Reject<NotSupportedException>(() => Engine.Run(filtered)); Check(rejectedDevice.IsDisposed && filtered.IsDisposed && RenderingServer.Service is null, "Linear integer filtering rejects before submission and releases renderer lifetime.");
        Console.WriteLine($"Integer canvas: raw signed/unsigned direct, atlas/proxy, GLSL/HLSL mixed descriptors/array layers, raw snapshots; 24 warm + 32 active frames, {allocated} managed bytes.");
    }
    private static void ColdChecks(RenderingDevice device, RDTextureFormat format, RDTextureView view)
    {
        var kinds = new[] { RenderingDevice.DataFormat.R8SINT, RenderingDevice.DataFormat.R8UINT, RenderingDevice.DataFormat.R16SINT, RenderingDevice.DataFormat.R16UINT, RenderingDevice.DataFormat.R32SINT, RenderingDevice.DataFormat.R32UINT };
        foreach (var kind in kinds)
        {
            format.Format = kind; format.Width = 2; format.Height = 2; format.Mipmaps = 2; format.UsageBits = Roles;
            var rid = device.TextureCreate(format, view); using var resource = new TextureRD { TextureRDRID = rid }; var value = RenderingDevice.PixelType(kind) == 1 ? -7 : 249;
            device.TextureClear(rid, Bits(value, 0, 0), 0, 2, 0, 1);
            using (var image = resource.GetImage()!) { Check(image.PixelFormat == Image.Format.Rgbaf && image.HasMipmaps && image.GetPixel(0, 0).R == value && image.GetPixel(0, 0).A == 1, "Integer snapshots preserve scalar width/signedness, missing channels and mips."); }
            if (kind == RenderingDevice.DataFormat.R32UINT)
            {
                device.TextureClear(rid, Bits(unchecked((int)0xffffffff), 0, 0), 0, 2, 0, 1);
                Check(BitConverter.ToUInt32(device.TextureGetData(rid, 0)) == uint.MaxValue, "Raw device readback preserves all UInt32 bits; Image float snapshots have float32 precision.");
            }
            device.FreeRID(rid);
        }
    }
    private static void Pixel(Image image, int x, Color expected) { var actual = image.GetPixel(x, 12); Check(Math.Abs(actual.R - expected.R) < .02f && Math.Abs(actual.G - expected.G) < .02f && Math.Abs(actual.B - expected.B) < .02f, $"Integer canvas {x}: {actual} != {expected}."); }
}
