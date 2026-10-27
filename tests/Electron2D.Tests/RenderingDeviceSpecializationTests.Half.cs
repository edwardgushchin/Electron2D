using System.Reflection;
using System.Runtime.InteropServices;
using Electron2D;
using static TextureArrayTests;

internal static partial class RenderingDeviceSpecializationTests
{
    private static void HalfExpressions()
    {
        var words = new List<uint> { 0x07230203, 0x10000, 0, 200, 0 }; uint next = 20;
        void Emit(uint op, params uint[] args) { words.Add((uint)(args.Length + 1) << 16 | op); words.AddRange(args); }
        Emit(17, 9); Emit(17, 10); Emit(20, 1); Emit(22, 2, 16); Emit(22, 3, 32); Emit(22, 4, 64); Emit(23, 5, 2, 2);
        uint Constant(uint type, ulong bits) { var id = next++; Emit(50, type == 4 ? [type, id, (uint)bits, (uint)(bits >> 32)] : [type, id, (uint)bits]); return id; }
        var half = Constant(2, 0x7e55); Emit(71, half, 1, 0); var expected = new Dictionary<uint, ulong>();
        uint Convert(uint type, uint source, ulong bits) { var id = next++; Emit(52, type, id, 115, source); expected.Add(id, bits); return id; }
        Convert(2, Constant(3, 0x80000000), 0x8000); Convert(2, Constant(3, BitConverter.SingleToUInt32Bits(1f / 16777216)), 1);
        Convert(2, Constant(3, BitConverter.SingleToUInt32Bits(1.00048828125f)), 0x3c00); Convert(2, Constant(4, BitConverter.DoubleToUInt64Bits(1 + 1d / 2048 + 1d / 1073741824)), 0x3c01);
        Convert(2, Constant(3, 0x7f800000), 0x7c00); Convert(2, Constant(3, 0xff800000), 0xfc00); Convert(2, Constant(3, BitConverter.SingleToUInt32Bits(65520)), 0x7c00);
        Convert(3, Constant(2, 1), BitConverter.SingleToUInt32Bits(1f / 16777216)); Convert(4, Constant(2, 0x8000), 0x8000000000000000); Convert(4, Constant(2, 0x7bff), BitConverter.DoubleToUInt64Bits(65504));
        var yes = next++; Emit(48, 1, yes); var select = next++; Emit(52, 2, select, 169, yes, half, half); expected.Add(select, 0x7e55);
        var source = MemoryMarshal.AsBytes(words.ToArray().AsSpan()).ToArray();
        void Verify(byte[] bytes, Dictionary<uint, ulong> values)
        {
            var result = MemoryMarshal.Cast<byte, uint>(bytes.AsSpan()); var found = 0;
            for (var at = 5; at < result.Length; at += (int)(result[at] >> 16))
                if ((result[at] & 65535) == 43 && values.TryGetValue(result[at + 2], out var value))
                { var bits = (ulong)result[at + 3]; if ((result[at] >> 16) == 5) bits |= (ulong)result[at + 4] << 32; Check(bits == value, "Half constant/conversion preserves rounding, width and exact bits."); found++; }
            Check(found == values.Count, "Every half result was frozen.");
        }
        Verify(Freeze(source), expected);
        Verify(Freeze(source, new() { [0] = (7, 0x8000) }), new() { [half] = 0x8000, [select] = 0x8000 });
        Reject<ArgumentException>(() => Freeze(source, new() { [0] = (3, 0x3f800000) })); Reject<ArgumentException>(() => Freeze(source, new() { [0] = (7, 0x10000) }));
        Console.WriteLine("PASS Half constants, exact overrides, width-only 16/32/64 conversions, ties/overflow/subnormal/signed-zero and malformed payloads.");
    }
    private static void NarrowRequirements()
    {
        var type = typeof(RenderingDevice).Assembly.GetType("Electron2D.ShaderPrecision")!;
        var read = type.GetMethod("ReadRequirements", BindingFlags.NonPublic | BindingFlags.Static)!; var narrow = type.GetMethod("ReadNarrowRequirements", BindingFlags.NonPublic | BindingFlags.Static)!;
        int Parse(uint capability, uint? storage = null, bool bufferBlock = false)
        {
            var words = new List<uint> { 0x07230203, 0x10000, 0, 100, 0, 2u << 16 | 17, capability, 3u << 16 | 22, 1, 16 };
            if (storage is { } value)
            {
                words.AddRange([3u << 16 | 30, 2, 1, 4u << 16 | 32, 3, value, 2, 4u << 16 | 59, 3, 4, value]);
                if (bufferBlock) words.AddRange([3u << 16 | 71, 2, 3]);
            }
            var code = MemoryMarshal.AsBytes(words.ToArray().AsSpan()).ToArray();
            try { read.Invoke(null, [code]); return (int)narrow.Invoke(null, [code])!; }
            catch (TargetInvocationException error) when (error.InnerException is not null) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
        }
        Check(Parse(9) == 16 && Parse(4433, 12) == 1 && Parse(4434, 2) == 3 && Parse(4433, 2, true) == 1 && Parse(4435, 9) == 20 && Parse(4436, 1) == 8 && Parse(4436, 3) == 8, "Arithmetic and each storage capability remain independent; half push records its exact reconstruction prerequisite.");
        foreach (var storage in new uint[] { 4, 6, 7 }) Check(Parse(9, storage) == 16, "Private/workgroup/function Half data uses arithmetic opt-in.");
        foreach (var storage in new uint[] { 1, 2, 3, 9, 12 }) Reject<ArgumentException>(() => Parse(9, storage));
        Reject<ArgumentException>(() => Parse(1)); Reject<NotSupportedException>(() => Parse(9, 0));
        var layout = typeof(RenderingDevice).Assembly.GetType("Electron2D.GPUDeviceFactory+Vulkan11Features")!;
        Check(Marshal.OffsetOf(layout, "Flags").ToInt32() == (IntPtr.Size == 8 ? 16 : 8) && Marshal.SizeOf(layout) == (IntPtr.Size == 8 ? 64 : 56), "Vulkan 1.1 feature ABI matches the host header and twelve flags.");
        ExportHalfModules();
        Console.WriteLine("PASS half capability/storage dependency guards and Vulkan 1.1 host ABI.");
    }
    private static void ExportHalfModules()
    {
        var directory = Environment.GetEnvironmentVariable("ELECTRON2D_FP16_OUTPUT"); var shaders = Environment.GetEnvironmentVariable("ELECTRON2D_FP16_SHADERS"); if (directory is null || shaders is null) return;
        byte[] Read(string file) => File.ReadAllBytes(System.IO.Path.Combine(shaders, file));
        var reflect = typeof(RenderingDevice).Assembly.GetType("Electron2D.ShaderCompiler")!.GetMethod("ValidateDeviceGraphics", BindingFlags.NonPublic | BindingFlags.Static)!;
        void Interface(string fragment)
        {
            try { reflect.Invoke(null, [Read("io.vert.glsl.spv"), Read(fragment)]); }
            catch (TargetInvocationException error) when (error.InnerException is not null) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
        }
        Interface("io.frag.glsl.spv"); Reject<ArgumentException>(() => Interface("io-mismatch.frag.glsl.spv"));
        Console.WriteLine("PASS half interstage native metadata and width mismatch; GPU storageInputOutput16 execution remains unavailable on this host.");
        var remap = typeof(RenderingDevice).GetMethod("Remap", BindingFlags.NonPublic | BindingFlags.Static)!;
        foreach (var matrix in new[] { false, true })
        {
            var malformed = Freeze(Read("data.comp.glsl.spv")); var words = MemoryMarshal.Cast<byte, uint>(malformed.AsSpan()); var changed = false;
            for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
                if ((words[at] & 65535) == 72 && words[at] >> 16 == 5 && (matrix ? words[at + 3] == 7 : words[at + 3] == 35 && words[at + 4] == 126))
                { words[at + 4] = matrix ? 3u : 127u; changed = true; break; }
            Check(changed, "Malformed fixture changes a half push matrix stride or final halfword offset.");
            Reject<ArgumentException>(() =>
            {
                try { remap.Invoke(null, [malformed, RenderingDevice.ShaderStage.Compute]); }
                catch (TargetInvocationException error) when (error.InnerException is not null) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
            });
        }
        Console.WriteLine("PASS misaligned halfword push offsets and matrix strides reject before native execution.");
        foreach (var source in Directory.EnumerateFiles(shaders, "*.spv"))
        {
            if (System.IO.Path.GetFileName(source).StartsWith("io.", StringComparison.Ordinal)) continue;
            var stage = source.Contains(".vert.", StringComparison.Ordinal) ? RenderingDevice.ShaderStage.Vertex : source.Contains(".frag.", StringComparison.Ordinal) ? RenderingDevice.ShaderStage.Fragment : RenderingDevice.ShaderStage.Compute;
            var frozen = Freeze(File.ReadAllBytes(source)); File.WriteAllBytes(System.IO.Path.Combine(directory, System.IO.Path.GetFileName(source) + "-frozen.spv"), frozen);
            var translated = remap.Invoke(null, [frozen, stage])!; File.WriteAllBytes(System.IO.Path.Combine(directory, System.IO.Path.GetFileName(source) + "-translated.spv"), (byte[])translated.GetType().GetField("Item1")!.GetValue(translated)!);
        }
    }
}
