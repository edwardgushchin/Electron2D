using System.Runtime.InteropServices;
using System.Text;
using static Electron2D.MaterialLightingProgram;

namespace Electron2D;

internal static partial class TexelBufferProgram
{
    private readonly record struct Library(uint Decode, uint Encode);

    private static Library ImportLibrary(Module target)
    {
        var bytes = BuiltInShaders.Read("TexelBuffer.comp.spv");
        var relocations = MemoryMarshal.Cast<byte, uint>(BuiltInShaders.Read("TexelBuffer.comp.reloc"));
        var words = MemoryMarshal.Cast<byte, uint>(bytes.AsSpan()); var shift = target.Header[3];
        foreach (var offset in relocations) words[(int)offset] = checked(words[(int)offset] + shift);
        var library = new Module(bytes); target.Header[3] = checked(shift + library.Header[3]);
        var aliases = new Dictionary<uint, uint>(); var duplicates = new HashSet<uint[]>(); var keys = new Dictionary<string, uint>();
        bool Unique(uint op) => op is >= 19 and <= 27 or 32 or 33 or 11;
        string Key(uint[] i) => i[0] + ":" + string.Join(',', i.AsSpan(2).ToArray());
        uint Alias(uint id) { while (aliases.TryGetValue(id, out var next)) id = next; return id; }
        foreach (var i in target.Instructions) if (Unique(Op(i))) keys.TryAdd(Key(i), i[1]);
        var position = 5;
        foreach (var i in library.Instructions)
        {
            for (var operand = 1; operand < i.Length; operand++) if (relocations.BinarySearch((uint)(position + operand)) >= 0) i[operand] = Alias(i[operand]);
            if (Unique(Op(i)))
            {
                if (keys.TryGetValue(Key(i), out var existing)) { aliases.Add(i[1], existing); duplicates.Add(i); }
                else keys.Add(Key(i), i[1]);
            }
            position += i.Length;
        }
        position = 5;
        foreach (var i in library.Instructions)
        {
            for (var operand = 1; operand < i.Length; operand++) if (relocations.BinarySearch((uint)(position + operand)) >= 0) i[operand] = Alias(i[operand]);
            position += i.Length;
        }
        var names = library.Instructions.Where(i => Op(i) == 5).Select(i => (Key: Encoding.UTF8.GetString(MemoryMarshal.AsBytes(i.AsSpan(2))).TrimEnd('\0'), Value: i[1])).ToArray();
        uint Function(string name) => names.Single(p => p.Key.StartsWith(name + "(", StringComparison.Ordinal)).Value;
        var result = new Library(Function("e2dDecode"), Function("e2dEncode"));
        var entry = library.Instructions.Single(i => Op(i) == 15)[2]; var globals = new List<uint[]>(); var functions = new List<uint[]>(); var active = 0u;
        foreach (var i in library.Instructions)
        {
            var op = Op(i); if (op == 54) active = i[2];
            if (active != 0) { if (active != entry) functions.Add(i); if (op == 56) active = 0; continue; }
            if (duplicates.Contains(i) || Category(op) != 10 && op != 11 && !(op == 71 && i[2] is 2 or 3 or 6 || op == 72) || op == 59) continue;
            globals.Add(i);
        }
        var firstFunction = target.Instructions.FindIndex(i => Op(i) == 54);
        target.Instructions.InsertRange(firstFunction, globals.Where(i => Op(i) != 11));
        var firstDeclaration = target.Instructions.FindIndex(i => Category(Op(i)) == 10);
        target.Instructions.InsertRange(firstDeclaration, globals.Where(i => Op(i) == 11));
        target.Instructions.AddRange(functions);
        return result;
    }
}
