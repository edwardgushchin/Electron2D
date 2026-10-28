using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class GraphicsStorage
{
    private static void Depth(string mode)
    {
        if (mode == "depth-canvas") { DepthCanvas(); return; }
        using var device = RenderingServer.CreateLocalRenderingDevice(); Console.WriteLine("Depth/stencil device: " + device.GetDeviceName());
        foreach (var language in new[] { "glsl", "hlsl" }) device.FreeRID(DepthPixels(device, language, mode));
        if (mode is "depth" or "depth-source") { DepthViews(device); DepthSampling(device); }
        if (mode == "depth-refuse") { device.FreeRID(SamplesCoverage(device, "glsl", false)); device.FreeRID(ExecuteWide(device, "glsl", "wide")); }
    }
    private static RDPipelineDepthStencilState FaceState(int front, int back, CompareOperator compare = CompareOperator.Always) => new()
    {
        EnableStencil = true,
        FrontOpCompare = compare,
        BackOpCompare = compare,
        FrontOpReference = front,
        BackOpReference = back,
        FrontOpCompareMask = -1,
        BackOpCompareMask = -1,
        FrontOpWriteMask = -1,
        BackOpWriteMask = -1,
        FrontOpPass = StencilOperation.Keep,
        BackOpPass = StencilOperation.Keep,
        FrontOpFail = StencilOperation.Keep,
        BackOpFail = StencilOperation.Keep,
        FrontOpDepthFail = StencilOperation.Keep,
        BackOpDepthFail = StencilOperation.Keep
    };
    private static RID DepthPixels(RenderingDevice device, string language, string mode)
    {
        var shader = Shader(device, "depth", language, mode == "depth-source");
        var depthFormat = device.TextureIsFormatSupportedForUsage(DataFormat.D32SFLOATS8UINT, TextureUsageBits.DepthStencilAttachment) ? DataFormat.D32SFLOATS8UINT : DataFormat.D24UNORMS8UINT;
        using var depthDescription = new RDTextureFormat { Width = 8, Height = 4, Format = depthFormat, UsageBits = TextureUsageBits.DepthStencilAttachment };
        using var colorDescription = new RDTextureFormat { Width = 8, Height = 4, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom | TextureUsageBits.Sampling };
        using var view = new RDTextureView(); var image = device.TextureCreate(colorDescription, view); var depth = device.TextureCreate(depthDescription, view);
        var target = device.FramebufferCreate([depth, image]); var targetAlive = true; var colorTarget = device.FramebufferCreate([image]); var format = device.FramebufferGetFormat(target);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
        RID Pipeline(RDPipelineDepthStencilState state) => device.RenderPipelineCreate(shader, format, -1, RenderPrimitive.Triangles, raster, blend, depthStencilState: state);
        using var plainState = new RDPipelineDepthStencilState(); var plain = Pipeline(plainState);
        using var bounds = new RDPipelineDepthStencilState();
        Check(!bounds.EnableDepthRange && bounds.DepthRangeMin == 0 && bounds.DepthRangeMax == 0, "Depth bounds preserve the declared defaults.");
        foreach (var value in new[] { -.01f, 1.01f, float.NaN, float.PositiveInfinity, float.NegativeInfinity }) { Reject<ArgumentOutOfRangeException>(() => bounds.DepthRangeMin = value); Reject<ArgumentOutOfRangeException>(() => bounds.DepthRangeMax = value); }
        bounds.DepthRangeMin = .75f; bounds.DepthRangeMax = .25f; device.FreeRID(Pipeline(bounds)); bounds.EnableDepthRange = true;
        Reject<ArgumentException>(() => Pipeline(bounds)); bounds.DepthRangeMin = .25f; bounds.DepthRangeMax = .75f;
        Reject<ArgumentException>(() => device.RenderPipelineCreate(shader, device.FramebufferGetFormat(colorTarget), -1, RenderPrimitive.Triangles, raster, blend, depthStencilState: bounds));
        float[] push = [1, .5f, .25f, 1, .9f, 0, 0, 0]; float[] pixels = new float[128]; Color[] clear = [new(0, 0, 0, 0)];
        void Draw(RID pipeline, DrawFlags flags = 0, float stored = .5f, int stencil = 0)
        {
            var list = device.DrawListBegin(target, flags, (flags & DrawFlags.ClearColorAll) != 0 ? clear : default, clearDepthValue: stored, clearStencilValue: stencil);
            device.DrawListBindRenderPipeline(list, pipeline); device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(push.AsSpan()), 32); device.DrawListDraw(list, false, 1, 12); device.DrawListEnd();
        }
        void Read() => device.TextureGetData(image, 0, MemoryMarshal.AsBytes(pixels.AsSpan()));
        void Halves(bool front, bool back, bool frontLeft, string message)
        {
            for (var y = 0; y < 4; y++) for (var x = 0; x < 8; x++) { var expected = (x < 4) == frontLeft ? front : back; Check(pixels[(y * 8 + x) * 4] == (expected ? 1 : 0), message + " at " + x + "," + y); }
        }
        if (mode is "depth-unavailable" or "depth-failure")
        {
            using var separate = FaceState(7, 9);
            for (var i = 0; i < 3; i++) { if (mode == "depth-unavailable") Reject<NotSupportedException>(() => Pipeline(separate)); else Reject<InvalidOperationException>(() => Pipeline(separate)); }
            Draw(plain, DrawFlags.ClearAll); Read(); Check(pixels[0] == 1 && pixels[124] == 1, "Ordinary graphics survives three native depth/stencil creation refusals.");
        }
        else
        {
            if (mode == "depth-refuse") { for (var i = 0; i < 3; i++) Reject<NotSupportedException>(() => Pipeline(bounds)); }
            else
            {
                var range = Pipeline(bounds); bounds.DepthRangeMin = 0; bounds.DepthRangeMax = 0;
                foreach (var stored in new[] { 0f, .125f, .25f, .5f, .75f, .875f, 1f })
                {
                    if (depthFormat != DataFormat.D32SFLOATS8UINT && stored is .25f or .75f) continue;
                    Draw(range, DrawFlags.ClearAll, stored); Read(); var expected = stored >= .25f && stored <= .75f; Halves(expected, expected, true, "Bounds test stored depth and retain the copied inclusive range");
                }
                bounds.DepthRangeMin = 0; bounds.DepthRangeMax = 1; var full = Pipeline(bounds);
                foreach (var stored in new[] { 0f, 1f }) { Draw(full, DrawFlags.ClearAll, stored); Read(); Halves(true, true, true, "Bounds include both normalized endpoints"); }
                bounds.DepthRangeMin = bounds.DepthRangeMax = 0; var zero = Pipeline(bounds);
                Draw(zero, DrawFlags.ClearAll, 0); Read(); Halves(true, true, true, "Default zero-sized range includes stored zero"); Draw(zero, DrawFlags.ClearAll, .5f); Read(); Halves(false, false, true, "Default range rejects nonzero stored depth");
                bounds.EnableDepthRange = false; bounds.DepthRangeMin = .75f; bounds.DepthRangeMax = .25f; var disabled = Pipeline(bounds);
                Draw(disabled, DrawFlags.ClearAll, .9f); Read(); Halves(true, true, true, "Disabled ranges ignore their stored ordering");
                bounds.DepthRangeMin = .2f; bounds.DepthRangeMax = .8f; bounds.EnableDepthRange = true; bounds.EnableDepthTest = true; bounds.EnableDepthWrite = true; bounds.DepthCompareOperator = CompareOperator.Less; var writes = Pipeline(bounds);
                push[4] = .1f; Draw(writes, DrawFlags.ClearAll, .5f); push[0] = 0; push[1] = 1; push[4] = .05f; Draw(writes); Read(); Check(pixels[0] == 1 && pixels[1] == .5f, "A prior depth write changes the following stored-depth bounds result"); push[0] = 1; push[1] = .5f; push[4] = .9f;
                void Cycle() { Draw(range, DrawFlags.ClearAll); Read(); }
                for (var i = 0; i < 16; i++) Cycle(); var allocated = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Cycle(); Check(allocated == GC.GetAllocatedBytesForCurrentThread(), "Prepared depth bounds drawing/readback adds zero owner bytes.");
                foreach (var rid in new[] { range, full, zero, disabled, writes }) device.FreeRID(rid);
            }
            using var probe = FaceState(17, 34); probe.FrontOpPass = probe.BackOpPass = StencilOperation.Replace; var probeWriter = Pipeline(probe);
            using var readFront = FaceState(17, 17, CompareOperator.Equal); var probeReader = Pipeline(readFront);
            Draw(probeWriter, DrawFlags.ClearAll); Draw(probeReader, DrawFlags.ClearColorAll); Read(); var frontLeft = pixels[0] == 1; Check(frontLeft != (pixels[16] == 1), "One draw covers two independent stencil faces"); Halves(true, false, frontLeft, "Shared-reference reader identifies the written front face");
            using var compare = FaceState(0xA5, 0x35, CompareOperator.Equal); compare.FrontOpCompareMask = 0x0F; compare.BackOpCompareMask = 0xF0; compare.FrontOpWriteMask = compare.BackOpWriteMask = 0;
            var comparePipeline = Pipeline(compare); compare.FrontOpCompareMask = compare.BackOpCompareMask = 0; compare.FrontOpReference = compare.BackOpReference = 0;
            foreach (var (stencil, front, back) in new[] { (0x65, true, false), (0x35, true, true), (0x39, false, true), (0x60, false, false) }) { Draw(comparePipeline, DrawFlags.ClearAll, stencil: stencil); Read(); Halves(front, back, frontLeft, "Copied independent compare masks/references"); }
            using var frontResult = FaceState(0x65, 0x65, CompareOperator.Equal); using var backResult = FaceState(0x39, 0x39, CompareOperator.Equal); var frontReader = Pipeline(frontResult); var backReader = Pipeline(backResult);
            foreach (var outcome in new[] { 0, 1, 2 })
            {
                using var write = FaceState(0xA5, 0x3C, outcome == 1 ? CompareOperator.Never : CompareOperator.Always); write.FrontOpWriteMask = 0x0F; write.BackOpWriteMask = 0xF0;
                write.EnableDepthTest = outcome == 2; write.DepthCompareOperator = CompareOperator.Never;
                if (outcome == 0) write.FrontOpPass = write.BackOpPass = StencilOperation.Replace; else if (outcome == 1) write.FrontOpFail = write.BackOpFail = StencilOperation.Replace; else write.FrontOpDepthFail = write.BackOpDepthFail = StencilOperation.Replace;
                var writer = Pipeline(write); write.FrontOpReference = write.BackOpReference = 0; write.FrontOpWriteMask = write.BackOpWriteMask = 0;
                Draw(writer, DrawFlags.ClearAll, stencil: 0x69); Draw(frontReader, DrawFlags.ClearColorAll); Read(); Halves(true, false, frontLeft, "Independent write masks/references preserve front bits");
                Draw(writer, DrawFlags.ClearAll, stencil: 0x69); Draw(backReader, DrawFlags.ClearColorAll); Read(); Halves(false, true, frontLeft, "Independent write masks/references preserve back bits"); device.FreeRID(writer);
            }
            void Faces() { Draw(comparePipeline, DrawFlags.ClearAll, stencil: 0x39); Draw(plain, DrawFlags.ClearColorAll); Draw(comparePipeline, DrawFlags.ClearColorAll, stencil: 0x39); Read(); }
            for (var i = 0; i < 16; i++) Faces(); var faceBytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Faces(); Check(faceBytes == GC.GetAllocatedBytesForCurrentThread(), "Prepared switching between independent and ordinary stencil state adds zero owner bytes."); Halves(false, true, frontLeft, "Ordinary dynamic reference cannot overwrite copied independent faces");
            foreach (var rid in new[] { probeWriter, probeReader, comparePipeline, frontReader, backReader }) device.FreeRID(rid);
            using var pendingState = FaceState(1, 2); var pending = Pipeline(pendingState); Draw(pending, DrawFlags.ClearAll); device.FreeRID(pending); device.FreeRID(target); targetAlive = false; Read(); Check(pixels[0] == 1, "Recorded independent state retains pending pipeline/framebuffer ownership.");
        }
        using (var disposed = new RDPipelineDepthStencilState()) { disposed.Dispose(); Reject<ObjectDisposedException>(() => disposed.EnableDepthRange = true); Reject<ObjectDisposedException>(() => _ = disposed.DepthRangeMin); }
        device.FreeRID(plain); device.FreeRID(colorTarget); if (targetAlive) device.FreeRID(target); device.FreeRID(depth); device.FreeRID(shader);
        Console.WriteLine("PASS " + language + ": stored-depth bounds, independent two-sided stencil, copied state, native refusal and zero prepared owner bytes."); return image;
    }
    private static void DepthViews(RenderingDevice device)
    {
        var shader = Shader(device, "depth", "glsl", false); var images = SampleTarget(device, 2); device.FreeRID(images.Target);
        using var format = new RDTextureFormat { Width = 4, Height = 4, TextureType = TextureType.TextureArray, ArrayLayers = 2, Samples = TextureSamples.Samples4, Format = DataFormat.D32SFLOATS8UINT, UsageBits = TextureUsageBits.DepthStencilAttachment }; using var view = new RDTextureView(); var depth = device.TextureCreate(format, view);
        using var pass = new RDFramebufferPass { ColorAttachments = [0], ResolveAttachments = [1], DepthAttachment = 2 }; var target = device.FramebufferCreateMultipass([images.Source, images.Resolve, depth], [pass, pass], viewCount: 2);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; using var samples = new RDPipelineMultisampleState { SampleCount = TextureSamples.Samples4, SampleMasks = [1, 0, 0, 0], EnableAlphaToOne = true, EnableSampleShading = true, MinSampleShading = 1 };
        using var state = FaceState(0xA5, 0x35, CompareOperator.Equal); state.FrontOpCompareMask = 0x0F; state.BackOpCompareMask = 0xF0; state.EnableDepthRange = true; state.DepthRangeMin = .25f; state.DepthRangeMax = .75f;
        RID Pipeline(uint index) => device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend, depthStencilState: state, multisampleState: samples, forRenderPass: index);
        var p0 = Pipeline(0); samples.SampleMasks = [3, 0, 0, 0]; var p1 = Pipeline(1); state.DepthRangeMin = .8f; state.DepthRangeMax = 1; var blocked = Pipeline(1); state.FrontOpReference = state.BackOpReference = 0; samples.SampleMasks = [0, 0, 0, 0];
        float[] push = [1, .5f, .25f, .25f, 0, 0, 0, 0]; float[] pixels = new float[64]; Color[] clear = [new(0, 0, 0, 0), new(0, 0, 0, 0)];
        void Draw(RID second)
        {
            var list = device.DrawListBegin(target, DrawFlags.ClearAll, clear, clearDepthValue: .5f, clearStencilValue: 0x39); device.DrawListBindRenderPipeline(list, p0); device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(push.AsSpan()), 32); device.DrawListDraw(list, false, 1, 12);
            device.DrawListSwitchToNextPass(); device.DrawListBindRenderPipeline(list, second); device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(push.AsSpan()), 32); device.DrawListDraw(list, false, 1, 12); device.DrawListEnd();
        }
        foreach (var (second, expected) in new[] { (p1, .5f), (blocked, .25f) })
        {
            Draw(second);
            for (uint layer = 0; layer < 2; layer++)
            {
                device.TextureGetData(images.Resolve, layer, MemoryMarshal.AsBytes(pixels.AsSpan())); var covered = 0;
                for (var i = 0; i < 64; i += 4) { Check(pixels[i] == 0 || pixels[i] == expected, "Every native view/pass retains copied bounds, faces and masks."); if (pixels[i] == expected) { covered++; Check(pixels[i + 3] == expected, "Alpha-to-one remains independent of copied depth/stencil."); } }
                Check(covered == 8, "Exactly one stencil face survives in every multisample view.");
            }
        }
        for (var i = 0; i < 16; i++) { Draw(p1); device.TextureGetData(images.Resolve, 0, MemoryMarshal.AsBytes(pixels.AsSpan())); }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 32; i++) { Draw(p1); device.TextureGetData(images.Resolve, 0, MemoryMarshal.AsBytes(pixels.AsSpan())); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared depth/stencil multiview/passes/resolve allocate zero owner bytes.");
        foreach (var rid in new[] { p0, p1, blocked, target, depth, images.Source, images.Resolve, shader }) device.FreeRID(rid); Console.WriteLine("PASS copied depth bounds/faces through two views/passes and multisample resolves.");
    }
    private static void DepthSampling(RenderingDevice device)
    {
        byte[] Read(string name) => File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", name)); using var code = new RDShaderSPIRV { BytecodeVertex = Read("wide.vert.glsl.spv"), BytecodeFragment = Read("sample-swizzle.frag.glsl.spv") }; var shader = device.ShaderCreateFromSPIRV(code); var images = SampleTarget(device); device.FreeRID(images.Target);
        using var depthFormat = new RDTextureFormat { Width = 4, Height = 4, Samples = TextureSamples.Samples4, Format = DataFormat.D32SFLOATS8UINT, UsageBits = TextureUsageBits.DepthStencilAttachment }; using var view = new RDTextureView(); var depth = device.TextureCreate(depthFormat, view); var target = device.FramebufferCreate([images.Source, depth]);
        using var sourceFormat = new RDTextureFormat { Width = 1, Height = 1, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.Sampling }; using var sourceView = new RDTextureView { SwizzleR = TextureSwizzle.G }; float[] color = [.2f, .8f, .4f, .25f]; var source = device.TextureCreate(sourceFormat, sourceView, [MemoryMarshal.AsBytes(color.AsSpan()).ToArray()]);
        using var samplerState = new RDSamplerState(); var sampler = device.SamplerCreate(samplerState); using var uniform = new RDUniform { UniformType = UniformType.SamplerWithTexture, Binding = 0 }; uniform.AddID(sampler); uniform.AddID(source); var set = device.UniformSetCreate([uniform], shader, 0);
        using var raster = new RDPipelineRasterizationState { LineWidth = 4 }; using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; using var samples = new RDPipelineMultisampleState { SampleCount = TextureSamples.Samples4, SampleMasks = [1, 0, 0, 0], EnableAlphaToOne = true, EnableSampleShading = true, MinSampleShading = 1 };
        using var state = FaceState(7, 9, CompareOperator.Equal); state.EnableDepthRange = true; state.DepthRangeMin = .25f; state.DepthRangeMax = .75f;
        RID Pipeline() => device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Lines, raster, blend, depthStencilState: state, multisampleState: samples);
        var pipeline = Pipeline(); state.DepthRangeMin = .8f; state.DepthRangeMax = 1; var blocked = Pipeline(); state.DepthRangeMin = .25f; state.DepthRangeMax = .75f; raster.DiscardPrimitives = true; var discard = Pipeline(); state.FrontOpReference = state.BackOpReference = 0; float[] pixels = new float[64]; Color[] clear = [new(0, 0, 0, 0)];
        void Draw(RID p)
        {
            var list = device.DrawListBegin(target, DrawFlags.ClearAll, clear, clearDepthValue: .5f, clearStencilValue: 7); device.DrawListBindRenderPipeline(list, p); device.DrawListBindUniformSet(list, set, 0); device.DrawListDraw(list, false, 1, 2); device.DrawListEnd(); device.TextureResolveMultisample(images.Source, images.Resolve); device.TextureGetData(images.Resolve, 0, MemoryMarshal.AsBytes(pixels.AsSpan()));
        }
        Draw(pipeline); for (var y = 0; y < 4; y++) Check(Math.Abs(pixels[(y * 4 + 2) * 4] - .2f) < .0001f && pixels[(y * 4 + 2) * 4 + 3] == .25f, "Sampled component variant retains copied bounds, stencil, line width, masks and alpha-to-one.");
        foreach (var rejected in new[] { blocked, discard }) { Draw(rejected); Check(pixels.All(v => v == 0), "Copied bounds and primitive discard independently suppress component-map variants."); }
        for (var i = 0; i < 16; i++) Draw(pipeline); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Draw(pipeline); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared depth/stencil sampled line variants add zero owner bytes.");
        foreach (var rid in new[] { pipeline, blocked, discard, set, sampler, source, target, depth, images.Source, images.Resolve, shader }) device.FreeRID(rid); Console.WriteLine("PASS copied depth/stencil through sampled line/MSAA/discard variants.");
    }
    private static void DepthCanvas()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); using var root = new Window { Size = new(16, 16) }; using var texture = new TextureRD(); var frames = 0;
        root.Ready += _ => { texture.TextureRDRID = DepthPixels(RenderingServer.GetRenderingDevice()!, "glsl", "depth"); root.AddChild(new Patch(texture)); RenderingServer.FramePostDraw += Captured; };
        void Captured()
        {
            if (++frames < 3) return; RenderingServer.FramePostDraw -= Captured; using var image = root.GetTexture().GetImage()!; var pixel = image.GetPixel(8, 8); Check(pixel.R > .99f && Math.Abs(pixel.G - .5f) < .01f && Math.Abs(pixel.B - .25f) < .01f, "Independent native depth/stencil output reaches retained canvas."); root.Tree!.Quit();
        }
        Engine.Run(root); Console.WriteLine("PASS depth/stencil output reaches retained canvas.");
    }
}
