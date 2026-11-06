using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class FloatAtomic32Tests
{
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { _checks++; return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static RDUniform Uniform(int binding, UniformType type, params RID[] ids)
    { var value = new RDUniform { Binding = binding, UniformType = type }; foreach (var id in ids) value.AddID(id); return value; }
    private static RID Texture(RenderingDevice device, bool samples = false, bool array = false)
    {
        using var format = new RDTextureFormat { Width = 4, Height = 4, Format = DataFormat.R32SFLOAT, TextureType = array ? TextureType.TextureArray : TextureType.Texture, ArrayLayers = array ? 2u : 1u, Samples = samples ? TextureSamples.Samples4 : TextureSamples.Samples1, UsageBits = TextureUsageBits.Storage | TextureUsageBits.StorageAtomic | TextureUsageBits.CanCopyFrom };
        using var view = new RDTextureView(); return device.TextureCreate(format, view);
    }
    private static void Dispatch(RenderingDevice device, RID pipeline, RID set, uint count = 1)
    { var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, set, 0); device.ComputeListDispatch(list, count, 1, 1); device.ComputeListEnd(); }
    private static void Arrays(RenderingDevice device, bool samples)
    {
        var name = samples ? "multisample-arrays" : "arrays"; var shape = samples ? "MS" : ""; var sample = samples ? ",int(id%4)" : "";
        var shader = Shader(device, name, "#version 450\n#extension GL_EXT_shader_atomic_float:require\nlayout(local_size_x=64)in;layout(set=0,binding=0,r32f)uniform coherent image2D" + shape + " images[2];layout(set=0,binding=1,r32f)uniform image2D" + shape + "Array layers;layout(set=0,binding=2,std430)buffer R{float old[64];float invalid;float layer[8];}r;float add(uint id){return imageAtomicAdd(images[id%2],ivec2(0)" + sample + ",.25);}void main(){uint id=gl_LocalInvocationIndex;r.old[id]=add(id);imageAtomicAdd(layers,ivec3(1,1,int(id%2))" + sample + ",.5);if(id==0)r.invalid=imageAtomicAdd(images[gl_WorkGroupID.x+2],ivec2(0)" + sample + ",99.0);barrier();memoryBarrierImage();if(id<8)r.layer[id]=imageLoad(layers,ivec3(1,1,int(id%2))" + (samples ? ",int(id/2)" : "") + ").r;}");
        var a = Texture(device, samples); var b = Texture(device, samples); var layers = Texture(device, samples, true); var output = device.StorageBufferCreate(73 * 4);
        using var x = Uniform(0, UniformType.Image, a, b); using var y = Uniform(1, UniformType.Image, layers); using var z = Uniform(2, UniformType.StorageBuffer, output); var set = device.UniformSetCreate([x, y, z], shader, 0); var pipeline = device.ComputePipelineCreate(shader);
        Dispatch(device, pipeline, set); var data = new byte[73 * 4]; device.BufferGetData(output, data.AsSpan()); var values = MemoryMarshal.Cast<byte, float>(data);
        Check(values[64] == 0, "Invalid floating image descriptor selection returns zero without touching another image.");
        for (var index = 0; index < (samples ? 4 : 2); index++)
        {
            var selected = Enumerable.Range(0, 64).Where(i => i % (samples ? 4 : 2) == index).Select(i => ValuesToFloat(data, i)).Order().ToArray();
            Check(selected.SequenceEqual(Enumerable.Range(0, selected.Length).Select(i => i * .25f)), "Descriptor/layer/sample selection preserves each atomic prior value.");
        }
        for (var i = 0; i < 8; i++) Check(values[65 + i] == (samples ? (i / 2 % 2 == i % 2 ? 8 : 0) : 16), "Array atomics affect only the selected layer and sample.");
        var before = device.GetMemoryUsage(MemoryType.Textures); device.FreeRID(a); Check(!device.UniformSetIsValid(set), "Retiring an atomic descriptor invalidates its set.");
        Check(device.GetMemoryUsage(MemoryType.Textures) <= before, "Atomic retirement releases retained texture storage after completion.");
        static float ValuesToFloat(byte[] data, int index) => BitConverter.ToSingle(data, index * 4);
    }
    private static void BaseOperations(RenderingDevice device)
    {
        var shader = Shader(device, "base-operations", """
            #version 450
            #extension GL_EXT_shader_atomic_float:require
            #extension GL_KHR_memory_scope_semantics:require
            layout(local_size_x=1)in;
            layout(set=0,binding=0,r32f)uniform coherent volatile image2D images[2];
            layout(set=0,binding=1,std430)buffer R{float cell;float result[7];}r;
            shared float local_value;
            void main(){
                atomicStore(r.cell,-3.5,gl_ScopeDevice,gl_StorageSemanticsBuffer,gl_SemanticsRelease);
                r.result[0]=atomicLoad(r.cell,gl_ScopeDevice,gl_StorageSemanticsBuffer,gl_SemanticsAcquire);
                atomicStore(local_value,2.25,gl_ScopeWorkgroup,gl_StorageSemanticsShared,gl_SemanticsRelease);
                r.result[1]=atomicLoad(local_value,gl_ScopeWorkgroup,gl_StorageSemanticsShared,gl_SemanticsAcquire);
                for(int i=0;i<2;i++){
                    imageAtomicStore(images[i],ivec2(0),float(i)+.5,gl_ScopeDevice,gl_StorageSemanticsImage,gl_SemanticsRelease);
                    r.result[2+i*2]=imageAtomicLoad(images[i],ivec2(0),gl_ScopeDevice,gl_StorageSemanticsImage,gl_SemanticsAcquire);
                    r.result[3+i*2]=imageAtomicExchange(images[i],ivec2(0),4.5);
                }
                r.result[6]=imageAtomicLoad(images[gl_WorkGroupID.x+2],ivec2(0),gl_ScopeDevice,gl_StorageSemanticsImage,gl_SemanticsAcquire);
            }
            """);
        var a = Texture(device); var b = Texture(device); var output = device.StorageBufferCreate(32); using var x = Uniform(0, UniformType.Image, a, b); using var y = Uniform(1, UniformType.StorageBuffer, output); var set = device.UniformSetCreate([x, y], shader, 0); Dispatch(device, device.ComputePipelineCreate(shader), set);
        Check(MemoryMarshal.Cast<byte, float>(device.BufferGetData(output)).SequenceEqual([-3.5f, -3.5f, 2.25f, .5f, .5f, 1.5f, 1.5f, 0]), "Native floating atomic load/store/exchange preserve Device/Workgroup scope and acquire/release memory operands through descriptor lowering.");
    }
    private static void Graphics(RenderingDevice device)
    {
        var shader = Shader(device, "graphics", new RDShaderSource { SourceVertex = """
            #version 450
            #extension GL_EXT_shader_atomic_float:require
            layout(set=0,binding=0,std430)buffer B{float total;}b;
            layout(set=0,binding=1,r32f)uniform image2D image;
            void main(){vec2 p[3]=vec2[3](vec2(-1,-1),vec2(3,-1),vec2(-1,3));gl_Position=vec4(p[gl_VertexIndex],0,1);atomicAdd(b.total,.5);imageAtomicAdd(image,ivec2(0),.25);}
            """, SourceFragment = """
            #version 450
            #extension GL_EXT_shader_atomic_float:require
            layout(set=0,binding=0,std430)buffer B{float total;}b;
            layout(set=0,binding=1,r32f)uniform image2D image;
            layout(location=0)out vec4 color;
            void main(){atomicAdd(b.total,.125);imageAtomicAdd(image,ivec2(0),.0625);color=vec4(.25,.5,.75,1);}
            """ });
        var buffer = device.StorageBufferCreate(4); var image = Texture(device); using var x = Uniform(0, UniformType.StorageBuffer, buffer); using var y = Uniform(1, UniformType.Image, image); var set = device.UniformSetCreate([x, y], shader, 0);
        using var format = new RDTextureFormat { Width = 4, Height = 4, Format = DataFormat.R8G8B8A8UNORM, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom }; using var view = new RDTextureView(); var target = device.TextureCreate(format, view); var framebuffer = device.FramebufferCreate([target]);
        using var raster = new RDPipelineRasterizationState(); using var blendAttachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [blendAttachment] }; var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(framebuffer), -1, RenderPrimitive.Triangles, raster, blend);
        var list = device.DrawListBegin(framebuffer); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 0); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
        Check(BitConverter.ToSingle(device.BufferGetData(buffer)) == 3.5f && BitConverter.ToSingle(device.TextureGetData(image, 0)) == 1.75f, "Both graphics stages atomically accumulate into the same buffer and image, visible to later reads.");
        var pixels = device.TextureGetData(target, 0); Check(pixels[0] is >= 63 and <= 65 && pixels[1] is >= 127 and <= 129 && pixels[2] is >= 190 and <= 192, "Floating atomic graphics retain real rendered color output.");
        if (_output is not null) { using var frame = Electron2D.Image.CreateFromData(4, 4, false, Electron2D.Image.Format.Rgba8, pixels); frame.SavePNG(System.IO.Path.Combine(_output, "graphics.png")); }
    }
    private static void Addresses(RenderingDevice device)
    {
        var shader = Shader(device, "addresses", """
            #version 450
            #extension GL_EXT_shader_atomic_float:require
            #extension GL_EXT_buffer_reference:require
            #extension GL_EXT_shader_explicit_arithmetic_types_int64:require
            layout(local_size_x=64)in;
            layout(buffer_reference,std430,buffer_reference_align=4)buffer B{float total;};
            layout(set=0,binding=0,std430)readonly buffer P{uint64_t address;}p;
            void main(){atomicAdd(B(p.address).total,.25);}
            """);
        var buffer = device.StorageBufferCreate(4, creationBits: BufferCreationBits.DeviceAddress); var address = BitConverter.GetBytes(device.BufferGetDeviceAddress(buffer)); var pointer = device.StorageBufferCreate(8, address); using var uniform = Uniform(0, UniformType.StorageBuffer, pointer); var set = device.UniformSetCreate([uniform], shader, 0); Dispatch(device, device.ComputePipelineCreate(shader), set);
        Check(BitConverter.ToSingle(device.BufferGetData(buffer)) == 16, "Physical-storage FP32 atomics require and execute the same buffer feature on an address-enabled allocation.");
    }
    private static void Plain(RenderingDevice device)
    {
        var shader = Shader(device, "plain", "#version 450\nlayout(local_size_x=1)in;layout(set=0,binding=0,std430)buffer R{float v;}r;void main(){r.v=4.25;}");
        var buffer = device.StorageBufferCreate(4); using var value = Uniform(0, UniformType.StorageBuffer, buffer); var set = device.UniformSetCreate([value], shader, 0); Dispatch(device, device.ComputePipelineCreate(shader), set);
        Check(BitConverter.ToSingle(device.BufferGetData(buffer)) == 4.25f, "Ordinary FP32 compute executes independently of optional atomics.");
    }
    private static void Negatives(RenderingDevice device)
    {
        Check(!device.TextureIsFormatSupportedForUsage(DataFormat.R16SFLOAT, TextureUsageBits.Storage | TextureUsageBits.StorageAtomic), "Half image atomics retain their separate format feature gate.");
        if (_input is not null)
        {
            foreach (var name in new[] { "texel-add", "texel-exchange" })
            {
                using var code = new RDShaderSPIRV { BytecodeCompute = File.ReadAllBytes(System.IO.Path.Combine(_input, "negative", name + ".spv")) };
                Reject<NotSupportedException>(() => device.ShaderCreateFromSPIRV(code));
                Reject<NotSupportedException>(() => device.ShaderCompileBinaryFromSPIRV(code));
            }
            return;
        }
        foreach (var operation in new[] { "imageAtomicAdd(cell,0,.5)", "imageAtomicExchange(cell,0,.5)" })
        {
            using var source = new RDShaderSource { SourceCompute = "#version 450\n#extension GL_EXT_shader_atomic_float:require\nlayout(local_size_x=1)in;layout(set=0,binding=0,r32f)uniform imageBuffer cell;void main(){" + operation + ";}" };
            using var module = device.ShaderCompileSPIRVFromSource(source, false);
            Check(module.CompileErrorCompute.Contains("Texel atomics require", StringComparison.Ordinal), "The source adapter refuses unsupported formatted floating atomics before publishing bytecode: " + module.CompileErrorCompute);
            using var ordinary = new RDShaderSource { SourceCompute = "#version 450\n#extension GL_EXT_shader_atomic_float:require\nlayout(local_size_x=1)in;layout(set=0,binding=0,r32f)uniform image2D cell;void main(){" + operation.Replace("cell,0,", "cell,ivec2(0),", StringComparison.Ordinal) + ";}" };
            using var original = device.ShaderCompileSPIRVFromSource(ordinary, false); Check(original.CompileErrorCompute == "", original.CompileErrorCompute);
            var name = operation.StartsWith("imageAtomicAdd", StringComparison.Ordinal) ? "texel-add" : "texel-exchange";
            var words = MemoryMarshal.Cast<byte, uint>(original.BytecodeCompute.AsSpan()).ToArray(); var integers = new HashSet<uint>(); uint zero = 0;
            for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
            { var op = words[at] & 65535; if (op == 21 && words[at + 2] == 32) integers.Add(words[at + 1]); if (op == 43 && integers.Contains(words[at + 1]) && words[at + 3] == 0) zero = words[at + 2]; }
            Check(zero != 0, "The negative texel module reuses its existing integer-zero coordinate.");
            for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
            { var op = words[at] & 65535; if (op == 25) words[at + 3] = 5; if (op == 60) words[at + 4] = zero; }
            // Valid original ImageBuffer modules exercise runtime refusal after the source adapter has already refused authoring.
            var imageBuffer = words.Take(5).Concat(new uint[] { 2u << 16 | 17, 47 }).Concat(words.Skip(5)).ToArray(); var bytes = MemoryMarshal.AsBytes(imageBuffer.AsSpan()).ToArray();
            if (_output is not null) { var folder = System.IO.Path.Combine(_output, "negative"); Directory.CreateDirectory(folder); File.WriteAllBytes(System.IO.Path.Combine(folder, name + ".spv"), bytes); }
            using var raw = new RDShaderSPIRV { BytecodeCompute = bytes }; Reject<NotSupportedException>(() => device.ShaderCreateFromSPIRV(raw)); Reject<NotSupportedException>(() => device.ShaderCompileBinaryFromSPIRV(raw));
        }
        foreach (var (extension, declaration, operation) in new[] {
            ("GL_EXT_shader_atomic_float2", "double cell;", "atomicMin(b.cell,.5lf)"),
            ("GL_EXT_shader_atomic_float2", "float cell;", "atomicMin(b.cell,.5)") })
        {
            using var source = new RDShaderSource { SourceCompute = "#version 450\n#extension " + extension + ":require\nlayout(local_size_x=1)in;layout(set=0,binding=0,std430)buffer B{" + declaration + "}b;void main(){" + operation + ";}" };
            using var module = device.ShaderCompileSPIRVFromSource(source, false); Check(module.CompileErrorCompute == "", module.CompileErrorCompute);
            Reject<NotSupportedException>(() => device.ShaderCreateFromSPIRV(module));
            Reject<NotSupportedException>(() => device.ShaderCompileBinaryFromSPIRV(module));
        }
    }
    private static void Refusal(RenderingDevice device)
    {
        Check(!device.TextureIsFormatSupportedForUsage(DataFormat.R32SFLOAT, TextureUsageBits.Storage | TextureUsageBits.StorageAtomic), "A refused logical feature cannot advertise floating image atomics.");
        Reject<NotSupportedException>(() => Texture(device));
        using var ordinary = new RDTextureFormat { Format = DataFormat.R32SFLOAT, UsageBits = TextureUsageBits.Storage }; using var view = new RDTextureView(); Check(device.TextureCreate(ordinary, view) != default, "Ordinary FP32 storage remains available after atomic refusal.");
        if (_input is null) throw new InvalidOperationException("Refusal requires compiled public shader binaries.");
        foreach (var name in new[] { "buffer-shared", "image", "arrays", "base-operations", "graphics", "addresses" })
        {
            Reject<NotSupportedException>(() => device.ShaderCreateFromBytecode(File.ReadAllBytes(System.IO.Path.Combine(_input, name + ".bin"))));
            using var code = new RDShaderSPIRV();
            foreach (var stage in new[] { ShaderStage.Compute, ShaderStage.Vertex, ShaderStage.Fragment }) { var path = System.IO.Path.Combine(_input, name + "." + stage + ".spv"); if (File.Exists(path)) code.SetStageBytecode(stage, File.ReadAllBytes(path)); }
            Reject<NotSupportedException>(() => device.ShaderCreateFromSPIRV(code));
            Reject<NotSupportedException>(() => device.ShaderCompileBinaryFromSPIRV(code));
        }
        Plain(device);
        Console.WriteLine($"FP32 atomic refusal PASS {_checks}");
    }
}
