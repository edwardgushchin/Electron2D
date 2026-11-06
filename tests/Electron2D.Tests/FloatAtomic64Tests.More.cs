using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class FloatAtomic64Tests
{
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { _checks++; return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static RDUniform Uniform(int binding, UniformType type, params RID[] resources)
    { var value = new RDUniform { Binding = binding, UniformType = type }; foreach (var resource in resources) value.AddID(resource); return value; }
    private static void Dispatch(RenderingDevice device, RID pipeline, RID set)
    { var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, set, 0); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); }
    private static void BaseOperations(RenderingDevice device)
    {
        var shader = Shader(device, "base-operations", """
            #version 450
            #extension GL_EXT_shader_atomic_float:require
            #extension GL_KHR_memory_scope_semantics:require
            layout(local_size_x=1)in;
            layout(set=0,binding=0,std430)buffer B{double cell;double result[6];}b;
            shared double local_value;
            void main(){
                atomicStore(b.cell,-3.5LF,gl_ScopeDevice,gl_StorageSemanticsBuffer,gl_SemanticsRelease);
                b.result[0]=atomicLoad(b.cell,gl_ScopeDevice,gl_StorageSemanticsBuffer,gl_SemanticsAcquire);
                b.result[1]=atomicExchange(b.cell,3.25LF);
                atomicStore(local_value,-2.5LF,gl_ScopeWorkgroup,gl_StorageSemanticsShared,gl_SemanticsRelease);
                b.result[2]=atomicLoad(local_value,gl_ScopeWorkgroup,gl_StorageSemanticsShared,gl_SemanticsAcquire);
                b.result[3]=atomicExchange(local_value,packDouble2x32(uvec2(0,0x7ff00000u)));
                b.result[4]=atomicExchange(local_value,packDouble2x32(uvec2(1,0x80000000u)));
                b.result[5]=atomicLoad(local_value,gl_ScopeWorkgroup,gl_StorageSemanticsShared,gl_SemanticsAcquire);
            }
            """);
        var buffer = device.StorageBufferCreate(56); using var uniform = Uniform(0, UniformType.StorageBuffer, buffer); var set = device.UniformSetCreate([uniform], shader, 0); Dispatch(device, device.ComputePipelineCreate(shader), set);
        var bytes = device.BufferGetData(buffer); var values = MemoryMarshal.Cast<byte, double>(bytes);
        Check(values[..5].SequenceEqual([3.25, -3.5, -3.5, -2.5, -2.5]), "Native double load/store/exchange preserve Device/Workgroup scope and acquire/release operands.");
        Check(double.IsPositiveInfinity(values[5]) && BitConverter.ToUInt64(bytes, 48) == 0x8000000000000001, "Double base atomics preserve infinity and a negative subnormal's exact two-word representation.");
    }
    private static void AddOnly(RenderingDevice device, bool sharedOnly = false)
    {
        var shader = Shader(device, sharedOnly ? "shared-only" : "add-only", "#version 450\n#extension GL_EXT_shader_atomic_float:require\nlayout(local_size_x=64)in;layout(set=0,binding=0,std430)buffer B{double total;double shared_sum;}b;shared double local_sum;void main(){uint id=gl_LocalInvocationIndex;if(id==0)local_sum=1.0LF;barrier();" + (sharedOnly ? "" : "atomicAdd(b.total,0.0000000000000002220446049250313080847263336181640625LF);") + "atomicAdd(local_sum,0.0000000000000002220446049250313080847263336181640625LF);barrier();if(id==0){b.shared_sum=local_sum;" + (sharedOnly ? "b.total=atomicExchange(local_sum,2.25LF);" : "") + "}}");
        var initial = new[] { 1.0, 0 }; var buffer = device.StorageBufferCreate(16, MemoryMarshal.AsBytes(initial.AsSpan())); using var uniform = Uniform(0, UniformType.StorageBuffer, buffer); var set = device.UniformSetCreate([uniform], shader, 0); Dispatch(device, device.ComputePipelineCreate(shader), set);
        var values = MemoryMarshal.Cast<byte, double>(device.BufferGetData(buffer)); Check(values[0] == 1 + Math.ScaleB(64.0, -52) && values[1] == values[0], "Independent double add-only/shared-only profiles execute without unrelated atomic features.");
    }
    private static void Arrays(RenderingDevice device)
    {
        var shader = Shader(device, "buffer-arrays", """
            #version 450
            #extension GL_EXT_shader_atomic_float:require
            layout(local_size_x=64)in;
            layout(set=0,binding=0,std430)buffer B{double value[4];}b;
            layout(set=0,binding=1,std430)buffer R{double previous[64];}r;
            double add(uint index){return atomicAdd(b.value[index],.25LF);}
            void main(){uint id=gl_LocalInvocationIndex;r.previous[id]=add(id%4);}
            """);
        var buffer = device.StorageBufferCreate(32); var output = device.StorageBufferCreate(512); using var x = Uniform(0, UniformType.StorageBuffer, buffer); using var y = Uniform(1, UniformType.StorageBuffer, output); var set = device.UniformSetCreate([x, y], shader, 0); Dispatch(device, device.ComputePipelineCreate(shader), set);
        Check(MemoryMarshal.Cast<byte, double>(device.BufferGetData(buffer)).SequenceEqual([4.0, 4.0, 4.0, 4.0]), "Atomic double array access retains eight-byte stride and independent cells through a helper.");
        var values = MemoryMarshal.Cast<byte, double>(device.BufferGetData(output)).ToArray();
        for (var cell = 0; cell < 4; cell++) Check(Enumerable.Range(0, 64).Where(i => i % 4 == cell).Select(i => values[i]).Order().SequenceEqual(Enumerable.Range(0, 16).Select(i => i * .25)), "Each double array cell returns its exact prior-value sequence.");
        device.FreeRID(buffer); Check(!device.UniformSetIsValid(set), "Retiring an FP64 atomic array invalidates its borrowed descriptor set."); Reject<ArgumentException>(() => device.UniformSetCreate([x, y], shader, 0));
    }
    private static void Mixed(RenderingDevice device)
    {
        var shader = Shader(device, "mixed", """
            #version 450
            #extension GL_EXT_shader_atomic_float:require
            layout(local_size_x=64)in;
            layout(set=0,binding=0,std430)buffer B{double total;double shared_d;float single;float shared_f;}b;
            shared double d;shared float f;
            void main(){uint id=gl_LocalInvocationIndex;if(id==0){d=1.0LF;f=1.0;}barrier();
                atomicAdd(b.total,0.0000000000000002220446049250313080847263336181640625LF);atomicAdd(b.single,.25);
                atomicAdd(d,0.0000000000000002220446049250313080847263336181640625LF);atomicAdd(f,.5);barrier();
                if(id==0){b.shared_d=atomicExchange(d,0.0LF);b.shared_f=atomicExchange(f,0.0);}}
            """);
        var initial = new byte[24]; BitConverter.TryWriteBytes(initial.AsSpan(), 1.0); BitConverter.TryWriteBytes(initial.AsSpan(16), 1f); var buffer = device.StorageBufferCreate(24, initial); using var uniform = Uniform(0, UniformType.StorageBuffer, buffer); var set = device.UniformSetCreate([uniform], shader, 0); Dispatch(device, device.ComputePipelineCreate(shader), set);
        var bytes = device.BufferGetData(buffer); Check(BitConverter.ToDouble(bytes) == 1 + Math.ScaleB(64.0, -52) && BitConverter.ToDouble(bytes, 8) == BitConverter.ToDouble(bytes) && BitConverter.ToSingle(bytes, 16) == 17 && BitConverter.ToSingle(bytes, 20) == 33, "One kernel executes both independent FP32/FP64 buffer/shared feature families without changing packed offsets.");
    }
    private static void Graphics(RenderingDevice device)
    {
        var shader = Shader(device, "graphics", new RDShaderSource { SourceVertex = """
            #version 450
            #extension GL_EXT_shader_atomic_float:require
            layout(set=0,binding=0,std430)buffer B{double total;}b;
            void main(){vec2 p[3]=vec2[3](vec2(-1,-1),vec2(3,-1),vec2(-1,3));gl_Position=vec4(p[gl_VertexIndex],0,1);atomicAdd(b.total,0.0000000000009094947017729282379150390625LF);}
            """, SourceFragment = """
            #version 450
            #extension GL_EXT_shader_atomic_float:require
            layout(set=0,binding=0,std430)buffer B{double total;}b;layout(location=0)out vec4 color;
            void main(){atomicAdd(b.total,0.00000000000005684341886080801486968994140625LF);color=vec4(.25,.5,.75,1);}
            """ });
        var buffer = device.StorageBufferCreate(8, BitConverter.GetBytes(1.0)); using var uniform = Uniform(0, UniformType.StorageBuffer, buffer); var set = device.UniformSetCreate([uniform], shader, 0);
        using var format = new RDTextureFormat { Width = 4, Height = 4, Format = DataFormat.R8G8B8A8UNORM, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom }; using var view = new RDTextureView(); var target = device.TextureCreate(format, view); var framebuffer = device.FramebufferCreate([target]);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(framebuffer), -1, RenderPrimitive.Triangles, raster, blend);
        var list = device.DrawListBegin(framebuffer); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 0); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
        Check(BitConverter.ToDouble(device.BufferGetData(buffer)) == 1 + Math.ScaleB(4.0, -40), "Both writable graphics stages retain double atomic contributions below FP32 resolution.");
        var pixels = device.TextureGetData(target, 0); Check(pixels[0] is >= 63 and <= 65 && pixels[1] is >= 127 and <= 129 && pixels[2] is >= 190 and <= 192, "FP64 atomic graphics retain actual color output.");
        if (_output is not null) { using var image = Electron2D.Image.CreateFromData(4, 4, false, Electron2D.Image.Format.Rgba8, pixels); image.SavePNG(System.IO.Path.Combine(_output, "graphics.png")); }
        var reader = Shader(device, "graphics-consumer", "#version 450\nlayout(local_size_x=1)in;layout(set=0,binding=0,std430)readonly buffer B{double total;}b;layout(set=0,binding=1,std430)buffer R{float delta;}r;void main(){r.delta=float((b.total-1.0LF)*1099511627776.0LF);}");
        var output = device.StorageBufferCreate(4); using var result = Uniform(1, UniformType.StorageBuffer, output); var readerSet = device.UniformSetCreate([uniform, result], reader, 0); Dispatch(device, device.ComputePipelineCreate(reader), readerSet); Check(BitConverter.ToSingle(device.BufferGetData(output)) == 4, "A later compute consumer observes complete double graphics atomic writes.");
    }
    private static void Addresses(RenderingDevice device)
    {
        var shader = Shader(device, "addresses", """
            #version 450
            #extension GL_EXT_shader_atomic_float:require
            #extension GL_EXT_buffer_reference:require
            #extension GL_EXT_shader_explicit_arithmetic_types_int64:require
            layout(local_size_x=64)in;layout(buffer_reference,std430,buffer_reference_align=8)buffer B{double total;};
            layout(set=0,binding=0,std430)readonly buffer P{uint64_t address;}p;
            void main(){atomicAdd(B(p.address).total,0.0000000000000002220446049250313080847263336181640625LF);}
            """);
        var buffer = device.StorageBufferCreate(8, BitConverter.GetBytes(1.0), creationBits: BufferCreationBits.DeviceAddress); var pointer = device.StorageBufferCreate(8, BitConverter.GetBytes(device.BufferGetDeviceAddress(buffer))); using var uniform = Uniform(0, UniformType.StorageBuffer, pointer); var set = device.UniformSetCreate([uniform], shader, 0); Dispatch(device, device.ComputePipelineCreate(shader), set); Check(BitConverter.ToDouble(device.BufferGetData(buffer)) == 1 + Math.ScaleB(64.0, -52), "Address-enabled buffers execute FP64 atomics through physical-storage pointers.");
    }
    private static void SinglePrecision(RenderingDevice device)
    {
        var shader = Shader(device, "fp32", "#version 450\n#extension GL_EXT_shader_atomic_float:require\nlayout(local_size_x=32)in;layout(set=0,binding=0,std430)buffer B{float total;float shared_sum;}b;layout(set=0,binding=1,r32f)uniform image2D image;shared float s;void main(){uint id=gl_LocalInvocationIndex;if(id==0)s=0.0;barrier();atomicAdd(b.total,.25);atomicAdd(s,.5);imageAtomicAdd(image,ivec2(0),.125);barrier();if(id==0)b.shared_sum=atomicExchange(s,0.0);}");
        var buffer = device.StorageBufferCreate(8); using var format = new RDTextureFormat { Format = DataFormat.R32SFLOAT, UsageBits = TextureUsageBits.Storage | TextureUsageBits.StorageAtomic | TextureUsageBits.CanCopyFrom }; using var view = new RDTextureView(); var image = device.TextureCreate(format, view); using var x = Uniform(0, UniformType.StorageBuffer, buffer); using var y = Uniform(1, UniformType.Image, image); var set = device.UniformSetCreate([x, y], shader, 0); Dispatch(device, device.ComputePipelineCreate(shader), set);
        Check(MemoryMarshal.Cast<byte, float>(device.BufferGetData(buffer)).SequenceEqual([8f, 16f]) && BitConverter.ToSingle(device.TextureGetData(image, 0)) == 4, "Existing FP32 buffer/shared/image atomics survive independent FP64 refusal or native ABI fallback.");
    }
    private static void PlainDouble(RenderingDevice device)
    {
        var shader = Shader(device, "plain-double", "#version 450\nlayout(local_size_x=1)in;layout(set=0,binding=0,std430)buffer B{double value;}b;void main(){b.value=1.0LF+0.0000000000009094947017729282379150390625LF;}"); var buffer = device.StorageBufferCreate(8); using var uniform = Uniform(0, UniformType.StorageBuffer, buffer); var set = device.UniformSetCreate([uniform], shader, 0); Dispatch(device, device.ComputePipelineCreate(shader), set); Check(BitConverter.ToDouble(device.BufferGetData(buffer)) == 1 + Math.ScaleB(1.0, -40), "Ordinary double arithmetic/storage remain available after optional FP64 atomic refusal.");
    }
    private static void Refusal(RenderingDevice device, string mode)
    {
        if (_input is null) throw new InvalidOperationException("Feature profiles require compiled public shader fixtures.");
        var rejected = mode switch { "base" => new[] { "buffer-shared", "add-only", "shared-only", "mixed", "buffer-arrays", "graphics", "addresses", "unused" }, "add" => ["base-operations", "buffer-shared", "shared-only", "mixed"], "buffer" => ["base-operations", "buffer-shared", "add-only", "shared-only", "mixed"], "shared" => ["base-operations", "buffer-shared", "add-only", "mixed", "buffer-arrays", "graphics", "addresses"], _ => ["base-operations", "buffer-shared", "add-only", "shared-only", "mixed", "buffer-arrays", "graphics", "addresses", "unused"] };
        foreach (var name in rejected)
        {
            Reject<NotSupportedException>(() => device.ShaderCreateFromBytecode(File.ReadAllBytes(System.IO.Path.Combine(_input, name + ".bin"))));
            using var code = new RDShaderSPIRV(); foreach (var stage in new[] { ShaderStage.Compute, ShaderStage.Vertex, ShaderStage.Fragment }) { var path = System.IO.Path.Combine(_input, name + "." + stage + ".spv"); if (File.Exists(path)) code.SetStageBytecode(stage, File.ReadAllBytes(path)); }
            Reject<NotSupportedException>(() => device.ShaderCreateFromSPIRV(code)); Reject<NotSupportedException>(() => device.ShaderCompileBinaryFromSPIRV(code));
        }
        if (mode == "base") BaseOperations(device);
        if (mode == "add") { AddOnly(device); Arrays(device); Unused(device); }
        if (mode == "buffer") { Arrays(device); Addresses(device); Unused(device); }
        if (mode == "shared") { AddOnly(device, true); Unused(device); }
        SinglePrecision(device);
        if (mode == "precision") Reject<NotSupportedException>(() => Shader(device, "plain-double", "")); else PlainDouble(device);
        Console.WriteLine($"FP64 atomic profile {mode} PASS {_checks}");
    }
    private static void Unused(RenderingDevice device)
    {
        RID shader;
        if (_input is not null) shader = device.ShaderCreateFromBytecode(File.ReadAllBytes(System.IO.Path.Combine(_input, "unused.bin")));
        else
        {
            using var source = new RDShaderSource { SourceCompute = "#version 450\nlayout(local_size_x=1)in;void main(){}" }; using var original = device.ShaderCompileSPIRVFromSource(source, false); Check(original.CompileErrorCompute == "", original.CompileErrorCompute);
            var words = MemoryMarshal.Cast<byte, uint>(original.BytecodeCompute.AsSpan()).ToArray(); var at = 5; while ((words[at] & 65535) == 17) at += (int)(words[at] >> 16);
            var extension = MemoryMarshal.Cast<byte, uint>("SPV_EXT_shader_atomic_float_add\0"u8).ToArray(); var patched = words.Take(at).Concat(new uint[] { 2u << 16 | 17, 10, 2u << 16 | 17, 6034, (uint)(extension.Length + 1) << 16 | 10 }).Concat(extension).Concat(words.Skip(at)).ToArray();
            using var module = new RDShaderSPIRV { BytecodeCompute = MemoryMarshal.AsBytes(patched.AsSpan()).ToArray() };
            if (_output is not null) { File.WriteAllBytes(System.IO.Path.Combine(_output, "unused.Compute.spv"), module.BytecodeCompute); File.WriteAllBytes(System.IO.Path.Combine(_output, "unused.bin"), device.ShaderCompileBinaryFromSPIRV(module)); }
            shader = device.ShaderCreateFromSPIRV(module);
        }
        var pipeline = device.ComputePipelineCreate(shader); var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); device.Submit(); device.Sync(); Check(shader != default, "A declared but unused AtomicFloat64Add capability executes only with its enabled logical add feature.");
    }
}
