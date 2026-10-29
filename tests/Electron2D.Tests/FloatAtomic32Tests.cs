using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class FloatAtomic32Tests
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
        if (_output is not null) { Directory.CreateDirectory(_output); File.WriteAllBytes(System.IO.Path.Combine(_output, name + ".bin"), device.ShaderCompileBinaryFromSPIRV(module, name)); foreach (var stage in new[] { ShaderStage.Compute, ShaderStage.Vertex, ShaderStage.Fragment }) { var code = module.GetStageBytecode(stage); if (code.Length != 0) File.WriteAllBytes(System.IO.Path.Combine(_output, name + "." + stage + ".spv"), code); } }
        var before = new[] { module.BytecodeCompute, module.BytecodeVertex, module.BytecodeFragment }; var result = device.ShaderCreateFromSPIRV(module, name);
        Check(before[0].SequenceEqual(module.BytecodeCompute) && before[1].SequenceEqual(module.BytecodeVertex) && before[2].SequenceEqual(module.BytecodeFragment), "Native floating atomic adaptation preserves the original exported modules.");
        return result;
    }
    internal static void Run(string? binaryDirectory = null)
    {
        _input = binaryDirectory; _output = Environment.GetEnvironmentVariable("ELECTRON2D_FLOAT_ATOMIC_OUTPUT");
        using var device = RenderingServer.CreateLocalRenderingDevice();
        if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_FLOAT_ATOMIC_MASK") == "all") { Refusal(device); return; }
        Check(device.TextureIsFormatSupportedForUsage(DataFormat.R32SFLOAT, TextureUsageBits.Storage | TextureUsageBits.StorageAtomic), "The selected native profile must expose executable R32SFLOAT atomics.");
        BufferShared(device); Image(device); Arrays(device, false); Arrays(device, true); BaseOperations(device); Graphics(device); Addresses(device); Plain(device); Negatives(device);
        Console.WriteLine($"FP32 atomics PASS {_checks}; {(_input is null ? "source/SPIR-V" : "binary")} on {device.GetDeviceName()}");
    }
    private static void BufferShared(RenderingDevice device)
    {
        var shader = Shader(device, "buffer-shared", """
            #version 450
            #extension GL_EXT_shader_atomic_float : require
            layout(local_size_x=64) in;
            layout(set=0,binding=0,std430) buffer B { float total; float shared_sum; float exchange; uint exchange_before; uint nan_before; float old[64]; } b;
            shared float local_sum;
            void main() {
                uint id=gl_LocalInvocationIndex;
                if(id==0) local_sum=0.0;
                barrier();
                b.old[id]=atomicAdd(b.total,.25);
                atomicAdd(local_sum,.5);
                barrier();
                if(id==0) {
                    b.shared_sum=atomicExchange(local_sum,0.0);
                    b.exchange_before=floatBitsToUint(atomicExchange(b.exchange,uintBitsToFloat(0x7fc12345u)));
                    b.nan_before=floatBitsToUint(atomicExchange(b.exchange,-0.0));
                }
            }
            """);
        var initial = new uint[69]; initial[2] = BitConverter.SingleToUInt32Bits(3.25f); var data = new byte[initial.Length * 4];
        var buffer = device.StorageBufferCreate((uint)data.Length); using var uniform = new RDUniform { Binding = 0, UniformType = UniformType.StorageBuffer }; uniform.AddID(buffer); var set = device.UniformSetCreate([uniform], shader, 0); var pipeline = device.ComputePipelineCreate(shader);
        void Dispatch() { var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, set, 0); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); }
        void Execute() { device.BufferUpdate(buffer, 0, (uint)data.Length, MemoryMarshal.AsBytes(initial.AsSpan())); Dispatch(); device.BufferGetData(buffer, data.AsSpan()); }
        Execute(); var values = MemoryMarshal.Cast<byte, float>(data); var bits = MemoryMarshal.Cast<byte, uint>(data);
        Check(values[0] == 16 && values[1] == 32, "Concurrent native buffer/shared floating adds retain every contribution.");
        Check(bits[2] == 0x80000000 && bits[3] == initial[2] && bits[4] == 0x7fc12345, "Atomic exchange preserves original values, NaN payload bits and negative zero.");
        var old = values[5..].ToArray(); Array.Sort(old); Check(Enumerable.Range(0, 64).All(i => old[i] == i * .25f), "Every floating atomic add returns a unique prior value.");
        Dispatch(); device.BufferGetData(buffer, data.AsSpan()); Check(MemoryMarshal.Cast<byte, float>(data)[0] == 32, "Independent dispatches retain ordered floating updates.");
        for (var i = 0; i < 16; i++) Execute(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Execute(); var delta = GC.GetAllocatedBytesForCurrentThread() - before; Check(delta == 0, "Prepared FP32 buffer/shared cycles allocate zero owner managed bytes: " + delta);
    }
    private static void Image(RenderingDevice device)
    {
        using var format = new RDTextureFormat { Width = 4, Height = 4, Format = DataFormat.R32SFLOAT, UsageBits = TextureUsageBits.Storage | TextureUsageBits.StorageAtomic | TextureUsageBits.CanCopyFrom }; using var view = new RDTextureView(); var image = device.TextureCreate(format, view);
        var shader = Shader(device, "image", """
            #version 450
            #extension GL_EXT_shader_atomic_float : require
            layout(local_size_x=64) in;
            layout(set=0,binding=0,r32f) uniform image2D image;
            layout(set=0,binding=1,std430) buffer R { float old[64]; } r;
            void main(){uint id=gl_LocalInvocationIndex;r.old[id]=imageAtomicAdd(image,ivec2(0),.125);}
            """);
        var buffer = device.StorageBufferCreate(256); using var texture = new RDUniform { Binding = 0, UniformType = UniformType.Image }; texture.AddID(image); using var result = new RDUniform { Binding = 1, UniformType = UniformType.StorageBuffer }; result.AddID(buffer); var set = device.UniformSetCreate([texture, result], shader, 0); var pipeline = device.ComputePipelineCreate(shader);
        var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, set, 0); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd();
        var values = device.TextureGetData(image, 0); Check(MemoryMarshal.Cast<byte, float>(values)[0] == 8, "Native R32SFLOAT image atomic additions feed raw texture readback.");
        var old = MemoryMarshal.Cast<byte, float>(device.BufferGetData(buffer)).ToArray(); Array.Sort(old); Check(Enumerable.Range(0, 64).All(i => old[i] == i * .125f), "Image floating atomics return exact prior values.");
    }
}
