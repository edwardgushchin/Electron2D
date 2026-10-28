using System.Runtime.InteropServices;

namespace Electron2D;

internal static partial class ShaderPrecision
{
    internal static int ReadNarrowRequirements(byte[] code)
    {
        // ReadRequirements validates instruction bounds before this capability/storage pass.
        var words = MemoryMarshal.Cast<byte, uint>(code.AsSpan()); var declared = 0; var hasNarrow = false;
        var types = new Dictionary<uint, uint[]>(); var blocks = new HashSet<uint>(); var explicitBlocks = new HashSet<uint>(); var variables = new List<(uint Type, uint Storage)>();
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
        {
            var count = (int)(words[at] >> 16); var op = words[at] & 65535;
            if (op == 21 && count == 4 && words[at + 2] is 8 or 16 || op == 22 && count == 3 && words[at + 2] == 16) hasNarrow = true;
            if (op == 17) declared |= words[at + 1] switch { 9 => 16, 22 => 32, 39 => 64, 4448 => 128, 4449 => 384, 4450 => 512, 4433 => 1, 4434 => 3, 4435 => 4, 4436 => 8, _ => 0 };
            if (op is >= 19 and <= 33 && count >= 2) types.Add(words[at + 1], words.Slice(at, count).ToArray());
            if (op == 71 && count == 3 && words[at + 2] == 3) blocks.Add(words[at + 1]);
            if (op == 71 && count == 3 && words[at + 2] == 2) explicitBlocks.Add(words[at + 1]);
            if (op == 59 && count >= 4) variables.Add((words[at + 1], words[at + 3]));
        }
        if (!hasNarrow) return declared;
        var cache = new Dictionary<uint, int>(); var visiting = new HashSet<uint>();
        int Arithmetic(uint id)
        {
            if (cache.TryGetValue(id, out var value)) return value;
            if (!types.TryGetValue(id, out var type)) throw new ArgumentException("A shader variable refers to an unknown type.");
            if (visiting.Count >= 128 || !visiting.Add(id)) throw new ArgumentException("Shader value types are cyclic or exceed the module depth.");
            var op = type[0] & 65535;
            value = op switch
            {
                22 when type.Length == 3 && type[2] == 16 => 16,
                21 when type.Length == 4 && type[2] == 16 => 32,
                21 when type.Length == 4 && type[2] == 8 => 64,
                23 or 24 or 28 or 29 when type.Length >= 3 => Arithmetic(type[2]),
                32 when type.Length == 4 && type[2] == 5349 => 0,
                32 when type.Length == 4 => Arithmetic(type[3]),
                30 => type.Skip(2).Aggregate(0, (mask, element) => mask | Arithmetic(element)),
                _ => 0
            };
            visiting.Remove(id); cache.Add(id, value); return value;
        }
        var pushArithmetic = 0;
        foreach (var variable in variables)
        {
            var arithmetic = Arithmetic(variable.Type); if (arithmetic == 0) continue;
            var pointer = types[variable.Type];
            var sixteen = (arithmetic & 48) != 0; var eight = (arithmetic & 64) != 0;
            if (eight && variable.Storage is 1 or 3) throw new NotSupportedException("Native 8-bit stage input/output storage is unavailable.");
            if (variable.Storage == 4 && explicitBlocks.Contains(pointer[3])) throw new NotSupportedException("Narrow workgroup blocks require their explicit-layout adapter.");
            var buffer = (sixteen ? 1 : 0) | (eight ? 128 : 0);
            var bit = variable.Storage switch
            {
                12 => buffer,
                2 => blocks.Contains(pointer[3]) ? buffer : (sixteen ? 3 : 0) | (eight ? 384 : 0),
                9 => (sixteen ? 4 : 0) | (eight ? 512 : 0),
                1 or 3 => 8,
                4 or 6 or 7 => arithmetic,
                _ => throw new NotSupportedException("This narrow storage class requires its native resource adapter.")
            };
            if ((declared & bit) != bit) throw new ArgumentException("A narrow shader variable is missing its storage/arithmetic capability.");
            if (variable.Storage == 9) pushArithmetic |= arithmetic;
        }
        return declared | pushArithmetic;
    }
}
