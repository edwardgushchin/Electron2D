using System.Runtime.InteropServices;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private static (byte[] Code, Dictionary<uint, uint> Images) LowerReadOnlyImages(byte[] code, ShaderStage stage)
    {
        if (stage == ShaderStage.Compute) return (code, []);
        var words = MemoryMarshal.Cast<byte, uint>(code.AsSpan()); var instructions = new List<uint[]>();
        for (var at = 5; at < words.Length;)
        {
            var count = (int)(words[at] >> 16); if (count == 0 || count > words.Length - at) throw new ArgumentException("Truncated readonly image instruction.", nameof(code));
            instructions.Add(words.Slice(at, count).ToArray()); at += count;
        }
        static uint Op(uint[] item) => item[0] & 65535;
        var images = instructions.Where(i => Op(i) == 25 && i.Length >= 9 && i[3] == 1 && i[7] == 2).ToDictionary(i => i[1]);
        if (images.Count == 0) return (code, []);
        var pointers = instructions.Where(i => Op(i) == 32 && i.Length == 4).ToDictionary(i => i[1], i => i[3]);
        var readOnly = instructions.Where(i => Op(i) == 71 && i.Length == 3 && i[2] == 24).Select(i => i[1]).ToHashSet();
        var variables = instructions.Where(i => Op(i) == 59 && i.Length >= 4 && i[3] == 0 && pointers.TryGetValue(i[1], out var image) && images.ContainsKey(image)).ToArray();
        if (variables.Any(v => !readOnly.Contains(v[2]))) throw new NotSupportedException("Graphics storage images require readonly declarations.");
        if (images.Values.Any(i => i[4] != 0 || i[5] != 0 || i[6] != 0)) throw new NotSupportedException("Graphics storage images require ordinary color images.");
        var formats = variables.ToDictionary(v => v[2], v => images[pointers[v[1]]][8]);
        var translated = images.Keys.ToHashSet();
        var definitions = new List<uint[]>(); var next = words[3];
        uint Fresh() => next < 1_048_576 ? next++ : throw new ArgumentException("Readonly image expansion exceeds the module identity bound.");
        var integer = instructions.FirstOrDefault(i => Op(i) == 21 && i.Length == 4 && i[2] == 32 && i[3] == 1)?[1] ?? 0;
        if (integer == 0) { integer = Fresh(); definitions.Add([(4u << 16) | 21, integer, 32, 1]); }
        var zero = Fresh(); definitions.Add([(4u << 16) | 43, integer, zero, 0]);
        var values = instructions.Where(i => i.Length >= 3 && translated.Contains(i[1])).ToDictionary(i => i[2], i => i[1]);
        var output = new List<uint>(words.Length); output.AddRange(words[..5].ToArray()); var emitted = false;
        foreach (var item in instructions)
        {
            var op = Op(item);
            if (!emitted && op == 54) { foreach (var definition in definitions) output.AddRange(definition); emitted = true; }
            if (op == 71 && item.Length == 3 && item[2] == 24 && formats.ContainsKey(item[1])) continue;
            if (op == 25 && translated.Contains(item[1])) { item[7] = 1; item[8] = 0; output.AddRange(item); continue; }
            if (op == 98 && item.Length >= 5 && values.ContainsKey(item[3]))
            {
                if (item.Length != 5 && !(item.Length == 6 && item[5] == 0)) throw new NotSupportedException("Readonly graphics image operands require their native image-load adapter.");
                output.AddRange([(7u << 16) | 95, item[1], item[2], item[3], item[4], 2, zero]); continue;
            }
            if (op == 104 && item.Length == 4 && values.ContainsKey(item[3])) { output.AddRange([(5u << 16) | 103, item[1], item[2], item[3], zero]); continue; }
            if (op == 99 && item.Length >= 4 && values.ContainsKey(item[1])) throw new ArgumentException("A readonly graphics image cannot be written.", nameof(code));
            output.AddRange(item);
        }
        output[3] = next; return (MergeReadOnlyImageTypes(output), formats);
    }
    private static byte[] MergeReadOnlyImageTypes(List<uint> words)
    {
        // Readonly conversion can make an image and its pointer identical to an existing sampled type.
        var aliases = new Dictionary<uint, uint>(); var types = new Dictionary<string, uint>(); var output = new List<uint>(words.Count);
        output.AddRange(words.Take(5));
        uint Resolve(uint id) { while (aliases.TryGetValue(id, out var target)) id = target; return id; }
        for (var at = 5; at < words.Count;)
        {
            var count = (int)(words[at] >> 16); var item = words.GetRange(at, count).ToArray(); var op = item[0] & 65535; at += count;
            if (op is 27 or 28 or 29) item[2] = Resolve(item[2]);
            if (op == 32) item[3] = Resolve(item[3]);
            if (op is 30 or 33) for (var i = 2; i < item.Length; i++) item[i] = Resolve(item[i]);
            if (op is 25 or 27 or 32 or 33)
            {
                var key = op + ":" + string.Join(',', item.Skip(2));
                if (types.TryGetValue(key, out var original)) { aliases.Add(item[1], original); continue; }
                types.Add(key, item[1]);
            }
            // Later first operands are result types or value IDs, never type-valued literals.
            if (op >= 41 && item.Length > 1) item[1] = Resolve(item[1]);
            if (op == 54) item[4] = Resolve(item[4]);
            output.AddRange(item);
        }
        for (var at = 5; at < output.Count; at += (int)(output[at] >> 16))
        {
            var op = output[at] & 65535;
            if (op is 5 or 71) output[at + 1] = Resolve(output[at + 1]);
        }
        return MemoryMarshal.AsBytes(output.ToArray().AsSpan()).ToArray();
    }
}
