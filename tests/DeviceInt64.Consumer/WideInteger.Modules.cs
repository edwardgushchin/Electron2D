using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class WideInteger
{
    private static byte[] WideLayout(byte[] bytes)
    {
        var words = MemoryMarshal.Cast<byte, uint>(bytes.AsSpan()).ToArray(); var code = new List<uint[]>();
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16)) code.Add(words[at..(at + (int)(words[at] >> 16))]);
        uint Op(uint[] i) => i[0] & 65535;
        var types = code.Where(i => Op(i) is >= 19 and <= 39).ToDictionary(i => i[1]); var wide = types.Values.Single(i => Op(i) == 21 && i[2] == 64 && i[3] == 0);
        var values = code.Where(i => i.Length > 2 && types.ContainsKey(i[1]) && Op(i) >= 41 && Op(i) is not (71 or 72)).ToDictionary(i => i[2], i => i[1]);
        var indices = code.Where(i => Op(i) == 113 && types[i[1]][2] == 32 && types[values[i[3]]][2] == 64).ToDictionary(i => i[2], i => i[3]);
        var lengths = code.Where(i => Op(i) == 43 && i.Length == 4 && types[i[1]][0] % 65536 == 21).ToDictionary(i => i[2], i => i[3]);
        var result = new List<uint>(words[..5]); var moved = false; var arrays = 0; var accesses = 0;
        foreach (var instruction in code)
        {
            var op = Op(instruction); if (!moved && op is >= 19 and <= 39) { result.AddRange(wide); moved = true; }
            if (ReferenceEquals(instruction, wide)) continue;
            var i = instruction.ToArray();
            if (op == 28 && lengths.TryGetValue(i[3], out var count)) { var id = words[3]++; result.AddRange([5u << 16 | 43, wide[1], id, count, 0]); i[3] = id; arrays++; }
            if (op is 65 or 66) for (var j = 4; j < i.Length; j++) if (indices.TryGetValue(i[j], out var value)) { i[j] = value; accesses++; }
            result.AddRange(i);
        }
        Check(arrays > 0 && accesses > 0, "Fixture exercises native 64-bit array extents and access-chain indices."); result[3] = words[3]; var module = MemoryMarshal.AsBytes(result.ToArray().AsSpan()).ToArray();
        if (Environment.GetEnvironmentVariable("ELECTRON2D_INT64_OUTPUT") is { } output) File.WriteAllBytes(System.IO.Path.Combine(output, "wide-layout.comp.spv"), module);
        return module;
    }
    private static void Mixed(RenderingDevice device)
    {
        using var code = Code("mixed.comp.hlsl"); var shader = device.ShaderCreateFromSPIRV(code); var pipeline = device.ComputePipelineCreate(shader);
        uint[] values = [0, 0x7fffffff, 0x80000001, 0xf1234567]; var input = device.StorageBufferCreate(16, MemoryMarshal.AsBytes(values.AsSpan())); var output = device.StorageBufferCreate(32);
        using var from = new RDUniform { Binding = 0, UniformType = UniformType.StorageBuffer }; from.AddID(input); using var to = new RDUniform { Binding = 1, UniformType = UniformType.StorageBuffer }; to.AddID(output); var set = device.UniformSetCreate([from, to], shader, 0);
        var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, set, 0); device.ComputeListDispatch(list, 4, 1, 1); device.ComputeListEnd(); var result = MemoryMarshal.Cast<byte, double>(device.BufferGetData(output).AsSpan());
        for (var i = 0; i < 4; i++) Check(result[i] == values[i], "HLSL UInt32-to-double native Int64 intermediate preserves unsigned values.");
        Console.WriteLine("PASS native HLSL UInt32-to-FP64 conversion through enabled Int64.");
    }
}
