using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

try { NarrowInteger.Run(args.Length == 0 ? "compute" : args[0]); return 0; }
catch (Exception error) { Console.Error.WriteLine(error); return 1; }

internal static partial class NarrowInteger
{
    internal static void Run(string mode)
    {
        Managed(); if (mode == "managed") return; if (mode == "canvas") { Canvas(); Canvas(); return; }
        using var device = RenderingServer.CreateLocalRenderingDevice(); Console.WriteLine("Native Int16 device: " + device.GetDeviceName());
        if (mode == "graphics") { Graphics(device); Graphics(device, "hlsl"); return; }
        if (mode == "profiles" || mode.StartsWith("unavailable", StringComparison.Ordinal)) { Profiles(device, mode); return; }
        Check(mode is "compute" or "source", "Known native profile."); Compute(device, "data.comp.glsl", mode == "source"); Compute(device, "data.comp.hlsl", mode == "source");
    }
    private static void Managed()
    {
        using var signed = new RDPipelineSpecializationConstant<short>(); using var unsigned = new RDPipelineSpecializationConstant<ushort>();
        Check(signed.Value is null && unsigned.Value is null, "Int16 values start unassigned.");
        var a = signed.GetPropertyList().OfType<PropertyDescriptor<RDPipelineSpecializationConstant<short>, short?>>().Single(p => p.Name == "Value");
        var b = unsigned.GetPropertyList().OfType<PropertyDescriptor<RDPipelineSpecializationConstant<ushort>, ushort?>>().Single(p => p.Name == "Value");
        for (var bits = 0; bits <= ushort.MaxValue; bits++) { a.SetValue(signed, unchecked((short)bits)); b.SetValue(unsigned, (ushort)bits); Check(unchecked((ushort)a.GetValue(signed)!.Value) == bits && b.GetValue(unsigned) == bits, "Typed nullable metadata preserves all Int16/UInt16 patterns."); }
        signed.RevertProperty(a); unsigned.RevertProperty(b); Check(signed.Value is null && unsigned.Value is null, "Int16 reverts to unassigned."); signed.Dispose(); Reject<ObjectDisposedException>(() => _ = signed.Value);
        Reject<NotSupportedException>(() => new RDPipelineSpecializationConstant<char>()); Console.WriteLine("PASS all 65536 Int16/UInt16 metadata patterns, unassigned/reversion and disposal.");
    }
    private static RDShaderSPIRV Code(string name) => new() { BytecodeCompute = File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", name + ".spv")) };
    private static void Compute(RenderingDevice device, string name, bool authoring = false)
    {
        using var source = new RDShaderSource { Language = name.EndsWith("hlsl", StringComparison.Ordinal) ? ShaderLanguage.HLSL : ShaderLanguage.GLSL, SourceCompute = authoring ? File.ReadAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", name)) : "" };
        using var code = authoring ? device.ShaderCompileSPIRVFromSource(source, false) : Code(name); Check(code.CompileErrorCompute == "", code.CompileErrorCompute); var shader = device.ShaderCreateFromBytecode(device.ShaderCompileBinaryFromSPIRV(code)); var hlsl = name.EndsWith("hlsl", StringComparison.Ordinal);
        using RDPipelineSpecializationConstant factor = hlsl ? new RDPipelineSpecializationConstant<int> { ConstantID = 0, Value = -123 } : new RDPipelineSpecializationConstant<short> { ConstantID = 0, Value = -123 };
        using RDPipelineSpecializationConstant count = hlsl ? new RDPipelineSpecializationConstant<uint> { ConstantID = 1, Value = 65000 } : new RDPipelineSpecializationConstant<ushort> { ConstantID = 1, Value = 65000 };
        var normal = device.ComputePipelineCreate(shader); var custom = device.ComputePipelineCreate(shader, [factor, count]);
        using var mismatch = new RDPipelineSpecializationConstant<Half> { Value = (Half)1 }; Reject<ArgumentException>(() => device.ComputePipelineCreate(shader, [mismatch]));
        using var unassigned = new RDPipelineSpecializationConstant<short>(); Reject<ArgumentException>(() => device.ComputePipelineCreate(shader, [unassigned])); Reject<ArgumentException>(() => device.ComputePipelineCreate(shader, [factor, factor])); factor.Dispose(); count.Dispose(); Reject<ObjectDisposedException>(() => device.ComputePipelineCreate(shader, [factor]));
        short[] x = [-32768, -7, 123, 32767]; ushort[] y = [65535, 3, 32768, 17]; var data = new ushort[8]; for (var i = 0; i < 4; i++) { data[i * 2] = unchecked((ushort)x[i]); data[i * 2 + 1] = y[i]; }
        var input = device.StorageBufferCreate(16, MemoryMarshal.AsBytes(data.AsSpan())); var output = device.StorageBufferCreate(256);
        ushort[] parameters = [2, 65530, 0, 0, 0, 0, 0, 0]; var userBuffer = device.UniformBufferCreate(16, MemoryMarshal.AsBytes(parameters.AsSpan()));
        using var from = new RDUniform { Binding = 0, UniformType = UniformType.StorageBuffer }; from.AddID(input); using var to = new RDUniform { Binding = 1, UniformType = UniformType.StorageBuffer }; to.AddID(output); using var user = new RDUniform { Binding = 0, UniformType = UniformType.UniformBuffer }; user.AddID(userBuffer);
        var storage = device.UniformSetCreate([from, to], shader, 0); var uniforms = device.UniformSetCreate([user], shader, 1);
        var push = new byte[128]; void Put(int at, ushort value) => BinaryPrimitives.WriteUInt16LittleEndian(push.AsSpan(at), value);
        Put(0, 0x8001); Put(2, 0xffff); Put(4, 11); Put(6, 60000); Put(8, 7); Put(10, 0xfff9); Put(12, 1234); Put(16, 1); Put(18, 0xfffd); Put(126, 0xabcd); var snapshot = push.ToArray(); var result = new uint[64];
        void Dispatch(RID pipeline)
        {
            snapshot.CopyTo(push, 0); var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, storage, 0); device.ComputeListBindUniformSet(list, uniforms, 1);
            device.ComputeListSetPushConstant(list, push, 128); push.AsSpan().Clear(); device.ComputeListDispatch(list, 4, 1, 1); device.ComputeListEnd(); device.BufferGetData(output, MemoryMarshal.AsBytes(result.AsSpan()));
        }
        void Verify(short scale, ushort limit)
        {
            for (var i = 0; i < 4; i++)
            {
                uint S(int value) => unchecked((uint)(short)value); uint U(int value) => unchecked((ushort)value);
                uint[] expected = [S(x[i] + scale), U(y[i] + limit), S(x[i] / 3), (uint)((y[i] & 32767) % 3), S(x[i] >> 3), U(y[i] << 3), U(y[i] ^ limit), x[i] < 0 ? 1u : 0u, S(-32767), 65535, i % 2 == 0 ? 11u : 60000u, S(new short[] { 7, -7, 1234 }[i % 3]), S(1 - 3), 0xabcd, S(x[i]), U(y[i] + 65530)];
                for (var j = 0; j < expected.Length; j++) Check(result[i * 16 + j] == expected[j], $"{name} output {i}/{j}: {result[i * 16 + j]:x8} != {expected[j]:x8}");
            }
        }
        Dispatch(normal); Verify(-7, 5); Dispatch(custom); Verify(-123, 65000);
        var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, custom); Reject<ArgumentException>(() => device.ComputeListSetPushConstant(list, push, 126)); Reject<ArgumentException>(() => device.ComputeListSetPushConstant(list, push.AsSpan(0, 127), 128)); device.ComputeListEnd();
        for (var i = 0; i < 16; i++) Dispatch(custom); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 16; i++) Dispatch(custom); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared integer dispatch/readback allocates zero owner-thread bytes."); Verify(-123, 65000);
        Console.WriteLine("PASS " + name + ": Int16/UInt16 arithmetic, limits, compact SSBO/UBO, packed 128-byte push, aggregate/helper data, specialization snapshots and zero owner bytes.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
