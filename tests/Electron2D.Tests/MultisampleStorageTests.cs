using System.Globalization;
using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static class MultisampleStorageTests
{
    private static int _checks;
    private static string? _input, _output;
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); _checks++; }
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
    private const string Vertex = """
        #version 450
        void main() {
            vec2 p[3]=vec2[3](vec2(-1,-1),vec2(3,-1),vec2(-1,3));
            gl_Position=vec4(p[gl_VertexIndex],0,1);
        }
        """;
    private static RID Image(RenderingDevice device, DataFormat encoding = DataFormat.R8G8B8A8UNORM, bool array = false, TextureSamples samples = TextureSamples.Samples4, TextureUsageBits? usage = null)
    {
        using var format = new RDTextureFormat { Width = 4, Height = 4, Format = encoding, TextureType = array ? TextureType.TextureArray : TextureType.Texture, ArrayLayers = array ? 2u : 1u, Samples = samples, UsageBits = usage ?? (TextureUsageBits.Storage | TextureUsageBits.Sampling) };
        using var view = new RDTextureView(); return device.TextureCreate(format, view);
    }
    private static RDUniform Uniform(int binding, UniformType type, params RID[] resources)
    {
        var uniform = new RDUniform { Binding = binding, UniformType = type }; foreach (var id in resources) uniform.AddID(id); return uniform;
    }
    private static void Dispatch(RenderingDevice device, RID pipeline, RID set, uint count = 1)
    {
        var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, set, 0); device.ComputeListDispatch(list, count, 1, 1); device.ComputeListEnd();
    }
    internal static void Run(string? binaryDirectory = null)
    {
        _input = binaryDirectory; _output = Environment.GetEnvironmentVariable("ELECTRON2D_MS_STORAGE_OUTPUT");
        using var device = RenderingServer.CreateLocalRenderingDevice();
        if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_MULTISAMPLE_MASK") == "storage")
        {
            var ordinary = Image(device, samples: TextureSamples.Samples1);
            var sampled = Image(device, usage: TextureUsageBits.Sampling | TextureUsageBits.ColorAttachment);
            Check(ordinary != default && sampled != default, "Optional storage refusal preserves ordinary storage and MSAA sampling.");
            Reject<NotSupportedException>(() => Image(device));
            if (_input is null) throw new InvalidOperationException("The refusal scenario requires compiled shader fixtures.");
            Reject<NotSupportedException>(() => device.ShaderCreateFromBytecode(File.ReadAllBytes(System.IO.Path.Combine(_input, "fill.bin"))));
            Console.WriteLine($"Multisample storage refusal PASS {_checks}"); return;
        }
        var left = Image(device); var right = Image(device, usage: TextureUsageBits.Storage); var layers = Image(device, array: true);
        using var description = device.TextureGetFormat(right);
        Check(description.UsageBits == TextureUsageBits.Storage, "Initialization retains the public storage-only role.");
        Reject<ArgumentException>(() => device.FramebufferCreate([right]));
        Compute(device, left, right, layers);
        Graphics(device, left, right, layers);
        Numeric(device, DataFormat.R32UINT); Numeric(device, DataFormat.R32SINT);
        Negatives(device, left);
        Console.WriteLine($"Multisample storage PASS {_checks}; {(_input is null ? "source/SPIR-V" : "binary")} on {device.GetDeviceName()}");
    }
    private static void Compute(RenderingDevice device, RID left, RID right, RID layers)
    {
        var shader = Shader(device, "fill", new RDShaderSource { SourceCompute = """
            #version 450
            layout(local_size_x=1) in;
            layout(set=0,binding=0,rgba8) uniform image2DMS choices[2];
            layout(set=0,binding=1,rgba8) uniform image2DMSArray layers;
            layout(set=0,binding=2,std430) buffer Results { vec4 value[]; } result;
            void main() {
                int selected=int(gl_GlobalInvocationID.x);
                result.value[16+selected]=imageLoad(choices[selected],ivec2(3),3);
                for(int s=0;s<4;s++) {
                    vec4 v=vec4(float(s+1)/8.0+float(selected)/4.0,.25,.75,1);
                    imageStore(choices[selected],ivec2(0),s,v);
                    imageStore(layers,ivec3(1,1,selected),s,v);
                    memoryBarrierImage();
                    result.value[selected*4+s]=imageLoad(choices[selected],ivec2(0),s);
                    result.value[8+selected*4+s]=imageLoad(layers,ivec3(1,1,selected),s);
                }
                result.value[19+selected]=vec4(imageSamples(choices[selected+2]));
                if(selected==0)result.value[18]=vec4(imageSize(choices[0]),imageSamples(layers),imageSize(layers).z);
            }
            """ });
        var output = device.StorageBufferCreate(21 * 16); using var a = Uniform(0, UniformType.Image, left, right); using var b = Uniform(1, UniformType.Image, layers); using var c = Uniform(2, UniformType.StorageBuffer, output);
        var set = device.UniformSetCreate([a, b, c], shader, 0); var pipeline = device.ComputePipelineCreate(shader);
        Dispatch(device, pipeline, set, 2); var bytes = device.BufferGetData(output); var values = MemoryMarshal.Cast<byte, float>(bytes);
        for (var i = 0; i < 16; i++) Check(Math.Abs(values[i * 4] - ((i % 4 + 1) / 8f + (i / 4 % 2) / 4f)) < .004f, "Compute selects, writes and reads the requested MSAA descriptor/layer/sample.");
        Check(values.Slice(64, 8).SequenceEqual(new float[8]), "Storage-only and sampled/storage images initialize every sample to zero.");
        Check(values.Slice(72, 4).SequenceEqual([4f, 4f, 4f, 2f]), "Storage image queries retain dimensions, samples and array extent: " + string.Join(",", values.Slice(72, 4).ToArray()));
        Check(values.Slice(76, 8).SequenceEqual(new float[8]), "Invalid descriptor selectors query zero without native image access.");
        for (var i = 0; i < 8; i++) { Dispatch(device, pipeline, set, 2); device.BufferGetData(output, bytes.AsSpan()); }
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 32; i++) { Dispatch(device, pipeline, set, 2); device.BufferGetData(output, bytes.AsSpan()); }
        Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "Prepared multisample storage dispatch/readback allocates zero owner-thread managed bytes.");
        using var view = new RDTextureView(); var slice = device.TextureCreateSharedFromSlice(view, layers, 1, 0);
        var reader = Shader(device, "slice", new RDShaderSource { SourceCompute = "#version 450\nlayout(local_size_x=1) in;layout(set=0,binding=0,rgba8) readonly uniform image2DMS source;layout(set=0,binding=1,std430) buffer R{vec4 v;} r;void main(){r.v=imageLoad(source,ivec2(1),2);}" });
        using var source = Uniform(0, UniformType.Image, slice); using var result = Uniform(1, UniformType.StorageBuffer, output); var readSet = device.UniformSetCreate([source, result], reader, 0); var readPipeline = device.ComputePipelineCreate(reader);
        Dispatch(device, readPipeline, readSet); device.FreeRID(slice); Check(!device.UniformSetIsValid(readSet), "Retiring a storage slice invalidates its descriptor set.");
        device.BufferGetData(output, bytes.AsSpan()); Check(Math.Abs(MemoryMarshal.Cast<byte, float>(bytes)[0] - .625f) < .004f, "Pending storage reads retain their selected layer until completion.");
    }
    private static void Graphics(RenderingDevice device, RID left, RID right, RID layers)
    {
        var shader = Shader(device, "graphics", new RDShaderSource { SourceVertex = """
            #version 450
            layout(set=0,binding=0,rgba8) uniform image2DMS source;
            layout(set=0,binding=2,rgba8) readonly uniform image2DMSArray layers;
            layout(location=0) out vec4 value;
            void main(){
                vec2 p[3]=vec2[3](vec2(-1,-1),vec2(3,-1),vec2(-1,3));gl_Position=vec4(p[gl_VertexIndex],0,1);
                value=imageLoad(source,ivec2(0),1);value.b=imageLoad(layers,ivec3(1,1,1),3).r;
                if(gl_VertexIndex==0)imageStore(source,ivec2(2),3,vec4(.875,.5,.25,1));
            }
            """, SourceFragment = """
            #version 450
            layout(set=0,binding=0,rgba8) readonly uniform image2DMS source;
            layout(set=0,binding=1,rgba8) writeonly uniform image2DMS target;
            layout(location=0) in vec4 value;layout(location=0) out vec4 color;
            void main(){color=vec4(imageLoad(source,ivec2(0),2).r,value.r,value.b,1);imageStore(target,ivec2(gl_FragCoord.xy),2,color);}
            """ });
        using var a = Uniform(0, UniformType.Image, left); using var b = Uniform(1, UniformType.Image, right); using var layered = Uniform(2, UniformType.Image, layers); var set = device.UniformSetCreate([a, b, layered], shader, 0);
        var output = Image(device, samples: TextureSamples.Samples1, usage: TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom); var framebuffer = device.FramebufferCreate([output]);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
        var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(framebuffer), -1, RenderPrimitive.Triangles, raster, blend);
        void Draw() { var list = device.DrawListBegin(framebuffer, DrawFlags.ClearColorAll, [Colors.Black]); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 0); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); }
        Draw(); var pixels = device.TextureGetData(output, 0);
        Check(pixels[0] is >= 95 and <= 97 && pixels[1] is >= 63 and <= 65 && pixels[2] is >= 190 and <= 192, "Vertex and readonly fragment MS storage loads reach the real color target.");
        if (_output is not null) { using var image = Electron2D.Image.CreateFromData(4, 4, false, Electron2D.Image.Format.Rgba8, pixels); image.SavePNG(System.IO.Path.Combine(_output, "graphics.png")); }
        for (var i = 0; i < 8; i++) { Draw(); device.TextureGetData(output, 0, pixels.AsSpan()); }
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 32; i++) { Draw(); device.TextureGetData(output, 0, pixels.AsSpan()); }
        Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "Prepared multisample graphics storage/readback allocates zero owner-thread managed bytes.");
        var feedback = Image(device, usage: TextureUsageBits.ColorAttachment | TextureUsageBits.Storage); var feedbackFramebuffer = device.FramebufferCreate([feedback]);
        using var feedbackSource = Uniform(0, UniformType.Image, feedback); var feedbackSet = device.UniformSetCreate([feedbackSource, b, layered], shader, 0); using var samples = new RDPipelineMultisampleState { SampleCount = TextureSamples.Samples4 };
        var feedbackPipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(feedbackFramebuffer), -1, RenderPrimitive.Triangles, raster, blend, multisampleState: samples);
        var feedbackList = device.DrawListBegin(feedbackFramebuffer); device.DrawListBindRenderPipeline(feedbackList, feedbackPipeline); device.DrawListBindUniformSet(feedbackList, feedbackSet, 0);
        try { Reject<InvalidOperationException>(() => device.DrawListDraw(feedbackList, false, 1, 3)); } finally { device.DrawListEnd(); }
        Draw(); device.TextureGetData(output, 0, pixels.AsSpan()); Check(pixels[0] is >= 95 and <= 97, "Storage output-feedback refusal preserves later graphics execution.");
        var verify = Shader(device, "graphics-readback", new RDShaderSource { SourceCompute = "#version 450\nlayout(local_size_x=1) in;layout(set=0,binding=0,rgba8) readonly uniform image2DMS a;layout(set=0,binding=1,rgba8) readonly uniform image2DMS b;layout(set=0,binding=2,std430) buffer R{vec4 v[2];}r;void main(){r.v[0]=imageLoad(a,ivec2(2),3);r.v[1]=imageLoad(b,ivec2(1),2);}" });
        var result = device.StorageBufferCreate(32); using var c = Uniform(2, UniformType.StorageBuffer, result); var verifySet = device.UniformSetCreate([a, b, c], verify, 0); Dispatch(device, device.ComputePipelineCreate(verify), verifySet);
        var data = MemoryMarshal.Cast<byte, float>(device.BufferGetData(result));
        Check(Math.Abs(data[0] - .875f) < .004f && Math.Abs(data[4] - .375f) < .004f && Math.Abs(data[5] - .25f) < .004f, "Vertex and fragment stores are visible to later compute loads.");
    }
    private static void Numeric(RenderingDevice device, DataFormat format)
    {
        var signed = format == DataFormat.R32SINT; var prefix = signed ? "i" : "u";
        var first = Image(device, format, array: true, usage: TextureUsageBits.Storage | TextureUsageBits.StorageAtomic); var second = Image(device, format, array: true, usage: TextureUsageBits.Storage | TextureUsageBits.StorageAtomic);
        var shader = Shader(device, "atomic" + format, new RDShaderSource { SourceCompute = "#version 450\nlayout(local_size_x=1) in;layout(set=0,binding=0,r32" + (signed ? "i" : "ui") + ") uniform " + prefix + "image2DMSArray images[2];layout(set=0,binding=1,std430) buffer R{" + (signed ? "int" : "uint") + " v[16];}r;void main(){int n=int(gl_GlobalInvocationID.x);for(int s=0;s<4;s++){r.v[n*8+s]=imageAtomicAdd(images[n],ivec3(0,0,n),s," + (signed ? "-3" : "3u") + ");r.v[n*8+4+s]=imageLoad(images[n],ivec3(0,0,n),s).r;}}" });
        var output = device.StorageBufferCreate(64); using var a = Uniform(0, UniformType.Image, first, second); using var b = Uniform(1, UniformType.StorageBuffer, output); var set = device.UniformSetCreate([a, b], shader, 0); Dispatch(device, device.ComputePipelineCreate(shader), set, 2);
        var values = MemoryMarshal.Cast<byte, int>(device.BufferGetData(output));
        for (var n = 0; n < 2; n++) Check(values.Slice(n * 8, 4).SequenceEqual([0, 0, 0, 0]) && values.Slice(n * 8 + 4, 4).ToArray().All(x => x == (signed ? -3 : 3)), "R32 MS array atomics preserve signedness and select each descriptor/layer/sample independently.");
    }
    private static void Negatives(RenderingDevice device, RID multi)
    {
        var shader = Shader(device, "negative", new RDShaderSource { SourceCompute = "#version 450\nlayout(local_size_x=1) in;layout(set=0,binding=0,rgba8) uniform image2DMS image;void main(){imageStore(image,ivec2(0),0,vec4(1));}" });
        var single = Image(device, samples: TextureSamples.Samples1); var array = Image(device, array: true); var wrongFormat = Image(device, DataFormat.R32UINT); var sampled = Image(device, usage: TextureUsageBits.ColorAttachment | TextureUsageBits.Sampling);
        foreach (var id in new[] { single, array, wrongFormat, sampled }) { using var u = Uniform(0, UniformType.Image, id); Reject<ArgumentException>(() => device.UniformSetCreate([u], shader, 0)); }
        using var invalid = new RDTextureFormat { Width = 4, Height = 4, Samples = TextureSamples.Samples4, UsageBits = TextureUsageBits.Storage, MIPMaps = 2 }; using var view = new RDTextureView();
        Reject<NotSupportedException>(() => device.TextureCreate(invalid, view)); invalid.MIPMaps = 1; Reject<NotSupportedException>(() => device.TextureCreate(invalid, view, [new byte[64]]));
        invalid.Format = DataFormat.D32SFLOAT; invalid.UsageBits |= TextureUsageBits.DepthStencilAttachment; Reject<NotSupportedException>(() => device.TextureCreate(invalid, view));
        byte[] original;
        if (_input is not null) original = File.ReadAllBytes(System.IO.Path.Combine(_input, "negative.Compute.spv"));
        else { using var source = new RDShaderSource { SourceCompute = "#version 450\nlayout(local_size_x=1) in;layout(set=0,binding=0,rgba8) uniform image2DMS image;void main(){imageStore(image,ivec2(0),0,vec4(1));}" }; using var module = device.ShaderCompileSPIRVFromSource(source, false); original = module.BytecodeCompute; }
        var words = MemoryMarshal.Cast<byte, uint>(original.AsSpan()); var output = new List<uint>(words[..5].ToArray());
        for (var at = 5; at < words.Length;) { var count = (int)(words[at] >> 16); if (!((words[at] & 65535) == 17 && words[at + 1] == 27)) output.AddRange(words.Slice(at, count).ToArray()); at += count; }
        using var malformed = new RDShaderSPIRV { BytecodeCompute = MemoryMarshal.AsBytes(output.ToArray().AsSpan()).ToArray() }; Reject<ArgumentException>(() => device.ShaderCreateFromSPIRV(malformed));
        using var uniform = Uniform(0, UniformType.Image, multi); var set = device.UniformSetCreate([uniform], shader, 0); device.FreeRID(multi); Check(!device.UniformSetIsValid(set), "Owner retirement invalidates multisample storage borrowers.");
    }
}
