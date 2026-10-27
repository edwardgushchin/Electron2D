using System.Runtime.InteropServices;

namespace Electron2D;

internal static partial class ShaderPrecision
{
    internal static int ReadNarrowRequirements(byte[] code)
    {
        // ReadRequirements validates instruction bounds before this capability/storage pass.
        var words = MemoryMarshal.Cast<byte, uint>(code.AsSpan()); var declared = 0; var hasHalf = false;
        var types = new Dictionary<uint, uint[]>(); var blocks = new HashSet<uint>(); var variables = new List<(uint Type, uint Storage)>();
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
        {
            var count = (int)(words[at] >> 16); var op = words[at] & 65535;
            if (op == 22 && count == 3 && words[at + 2] == 16) hasHalf = true;
            if (op == 17) declared |= words[at + 1] switch { 9 => 16, 4433 => 1, 4434 => 3, 4435 => 4, 4436 => 8, _ => 0 };
            if (op is >= 19 and <= 33 && count >= 2) types.Add(words[at + 1], words.Slice(at, count).ToArray());
            if (op == 71 && count == 3 && words[at + 2] == 3) blocks.Add(words[at + 1]);
            if (op == 59 && count >= 4) variables.Add((words[at + 1], words[at + 3]));
        }
        if (!hasHalf) return declared;
        var cache = new Dictionary<uint, bool>(); var visiting = new HashSet<uint>();
        bool HalfType(uint id)
        {
            if (cache.TryGetValue(id, out var value)) return value;
            if (!types.TryGetValue(id, out var type)) throw new ArgumentException("A shader variable refers to an unknown type.");
            if (visiting.Count >= 128 || !visiting.Add(id)) throw new ArgumentException("Shader value types are cyclic or exceed the module depth.");
            var op = type[0] & 65535;
            value = op switch
            {
                22 when type.Length == 3 => type[2] == 16,
                23 or 24 or 28 or 29 when type.Length >= 3 => HalfType(type[2]),
                32 when type.Length == 4 => HalfType(type[3]),
                30 => type.Skip(2).Any(HalfType),
                _ => false
            };
            visiting.Remove(id); cache.Add(id, value); return value;
        }
        var required = 0;
        foreach (var variable in variables)
        {
            if (!HalfType(variable.Type)) continue;
            var pointer = types[variable.Type];
            var bit = variable.Storage switch { 12 => 1, 2 => blocks.Contains(pointer[3]) ? 1 : 3, 9 => 4, 1 or 3 => 8, 4 or 6 or 7 => 16, _ => throw new NotSupportedException("This half storage class requires its native resource adapter.") };
            if ((declared & bit) != bit) throw new ArgumentException("A half shader variable is missing its storage/arithmetic capability.");
            required |= bit;
        }
        return declared | ((required & 4) != 0 ? 16 : 0);
    }
}
