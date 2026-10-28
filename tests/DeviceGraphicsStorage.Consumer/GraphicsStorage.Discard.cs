using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class GraphicsStorage
{
    private static void Discard(string mode)
    {
        if (mode == "discard-canvas") { foreach (var language in new[] { "glsl", "hlsl" }) DiscardCanvas(language); return; }
        using var device = RenderingServer.CreateLocalRenderingDevice(); Console.WriteLine("Discard device: " + device.GetDeviceName());
        if (mode is "discard-unavailable" or "discard-failure" or "discard-vertex-only" or "discard-no-vertex") { DiscardProfiles(device, mode); return; }
        foreach (var language in new[] { "glsl", "hlsl" }) device.FreeRID(ExecuteDiscard(device, language, mode == "discard-source"));
        DiscardSampling(device);
    }
    private static void DiscardProfiles(RenderingDevice device, string mode)
    {
        foreach (var language in new[] { "glsl", "hlsl" })
        {
            using var code = new RDShaderSPIRV { BytecodeVertex = File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", "discard.vert." + language + ".spv")) };
            if (mode == "discard-no-vertex") { Reject<NotSupportedException>(() => device.ShaderCompileBinaryFromSPIRV(code)); Reject<NotSupportedException>(() => device.ShaderCreateFromSPIRV(code)); continue; }
            var shader = device.ShaderCreateFromSPIRV(code); var format = device.FramebufferFormatCreateEmpty(); using var raster = new RDPipelineRasterizationState { DiscardPrimitives = true }; using var blend = new RDPipelineColorBlendState();
            if (mode == "discard-vertex-only")
            {
                var target = device.FramebufferCreateEmpty(new(2, 2)); var pipeline = device.RenderPipelineCreate(shader, format, -1, RenderPrimitive.Triangles, raster, blend); var buffer = device.StorageBufferCreate(4); var set = Set(device, shader, buffer);
                var list = device.DrawListBegin(target); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 0); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); uint[] result = [0]; device.BufferGetData(buffer, MemoryMarshal.AsBytes(result.AsSpan())); Check(result[0] == 3, "Vertex-only discard does not require fragment stores."); foreach (var rid in new[] { pipeline, set, buffer, target }) device.FreeRID(rid);
            }
            else for (var i = 0; i < 3; i++)
                    if (mode == "discard-unavailable") Reject<NotSupportedException>(() => device.RenderPipelineCreate(shader, format, -1, RenderPrimitive.Triangles, raster, blend));
                    else Reject<InvalidOperationException>(() => device.RenderPipelineCreate(shader, format, -1, RenderPrimitive.Triangles, raster, blend));
            device.FreeRID(shader);
        }
        if (mode is "discard-unavailable" or "discard-failure") device.FreeRID(Execute(device, "glsl"));
        Console.WriteLine("PASS " + mode + ": explicit capability/failure/refusal and retained executing sibling profile.");
    }
    private static RID ExecuteDiscard(RenderingDevice device, string language, bool authoring)
    {
        var pair = Shader(device, "write", language, authoring); RID only;
        if (authoring)
        {
            using var source = new RDShaderSource { Language = language == "glsl" ? ShaderLanguage.GLSL : ShaderLanguage.HLSL, SourceVertex = File.ReadAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", "discard.vert." + language)) };
            using var compiled = device.ShaderCompileSPIRVFromSource(source); Check(compiled.CompileErrorVertex == "" && compiled.BytecodeFragment.Length == 0, compiled.CompileErrorVertex); only = device.ShaderCreateFromSPIRV(compiled);
        }
        else
        {
            using var code = new RDShaderSPIRV { BytecodeVertex = File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", "discard.vert." + language + ".spv")) };
            var binary = device.ShaderCompileBinaryFromSPIRV(code); Check(BinaryPrimitives.ReadUInt32LittleEndian(binary.AsSpan(8)) == 2, "Vertex-only binary uses its explicit version-two format.");
            var placeholder = device.ShaderCreatePlaceholder(); only = device.ShaderCreateFromBytecode(binary, placeholder); Check(only == placeholder, "Vertex-only binary populates the same placeholder.");
            var bad = binary.ToArray(); BinaryPrimitives.WriteUInt32LittleEndian(bad.AsSpan(8), 1); Reject<ArgumentException>(() => device.ShaderCreateFromBytecode(bad));
            bad = binary.ToArray(); BinaryPrimitives.WriteUInt32LittleEndian(bad.AsSpan(8), 3); Reject<NotSupportedException>(() => device.ShaderCreateFromBytecode(bad));
        }
        var format = device.FramebufferFormatCreateEmpty(); var target = device.FramebufferCreateEmpty(new(7, 5), validateWithFormat: format);
        using var raster = new RDPipelineRasterizationState { DiscardPrimitives = true }; using var blend = new RDPipelineColorBlendState(); using var factor = new RDPipelineSpecializationConstant<uint> { ConstantID = 0, Value = 2 };
        var pairedPipeline = device.RenderPipelineCreate(pair, format, -1, RenderPrimitive.Triangles, raster, blend, specializationConstants: [factor]); var onlyPipeline = device.RenderPipelineCreate(only, format, -1, RenderPrimitive.Triangles, raster, blend, specializationConstants: [factor]);
        raster.DiscardPrimitives = false; Reject<ArgumentException>(() => device.RenderPipelineCreate(only, format, -1, RenderPrimitive.Triangles, raster, blend));
        var buffer = device.StorageBufferCreate(48, usage: StorageBufferUsage.DispatchIndirect); var pairSet = Set(device, pair, buffer); var onlySet = Set(device, only, buffer); uint[] push = [1]; uint[] result = new uint[12];
        var compute = Shader(device, "chain", language, authoring); var kernel = device.ComputePipelineCreate(compute); var computeSet = Set(device, compute, buffer);
        void Draw()
        {
            device.BufferClear(buffer, 0, 48); var list = device.DrawListBegin(target, region: new(1, 1, 2, 2)); device.DrawListBindRenderPipeline(list, pairedPipeline); device.DrawListBindUniformSet(list, pairSet, 0); device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(push.AsSpan()), 4); device.DrawListDraw(list, false, 1, 3);
            device.DrawListBindRenderPipeline(list, onlyPipeline); device.DrawListBindUniformSet(list, onlySet, 0); device.DrawListDrawIndirect(list, false, buffer, offset: 16, drawCount: 0); device.DrawListDrawIndirect(list, false, buffer, offset: 16, drawCount: 2); device.DrawListEnd();
            list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, kernel); device.ComputeListBindUniformSet(list, computeSet, 0); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); device.BufferGetData(buffer, MemoryMarshal.AsBytes(result.AsSpan()));
        }
        Draw(); Check(result[0] == 18 && result[1] == 0 && result[2] == 18, "Copied discard state executes every vertex, skips the fragment and orders direct/indirect writes before compute.");
        for (var i = 0; i < 16; i++) Draw(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Draw(); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared discard cycles allocate zero owner bytes.");
        using var colorFormat = new RDTextureFormat { Width = 4, Height = 4, ArrayLayers = 2, TextureType = TextureType.TextureArray, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom | TextureUsageBits.Sampling }; using var colorView = new RDTextureView(); var image = device.TextureCreate(colorFormat, colorView);
        using var first = new RDFramebufferPass { ColorAttachments = [0] }; using var second = new RDFramebufferPass { ColorAttachments = [0] }; var colorTarget = device.FramebufferCreateMultipass([image], [first, second], viewCount: 2);
        using var attachment = new RDPipelineColorBlendStateAttachment(); using var colorBlend = new RDPipelineColorBlendState { Attachments = [attachment] }; raster.DiscardPrimitives = true;
        var p0 = device.RenderPipelineCreate(only, device.FramebufferGetFormat(colorTarget), -1, RenderPrimitive.Triangles, raster, colorBlend, specializationConstants: [factor]); var p1 = device.RenderPipelineCreate(only, device.FramebufferGetFormat(colorTarget), -1, RenderPrimitive.Triangles, raster, colorBlend, specializationConstants: [factor], forRenderPass: 1);
        device.BufferClear(buffer, 0, 48); var colorList = device.DrawListBegin(colorTarget, DrawFlags.ClearColorAll, [new(.2f, .3f, .4f, 1)]); device.DrawListBindRenderPipeline(colorList, p0); device.DrawListBindUniformSet(colorList, onlySet, 0); device.DrawListDraw(colorList, false, 1, 3); device.DrawListSwitchToNextPass(); device.DrawListBindRenderPipeline(colorList, p1); device.DrawListBindUniformSet(colorList, onlySet, 0); device.DrawListDraw(colorList, false, 1, 3); device.DrawListEnd(); device.BufferGetData(buffer, MemoryMarshal.AsBytes(result.AsSpan()));
        Check(result[0] == 24 && result[1] == 0, "Both discard passes execute every view without fragments."); var pixels = new float[64];
        for (uint layer = 0; layer < 2; layer++) { device.TextureGetData(image, layer, MemoryMarshal.AsBytes(pixels.AsSpan())); for (var i = 0; i < pixels.Length; i += 4) Check(Math.Abs(pixels[i] - .2f) < .001f && Math.Abs(pixels[i + 1] - .3f) < .001f && Math.Abs(pixels[i + 2] - .4f) < .001f && pixels[i + 3] == 1, "Discard preserves color attachments in every view and pass."); }
        var single4 = device.FramebufferFormatCreateEmpty(TextureSamples.Samples4); var empty4 = device.FramebufferCreateEmpty(new(4, 4), TextureSamples.Samples4, single4); using var msaa = new RDPipelineMultisampleState { SampleCount = TextureSamples.Samples4 }; var sampled = device.RenderPipelineCreate(only, single4, -1, RenderPrimitive.Triangles, raster, blend, multisampleState: msaa);
        device.BufferClear(buffer, 0, 48); colorList = device.DrawListBegin(empty4); device.DrawListBindRenderPipeline(colorList, sampled); device.DrawListBindUniformSet(colorList, onlySet, 0); device.DrawListDraw(colorList, false, 1, 3); device.DrawListEnd(); device.FreeRID(empty4); device.BufferGetData(buffer, MemoryMarshal.AsBytes(result.AsSpan())); Check(result[0] == 3, "MSAA discard retains exact framebuffer compatibility and retires pending work.");
        foreach (var rid in new[] { p0, p1, colorTarget, image, sampled }) device.FreeRID(rid);
        using var resultFormat = new RDTextureFormat { Width = 1, Height = 1, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom | TextureUsageBits.Sampling }; using var resultView = new RDTextureView(); var resultImage = device.TextureCreate(resultFormat, resultView); var resultTarget = device.FramebufferCreate([resultImage]);
        var reader = Shader(device, "read", language, authoring); var readSet = Set(device, reader, buffer); using var ordinary = new RDPipelineRasterizationState(); var readPipeline = device.RenderPipelineCreate(reader, device.FramebufferGetFormat(resultTarget), -1, RenderPrimitive.Triangles, ordinary, colorBlend);
        colorList = device.ComputeListBegin(); device.ComputeListBindComputePipeline(colorList, kernel); device.ComputeListBindUniformSet(colorList, computeSet, 0); device.ComputeListDispatch(colorList, 1, 1, 1); device.ComputeListEnd(); colorList = device.DrawListBegin(resultTarget); device.DrawListBindRenderPipeline(colorList, readPipeline); device.DrawListBindUniformSet(colorList, readSet, 0); device.DrawListDraw(colorList, false, 1, 3); device.DrawListEnd();
        var finalPixels = new float[4]; device.TextureGetData(resultImage, 0, MemoryMarshal.AsBytes(finalPixels.AsSpan())); Check(Math.Abs(finalPixels[0] - .3f) < .001f && finalPixels[1] == 0 && Math.Abs(finalPixels[2] - .03f) < .001f, "Discard writes reach ordinary graphics after compute."); foreach (var rid in new[] { reader, readSet, readPipeline, resultTarget }) device.FreeRID(rid);
        foreach (var rid in new[] { pairedPipeline, onlyPipeline, pairSet, onlySet, computeSet, kernel, pair, only, compute, target, buffer }) device.FreeRID(rid);
        Console.WriteLine("PASS " + language + ": vertex-only/no-position source or binary, paired fragment suppression, copied discard, specialization, indirect ordering, compute/readback and 32 zero-owner-byte cycles."); return resultImage;
    }
    private static void DiscardSampling(RenderingDevice device)
    {
        byte[] Read(string name) => File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", name));
        using var code = new RDShaderSPIRV { BytecodeVertex = Read("discard-sample.vert.glsl.spv"), BytecodeFragment = Read("write.frag.glsl.spv") }; var shader = device.ShaderCreateFromSPIRV(code);
        using var sourceFormat = new RDTextureFormat { Width = 1, Height = 1, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.Sampling | TextureUsageBits.CanUpdate }; using var sourceView = new RDTextureView { SwizzleR = TextureSwizzle.G }; float[] source = [2, 3, 4, 1]; var texture = device.TextureCreate(sourceFormat, sourceView, [MemoryMarshal.AsBytes(source.AsSpan()).ToArray()]);
        using var targetFormat = new RDTextureFormat { Width = 1, Height = 1, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom }; using var targetView = new RDTextureView(); var image = device.TextureCreate(targetFormat, targetView); var target = device.FramebufferCreate([image]);
        using var raster = new RDPipelineRasterizationState { DiscardPrimitives = true }; using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend);
        var buffer = device.StorageBufferCreate(8); using var samplerState = new RDSamplerState(); var sampler = device.SamplerCreate(samplerState); using var store = new RDUniform { UniformType = UniformType.StorageBuffer, Binding = 0 }; store.AddID(buffer); using var sample = new RDUniform { UniformType = UniformType.SamplerWithTexture, Binding = 1 }; sample.AddID(sampler); sample.AddID(texture); var set = device.UniformSetCreate([store, sample], shader, 0); uint[] push = [1]; uint[] result = [0, 0]; float[] pixel = new float[4];
        void Draw()
        {
            device.BufferClear(buffer, 0, 8); var list = device.DrawListBegin(target, DrawFlags.ClearColorAll, [new(.1f, .2f, .3f, 1)]); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 0); device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(push.AsSpan()), 4); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); device.BufferGetData(buffer, MemoryMarshal.AsBytes(result.AsSpan())); device.TextureGetData(image, 0, MemoryMarshal.AsBytes(pixel.AsSpan()));
        }
        Draw(); Check(result[0] == 9 && result[1] == 0 && Math.Abs(pixel[0] - .1f) < .001f, "Sampled component variants retain copied discard and leave fragment storage and color untouched."); for (var i = 0; i < 16; i++) Draw(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Draw(); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared sampled discard variants allocate zero owner bytes.");
        foreach (var rid in new[] { set, pipeline, shader, buffer, sampler, texture, target, image }) device.FreeRID(rid); Console.WriteLine("PASS sampled-swizzle discard variants and 32 zero-owner-byte cycles.");
    }
    private static void DiscardCanvas(string language)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); using var root = new Window { Size = new(16, 16) }; using var texture = new TextureRD(); var frames = 0;
        root.Ready += _ => { texture.TextureRDRID = ExecuteDiscard(RenderingServer.GetRenderingDevice()!, language, false); root.AddChild(new Patch(texture)); RenderingServer.FramePostDraw += Captured; };
        void Captured()
        {
            if (++frames < 3) return; RenderingServer.FramePostDraw -= Captured; using var image = root.GetTexture().GetImage()!; var pixel = image.GetPixel(8, 8); Check(Math.Abs(pixel.R - .3f) < .01f && pixel.G < .01f && Math.Abs(pixel.B - .03f) < .01f, "Discard vertex writes reach retained canvas pixels."); root.Tree!.Quit();
        }
        Engine.Run(root); Console.WriteLine("PASS " + language + ": discard vertex output reaches retained canvas.");
    }

}
