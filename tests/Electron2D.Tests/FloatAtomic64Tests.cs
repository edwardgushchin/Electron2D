using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class FloatAtomic64Tests
{
    private static int _checks;
    private static string? _input, _output;
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); _checks++; }
    private static RID Shader(RenderingDevice device, string name, string compute) => Shader(device, name, new RDShaderSource { SourceCompute = compute });
    private static RID Shader(RenderingDevice device, string name, RDShaderSource source)
    {
        using var owned = source;
        if (_input is not null) return device.ShaderCreateFromBytecode(File.ReadAllBytes(System.IO.Path.Combine(_input, name + ".bin")));
        using var module = device.ShaderCompileSPIRVFromSource(source, false);
        Check(module.CompileErrorCompute + module.CompileErrorVertex + module.CompileErrorFragment == "", name + module.CompileErrorCompute + module.CompileErrorVertex + module.CompileErrorFragment);
        if (_output is not null) { Directory.CreateDirectory(_output); File.WriteAllBytes(System.IO.Path.Combine(_output, name + ".bin"), device.ShaderCompileBinaryFromSPIRV(module, name)); foreach (var stage in new[] { ShaderStage.Compute, ShaderStage.Vertex, ShaderStage.Fragment }) { var bytes = module.GetStageBytecode(stage); if (bytes.Length != 0) File.WriteAllBytes(System.IO.Path.Combine(_output, name + "." + stage + ".spv"), bytes); } }
        var before = new[] { module.BytecodeCompute, module.BytecodeVertex, module.BytecodeFragment }; var shader = device.ShaderCreateFromSPIRV(module, name);
        Check(before[0].SequenceEqual(module.BytecodeCompute) && before[1].SequenceEqual(module.BytecodeVertex) && before[2].SequenceEqual(module.BytecodeFragment), "FP64 atomic adaptation preserves original exported modules.");
        return shader;
    }
    internal static void Run(string? binaryDirectory = null)
    {
        _input = binaryDirectory; _output = Environment.GetEnvironmentVariable("ELECTRON2D_FLOAT_ATOMIC64_OUTPUT");
        using var device = RenderingServer.CreateLocalRenderingDevice();
        if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_FLOAT_ATOMIC_MASK") is { } mode && mode is "wide" or "precision" or "base" or "add" or "buffer" or "shared") { Refusal(device, mode); return; }
        BufferShared(device); BaseOperations(device); AddOnly(device); AddOnly(device, true); Arrays(device); Mixed(device); Graphics(device); Addresses(device); SinglePrecision(device); PlainDouble(device); Unused(device);
        Console.WriteLine($"FP64 atomics PASS {_checks}; {(_input is null ? "source/SPIR-V" : "binary")} on {device.GetDeviceName()}");
    }
    private static void BufferShared(RenderingDevice device)
    {
        var shader = Shader(device, "buffer-shared", """
            #version 450
            #extension GL_EXT_shader_atomic_float:require
            layout(local_size_x=64)in;
            layout(constant_id=0)const double increment=0.0000000000000002220446049250313080847263336181640625LF;
            layout(set=0,binding=0,std430)buffer B{double total;double shared_sum;double exchange;uvec2 exchange_before;uvec2 nan_before;double old[64];}b;
            shared double local_sum;
            void main(){uint id=gl_LocalInvocationIndex;if(id==0)local_sum=1.0LF;barrier();
                b.old[id]=atomicAdd(b.total,increment);atomicAdd(local_sum,increment);barrier();
                if(id==0){b.shared_sum=atomicExchange(local_sum,0.0LF);
                    b.exchange_before=unpackDouble2x32(atomicExchange(b.exchange,packDouble2x32(uvec2(0x89abcdefu,0x7ff81234u))));
                    b.nan_before=unpackDouble2x32(atomicExchange(b.exchange,-0.0LF));}}
            """);
        var initial = new ulong[69]; initial[0] = BitConverter.DoubleToUInt64Bits(1); initial[2] = BitConverter.DoubleToUInt64Bits(3.25); var data = new byte[initial.Length * 8];
        var buffer = device.StorageBufferCreate((uint)data.Length); using var value = new RDUniform { Binding = 0, UniformType = UniformType.StorageBuffer }; value.AddID(buffer); var set = device.UniformSetCreate([value], shader, 0); var pipeline = device.ComputePipelineCreate(shader);
        void Dispatch(RID selected) { var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, selected); device.ComputeListBindUniformSet(list, set, 0); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); }
        void Execute() { device.BufferUpdate(buffer, 0, (uint)data.Length, MemoryMarshal.AsBytes(initial.AsSpan())); Dispatch(pipeline); device.BufferGetData(buffer, data.AsSpan()); }
        Execute(); var values = MemoryMarshal.Cast<byte, double>(data); var bits = MemoryMarshal.Cast<byte, ulong>(data); var epsilon = Math.ScaleB(1.0, -52);
        Check(values[0] == 1 + 64 * epsilon && values[1] == 1 + 64 * epsilon, "Contending native FP64 buffer/shared adds retain increments below FP32 precision.");
        Check(bits[2] == 0x8000000000000000 && bits[3] == initial[2] && bits[4] == 0x7ff8123489abcdef, "Double exchange preserves both words, NaN payload and signed zero without Int64 shader arithmetic.");
        var old = values[5..].ToArray(); Array.Sort(old); Check(Enumerable.Range(0, 64).All(i => old[i] == 1 + i * epsilon), "Each double atomic add returns one exact distinct prior value.");
        Dispatch(pipeline); device.BufferGetData(buffer, data.AsSpan()); Check(MemoryMarshal.Cast<byte, double>(data)[0] == 1 + 128 * epsilon, "Independent dispatches order the same FP64 accumulator.");
        using var increment = new RDPipelineSpecializationConstant<double> { ConstantID = 0, Value = Math.ScaleB(1.0, -50) }; var specialized = device.ComputePipelineCreate(shader, [increment]);
        device.BufferUpdate(buffer, 0, (uint)data.Length, MemoryMarshal.AsBytes(initial.AsSpan())); Dispatch(specialized); device.BufferGetData(buffer, data.AsSpan()); Check(MemoryMarshal.Cast<byte, double>(data)[0] == 1 + 256 * epsilon, "Typed double specialization reaches native atomic operands without narrowing.");
        for (var i = 0; i < 16; i++) Execute(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Execute(); var delta = GC.GetAllocatedBytesForCurrentThread() - before; Check(delta == 0, "Prepared double atomic cycles allocate zero owner managed bytes: " + delta);
    }
}
