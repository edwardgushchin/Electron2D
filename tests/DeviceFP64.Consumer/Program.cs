using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

try { FP64.Run(args.Length == 0 ? "managed" : args[0]); return 0; }
catch (Exception error) { Console.Error.WriteLine(error); return 1; }

internal static partial class FP64
{
    private const double Scale = 1099511627776d;
    internal static void Run(string mode)
    {
        Managed();
        if (mode == "managed") return;
        if (mode == "canvas") { Canvas(); return; }
        using var device = RenderingServer.CreateLocalRenderingDevice();
        Console.WriteLine("Native FP64 device: " + device.GetDeviceName());
        if (mode == "unavailable")
        {
            using var code = Code("data.comp.glsl"); Reject<NotSupportedException>(() => device.ShaderCreateFromSPIRV(code)); Reject<NotSupportedException>(() => device.ShaderCompileBinaryFromSPIRV(code));
            using var ordinary = Code("ordinary.comp.glsl"); var shader = device.ShaderCreateFromSPIRV(ordinary); var pipeline = device.ComputePipelineCreate(shader); var buffer = device.StorageBufferCreate(16);
            using var value = new RDUniform { Binding = 0, UniformType = UniformType.StorageBuffer }; value.AddID(buffer); var set = device.UniformSetCreate([value], shader, 0);
            var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, set, 0); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd();
            Check(MemoryMarshal.Cast<byte, float>(device.BufferGetData(buffer).AsSpan())[0] == 3.25f, "Ordinary native compute survives optional FP64 refusal.");
            Console.WriteLine("PASS explicit unavailable FP64 and executing ordinary fallback."); return;
        }
        if (mode == "graphics") { Graphics(device, "glsl"); Graphics(device, "hlsl"); return; }
        Check(mode == "compute", "Known profile.");
        Compute(device, "data.comp.glsl", true); Compute(device, "data-column.comp.glsl", false); Compute(device, "data.comp.hlsl", true);
    }

    private static void Managed()
    {
        using var value = new RDPipelineSpecializationConstant<double>(); Check(value.Value is null, "Double descriptions start unassigned.");
        var descriptor = value.GetPropertyList().OfType<PropertyDescriptor<RDPipelineSpecializationConstant<double>, double?>>().Single(p => p.Name == "Value");
        foreach (var bits in new ulong[] { 0, 0x8000000000000000, 0x3ff0000000000001, 0x7ff0000000000000, 0xfff0000000000000, 0x7ff8123456789abc })
        {
            descriptor.SetValue(value, BitConverter.UInt64BitsToDouble(bits));
            Check(BitConverter.DoubleToUInt64Bits(descriptor.GetValue(value)!.Value) == bits, "Typed metadata retains all double bits.");
        }
        value.RevertProperty(descriptor); Check(value.Value is null, "Double metadata reverts to unassigned.");
        Reject<NotSupportedException>(() => new RDPipelineSpecializationConstant<byte>());
        value.Dispose(); Reject<ObjectDisposedException>(() => _ = value.Value);
        Console.WriteLine("PASS Double metadata, signed zero/NaN/infinity bits and lifecycle.");
    }

    private static RDShaderSPIRV Code(string name) => new() { BytecodeCompute = File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", name + ".spv")) };
    private static void Compute(RenderingDevice device, string name, bool rowMajor)
    {
        using var code = Code(name); var shader = device.ShaderCreateFromBytecode(device.ShaderCompileBinaryFromSPIRV(code));
        var hlsl = name.EndsWith("hlsl", StringComparison.Ordinal); var chosenFactor = hlsl ? 3 : 3 + 1 / Scale;
        using RDPipelineSpecializationConstant factor = hlsl ? new RDPipelineSpecializationConstant<float> { ConstantID = 0, Value = 3 } : new RDPipelineSpecializationConstant<double> { ConstantID = 0, Value = chosenFactor };
        using var choose = new RDPipelineSpecializationConstant<bool> { ConstantID = 1, Value = false };
        var ordinary = device.ComputePipelineCreate(shader); var custom = device.ComputePipelineCreate(shader, [factor, choose]);
        using RDPipelineSpecializationConstant wrong = hlsl ? new RDPipelineSpecializationConstant<double> { ConstantID = 0, Value = 3 } : new RDPipelineSpecializationConstant<float> { ConstantID = 0, Value = 3 };
        Reject<ArgumentException>(() => device.ComputePipelineCreate(shader, [wrong]));
        using var empty = new RDPipelineSpecializationConstant<double>(); Reject<ArgumentException>(() => device.ComputePipelineCreate(shader, [empty]));
        Reject<ArgumentException>(() => device.ComputePipelineCreate(shader, [factor, factor]));
        choose.Value = true; factor.Dispose(); choose.Dispose();
        Reject<ObjectDisposedException>(() => device.ComputePipelineCreate(shader, [factor]));
        double[] inputs = [1 + 1 / Scale, 1 + 2 / Scale, 1 + 3 / Scale, 1 + 4 / Scale];
        double[] bias = [.125, -.25]; var input = device.StorageBufferCreate(32, MemoryMarshal.AsBytes(inputs.AsSpan())); var output = device.StorageBufferCreate(48); var uniform = device.UniformBufferCreate(16, MemoryMarshal.AsBytes(bias.AsSpan()));
        using var source = new RDUniform { Binding = 0, UniformType = UniformType.StorageBuffer }; source.AddID(input);
        using var destination = new RDUniform { Binding = 1, UniformType = UniformType.StorageBuffer }; destination.AddID(output);
        using var parameters = new RDUniform { Binding = 0, UniformType = UniformType.UniformBuffer }; parameters.AddID(uniform);
        var buffers = device.UniformSetCreate([source, destination], shader, 0); var uniforms = device.UniformSetCreate([parameters], shader, 1);
        var shortBuffer = device.UniformBufferCreate(8); using var shortUniform = new RDUniform { Binding = 0, UniformType = UniformType.UniformBuffer }; shortUniform.AddID(shortBuffer); var shortSet = device.UniformSetCreate([shortUniform], shader, 1);
        var push = new byte[128];
        void Put(int offset, double v) => Check(BitConverter.TryWriteBytes(push.AsSpan(offset), v), "Push fixture capacity.");
        Put(0, .5); Put(16, 1.25); Put(24, 2.5); double[] matrix = [3.25, 4.5, 5.75, 6]; for (var i = 0; i < 4; i++) Put(32 + i * 8, matrix[i]);
        Put(64, 7.25); Put(72, 8.5); Put(80, 9.75); BinaryPrimitives.WriteSingleLittleEndian(push.AsSpan(88), 10.5f); BinaryPrimitives.WriteUInt32LittleEndian(push.AsSpan(92), 11); Put(120, 12.25);
        var originalPush = push.ToArray(); var readback = new byte[48];
        var invalidList = device.ComputeListBegin(); device.ComputeListBindComputePipeline(invalidList, custom); device.ComputeListBindUniformSet(invalidList, buffers, 0); device.ComputeListSetPushConstant(invalidList, push, 128);
        Reject<ArgumentException>(() => { device.ComputeListBindUniformSet(invalidList, shortSet, 1); device.ComputeListDispatch(invalidList, 4, 1, 1); }); device.ComputeListEnd();
        void Dispatch(RID pipeline)
        {
            device.BufferUpdate(input, 0, 32, MemoryMarshal.AsBytes(inputs.AsSpan()));
            var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, buffers, 0); device.ComputeListBindUniformSet(list, uniforms, 1); device.ComputeListSetPushConstant(list, push, 128);
            push.AsSpan().Clear(); device.ComputeListDispatch(list, 4, 1, 1); device.ComputeListEnd(); originalPush.CopyTo(push, 0); device.BufferGetData(output, readback.AsSpan());
        }
        void Verify(double expectedFactor, bool expectedChoose)
        {
            var result = MemoryMarshal.Cast<byte, double>(readback.AsSpan());
            for (var i = 0; i < 4; i++)
            {
                var m = matrix[rowMajor ? (i >> 1) * 2 + (i & 1) : (i & 1) * 2 + (i >> 1)];
                var expected = (inputs[i] - 1) * Scale * expectedFactor + bias[i & 1] + .5 + ((i & 1) == 0 ? 1.25 : 2.5) + m + ((i & 1) == 0 ? 7.25 : 8.5) + 9.75 + 10.5 + 11 + 12.25;
                Check(Math.Abs(result[i] - expected) < 1e-12, $"{name}/{i}: {result[i]:R} != {expected:R}.");
            }
            Check(BitConverter.DoubleToUInt64Bits(result[4]) == BitConverter.DoubleToUInt64Bits(expectedFactor), "Native specialization retains low double mantissa bits.");
            Check(result[5] == (expectedChoose ? .5 : 12.25), "Boolean selection preserves double values.");
        }
        Dispatch(ordinary); Verify(2, true); Dispatch(custom); Verify(chosenFactor, false);
        for (var i = 0; i < 4; i++) inputs[i] += 4 / Scale;
        Dispatch(custom); Verify(chosenFactor, false);
        var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, custom);
        Reject<ArgumentException>(() => device.ComputeListSetPushConstant(list, push, 120)); Reject<ArgumentException>(() => device.ComputeListSetPushConstant(list, push.AsSpan(0, 127), 128)); device.ComputeListEnd();
        for (var i = 0; i < 16; i++) Dispatch(custom);
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 16; i++) Dispatch(custom);
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared FP64 recording/readback allocates no owner-thread managed bytes."); Verify(chosenFactor, false);
        Console.WriteLine("PASS " + name + ": precise native arithmetic, raw buffers/uniforms, 128-byte scalar/vector/matrix/array/struct push, typed specialization, snapshot/lifetime and zero owner bytes.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
