using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

try { WideInteger.Run(args.Length == 0 ? "managed" : args[0]); return 0; }
catch (Exception error) { Console.Error.WriteLine(error); return 1; }

internal static partial class WideInteger
{
    private const long DefaultSigned = -1099511627773, CustomSigned = -1125899906768059;
    private const ulong DefaultUnsigned = 0xfedcba9800000011, CustomUnsigned = 0x8000000000000007;
    internal static void Run(string mode)
    {
        Managed(); if (mode == "managed") return; if (mode == "canvas") { Canvas(); return; }
        using var device = RenderingServer.CreateLocalRenderingDevice(); Console.WriteLine("Native integer device: " + device.GetDeviceName());
        if (mode.StartsWith("unavailable", StringComparison.Ordinal))
        {
            if (mode != "unavailable-fp64") { using var code = Code("data.comp.glsl"); Reject<NotSupportedException>(() => device.ShaderCreateFromSPIRV(code)); Reject<NotSupportedException>(() => device.ShaderCompileBinaryFromSPIRV(code)); }
            if (mode != "unavailable-int64") { using var code = Code("floating.comp.glsl"); Reject<NotSupportedException>(() => device.ShaderCreateFromSPIRV(code)); }
            Scalar(device, "ordinary.comp.glsl", false);
            if (mode == "unavailable-int64") Scalar(device, "floating.comp.glsl", true);
            if (mode == "unavailable-fp64") Compute(device, "data.comp.glsl");
            Console.WriteLine("PASS independent optional precision refusal and executing remaining native profile."); return;
        }
        if (mode == "graphics") { Graphics(device); Graphics(device, "hlsl"); return; }
        Check(mode == "compute", "Known profile."); Compute(device, "data.comp.glsl"); Compute(device, "data.comp.glsl", true); Compute(device, "data.comp.hlsl"); Mixed(device);
    }
    private static void Managed()
    {
        using var signed = new RDPipelineSpecializationConstant<long>(); using var unsigned = new RDPipelineSpecializationConstant<ulong>();
        Check(signed.Value is null && unsigned.Value is null, "Wide values start unassigned.");
        var s = signed.GetPropertyList().OfType<PropertyDescriptor<RDPipelineSpecializationConstant<long>, long?>>().Single(p => p.Name == "Value");
        var u = unsigned.GetPropertyList().OfType<PropertyDescriptor<RDPipelineSpecializationConstant<ulong>, ulong?>>().Single(p => p.Name == "Value");
        foreach (var bits in new ulong[] { 0, 1, 0x8000000000000000, 0x8000000000000001, 0x123456789abcdef0, ulong.MaxValue })
        { s.SetValue(signed, unchecked((long)bits)); u.SetValue(unsigned, bits); Check(unchecked((ulong)s.GetValue(signed)!.Value) == bits && u.GetValue(unsigned) == bits, "Typed metadata preserves both words and signedness."); }
        signed.RevertProperty(s); unsigned.RevertProperty(u); Check(signed.Value is null && unsigned.Value is null, "Wide values revert to unassigned.");
        signed.Dispose(); unsigned.Dispose(); Reject<ObjectDisposedException>(() => _ = signed.Value); Reject<ObjectDisposedException>(() => _ = unsigned.Value); Reject<NotSupportedException>(() => new RDPipelineSpecializationConstant<byte>());
        Console.WriteLine("PASS Int64/UInt64 metadata, exact boundary bits and lifecycle.");
    }
    private static RDShaderSPIRV Code(string name) => new() { BytecodeCompute = File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", name + ".spv")) };
    private static void Scalar(RenderingDevice device, string name, bool wide)
    {
        using var code = Code(name); var shader = device.ShaderCreateFromSPIRV(code); var pipeline = device.ComputePipelineCreate(shader); var output = device.StorageBufferCreate(16);
        using var uniform = new RDUniform { Binding = 0, UniformType = UniformType.StorageBuffer }; uniform.AddID(output); var set = device.UniformSetCreate([uniform], shader, 0);
        var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, set, 0); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); var bytes = device.BufferGetData(output);
        Check(wide ? BinaryPrimitives.ReadUInt64LittleEndian(bytes) == BitConverter.DoubleToUInt64Bits(1 + 1d / 1099511627776) : BinaryPrimitives.ReadSingleLittleEndian(bytes) == 3.25f, "Remaining native precision executes exact scalar output.");
    }
    private static void Compute(RenderingDevice device, string name, bool wideLayout = false)
    {
        using var code = Code(name); if (wideLayout) code.BytecodeCompute = WideLayout(code.BytecodeCompute);
        var shader = device.ShaderCreateFromBytecode(device.ShaderCompileBinaryFromSPIRV(code));
        var hlsl = name.EndsWith("hlsl", StringComparison.Ordinal); var chosenSigned = hlsl ? -(1L << 40) + 7 : CustomSigned; var chosenUnsigned = hlsl ? 0xfedcba9800000013UL : CustomUnsigned;
        using RDPipelineSpecializationConstant signed = hlsl ? new RDPipelineSpecializationConstant<int> { ConstantID = 0, Value = 7 } : new RDPipelineSpecializationConstant<long> { ConstantID = 0, Value = chosenSigned };
        using RDPipelineSpecializationConstant unsigned = hlsl ? new RDPipelineSpecializationConstant<uint> { ConstantID = 1, Value = 19 } : new RDPipelineSpecializationConstant<ulong> { ConstantID = 1, Value = chosenUnsigned };
        using var choose = new RDPipelineSpecializationConstant<bool> { ConstantID = 2, Value = false };
        var ordinary = device.ComputePipelineCreate(shader); var custom = device.ComputePipelineCreate(shader, [signed, unsigned, choose]);
        using var mismatch = new RDPipelineSpecializationConstant<ulong> { ConstantID = 0, Value = 1 }; Reject<ArgumentException>(() => device.ComputePipelineCreate(shader, [mismatch]));
        using var unassigned = new RDPipelineSpecializationConstant<long>(); Reject<ArgumentException>(() => device.ComputePipelineCreate(shader, [unassigned])); Reject<ArgumentException>(() => device.ComputePipelineCreate(shader, [signed, signed]));
        choose.Value = true; signed.Dispose(); unsigned.Dispose(); choose.Dispose(); Reject<ObjectDisposedException>(() => device.ComputePipelineCreate(shader, [signed]));
        long[] inputs = [-(1L << 40) + 123, -7, long.MinValue + 1000, long.MaxValue - 1000]; ulong[] large = [ulong.MaxValue - 123, (1UL << 40) + 123, (1UL << 63) + 9, (1UL << 53) + 1]; var sourceBytes = new byte[64];
        for (var i = 0; i < 4; i++) { BinaryPrimitives.WriteInt64LittleEndian(sourceBytes.AsSpan(i * 16), inputs[i]); BinaryPrimitives.WriteUInt64LittleEndian(sourceBytes.AsSpan(i * 16 + 8), large[i]); }
        var input = device.StorageBufferCreate(64, sourceBytes); var output = device.StorageBufferCreate(512); long[] offsets = [(1L << 48) + 1, -(1L << 48) + 3]; ulong[] masks = [0xf0f0f0f0f0f0f0f0, 0x0011223344556677]; var parameters = new byte[32]; MemoryMarshal.AsBytes(offsets.AsSpan()).CopyTo(parameters); MemoryMarshal.AsBytes(masks.AsSpan()).CopyTo(parameters.AsSpan(16)); var userBuffer = device.UniformBufferCreate(32, parameters);
        using var from = new RDUniform { Binding = 0, UniformType = UniformType.StorageBuffer }; from.AddID(input); using var to = new RDUniform { Binding = 1, UniformType = UniformType.StorageBuffer }; to.AddID(output); using var user = new RDUniform { Binding = 0, UniformType = UniformType.UniformBuffer }; user.AddID(userBuffer);
        var storage = device.UniformSetCreate([from, to], shader, 0); var uniforms = device.UniformSetCreate([user], shader, 1);
        long[] tail = [(1L << 35) + 7, -(1L << 37) - 1]; ulong[] vector = [(1UL << 48) + 13, (1UL << 56) + 17]; const long pairSigned = long.MinValue + 1; const ulong pairUnsigned = 0xaabbccddeeff0011, last = 0xfedcba9876543210;
        var push = new byte[128]; void Put(int at, ulong bits) => BinaryPrimitives.WriteUInt64LittleEndian(push.AsSpan(at), bits);
        Put(0, unchecked((ulong)-11L)); Put(16, vector[0]); Put(24, vector[1]); Put(32, (ulong)tail[0]); Put(40, unchecked((ulong)tail[1])); Put(48, unchecked((ulong)pairSigned)); Put(56, pairUnsigned); Put(64, 1); Put(72, unchecked((ulong)-3L)); Put(80, 40); Put(120, last);
        var snapshot = push.ToArray(); var result = new ulong[64];
        void Dispatch(RID pipeline)
        {
            device.BufferUpdate(input, 0, 64, sourceBytes); var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, storage, 0); device.ComputeListBindUniformSet(list, uniforms, 1); device.ComputeListSetPushConstant(list, push, 128);
            push.AsSpan().Clear(); device.ComputeListDispatch(list, 4, 1, 1); device.ComputeListEnd(); snapshot.CopyTo(push, 0); device.BufferGetData(output, MemoryMarshal.AsBytes(result.AsSpan()));
        }
        void Verify(long factor, ulong multiplier, bool flag)
        {
            for (var i = 0; i < 4; i++)
            {
                var x = inputs[i]; var y = large[i]; ulong[] expected = [unchecked((ulong)(x + factor - 11)), unchecked(y * multiplier + vector[i & 1]), unchecked((ulong)(x / -3)), unchecked((ulong)(x - x / -3 * -3)), y >> 40, unchecked((ulong)(x >> 40)), y << 40, ((y & masks[0]) ^ masks[1]) | last, x > factor ? 1UL : 0, y > vector[i & 1] ? 1UL : 0, unchecked((ulong)(tail[i & 1] + offsets[i & 1] + pairSigned)), pairUnsigned ^ vector[1], multiplier, unchecked((ulong)factor), flag ? y : unchecked((ulong)x), unchecked((ulong)(uint)y + (ulong)(long)(int)x)];
                for (var j = 0; j < 16; j++) Check(result[i * 16 + j] == expected[j], $"{name} result {i}/{j}: {result[i * 16 + j]:x16} != {expected[j]:x16}");
            }
        }
        Dispatch(ordinary); Verify(DefaultSigned, DefaultUnsigned, true); Dispatch(custom); Verify(chosenSigned, chosenUnsigned, false);
        for (var i = 0; i < 4; i++) { inputs[i] += 17; large[i] -= 19; BinaryPrimitives.WriteInt64LittleEndian(sourceBytes.AsSpan(i * 16), inputs[i]); BinaryPrimitives.WriteUInt64LittleEndian(sourceBytes.AsSpan(i * 16 + 8), large[i]); }
        Dispatch(custom); Verify(chosenSigned, chosenUnsigned, false);
        var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, custom); Reject<ArgumentException>(() => device.ComputeListSetPushConstant(list, push, 124)); Reject<ArgumentException>(() => device.ComputeListSetPushConstant(list, push.AsSpan(0, 127), 128)); device.ComputeListEnd();
        for (var i = 0; i < 16; i++) Dispatch(custom); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 16; i++) Dispatch(custom); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared wide integer work/readback allocates zero owner bytes."); Verify(chosenSigned, chosenUnsigned, false);
        Console.WriteLine("PASS " + name + ": signed/unsigned 64-bit arithmetic, raw buffers/UBO, 128-byte push/helpers/64-bit indices, typed specialization, bytecode/snapshots and zero owner bytes.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
