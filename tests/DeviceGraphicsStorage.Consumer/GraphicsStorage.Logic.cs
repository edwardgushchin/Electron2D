using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class GraphicsStorage
{
    private static readonly uint[] LogicTruth = [0x00, 0x88, 0x44, 0xCC, 0x22, 0xAA, 0x66, 0xEE, 0x11, 0x99, 0x55, 0xDD, 0x33, 0xBB, 0x77, 0xFF];
    private static void Logic(string mode)
    {
        if (mode == "logic-canvas") { LogicCanvas(); LogicCanvas(); return; }
        LogicMetadata();
        using var device = RenderingServer.CreateLocalRenderingDevice(); Console.WriteLine("Logic device: " + device.GetDeviceName());
        if (mode is "logic-refuse" or "logic-unavailable" or "logic-failure") { LogicRefusal(device, mode); return; }
        if (mode is "logic-edges" or "logic-samples") { foreach (var language in new[] { "glsl", "hlsl" }) LogicEdges(device, language, mode == "logic-samples"); return; }
        foreach (var language in new[] { "glsl", "hlsl" })
            foreach (var format in new[] { DataFormat.R32G32B32A32UINT, DataFormat.R32G32B32A32SINT, DataFormat.R8G8B8A8UINT, DataFormat.R8G8B8A8SINT, DataFormat.R8G8B8A8UNORM, DataFormat.R32G32B32A32SFLOAT, DataFormat.R8G8B8A8SRGB }) LogicPixels(device, language, format, mode == "logic-source");
    }
    private static void LogicMetadata()
    {
        var state = new RDPipelineColorBlendState(); Check(!state.EnableLogicOp && state.LogicOp == LogicOperation.Clear, "Copied logical color descriptions retain both defaults.");
        var enabled = state.GetPropertyList().Single(p => p.Name == nameof(state.EnableLogicOp)); var operation = state.GetPropertyList().Single(p => p.Name == nameof(state.LogicOp)); Check(enabled.ValueType == typeof(bool) && operation.ValueType == typeof(LogicOperation), "Logical color metadata retains its Boolean and enum types.");
        state.EnableLogicOp = true; state.LogicOp = LogicOperation.Xor; Check(state.PropertyCanRevert(enabled) && state.PropertyCanRevert(operation), "Logical color descriptions retain typed revert values."); state.RevertProperty(enabled); state.RevertProperty(operation); Check(!state.EnableLogicOp && state.LogicOp == LogicOperation.Clear, "Logical color reversion restores both defaults.");
        Reject<ArgumentOutOfRangeException>(() => state.LogicOp = (LogicOperation)17); state.Dispose(); Reject<ObjectDisposedException>(() => state.EnableLogicOp = true); Reject<ObjectDisposedException>(() => _ = state.LogicOp);
    }
    private static RID LogicShader(RenderingDevice device, string language, string kind, bool source)
    {
        string Path(string name) => System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", name + "." + language);
        if (source)
        {
            using var text = new RDShaderSource { Language = language == "glsl" ? ShaderLanguage.GLSL : ShaderLanguage.HLSL, SourceVertex = File.ReadAllText(Path("read.vert")), SourceFragment = File.ReadAllText(Path("logic-" + kind + ".frag")) }; using var code = device.ShaderCompileSPIRVFromSource(text); Check(code.CompileErrorVertex == "" && code.CompileErrorFragment == "", code.CompileErrorVertex + code.CompileErrorFragment); return device.ShaderCreateFromSPIRV(code);
        }
        using var binarySource = new RDShaderSPIRV { BytecodeVertex = File.ReadAllBytes(Path("read.vert") + ".spv"), BytecodeFragment = File.ReadAllBytes(Path("logic-" + kind + ".frag") + ".spv") };
        var placeholder = device.ShaderCreatePlaceholder(); Check(device.ShaderCreateFromBytecode(device.ShaderCompileBinaryFromSPIRV(binarySource), placeholder) == placeholder, "Logic shaders preserve binary placeholder identity."); return placeholder;
    }
    private static void LogicRefusal(RenderingDevice device, string mode)
    {
        foreach (var language in new[] { "glsl", "hlsl" })
        {
            var shader = LogicShader(device, language, "float", false); using var format = new RDTextureFormat { Width = 4, Height = 4, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom }; using var view = new RDTextureView(); var image = device.TextureCreate(format, view); var target = device.FramebufferCreate([image]);
            using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { EnableLogicOp = true, Attachments = [attachment] };
            for (var i = 0; i < 16; i++) { blend.LogicOp = (LogicOperation)i; if (mode == "logic-failure") Reject<InvalidOperationException>(() => device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend)); else Reject<NotSupportedException>(() => device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend)); }
            blend.EnableLogicOp = false; var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend); float[] push = [.25f, .5f, .75f, 1], result = new float[64]; var list = device.DrawListBegin(target); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(push.AsSpan()), 16); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); device.TextureGetData(image, 0, MemoryMarshal.AsBytes(result.AsSpan())); Check(result[0] == .25f && result[1] == .5f && result[2] == .75f && result[3] == 1, "Refusing logical state preserves executing disabled-state graphics.");
            foreach (var rid in new[] { pipeline, target, image, shader }) device.FreeRID(rid);
        }
        Console.WriteLine("PASS " + mode + ": all logical operations reject explicitly while disabled-state graphics executes.");
    }
    private static void LogicPixels(RenderingDevice device, string language, DataFormat pixelFormat, bool source)
    {
        var wide = pixelFormat is DataFormat.R32G32B32A32UINT or DataFormat.R32G32B32A32SINT or DataFormat.R32G32B32A32SFLOAT;
        var signed = pixelFormat is DataFormat.R32G32B32A32SINT or DataFormat.R8G8B8A8SINT;
        var floating = pixelFormat is DataFormat.R32G32B32A32SFLOAT or DataFormat.R8G8B8A8SRGB;
        var normalized = pixelFormat == DataFormat.R8G8B8A8UNORM; var integer = !floating && !normalized;
        var shader = LogicShader(device, language, integer ? signed ? "int" : "uint" : "float", source);
        using var textureFormat = new RDTextureFormat { Width = 4, Height = 4, Format = pixelFormat, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom | TextureUsageBits.Sampling }; using var view = new RDTextureView();
        Check(device.TextureIsFormatSupportedForUsage(pixelFormat, textureFormat.UsageBits), "The tested native color format must be available: " + pixelFormat);
        var image = device.TextureCreate(textureFormat, view); var target = device.FramebufferCreate([image]);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment { EnableBlend = true }; using var blend = new RDPipelineColorBlendState { EnableLogicOp = true, Attachments = [attachment] };
        Check(blend.LogicOp == LogicOperation.Clear, "Logical operation defaults to Clear."); Reject<ArgumentOutOfRangeException>(() => blend.LogicOp = LogicOperation.Max); Reject<ArgumentOutOfRangeException>(() => blend.LogicOp = (LogicOperation)(-1));
        var pipelines = new RID[16]; for (var i = 0; i < 16; i++) { blend.LogicOp = (LogicOperation)i; pipelines[i] = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend); }
        attachment.EnableBlend = false; attachment.WriteR = false; blend.EnableLogicOp = false; blend.LogicOp = LogicOperation.Set;
        uint value = wide ? 0xCCCCCCCC : signed ? unchecked((uint)-52) : 0xCC; uint destination = wide ? 0xAAAAAAAA : signed ? unchecked((uint)-86) : 0xAA;
        uint[] push = floating ? [BitConverter.SingleToUInt32Bits(.25f), BitConverter.SingleToUInt32Bits(.5f), BitConverter.SingleToUInt32Bits(.75f), BitConverter.SingleToUInt32Bits(1)] : normalized ? [BitConverter.SingleToUInt32Bits(.8f), BitConverter.SingleToUInt32Bits(.8f), BitConverter.SingleToUInt32Bits(.8f), BitConverter.SingleToUInt32Bits(.8f)] : [value, value, value, value];
        var lane = integer ? BitConverter.UInt32BitsToSingle(destination) : 170f / 255; Color[] clear = [new(lane, lane, lane, lane)]; byte[] bytes = new byte[16 * (wide ? 16 : 4)];
        void Draw(int operation)
        {
            var list = device.DrawListBegin(target, DrawFlags.ClearColorAll, clear); device.DrawListBindRenderPipeline(list, pipelines[operation]); device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(push.AsSpan()), 16); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); device.TextureGetData(image, 0, bytes);
        }
        for (var operation = 0; operation < 16; operation++)
        {
            Draw(operation);
            if (floating)
            {
                if (wide) { var pixels = MemoryMarshal.Cast<byte, float>(bytes); for (var i = 0; i < pixels.Length; i++) Check(pixels[i] == (i % 4 == 0 ? .25f : i % 4 == 1 ? .5f : i % 4 == 2 ? .75f : 1), "Floating-point attachments copy source and ignore logic and arithmetic blending."); }
                else { byte[] expected = [137, 188, 225, 255]; for (var i = 0; i < bytes.Length; i++) Check(Math.Abs(bytes[i] - expected[i % 4]) <= 1, "sRGB attachments encode the unblended source without bitwise operations."); }
            }
            else if (wide) { var expected = LogicTruth[operation] * 0x01010101u; foreach (var pixel in MemoryMarshal.Cast<byte, uint>(bytes)) Check(pixel == expected, "All 16 logical truth tables apply to encoded 32-bit components."); }
            else foreach (var pixel in bytes) Check(pixel == LogicTruth[operation], "All 16 logical truth tables apply to encoded normalized/signed/unsigned byte components.");
        }
        for (var i = 0; i < 16; i++) Draw(6); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Draw(6); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared logical draw/span-readback cycles allocate zero owner bytes.");
        foreach (var pipeline in pipelines) device.FreeRID(pipeline); foreach (var rid in new[] { target, image, shader }) device.FreeRID(rid);
        Console.WriteLine("PASS " + language + " " + pixelFormat + ": 16 native logical operations, copied state, ignored arithmetic blending and zero owner bytes.");
    }
    private static void LogicEdges(RenderingDevice device, string language, bool multisample)
    {
        var shader = LogicShader(device, language, "mixed", false); using var view = new RDTextureView();
        using var format = new RDTextureFormat { Width = 4, Height = 4, TextureType = TextureType.TextureArray, ArrayLayers = 2, Format = DataFormat.R8G8B8A8UINT, Samples = multisample ? TextureSamples.Samples4 : TextureSamples.Samples1, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom | (multisample ? 0 : TextureUsageBits.Sampling) };
        var integer = device.TextureCreate(format, view); format.Format = DataFormat.R8G8B8A8UNORM; var normalized = device.TextureCreate(format, view);
        RID integerResolve = default, normalizedResolve = default;
        if (multisample) { format.Samples = TextureSamples.Samples1; format.IsResolveBuffer = true; format.UsageBits |= TextureUsageBits.Sampling; normalizedResolve = device.TextureCreate(format, view); format.Format = DataFormat.R8G8B8A8UINT; integerResolve = device.TextureCreate(format, view); format.IsResolveBuffer = false; }
        format.Samples = multisample ? TextureSamples.Samples4 : TextureSamples.Samples1; format.Format = DataFormat.D32SFLOATS8UINT; format.UsageBits = TextureUsageBits.DepthStencilAttachment; var depth = device.TextureCreate(format, view);
        using var first = new RDFramebufferPass { ColorAttachments = [0, 1], DepthAttachment = 2, ResolveAttachments = multisample ? [3, 4] : [] }; using var second = new RDFramebufferPass { ColorAttachments = [0, 1], DepthAttachment = 2, ResolveAttachments = multisample ? [3, 4] : [] }; var target = device.FramebufferCreateMultipass(multisample ? [integer, normalized, depth, integerResolve, normalizedResolve] : [integer, normalized, depth], [first, second], viewCount: 2);
        using var sourceFormat = new RDTextureFormat { Width = 1, Height = 1, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.Sampling }; using var sourceView = new RDTextureView { SwizzleR = TextureSwizzle.B, SwizzleB = TextureSwizzle.R }; float[] source = [.8f, .4f, .2f, 1]; var sampled = device.TextureCreate(sourceFormat, sourceView, [MemoryMarshal.AsBytes(source.AsSpan()).ToArray()]);
        using var samplerState = new RDSamplerState(); var sampler = device.SamplerCreate(samplerState); sourceFormat.Width = 4; sourceFormat.Height = 4; sourceFormat.Format = DataFormat.R32UINT; sourceFormat.UsageBits = TextureUsageBits.Storage | TextureUsageBits.CanCopyFrom | TextureUsageBits.CanCopyTo; var counter = device.TextureCreate(sourceFormat, view);
        using var imageUniform = new RDUniform { Binding = 1, UniformType = UniformType.Image }; imageUniform.AddID(counter); using var sampledUniform = new RDUniform { Binding = 0, UniformType = UniformType.SamplerWithTexture }; sampledUniform.AddID(sampler); sampledUniform.AddID(sampled); var set = device.UniformSetCreate([sampledUniform, imageUniform], shader, 0);
        using var a = new RDPipelineColorBlendStateAttachment { EnableBlend = true, WriteG = false, WriteA = false }; using var b = new RDPipelineColorBlendStateAttachment { EnableBlend = true, WriteR = false, WriteB = false }; using var blend = new RDPipelineColorBlendState { EnableLogicOp = true, LogicOp = LogicOperation.Xor, Attachments = [a, b] }; using var raster = new RDPipelineRasterizationState(); using var faces = FaceState(3, 5); faces.EnableDepthRange = true; faces.DepthRangeMax = 1;
        using var samples = new RDPipelineMultisampleState { SampleCount = multisample ? TextureSamples.Samples4 : TextureSamples.Samples1, EnableSampleShading = multisample, MinSampleShading = 1, SampleMasks = multisample ? [15, 0, 0, 0] : [] };
        var firstPipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend, depthStencilState: faces, multisampleState: samples); blend.LogicOp = LogicOperation.Invert; var secondPipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend, depthStencilState: faces, multisampleState: samples, forRenderPass: 1);
        a.WriteR = false; b.WriteG = false; blend.EnableLogicOp = false; samples.SampleMasks = multisample ? [0, 0, 0, 0] : [0]; faces.DepthRangeMax = 0;
        var bits = BitConverter.UInt32BitsToSingle(0xAA); Color[] clear = multisample ? [new(bits, bits, bits, bits), new(170f / 255, 170f / 255, 170f / 255, 170f / 255), new(bits, bits, bits, bits), new(170f / 255, 170f / 255, 170f / 255, 170f / 255)] : [new(bits, bits, bits, bits), new(170f / 255, 170f / 255, 170f / 255, 170f / 255)]; byte[] bytes = new byte[64]; uint[] counts = new uint[16]; byte[] expectedInteger = [0x66, 0xAA, 0x99, 0xAA], expectedNormalized = [0xAA, 0x33, 0xAA, 0xAA];
        void Draw()
        {
            device.TextureClear(counter, default, 0, 1, 0, 1); var list = device.DrawListBegin(target, DrawFlags.ClearColorAll | DrawFlags.ClearDepth | DrawFlags.ClearStencil, clear);
            for (var pass = 0; pass < 2; pass++) { if (pass != 0) device.DrawListSwitchToNextPass(); device.DrawListBindRenderPipeline(list, pass == 0 ? firstPipeline : secondPipeline); device.DrawListBindUniformSet(list, set, 0); device.DrawListEnableScissor(list, new(0, 0, 2, 4)); device.DrawListDraw(list, false, 1, 3); device.DrawListEnableScissor(list, new(2, 0, 2, 4)); device.DrawListDraw(list, false, 1, 3); }
            device.DrawListEnd();
            for (uint layer = 0; layer < 2; layer++) { device.TextureGetData(multisample ? integerResolve : integer, layer, bytes); for (var i = 0; i < 64; i++) Check(bytes[i] == expectedInteger[i % 4], "Integer MRT preserves per-channel logical masks across views and passes."); device.TextureGetData(multisample ? normalizedResolve : normalized, layer, bytes); for (var i = 0; i < 64; i++) Check(Math.Abs(bytes[i] - expectedNormalized[i % 4]) <= 1, "Normalized MRT retains component maps, logical masks and sample resolves."); }
            device.TextureGetData(counter, 0, MemoryMarshal.AsBytes(counts.AsSpan())); foreach (var count in counts) Check(count == (multisample ? 16 : 4), "Native logical state composes with authoritative image atomics, sample shading and ordered scissor/view/pass restarts.");
        }
        Draw(); for (var i = 0; i < 16; i++) Draw(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Draw(); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared logical MRT/image/view/pass cycles allocate zero owner bytes.");
        foreach (var rid in new[] { firstPipeline, secondPipeline, set, target, integer, normalized, depth, sampled, sampler, counter, shader }) device.FreeRID(rid); if (multisample) { device.FreeRID(integerResolve); device.FreeRID(normalizedResolve); }
        Console.WriteLine("PASS " + language + ": logical MRT masks, component variants, views/passes/scissor, image atomics, copied depth faces/bounds" + (multisample ? ", sample masks/shading and resolves" : "") + " and zero owner bytes.");
    }
    private static void LogicCanvas()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); using var root = new Window { Size = new(16, 16) }; using var texture = new TextureRD(); var frames = 0;
        root.Ready += _ =>
        {
            var device = RenderingServer.GetRenderingDevice()!; var shader = LogicShader(device, "glsl", "float", false);
            using var format = new RDTextureFormat { Width = 4, Height = 4, Format = DataFormat.R8G8B8A8UNORM, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.Sampling }; using var view = new RDTextureView(); var image = device.TextureCreate(format, view); var target = device.FramebufferCreate([image]);
            using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment { EnableBlend = true, WriteA = false }; using var blend = new RDPipelineColorBlendState { EnableLogicOp = true, LogicOp = LogicOperation.Xor, Attachments = [attachment] }; var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend);
            float[] push = [.8f, .8f, .8f, 1]; var list = device.DrawListBegin(target, DrawFlags.ClearColorAll, [new(170f / 255, 170f / 255, 170f / 255, 1)]); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(push.AsSpan()), 16); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
            foreach (var rid in new[] { pipeline, target, shader }) device.FreeRID(rid); texture.TextureRDRID = image; root.AddChild(new Patch(texture)); RenderingServer.FramePostDraw += Captured;
        };
        void Captured()
        {
            if (++frames < 3) return; RenderingServer.FramePostDraw -= Captured; using var image = root.GetTexture().GetImage()!; var pixel = image.GetPixel(8, 8); Check(Math.Abs(pixel.R - .4f) < .01f && Math.Abs(pixel.G - .4f) < .01f && Math.Abs(pixel.B - .4f) < .01f && pixel.A > .99f, "Logical color results reach retained canvas pixels after their pipeline is released."); root.Tree!.Quit();
        }
        Engine.Run(root); Console.WriteLine("PASS logical XOR reaches retained canvas pixels and sequential renderer recreation.");
    }

}
