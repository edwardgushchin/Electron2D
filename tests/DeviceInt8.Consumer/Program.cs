using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

try { ByteInteger.Run(args.Length == 0 ? "compute" : args[0]); return 0; }
catch (Exception error) { Console.Error.WriteLine(error); return 1; }

internal static partial class ByteInteger
{
    internal static void Run(string mode)
    {
        Managed(); if (mode == "managed") return; if (mode == "canvas") { Canvas(); Canvas(); return; }
        using var device = RenderingServer.CreateLocalRenderingDevice(); Console.WriteLine("Native Int8 device: " + device.GetDeviceName());
        if (mode == "graphics") { Graphics(device); return; }
        if (mode == "profiles" || mode.StartsWith("unavailable", StringComparison.Ordinal)) { Profiles(device, mode); return; }
        Check(mode is "compute" or "source", "Known native profile."); Compute(device, "data.comp.glsl", mode == "source"); if (mode == "source") HLSLDiagnostic(device);
    }
    private static void Managed()
    {
        using var signed = new RDPipelineSpecializationConstant<sbyte>(); using var unsigned = new RDPipelineSpecializationConstant<byte>();
        Check(signed.Value is null && unsigned.Value is null, "Int16 values start unassigned.");
        var a = signed.GetPropertyList().OfType<PropertyDescriptor<RDPipelineSpecializationConstant<sbyte>, sbyte?>>().Single(p => p.Name == "Value");
        var b = unsigned.GetPropertyList().OfType<PropertyDescriptor<RDPipelineSpecializationConstant<byte>, byte?>>().Single(p => p.Name == "Value");
        for (var bits = 0; bits <= byte.MaxValue; bits++) { a.SetValue(signed, unchecked((sbyte)bits)); b.SetValue(unsigned, (byte)bits); Check(unchecked((byte)a.GetValue(signed)!.Value) == bits && b.GetValue(unsigned) == bits, "Typed nullable metadata preserves all Int8/UInt8 patterns."); }
        signed.RevertProperty(a); unsigned.RevertProperty(b); Check(signed.Value is null && unsigned.Value is null, "Int16 reverts to unassigned."); signed.Dispose(); Reject<ObjectDisposedException>(() => _ = signed.Value);
        Reject<NotSupportedException>(() => new RDPipelineSpecializationConstant<char>()); Console.WriteLine("PASS all 256 Int8/UInt8 metadata patterns, unassigned/reversion and disposal.");
    }
    private static RDShaderSPIRV Code(string name) => new() { BytecodeCompute = File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", name + ".spv")) };
    private static void Compute(RenderingDevice device, string name, bool authoring = false)
    {
        using var source = new RDShaderSource { Language = name.EndsWith("hlsl", StringComparison.Ordinal) ? ShaderLanguage.HLSL : ShaderLanguage.GLSL, SourceCompute = authoring ? File.ReadAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", name)) : "" };
        using var code = authoring ? device.ShaderCompileSPIRVFromSource(source, false) : Code(name); Check(code.CompileErrorCompute == "", code.CompileErrorCompute); if (!authoring) code.BytecodeCompute = DirectExtent(code.BytecodeCompute); var shader = device.ShaderCreateFromBytecode(device.ShaderCompileBinaryFromSPIRV(code));
        using var factor = new RDPipelineSpecializationConstant<sbyte> { ConstantID = 0, Value = -123 };
        using var count = new RDPipelineSpecializationConstant<byte> { ConstantID = 1, Value = 250 };
        using var zeroLength = new RDPipelineSpecializationConstant<byte> { ConstantID = 2, Value = 0 }; Reject<ArgumentException>(() => device.ComputePipelineCreate(shader, [zeroLength]));
        var normal = device.ComputePipelineCreate(shader); var custom = device.ComputePipelineCreate(shader, [factor, count]);
        using var mismatch = new RDPipelineSpecializationConstant<Half> { Value = (Half)1 }; Reject<ArgumentException>(() => device.ComputePipelineCreate(shader, [mismatch]));
        using var unassigned = new RDPipelineSpecializationConstant<sbyte>(); Reject<ArgumentException>(() => device.ComputePipelineCreate(shader, [unassigned])); Reject<ArgumentException>(() => device.ComputePipelineCreate(shader, [factor, factor])); factor.Dispose(); count.Dispose(); Reject<ObjectDisposedException>(() => device.ComputePipelineCreate(shader, [factor]));
        sbyte[] x = [-128, -7, 123, 127]; byte[] y = [255, 3, 128, 17]; var data = new byte[8]; for (var i = 0; i < 4; i++) { data[i * 2] = unchecked((byte)x[i]); data[i * 2 + 1] = y[i]; }
        var input = device.StorageBufferCreate(8, MemoryMarshal.AsBytes(data.AsSpan())); var output = device.StorageBufferCreate(256);
        byte[] parameters = new byte[16]; parameters[0] = 2; parameters[1] = 250; BinaryPrimitives.WriteUInt16LittleEndian(parameters.AsSpan(2), BitConverter.HalfToUInt16Bits((Half).5f)); BinaryPrimitives.WriteInt16LittleEndian(parameters.AsSpan(4), -300); var userBuffer = device.UniformBufferCreate(16, MemoryMarshal.AsBytes(parameters.AsSpan()));
        using var from = new RDUniform { Binding = 0, UniformType = UniformType.StorageBuffer }; from.AddID(input); using var to = new RDUniform { Binding = 1, UniformType = UniformType.StorageBuffer }; to.AddID(output); using var user = new RDUniform { Binding = 0, UniformType = UniformType.UniformBuffer }; user.AddID(userBuffer);
        var storage = device.UniformSetCreate([from, to], shader, 0); var uniforms = device.UniformSetCreate([user], shader, 1);
        var push = new byte[128];
        push[0] = 0x81; push[1] = 0xff; push[2] = 0x80; push[3] = 0x7f; push[4] = 11; push[5] = 200; push[6] = 255; push[7] = 0;
        push[8] = 7; push[9] = 0xf9; push[10] = 123; push[12] = 1; push[13] = 0xfd;
        BinaryPrimitives.WriteInt16LittleEndian(push.AsSpan(16), -12345); BinaryPrimitives.WriteUInt16LittleEndian(push.AsSpan(18), BitConverter.HalfToUInt16Bits((Half).5f)); push[127] = 0xcd; var snapshot = push.ToArray(); var result = new uint[64];
        void Dispatch(RID pipeline)
        {
            snapshot.CopyTo(push, 0); var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, storage, 0); device.ComputeListBindUniformSet(list, uniforms, 1);
            device.ComputeListSetPushConstant(list, push, 128); push.AsSpan().Clear(); device.ComputeListDispatch(list, 4, 1, 1); device.ComputeListEnd(); device.BufferGetData(output, MemoryMarshal.AsBytes(result.AsSpan()));
        }
        void Verify(sbyte scale, byte limit)
        {
            for (var i = 0; i < 4; i++)
            {
                uint S(int value) => unchecked((uint)(sbyte)value); uint U(int value) => unchecked((byte)value);
                uint[] expected = [S(x[i] + scale), U(y[i] + limit), S(x[i] / 3), (uint)((y[i] & 127) % 3), S(x[i] >> 3), U(y[i] << 3), U(y[i] ^ limit), x[i] < 0 ? 1u : 0u, unchecked((uint)-255), 382, new uint[] { 11, 200, 255, 0 }[i % 4], S(new sbyte[] { 7, -7, 123 }[i % 3]), unchecked((uint)-12343), 0xcd, unchecked((uint)(x[i] - 296)), U(y[i] + 250)];
                for (var j = 0; j < expected.Length; j++) Check(result[i * 16 + j] == expected[j], $"{name} output {i}/{j}: {result[i * 16 + j]:x8} != {expected[j]:x8}");
            }
        }
        Dispatch(normal); Verify(-7, 5); Dispatch(custom); Verify(-123, 250);
        var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, custom); Reject<ArgumentException>(() => device.ComputeListSetPushConstant(list, push, 126)); Reject<ArgumentException>(() => device.ComputeListSetPushConstant(list, push.AsSpan(0, 127), 128)); device.ComputeListEnd();
        for (var i = 0; i < 16; i++) Dispatch(custom); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 16; i++) Dispatch(custom); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared integer dispatch/readback allocates zero owner-thread bytes."); Verify(-123, 250);
        Console.WriteLine("PASS " + name + ": Int8/UInt8 arithmetic, limits, compact SSBO/UBO, packed 128-byte push, aggregate/helper data, specialization snapshots and zero owner bytes.");
    }
    private static byte[] DirectExtent(byte[] code)
    {
        var words = MemoryMarshal.Cast<byte, uint>(code.AsSpan()); uint length = 0, converted = 0; var changed = 0;
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16)) if ((words[at] & 65535) == 71 && words[at + 2] == 1 && words[at + 3] == 2) length = words[at + 1];
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16)) if ((words[at] & 65535) == 52 && words[at + 3] == 113 && words[at + 4] == length) converted = words[at + 2];
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16)) if ((words[at] & 65535) == 28 && words[at + 3] == converted) { words[at + 3] = length; changed++; }
        Check(length != 0 && converted != 0 && changed == 1, "Fixture gives the push array a direct unsigned byte specialization extent.");
        var output = Environment.GetEnvironmentVariable("ELECTRON2D_INT8_OUTPUT"); if (output is not null) File.WriteAllBytes(System.IO.Path.Combine(output, "direct-extent8.spv"), code);
        return code;
    }
    private static void HLSLDiagnostic(RenderingDevice device)
    {
        using var source = new RDShaderSource { Language = ShaderLanguage.HLSL, SourceCompute = "[[vk::binding(0,0)]] RWStructuredBuffer<int> result; [numthreads(1,1,1)]void main(){float16_t h=float16_t(1);int8_t x=int8_t(7);result[0]=int(x)+int(h);}" };
        using var code = device.ShaderCompileSPIRVFromSource(source, false); Check(code.BytecodeCompute.Length == 0 && code.CompileErrorCompute.Contains("int8_t", StringComparison.Ordinal), "Pinned HLSL frontend reports its absent native8 type instead of widening storage."); Reject<ArgumentException>(() => device.ShaderCreateFromSPIRV(code));
        Console.WriteLine("PASS explicit HLSL native8 frontend diagnostic; GLSL/SPIR-V execution remains available.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
