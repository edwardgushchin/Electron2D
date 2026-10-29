using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static class MultisampleCopyTests
{
    private static int _checks;
    private static string? _input, _output;
    private const TextureUsageBits Copies = TextureUsageBits.CanCopyFrom | TextureUsageBits.CanCopyTo;
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); _checks++; }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { _checks++; return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static RID Shader(RenderingDevice device, string name, RDShaderSource source)
    {
        using (source)
        {
            if (_input is not null) return device.ShaderCreateFromBytecode(File.ReadAllBytes(System.IO.Path.Combine(_input, name + ".bin")));
            using var module = device.ShaderCompileSPIRVFromSource(source, false);
            Check(module.CompileErrorVertex + module.CompileErrorFragment + module.CompileErrorCompute == "", name + ": " + module.CompileErrorVertex + module.CompileErrorFragment + module.CompileErrorCompute);
            if (_output is not null) { Directory.CreateDirectory(_output); File.WriteAllBytes(System.IO.Path.Combine(_output, name + ".bin"), device.ShaderCompileBinaryFromSPIRV(module, name)); foreach (var stage in new[] { ShaderStage.Vertex, ShaderStage.Fragment, ShaderStage.Compute }) { var code = module.GetStageBytecode(stage); if (code.Length != 0) File.WriteAllBytes(System.IO.Path.Combine(_output, name + "." + stage + ".spv"), code); } }
            return device.ShaderCreateFromSPIRV(module, name);
        }
    }
    private const string Vertex = "#version 450\nvoid main(){vec2 p[3]=vec2[3](vec2(-1,-1),vec2(3,-1),vec2(-1,3));gl_Position=vec4(p[gl_VertexIndex],.5,1);}";
    private static bool Depth(DataFormat format) => format is DataFormat.D16UNORM or DataFormat.X8D24UNORMPACK32 or DataFormat.D32SFLOAT or DataFormat.D24UNORMS8UINT or DataFormat.D32SFLOATS8UINT;
    private static bool Stencil(DataFormat format) => format is DataFormat.D24UNORMS8UINT or DataFormat.D32SFLOATS8UINT;
    private static RID Image(RenderingDevice device, DataFormat encoding, TextureSamples samples = TextureSamples.Samples4, uint layers = 1, TextureUsageBits copies = Copies)
    {
        using var format = new RDTextureFormat { Width = 4, Height = 4, Format = encoding, TextureType = layers == 1 ? TextureType.Texture : TextureType.TextureArray, ArrayLayers = layers, Samples = samples, UsageBits = TextureUsageBits.Sampling | (Depth(encoding) ? TextureUsageBits.DepthStencilAttachment : TextureUsageBits.ColorAttachment) | copies };
        using var view = new RDTextureView(); return device.TextureCreate(format, view);
    }
    private static void Copy(RenderingDevice device, RID source, RID destination, Vector3 from = default, Vector3 to = default, uint width = 4, uint height = 4, uint fromLayer = 0, uint toLayer = 0) => device.TextureCopy(source, destination, from, to, new(width, height, 1), 0, 0, fromLayer, toLayer);
    internal static void Run(string? binaryDirectory = null)
    {
        _input = binaryDirectory; _output = Environment.GetEnvironmentVariable("ELECTRON2D_MS_COPIES_OUTPUT");
        using var device = RenderingServer.CreateLocalRenderingDevice();
        var redSource = Image(device, DataFormat.R8G8B8A8UNORM); var redTarget = Image(device, DataFormat.R8G8B8A8UNORM); Copy(device, redSource, redTarget);
        foreach (var samples in new[] { TextureSamples.Samples2, TextureSamples.Samples4, TextureSamples.Samples8 })
            foreach (var format in new[] { DataFormat.R8G8B8A8UNORM, DataFormat.R32SFLOAT, DataFormat.R32SINT, DataFormat.R32UINT, DataFormat.D16UNORM, DataFormat.X8D24UNORMPACK32, DataFormat.D32SFLOAT, DataFormat.D24UNORMS8UINT, DataFormat.D32SFLOATS8UINT }) Exercise(device, format, samples);
        Storage(device);
        Reinterpret(device);
        Negatives(device);
        Console.WriteLine($"Multisample copies PASS {_checks}; {(_input is null ? "source/SPIR-V" : "binary")} on {device.GetDeviceName()}");
    }
    private static RID Produce(RenderingDevice device, RID target, DataFormat format, TextureSamples samples)
    {
        var depth = Depth(format); var integer = format is DataFormat.R32SINT or DataFormat.R32UINT;
        var type = format == DataFormat.R32SINT ? "ivec4" : format == DataFormat.R32UINT ? "uvec4" : "vec4";
        var color = depth ? Image(device, DataFormat.R8G8B8A8UNORM, samples) : target;
        var framebuffer = device.FramebufferCreate(depth ? [color, target] : [target]);
        var expression = integer ? "(gl_SampleID+1)*16+int(gl_FragCoord.x)+int(gl_FragCoord.y)*4" : "float(gl_SampleID+1)/16.0+(floor(gl_FragCoord.x)+floor(gl_FragCoord.y)*4.0)/64.0";
        var shader = Shader(device, "producer-" + format, new RDShaderSource { SourceVertex = Vertex, SourceFragment = "#version 450\nlayout(location=0) out " + type + " color;void main(){float d=float(gl_SampleID+1)/16.0+(floor(gl_FragCoord.x)+floor(gl_FragCoord.y)*4.0)/64.0;color=" + type + "(" + expression + ");gl_FragDepth=d;}" });
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
        var count = 1 << (int)samples;
        for (var sample = 0; sample < (Stencil(format) ? count : 1); sample++)
        {
            using var state = new RDPipelineDepthStencilState { EnableDepthTest = depth, EnableDepthWrite = depth, DepthCompareOperator = CompareOperator.Always, EnableStencil = Stencil(format), FrontOpCompare = CompareOperator.Always, BackOpCompare = CompareOperator.Always, FrontOpPass = StencilOperation.Replace, BackOpPass = StencilOperation.Replace, FrontOpWriteMask = 255, BackOpWriteMask = 255, FrontOpReference = 17 + sample, BackOpReference = 17 + sample };
            using var multi = new RDPipelineMultisampleState { SampleCount = samples, SampleMasks = Stencil(format) ? Enumerable.Repeat(1u << sample, count).ToArray() : [] };
            var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(framebuffer), -1, RenderPrimitive.Triangles, raster, blend, depthStencilState: state, multisampleState: multi);
            var list = device.DrawListBegin(framebuffer, sample == 0 ? DrawFlags.ClearAll : DrawFlags.DefaultAll, [Colors.Black]); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
        }
        return framebuffer;
    }
    private sealed class Reader
    {
        private readonly RenderingDevice _device;
        private readonly RID _pipeline, _set, _buffer;
        private readonly byte[] _data;
        internal Reader(RenderingDevice device, RID source, DataFormat format, TextureSamples samples, ShaderLanguage language)
        {
            _device = device; var count = 1 << (int)samples; _data = new byte[4 * 16 * count]; _buffer = device.StorageBufferCreate((uint)_data.Length);
            var type = format == DataFormat.R32SINT ? "i" : format == DataFormat.R32UINT ? "u" : "";
            var scalar = type == "i" ? "int" : type == "u" ? "uint" : "float";
            var shader = Shader(device, "reader-" + type + language, new RDShaderSource { Language = language, SourceCompute = language == ShaderLanguage.GLSL ? "#version 450\nlayout(local_size_x=1) in;layout(set=0,binding=0) uniform " + type + "sampler2DMS image;layout(set=0,binding=1,std430) buffer R{float v[];}r;void main(){ivec2 p=ivec2(gl_GlobalInvocationID.xy);int n=textureSamples(image);for(int s=0;s<n;s++)r.v[(p.y*4+p.x)*n+s]=float(texelFetch(image,p,s).r);}" : "[[vk::binding(0,0)]] Texture2DMS<" + scalar + "> image;[[vk::binding(1,0)]] RWStructuredBuffer<float> r;[numthreads(1,1,1)] void main(uint3 id:SV_DispatchThreadID){uint w,h,n;image.GetDimensions(w,h,n);for(uint s=0;s<n;s++)r[(id.y*4+id.x)*n+s]=float(image.Load(int2(id.xy),s));}" });
            using var samplerState = new RDSamplerState(); var sampler = device.SamplerCreate(samplerState);
            using var image = new RDUniform { Binding = 0, UniformType = language == ShaderLanguage.GLSL ? UniformType.SamplerWithTexture : UniformType.Texture }; if (language == ShaderLanguage.GLSL) image.AddID(sampler); image.AddID(source);
            using var buffer = new RDUniform { Binding = 1, UniformType = UniformType.StorageBuffer }; buffer.AddID(_buffer);
            _set = device.UniformSetCreate([image, buffer], shader, 0); _pipeline = device.ComputePipelineCreate(shader);
        }
        internal void Read()
        {
            var list = _device.ComputeListBegin(); _device.ComputeListBindComputePipeline(list, _pipeline); _device.ComputeListBindUniformSet(list, _set, 0); _device.ComputeListDispatch(list, 4, 4, 1); _device.ComputeListEnd(); _device.BufferGetData(_buffer, _data.AsSpan());
        }
        internal float[] Snapshot() { Read(); return MemoryMarshal.Cast<byte, float>(_data).ToArray(); }
    }
    private static void Exercise(RenderingDevice device, DataFormat format, TextureSamples samples)
    {
        var source = Image(device, format, samples); var target = Image(device, format, samples, 2); Produce(device, source, format, samples);
        var n = 1 << (int)samples; using var view = new RDTextureView(); var slice = device.TextureCreateSharedFromSlice(view, target, 1, 0); var untouched = device.TextureCreateSharedFromSlice(view, target, 0, 0);
        var sourceReader = new Reader(device, source, format, samples, ShaderLanguage.GLSL); var reader = new Reader(device, slice, format, samples, ShaderLanguage.HLSL); var neighbor = new Reader(device, untouched, format, samples, ShaderLanguage.GLSL);
        var expected = sourceReader.Snapshot();
        var numeric = format is DataFormat.R32SINT or DataFormat.R32UINT; var tolerance = format == DataFormat.R8G8B8A8UNORM ? .002f : .00002f;
        Check(Enumerable.Range(0, expected.Length).All(i => MathF.Abs(expected[i] - (numeric ? (i % n + 1) * 16 + i / n : (i % n + 1) / 16f + (i / n) / 64f)) <= tolerance), "Independent pixel/sample producer values: " + format);
        Copy(device, source, slice); Check(reader.Snapshot().AsSpan().SequenceEqual(expected), "All samples survive full shared-slice copy: " + format);
        Check(neighbor.Snapshot().All(x => x == 0), "Copy preserves neighboring native array layer: " + format);
        Copy(device, source, slice, new(0, 0, 0), new(2, 2, 0), 2, 2); var rectangular = expected.ToArray(); for (var y = 0; y < 2; y++) for (var x = 0; x < 2; x++) expected.AsSpan((y * 4 + x) * n, n).CopyTo(rectangular.AsSpan(((y + 2) * 4 + x + 2) * n));
        Check(reader.Snapshot().AsSpan().SequenceEqual(rectangular), "Rectangles preserve every sample and outside texel: " + format);
        Copy(device, source, source, default, new(2, 2, 0), 2, 2); Check(sourceReader.Snapshot().AsSpan().SequenceEqual(rectangular), "Disjoint self-copy retains matching samples: " + format);
        Copy(device, target, target, fromLayer: 1, toLayer: 0); Check(neighbor.Snapshot().AsSpan().SequenceEqual(rectangular), "Cross-layer same-image copy preserves samples: " + format);
        if (Stencil(format)) StencilChecks(device, slice, samples);
        void Execute() { Copy(device, source, source, default, new(2, 2, 0), 2, 2); Copy(device, source, slice); reader.Read(); }
        for (var i = 0; i < 16; i++) Execute(); var bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Execute(); var delta = GC.GetAllocatedBytesForCurrentThread() - bytes; Check(delta == 0, "Allocated " + delta + "; prepared copies and sample reads allocate zero owner managed bytes: " + format);
        Copy(device, source, slice); device.FreeRID(source); Check(reader.Snapshot().AsSpan().SequenceEqual(rectangular), "Recorded copy survives source retirement: " + format);
        if (format == DataFormat.R8G8B8A8UNORM && samples == TextureSamples.Samples4) Resolve(device, slice, rectangular);
        Console.WriteLine(format + " " + samples + ": per-sample, rectangular, self, layers, retirement and zero-owner-byte cycles passed.");
    }
    private static void StencilChecks(RenderingDevice device, RID depth, TextureSamples samples)
    {
        var target = Image(device, DataFormat.R8G8B8A8UNORM, samples); var framebuffer = device.FramebufferCreate([target, depth]);
        var shader = Shader(device, "stencil-check", new RDShaderSource { SourceVertex = Vertex, SourceFragment = "#version 450\nlayout(location=0) out vec4 color;void main(){color=vec4(1);}" });
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
        var n = 1 << (int)samples; var reader = new Reader(device, target, DataFormat.R8G8B8A8UNORM, samples, ShaderLanguage.GLSL);
        for (var s = 0; s < n; s++)
        {
            using var state = new RDPipelineDepthStencilState { EnableStencil = true, FrontOpCompare = CompareOperator.Equal, BackOpCompare = CompareOperator.Equal, FrontOpCompareMask = 255, BackOpCompareMask = 255, FrontOpReference = 17 + s, BackOpReference = 17 + s, FrontOpPass = StencilOperation.Keep, BackOpPass = StencilOperation.Keep, FrontOpFail = StencilOperation.Keep, BackOpFail = StencilOperation.Keep };
            using var multi = new RDPipelineMultisampleState { SampleCount = samples };
            var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(framebuffer), -1, RenderPrimitive.Triangles, raster, blend, depthStencilState: state, multisampleState: multi);
            var list = device.DrawListBegin(framebuffer, DrawFlags.ClearColorAll, [Colors.Black]); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); var actual = reader.Snapshot();
            Check(Enumerable.Range(0, actual.Length).All(i => actual[i] == (i % n == s ? 1 : 0)), "Copied stencil preserves each independent sample reference.");
        }
    }
    private static void Storage(RenderingDevice device)
    {
        using var format = new RDTextureFormat { Width = 4, Height = 4, Format = DataFormat.R32UINT, Samples = TextureSamples.Samples4, UsageBits = TextureUsageBits.Sampling | TextureUsageBits.Storage | Copies };
        using var view = new RDTextureView(); var source = device.TextureCreate(format, view); var target = Image(device, DataFormat.R32UINT);
        var shader = Shader(device, "storage-producer", new RDShaderSource { SourceCompute = "#version 450\nlayout(local_size_x=1) in;layout(set=0,binding=0,r32ui) uniform uimage2DMS image;void main(){ivec2 p=ivec2(gl_GlobalInvocationID.xy);for(int s=0;s<imageSamples(image);s++)imageStore(image,p,s,uvec4((s+1)*16+p.x+p.y*4));}" });
        using var image = new RDUniform { Binding = 0, UniformType = UniformType.Image }; image.AddID(source); var set = device.UniformSetCreate([image], shader, 0); var pipeline = device.ComputePipelineCreate(shader);
        var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, set, 0); device.ComputeListDispatch(list, 4, 4, 1); device.ComputeListEnd(); Copy(device, source, target);
        var reader = new Reader(device, target, DataFormat.R32UINT, TextureSamples.Samples4, ShaderLanguage.GLSL); var values = reader.Snapshot();
        Check(Enumerable.Range(0, values.Length).All(i => values[i] == (i % 4 + 1) * 16 + i / 4), "Native storage-only producer feeds copies with independent samples on the same GPU timeline.");
    }
    private static void Resolve(RenderingDevice device, RID source, float[] expected)
    {
        using var format = new RDTextureFormat { Width = 4, Height = 4, Format = DataFormat.R8G8B8A8UNORM, IsResolveBuffer = true, UsageBits = TextureUsageBits.ColorAttachment | Copies };
        using var view = new RDTextureView(); var target = device.TextureCreate(format, view); device.TextureResolveMultisample(source, target); var pixels = device.TextureGetData(target, 0);
        Check(Enumerable.Range(0, 16).All(p => MathF.Abs(pixels[p * 4] / 255f - (expected[p * 4] + expected[p * 4 + 1] + expected[p * 4 + 2] + expected[p * 4 + 3]) / 4f) <= .004f), "Existing native resolve consumes the copied shared MS image.");
    }
    private static void Reinterpret(RenderingDevice device)
    {
        var source = Image(device, DataFormat.R32UINT); var target = Image(device, DataFormat.R32SINT); Produce(device, source, DataFormat.R32UINT, TextureSamples.Samples4);
        var a = new Reader(device, source, DataFormat.R32UINT, TextureSamples.Samples4, ShaderLanguage.GLSL); var b = new Reader(device, target, DataFormat.R32SINT, TextureSamples.Samples4, ShaderLanguage.HLSL);
        var expected = a.Snapshot(); Copy(device, source, target); Check(b.Snapshot().AsSpan().SequenceEqual(expected), "Size-compatible color formats retain encoded sample values without conversion.");
    }
    private static void Negatives(RenderingDevice device)
    {
        var source = Image(device, DataFormat.R8G8B8A8UNORM); var target = Image(device, DataFormat.R8G8B8A8UNORM);
        var single = Image(device, DataFormat.R8G8B8A8UNORM, TextureSamples.Samples1); Reject<ArgumentException>(() => Copy(device, source, single)); Reject<ArgumentException>(() => Copy(device, single, target));
        var otherCount = Image(device, DataFormat.R8G8B8A8UNORM, TextureSamples.Samples8); Reject<ArgumentException>(() => Copy(device, source, otherCount));
        var depth = Image(device, DataFormat.D32SFLOAT); Reject<ArgumentException>(() => Copy(device, source, depth));
        var noFrom = Image(device, DataFormat.R8G8B8A8UNORM, copies: TextureUsageBits.CanCopyTo); var noTo = Image(device, DataFormat.R8G8B8A8UNORM, copies: TextureUsageBits.CanCopyFrom); Reject<InvalidOperationException>(() => Copy(device, noFrom, target)); Reject<InvalidOperationException>(() => Copy(device, source, noTo));
        Reject<ArgumentException>(() => Copy(device, source, source)); Reject<ArgumentOutOfRangeException>(() => Copy(device, source, target, to: new(1, 0, 0))); Reject<ArgumentOutOfRangeException>(() => Copy(device, source, target, fromLayer: 1));
        Reject<ArgumentOutOfRangeException>(() => device.TextureCopy(source, target, default, default, new(1, 1, 1), 1, 0, 0, 0));
        Reject<ArgumentOutOfRangeException>(() => Copy(device, source, target, from: new(float.NaN, 0, 0))); Reject<ArgumentOutOfRangeException>(() => Copy(device, source, target, height: 0));
        var wrongDepth = Image(device, DataFormat.D16UNORM); Reject<ArgumentException>(() => Copy(device, depth, wrongDepth));
        var framebuffer = device.FramebufferCreate([source]); var draw = device.DrawListBegin(framebuffer); try { Reject<InvalidOperationException>(() => Copy(device, source, target)); } finally { device.DrawListEnd(); }
        var worker = Task.Run(() => { try { Copy(device, source, target); return false; } catch (InvalidOperationException) { return true; } }).GetAwaiter().GetResult(); Check(worker, "Copies reject a non-owner thread.");
        using var view = new RDTextureView(); var alias = device.TextureCreateShared(view, source); Reject<ArgumentException>(() => Copy(device, source, alias));
        var list = device.ComputeListBegin(); try { Reject<InvalidOperationException>(() => Copy(device, source, target)); } finally { device.ComputeListEnd(); }
        Reject<NotSupportedException>(() => device.TextureGetData(source, 0)); Reject<InvalidOperationException>(() => device.TextureUpdate(source, 0, new byte[64]));
    }
}
