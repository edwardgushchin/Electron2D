using System.Runtime.InteropServices;

namespace Electron2D;

internal static partial class ShaderPrecision
{
    internal static (bool Declared, bool Buffer, bool Shared) ReadAtomicRequirements(byte[] code)
    {
        // ReadRequirements validates the shared instruction/header boundaries before this storage classification.
        var words = MemoryMarshal.Cast<byte, uint>(code.AsSpan()); var declared = false; var operations = false;
        var integers = new HashSet<uint>(); var pointers = new Dictionary<uint, (uint Storage, uint Element)>();
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
        {
            var count = (int)(words[at] >> 16); var op = words[at] & 65535;
            if (op == 17 && words[at + 1] == 12) declared = true;
            if (op == 21 && words[at + 2] == 64) integers.Add(words[at + 1]);
            if (op == 32)
            { if (count != 4) throw new ArgumentException("An atomic pointer requires an explicit storage class and element type."); pointers.Add(words[at + 1], (words[at + 2], words[at + 3])); }
            operations |= op is >= 227 and <= 242;
        }
        if (!operations || integers.Count == 0) return (declared, false, false);
        var values = new Dictionary<uint, uint>();
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
        {
            var count = (int)(words[at] >> 16); var op = words[at] & 65535;
            if (op is 55 or 57 or 59 or 60 or 61 or 65 or 66 or 67 or 70 or 83 or 120 or 169 or 245 && count >= 3 && pointers.ContainsKey(words[at + 1])) values.Add(words[at + 2], words[at + 1]);
        }
        var buffer = false; var shared = false;
        for (var at = 5; at < words.Length; at += (int)(words[at] >> 16))
        {
            var count = (int)(words[at] >> 16); var op = words[at] & 65535;
            if (op is < 227 or > 242) continue;
            var minimum = op is 227 or 232 or 233 ? 6 : op == 228 ? 5 : op is 230 or 231 ? 9 : 7;
            if (count < minimum) throw new ArgumentException("An atomic instruction is missing required operands.");
            var id = words[at + (op == 228 ? 1 : 3)];
            if (!values.TryGetValue(id, out var type) || !pointers.TryGetValue(type, out var pointer)) throw new NotSupportedException("Atomics require a typed native buffer or shared pointer.");
            if (!integers.Contains(pointer.Element)) continue;
            if (!declared) throw new ArgumentException("A 64-bit atomic instruction requires its Int64Atomics capability.");
            if (pointer.Storage is 2 or 12) buffer = true;
            else if (pointer.Storage == 4) shared = true;
            else throw new NotSupportedException("This 64-bit atomic storage class requires its native resource adapter.");
        }
        return (declared, buffer, shared);
    }
}
