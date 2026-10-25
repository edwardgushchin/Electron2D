using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using static Electron2D.MaterialLightingProgram;

namespace Electron2D;

internal sealed record CanvasStageEntry(uint Function, uint State, uint StateType);

internal static class CanvasStageProgram
{
    private static readonly UTF8Encoding UTF8 = new(false, true);
    internal static byte[] Annotate(byte[] code, CanvasStageSource source, string prefix, int[] widths)
    {
        var module = new Module(code);
        var names = module.Instructions.Where(i => Op(i) == 5).ToDictionary(i => i[1], i => Text(i, 2));
        var function = names.Single(p => p.Value.StartsWith(source.Function + "(", StringComparison.Ordinal)).Key;
        var state = names.Single(p => p.Value == source.State).Key;
        var text = UTF8.GetBytes(prefix + "1:" + function.ToString(CultureInfo.InvariantCulture) + ":" + state.ToString(CultureInfo.InvariantCulture));
        var bytes = new byte[(text.Length + 4) & ~3]; text.CopyTo(bytes, 0);
        var marker = Instruction(7, [module.Header[3]++, .. MemoryMarshal.Cast<byte, uint>(bytes).ToArray()]);
        var at = module.Instructions.FindIndex(i => Category(Op(i)) >= 6);
        module.Instructions.Insert(at < 0 ? module.Instructions.Count : at, marker);
        var result = module.Write(); _ = Read(result, prefix, widths); return result;
    }

    private static string Text(uint[] instruction, int first)
    {
        var bytes = MemoryMarshal.AsBytes(instruction.AsSpan(first)); var end = bytes.IndexOf((byte)0);
        if (end < 0 || bytes[(end + 1)..].IndexOfAnyExcept((byte)0) >= 0) throw new ArgumentException("Invalid canvas stage metadata string padding.");
        return UTF8.GetString(bytes[..end]);
    }

    internal static CanvasStageEntry? Read(byte[] code, string prefix, int[] widths)
    {
        var module = new Module(code);
        var records = module.Instructions.Where(i => Op(i) == 7).Select(i => Text(i, 2)).Where(s => s.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
        if (records.Length == 0) return null;
        if (records.Length != 1) throw new ArgumentException("A canvas shader has at most one authored stage entry.");
        var parts = records[0].Split(':');
        if (parts.Length != 5 || parts[2] != "1") throw new NotSupportedException("Unsupported canvas stage metadata version.");
        uint ID(string value)
        {
            if (!uint.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id == 0 || id >= module.Header[3] || id.ToString(CultureInfo.InvariantCulture) != value)
                throw new ArgumentException("Canvas stage metadata requires canonical live module IDs.");
            return id;
        }
        var function = ID(parts[3]); var state = ID(parts[4]);
        var entries = module.Instructions.Where(i => Op(i) == 15).ToArray();
        if (entries.Length != 1 || entries[0].Length < 4 || entries[0][1] != 4) throw new NotSupportedException("Authored canvas stages require a fragment module.");
        var types = module.Instructions.Where(i => Op(i) is >= 19 and <= 33).ToDictionary(i => i[1]);
        var functions = module.Instructions.Where(i => Op(i) == 54 && i[2] == function).ToArray();
        if (functions.Length != 1 || functions[0].Length != 5 || !types.TryGetValue(functions[0][1], out var result) || Op(result) != 19 ||
            !types.TryGetValue(functions[0][4], out var signature) || Op(signature) != 33 || signature.Length != 3 || signature[2] != functions[0][1])
            throw new ArgumentException("The canvas stage function must return void and have no parameters.");
        var variables = module.Instructions.TakeWhile(i => Op(i) != 54).Where(i => Op(i) == 59 && i[2] == state).ToArray();
        if (variables.Length != 1 || variables[0].Length < 4 || variables[0][3] != 6 || !types.TryGetValue(variables[0][1], out var pointer) || Op(pointer) != 32 || pointer.Length != 4 || pointer[2] != 6 ||
            !types.TryGetValue(pointer[3], out var structure) || Op(structure) != 30 || structure.Length != widths.Length + 2)
            throw new ArgumentException("Canvas stage state requires its private typed structure.");
        for (var field = 0; field < widths.Length; field++)
        {
            if (!types.TryGetValue(structure[field + 2], out var type)) throw new ArgumentException("Missing canvas stage field type.");
            var width = widths[field];
            if (width == 0) { if (Op(type) != 20 || type.Length != 2) throw new ArgumentException("Canvas stage flags must be Boolean."); continue; }
            if (width > 1)
            {
                if (Op(type) != 23 || type.Length != 4 || type[3] != width || !types.TryGetValue(type[2], out type)) throw new ArgumentException("Invalid canvas stage vector shape.");
            }
            if (Op(type) != 22 || type.Length != 3 || type[2] != 32) throw new ArgumentException("Canvas stage values require 32-bit floating point.");
        }
        uint owner = 0; var calls = 0;
        foreach (var instruction in module.Instructions)
        {
            if (Op(instruction) == 54) owner = instruction[2];
            if (Op(instruction) != 57 || instruction[3] != function) continue;
            if (owner != entries[0][2] || instruction.Length != 4) throw new ArgumentException("The canvas stage marker call belongs only to the fragment entry.");
            calls++;
        }
        if (calls != 1) throw new ArgumentException("Canvas stage metadata requires one fragment-entry marker call.");
        return new(function, state, structure[1]);
    }

    internal static byte[] RemoveMarker(byte[] code, CanvasStageEntry? entry, string prefix)
    {
        if (entry is null) return code;
        var module = new Module(code);
        if (module.Instructions.RemoveAll(i => Op(i) == 57 && i[3] == entry.Function) != 1)
            throw new ArgumentException("The native canvas stage marker call is absent.");
        module.Instructions.RemoveAll(i => Op(i) == 7 && Text(i, 2).StartsWith(prefix, StringComparison.Ordinal));
        return module.Write();
    }
}
