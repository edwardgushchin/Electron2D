using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class GraphicsStorage
{
    private static void Wide(string mode)
    {
        if (mode == "wide-canvas") { WideCanvas(); return; }
        using var device = RenderingServer.CreateLocalRenderingDevice(); Console.WriteLine("Wide-line device: " + device.GetDeviceName());
        foreach (var language in new[] { "glsl", "hlsl" }) device.FreeRID(ExecuteWide(device, language, mode));
        if (mode is "wide-unavailable" or "wide-create-refusal") device.FreeRID(Execute(device, "glsl"));
    }
    private static RID ExecuteWide(RenderingDevice device, string language, string mode)
    {
        var shader = Shader(device, "wide", language, mode == "wide-source");
        using var format = new RDTextureFormat { Width = 32, Height = 32, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom | TextureUsageBits.Sampling };
        using var view = new RDTextureView(); var image = device.TextureCreate(format, view); var target = device.FramebufferCreate([image]); var targetFormat = device.FramebufferGetFormat(target);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
        Check(raster.LineWidth == 1, "Default line width is one pixel.");
        foreach (var value in new[] { 0f, -1, float.NaN, float.PositiveInfinity, float.NegativeInfinity }) Reject<ArgumentOutOfRangeException>(() => raster.LineWidth = value);
        var thin = device.RenderPipelineCreate(shader, targetFormat, -1, RenderPrimitive.Lines, raster, blend);
        raster.LineWidth = 4;
        if (mode is "wide-unavailable" or "wide-create-refusal")
        {
            for (var i = 0; i < 3; i++) Reject<NotSupportedException>(() => device.RenderPipelineCreate(shader, targetFormat, -1, RenderPrimitive.Lines, raster, blend));
        }
        else if (mode == "wide-failure")
        {
            for (var i = 0; i < 3; i++) Reject<InvalidOperationException>(() => device.RenderPipelineCreate(shader, targetFormat, -1, RenderPrimitive.Lines, raster, blend));
        }
        else
        {
            var wide = device.RenderPipelineCreate(shader, targetFormat, -1, RenderPrimitive.Lines, raster, blend); raster.LineWidth = 8;
            var strip = device.RenderPipelineCreate(shader, targetFormat, -1, RenderPrimitive.LineStrips, raster, blend); raster.LineWidth = 4.5f;
            var fractional = device.RenderPipelineCreate(shader, targetFormat, -1, RenderPrimitive.Lines, raster, blend); raster.LineWidth = 4; raster.Wireframe = true;
            var wire = device.RenderPipelineCreate(shader, targetFormat, -1, RenderPrimitive.Triangles, raster, blend); raster.Wireframe = false; raster.DiscardPrimitives = true;
            var discard = device.RenderPipelineCreate(shader, targetFormat, -1, RenderPrimitive.Lines, raster, blend); raster.DiscardPrimitives = false; raster.LineWidth = float.MaxValue;
            var maximum = device.RenderPipelineCreate(shader, targetFormat, -1, RenderPrimitive.Lines, raster, blend); raster.LineWidth = float.Epsilon;
            var minimum = device.RenderPipelineCreate(shader, targetFormat, -1, RenderPrimitive.Lines, raster, blend); raster.LineWidth = 1;
            var pixels = new float[32 * 32 * 4];
            void Draw(RID pipeline, uint vertices = 2)
            {
                var list = device.DrawListBegin(target, DrawFlags.ClearColorAll, [Colors.Black]); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListDraw(list, false, 1, vertices); device.DrawListEnd(); device.TextureGetData(image, 0, MemoryMarshal.AsBytes(pixels.AsSpan()));
            }
            int Rows() { var count = 0; for (var y = 0; y < 32; y++) if (pixels[(y * 32 + 16) * 4 + 1] > .5f) count++; return count; }
            Draw(thin); Check(Rows() == 1, "Ordinary lines remain one pixel after optional feature enable.");
            Draw(wide); Check(Rows() == 4, "Copied four-pixel line executes after resource mutation.");
            Draw(strip); Check(Rows() == 8, "Line strips retain their own eight-pixel width.");
            Draw(fractional); Check(Rows() is >= 4 and <= 5, "Fractional width uses native raster precision.");
            Draw(minimum); Check(Rows() == 1, "Positive subminimum width clamps to the native minimum.");
            Draw(maximum); Check(Rows() == 32, "Oversized finite width clamps to the native maximum and fills the target height.");
            Draw(wire, 3); Check(Rows() >= 4, "Polygon outlines use the copied line width.");
            Draw(discard); Check(Rows() == 0, "Wide pipeline preserves primitive discard.");
            for (var i = 0; i < 16; i++) Draw(wide); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Draw(wide); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared wide-line cycles allocate zero owner bytes.");
            Draw(wide); foreach (var rid in new[] { wide, strip, fractional, wire, discard, minimum, maximum }) device.FreeRID(rid);
            if (language == "glsl") { WideViewsAndSampling(device, shader); WideMSAA(device, shader); }
        }
        if (mode is "wide-unavailable" or "wide-create-refusal")
        {
            raster.LineWidth = 1; raster.Wireframe = true;
            var outline = device.RenderPipelineCreate(shader, targetFormat, -1, RenderPrimitive.Triangles, raster, blend);
            var dl = device.DrawListBegin(target, DrawFlags.ClearColorAll, [Colors.Black]); device.DrawListBindRenderPipeline(dl, outline); device.DrawListDraw(dl, false, 1, 3); device.DrawListEnd(); device.FreeRID(outline);
        }
        if (mode is "wide-unavailable" or "wide-create-refusal" or "wide-failure")
        {
            var pixels = new float[32 * 32 * 4]; var list = device.DrawListBegin(target, DrawFlags.ClearColorAll, [Colors.Black]); device.DrawListBindRenderPipeline(list, thin); device.DrawListDraw(list, false, 1, 2); device.DrawListEnd(); device.TextureGetData(image, 0, MemoryMarshal.AsBytes(pixels.AsSpan())); Check(pixels.Any(v => v == .8f), "Optional wide-line refusal retains executing ordinary graphics.");
        }
        device.FreeRID(thin); device.FreeRID(shader); device.FreeRID(target);
        using (var disposed = new RDPipelineRasterizationState()) { disposed.Dispose(); Reject<ObjectDisposedException>(() => _ = disposed.LineWidth); Reject<ObjectDisposedException>(() => disposed.LineWidth = 4); }
        Console.WriteLine("PASS " + language + " " + mode + ": copied width, raster pixels, precision, lifecycle and optional-feature independence."); return image;
    }
    private static void WideViewsAndSampling(RenderingDevice device, RID shader)
    {
        using var format = new RDTextureFormat { TextureType = TextureType.TextureArray, Width = 32, Height = 32, ArrayLayers = 2, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom };
        using var view = new RDTextureView(); var image = device.TextureCreate(format, view); using var pass = new RDFramebufferPass { ColorAttachments = [0] }; var target = device.FramebufferCreateMultipass([image], [pass, pass], viewCount: 2);
        using var raster = new RDPipelineRasterizationState { LineWidth = 4 }; using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
        var p0 = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Lines, raster, blend); var p1 = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Lines, raster, blend, forRenderPass: 1);
        var list = device.DrawListBegin(target, DrawFlags.ClearColorAll, [Colors.Black]); device.DrawListBindRenderPipeline(list, p0); device.DrawListDraw(list, false, 1, 2); device.DrawListSwitchToNextPass(); device.DrawListBindRenderPipeline(list, p1); device.DrawListDraw(list, false, 1, 2); device.DrawListEnd(); float[] pixels = new float[32 * 32 * 4];
        for (uint layer = 0; layer < 2; layer++) { device.TextureGetData(image, layer, MemoryMarshal.AsBytes(pixels.AsSpan())); var rows = 0; for (var y = 0; y < 32; y++) if (pixels[(y * 32 + 16) * 4 + 1] > .5f) rows++; Check(rows == 4, "Every view/pass retains native line width."); }
        foreach (var rid in new[] { p0, p1, target, image }) device.FreeRID(rid);
        using var sourceFormat = new RDTextureFormat { Width = 1, Height = 1, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.Sampling }; using var sourceView = new RDTextureView { SwizzleR = TextureSwizzle.G }; float[] color = [.2f, .8f, .4f, 1]; var source = device.TextureCreate(sourceFormat, sourceView, [MemoryMarshal.AsBytes(color.AsSpan()).ToArray()]);
        using var ordinary = new RDTextureFormat { Width = 32, Height = 32, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom }; image = device.TextureCreate(ordinary, view); target = device.FramebufferCreate([image]);
        byte[] Read(string name) => File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", name)); using var code = new RDShaderSPIRV { BytecodeVertex = Read("wide.vert.glsl.spv"), BytecodeFragment = Read("wide-sample.frag.glsl.spv") }; var sampled = device.ShaderCreateFromSPIRV(code);
        var pipeline = device.RenderPipelineCreate(sampled, device.FramebufferGetFormat(target), -1, RenderPrimitive.Lines, raster, blend); using var samplerState = new RDSamplerState(); var sampler = device.SamplerCreate(samplerState); using var uniform = new RDUniform { UniformType = UniformType.SamplerWithTexture, Binding = 0 }; uniform.AddID(sampler); uniform.AddID(source); var set = device.UniformSetCreate([uniform], sampled, 0);
        void Draw()
        {
            var dl = device.DrawListBegin(target, DrawFlags.ClearColorAll, [Colors.Black]); device.DrawListBindRenderPipeline(dl, pipeline); device.DrawListBindUniformSet(dl, set, 0); device.DrawListDraw(dl, false, 1, 2); device.DrawListEnd(); device.TextureGetData(image, 0, MemoryMarshal.AsBytes(pixels.AsSpan()));
        }
        Draw(); var count = 0; for (var y = 0; y < 32; y++) if (pixels[(y * 32 + 16) * 4] > .5f) count++; Check(count == 4, "Swizzle variant retains width and component mapping.");
        for (var i = 0; i < 16; i++) Draw(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Draw(); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared sampled wide-line cycles allocate zero owner bytes.");
        foreach (var rid in new[] { pipeline, sampled, set, sampler, source, target, image }) device.FreeRID(rid); Console.WriteLine("PASS multiview/passes and sampled component variants retain copied line width.");
    }
    private static void WideMSAA(RenderingDevice device, RID shader)
    {
        using var format = new RDTextureFormat { Width = 32, Height = 32, Format = DataFormat.R32G32B32A32SFLOAT, Samples = TextureSamples.Samples4, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom };
        using var view = new RDTextureView(); var source = device.TextureCreate(format, view); var target = device.FramebufferCreate([source]);
        format.Samples = TextureSamples.Samples1; format.IsResolveBuffer = true; format.UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyTo | TextureUsageBits.CanCopyFrom; var image = device.TextureCreate(format, view);
        using var raster = new RDPipelineRasterizationState { LineWidth = 4 }; using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; using var samples = new RDPipelineMultisampleState { SampleCount = TextureSamples.Samples4 };
        var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Lines, raster, blend, multisampleState: samples);
        var list = device.DrawListBegin(target, DrawFlags.ClearColorAll, [Colors.Black]); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListDraw(list, false, 1, 2); device.DrawListEnd(); device.TextureResolveMultisample(source, image); device.FreeRID(pipeline); device.FreeRID(target); device.FreeRID(source);
        float[] pixels = new float[32 * 32 * 4]; device.TextureGetData(image, 0, MemoryMarshal.AsBytes(pixels.AsSpan())); var rows = 0; for (var y = 0; y < 32; y++) if (pixels[(y * 32 + 16) * 4 + 1] > .5f) rows++; Check(rows is >= 3 and <= 5, "MSAA line width survives resolve and pending resource retirement."); device.FreeRID(image);
        Console.WriteLine("PASS exact four-sample wide raster, resolve and pending-work retirement.");
    }
    private static void WideCanvas()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); using var root = new Window { Size = new(32, 32) }; using var texture = new TextureRD(); var frames = 0;
        root.Ready += _ => { texture.TextureRDRID = ExecuteWide(RenderingServer.GetRenderingDevice()!, "glsl", "wide"); root.AddChild(new Patch(texture)); RenderingServer.FramePostDraw += Captured; };
        void Captured()
        {
            if (++frames < 3) return; RenderingServer.FramePostDraw -= Captured; using var image = root.GetTexture().GetImage()!; var pixel = image.GetPixel(8, 8); Check(pixel.G > .5f, "Wide-line target reaches retained canvas."); root.Tree!.Quit();
        }
        Engine.Run(root); Console.WriteLine("PASS wide-line output reaches retained canvas.");
    }
}
