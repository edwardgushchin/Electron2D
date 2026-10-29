using System.Globalization;
using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static class MultisampleSamplingTests
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
    private static RID Image(RenderingDevice device, bool depth = false, bool array = false, TextureSamples samples = TextureSamples.Samples4)
    {
        using var format = new RDTextureFormat { Width = 4, Height = 4, Format = depth ? DataFormat.D32SFLOAT : DataFormat.R8G8B8A8UNORM, TextureType = array ? TextureType.TextureArray : TextureType.Texture, ArrayLayers = array ? 2u : 1u, Samples = samples, UsageBits = TextureUsageBits.Sampling | (depth ? TextureUsageBits.DepthStencilAttachment : TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom | TextureUsageBits.CanCopyTo) };
        using var view = new RDTextureView(); return device.TextureCreate(format, view);
    }
    private static RID Set(RenderingDevice device, RID shader, uint index, int binding, UniformType type, params RID[] resources)
    {
        using var uniform = new RDUniform { Binding = binding, UniformType = type }; foreach (var id in resources) uniform.AddID(id);
        return device.UniformSetCreate([uniform], shader, index);
    }
    internal static void Run(string? binaryDirectory = null)
    {
        _input = binaryDirectory; _output = Environment.GetEnvironmentVariable("ELECTRON2D_MS_SAMPLING_OUTPUT");
        using var device = RenderingServer.CreateLocalRenderingDevice();
        var color = Image(device); var depth = Image(device, depth: true); var array = Image(device, array: true);
        var blue = Image(device); device.TextureClear(blue, Colors.Blue, 0, 1, 0, 1);
        Produce(device, color, depth, array);
        foreach (var language in new[] { ShaderLanguage.GLSL, ShaderLanguage.HLSL }) { Compute(device, language, color, depth, array, blue); Graphics(device, language, color); }
        MismatchedStages(device, color);
        Numeric(device);
        Console.WriteLine($"Multisample shader sampling PASS {_checks}; {(_input is null ? "source/SPIR-V" : "binary")} on {device.GetDeviceName()}");
    }
    private static void Produce(RenderingDevice device, RID color, RID depth, RID array)
    {
        using var raster = new RDPipelineRasterizationState(); using var blendAttachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [blendAttachment] };
        using var depthState = new RDPipelineDepthStencilState { EnableDepthTest = true, EnableDepthWrite = true, DepthCompareOperator = CompareOperator.Always };
        using var samples = new RDPipelineMultisampleState { SampleCount = TextureSamples.Samples4 };
        for (var layer = -1; layer < 2; layer++)
        {
            using var view = new RDTextureView(); var target = layer < 0 ? color : device.TextureCreateSharedFromSlice(view, array, (uint)layer, 0);
            var framebuffer = layer < 0 ? device.FramebufferCreate([target, depth]) : device.FramebufferCreate([target]);
            var offset = layer == 1 ? ".25" : "0";
            var shader = Shader(device, "producer" + layer.ToString(CultureInfo.InvariantCulture), new RDShaderSource { SourceVertex = Vertex, SourceFragment = "#version 450\nlayout(location=0) out vec4 color;\nvoid main(){float v=float(gl_SampleID+1)/8.0; color=vec4(v+" + offset + ",.25,.75,1);gl_FragDepth=v;}" });
            var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(framebuffer), -1, RenderPrimitive.Triangles, raster, blend, depthStencilState: layer < 0 ? depthState : null, multisampleState: samples);
            var list = device.DrawListBegin(framebuffer, DrawFlags.ClearAll, [Colors.Black]); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
        }
    }
    private static void Compute(RenderingDevice device, ShaderLanguage language, RID color, RID depth, RID array, RID blue)
    {
        var shader = Shader(device, "compute" + language, new RDShaderSource { Language = language, SourceCompute = language == ShaderLanguage.GLSL ? """
            #version 450
            layout(local_size_x=1) in;
            layout(set=0,binding=0) uniform sampler2DMS color;
            layout(set=0,binding=1) uniform sampler2DMS depth;
            layout(set=0,binding=2) uniform sampler2DMSArray layers;
            layout(set=0,binding=3) uniform sampler2DMS choices[2];
            layout(set=1,binding=0,std430) buffer Results { vec4 value[]; } result;
            void main() {
                uint selected=gl_GlobalInvocationID.x;
                for (int i=0;i<4;i++) {
                    if(selected==0) {
                        result.value[i]=texelFetch(color,ivec2(0),i);
                        result.value[4+i]=texelFetch(depth,ivec2(0),i);
                        result.value[8+i]=texelFetch(layers,ivec3(0,0,1),i);
                    }
                    result.value[12+selected*4+i]=texelFetch(choices[selected],ivec2(0),i);
                }
                if(selected==0) {
                    result.value[20]=vec4(textureSamples(color),textureSamples(depth),textureSamples(layers),textureSamples(choices[1]));
                    result.value[21]=vec4(textureSize(color),textureSize(layers).z,0);
                }
            }
            """ : """
            [[vk::binding(0,0)]] Texture2DMS<float4> color;
            [[vk::binding(1,0)]] Texture2DMS<float> depth;
            [[vk::binding(2,0)]] Texture2DMSArray<float4> layers;
            [[vk::binding(3,0)]] Texture2DMS<float4> choices[2];
            [[vk::binding(0,1)]] RWStructuredBuffer<float4> result;
            [numthreads(1,1,1)] void main(uint3 id:SV_DispatchThreadID) {
                uint selected=id.x;
                for(int i=0;i<4;i++) {
                    if(selected==0) {
                        result[i]=color.Load(int2(0,0),i);
                        result[4+i]=float4(depth.Load(int2(0,0),i),0,0,1);
                        result[8+i]=layers.Load(int3(0,0,1),i);
                    }
                    result[12+selected*4+i]=choices[selected].Load(int2(0,0),i);
                }
                if(selected==0) {
                    uint w,h,n,a,s; color.GetDimensions(w,h,n); result[20].x=n;
                    depth.GetDimensions(w,h,n); result[20].y=n;
                    layers.GetDimensions(w,h,a,s); result[20].z=s;
                    choices[1].GetDimensions(w,h,n); result[20].w=n;
                    color.GetDimensions(w,h,n); result[21]=float4(w,h,a,0);
                }
            }
            """ });
        using var samplerState = new RDSamplerState(); var sampler = device.SamplerCreate(samplerState);
        using var first = new RDUniform { Binding = 0, UniformType = language == ShaderLanguage.GLSL ? UniformType.SamplerWithTexture : UniformType.Texture };
        using var second = new RDUniform { Binding = 1, UniformType = first.UniformType };
        using var third = new RDUniform { Binding = 2, UniformType = first.UniformType };
        using var fourth = new RDUniform { Binding = 3, UniformType = first.UniformType };
        void Add(RDUniform uniform, RID image) { if (language == ShaderLanguage.GLSL) uniform.AddID(sampler); uniform.AddID(image); }
        using var aliasView = new RDTextureView { SwizzleG = TextureSwizzle.B, SwizzleB = TextureSwizzle.G }; var alias = device.TextureCreateShared(aliasView, color);
        Add(first, alias); Add(second, depth); Add(third, array); Add(fourth, color); Add(fourth, blue);
        var images = device.UniformSetCreate([first, second, third, fourth], shader, 0);
        var single = Image(device, samples: TextureSamples.Samples1); first.ClearIDs(); Add(first, single);
        Reject<ArgumentException>(() => device.UniformSetCreate([first, second, third, fourth], shader, 0));
        first.ClearIDs(); Add(first, alias); third.ClearIDs(); Add(third, color);
        Reject<ArgumentException>(() => device.UniformSetCreate([first, second, third, fourth], shader, 0));
        var output = device.StorageBufferCreate(22 * 16); var result = Set(device, shader, 1, 0, UniformType.StorageBuffer, output); var pipeline = device.ComputePipelineCreate(shader);
        void Execute()
        {
            var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, images, 0); device.ComputeListBindUniformSet(list, result, 1); device.ComputeListDispatch(list, 2, 1, 1); device.ComputeListEnd();
        }
        Execute(); var bytes = device.BufferGetData(output); var values = MemoryMarshal.Cast<byte, float>(bytes);
        for (var i = 0; i < 4; i++)
        {
            Check(Math.Abs(values[i * 4] - (i + 1) / 8f) < .004f, language + " reads distinct stored color samples.");
            Check(Math.Abs(values[(4 + i) * 4] - (i + 1) / 8f) < .00001f, language + " reads distinct stored depth samples.");
            Check(Math.Abs(values[(8 + i) * 4] - ((i + 1) / 8f + .25f)) < .004f, language + " reads the requested MSAA array layer.");
            Check(Math.Abs(values[(12 + i) * 4] - values[i * 4]) < .00001f && values[(16 + i) * 4] == 0 && values[(16 + i) * 4 + 2] == 1, language + " selects both multisample descriptor-array entries.");
        }
        Check(Math.Abs(values[1] - .75f) < .004f && Math.Abs(values[2] - .25f) < .004f, "Multisample component maps apply after the selected sample load.");
        Check(values.Slice(80, 4).SequenceEqual([4f, 4f, 4f, 4f]) && values.Slice(84, 4).SequenceEqual([4f, 4f, 2f, 0f]), language + " queries dimensions, array layers and native sample counts.");
        for (var i = 0; i < 8; i++) { Execute(); device.BufferGetData(output, bytes.AsSpan()); }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 32; i++) { Execute(); device.BufferGetData(output, bytes.AsSpan()); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared multisample dispatch/readback has zero owner-thread managed allocation.");
        Execute(); device.FreeRID(alias); Check(!device.UniformSetIsValid(images), "Retiring a multisample view invalidates its borrowed uniform set.");
        device.BufferGetData(output, bytes.AsSpan()); Check(Math.Abs(MemoryMarshal.Cast<byte, float>(bytes)[0] - .125f) < .004f, "Recorded multisample reads finish before their view retires.");
    }

    private static void Graphics(RenderingDevice device, ShaderLanguage language, RID color)
    {
        var shader = Shader(device, "graphics" + language, new RDShaderSource
        {
            Language = language,
            SourceVertex = language == ShaderLanguage.GLSL ? """
                #version 450
                layout(set=0,binding=0) uniform sampler2DMS image;
                void main() {
                    vec2 p[3]=vec2[3](vec2(-1,-1),vec2(3,-1),vec2(-1,3));
                    gl_Position=vec4(p[gl_VertexIndex]+vec2(texelFetch(image,ivec2(0),0).r*4,0),0,1);
                }
                """ : """
                [[vk::binding(0,0)]] Texture2DMS<float4> image;
                float4 main(uint id:SV_VertexID):SV_Position {
                    float2 p[3]={float2(-1,-1),float2(3,-1),float2(-1,3)};
                    return float4(p[id]+float2(image.Load(int2(0,0),0).r*4,0),0,1);
                }
                """,
            SourceFragment = language == ShaderLanguage.GLSL ? """
                #version 450
                layout(set=0,binding=0) uniform sampler2DMS image;
                layout(location=0) out vec4 color;
                void main() { color=vec4(0); for(int i=0;i<textureSamples(image);i++)color+=texelFetch(image,ivec2(0),i)/float(textureSamples(image)); }
                """ : """
                [[vk::binding(0,0)]] Texture2DMS<float4> image;
                float4 main():SV_Target0 {
                    uint w,h,n; image.GetDimensions(w,h,n); float4 color=0;
                    for(uint i=0;i<n;i++)color+=image.Load(int2(0,0),i)/float(n);
                    return color;
                }
                """
        });
        using var samplerState = new RDSamplerState(); var sampler = device.SamplerCreate(samplerState);
        var images = language == ShaderLanguage.GLSL ? Set(device, shader, 0, 0, UniformType.SamplerWithTexture, sampler, color) : Set(device, shader, 0, 0, UniformType.Texture, color);
        var output = Image(device, samples: TextureSamples.Samples1); var framebuffer = device.FramebufferCreate([output]);
        using var raster = new RDPipelineRasterizationState(); using var blendAttachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [blendAttachment] };
        var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(framebuffer), -1, RenderPrimitive.Triangles, raster, blend);
        void Draw()
        {
            var list = device.DrawListBegin(framebuffer, DrawFlags.ClearColorAll, [Colors.Black]); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, images, 0); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
        }
        Draw(); var bytes = device.TextureGetData(output, 0);
        Check(bytes[0] == 0 && bytes[8] is >= 79 and <= 81 && bytes[9] is >= 63 and <= 65 && bytes[10] is >= 190 and <= 192, language + " vertex sample loads shift geometry and fragment samples perform a custom resolve.");
        if (_output is not null) { using var image = Electron2D.Image.CreateFromData(4, 4, false, Electron2D.Image.Format.Rgba8, bytes); image.SavePNG(System.IO.Path.Combine(_output, "graphics" + language + ".png")); }
        for (var i = 0; i < 8; i++) { Draw(); device.TextureGetData(output, 0, bytes.AsSpan()); }
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 32; i++) { Draw(); device.TextureGetData(output, 0, bytes.AsSpan()); }
        Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "Prepared multisample vertex/fragment draw/readback has zero owner-thread managed allocation.");
        var feedback = device.FramebufferCreate([color]); using var samples = new RDPipelineMultisampleState { SampleCount = TextureSamples.Samples4 };
        var feedbackPipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(feedback), -1, RenderPrimitive.Triangles, raster, blend, multisampleState: samples);
        var feedbackList = device.DrawListBegin(feedback, 0); device.DrawListBindRenderPipeline(feedbackList, feedbackPipeline); device.DrawListBindUniformSet(feedbackList, images, 0);
        try { Reject<InvalidOperationException>(() => device.DrawListDraw(feedbackList, false, 1, 3)); } finally { device.DrawListEnd(); }
    }
    private static void MismatchedStages(RenderingDevice device, RID multisample)
    {
        using var module = new RDShaderSPIRV();
        if (_input is not null)
        {
            module.BytecodeVertex = File.ReadAllBytes(System.IO.Path.Combine(_input, "mismatch.Vertex.spv"));
            module.BytecodeFragment = File.ReadAllBytes(System.IO.Path.Combine(_input, "mismatch.Fragment.spv"));
        }
        else
        {
            using var source = new RDShaderSource { SourceVertex = """
                #version 450
                layout(set=0,binding=0) uniform sampler2D image;
                void main(){gl_Position=vec4(texelFetch(image,ivec2(0),0).r,0,0,1);}
                """, SourceFragment = """
                #version 450
                layout(set=0,binding=0) uniform sampler2DMS image;
                layout(location=0) out vec4 color;
                void main(){color=texelFetch(image,ivec2(0),0);}
                """ };
            using var compiled = device.ShaderCompileSPIRVFromSource(source, false);
            Check(compiled.CompileErrorVertex + compiled.CompileErrorFragment == "", "Each mismatched stage is valid individually.");
            module.BytecodeVertex = compiled.BytecodeVertex; module.BytecodeFragment = compiled.BytecodeFragment;
            if (_output is not null)
            {
                File.WriteAllBytes(System.IO.Path.Combine(_output, "mismatch.Vertex.spv"), module.BytecodeVertex);
                File.WriteAllBytes(System.IO.Path.Combine(_output, "mismatch.Fragment.spv"), module.BytecodeFragment);
            }
        }
        Reject<ArgumentException>(() => device.ShaderCreateFromSPIRV(module));
        using var ordinary = new RDShaderSPIRV { BytecodeVertex = module.BytecodeVertex }; var shader = device.ShaderCreateFromSPIRV(ordinary);
        using var samplerState = new RDSamplerState(); var sampler = device.SamplerCreate(samplerState);
        Reject<ArgumentException>(() => Set(device, shader, 0, 0, UniformType.SamplerWithTexture, sampler, multisample));
    }

    private static void Numeric(RenderingDevice device)
    {
        foreach (var signed in new[] { false, true })
        {
            using var format = new RDTextureFormat { Width = 4, Height = 4, Format = signed ? DataFormat.R32SINT : DataFormat.R32UINT, Samples = TextureSamples.Samples4, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.Sampling };
            using var view = new RDTextureView(); var image = device.TextureCreate(format, view);
            var type = signed ? "ivec4" : "uvec4"; var scalar = signed ? "int" : "uint";
            var producer = Shader(device, "integer-producer" + signed, new RDShaderSource { SourceVertex = Vertex, SourceFragment = "#version 450\nlayout(location=0)out " + type + " value;\nvoid main(){value=" + type + "(" + (signed ? "-(gl_SampleID+1)*7" : "uint(gl_SampleID+1)*7u") + ");}" });
            var framebuffer = device.FramebufferCreate([image]); using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; using var multi = new RDPipelineMultisampleState { SampleCount = TextureSamples.Samples4 };
            var graphics = device.RenderPipelineCreate(producer, device.FramebufferGetFormat(framebuffer), -1, RenderPrimitive.Triangles, raster, blend, multisampleState: multi);
            var draw = device.DrawListBegin(framebuffer, 0); device.DrawListBindRenderPipeline(draw, graphics); device.DrawListDraw(draw, false, 1, 3); device.DrawListEnd();
            foreach (var language in new[] { ShaderLanguage.GLSL, ShaderLanguage.HLSL })
            {
                var source = language == ShaderLanguage.GLSL
                    ? "#version 450\nlayout(local_size_x=1)in;layout(set=0,binding=0)uniform " + (signed ? "isampler2DMS" : "usampler2DMS") + " image;layout(set=1,binding=0,std430)buffer Result{int value[];}result;void main(){for(int i=0;i<4;i++)result.value[i]=int(texelFetch(image,ivec2(0),i).r);}"
                    : "[[vk::binding(0,0)]] Texture2DMS<" + scalar + "4> image;[[vk::binding(0,1)]] RWStructuredBuffer<int> result;[numthreads(1,1,1)]void main(){for(int i=0;i<4;i++)result[i]=int(image.Load(int2(0,0),i).r);}";
                var shader = Shader(device, "integer-reader" + signed + language, new RDShaderSource { Language = language, SourceCompute = source });
                using var samplerState = new RDSamplerState { MinFilter = SamplerFilter.Linear, MagFilter = SamplerFilter.Linear }; var sampler = device.SamplerCreate(samplerState);
                var input = language == ShaderLanguage.GLSL ? Set(device, shader, 0, 0, UniformType.SamplerWithTexture, sampler, image) : Set(device, shader, 0, 0, UniformType.Texture, image);
                var output = device.StorageBufferCreate(16); var result = Set(device, shader, 1, 0, UniformType.StorageBuffer, output); var pipeline = device.ComputePipelineCreate(shader);
                var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, input, 0); device.ComputeListBindUniformSet(list, result, 1); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd();
                var data = device.BufferGetData(output); var values = MemoryMarshal.Cast<byte, int>(data);
                for (var i = 0; i < 4; i++) Check(values[i] == (signed ? -1 : 1) * (i + 1) * 7, language + " integer MSAA loads preserve sample bits and ignore filtering state.");
            }
        }
    }
}
