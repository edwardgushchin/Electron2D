using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static class DepthResolveTests
{
    private static int _checks;
    private static string? _input, _output;
    private const TextureUsageBits Copies = TextureUsageBits.CanCopyFrom | TextureUsageBits.CanCopyTo;
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); _checks++; }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { _checks++; return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static RID Shader(RenderingDevice device, bool reader = false)
    {
        var name = reader ? "reader" : "producer";
        if (_input is not null) return device.ShaderCreateFromBytecode(File.ReadAllBytes(System.IO.Path.Combine(_input, name + ".bin")));
        using var source = new RDShaderSource
        {
            SourceVertex = "#version 450\nvoid main(){vec2 p[3]=vec2[3](vec2(-1,-1),vec2(3,-1),vec2(-1,3));gl_Position=vec4(p[gl_VertexIndex],.5,1);}",
            SourceFragment = "#version 450\nlayout(location=0)out vec4 color;layout(push_constant)uniform P{float layer;}p;void main(){float d=float(gl_SampleID+1)/16.0+(floor(gl_FragCoord.x)+floor(gl_FragCoord.y)*4.0)/64.0+p.layer/8.0;gl_FragDepth=d;color=vec4(d);}"
        };
        if (reader) { source.SourceVertex = ""; source.SourceFragment = ""; source.SourceCompute = "#version 450\nlayout(local_size_x=4,local_size_y=4)in;layout(set=0,binding=0)uniform sampler2DArray image;layout(set=0,binding=1,std430)buffer O{float values[];};void main(){ivec3 p=ivec3(gl_GlobalInvocationID);values[p.x+p.y*4+p.z*16]=texelFetch(image,p,0).r;}"; }
        using var module = device.ShaderCompileSPIRVFromSource(source, false);
        Check(module.CompileErrorVertex + module.CompileErrorFragment + module.CompileErrorCompute == "", module.CompileErrorVertex + module.CompileErrorFragment + module.CompileErrorCompute);
        if (_output is not null)
        {
            Directory.CreateDirectory(_output); File.WriteAllBytes(System.IO.Path.Combine(_output, name + ".bin"), device.ShaderCompileBinaryFromSPIRV(module));
            if (reader) File.WriteAllBytes(System.IO.Path.Combine(_output, name + ".Compute.spv"), module.BytecodeCompute);
            else { File.WriteAllBytes(System.IO.Path.Combine(_output, name + ".Vertex.spv"), module.BytecodeVertex); File.WriteAllBytes(System.IO.Path.Combine(_output, name + ".Fragment.spv"), module.BytecodeFragment); }
        }
        return device.ShaderCreateFromSPIRV(module);
    }
    private static bool Stencil(DataFormat format) => format is DataFormat.D24UNORMS8UINT or DataFormat.D32SFLOATS8UINT;
    private static RID Image(RenderingDevice device, DataFormat encoding, TextureSamples samples, uint layers, bool resolve = false)
    {
        using var format = new RDTextureFormat
        {
            Width = 4,
            Height = 4,
            Format = encoding,
            Samples = samples,
            TextureType = layers == 1 ? TextureType.Texture : TextureType.TextureArray,
            ArrayLayers = layers,
            UsageBits = Copies | TextureUsageBits.Sampling | (resolve ? TextureUsageBits.DepthResolveAttachment : encoding == DataFormat.R8G8B8A8UNORM ? TextureUsageBits.ColorAttachment : TextureUsageBits.DepthStencilAttachment)
        };
        using var view = new RDTextureView(); return device.TextureCreate(format, view);
    }
    internal static void Run(string? binaryDirectory = null)
    {
        _input = binaryDirectory; _output = Environment.GetEnvironmentVariable("ELECTRON2D_DEPTH_RESOLVE_OUTPUT");
        using var device = RenderingServer.CreateLocalRenderingDevice();
        if (Environment.GetEnvironmentVariable("ELECTRON2D_DEPTH_RESOLVE_REFUSAL") is { } refusal) { Refusal(device, refusal); return; }
        var shader = Shader(device); var reader = Shader(device, true);
        foreach (var samples in new[] { TextureSamples.Samples2, TextureSamples.Samples4, TextureSamples.Samples8 })
            foreach (var format in new[] { DataFormat.D16UNORM, DataFormat.X8D24UNORMPACK32, DataFormat.D32SFLOAT, DataFormat.D24UNORMS8UINT, DataFormat.D32SFLOATS8UINT }) Exercise(device, shader, reader, format, samples);
        Automatic(device);
        Console.WriteLine($"Depth/stencil resolve PASS {_checks}; {(_input is null ? "source/SPIR-V" : "binary")} on {device.GetDeviceName()}");
    }
    private static void Refusal(RenderingDevice device, string profile)
    {
        Check(!device.TextureIsFormatSupportedForUsage(DataFormat.D32SFLOATS8UINT, TextureUsageBits.DepthResolveAttachment), "Combined aspect resolve rejects without both enabled modes.");
        Check(device.TextureIsFormatSupportedForUsage(DataFormat.D32SFLOAT, TextureUsageBits.DepthResolveAttachment) == (profile == "stencil"), "Depth resolve has its independent gate.");
        Reject<NotSupportedException>(() => Image(device, DataFormat.D32SFLOATS8UINT, TextureSamples.Samples1, 1, true));
        using var state = new RDTextureFormat { Width = 4, Height = 4, Format = DataFormat.D32SFLOATS8UINT, UsageBits = TextureUsageBits.DepthStencilAttachment, IsResolveBuffer = true }; using var view = new RDTextureView();
        Reject<NotSupportedException>(() => device.TextureCreate(state, view));
        state.IsResolveBuffer = false; var ordinaryDepth = device.TextureCreate(state, view); var depthFramebuffer = device.FramebufferCreate([ordinaryDepth]);
        device.DrawListBegin(depthFramebuffer, DrawFlags.ClearDepth | DrawFlags.ClearStencil, clearDepthValue: .5f, clearStencilValue: 19); device.DrawListEnd();
        var color = Image(device, DataFormat.R8G8B8A8UNORM, TextureSamples.Samples4, 1);
        state.Format = DataFormat.R8G8B8A8UNORM; state.UsageBits = Copies | TextureUsageBits.ColorAttachment; state.IsResolveBuffer = true;
        var resolved = device.TextureCreate(state, view); var framebuffer = device.FramebufferCreate([color]);
        device.DrawListBegin(framebuffer, DrawFlags.ClearColor0, [Colors.White]); device.DrawListEnd(); device.TextureResolveMultisample(color, resolved);
        Check(device.TextureGetData(resolved, 0).All(value => value == 255), "Ordinary color resolve executes after depth feature refusal.");
        if (profile == "stencil")
        {
            var source = Image(device, DataFormat.D32SFLOAT, TextureSamples.Samples4, 1); var destination = Image(device, DataFormat.D32SFLOAT, TextureSamples.Samples1, 1, true);
            var target = device.FramebufferCreate([source, destination]); device.DrawListBegin(target, DrawFlags.ClearDepth, clearDepthValue: .625f); device.DrawListEnd();
            Check(BinaryPrimitives.ReadSingleLittleEndian(device.TextureGetData(destination, 0)) == .625f, "Depth-only native resolve survives stencil refusal.");
        }
        Console.WriteLine($"Depth/stencil resolve refusal {profile} PASS {_checks}; ordinary native rendering preserved");
    }
    private static void Exercise(RenderingDevice device, RID shader, RID reader, DataFormat encoding, TextureSamples samples)
    {
        Check(device.TextureIsFormatSupportedForUsage(encoding, TextureUsageBits.DepthResolveAttachment | Copies | TextureUsageBits.Sampling), "The native depth resolve format/roles are available.");
        var source = Image(device, encoding, samples, 2); var destination = Image(device, encoding, TextureSamples.Samples1, 2, true); var color = Image(device, DataFormat.R8G8B8A8UNORM, samples, 1);
        for (uint layer = 0; layer < 2; layer++)
        {
            using var view = new RDTextureView(); var slice = device.TextureCreateSharedFromSlice(view, source, layer, 0); var framebuffer = device.FramebufferCreate([color, slice]);
            using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
            var count = 1 << (int)samples;
            for (var sample = 0; sample < (Stencil(encoding) ? count : 1); sample++)
            {
                using var depth = new RDPipelineDepthStencilState
                {
                    EnableDepthTest = true,
                    EnableDepthWrite = true,
                    DepthCompareOperator = CompareOperator.Always,
                    EnableStencil = Stencil(encoding),
                    FrontOpCompare = CompareOperator.Always,
                    BackOpCompare = CompareOperator.Always,
                    FrontOpPass = StencilOperation.Replace,
                    BackOpPass = StencilOperation.Replace,
                    FrontOpWriteMask = 255,
                    BackOpWriteMask = 255,
                    FrontOpReference = 17 + sample + (int)layer * 16,
                    BackOpReference = 17 + sample + (int)layer * 16
                };
                using var multi = new RDPipelineMultisampleState { SampleCount = samples, SampleMasks = Stencil(encoding) ? Enumerable.Repeat(1u << sample, count).ToArray() : [] };
                var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(framebuffer), -1, RenderPrimitive.Triangles, raster, blend, depthStencilState: depth, multisampleState: multi);
                var list = device.DrawListBegin(framebuffer, sample == 0 ? DrawFlags.ClearAll : DrawFlags.DefaultAll, [Colors.Black]); device.DrawListBindRenderPipeline(list, pipeline);
                var push = (float)layer; device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref push, 1)), 4); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
            }
        }
        device.TextureResolveMultisample(source, destination);
        for (uint layer = 0; layer < 2; layer++) CheckData(device.TextureGetData(destination, layer), encoding, layer);
        using var samplerState = new RDSamplerState(); var sampler = device.SamplerCreate(samplerState); var output = device.StorageBufferCreate(128);
        using var sampled = new RDUniform { Binding = 0, UniformType = UniformType.SamplerWithTexture }; sampled.AddID(sampler); sampled.AddID(destination);
        using var buffer = new RDUniform { Binding = 1, UniformType = UniformType.StorageBuffer }; buffer.AddID(output);
        var set = device.UniformSetCreate([sampled, buffer], reader, 0); var compute = device.ComputePipelineCreate(reader);
        var computeList = device.ComputeListBegin(); device.ComputeListBindComputePipeline(computeList, compute); device.ComputeListBindUniformSet(computeList, set, 0); device.ComputeListDispatch(computeList, 1, 1, 2); device.ComputeListEnd();
        var values = device.BufferGetData(output);
        for (var i = 0; i < 32; i++) Check(Math.Abs(BinaryPrimitives.ReadSingleLittleEndian(values.AsSpan(i * 4)) - (1f / 16 + i % 16 / 64f + i / 16 / 8f)) < .00002f, "Native GPU sampling consumes the resolved depth after layout restoration.");
        var data = new byte[16 * (encoding == DataFormat.D16UNORM ? 2 : encoding == DataFormat.D32SFLOATS8UINT ? 8 : 4)];
        void Cycle() { device.TextureResolveMultisample(source, destination); device.TextureGetData(destination, 0, data.AsSpan()); }
        for (var i = 0; i < 16; i++) Cycle(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Cycle(); var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated == 0, "Prepared depth/stencil resolve/readback allocates zero owner managed bytes: " + allocated);
        using var shared = new RDTextureView();
        var fromSlice = device.TextureCreateSharedFromSlice(shared, source, 1, 0); var fromNested = device.TextureCreateSharedFromSlice(shared, fromSlice, 1, 0);
        var toSlice = device.TextureCreateSharedFromSlice(shared, destination, 0, 0); var toNested = device.TextureCreateSharedFromSlice(shared, toSlice, 0, 0);
        device.TextureResolveMultisample(fromNested, toNested); CheckData(device.TextureGetData(destination, 0), encoding, 1);
        CheckData(device.TextureGetData(destination, 1), encoding, 1);
        Reject<ArgumentException>(() => device.TextureResolveMultisample(destination, source));
        Reject<ArgumentException>(() => device.TextureResolveMultisample(source, source));
        device.DrawListBegin(device.FramebufferCreate([color]));
        Reject<InvalidOperationException>(() => device.TextureResolveMultisample(source, destination)); device.DrawListEnd();
        device.FreeRID(source); device.FreeRID(destination); device.FreeRID(color);
        Reject<ArgumentException>(() => device.TextureResolveMultisample(fromNested, toNested));
    }
    private static void Automatic(RenderingDevice device)
    {
        using var format = new RDTextureFormat { Width = 4, Height = 4, Format = DataFormat.D32SFLOATS8UINT, Samples = TextureSamples.Samples4, TextureType = TextureType.TextureArray, ArrayLayers = 2, UsageBits = TextureUsageBits.DepthStencilAttachment };
        using var view = new RDTextureView(); var source = device.TextureCreate(format, view);
        format.Samples = TextureSamples.Samples1; format.UsageBits = TextureUsageBits.DepthResolveAttachment | TextureUsageBits.CanCopyFrom | TextureUsageBits.CanCopyTo | TextureUsageBits.Sampling;
        var destination = device.TextureCreate(format, view); var framebuffer = device.FramebufferCreate([source, destination], viewCount: 2);
        device.DrawListBegin(framebuffer, DrawFlags.ClearDepth | DrawFlags.ClearStencil, clearDepthValue: .375f, clearStencilValue: 91); device.DrawListEnd();
        for (uint layer = 0; layer < 2; layer++) Constant(device.TextureGetData(destination, layer), .375f, 91);
        var snapshot = new byte[128]; var completed = 0;
        device.TextureGetDataAsync(destination, 1, snapshot.AsMemory(), () => completed++);
        device.DrawListBegin(framebuffer, DrawFlags.ClearDepth | DrawFlags.ClearStencil, clearDepthValue: .75f, clearStencilValue: 37); device.DrawListEnd();
        device.Submit(); device.Sync(); Check(completed == 1, "Queued resolve readback completes once."); Constant(snapshot, .375f, 91); Constant(device.TextureGetData(destination, 1), .75f, 37);
        Reject<InvalidOperationException>(() => device.TextureResolveMultisample(source, destination));
        Reject<ArgumentException>(() => device.FramebufferCreate([destination]));
        format.Samples = TextureSamples.Samples4;
        Reject<ArgumentException>(() => device.TextureCreate(format, view));
        format.Samples = TextureSamples.Samples1; format.UsageBits = Copies | TextureUsageBits.DepthStencilAttachment; format.IsResolveBuffer = true;
        var marked = device.TextureCreate(format, view);
        using var markedInfo = device.TextureGetFormat(marked); Check(markedInfo.IsResolveBuffer, "Depth resolve marker survives capture.");
        format.IsResolveBuffer = false; format.Samples = TextureSamples.Samples4; var copySource = device.TextureCreate(format, view);
        var copyFramebuffer = device.FramebufferCreate([copySource], viewCount: 2);
        device.DrawListBegin(copyFramebuffer, DrawFlags.ClearDepth | DrawFlags.ClearStencil, clearDepthValue: .625f, clearStencilValue: 123); device.DrawListEnd();
        device.TextureResolveMultisample(copySource, marked); Constant(device.TextureGetData(marked, 0), .625f, 123);
        Reject<InvalidOperationException>(() => Task.Run(() => device.TextureResolveMultisample(copySource, marked)).GetAwaiter().GetResult());
        format.Samples = TextureSamples.Samples1; format.IsResolveBuffer = true; format.ArrayLayers = 1; var wrongLayers = device.TextureCreate(format, view);
        Reject<ArgumentException>(() => device.TextureResolveMultisample(copySource, wrongLayers));
        format.ArrayLayers = 2; format.Width = 2; var wrongExtent = device.TextureCreate(format, view);
        Reject<ArgumentException>(() => device.TextureResolveMultisample(copySource, wrongExtent));
        format.Width = 4; format.Format = DataFormat.D16UNORM; var wrongFormat = device.TextureCreate(format, view);
        Reject<ArgumentException>(() => device.TextureResolveMultisample(copySource, wrongFormat));
        format.Format = DataFormat.D32SFLOATS8UINT; format.IsResolveBuffer = false; var unmarked = device.TextureCreate(format, view);
        Reject<ArgumentException>(() => device.TextureResolveMultisample(copySource, unmarked));
        device.DrawListBegin(copyFramebuffer, DrawFlags.ClearDepth | DrawFlags.ClearStencil, clearDepthValue: .25f, clearStencilValue: 127); device.DrawListEnd(); device.TextureResolveMultisample(copySource, marked);
        device.FreeRID(copySource); Constant(device.TextureGetData(marked, 1), .25f, 127);
        device.FreeRID(marked); device.FreeRID(source); device.FreeRID(destination);
        Check(!device.FramebufferIsValid(framebuffer), "Retiring a resolve image invalidates its borrowing framebuffer.");
    }
    private static void Constant(byte[] data, float depth, byte stencil)
    {
        for (var pixel = 0; pixel < 16; pixel++)
        {
            var bytes = data.AsSpan(pixel * 8, 8);
            Check(BinaryPrimitives.ReadSingleLittleEndian(bytes) == depth && bytes[4] == stencil && bytes[5] == 0 && bytes[6] == 0 && bytes[7] == 0, "Framebuffer resolve preserves both native aspects and request-time snapshots.");
        }
    }
    private static void CheckData(byte[] bytes, DataFormat encoding, uint layer)
    {
        var stride = encoding == DataFormat.D16UNORM ? 2 : encoding == DataFormat.D32SFLOATS8UINT ? 8 : 4;
        for (var pixel = 0; pixel < 16; pixel++)
        {
            var data = bytes.AsSpan(pixel * stride, stride); var expected = 1f / 16 + pixel / 64f + layer / 8f;
            var actual = encoding == DataFormat.D16UNORM ? BinaryPrimitives.ReadUInt16LittleEndian(data) / 65535f : encoding is DataFormat.X8D24UNORMPACK32 or DataFormat.D24UNORMS8UINT ? (BinaryPrimitives.ReadUInt32LittleEndian(data) & 0xffffff) / 16777215f : BinaryPrimitives.ReadSingleLittleEndian(data);
            Check(MathF.Abs(actual - expected) <= (encoding == DataFormat.D16UNORM ? 1f / 65535 : 1f / 16777215), "Resolve selects sample zero at every pixel/layer.");
            if (Stencil(encoding)) Check(data[encoding == DataFormat.D24UNORMS8UINT ? 3 : 4] == 17 + layer * 16, "Stencil resolves its own sample zero.");
            if (encoding == DataFormat.X8D24UNORMPACK32) Check(data[3] == 0, "Unused depth bits remain canonical.");
            if (encoding == DataFormat.D32SFLOATS8UINT) Check(data[5..].SequenceEqual(new byte[3]), "Depth/stencil padding remains canonical.");
        }
    }
}
