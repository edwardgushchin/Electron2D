using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class GraphicsStorage
{
    private static void Samples(string mode)
    {
        if (mode == "samples-canvas") { SamplesCanvas(); return; }
        using var device = RenderingServer.CreateLocalRenderingDevice(); Console.WriteLine("Multisample device: " + device.GetDeviceName());
        if (mode.StartsWith("samples-refuse", StringComparison.Ordinal)) { SamplesRefusal(device, mode); return; }
        if (mode is "samples-unavailable" or "samples-failure") { SamplesFailure(device, mode); return; }
        foreach (var language in new[] { "glsl", "hlsl" }) device.FreeRID(SamplesCoverage(device, language, mode == "samples-source"));
        SamplesViews(device); SamplesSwizzle(device);
    }
    private static RID SampleShader(RenderingDevice device, string name, string language, bool authoring = false)
    {
        var vertex = name == "sample-interpolation" ? name : "sample";
        string FileName(string stem, string stage, string suffix) => System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", stem + "." + stage + "." + language + suffix);
        if (authoring)
        {
            using var source = new RDShaderSource { Language = language == "glsl" ? ShaderLanguage.GLSL : ShaderLanguage.HLSL, SourceVertex = File.ReadAllText(FileName(vertex, "vert", "")), SourceFragment = File.ReadAllText(FileName(name, "frag", "")) };
            using var code = device.ShaderCompileSPIRVFromSource(source); Check(code.CompileErrorVertex == "" && code.CompileErrorFragment == "", code.CompileErrorVertex + code.CompileErrorFragment); return device.ShaderCreateFromSPIRV(code);
        }
        using var bytes = SampleCode(name, language); var binary = device.ShaderCompileBinaryFromSPIRV(bytes);
        if (Environment.GetEnvironmentVariable("ELECTRON2D_SAMPLE_OUTPUT") is { } output) { Directory.CreateDirectory(output); File.WriteAllBytes(System.IO.Path.Combine(output, name + "." + language + ".bin"), binary); }
        return device.ShaderCreateFromBytecode(binary);
    }
    private static RDShaderSPIRV SampleCode(string name, string language)
    {
        byte[] Read(string stem, string stage) => File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", stem + "." + stage + "." + language + ".spv"));
        return new() { BytecodeVertex = Read(name == "sample-interpolation" ? name : "sample", "vert"), BytecodeFragment = Read(name, "frag") };
    }
    private static (RID Source, RID Resolve, RID Target) SampleTarget(RenderingDevice device, uint layers = 1)
    {
        using var format = new RDTextureFormat { Width = 4, Height = 4, TextureType = layers == 1 ? TextureType.Texture : TextureType.TextureArray, ArrayLayers = layers, Format = DataFormat.R32G32B32A32SFLOAT, Samples = TextureSamples.Samples4, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom };
        using var view = new RDTextureView(); var source = device.TextureCreate(format, view); var target = device.FramebufferCreate([source], viewCount: (int)layers);
        format.Samples = TextureSamples.Samples1; format.IsResolveBuffer = true; format.UsageBits |= TextureUsageBits.CanCopyTo | TextureUsageBits.Sampling; var resolve = device.TextureCreate(format, view); return (source, resolve, target);
    }
    private static RID SamplesCoverage(RenderingDevice device, string language, bool authoring)
    {
        var shader = SampleShader(device, "sample", language, authoring); var images = SampleTarget(device); var format = device.FramebufferGetFormat(images.Target);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; using var state = new RDPipelineMultisampleState { SampleCount = TextureSamples.Samples4 };
        Check(!state.EnableAlphaToOne && !state.EnableSampleShading && state.MinSampleShading == 0 && state.SampleMasks.Length == 0, "Multisample defaults retain ordinary coverage and frequency.");
        foreach (var bad in new[] { -.1f, 1.1f, float.NaN, float.PositiveInfinity }) Reject<ArgumentOutOfRangeException>(() => state.MinSampleShading = bad);
        Reject<ArgumentNullException>(() => state.SampleMasks = null!); state.SampleMasks = [1, 2]; Reject<ArgumentException>(() => device.RenderPipelineCreate(shader, format, -1, RenderPrimitive.Triangles, raster, blend, multisampleState: state));
        uint[] original = [1, 0, 0, 0]; state.SampleMasks = original; original[0] = 0; var copy = state.SampleMasks; copy[0] = 0; Check(state.SampleMasks[0] == 1, "Mask description copies both setter and getter arrays.");
        var pipelines = new List<(RID Pipeline, float Coverage)>();
        foreach (var mask in new uint[]?[] { null, [0, 0, 0, 0], [1, 0, 0, 0], [3, 0, 0, 0], [7, 0, 0, 0], [uint.MaxValue, 0, 0, 0], [0xFFFFFFF0, 0, 0, 0], [0, uint.MaxValue, uint.MaxValue, uint.MaxValue] })
        {
            state.SampleMasks = mask ?? []; var pipeline = device.RenderPipelineCreate(shader, format, -1, RenderPrimitive.Triangles, raster, blend, multisampleState: state);
            var value = mask is null ? 1 : System.Numerics.BitOperations.PopCount(mask[0] & 15) / 4f; pipelines.Add((pipeline, value));
        }
        state.SampleMasks = []; state.EnableAlphaToOne = true; var one = device.RenderPipelineCreate(shader, format, -1, RenderPrimitive.Triangles, raster, blend, multisampleState: state);
        state.EnableAlphaToCoverage = true; var oneCoverage = device.RenderPipelineCreate(shader, format, -1, RenderPrimitive.Triangles, raster, blend, multisampleState: state); state.EnableAlphaToOne = false;
        var coverage = device.RenderPipelineCreate(shader, format, -1, RenderPrimitive.Triangles, raster, blend, multisampleState: state); state.SampleMasks = [0, 0, 0, 0]; state.EnableAlphaToCoverage = false;
        state.SampleMasks = []; attachment.EnableBlend = true; attachment.SrcColorBlendFactor = BlendFactor.SrcAlpha; attachment.DstColorBlendFactor = BlendFactor.Zero;
        var blended = device.RenderPipelineCreate(shader, format, -1, RenderPrimitive.Triangles, raster, blend, multisampleState: state); state.EnableAlphaToOne = true;
        var blendedOne = device.RenderPipelineCreate(shader, format, -1, RenderPrimitive.Triangles, raster, blend, multisampleState: state); attachment.EnableBlend = false;
        raster.DiscardPrimitives = true; state.SampleMasks = [1, 0, 0, 0]; state.EnableAlphaToOne = true; state.EnableSampleShading = true; state.MinSampleShading = 1;
        var discarded = device.RenderPipelineCreate(shader, format, -1, RenderPrimitive.Triangles, raster, blend, multisampleState: state); raster.DiscardPrimitives = false;
        float[] pixels = new float[64]; Color[] clear = [new(0, 0, 0, 0)];
        void Draw(RID pipeline)
        {
            var list = device.DrawListBegin(images.Target, DrawFlags.ClearColorAll, clear); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); device.TextureResolveMultisample(images.Source, images.Resolve); device.TextureGetData(images.Resolve, 0, MemoryMarshal.AsBytes(pixels.AsSpan()));
        }
        foreach (var (pipeline, expected) in pipelines) { Draw(pipeline); for (var i = 0; i < 64; i += 4) Check(Math.Abs(pixels[i] - expected) < .0001f && Math.Abs(pixels[i + 3] - .25f * expected) < .0001f, "Sample mask controls exact resolved coverage and preserves copied state."); }
        Draw(discarded); Check(pixels[0] == 0 && pixels[3] == 0, "Copied multisample state retains primitive discard.");
        Draw(blended); Check(pixels[0] == .25f, "Ordinary blending reads fragment alpha."); Draw(blendedOne); Check(pixels[0] == 1, "Blending consumes alpha replaced by alpha-to-one.");
        Draw(one); Check(pixels[0] == 1 && pixels[3] == 1, "Alpha-to-one also executes independently of alpha-to-coverage.");
        Draw(coverage); var alphaCoverage = pixels[0]; Check(alphaCoverage > 0 && alphaCoverage < 1 && Math.Abs(pixels[3] - .25f * alphaCoverage) < .0001f, "Alpha-to-coverage precedes alpha replacement.");
        Draw(oneCoverage); Check(pixels[0] == alphaCoverage && pixels[3] == alphaCoverage, "Alpha-to-one preserves alpha-derived coverage and replaces surviving alpha.");
        for (var i = 0; i < 16; i++) Draw(pipelines[2].Pipeline); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Draw(pipelines[2].Pipeline); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared mask/resolve/readback cycles allocate zero owner bytes.");
        SamplesFrequency(device, language, images, raster, blend, authoring); SamplesEmpty(device, language, authoring); Draw(one);
        foreach (var entry in pipelines) device.FreeRID(entry.Pipeline); foreach (var rid in new[] { one, oneCoverage, coverage, discarded, blended, blendedOne, images.Target, images.Source, shader }) device.FreeRID(rid);
        using (var disposed = new RDPipelineMultisampleState()) { disposed.Dispose(); Reject<ObjectDisposedException>(() => disposed.EnableSampleShading = true); Reject<ObjectDisposedException>(() => _ = disposed.SampleMasks); }
        Console.WriteLine("PASS " + language + ": copied masks, exact sample coverage, alpha ordering and 32 zero-owner-byte cycles."); return images.Resolve;
    }
    private static void SamplesFrequency(RenderingDevice device, string language, (RID Source, RID Resolve, RID Target) images, RDPipelineRasterizationState raster, RDPipelineColorBlendState blend, bool authoring)
    {
        var shader = SampleShader(device, "sample-frequency", language, authoring); var buffer = device.StorageBufferCreate(48); var set = Set(device, shader, buffer); uint[] result = new uint[12];
        using var state = new RDPipelineMultisampleState { SampleCount = TextureSamples.Samples4, EnableSampleShading = true }; var format = device.FramebufferGetFormat(images.Target); var pipelines = new List<(RID Pipeline, uint Minimum)>();
        foreach (var fraction in new[] { 0f, .25f, .5f, 1f }) { state.MinSampleShading = fraction; pipelines.Add((device.RenderPipelineCreate(shader, format, -1, RenderPrimitive.Triangles, raster, blend, multisampleState: state), (uint)Math.Max(1, Math.Ceiling(4 * fraction)) * 16)); }
        state.EnableSampleShading = false; state.MinSampleShading = 1; var ordinary = device.RenderPipelineCreate(shader, format, -1, RenderPrimitive.Triangles, raster, blend, multisampleState: state);
        void Draw(RID pipeline)
        {
            device.BufferClear(buffer, 0, 48); var list = device.DrawListBegin(images.Target); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 0); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); device.BufferGetData(buffer, MemoryMarshal.AsBytes(result.AsSpan()));
        }
        foreach (var (pipeline, minimum) in pipelines) { Draw(pipeline); Check(result[0] >= minimum && result[0] <= 64, "Sample shading supplies its minimum distinct invocations."); }
        Draw(pipelines[^1].Pipeline); Check(result[0] == 64, "Full sample shading executes one fragment invocation per sample."); Draw(ordinary); Check(result[0] >= 16 && result[0] <= 64, "Disabled sample shading ignores its copied minimum.");
        for (var i = 0; i < 16; i++) Draw(pipelines[^1].Pipeline); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Draw(pipelines[^1].Pipeline); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared per-sample storage draw/readback cycles allocate zero owner bytes.");
        var compute = Shader(device, "chain", language, authoring); var kernel = device.ComputePipelineCreate(compute); var computeSet = Set(device, compute, buffer); Draw(pipelines[^1].Pipeline); var cl = device.ComputeListBegin(); device.ComputeListBindComputePipeline(cl, kernel); device.ComputeListBindUniformSet(cl, computeSet, 0); device.ComputeListDispatch(cl, 1, 1, 1); device.ComputeListEnd(); device.BufferGetData(buffer, MemoryMarshal.AsBytes(result.AsSpan())); Check(result[2] == 64, "Per-sample writes reach following compute without CPU producer replay.");
        foreach (var entry in pipelines) device.FreeRID(entry.Pipeline); foreach (var rid in new[] { ordinary, shader, set, compute, kernel, computeSet }) device.FreeRID(rid);
        foreach (var name in new[] { "sample-id", "sample-interpolation" })
        {
            shader = SampleShader(device, name, language, authoring); set = Set(device, shader, buffer); var pipeline = device.RenderPipelineCreate(shader, format, -1, RenderPrimitive.Triangles, raster, blend, multisampleState: state);
            Draw(pipeline); if (name == "sample-id") Check(result.Take(4).All(v => v == 16), "Sample index/position inputs force full per-sample execution even with the state disabled."); else Check(result[0] == 64, "Sample interpolation independently executes native sample frequency.");
            device.FreeRID(pipeline); device.FreeRID(shader); device.FreeRID(set);
        }
        device.FreeRID(buffer); Console.WriteLine("PASS " + language + ": minimum/full frequency, sample index/position/interpolation, compute ordering and zero-owner-byte cycles.");
    }
    private static void SamplesEmpty(RenderingDevice device, string language, bool authoring)
    {
        var shader = SampleShader(device, "sample-empty", language, authoring); var target = device.FramebufferCreateEmpty(new(4, 4), TextureSamples.Samples4); var buffer = device.StorageBufferCreate(4); var set = Set(device, shader, buffer);
        using var raster = new RDPipelineRasterizationState(); using var blend = new RDPipelineColorBlendState(); using var samples = new RDPipelineMultisampleState { SampleCount = TextureSamples.Samples4, EnableSampleShading = true, MinSampleShading = 1 };
        var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend, multisampleState: samples); uint[] count = [0];
        void Record() { device.BufferClear(buffer, 0, 4); var dl = device.DrawListBegin(target); device.DrawListBindRenderPipeline(dl, pipeline); device.DrawListBindUniformSet(dl, set, 0); device.DrawListDraw(dl, false, 1, 3); device.DrawListEnd(); }
        void Draw() { Record(); device.BufferGetData(buffer, MemoryMarshal.AsBytes(count.AsSpan())); }
        Draw(); Check(count[0] == 64, "Attachmentless per-sample fragment producers execute at the exact native sample count.");
        for (var i = 0; i < 16; i++) Draw(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Draw(); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared attachmentless sample producers allocate zero owner bytes.");
        Record(); device.FreeRID(target); device.FreeRID(pipeline); device.BufferGetData(buffer, MemoryMarshal.AsBytes(count.AsSpan())); Check(count[0] == 64, "Pending attachmentless target/pipeline retirement preserves recorded sample work."); foreach (var rid in new[] { shader, set, buffer }) device.FreeRID(rid);
    }
    private static void SamplesViews(RenderingDevice device)
    {
        var shader = SampleShader(device, "sample", "glsl"); var images = SampleTarget(device, 2); device.FreeRID(images.Target);
        using var pass = new RDFramebufferPass { ColorAttachments = [0], ResolveAttachments = [1] }; var target = device.FramebufferCreateMultipass([images.Source, images.Resolve], [pass, pass], viewCount: 2);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; using var samples = new RDPipelineMultisampleState { SampleCount = TextureSamples.Samples4, SampleMasks = [1, 0, 0, 0] };
        var p0 = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend, multisampleState: samples); samples.SampleMasks = [3, 0, 0, 0]; var p1 = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend, multisampleState: samples, forRenderPass: 1); samples.SampleMasks = [0, 0, 0, 0];
        var list = device.DrawListBegin(target, DrawFlags.ClearColorAll, [new(0, 0, 0, 0), new(0, 0, 0, 0)]); device.DrawListBindRenderPipeline(list, p0); device.DrawListDraw(list, false, 1, 3); device.DrawListSwitchToNextPass(); device.DrawListBindRenderPipeline(list, p1); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
        float[] pixels = new float[64]; for (uint layer = 0; layer < 2; layer++) { device.TextureGetData(images.Resolve, layer, MemoryMarshal.AsBytes(pixels.AsSpan())); Check(pixels[0] == .5f, "Every view/pass retains the copied mask and automatic resolve."); }
        foreach (var rid in new[] { shader, p0, p1, target, images.Source, images.Resolve }) device.FreeRID(rid); Console.WriteLine("PASS sample masks across two views/passes and automatic resolves.");
    }
    private static void SamplesSwizzle(RenderingDevice device)
    {
        byte[] Read(string name) => File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", name)); using var code = new RDShaderSPIRV { BytecodeVertex = Read("wide.vert.glsl.spv"), BytecodeFragment = Read("sample-swizzle.frag.glsl.spv") }; var shader = device.ShaderCreateFromSPIRV(code); var images = SampleTarget(device);
        using var format = new RDTextureFormat { Width = 1, Height = 1, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.Sampling }; using var view = new RDTextureView { SwizzleR = TextureSwizzle.G }; float[] color = [.2f, .8f, .4f, .25f]; var source = device.TextureCreate(format, view, [MemoryMarshal.AsBytes(color.AsSpan()).ToArray()]);
        using var samplerState = new RDSamplerState(); var sampler = device.SamplerCreate(samplerState); using var uniform = new RDUniform { UniformType = UniformType.SamplerWithTexture, Binding = 0 }; uniform.AddID(sampler); uniform.AddID(source); var set = device.UniformSetCreate([uniform], shader, 0);
        using var raster = new RDPipelineRasterizationState { LineWidth = 4 }; using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; using var samples = new RDPipelineMultisampleState { SampleCount = TextureSamples.Samples4, SampleMasks = [1, 0, 0, 0], EnableAlphaToOne = true, EnableSampleShading = true, MinSampleShading = 1 };
        var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(images.Target), -1, RenderPrimitive.Lines, raster, blend, multisampleState: samples); float[] pixels = new float[64]; Color[] clear = [new(0, 0, 0, 0)];
        void Draw()
        {
            var list = device.DrawListBegin(images.Target, DrawFlags.ClearColorAll, clear); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 0); device.DrawListDraw(list, false, 1, 2); device.DrawListEnd(); device.TextureResolveMultisample(images.Source, images.Resolve); device.TextureGetData(images.Resolve, 0, MemoryMarshal.AsBytes(pixels.AsSpan()));
        }
        Draw(); var rows = 0; for (var y = 0; y < 4; y++) if (Math.Abs(pixels[(y * 4 + 2) * 4] - .2f) < .0001f && pixels[(y * 4 + 2) * 4 + 3] == .25f) rows++; Check(rows == 4, "Sampled variants preserve mask, alpha-to-one, sample shading and component maps together.");
        for (var i = 0; i < 16; i++) Draw(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Draw(); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared multisample component variants allocate zero owner bytes.");
        foreach (var rid in new[] { pipeline, set, sampler, source, images.Target, images.Source, images.Resolve, shader }) device.FreeRID(rid); Console.WriteLine("PASS copied multisample state through sampled variants and zero-owner-byte cycles.");
    }
    private static void SamplesRefusal(RenderingDevice device, string mode)
    {
        var missingShading = !mode.EndsWith("alpha", StringComparison.Ordinal); var missingAlpha = !mode.EndsWith("shading", StringComparison.Ordinal);
        var images = SampleTarget(device); using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; using var state = new RDPipelineMultisampleState { SampleCount = TextureSamples.Samples4, SampleMasks = [1, 0, 0, 0] };
        foreach (var language in new[] { "glsl", "hlsl" })
        {
            var shader = SampleShader(device, "sample", language); var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(images.Target), -1, RenderPrimitive.Triangles, raster, blend, multisampleState: state);
            state.EnableSampleShading = true; state.MinSampleShading = 1;
            if (missingShading) Reject<NotSupportedException>(() => device.RenderPipelineCreate(shader, device.FramebufferGetFormat(images.Target), -1, RenderPrimitive.Triangles, raster, blend, multisampleState: state));
            else { var enabled = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(images.Target), -1, RenderPrimitive.Triangles, raster, blend, multisampleState: state); device.FreeRID(enabled); }
            state.EnableSampleShading = false; state.EnableAlphaToOne = true;
            if (missingAlpha) Reject<NotSupportedException>(() => device.RenderPipelineCreate(shader, device.FramebufferGetFormat(images.Target), -1, RenderPrimitive.Triangles, raster, blend, multisampleState: state));
            else { var enabled = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(images.Target), -1, RenderPrimitive.Triangles, raster, blend, multisampleState: state); device.FreeRID(enabled); }
            state.EnableAlphaToOne = false;
            foreach (var name in new[] { "sample-id", "sample-interpolation" })
            {
                if (missingShading)
                {
                    using var code = SampleCode(name, language);
                    Reject<NotSupportedException>(() => device.ShaderCreateFromSPIRV(code)); Reject<NotSupportedException>(() => device.ShaderCompileBinaryFromSPIRV(code));
                    var output = Environment.GetEnvironmentVariable("ELECTRON2D_SAMPLE_OUTPUT") ?? throw new InvalidOperationException("Prepare sample shader binaries with ELECTRON2D_SAMPLE_OUTPUT on a capable device first.");
                    var binary = File.ReadAllBytes(System.IO.Path.Combine(output, name + "." + language + ".bin")); var placeholder = device.ShaderCreatePlaceholder(); Reject<NotSupportedException>(() => device.ShaderCreateFromBytecode(binary, placeholder));
                    using var plain = SampleCode("sample", language); Check(device.ShaderCreateFromBytecode(device.ShaderCompileBinaryFromSPIRV(plain), placeholder) == placeholder, "Refused sample shader leaves its placeholder reusable."); device.FreeRID(placeholder);
                    var words = MemoryMarshal.Cast<byte, uint>(code.BytecodeFragment.AsSpan()); var without = new List<uint>(words[..5].ToArray());
                    for (var at = 5; at < words.Length; at += (int)(words[at] >> 16)) { var count = (int)(words[at] >> 16); if ((words[at] & 65535) != 17 || words[at + 1] != 35) without.AddRange(words.Slice(at, count).ToArray()); }
                    code.BytecodeFragment = MemoryMarshal.AsBytes(without.ToArray().AsSpan()).ToArray(); Reject<ArgumentException>(() => device.ShaderCreateFromSPIRV(code));
                }
                else device.FreeRID(SampleShader(device, name, language));
            }
            float[] pixels = new float[64]; var dl = device.DrawListBegin(images.Target, DrawFlags.ClearColorAll, [new(0, 0, 0, 0)]); device.DrawListBindRenderPipeline(dl, pipeline); device.DrawListDraw(dl, false, 1, 3); device.DrawListEnd(); device.TextureResolveMultisample(images.Source, images.Resolve); device.TextureGetData(images.Resolve, 0, MemoryMarshal.AsBytes(pixels.AsSpan())); Check(pixels[0] == .25f, "Masks retain executing coverage when optional sample features are unavailable.");
            device.FreeRID(pipeline); device.FreeRID(shader);
        }
        if (!missingShading) SamplesFrequency(device, "glsl", images, raster, blend, false);
        foreach (var rid in new[] { images.Target, images.Source, images.Resolve }) device.FreeRID(rid); device.FreeRID(Execute(device, "glsl")); device.FreeRID(ExecuteWide(device, "glsl", "wide")); Console.WriteLine("PASS independent sample-feature refusal preserves mask, storage and wide-line siblings.");
    }
    private static void SamplesFailure(RenderingDevice device, string mode)
    {
        var images = SampleTarget(device); var shader = SampleShader(device, "sample", "glsl"); using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; using var state = new RDPipelineMultisampleState { SampleCount = TextureSamples.Samples4, SampleMasks = [1, 0, 0, 0] };
        for (var i = 0; i < 3; i++)
            if (mode == "samples-unavailable") Reject<NotSupportedException>(() => device.RenderPipelineCreate(shader, device.FramebufferGetFormat(images.Target), -1, RenderPrimitive.Triangles, raster, blend, multisampleState: state));
            else Reject<InvalidOperationException>(() => device.RenderPipelineCreate(shader, device.FramebufferGetFormat(images.Target), -1, RenderPrimitive.Triangles, raster, blend, multisampleState: state));
        foreach (var rid in new[] { images.Target, images.Source, images.Resolve, shader }) device.FreeRID(rid); device.FreeRID(Execute(device, "glsl")); Console.WriteLine("PASS multisample adapter refusal/native creation failure preserves ordinary graphics.");
    }
    private static void SamplesCanvas()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); using var root = new Window { Size = new(16, 16) }; using var texture = new TextureRD(); var frames = 0;
        root.Ready += _ => { texture.TextureRDRID = SamplesCoverage(RenderingServer.GetRenderingDevice()!, "glsl", false); root.AddChild(new Patch(texture)); RenderingServer.FramePostDraw += Captured; };
        void Captured()
        {
            if (++frames < 3) return; RenderingServer.FramePostDraw -= Captured; using var image = root.GetTexture().GetImage()!; var pixel = image.GetPixel(8, 8); Check(pixel.R > .99f && Math.Abs(pixel.G - .5f) < .01f && Math.Abs(pixel.B - .25f) < .01f, "Resolved alpha-to-one output reaches retained canvas."); root.Tree!.Quit();
        }
        Engine.Run(root); Console.WriteLine("PASS multisample coverage/frequency output reaches retained canvas.");
    }
}
