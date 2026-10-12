using Electron2D;
using static TextureArrayTests;

internal static class RendererTextureDeviceTests
{
    private sealed class Patch(Texture texture, Rect2 rect) : Entity { protected override void OnDraw() => DrawTextureRect(texture, rect, false); }
    private sealed class Fill : Entity
    {
        internal Color Color = Colors.Green;
        protected override void OnDraw() => DrawRect(new(0, 0, 8, 8), Color);
        internal void Set(Color color) { Color = color; QueueRedraw(); }
    }
    private static void Compute(RenderingDevice device, RID source)
    {
        foreach (var name in new[] { "LocalTexture.comp", "LocalTextureHLSL.comp" })
        {
            using var samplerState = new RDSamplerState();
            using var nativeOutputPixels = Image.CreateEmpty(4, 4, false, Image.Format.Rgbaf); using var nativeOutput = ImageTexture.CreateFromImage(nativeOutputPixels); var output = RenderingServer.TextureGetRDTexture(nativeOutput.GetRID()); Check((device.TextureGetFormat(output).UsageBits & RenderingDevice.TextureUsageBits.Storage) != 0, "This native backend supports renderer image storage writes."); var sampler = device.SamplerCreate(samplerState); var count = device.StorageBufferCreate(4); using var module = new RDShaderSPIRV(); using var input = typeof(RendererTextureDeviceTests).Assembly.GetManifestResourceStream("Electron2D.Compute." + name + ".spv")!; using var bytes = new MemoryStream(); input.CopyTo(bytes); module.BytecodeCompute = bytes.ToArray(); var shader = device.ShaderCreateFromSPIRV(module); var pipeline = device.ComputePipelineCreate(shader);
            using var sourceBinding = new RDUniform { Binding = 7, UniformType = RenderingDevice.UniformType.SamplerWithTexture }; sourceBinding.AddID(sampler); sourceBinding.AddID(source); using var targetBinding = new RDUniform { Binding = 2, UniformType = RenderingDevice.UniformType.Image }; targetBinding.AddID(output); using var countBinding = new RDUniform { Binding = 4, UniformType = RenderingDevice.UniformType.StorageBuffer }; countBinding.AddID(count);
            var sources = device.UniformSetCreate([sourceBinding], shader, 3); var targets = device.UniformSetCreate([targetBinding], shader, 5); var counts = device.UniformSetCreate([countBinding], shader, 9); var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, sources, 3); device.ComputeListBindUniformSet(list, targets, 5); device.ComputeListBindUniformSet(list, counts, 9); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); var result = device.TextureGetData(output, 0); var pixel = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, float>(result.AsSpan()); Check(Math.Abs(pixel[0] - 1.25f) < .001 && pixel[1] == 0 && pixel[2] == 0 && pixel[3] == 1, name + " samples and writes actual renderer uploads through compute.");
            nativeOutput.Dispose(); Check(!device.UniformSetIsValid(targets) && !device.TextureIsValid(output), "Renderer source disposal invalidates dependent compute descriptors.");
            foreach (var rid in new[] { sources, targets, counts, pipeline, shader, count, sampler, output }) device.FreeRID(rid);
        }
    }
    private static void ColdEdges(RenderingDevice device, RID sampled)
    {
        using var local = RenderingServer.CreateLocalRenderingDevice(); Check(!local.TextureIsValid(sampled), "Renderer image identities never cross devices."); Reject<ArgumentException>(() => local.TextureGetFormat(sampled));
        Task.Run(() => Reject<InvalidOperationException>(() => device.TextureIsValid(sampled))).GetAwaiter().GetResult();
        using var listImage = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); using var listTexture = ImageTexture.CreateFromImage(listImage); var emptyRID = listTexture.GetRID();
        var list = device.ComputeListBegin(); Reject<InvalidOperationException>(() => RenderingServer.TextureGetRDTexture(emptyRID)); device.ComputeListEnd();
        using var empty = new ImageTexture(); Check(!RenderingServer.TextureGetRDTexture(empty.GetRID(), true).IsValid(), "Empty storage remains unavailable even with a view request.");
        foreach (var format in new[] { Image.Format.Rgba8, Image.Format.Rgbaf })
        {
            using var pixels = Image.CreateEmpty(4, 4, false, format); pixels.Fill(new(0.25f, 0.5f, 1, 1)); pixels.GenerateMipmaps(); using var image = ImageTexture.CreateFromImage(pixels); using var array = new TextureArray(); array.CreateFromImages([pixels, pixels]);
            var ordinary = RenderingServer.TextureGetRDTexture(image.GetRID()); var layered = RenderingServer.TextureGetRDTexture(array.GetRID());
            Check(device.TextureGetFormat(ordinary).Mipmaps == 3 && device.TextureGetFormat(layered).ArrayLayers == 2, "Every native mip/layer is represented.");
            Check(device.TextureGetData(ordinary, 0).AsSpan().SequenceEqual(pixels.GetData()) && device.TextureGetData(layered, 1).AsSpan().SequenceEqual(pixels.GetData()), "RGBA8/HDR mip chains preserve actual native bytes.");
            var queued = new byte[pixels.DataSize]; var completed = false; Action finished = () => completed = true; device.TextureGetDataAsync(ordinary, 0, queued.AsMemory(), finished); image.Dispose(); Check(!device.TextureIsValid(ordinary), "Disposal expires identity before queued completion."); device.FreeRID(ordinary); Check(completed && queued.AsSpan().SequenceEqual(pixels.GetData()), "Source disposal preserves already recorded readback."); array.Dispose(); Check(!device.TextureIsValid(layered), "Layered source disposal expires identity."); device.FreeRID(layered);
        }
    }
    internal static void Run()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var image = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8); image.Fill(Colors.Red); using var source = ImageTexture.CreateFromImage(image);
        using var green = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8); green.Fill(Colors.Green); using var blue = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8); blue.Fill(Colors.Blue);
        using var array = new TextureArray(); array.CreateFromImages([green, blue]); using var alias = new AtlasTexture { Atlas = source }; using var wrapper = new TextureRD();
        using var window = new Window { Size = new(128, 64), MinSize = new(128, 64), MaxSize = new(128, 64), Visible = true }; var sub = new SubViewport { Size = new(8, 8), RenderTargetUpdateMode = ViewportUpdateMode.Always }; var fill = new Fill(); sub.AddChild(fill); window.AddChild(sub); window.AddChild(new Patch(source, new(0, 0, 16, 16)) { Name = "Original" }); window.AddChild(new Patch(wrapper, new(20, 0, 16, 16)) { Name = "Imported" });
        RenderingDevice device = null!; RID imported = default, layers = default, viewport = default, proxy = default, copy = default, oldViewport = default, viewportFB = default; var frame = 0; long before = 0, allocated = 0; var deltas = new long[152]; var read = new byte[64]; var callbacks = 0;
        void Finished() { callbacks++; }
        Action finished = Finished;
        var oldFrame = new byte[8 * 8 * 4]; var oldFrameCompleted = false;
        Action oldFinished = () => { Check(oldFrame.AsSpan(0, 4).SequenceEqual(new byte[] { 0, 0, 255, 255 }), "Recorded viewport read survives target retirement and keeps request-time bytes."); oldFrameCompleted = true; };
        window.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); device = RenderingServer.GetRenderingDevice()!;
            Reject<NotSupportedException>(() => RenderingServer.TextureGetRDTexture(source.GetRID(), true)); imported = RenderingServer.TextureGetRDTexture(source.GetRID()); Check(imported.IsValid() && imported == RenderingServer.TextureGetRDTexture(alias.GetRID()), "Ordinary/atlas exports share actual native allocation.");
            var info = device.TextureGetFormat(imported); Check(info.Width == 4 && info.Height == 4 && info.Format == RenderingDevice.DataFormat.R8G8B8A8UNORM, "Native allocation metadata."); Check(device.TextureGetData(imported, 0).AsSpan(0, 4).SequenceEqual(new byte[] { 255, 0, 0, 255 }), "Initial authored upload precedes device read.");
            Compute(device, imported); ColdEdges(device, imported); wrapper.TextureRDRID = imported; layers = RenderingServer.TextureGetRDTexture(array.GetRID()); Check(device.TextureGetFormat(layers).ArrayLayers == 2 && device.TextureGetData(layers, 1).AsSpan(0, 4).SequenceEqual(new byte[] { 0, 0, 255, 255 }), "Layered exports retain all layers.");
            proxy = RenderingServer.TextureProxyCreate(source.GetRID()); Check(RenderingServer.TextureGetRDTexture(proxy) == imported, "Proxy source identity."); Check(!RenderingServer.TextureGetRDTexture(sub.GetTexture().GetRID()).IsValid(), "An unrendered viewport has no completed native image.");
            copy = device.TextureCreate(new RDTextureFormat { Format = RenderingDevice.DataFormat.R8G8B8A8UNORM, Width = 4, Height = 4, UsageBits = RenderingDevice.TextureUsageBits.Sampling | RenderingDevice.TextureUsageBits.CanCopyTo | RenderingDevice.TextureUsageBits.CanCopyFrom }, new RDTextureView());
            window.Tree!.ProcessFrameStarted += _ => { if (frame >= 24) before = GC.GetAllocatedBytesForCurrentThread(); }; RenderingServer.FramePreDraw += Pre; RenderingServer.FramePostDraw += Post;
        };
        void Pre()
        {
            if (frame == 10) { source.Update(green); array.UpdateLayer(image, 1); }
            if (frame == 16) GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
            if (frame >= 6 && frame < 88)
            {
                Check(RenderingServer.TextureGetRDTexture(source.GetRID()) == imported, "Stable authored texture allocation identity.");
                if (frame != 7 && frame != 8) Check(RenderingServer.TextureGetRDTexture(sub.GetTexture().GetRID()) == viewport, "Stable viewport allocation identity outside resize.");
                device.TextureCopy(imported, copy, new(0, 0, 0), new(0, 0, 0), new(4, 4, 1), 0, 0, 0, 0); device.TextureGetDataAsync(copy, 0, read.AsMemory(), finished);
            }
        }
        void Post()
        {
            if (frame >= 24) { deltas[frame] = GC.GetAllocatedBytesForCurrentThread() - before; allocated += deltas[frame]; }
            if (frame == 1)
            {
                viewport = RenderingServer.TextureGetRDTexture(sub.GetTexture().GetRID()); Check(device.TextureIsValid(viewport), "Completed viewport storage exports."); viewportFB = device.FramebufferCreate([viewport]); var pixels = device.TextureGetData(viewport, 0); Check(pixels[0] == 0 && pixels[1] == 255 && pixels[2] == 0, "Actual rendered viewport pixels."); fill.Set(Colors.Blue);
            }
            if (frame == 3) device.TextureUpdate(imported, 0, blue.GetData());
            if (frame == 4) { var fb = device.FramebufferCreate([viewport]); var list = device.DrawListBegin(fb, RenderingDevice.DrawFlags.ClearColor0, [Colors.Blue]); device.DrawListEnd(); device.FreeRID(fb); using var native = window.GetTexture().GetImage()!; using var authored = source.GetImage()!; Check(native.GetPixel(4, 4).B > .99f && native.GetPixel(24, 4).B > .99f && authored.GetPixel(0, 0).R > .99f, "Device writes affect renderer storage while authored CPU image remains unchanged."); var pixels = device.TextureGetData(viewport, 0); Check(pixels[0] == 0 && pixels[1] == 0 && pixels[2] == 255, "The same RID follows completed ping-pong images."); }
            if (frame == 6) { oldViewport = viewport; device.TextureGetDataAsync(viewport, 0, oldFrame.AsMemory(), oldFinished); sub.Size = new(16, 8); }
            if (frame == 8) { Check(oldFrameCompleted && !device.TextureIsValid(oldViewport), "Viewport resize expires old identity after preserving queued work."); Check(!device.FramebufferIsValid(viewportFB), "Viewport retirement invalidates dependent framebuffer."); device.FreeRID(viewportFB); device.FreeRID(oldViewport); viewport = RenderingServer.TextureGetRDTexture(sub.GetTexture().GetRID()); Check(device.TextureGetFormat(viewport).Width == 16 && viewport != oldViewport, "Resized viewport publishes its actual allocation."); }
            if (frame == 12)
            {
                Check(device.TextureGetData(imported, 0).AsSpan(0, 4).SequenceEqual(new byte[] { 0, 255, 0, 255 }), "Compatible CPU update keeps RID and native bytes."); Check(device.TextureGetData(layers, 1).AsSpan(0, 4).SequenceEqual(new byte[] { 255, 0, 0, 255 }), "Layer update keeps borrowed allocation.");
                using var pixels = window.GetTexture().GetImage()!; Check(pixels.GetPixel(4, 4).G > .99f && pixels.GetPixel(24, 4).G > .99f, "Original and borrowed canvas consumers agree."); var output = System.IO.Path.Combine("bin", "renderer-texture-device-validation", "2026-10-12"); Directory.CreateDirectory(output); pixels.SavePNG(System.IO.Path.Combine(output, "native.png"));
            }
            if (++frame == 152)
            {
                Check(callbacks == 82 && read[1] == 255, "Queued copies/readbacks complete in the shared renderer timeline."); device.FreeRID(imported); Check(source.GetWidth() == 4 && !device.TextureIsValid(imported), "Freeing the wrapper preserves source resource/native ownership."); var replacement = RenderingServer.TextureGetRDTexture(source.GetRID()); Check(replacement != imported && device.TextureIsValid(replacement), "A freed wrapper may be exported again."); source.SetImage(blue); Check(!device.TextureIsValid(replacement), "Allocation replacement expires old RID before reuse."); device.FreeRID(replacement);
                var resized = RenderingServer.TextureGetRDTexture(source.GetRID()); source.Dispose(); Check(!device.TextureIsValid(resized), "Source disposal expires the borrowing identity."); device.FreeRID(resized); device.FreeRID(layers); device.FreeRID(viewport); device.FreeRID(copy); RenderingServer.FreeRID(proxy); RenderingServer.FramePreDraw -= Pre; RenderingServer.FramePostDraw -= Post; window.Tree!.Quit();
            }
        }
        Engine.Run(window); if (allocated != 0) for (var i = 24; i < deltas.Length; i++) if (deltas[i] != 0) Console.WriteLine("Renderer texture allocation frame=" + i + " bytes=" + deltas[i]); Check(allocated == 0, "Renderer texture full frames allocated " + allocated); Console.WriteLine("Renderer-owned ordinary/array/viewport exports: real bytes/pixels, stable ping-pong identities, upload order, expiry and 64 active +64 idle frames at zero managed bytes passed.");
    }
}
