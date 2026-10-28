using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class GraphicsStorage
{
    private static void Views(string mode)
    {
        if (mode == "views-canvas") { ViewCanvas(); ViewCanvas(); return; }
        using var device = RenderingServer.CreateLocalRenderingDevice(); Console.WriteLine("Texture-view device: " + device.GetDeviceName()); if (mode is "views-unavailable" or "views-failure") { ViewRefusal(device, mode); return; }
        ViewTransfers(device); ViewFormats(device); ViewTargets(device); foreach (var language in new[] { "glsl", "hlsl" }) { ViewCompute(device, language, mode == "views-source"); ViewGraphics(device, language, mode == "views-source"); }
    }
    private static void ViewTransfers(RenderingDevice device)
    {
        using var format = new RDTextureFormat { Width = 8, Height = 8, MIPMaps = 3, ArrayLayers = 3, TextureType = TextureType.TextureArray, Format = DataFormat.R8G8B8A8UNORM, UsageBits = TextureUsageBits.Sampling | TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom | TextureUsageBits.CanCopyTo | TextureUsageBits.CanUpdate };
        using var view = new RDTextureView(); byte[][] data = new byte[3][];
        for (var layer = 0; layer < 3; layer++) { data[layer] = new byte[336]; data[layer].AsSpan(0, 256).Fill((byte)(20 + layer * 40)); data[layer].AsSpan(256, 64).Fill((byte)(30 + layer * 40)); data[layer].AsSpan(320, 16).Fill((byte)(40 + layer * 40)); }
        var root = device.TextureCreate(format, view, data); var full = device.TextureCreateShared(view, root); var slice = device.TextureCreateSharedFromSlice(view, root, 2, 1, 2);
        using (var description = device.TextureGetFormat(slice)) Check(description.Width == 4 && description.Height == 4 && description.ArrayLayers == 1 && description.MIPMaps == 2 && description.TextureType == TextureType.Texture && description.Format == DataFormat.R8G8B8A8UNORM, "Slice descriptions retain selected dimensions and stored format.");
        byte[] bytes = new byte[80]; device.TextureGetData(slice, 0, bytes); Check(bytes.AsSpan(0, 64).IndexOfAnyExcept((byte)110) < 0 && bytes.AsSpan(64).IndexOfAnyExcept((byte)120) < 0, "A slice reads the original layer and complete selected mip interval.");
        bytes.AsSpan().Fill(51); device.TextureUpdate(slice, 0, bytes); var original = device.TextureGetData(root, 2); Check(original.AsSpan(0, 256).IndexOfAnyExcept((byte)100) < 0 && original.AsSpan(256).IndexOfAnyExcept((byte)51) < 0, "Alias uploads change only their original mip/layer range.");
        byte[] before = new byte[80], after = new byte[80]; var callbacks = 0;
        device.TextureGetDataAsync(slice, 0, before.AsMemory(), () => callbacks++); device.TextureClear(slice, new(.4f, .4f, .4f, .4f), 0, 2, 0, 1); device.TextureGetDataAsync(slice, 0, after.AsMemory(), () => callbacks++); device.Submit(); device.Sync();
        Check(callbacks == 2 && before.AsSpan().IndexOfAnyExcept((byte)51) < 0 && after.AsSpan().IndexOfAnyExcept((byte)102) < 0, "Queued view readbacks snapshot ordered native mutations.");
        var one = device.TextureCreateSharedFromSlice(view, root, 2, 1); var twin = device.TextureCreateShared(view, one);
        using (var description = device.TextureGetFormat(twin)) Check(description.Width == 8 && description.ArrayLayers == 3 && description.MIPMaps == 3, "Creating a shared view from a view resolves the original owner.");
        var peer = device.TextureCreateSharedFromSlice(view, one, 2, 1); byte[] pattern = new byte[64]; for (var i = 0; i < 16; i++) pattern.AsSpan(i * 4, 4).Fill((byte)(i + 1)); device.TextureUpdate(one, 0, pattern);
        Reject<ArgumentException>(() => device.TextureCopy(one, peer, new(0, 0, 0), new(1, 0, 0), new(2, 4, 1), 0, 0, 0, 0));
        device.TextureCopy(one, peer, new(0, 0, 0), new(2, 0, 0), new(2, 4, 1), 0, 0, 0, 0); device.TextureGetData(peer, 0, pattern); for (var i = 0; i < 16; i++) Check(pattern[i * 4] == i / 4 * 4 + i % 2 + 1, "Disjoint self copies through distinct views retain source bytes.");
        Reject<ArgumentOutOfRangeException>(() => device.TextureCreateSharedFromSlice(view, root, uint.MaxValue, 0)); Reject<ArgumentOutOfRangeException>(() => device.TextureCreateSharedFromSlice(view, root, 0, 2, uint.MaxValue)); Reject<ArgumentOutOfRangeException>(() => device.TextureCreateSharedFromSlice(view, root, 0, 0, 0)); Reject<ArgumentOutOfRangeException>(() => device.TextureCreateSharedFromSlice(view, root, 0, 0, sliceType: (TextureSliceType)1));
        device.FreeRID(one); Check(device.TextureIsValid(peer) && device.TextureIsValid(twin), "Sibling views retain the root lifetime when an intermediate view is freed.");
        for (var i = 0; i < 16; i++) device.TextureGetData(slice, 0, bytes); var allocated = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) device.TextureGetData(slice, 0, bytes); Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "Prepared view span readback allocates zero owner bytes.");
        var framebuffer = device.FramebufferCreate([peer]); device.FreeRID(root); foreach (var rid in new[] { full, slice, peer, twin }) Check(!device.TextureIsValid(rid), "Freeing the original owner automatically frees every shared view."); Check(!device.FramebufferIsValid(framebuffer), "Owner retirement invalidates framebuffers borrowing a shared view."); device.FreeRID(framebuffer);
        Console.WriteLine("PASS view transfers: exact original mip/layer bytes, uploads, queued snapshots, clears, physical self-copy guards, root-relative factories, automatic lifetime and zero owner bytes.");
    }
    private static void ViewFormats(RenderingDevice device)
    {
        var pairs = new (DataFormat Stored, DataFormat Viewed, int Bytes)[]
        {
            (DataFormat.R8UNORM, DataFormat.R8UINT, 1), (DataFormat.R8G8UNORM, DataFormat.R8G8SINT, 2),
            (DataFormat.R8G8B8A8UNORM, DataFormat.R8G8B8A8SRGB, 4), (DataFormat.R8G8B8A8UNORM, DataFormat.B8G8R8A8UNORM, 4),
            (DataFormat.R16UNORM, DataFormat.R16SFLOAT, 2), (DataFormat.R16G16SFLOAT, DataFormat.R16G16UINT, 4),
            (DataFormat.R32SFLOAT, DataFormat.R32UINT, 4), (DataFormat.R32G32SFLOAT, DataFormat.R32G32SINT, 8),
            (DataFormat.R32G32B32A32SFLOAT, DataFormat.R32G32B32A32UINT, 16),
            (DataFormat.BC1RGBAUNORMBLOCK, DataFormat.BC1RGBASRGBBLOCK, 0), (DataFormat.BC7UNORMBLOCK, DataFormat.BC7SRGBBLOCK, 0)
        };
        const TextureUsageBits usage = TextureUsageBits.Sampling | TextureUsageBits.CanCopyFrom | TextureUsageBits.CanCopyTo | TextureUsageBits.CanUpdate;
        var checkedPairs = 0;
        foreach (var (stored, viewed, bytes) in pairs)
        {
            if (!device.TextureIsFormatSupportedForUsage(stored, usage) || !device.TextureIsFormatSupportedForUsage(viewed, usage)) continue;
            using var format = new RDTextureFormat { Width = 8, Height = 8, Format = stored, UsageBits = usage }; format.AddShareableFormat(stored); format.AddShareableFormat(viewed);
            using var initial = new RDTextureView { FormatOverride = viewed }; using var plain = new RDTextureView(); var root = device.TextureCreate(format, initial); var alias = device.TextureCreateShared(plain, root);
            byte[] data = device.TextureGetData(root, 0); Check(data.Length == (bytes == 0 ? stored == DataFormat.BC1RGBAUNORMBLOCK ? 32 : 64 : 64 * bytes), "Reinterpreted formats retain their native block byte size."); for (var i = 0; i < data.Length; i++) data[i] = (byte)(i * 13 + 7); device.TextureUpdate(alias, 0, data); Check(device.TextureGetData(root, 0).AsSpan().SequenceEqual(data), "Initial format overrides and default shared views read the same encoded native storage.");
            using var snapshot = device.TextureGetFormat(root); Check(snapshot.Format == stored, "Texture descriptions retain allocation format and a copied shareable list.");
            format.RemoveShareableFormat(viewed); Reject<ArgumentException>(() => device.TextureCreate(format, initial)); var copied = device.TextureCreate(snapshot, initial); device.FreeRID(copied); initial.FormatOverride = DataFormat.Max; Check(device.TextureGetData(alias, 0).AsSpan().SequenceEqual(data), "Mutating source descriptions cannot change a published native view."); device.FreeRID(root); checkedPairs++;
        }
        Check(checkedPairs >= 9, "This native profile executes normalized/integer/float, BGRA, SRGB, 8/16/32/64/128-bit and compressed format views.");
        using var invalid = new RDTextureFormat { Width = 4, Height = 4, Format = DataFormat.R8G8B8A8UNORM, UsageBits = usage }; using var view = new RDTextureView(); invalid.AddShareableFormat(DataFormat.R8G8B8A8UINT); Reject<ArgumentException>(() => device.TextureCreate(invalid, view)); invalid.AddShareableFormat(invalid.Format); invalid.AddShareableFormat(DataFormat.R8UNORM); Reject<NotSupportedException>(() => device.TextureCreate(invalid, view)); invalid.RemoveShareableFormat(DataFormat.R8UNORM); var owner = device.TextureCreate(invalid, view); view.FormatOverride = DataFormat.R32UINT; Reject<ArgumentException>(() => device.TextureCreateShared(view, owner)); view.Dispose(); Reject<ObjectDisposedException>(() => device.TextureCreateShared(view, owner)); device.FreeRID(owner); using var live = new RDTextureView(); Reject<ArgumentException>(() => device.TextureCreateShared(live, owner)); Reject<ArgumentNullException>(() => device.TextureCreateShared(null!, owner));
        Console.WriteLine("PASS native format views: " + checkedPairs + " format pairs, initial overrides, copied stored metadata, incompatible/undeclared formats and disposed identities.");
    }
    private static void ViewTargets(RenderingDevice device)
    {
        using var view = new RDTextureView();
        using var format = new RDTextureFormat { Width = 4, Height = 4, TextureType = TextureType.TextureArray, ArrayLayers = 2, Samples = TextureSamples.Samples4, Format = DataFormat.R8G8B8A8UNORM, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom };
        var multisample = device.TextureCreate(format, view); var source = device.TextureCreateSharedFromSlice(view, multisample, 1, 0); var target = device.FramebufferCreate([source]); var list = device.DrawListBegin(target, DrawFlags.ClearColorAll, [new(.2f, .4f, .8f, 1)]); device.DrawListEnd();
        format.Samples = TextureSamples.Samples1; format.IsResolveBuffer = true; format.UsageBits |= TextureUsageBits.CanCopyTo; var resolved = device.TextureCreate(format, view); var destination = device.TextureCreateSharedFromSlice(view, resolved, 1, 0); device.TextureResolveMultisample(source, destination); var data = device.TextureGetData(resolved, 1); Check(data[0] == 51 && data[1] == 102 && data[2] == 204 && data[3] == 255 && device.TextureGetData(resolved, 0).AsSpan().IndexOfAnyExcept((byte)0) < 0, "Shared multisample targets resolve into the selected original layer and preserve sibling data."); device.FreeRID(target); device.FreeRID(multisample); device.FreeRID(resolved);
        format.IsResolveBuffer = false; format.Width = format.Height = 8; format.MIPMaps = 2; format.Format = DataFormat.D32SFLOATS8UINT; format.UsageBits = TextureUsageBits.DepthStencilAttachment | TextureUsageBits.Sampling; var depthOwner = device.TextureCreate(format, view); var depth = device.TextureCreateSharedFromSlice(view, depthOwner, 1, 1);
        format.Width = format.Height = 4; format.MIPMaps = 1; format.Format = DataFormat.R8G8B8A8UNORM; format.TextureType = TextureType.Texture; format.ArrayLayers = 1; format.UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom; var color = device.TextureCreate(format, view); target = device.FramebufferCreate([color, depth]);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; using var state = new RDPipelineDepthStencilState { EnableDepthTest = true, DepthCompareOperator = CompareOperator.Less }; byte[] Read(string name) => File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", name)); using var code = new RDShaderSPIRV { BytecodeVertex = Read("read.vert.glsl.spv"), BytecodeFragment = Read("depth.frag.glsl.spv") }; var shader = device.ShaderCreateFromSPIRV(code); var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend, depthStencilState: state); float[] parameters = [.2f, .4f, .8f, 1, 0, 0, 0, 0];
        foreach (var clear in new[] { 1f, 0f }) { list = device.DrawListBegin(target, DrawFlags.ClearAll, [default], clearDepthValue: clear); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(parameters.AsSpan()), 32); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); data = device.TextureGetData(color, 0); Check(data[0] == (clear == 1 ? 51 : 0), "Selected depth/stencil mip/layer views participate in real native depth rejection."); }
        device.FreeRID(pipeline); device.FreeRID(shader); device.FreeRID(target); device.FreeRID(depthOwner);
        format.TextureType = TextureType.TextureArray; format.ArrayLayers = 2; format.UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.InputAttachment | TextureUsageBits.CanCopyFrom; var inputOwner = device.TextureCreate(format, view); var input = device.TextureCreateSharedFromSlice(view, inputOwner, 1, 0);
        using var first = new RDFramebufferPass { ColorAttachments = [0] }; using var next = new RDFramebufferPass { ColorAttachments = [1], InputAttachments = [0] }; target = device.FramebufferCreateMultipass([input, color], [first, next]);
        foreach (var language in new[] { "glsl", "hlsl" })
        {
            using var inputCode = new RDShaderSPIRV { BytecodeVertex = Read("read.vert." + language + ".spv"), BytecodeFragment = Read("views-input.frag." + language + ".spv") }; shader = device.ShaderCreateFromSPIRV(inputCode); pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend, forRenderPass: 1); using var uniform = ViewUniform(UniformType.InputAttachment, 7, input); var set = device.UniformSetCreate([uniform], shader, 6);
            list = device.DrawListBegin(target, DrawFlags.ClearColorAll, [new(.2f, .4f, .8f, 1), default]); device.DrawListSwitchToNextPass(); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 6); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); data = device.TextureGetData(color, 0); Check(data[0] == 204 && data[1] == 102 && data[2] == 51 && data[3] == 255, "Shared array slices without Sampling reach input-attachment snapshots in both languages."); device.FreeRID(set); device.FreeRID(pipeline); device.FreeRID(shader);
        }
        foreach (var rid in new[] { target, color, inputOwner }) device.FreeRID(rid); Console.WriteLine("PASS shared target views: multisample layer resolve, actual depth/stencil rejection and GLSL/HLSL input snapshots without sampled source usage.");
    }
    private static RID ViewShader(RenderingDevice device, string language, bool graphics, bool source)
    {
        string Path(string file) => System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", file + "." + language);
        if (source)
        {
            using var text = new RDShaderSource { Language = language == "glsl" ? ShaderLanguage.GLSL : ShaderLanguage.HLSL, SourceCompute = graphics ? "" : File.ReadAllText(Path("views.comp")), SourceVertex = graphics ? File.ReadAllText(Path("read.vert")) : "", SourceFragment = graphics ? File.ReadAllText(Path("views.frag")) : "" }; using var code = device.ShaderCompileSPIRVFromSource(text); Check(code.CompileErrorCompute == "" && code.CompileErrorVertex == "" && code.CompileErrorFragment == "", code.CompileErrorCompute + code.CompileErrorVertex + code.CompileErrorFragment); return device.ShaderCreateFromSPIRV(code);
        }
        using var bytes = new RDShaderSPIRV { BytecodeCompute = graphics ? [] : File.ReadAllBytes(Path("views.comp") + ".spv"), BytecodeVertex = graphics ? File.ReadAllBytes(Path("read.vert") + ".spv") : [], BytecodeFragment = graphics ? File.ReadAllBytes(Path("views.frag") + ".spv") : [] }; var placeholder = device.ShaderCreatePlaceholder(); Check(device.ShaderCreateFromBytecode(device.ShaderCompileBinaryFromSPIRV(bytes), placeholder) == placeholder, "View programs retain binary placeholder identity."); return placeholder;
    }
    private static RID ViewFloatRoot(RenderingDevice device)
    {
        using var format = new RDTextureFormat { Width = 8, Height = 8, ArrayLayers = 3, MIPMaps = 3, TextureType = TextureType.TextureArray, Format = DataFormat.R32SFLOAT, UsageBits = TextureUsageBits.Storage | TextureUsageBits.Sampling | TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom | TextureUsageBits.CanCopyTo | TextureUsageBits.CanUpdate }; format.AddShareableFormat(DataFormat.R32SFLOAT); format.AddShareableFormat(DataFormat.R32UINT); using var view = new RDTextureView(); float[] first = new float[84]; first.AsSpan(0, 64).Fill(.25f); return device.TextureCreate(format, view, [MemoryMarshal.AsBytes(first.AsSpan()).ToArray(), new byte[336], new byte[336]]);
    }
    private static RID ViewColorRoot(RenderingDevice device)
    {
        using var format = new RDTextureFormat { Width = 8, Height = 8, ArrayLayers = 3, MIPMaps = 3, TextureType = TextureType.TextureArray, Format = DataFormat.R8G8B8A8UNORM, UsageBits = TextureUsageBits.Sampling | TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom | TextureUsageBits.CanCopyTo }; using var view = new RDTextureView(); byte[][] layers = [new byte[336], new byte[336], new byte[336]]; int[] starts = [0, 256, 320], lengths = [256, 64, 16]; for (var layer = 0; layer < 3; layer++) for (var mip = 0; mip < 3; mip++) for (var i = starts[mip]; i < starts[mip] + lengths[mip]; i += 4) { var selected = layer == 2 && mip == 1; layers[layer][i] = selected ? (byte)51 : (byte)(15 + layer * 20 + mip * 7); layers[layer][i + 1] = selected ? (byte)102 : (byte)(25 + layer * 10 + mip * 11); layers[layer][i + 2] = selected ? (byte)204 : (byte)(35 + layer * 15 + mip * 13); layers[layer][i + 3] = 255; }
        return device.TextureCreate(format, view, layers);
    }
    private static RDUniform ViewUniform(UniformType type, int binding, params RID[] ids)
    { var result = new RDUniform { UniformType = type, Binding = binding }; foreach (var rid in ids) result.AddID(rid); return result; }
    private static void ViewCompute(RenderingDevice device, string language, bool source)
    {
        var root = ViewFloatRoot(device); var color = ViewColorRoot(device); using var plain = new RDTextureView(); using var integer = new RDTextureView { FormatOverride = DataFormat.R32UINT }; using var mapped = new RDTextureView { SwizzleR = TextureSwizzle.B, SwizzleB = TextureSwizzle.R };
        var sampled = device.TextureCreateSharedFromSlice(plain, root, 0, 0); var counter = device.TextureCreateSharedFromSlice(integer, root, 2, 1); var layered = device.TextureCreateShared(mapped, color);
        using (var description = device.TextureGetFormat(counter)) Check(description.Format == DataFormat.R32SFLOAT && description.Width == 4, "GetFormat preserves stored format while shader interfaces consume the selected UINT view.");
        using var state = new RDSamplerState(); var sampler = device.SamplerCreate(state); var shader = ViewShader(device, language, false, source); var pipeline = device.ComputePipelineCreate(shader); var output = device.StorageBufferCreate(256);
        using var sampleUniform = ViewUniform(UniformType.SamplerWithTexture, 0, sampler, sampled); using var imageUniform = ViewUniform(UniformType.Image, 1, counter); using var outputUniform = ViewUniform(UniformType.StorageBuffer, 2, output); using var arrayUniform = ViewUniform(UniformType.SamplerWithTexture, 3, sampler, layered); var set = device.UniformSetCreate([sampleUniform, imageUniform, outputUniform, arrayUniform], shader, 0); using var wrong = ViewUniform(UniformType.Image, 1, sampled); Reject<ArgumentException>(() => device.UniformSetCreate([sampleUniform, wrong, outputUniform, arrayUniform], shader, 0)); uint[] results = new uint[64], pixels = new uint[16];
        void Cycle()
        {
            device.TextureClear(counter, default, 0, 1, 0, 1); var list = device.ComputeListBegin();
            try { device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, set, 0); device.ComputeListDispatch(list, 1, 1, 1); }
            finally { device.ComputeListEnd(); }
            device.BufferGetData(output, MemoryMarshal.AsBytes(results.AsSpan())); device.TextureGetData(counter, 0, MemoryMarshal.AsBytes(pixels.AsSpan()));
        }
        Cycle(); for (var i = 0; i < 16; i++) Check(results[i * 4] == BitConverter.SingleToUInt32Bits(.25f) && Math.Abs(BitConverter.UInt32BitsToSingle(results[i * 4 + 1]) - .8f) < .00001f && results[i * 4 + 2] == 0 && results[i * 4 + 3] == 0x03010408 && pixels[i] == 1, "Native view sampling/array maps/query extents compose with typed atomic writes to a disjoint subresource of the same image.");
        for (var i = 0; i < 16; i++) Cycle(); var bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Cycle(); Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "Prepared view sample/atomic/readback cycles allocate zero owner bytes.");
        device.FreeRID(root); Check(!device.UniformSetIsValid(set), "Owner retirement invalidates descriptor sets borrowing native views."); foreach (var rid in new[] { set, pipeline, shader, output, sampler, color }) device.FreeRID(rid);
        Console.WriteLine("PASS " + language + " native views compute: typed atomic reinterpretation, disjoint same-image sampling, array maps, exact extents and zero owner bytes.");
    }
    private static void ViewGraphics(RenderingDevice device, string language, bool source)
    {
        var root = ViewFloatRoot(device); var color = ViewColorRoot(device); using var plain = new RDTextureView(); using var integer = new RDTextureView { FormatOverride = DataFormat.R32UINT };
        var sampled = device.TextureCreateSharedFromSlice(plain, root, 0, 0); var counter = device.TextureCreateSharedFromSlice(integer, root, 2, 1); var duplicate = device.TextureCreateSharedFromSlice(integer, root, 2, 1); var targetImage = device.TextureCreateSharedFromSlice(plain, color, 1, 1); var target = device.FramebufferCreate([targetImage]); var feedback = device.TextureCreateSharedFromSlice(plain, color, 1, 1); var disjoint = device.TextureCreateSharedFromSlice(plain, color, 0, 1);
        Reject<ArgumentException>(() => device.FramebufferCreate([targetImage, feedback]));
        using var state = new RDSamplerState(); var sampler = device.SamplerCreate(state); var shader = ViewShader(device, language, true, source); using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend); var output = device.StorageBufferCreate(64);
        using var sampleUniform = ViewUniform(UniformType.SamplerWithTexture, 0, sampler, sampled); using var first = ViewUniform(UniformType.Image, 1, counter); using var second = ViewUniform(UniformType.Image, 2, duplicate); using var values = ViewUniform(UniformType.StorageBuffer, 3, output); var set = device.UniformSetCreate([sampleUniform, first, second, values], shader, 0);
        using var feedbackSample = ViewUniform(UniformType.SamplerWithTexture, 0, sampler, feedback); using var disjointSample = ViewUniform(UniformType.SamplerWithTexture, 0, sampler, disjoint); var feedbackSet = device.UniformSetCreate([feedbackSample, first, second, values], shader, 0); var disjointSet = device.UniformSetCreate([disjointSample, first, second, values], shader, 0);
        byte[] bytes = new byte[64]; uint[] counters = new uint[16], results = new uint[16];
        void Draw(RID bindings)
        {
            device.TextureClear(counter, default, 0, 1, 0, 1); var list = device.DrawListBegin(target); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, bindings, 0);
            try { device.DrawListEnableScissor(list, new(0, 0, 2, 4)); device.DrawListDraw(list, false, 1, 3); device.DrawListEnableScissor(list, new(2, 0, 2, 4)); device.DrawListDraw(list, false, 1, 3); }
            finally { device.DrawListEnd(); }
            device.TextureGetData(targetImage, 0, bytes); device.TextureGetData(counter, 0, MemoryMarshal.AsBytes(counters.AsSpan())); device.BufferGetData(output, MemoryMarshal.AsBytes(results.AsSpan()));
        }
        Draw(set); for (var i = 0; i < 16; i++) Check(Math.Abs(bytes[i * 4] - 64) <= 1 && bytes[i * 4 + 1] == 255 && Math.Abs(bytes[i * 4 + 2] - 128) <= 1 && bytes[i * 4 + 3] == 255 && counters[i] == 3 && results[i] == 1, "Typed image aliases share atomic order, one physical GENERAL scope and sampled sibling pixels across pass/scissor restarts.");
        Reject<InvalidOperationException>(() => Draw(feedbackSet)); Draw(disjointSet); Check(bytes[0] == 22, "Disjoint source/output layer views of one image execute without false feedback."); Draw(set);
        for (var i = 0; i < 16; i++) Draw(set); var allocated = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Draw(set); Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "Prepared aliased graphics/image/scissor cycles allocate zero owner bytes.");
        foreach (var rid in new[] { set, feedbackSet, disjointSet, pipeline, shader, target, output, sampler, root, color }) device.FreeRID(rid);
        Console.WriteLine("PASS " + language + " native views graphics: shared atomic images, sampled GENERAL siblings, exact color pixels, physical feedback/overlap guards, disjoint layers and zero owner bytes.");
    }

    private static void ViewCanvas()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); using var root = new Window { Size = new(16, 16) }; using var texture = new TextureRD(); var frames = 0; RID owner = default, alias = default, borrowed = default, shared = default;
        root.Ready += _ =>
        {
            var device = RenderingServer.GetRenderingDevice()!; owner = ViewColorRoot(device); using var view = new RDTextureView { SwizzleR = TextureSwizzle.B, SwizzleB = TextureSwizzle.R }; alias = device.TextureCreateSharedFromSlice(view, owner, 2, 1, 2); texture.TextureRDRID = alias;
            using var image = texture.GetImage()!; var raw = image.GetPixel(1, 1); Check(image.Width == 4 && !image.HasMIPMaps && texture.HasMIPMaps && Math.Abs(raw.R - .2f) < .01f && Math.Abs(raw.B - .8f) < .01f, "Canvas view snapshots preserve selected raw mip/layer data before sampled channel mapping.");
            root.AddChild(new Patch(texture)); RenderingServer.FramePostDraw += Captured;
        };
        void Captured()
        {
            if (++frames < 3) return; var device = RenderingServer.GetRenderingDevice()!; if (frames == 4) { RenderingServer.FramePostDraw -= Captured; Reject<ArgumentException>(() => device.FreeRID(shared)); device.FreeRID(borrowed); device.FreeRID(owner); Check(!device.TextureIsValid(alias) && texture.GetWidth() == 0, "Retiring the original owner invalidates both canvas view identity and dimensions."); root.Tree!.Quit(); return; }
            using var image = root.GetTexture().GetImage()!; var pixel = image.GetPixel(8, 8); Check(Math.Abs(pixel.R - .8f) < .01f && Math.Abs(pixel.G - .4f) < .01f && Math.Abs(pixel.B - .2f) < .01f && pixel.A > .99f, "Native slice/full mip view and sampled component mapping reach retained canvas pixels.");
            using var cpu = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8); cpu.Fill(new(.3f, .2f, .1f, 1)); using var authored = ImageTexture.CreateFromImage(cpu); borrowed = RenderingServer.TextureGetRDTexture(authored.GetRID()); using var view = new RDTextureView(); shared = device.TextureCreateShared(view, borrowed); Check(device.TextureIsValid(shared), "Authored renderer sources expose an executing native shared view."); cpu.Fill(new(.6f, .5f, .2f, 1)); authored.Update(cpu); var updated = device.TextureGetData(shared, 0); Check(updated.AsSpan().SequenceEqual(cpu.GetData()), "Authored updates reach the pinned physical image through a shared borrow: " + string.Join(",", updated.AsSpan(0, 4).ToArray()) + " expected " + string.Join(",", cpu.GetData().AsSpan(0, 4).ToArray())); authored.Dispose(); Check(!device.TextureIsValid(shared), "Renderer source retirement immediately expires its shared view.");
        }
        Engine.Run(root); Console.WriteLine("PASS native view canvas: actual selected/mapped pixels, raw snapshots, borrowed source retirement, original-owner invalidation and renderer recreation.");
    }

    private static void ViewRefusal(RenderingDevice device, string mode)
    {
        using var format = new RDTextureFormat { Width = 4, Height = 4, Format = DataFormat.R8G8B8A8UNORM, UsageBits = TextureUsageBits.Sampling | TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom | TextureUsageBits.CanCopyTo }; using var view = new RDTextureView(); var root = device.TextureCreate(format, view); var before = device.GetMemoryUsage(MemoryType.Textures);
        void Refuse(Action action) { if (mode == "views-unavailable") Reject<NotSupportedException>(action); else Reject<InvalidOperationException>(action); }
        for (var i = 0; i < 3; i++) { Refuse(() => device.TextureCreateShared(view, root)); Refuse(() => device.TextureCreateSharedFromSlice(view, root, 0, 0)); }
        Check(device.GetMemoryUsage(MemoryType.Textures) == before, "Failed view creation rolls back source pins without allocating image payload.");
        format.AddShareableFormat(DataFormat.R8G8B8A8UNORM); format.AddShareableFormat(DataFormat.R8G8B8A8UINT); for (var i = 0; i < 3; i++) Refuse(() => device.TextureCreate(format, view)); Check(device.GetMemoryUsage(MemoryType.Textures) == before, "Failed mutable-view setup releases its owned native image and memory.");
        device.TextureClear(root, new(.4f, .4f, .4f, .4f), 0, 1, 0, 1); byte[] bytes = new byte[64]; device.TextureGetData(root, 0, bytes); Check(bytes.AsSpan().IndexOfAnyExcept((byte)102) < 0, "Native view refusal preserves executing ordinary image creation, clear and readback."); device.FreeRID(root);
        Console.WriteLine("PASS " + mode + ": repeated shared/slice/mutable creation refusal rolls back lifetime/memory and ordinary native graphics survives.");
    }

}
