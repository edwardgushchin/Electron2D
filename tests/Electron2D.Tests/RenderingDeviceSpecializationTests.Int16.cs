using System.Reflection;
using System.Runtime.InteropServices;
using Electron2D;
using static TextureArrayTests;

internal static partial class RenderingDeviceSpecializationTests
{
    private static void Integer16Expressions()
    {
        var words = new List<uint> { 0x07230203, 0x10000, 0, 1000, 0 }; uint next = 20;
        void Emit(uint op, params uint[] args) { words.Add((uint)(args.Length + 1) << 16 | op); words.AddRange(args); }
        Emit(17, 22); Emit(20, 1); Emit(21, 2, 16, 1); Emit(21, 3, 16, 0); Emit(21, 4, 32, 1); Emit(21, 5, 64, 0); Emit(23, 6, 2, 2);
        uint Constant(uint type, ulong bits)
        {
            var id = next++; if (type == 1) Emit(bits == 0 ? 49u : 48u, type, id);
            else if (type == 5) Emit(50, type, id, (uint)bits, (uint)(bits >> 32));
            else Emit(50, type, id, type == 2 ? unchecked((uint)(int)(short)(ushort)bits) : (uint)bits); return id;
        }
        var a = Constant(2, unchecked((ushort)-7)); var b = Constant(2, 3); var u = Constant(3, 65535); var v = Constant(3, 3); var yes = Constant(1, 1);
        Emit(71, a, 1, 0); Emit(71, u, 1, 1); var expected = new Dictionary<uint, ulong>();
        uint Operation(uint type, uint op, ulong bits, params uint[] args) { var id = next++; Emit(52, [type, id, op, .. args]); expected.Add(id, bits); return id; }
        foreach (var (op, bits) in new (uint, int)[] { (128, -4), (130, -10), (132, -21), (135, -2), (138, -1), (139, 2), (195, -1), (196, -56), (197, -5), (198, -6), (199, 1) }) Operation(2, op, unchecked((uint)bits), a, b);
        Operation(2, 126, 7, a); Operation(2, 200, 6, a); Operation(3, 128, 0, u, Constant(3, 1)); Operation(3, 130, 65535, Constant(3, 0), Constant(3, 1));
        Operation(3, 134, 21845, u, v); Operation(3, 137, 0, u, v); Operation(3, 194, 8191, u, v);
        Operation(4, 114, unchecked((uint)-7), a); Operation(5, 114, unchecked((ulong)-7L), a); Operation(5, 113, 65535, u);
        Operation(2, 114, unchecked((uint)-7), Constant(5, 0xfffffff9)); Operation(3, 113, 0xabcd, Constant(5, 0x12345678abcd));
        foreach (var (op, bits) in new (uint, ulong)[] { (170, 0), (171, 1), (172, 1), (173, 0), (174, 1), (175, 0), (176, 0), (177, 1), (178, 0), (179, 1) }) Operation(1, op, bits, a, b);
        Operation(2, 169, unchecked((uint)-7), yes, a, b);
        var vector = next++; Emit(51, 6, vector, a, b); var sum = next++; Emit(52, 6, sum, 128, vector, vector); Operation(2, 81, unchecked((uint)-14), sum, 0);
        byte[] Bytes() => MemoryMarshal.AsBytes(words.ToArray().AsSpan()).ToArray();
        Dictionary<uint, ulong> Results(byte[] bytes)
        {
            var data = MemoryMarshal.Cast<byte, uint>(bytes.AsSpan()); var result = new Dictionary<uint, ulong>();
            for (var at = 5; at < data.Length; at += (int)(data[at] >> 16))
                if ((data[at] & 65535) is 41 or 42 or 43) result[data[at + 2]] = (data[at] & 65535) == 41 ? 1 : (data[at] & 65535) == 42 ? 0 : data[at + 3] | ((data[at] >> 16) == 5 ? (ulong)data[at + 4] << 32 : 0);
            return result;
        }
        var normal = Results(Freeze(Bytes())); foreach (var pair in expected) Check(normal[pair.Key] == pair.Value, "Int16 constant operation preserves width and signed extension: " + pair.Key);
        // Every raw signed pattern must become a valid sign-extended literal, including metadata capture's low sixteen bits.
        var capture = typeof(RDPipelineSpecializationConstant).GetMethod("CaptureValue", BindingFlags.NonPublic | BindingFlags.Instance)!;
        using var signed = new RDPipelineSpecializationConstant<short>(); using var unsigned = new RDPipelineSpecializationConstant<ushort>();
        for (var bits = 0; bits <= ushort.MaxValue; bits++)
        {
            signed.Value = unchecked((short)bits); unsigned.Value = (ushort)bits;
            var s = ((int, ulong))capture.Invoke(signed, null)!; var t = ((int, ulong))capture.Invoke(unsigned, null)!;
            Check(s == (8, (ulong)bits) && t == (9, (ulong)bits), "Narrow capture never reads beyond the two-byte value.");
        }
        var minimal = words.Take(5).Concat(new uint[] { 4u << 16 | 21, 2, 16, 1, 4u << 16 | 71, 20, 1, 0, 4u << 16 | 50, 2, 20, 0xfffffff9 }).ToArray();
        byte[] Minimal() => MemoryMarshal.AsBytes(minimal.AsSpan()).ToArray();
        Check(Results(Freeze(Minimal(), new() { [0] = (8, 0x8000) }))[20] == 0xffff8000, "Signed override emits a sign-extended word.");
        foreach (var literal in new uint[] { 0xfff9, 0x100fff9 }) { minimal[^1] = literal; Reject<ArgumentException>(() => Freeze(Minimal())); }
        minimal[^1] = 0xfffffff9;
        Reject<ArgumentException>(() => Freeze(Minimal(), new() { [0] = (9, 7) })); Reject<ArgumentException>(() => Freeze(Minimal(), new() { [0] = (8, 65536) }));
        void Undefined(uint op, uint left, uint right) { var start = words.Count; Operation(2, op, 0, left, right); Reject<ArgumentException>(() => Freeze(Bytes())); words.RemoveRange(start, words.Count - start); }
        Undefined(135, a, Constant(2, 0)); Undefined(135, Constant(2, 0x8000), Constant(2, 65535)); Undefined(196, a, Constant(3, 16));
        foreach (var extent in new ushort[] { 0, 0x8000, 0xffff }) { var start = words.Count; Emit(28, 7, 2, Constant(2, extent)); Reject<ArgumentException>(() => Freeze(Bytes())); words.RemoveRange(start, words.Count - start); }
        Emit(28, 7, 2, Constant(3, 3)); var array = MemoryMarshal.Cast<byte, uint>(Freeze(Bytes()).AsSpan());
        uint normalized = 0, lengthType = 0;
        for (var at = 5; at < array.Length; at += (int)(array[at] >> 16)) if ((array[at] & 65535) == 28 && array[at + 1] == 7) normalized = array[at + 3];
        for (var at = 5; at < array.Length; at += (int)(array[at] >> 16)) if ((array[at] & 65535) == 43 && array[at + 2] == normalized) { Check(array[at + 3] == 3, "Normalized extent preserves the positive value."); lengthType = array[at + 1]; }
        var isWord = false; for (var at = 5; at < array.Length; at += (int)(array[at] >> 16)) if ((array[at] & 65535) == 21 && array[at + 1] == lengthType) isWord = array[at + 2] == 32 && array[at + 3] == 0;
        Check(normalized != 0 && isWord, "Positive 16-bit array extent uses a 32-bit unsigned layout selector.");
        Integer16Requirements(); ExportInteger16Modules();
        Console.WriteLine("PASS Int16/UInt16 capture, width-correct scalar/composite arithmetic, conversions, literal extension and undefined/bounded layout guards.");
    }
    private static void Integer16Requirements()
    {
        var type = typeof(RenderingDevice).Assembly.GetType("Electron2D.ShaderPrecision")!;
        var read = type.GetMethod("ReadRequirements", BindingFlags.NonPublic | BindingFlags.Static)!; var narrow = type.GetMethod("ReadNarrowRequirements", BindingFlags.NonPublic | BindingFlags.Static)!;
        int Parse(uint capability, uint storage, bool floating = false)
        {
            uint[] words = [0x07230203, 0x10000, 0, 100, 0, 2u << 16 | 17, capability, .. floating ? new uint[] { 3u << 16 | 22, 1, 16 } : new uint[] { 4u << 16 | 21, 1, 16, 1 }, 3u << 16 | 30, 2, 1, 4u << 16 | 32, 3, storage, 2, 4u << 16 | 59, 3, 4, storage];
            var code = MemoryMarshal.AsBytes(words.AsSpan()).ToArray();
            try { read.Invoke(null, [code]); return (int)narrow.Invoke(null, [code])!; }
            catch (TargetInvocationException error) when (error.InnerException is not null) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
        }
        Check(Parse(22, 7) == 32 && Parse(4433, 12) == 1 && Parse(4434, 2) == 3 && Parse(4435, 9) == 36 && Parse(4436, 1) == 8 && Parse(4436, 3) == 8, "Integer arithmetic/storage are independent and integer push requires exact arithmetic reconstruction.");
        foreach (var storage in new uint[] { 4, 6, 7 }) Check(Parse(22, storage) == 32, "Private/workgroup/function integer data uses Int16 arithmetic.");
        Reject<ArgumentException>(() => Parse(22, 7, true)); Reject<ArgumentException>(() => Parse(9, 7)); Reject<ArgumentException>(() => Parse(22, 12));
    }
    private static void ExportInteger16Modules()
    {
        var directory = Environment.GetEnvironmentVariable("ELECTRON2D_INT16_OUTPUT"); var shaders = Environment.GetEnvironmentVariable("ELECTRON2D_INT16_SHADERS"); if (directory is null || shaders is null) return;
        Directory.CreateDirectory(directory);
        var reflect = typeof(RenderingDevice).Assembly.GetType("Electron2D.ShaderCompiler")!.GetMethod("ValidateDeviceGraphics", BindingFlags.NonPublic | BindingFlags.Static)!;
        void Interface(string fragment)
        {
            try { reflect.Invoke(null, [File.ReadAllBytes(System.IO.Path.Combine(shaders, "io.vert.glsl.spv")), File.ReadAllBytes(System.IO.Path.Combine(shaders, fragment))]); }
            catch (TargetInvocationException error) when (error.InnerException is not null) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
        }
        Interface("io.frag.glsl.spv"); Reject<ArgumentException>(() => Interface("io-mismatch.frag.glsl.spv"));
        Console.WriteLine("PASS Int16 interstage metadata and signedness mismatch; native stage16 execution requires its physical feature.");
        var remap = typeof(RenderingDevice).GetMethod("Remap", BindingFlags.NonPublic | BindingFlags.Static)!;
        foreach (var source in Directory.EnumerateFiles(shaders, "*.spv"))
        {
            var stage = source.Contains(".vert.", StringComparison.Ordinal) ? RenderingDevice.ShaderStage.Vertex : source.Contains(".frag.", StringComparison.Ordinal) ? RenderingDevice.ShaderStage.Fragment : RenderingDevice.ShaderStage.Compute;
            var frozen = Freeze(File.ReadAllBytes(source)); File.WriteAllBytes(System.IO.Path.Combine(directory, System.IO.Path.GetFileName(source) + "-frozen.spv"), frozen);
            var translated = remap.Invoke(null, [frozen, stage])!; File.WriteAllBytes(System.IO.Path.Combine(directory, System.IO.Path.GetFileName(source) + "-translated.spv"), (byte[])translated.GetType().GetField("Item1")!.GetValue(translated)!);
        }
    }
}
