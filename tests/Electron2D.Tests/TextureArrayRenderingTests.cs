using Electron2D;
using static TextureArrayTests;

internal static class TextureArrayRenderingTests
{
    private sealed class Patch : Entity
    {
        protected override void OnDraw() => DrawRect(new(0, 0, 24, 24), Colors.White);
    }
    internal static void RunHost()
    {
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_RENDER_BACKEND") ?? "gpu";
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var red = MipImage(Colors.Red, Colors.Yellow, Colors.Cyan);
        using var blue = MipImage(Colors.Blue, Colors.Magenta, Colors.Green);
        using var green = Image.CreateEmpty(4, 4, true, Image.Format.Rgba8); green.Fill(Colors.Green);
        using var white = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); white.Fill(Colors.White);
        using var ordinary = ImageTexture.CreateFromImage(white);
        using var array = new TextureArray(); array.CreateFromImages([red, blue]);
        using var first = Load("ArrayHLSL"); using var second = Load("ArrayGLSL");
        using var leftMaterial = new ShaderMaterial { Shader = first }; using var middleMaterial = new ShaderMaterial { Shader = second };
        first.SetDefaultLayeredTextureParameter("layers", array); second.SetDefaultLayeredTextureParameter("layers", array);
        first.SetDefaultTextureParameter("detailMap", ordinary); second.SetDefaultTextureParameter("detailMap", ordinary);
        var root = new Window { Size = new(96, 32) };
        var left = new Patch { Name = "left", Position = new(4, 4), Material = leftMaterial };
        var middle = new Patch { Name = "middle", Position = new(36, 4), Material = middleMaterial };
        var right = new Patch { Name = "right", Position = new(68, 4) };
        root.AddChild(left); root.AddChild(middle); root.AddChild(right);
        var frames = 0; long before = 0, allocated = 0; nint initialNative = 0; RID source = default, proxy = default, ownedMaterial = default, ownedShader = default;
        if (backend == "compatibility")
        {
            Reject<NotSupportedException>(() => Engine.Run(root)); Check(root.IsDisposed && RenderingServer.Service is null, "Compatibility rejects shader arrays and cleans up.");
            Console.WriteLine("Compatibility rejected array shader use before drawing and released the host."); return;
        }
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black);
            Check(RenderingServer.GetCurrentRenderingMethod() == "gpu" && RenderingServer.GetCurrentRenderingDriverName() != "software", "Native GPU backend selected.");
            source = ServerChecks(red, blue, ordinary);
            proxy = RenderingServer.TextureProxyCreate(source); var nested = RenderingServer.TextureProxyCreate(proxy);
            using (var pixels = RenderingServer.Texture2DLayerGet(nested, 1)!) Check(pixels.GetPixel(0, 0) == Colors.Blue, "Nested layered proxy read.");
            RenderingServer.FreeRID(nested);
            ownedShader = RenderingServer.ShaderCreate(); RenderingServer.ShaderSetSPIRV(ownedShader, first.GetSPIRV());
            RenderingServer.ShaderSetDefaultTextureParameter(ownedShader, "detailMap", ordinary.GetRID());
            RenderingServer.ShaderSetDefaultTextureParameter(ownedShader, "layers", source);
            Check(RenderingServer.ShaderGetDefaultTextureParameter(ownedShader, "layers") == source, "Layered default RID roundtrip.");
            ownedMaterial = RenderingServer.MaterialCreate(); RenderingServer.MaterialSetShader(ownedMaterial, ownedShader);
            RenderingServer.MaterialSetParam(ownedMaterial, "layers", proxy);
            Check(RenderingServer.MaterialGetParam(ownedMaterial, "layers") == proxy, "Layered override RID roundtrip.");
            RenderingServer.CanvasItemSetMaterial(right.GetCanvasItem(), ownedMaterial);
            root.Tree!.ProcessFrameStarted += tree =>
            {
                before = GC.GetAllocatedBytesForCurrentThread();
                leftMaterial.SetShaderParameter("selectedLayer", (float)(frames % 2)); middleMaterial.SetShaderParameter("selectedLayer", (float)((frames + 1) % 2));
                RenderingServer.MaterialSetParam(ownedMaterial, "selectedLayer", (float)(frames % 2));
            };
            RenderingServer.FramePostDraw += () =>
            {
                var bytes = GC.GetAllocatedBytesForCurrentThread() - before; frames++; if (frames > 24) allocated += bytes;
                if (frames <= 3)
                {
                    using var image = root.GetTexture().GetImage()!;
                    if (frames == 1) { initialNative = VerifyMipUpload(array); Pixel(image, 16, Colors.Red); Pixel(image, 48, Colors.Blue); Pixel(image, 80, Colors.Red); array.UpdateLayer(green, 1); }
                    if (frames == 2)
                    {
                        Check(NativeTexture(array).Handle == initialNative, "Layer update reuses native array allocation.");
                        Pixel(image, 16, Colors.Green); Pixel(image, 48, Colors.Red); Pixel(image, 80, Colors.Blue);
                        using var hdr0 = Image.CreateEmpty(4, 4, false, Image.Format.Rgbaf); hdr0.Fill(new(1.5f, .2f, .4f, 1));
                        using var hdr1 = Image.CreateEmpty(4, 4, false, Image.Format.Rgbaf); hdr1.Fill(new(.25f, .5f, .75f, 1)); array.CreateFromImages([hdr0, hdr1]);
                    }
                    if (frames == 3) { Pixel(image, 16, new(1, .2f, .4f, 1)); Pixel(image, 48, new(.25f, .5f, .75f, 1)); array.CreateFromImages([red, blue]); }
                }
                if (frames == 16) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
                if (frames != 88) return;
                Check(allocated == 0, $"Prepared active array frames allocated {allocated} owner-thread managed bytes.");
                using var result = root.GetTexture().GetImage()!; Pixel(result, 16, Colors.Blue); Pixel(result, 48, Colors.Red); Pixel(result, 80, Colors.Blue);
                if (Environment.GetEnvironmentVariable("ELECTRON2D_ARRAY_SNAPSHOT") is { } path) result.SavePNG(path);
                RenderingServer.FreeRID(ownedMaterial); RenderingServer.FreeRID(ownedShader); RenderingServer.FreeRID(source);
                Reject<InvalidOperationException>(() => RenderingServer.Texture2DLayerGet(proxy, 0)); RenderingServer.FreeRID(proxy); root.Tree!.Quit();
            };
        };
        Engine.Run(root); Check(root.IsDisposed && RenderingServer.Service is null && frames == 88, "Array native lifecycle and shutdown.");
        Console.WriteLine($"Image arrays: HLSL/GLSL and owned RID pixels, compatible updates/HDR reallocation, proxies/defaults/guards/cleanup; 24 warmup + 64 active frames, {allocated} owner-thread managed bytes.");
    }
    private static RID ServerChecks(Image red, Image blue, Texture ordinary)
    {
        Reject<ArgumentOutOfRangeException>(() => RenderingServer.Texture2DLayeredCreate([red], (TextureLayered.LayeredType)99));
        var source = RenderingServer.Texture2DLayeredCreate([red, blue], TextureLayered.LayeredType.Array);
        var replacing = RenderingServer.Texture2DLayeredCreate([blue, red], TextureLayered.LayeredType.Array);
        var alias = RenderingServer.TextureProxyCreate(replacing);
        RenderingServer.TextureSetPath(replacing, "array-source"); RenderingServer.TextureReplace(source, replacing);
        Check(RenderingServer.TextureGetPath(source) == "array-source" && RenderingServer.TextureGetFormat(source) == Image.Format.Rgba8, "Replacement metadata.");
        using (var copied = RenderingServer.Texture2DLayerGet(alias, 0)!) Check(copied.GetPixel(0, 0) == Colors.Blue, "Replacement redirects layered aliases.");
        RenderingServer.FreeRID(alias); Reject<ArgumentException>(() => RenderingServer.Texture2DLayerGet(replacing, 0));
        RenderingServer.Texture2DUpdate(source, red, 0); RenderingServer.Texture2DUpdate(source, blue, 1);
        Reject<ArgumentOutOfRangeException>(() => RenderingServer.Texture2DUpdate(source, red, 2));
        Reject<ArgumentException>(() => RenderingServer.Texture2DLayerGet(ordinary.GetRID(), 0)); Reject<ArgumentException>(() => RenderingServer.Texture2DGet(source)); Reject<InvalidOperationException>(() => RenderingServer.FreeRID(ordinary.GetRID()));
        var borrowed = new TextureArray(); borrowed.CreateFromImages([red]); var borrowedRID = borrowed.GetRID(); Reject<InvalidOperationException>(() => RenderingServer.Texture2DUpdate(borrowedRID, red)); borrowed.Dispose();
        var placeholder = RenderingServer.Texture2DLayeredPlaceholderCreate(TextureLayered.LayeredType.Array); using (var pixels = RenderingServer.Texture2DLayerGet(placeholder, 0)!) Check(pixels.Width == 4, "Real diagnostic placeholder pixels."); RenderingServer.FreeRID(placeholder);
        var proxy = RenderingServer.TextureProxyCreate(source); Reject<ArgumentException>(() => RenderingServer.TextureProxyUpdate(proxy, ordinary.GetRID())); Reject<InvalidOperationException>(() => RenderingServer.TextureProxyUpdate(proxy, proxy)); RenderingServer.TextureProxyUpdate(proxy, source); RenderingServer.FreeRID(proxy);
        return source;
    }
    private static Image MipImage(Color first, Color second, Color third)
    {
        var data = new byte[84];
        Fill(0, 64, first); Fill(64, 16, second); Fill(80, 4, third);
        return Image.CreateFromData(4, 4, true, Image.Format.Rgba8, data);
        void Fill(int offset, int bytes, Color color)
        { for (var i = offset; i < offset + bytes; i += 4) { data[i] = (byte)(color.R * 255); data[i + 1] = (byte)(color.G * 255); data[i + 2] = (byte)(color.B * 255); data[i + 3] = 255; } }
    }
    private static GPUTexture NativeTexture(TextureLayered array)
    {
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var backend = (GPUCanvasBackend)typeof(RenderingServer).GetField("_backend", flags)!.GetValue(RenderingServer.Service)!;
        return ((Dictionary<TextureLayered, GPUTexture>)typeof(GPUCanvasBackend).GetField("_layeredTextures", flags)!.GetValue(backend)!)[array];
    }
    private static unsafe nint VerifyMipUpload(TextureLayered array)
    {
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var backend = (GPUCanvasBackend)typeof(RenderingServer).GetField("_backend", flags)!.GetValue(RenderingServer.Service)!;
        var deviceHandle = (RenderHandle)typeof(GPUCanvasBackend).GetField("_device", flags)!.GetValue(backend)!; var device = deviceHandle.DangerousGetHandle(); var texture = NativeTexture(array);
        Color[][] expected = [[Colors.Red, Colors.Yellow, Colors.Cyan], [Colors.Blue, Colors.Magenta, Colors.Green]];
        for (var layer = 0; layer < 2; layer++) for (var mip = 0; mip < 3; mip++)
            {
                var width = 4 >> mip; const int pitch = 64;
                var info = new SDL3.SDL.GPUTransferBufferCreateInfo { Usage = SDL3.SDL.GPUTransferBufferUsage.Download, Size = (uint)(pitch * width * 4) };
                using var transfer = new RenderHandle(SDL3.SDL.CreateGPUTransferBuffer(device, in info), h => SDL3.SDL.ReleaseGPUTransferBuffer(device, h), deviceHandle);
                var command = SDL3.SDL.AcquireGPUCommandBuffer(device); Check(command != 0, "Readback command acquired."); nint fence;
                try
                {
                    var pass = SDL3.SDL.BeginGPUCopyPass(command); Check(pass != 0, "Readback copy pass acquired.");
                    SDL3.SDL.DownloadFromGPUTexture(pass, new SDL3.SDL.GPUTextureRegion { Texture = texture.Handle, Layer = (uint)layer, MipLevel = (uint)mip, W = (uint)width, H = (uint)width, D = 1 },
                        new SDL3.SDL.GPUTextureTransferInfo { TransferBuffer = transfer.DangerousGetHandle(), PixelsPerRow = pitch, RowsPerLayer = (uint)width });
                    SDL3.SDL.EndGPUCopyPass(pass); var submitted = command; command = 0; fence = SDL3.SDL.SubmitGPUCommandBufferAndAcquireFence(submitted); Check(fence != 0, "Readback submitted.");
                }
                finally { if (command != 0) SDL3.SDL.CancelGPUCommandBuffer(command); }
                using var completion = new RenderHandle(fence, h => SDL3.SDL.ReleaseGPUFence(device, h), deviceHandle);
                Check(SDL3.SDL.WaitForGPUFences(device, true, new ReadOnlySpan<nint>(&fence, 1), 1), "Readback completion.");
                var mapped = SDL3.SDL.MapGPUTransferBuffer(device, transfer.DangerousGetHandle(), false); Check(mapped != 0, "Readback mapped.");
                try { var data = new ReadOnlySpan<byte>((void*)mapped, 4); var wanted = expected[layer][mip]; Check(data[0] == (byte)(wanted.R * 255) && data[1] == (byte)(wanted.G * 255) && data[2] == (byte)(wanted.B * 255), $"Physical array layer/mip {layer}/{mip}."); }
                finally { SDL3.SDL.UnmapGPUTransferBuffer(device, transfer.DangerousGetHandle()); }
            }
        return texture.Handle;
    }
    private static void Pixel(Image image, int x, Color expected)
    {
        var actual = image.GetPixel(x, 16); Check(Math.Abs(actual.R - expected.R) < .02f && Math.Abs(actual.G - expected.G) < .02f && Math.Abs(actual.B - expected.B) < .02f, $"Array sample {x}: {actual} / {expected}");
    }
}
