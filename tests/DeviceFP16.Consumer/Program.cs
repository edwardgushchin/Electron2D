using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

try { NarrowFloat.Run(args.Length == 0 ? "compute" : args[0]); return 0; }
catch (Exception error) { Console.Error.WriteLine(error); return 1; }

internal static partial class NarrowFloat
{
    internal static void Run(string mode)
    {
        Managed(); if (mode == "managed") return; if (mode == "canvas") { Canvas(); Canvas(); return; }
        using var device = RenderingServer.CreateLocalRenderingDevice(); Console.WriteLine("Native half device: " + device.GetDeviceName());
        if (mode == "graphics") { Graphics(device); Graphics(device, "hlsl"); return; }
        if (mode == "profiles" || mode.StartsWith("unavailable", StringComparison.Ordinal)) { Profiles(device, mode); return; }
        Check(mode == "compute", "Known native profile."); Compute(device, "data.comp.glsl"); Compute(device, "data.comp.hlsl");
    }
    private static void Managed()
    {
        using var value = new RDPipelineSpecializationConstant<Half>(); Check(value.Value is null, "Half starts unassigned.");
        var descriptor = value.GetPropertyList().OfType<PropertyDescriptor<RDPipelineSpecializationConstant<Half>, Half?>>().Single(p => p.Name == "Value");
        for (var bits = 0; bits <= ushort.MaxValue; bits++) { descriptor.SetValue(value, BitConverter.UInt16BitsToHalf((ushort)bits)); Check(BitConverter.HalfToUInt16Bits(descriptor.GetValue(value)!.Value) == bits, "Typed nullable metadata preserves every half bit pattern."); }
        value.RevertProperty(descriptor); Check(value.Value is null, "Half reverts to unassigned."); value.Dispose(); Reject<ObjectDisposedException>(() => _ = value.Value);
        Reject<NotSupportedException>(() => new RDPipelineSpecializationConstant<char>());
        Console.WriteLine("PASS all 65536 Half metadata patterns, unassigned/reversion and disposal.");
    }
    private static RDShaderSPIRV Code(string name) => new() { BytecodeCompute = File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", name + ".spv")) };
    private static void Compute(RenderingDevice device, string name)
    {
        using var code = Code(name); var shader = device.ShaderCreateFromBytecode(device.ShaderCompileBinaryFromSPIRV(code)); var hlsl = name.EndsWith("hlsl", StringComparison.Ordinal);
        using RDPipelineSpecializationConstant factor = hlsl ? new RDPipelineSpecializationConstant<float> { ConstantID = 0, Value = -1.5f } : new RDPipelineSpecializationConstant<Half> { ConstantID = 0, Value = (Half)(-1.5f) };
        using var choose = new RDPipelineSpecializationConstant<bool> { ConstantID = 1, Value = false }; using var convert = new RDPipelineSpecializationConstant<float> { ConstantID = 2, Value = 1.00048828125f };
        var normal = device.ComputePipelineCreate(shader); var custom = device.ComputePipelineCreate(shader, [factor, choose, convert]);
        using RDPipelineSpecializationConstant mismatch = hlsl ? new RDPipelineSpecializationConstant<Half> { ConstantID = 0, Value = (Half)1 } : new RDPipelineSpecializationConstant<float> { ConstantID = 0, Value = 1 };
        Reject<ArgumentException>(() => device.ComputePipelineCreate(shader, [mismatch])); using var unassigned = new RDPipelineSpecializationConstant<Half>(); Reject<ArgumentException>(() => device.ComputePipelineCreate(shader, [unassigned]));
        Reject<ArgumentException>(() => device.ComputePipelineCreate(shader, [factor, factor])); factor.Dispose(); choose.Value = true; convert.Value = 0; Reject<ObjectDisposedException>(() => device.ComputePipelineCreate(shader, [factor]));
        Half[] x = [(Half)(-1.25f), (Half).5f, (Half)16, (Half)(-4.5f)], y = [(Half)(-2), (Half)3.5f, (Half).125f, (Half)8]; var inputData = new Half[8];
        for (var i = 0; i < 4; i++) { inputData[i * 2] = x[i]; inputData[i * 2 + 1] = y[i]; }
        var input = device.StorageBufferCreate(16, MemoryMarshal.AsBytes(inputData.AsSpan())); var output = device.StorageBufferCreate(128); var parameters = new Half[8]; parameters[0] = (Half)2; parameters[1] = (Half)4; parameters[2] = (Half).25f;
        var userBuffer = device.UniformBufferCreate(16, MemoryMarshal.AsBytes(parameters.AsSpan()));
        using var from = new RDUniform { Binding = 0, UniformType = UniformType.StorageBuffer }; from.AddID(input); using var to = new RDUniform { Binding = 1, UniformType = UniformType.StorageBuffer }; to.AddID(output); using var user = new RDUniform { Binding = 0, UniformType = UniformType.UniformBuffer }; user.AddID(userBuffer);
        var storage = device.UniformSetCreate([from, to], shader, 0); var uniforms = device.UniformSetCreate([user], shader, 1);
        var push = new byte[128]; void Put(int at, float value) => BinaryPrimitives.WriteUInt16LittleEndian(push.AsSpan(at), BitConverter.HalfToUInt16Bits((Half)value));
        Put(0, .25f); BinaryPrimitives.WriteUInt16LittleEndian(push.AsSpan(2), 0x8000); Put(4, .5f); Put(6, .75f); Put(8, 1.25f); Put(10, -2.5f); Put(12, .125f);
        for (var i = 0; i < 3; i++) { Put(16 + i * 2, i + 1); Put(24 + i * 2, i + 4); Put(40 + i * 4, -i - 1); Put(42 + i * 4, -i - 4); }
        Put(32, .5f); Put(34, 1); BinaryPrimitives.WriteUInt32LittleEndian(push.AsSpan(36), 1); BinaryPrimitives.WriteUInt16LittleEndian(push.AsSpan(52), 0x7e55); BinaryPrimitives.WriteUInt16LittleEndian(push.AsSpan(54), 1); BinaryPrimitives.WriteUInt16LittleEndian(push.AsSpan(126), 0x7bff);
        var snapshot = push.ToArray(); var result = new ushort[64];
        void Dispatch(RID pipeline)
        {
            snapshot.CopyTo(push, 0); var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, storage, 0); device.ComputeListBindUniformSet(list, uniforms, 1);
            device.ComputeListSetPushConstant(list, push, 128); push.AsSpan().Clear(); device.ComputeListDispatch(list, 4, 1, 1); device.ComputeListEnd(); device.BufferGetData(output, MemoryMarshal.AsBytes(result.AsSpan()));
        }
        void Verify(Half scale, bool flag, ushort converted)
        {
            ushort Bits(Half value) => BitConverter.HalfToUInt16Bits(value);
            Half[] array = [(Half)1.25f, (Half)(-2.5f), (Half).125f];
            for (var i = 0; i < 4; i++)
            {
                ushort[] expected = [Bits(x[i] + scale + (Half).25f), Bits(y[i] * (Half)2 + (i % 2 == 0 ? (Half).5f : (Half).75f)), Bits((Half)(i % 3 + 4)), Bits((Half)(-i % 3 - 4)), Bits(array[i % 3]), Bits((Half)1.5f), (ushort)(i % 2 == 0 ? 0x7e55 : 1), 0x7bff, Bits(x[i]), Bits(y[i]), Bits(scale), Bits(flag ? x[i] : y[i]), Bits((Half)4.25f), converted, Bits((Half).25f), 0x8000];
                for (var j = 0; j < expected.Length; j++) Check(result[i * 16 + j] == expected[j], $"{name} output {i}/{j}: {result[i * 16 + j]:x4} != {expected[j]:x4}");
            }
        }
        Dispatch(normal); Verify((Half).75f, true, BitConverter.HalfToUInt16Bits((Half).333251953125f)); Dispatch(custom); Verify((Half)(-1.5f), false, 0x3c00);
        var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, custom); Reject<ArgumentException>(() => device.ComputeListSetPushConstant(list, push, 126)); Reject<ArgumentException>(() => device.ComputeListSetPushConstant(list, push.AsSpan(0, 127), 128)); device.ComputeListEnd();
        for (var i = 0; i < 16; i++) Dispatch(custom); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 16; i++) Dispatch(custom); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared half dispatch/readback allocates zero owner-thread managed bytes."); Verify((Half)(-1.5f), false, 0x3c00);
        Console.WriteLine("PASS " + name + ": half arithmetic, compact SSBO/UBO, 128-byte push, halfword strides, matrix/aggregate/helper data, exact raw bits, typed specialization/conversion, snapshots and zero owner bytes.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
