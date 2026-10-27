using System.Runtime.InteropServices;
using Electron2D;
using static TextureArrayTests;
using static Electron2D.RenderingDevice;

internal static partial class RenderingDeviceSpecializationTests
{
    private static RDShaderSPIRV Compute(string name)
    {
        using var input = typeof(RenderingDeviceSpecializationTests).Assembly.GetManifestResourceStream("Electron2D.Compute.LocalSpecialization" + name + ".comp.spv")!;
        using var stream = new MemoryStream(); input.CopyTo(stream); return new() { BytecodeCompute = stream.ToArray() };
    }
    internal static void Run()
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        RunManaged(); ComputeVariants(device); Layout(device); Graphics(device); Canvas();
        Console.WriteLine("Native specialization: typed values, dependent constants, workgroups, array layouts, pipeline ownership and zero-allocation recording passed on " + device.GetDeviceName());
    }
    private static byte[] Freeze(byte[] bytes, Dictionary<uint, (int Kind, ulong Bits)>? overrides = null)
    {
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var scalar = typeof(RenderingDevice).GetNestedType("Specialization", System.Reflection.BindingFlags.NonPublic)!;
        var dictionary = (System.Collections.IDictionary)Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(typeof(uint), scalar))!;
        foreach (var pair in overrides ?? []) dictionary.Add(pair.Key, Activator.CreateInstance(scalar, flags, null, [pair.Value.Kind, (ulong)pair.Value.Bits], null));
        var type = typeof(RenderingDevice).GetNestedType("SpecializationModule", System.Reflection.BindingFlags.NonPublic)!;
        try { var module = Activator.CreateInstance(type, flags, null, [bytes, dictionary, new HashSet<uint>()], null)!; return (byte[])type.GetMethod("Freeze", flags)!.Invoke(module, null)!; }
        catch (System.Reflection.TargetInvocationException error) when (error.InnerException is not null) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
    }
    private static void Export(byte[] bytes, ShaderStage stage, string name, Dictionary<uint, (int, uint)> overrides)
    {
        var directory = Environment.GetEnvironmentVariable("ELECTRON2D_DEVICE_SPECIALIZATION_OUTPUT"); if (directory is null) return; Directory.CreateDirectory(directory);
        var frozen = Freeze(bytes, overrides.ToDictionary(p => p.Key, p => (p.Value.Item1, (ulong)p.Value.Item2))); File.WriteAllBytes(System.IO.Path.Combine(directory, name + "-frozen.spv"), frozen);
        var remap = typeof(RenderingDevice).GetMethod("Remap", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        var translated = remap.Invoke(null, [frozen, stage])!; File.WriteAllBytes(System.IO.Path.Combine(directory, name + "-translated.spv"), (byte[])translated.GetType().GetField("Item1")!.GetValue(translated)!);
    }
    private static void RunManaged()
    {
        Expressions(); Float64Expressions(); Integer64Expressions(); HalfExpressions(); Integer16Expressions(); Integer8Expressions(); Atomic64Requirements(); NarrowRequirements();
        var parser = typeof(RenderingDevice).Assembly.GetType("Electron2D.ShaderPrecision")!.GetMethod("ReadRequirements", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
        (bool Float64, bool Int64) Requirements(params uint[] instructions)
        {
            var code = MemoryMarshal.AsBytes(new uint[] { 0x07230203, 0x10000, 0, 100, 0 }.Concat(instructions).ToArray().AsSpan()).ToArray();
            try { return ((bool, bool))parser.Invoke(null, [code])!; }
            catch (System.Reflection.TargetInvocationException error) when (error.InnerException is not null) { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
        }
        bool Precision(params uint[] instructions) => Requirements(instructions).Float64;
        Check(Requirements(2u << 16 | 17, 11, 4u << 16 | 21, 1, 64, 0) == (false, true), "Int64 has an independent native requirement.");
        Check(Requirements(2u << 16 | 17, 11) == (false, true), "Unused Int64 declarations still require native opt-in.");
        Check(Requirements(2u << 16 | 17, 10, 2u << 16 | 17, 11) == (true, true), "Mixed precision requires both independent features.");
        Check(Requirements(2u << 16 | 17, 12, 4u << 16 | 21, 1, 64, 0) == (false, true), "Int64Atomics implies Int64 even without an explicit declaration.");
        Check(!Precision(3u << 16 | 22, 1, 32), "Ordinary precision needs no FP64 feature.");
        Check(Precision(2u << 16 | 17, 10, 3u << 16 | 22, 1, 64), "FP64 type and declaration require native opt-in.");
        Check(Precision(2u << 16 | 17, 10), "Even unused declared FP64 requires native opt-in.");
        Reject<ArgumentException>(() => Precision(3u << 16 | 22, 1, 64));
        Reject<ArgumentException>(() => Precision(0)); Reject<ArgumentException>(() => Precision(3u << 16 | 22, 1));
        Reject<ArgumentException>(() => Precision(2u << 16 | 22, 1)); Reject<ArgumentException>(() => Precision(3u << 16 | 17, 10, 0));
        Reject<ArgumentException>(() => Precision(4u << 16 | 21, 1, 32, 2));
        foreach (var capability in new uint[] { 8 }) Reject<NotSupportedException>(() => Precision(2u << 16 | 17, capability));
        foreach (var width in new uint[] { 128 }) Reject<NotSupportedException>(() => Precision(3u << 16 | 22, 1, width));
        Reject<ArgumentException>(() => Precision(4u << 16 | 21, 1, 64, 0));
        Console.WriteLine("PASS precision capability/type and malformed/truncated instruction guards.");
    }
    private static void Integer64Expressions()
    {
        var words = new List<uint> { 0x07230203, 0x10000, 0, 1000, 0 }; uint next = 20;
        void Emit(uint op, params uint[] args) { words.Add((uint)(args.Length + 1) << 16 | op); words.AddRange(args); }
        Emit(17, 11); Emit(20, 1); Emit(21, 2, 32, 1); Emit(21, 3, 32, 0); Emit(21, 4, 64, 1); Emit(21, 5, 64, 0); Emit(23, 6, 5, 2); Emit(23, 7, 1, 2);
        uint Constant(uint type, ulong value)
        { var id = next++; if (type == 1) Emit(value == 0 ? 49u : 48u, type, id); else if (type is 4 or 5) Emit(50, type, id, (uint)value, (uint)(value >> 32)); else Emit(50, type, id, (uint)value); return id; }
        var a = Constant(4, unchecked((ulong)-4294967303L)); var b = Constant(4, 3); var maximum = Constant(5, ulong.MaxValue); var u = Constant(5, 0x123456789abcdef0); var low = Constant(5, 0x9abcdef0); var shift = Constant(3, 40); var small = Constant(2, unchecked((uint)-7)); var yes = Constant(1, 1); var no = Constant(1, 0);
        var expected = new Dictionary<uint, ulong>();
        uint Operation(uint type, uint op, ulong value, params uint[] args) { var id = next++; Emit(52, [type, id, op, .. args]); expected.Add(id, value); return id; }
        Operation(4, 128, unchecked((ulong)-4294967300L), a, b); Operation(4, 130, unchecked((ulong)-4294967306L), a, b); Operation(4, 132, unchecked((ulong)-12884901909L), a, b);
        Operation(4, 135, unchecked((ulong)-1431655767L), a, b); Operation(4, 138, unchecked((ulong)-2L), a, b); Operation(4, 139, 1, a, b);
        Operation(5, 134, 6148914691236517205, maximum, b); Operation(5, 137, 0, maximum, b); Operation(5, 128, 0, maximum, Constant(5, 1)); Operation(5, 130, ulong.MaxValue, Constant(5, 0), Constant(5, 1));
        Operation(5, 194, 0x123456, u, shift); Operation(4, 195, ulong.MaxValue, a, shift); Operation(5, 196, 0xbcdef00000000000, u, shift);
        Operation(5, 197, ulong.MaxValue, u, maximum); Operation(5, 198, 0xedcba9876543210f, u, maximum); Operation(5, 199, 0x123456789abcdef0, u, maximum); Operation(5, 200, 0xedcba9876543210f, u); Operation(4, 126, 4294967303, a);
        Operation(1, 170, 0, u, low); Operation(1, 171, 1, u, low); Operation(1, 172, 1, u, low); Operation(1, 173, 0, a, b); Operation(1, 174, 1, u, u); Operation(1, 175, 1, b, a); Operation(1, 176, 0, u, low); Operation(1, 177, 1, a, b); Operation(1, 178, 1, u, u); Operation(1, 179, 0, b, a);
        Operation(5, 113, 0xfffffff9, small); Operation(4, 114, 0xfffffffffffffff9, small); Operation(3, 113, 0x9abcdef0, u); Operation(2, 114, 0xfffffff9, a);
        var vector = next++; Emit(51, 6, vector, u, maximum); var condition = next++; Emit(51, 7, condition, yes, no); var reverse = next++; Emit(51, 6, reverse, maximum, u); var selected = next++; Emit(52, 6, selected, 169, condition, vector, reverse); Operation(5, 81, 0x123456789abcdef0, selected, 0); Operation(5, 81, 0x123456789abcdef0, selected, 1);
        var count = Constant(5, 2); Emit(28, 8, 5, count); var array = next++; Emit(51, 8, array, u, maximum); Operation(5, 81, ulong.MaxValue, array, 1);
        byte[] Bytes() => MemoryMarshal.AsBytes(words.ToArray().AsSpan()).ToArray();
        var verifiedResults = expected.Count; var frozen = MemoryMarshal.Cast<byte, uint>(Freeze(Bytes()).AsSpan()); var actual = new Dictionary<uint, ulong>(); var types = new Dictionary<uint, uint[]>(); uint length = 0;
        for (var at = 5; at < frozen.Length; at += (int)(frozen[at] >> 16))
        {
            var op = frozen[at] & 65535; var size = (int)(frozen[at] >> 16);
            if (op == 21) types[frozen[at + 1]] = frozen[at..(at + size)].ToArray();
            if (op == 28 && frozen[at + 1] == 8) length = frozen[at + 3];
            if (op is 41 or 42 or 43) actual[frozen[at + 2]] = op == 41 ? 1ul : op == 42 ? 0ul : frozen[at + 3] | (size == 5 ? (ulong)frozen[at + 4] << 32 : 0);
        }
        foreach (var pair in expected) Check(actual[pair.Key] == pair.Value, "Wide specialization expression " + pair.Key);
        Check(length != count && actual[length] == 2, "Wide array count lowers to bounded native layout metadata.");
        var undefined = Constant(5, 0); var start = words.Count; Operation(5, 134, 0, u, undefined); Reject<ArgumentException>(() => Freeze(Bytes())); words.RemoveRange(start, words.Count - start);
        var minimum = Constant(4, 0x8000000000000000); var negative = Constant(4, ulong.MaxValue); start = words.Count; Operation(4, 135, 0, minimum, negative); Reject<ArgumentException>(() => Freeze(Bytes())); words.RemoveRange(start, words.Count - start);
        foreach (var amount in new ulong[] { 64, ulong.MaxValue, 0x100000001 })
        { var index = Constant(5, amount); start = words.Count; Operation(5, 194, 0, u, index); Reject<ArgumentException>(() => Freeze(Bytes())); words.RemoveRange(start, words.Count - start); }
        start = words.Count; Emit(50, 4, next++, 0); Reject<ArgumentException>(() => Freeze(Bytes())); words.RemoveRange(start, words.Count - start);
        foreach (var invalidCount in new ulong[] { 0, 0x100000000, ulong.MaxValue })
        { start = words.Count; Emit(28, next++, 5, Constant(5, invalidCount)); Reject<ArgumentException>(() => Freeze(Bytes())); words.RemoveRange(start, words.Count - start); }
        start = words.Count; Emit(331, 999, 38, count, count, count); Reject<ArgumentException>(() => Freeze(Bytes())); words.RemoveRange(start, words.Count - start);
        Console.WriteLine("PASS " + verifiedResults + " wide scalar/vector arithmetic, conversions, comparisons, array layout and undefined/malformed guards.");
    }
    private static void Float64Expressions()
    {
        var words = new List<uint> { 0x07230203, 0x10000, 0, 1000, 0 };
        void Emit(uint op, params uint[] args) { words.Add((uint)(args.Length + 1) << 16 | op); words.AddRange(args); }
        Emit(17, 10); Emit(20, 1); Emit(22, 2, 32); Emit(22, 3, 64); Emit(23, 4, 3, 2);
        Emit(71, 10, 1, 7); Emit(50, 3, 10, 1, 0x3ff00000); Emit(48, 1, 11); Emit(50, 2, 12, BitConverter.SingleToUInt32Bits(1.25f));
        Emit(52, 3, 13, 115, 12); Emit(52, 2, 14, 115, 10); Emit(52, 3, 15, 169, 11, 10, 13);
        Emit(51, 4, 16, 10, 13); Emit(52, 3, 17, 81, 16, 1); Emit(52, 4, 18, 169, 11, 16, 16); Emit(52, 3, 19, 81, 18, 0);
        byte[] Bytes() => MemoryMarshal.AsBytes(words.ToArray().AsSpan()).ToArray();
        Dictionary<uint, ulong> Results(byte[] code)
        {
            var data = MemoryMarshal.Cast<byte, uint>(code.AsSpan()); var result = new Dictionary<uint, ulong>();
            for (var at = 5; at < data.Length; at += (int)(data[at] >> 16)) if ((data[at] & 65535) == 43) result[data[at + 2]] = data[at + 3] | ((data[at] >> 16) == 5 ? (ulong)data[at + 4] << 32 : 0);
            return result;
        }
        var normal = Results(Freeze(Bytes())); Check(normal[10] == 0x3ff0000000000001 && normal[13] == BitConverter.DoubleToUInt64Bits(1.25) && normal[14] == BitConverter.SingleToUInt32Bits(1) && normal[15] == normal[10] && normal[17] == normal[13] && normal[19] == normal[10], "Double defaults, float conversions, vector composites/extraction and selection freeze exactly.");
        foreach (var bits in new ulong[] { 0, 0x8000000000000000, 0x3ff0000000000001, 0x7ff0000000000000, 0xfff0000000000000, 0x7ff8123456789abc })
        {
            var result = Results(Freeze(Bytes(), new() { [7] = (4, bits) })); Check(result[10] == bits && result[15] == bits && result[19] == bits, "Double overrides preserve both words, signed zero and NaN payload through selection/composites.");
        }
        Reject<ArgumentException>(() => Freeze(Bytes(), new() { [7] = (3, 0) }));
        Emit(50, 3, 20, 0); Reject<ArgumentException>(() => Freeze(Bytes())); words.RemoveRange(words.Count - 4, 4);
        Emit(50, 3, 20, 0, 0, 0); Reject<ArgumentException>(() => Freeze(Bytes()));
        Console.WriteLine("PASS double scalar/composite constants, bidirectional float conversion, exact overrides and malformed literal widths.");
    }
    private static void Expressions()
    {
        var words = new List<uint> { 0x07230203, 0x10000, 0, 1000, 0 }; uint next = 10;
        void Emit(uint op, params uint[] operands) { words.Add((uint)(operands.Length + 1) << 16 | op); words.AddRange(operands); }
        Emit(20, 1); Emit(21, 2, 32, 1); Emit(21, 3, 32, 0); Emit(22, 4, 32); Emit(23, 5, 3, 2);
        uint Constant(uint type, uint bits) { var id = next++; if (type == 1) Emit(bits != 0 ? 48u : 49u, type, id); else Emit(50, type, id, bits); return id; }
        var a = Constant(2, unchecked((uint)-7)); var b = Constant(2, 3); var u = Constant(3, 7); var v = Constant(3, 3); var yes = Constant(1, 1); var no = Constant(1, 0); var f = Constant(4, BitConverter.SingleToUInt32Bits(1.0001f));
        var expected = new Dictionary<uint, uint>();
        uint Operation(uint type, uint op, uint result, params uint[] args) { var id = next++; Emit(52, [type, id, op, .. args]); expected[id] = result; return id; }
        foreach (var (op, result) in new (uint, uint)[] { (128, unchecked((uint)-4)), (130, unchecked((uint)-10)), (132, unchecked((uint)-21)), (135, unchecked((uint)-2)), (138, uint.MaxValue), (139, 2), (195, unchecked((uint)-1)), (196, unchecked((uint)-56)), (197, unchecked((uint)-5)), (198, unchecked((uint)-6)), (199, 1) }) Operation(2, op, result, a, b);
        foreach (var (op, result) in new (uint, uint)[] { (134, 2), (137, 1), (194, 0) }) Operation(3, op, result, u, v);
        Operation(2, 126, 7, a); Operation(2, 200, 6, a); Operation(3, 113, unchecked((uint)-7), a); Operation(2, 114, 7, u); Operation(4, 115, BitConverter.SingleToUInt32Bits(1.0001f), f); Operation(4, 116, BitConverter.SingleToUInt32Bits(1), f);
        foreach (var (op, result) in new (uint, uint)[] { (164, 0), (165, 1), (166, 1), (167, 0) }) Operation(1, op, result, yes, no);
        Operation(1, 168, 1, no); Operation(2, 169, unchecked((uint)-7), yes, a, b);
        foreach (var (op, result) in new (uint, uint)[] { (170, 0), (171, 1), (172, 1), (173, 0), (174, 1), (175, 0), (176, 0), (177, 1), (178, 0), (179, 1) }) Operation(1, op, result, a, b);
        var vector = next++; Emit(51, 5, vector, u, v); var doubled = next++; Emit(52, 5, doubled, 128, vector, vector); Operation(3, 81, 6, doubled, 1);
        var shuffled = next++; Emit(52, 5, shuffled, 79, vector, doubled, 3, 0); Operation(3, 81, 6, shuffled, 0);
        var inserted = next++; Emit(52, 5, inserted, 82, u, vector, 1); Operation(3, 81, 7, inserted, 1);
        byte[] Bytes() => MemoryMarshal.AsBytes(words.ToArray().AsSpan()).ToArray();
        var frozen = MemoryMarshal.Cast<byte, uint>(Freeze(Bytes()).AsSpan()); var results = new Dictionary<uint, uint>();
        for (var i = 5; i < frozen.Length; i += (int)(frozen[i] >> 16)) if ((frozen[i] & 65535) is 41 or 42 or 43) results[frozen[i + 2]] = (frozen[i] & 65535) == 41 ? 1u : (frozen[i] & 65535) == 42 ? 0u : frozen[i + 3];
        var verifiedResults = expected.Count; foreach (var pair in expected) Check(results[pair.Key] == pair.Value, "Specialization constant operation result: " + pair.Key);
        var zero = Constant(2, 0); Operation(2, 135, 0, a, zero); Reject<ArgumentException>(() => Freeze(Bytes())); words.RemoveRange(words.Count - 6, 6);
        Operation(2, 128, 0, a); Reject<ArgumentException>(() => Freeze(Bytes())); words.RemoveRange(words.Count - 5, 5);
        var cycle = next++; Emit(52, 2, cycle, 128, cycle, b); Reject<ArgumentException>(() => Freeze(Bytes())); words.RemoveRange(words.Count - 6, 6);
        Emit(28, 6, 3, Constant(3, uint.MaxValue)); var nullArray = next++; Emit(46, 6, nullArray); var extract = next++; Emit(52, 3, extract, 81, nullArray, 0); Reject<ArgumentException>(() => Freeze(Bytes()));
        Console.WriteLine("Specialization expression checks: " + verifiedResults + " scalar/composite results and malformed/undefined/cyclic/bounded cases.");
    }
    private static void ComputeVariants(RenderingDevice device)
    {
        using var module = Compute(""); Export(module.BytecodeCompute, ShaderStage.Compute, "compute-specialized", new() { [1] = (0, 1), [2] = (1, unchecked((uint)-3)), [3] = (2, 3), [4] = (3, BitConverter.SingleToUInt32Bits(2)), [5] = (2, 4) }); var shader = device.ShaderCreateFromSPIRV(module);
        using var flag = new RDPipelineSpecializationConstant<bool> { ConstantID = 1, Value = true };
        using var bias = new RDPipelineSpecializationConstant<int> { ConstantID = 2, Value = -3 };
        using var count = new RDPipelineSpecializationConstant<uint> { ConstantID = 3, Value = 3 };
        using var scale = new RDPipelineSpecializationConstant<float> { ConstantID = 4, Value = 2 };
        using var group = new RDPipelineSpecializationConstant<uint> { ConstantID = 5, Value = 4 };
        var normal = device.ComputePipelineCreate(shader); var custom = device.ComputePipelineCreate(shader, [flag, bias, count, scale, group]);
        flag.Value = false; var disabled = device.ComputePipelineCreate(shader, [flag, group]); bias.Value = 900; count.Value = 900; scale.Value = 900; group.Value = 900;
        using var empty = new RDPipelineSpecializationConstant<int>(); Check(empty.Value is null && empty.ConstantID == 0, "Typed specialization preserves unassigned value and identifier defaults.");
        var property = (PropertyDescriptor<RDPipelineSpecializationConstant<int>, int?>)empty.GetPropertyList().Single(p => p.Name == "Value"); empty.Value = 7; Check(property.GetValue(empty) == 7 && empty.PropertyCanRevert(property), "Nullable scalar metadata exposes assigned value."); empty.RevertProperty(property); Check(empty.Value is null, "Metadata reverts to unassigned value.");
        Reject<ArgumentException>(() => device.ComputePipelineCreate(shader, [empty]));
        Reject<NotSupportedException>(() => new RDPipelineSpecializationConstant<char>());
        Reject<ArgumentException>(() => device.ComputePipelineCreate(shader, [bias, bias]));
        bias.ConstantID = 3; Reject<ArgumentException>(() => device.ComputePipelineCreate(shader, [bias])); bias.ConstantID = -1; Reject<ArgumentOutOfRangeException>(() => device.ComputePipelineCreate(shader, [bias])); bias.ConstantID = 90; Reject<ArgumentException>(() => device.ComputePipelineCreate(shader, [bias]));
        group.Value = 0; Reject<ArgumentException>(() => device.ComputePipelineCreate(shader, [group])); group.Value = 1025; Reject<ArgumentException>(() => device.ComputePipelineCreate(shader, [group]));
        Reject<ArgumentNullException>(() => device.ComputePipelineCreate(shader, [null!])); scale.Dispose(); Reject<ObjectDisposedException>(() => device.ComputePipelineCreate(shader, [scale]));
        var buffer = device.StorageBufferCreate(16); using var u = new RDUniform { Binding = 0, UniformType = UniformType.StorageBuffer }; u.AddID(buffer); var set = device.UniformSetCreate([u], shader, 0); var result = new float[4];
        void Dispatch(RID pipeline)
        { var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, set, 0); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); device.BufferGetData(buffer, MemoryMarshal.AsBytes(result.AsSpan())); }
        Dispatch(normal); Check(result[0] == 11 && result[1] == 0, "Omitted descriptions retain scalar/workgroup defaults.");
        flag.Dispose(); bias.Dispose(); count.Dispose(); group.Dispose(); module.BytecodeCompute = [];
        Dispatch(custom); Check(result.SequenceEqual(new float[] { 62, 63, 64, 65 }), "Copied bool/int/uint/float specialization executes dependent integer expressions and a four-lane workgroup.");
        Dispatch(disabled); Check(result.All(v => v == -1), "A second independent variant keeps its copied Boolean value.");
        using var modeModule = Compute(""); var modeWords = MemoryMarshal.Cast<byte, uint>(modeModule.BytecodeCompute.AsSpan()).ToArray(); uint workgroup = 0; uint[] dimensions = [];
        for (var i = 5; i < modeWords.Length; i += (int)(modeWords[i] >> 16)) if ((modeWords[i] & 65535) == 71 && modeWords[i + 2] == 11 && modeWords[i + 3] == 25) workgroup = modeWords[i + 1];
        for (var i = 5; i < modeWords.Length; i += (int)(modeWords[i] >> 16)) if ((modeWords[i] & 65535) == 51 && modeWords[i + 2] == workgroup) dimensions = modeWords[(i + 3)..(i + 6)];
        var rewritten = new List<uint>(modeWords[..5]);
        for (var i = 5; i < modeWords.Length; i += (int)(modeWords[i] >> 16))
        {
            var length = (int)(modeWords[i] >> 16); var op = modeWords[i] & 65535;
            if (op == 71 && modeWords[i + 1] == workgroup && modeWords[i + 2] == 11) continue;
            if (op == 16 && modeWords[i + 2] == 17) rewritten.AddRange([(6u << 16) | 331, modeWords[i + 1], 38, .. dimensions]); else rewritten.AddRange(modeWords[i..(i + length)]);
        }
        modeModule.BytecodeCompute = MemoryMarshal.AsBytes(rewritten.ToArray().AsSpan()).ToArray(); Export(modeModule.BytecodeCompute, ShaderStage.Compute, "mode-id-specialized", new() { [5] = (2, 4) });
        var modeShader = device.ShaderCreateFromSPIRV(modeModule); using var localSize = new RDPipelineSpecializationConstant<uint> { ConstantID = 5, Value = 4 }; var modePipeline = device.ComputePipelineCreate(modeShader, [localSize]); var originalSet = set; set = device.UniformSetCreate([u], modeShader, 0); Dispatch(modePipeline); Check(result.SequenceEqual(new float[] { 11, 12, 13, 14 }), "LocalSizeId mode resolves to the specialized four-lane native workgroup."); set = originalSet; device.FreeRID(modePipeline); device.FreeRID(modeShader);

        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); for (var i = 0; i < 16; i++) Dispatch(custom); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Dispatch(custom); var allocated = GC.GetAllocatedBytesForCurrentThread() - before; Check(allocated == 0, "Prepared specialized compute recording/readback allocates zero bytes: " + allocated); Console.WriteLine("Specialized compute: 32 cycles, " + allocated + " managed bytes.");
        device.FreeRID(custom); Check(!device.ComputePipelineIsValid(custom) && device.ComputePipelineIsValid(disabled), "Freeing a variant preserves its sibling."); device.FreeRID(shader); Check(!device.ComputePipelineIsValid(disabled) && !device.UniformSetIsValid(set), "Shader lifetime remains borrowed by specialized pipelines and sets."); device.FreeRID(disabled); device.FreeRID(normal);
    }
    private static void Layout(RenderingDevice device)
    {
        using var module = Compute("Layout"); Export(module.BytecodeCompute, ShaderStage.Compute, "layout-specialized", new() { [0] = (2, 3) }); var shader = device.ShaderCreateFromSPIRV(module);
        using var count = new RDPipelineSpecializationConstant<uint> { Value = 3 }; var pipeline = device.ComputePipelineCreate(shader, [count]); var buffer = device.StorageBufferCreate(4); var uniform = device.UniformBufferCreate(48);
        using var input = new RDUniform { Binding = 0, UniformType = UniformType.UniformBuffer }; input.AddID(uniform); using var output = new RDUniform { Binding = 1, UniformType = UniformType.StorageBuffer }; output.AddID(buffer); var set = device.UniformSetCreate([input, output], shader, 0);
        float[] values = new float[12]; values[8] = 7; device.BufferUpdate(uniform, 0, 48, MemoryMarshal.AsBytes(values.AsSpan())); float[] push = [1, 2, 11];
        var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, set, 0); Reject<ArgumentException>(() => device.ComputeListSetPushConstant(list, new byte[8], 8)); device.ComputeListSetPushConstant(list, MemoryMarshal.AsBytes(push.AsSpan()), 12); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd(); float[] result = [0]; device.BufferGetData(buffer, MemoryMarshal.AsBytes(result.AsSpan())); Check(result[0] == 18, "Specialization changes native uniform extent and compact push array extent together.");
        var small = device.UniformBufferCreate(32); input.ClearIDs(); input.AddID(small); var smallSet = device.UniformSetCreate([input, output], shader, 0); list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, smallSet, 0); device.ComputeListSetPushConstant(list, MemoryMarshal.AsBytes(push.AsSpan()), 12); Reject<ArgumentException>(() => device.ComputeListDispatch(list, 1, 1, 1)); device.ComputeListEnd();
        var bytes = module.BytecodeCompute; var words = MemoryMarshal.Cast<byte, uint>(bytes.AsSpan());
        for (var i = 5; i < words.Length; i += (int)(words[i] >> 16)) if ((words[i] & 65535) == 50 && words[i + 3] == 2) words[i + 3] = 0;
        module.BytecodeCompute = bytes; var symbolic = device.ShaderCreateFromSPIRV(module); Reject<ArgumentException>(() => device.ComputePipelineCreate(symbolic)); Check(device.ComputePipelineIsValid(device.ComputePipelineCreate(symbolic, [count])), "A valid override repairs an unusable default before native/layout validation.");
        count.Value = 33; Reject<NotSupportedException>(() => device.ComputePipelineCreate(shader, [count]));
    }
    private static RID Graphics(RenderingDevice device)
    {
        using var module = RenderingDeviceGraphicsTests.Load("DeviceSpecialization"); Export(module.BytecodeVertex, ShaderStage.Vertex, "vertex-specialized", new() { [10] = (3, 0) }); Export(module.BytecodeFragment, ShaderStage.Fragment, "fragment-specialized", new() { [1] = (0, 0), [4] = (3, BitConverter.SingleToUInt32Bits(.125f)), [11] = (2, 3) }); var shader = device.ShaderCreateFromSPIRV(module);
        using var format = new RDTextureFormat { Width = 8, Height = 8, Format = DataFormat.R8G8B8A8UNORM, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom | TextureUsageBits.Sampling }; using var view = new RDTextureView(); var image = device.TextureCreate(format, view); var target = device.FramebufferCreate([image]);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
        using var red = new RDPipelineSpecializationConstant<bool> { ConstantID = 1, Value = false }; using var scale = new RDPipelineSpecializationConstant<float> { ConstantID = 4, Value = .125f }; using var shift = new RDPipelineSpecializationConstant<float> { ConstantID = 10, Value = 0 };
        using var length = new RDPipelineSpecializationConstant<uint> { ConstantID = 11, Value = 3 };
        var normal = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend); var custom = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend, specializationConstants: [red, scale, shift, length]); red.Value = true; scale.Value = 0; var pixels = new byte[256];
        float[] inputs = new float[12]; inputs[0] = inputs[4] = inputs[8] = 1; var uniform = device.UniformBufferCreate(48, MemoryMarshal.AsBytes(inputs.AsSpan())); using var description = new RDUniform { Binding = 0, UniformType = UniformType.UniformBuffer }; description.AddID(uniform); var set = device.UniformSetCreate([description], shader, 0); float[] push = [1, 1, 1];
        void Draw(RID pipeline)
        { var list = device.DrawListBegin(target, DrawFlags.ClearColorAll, [Colors.Black]); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 0); device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(push.AsSpan()), pipeline == normal ? 8u : 12u); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); device.TextureGetData(image, 0, pixels.AsSpan()); }
        Draw(normal); Check(pixels[0] == 255 && pixels[2] == 0, "Graphics defaults render red."); Draw(custom); Check(pixels[0] == 0 && pixels[2] is >= 127 and <= 128, "Paired-stage specialization renders copied half-intensity blue.");
        var directory = Environment.GetEnvironmentVariable("ELECTRON2D_DEVICE_SPECIALIZATION_OUTPUT"); if (directory is not null) { Directory.CreateDirectory(directory); using var proof = Electron2D.Image.CreateFromData(8, 8, false, Electron2D.Image.Format.Rgba8, pixels); proof.SavePNG(System.IO.Path.Combine(directory, "specialized-blue.png")); }
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); for (var i = 0; i < 16; i++) Draw(custom); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Draw(custom); var allocated = GC.GetAllocatedBytesForCurrentThread() - before; Check(allocated == 0, "Prepared specialized graphics allocates zero bytes: " + allocated); Console.WriteLine("Specialized graphics: 32 cycles, " + allocated + " managed bytes.");
        device.FreeRID(shader); var active = device.DrawListBegin(target); Reject<ArgumentException>(() => device.DrawListBindRenderPipeline(active, custom)); device.DrawListEnd(); device.FreeRID(custom); device.FreeRID(normal); return image;
    }
    private sealed class Patch(Texture texture) : Entity { protected override void OnDraw() => DrawTextureRect(texture, new(0, 0, 8, 8), false); }
    private static void Canvas()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); using var texture = new TextureRD(); using var root = new Window { Size = new(16, 16) }; var frames = 0;
        root.Ready += _ => { var device = RenderingServer.GetRenderingDevice()!; RenderingServer.SetDefaultClearColor(Colors.Black); texture.TextureRDRID = Graphics(device); root.AddChild(new Patch(texture)); RenderingServer.FramePostDraw += Captured; };
        void Captured()
        {
            if (++frames < 3) return; RenderingServer.FramePostDraw -= Captured; using var image = root.GetTexture().GetImage()!; var pixel = image.GetPixel(4, 4); Check(pixel.R == 0 && pixel.B is > .49f and < .51f, "Specialized graphics output reaches the real retained canvas without CPU transfer.");
            var directory = Environment.GetEnvironmentVariable("ELECTRON2D_DEVICE_SPECIALIZATION_OUTPUT"); if (directory is not null) image.SavePNG(System.IO.Path.Combine(directory, "canvas-specialized.png")); root.Tree!.Quit();
        }
        Engine.Run(root);
    }
}
