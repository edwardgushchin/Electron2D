using System.Runtime.InteropServices;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private static byte[] FlattenViewBlocks(byte[] code)
    {
        var words = MemoryMarshal.Cast<byte, uint>(code.AsSpan()); var instructions = new List<uint[]>();
        var types = new Dictionary<uint, uint[]>(); var constants = new Dictionary<uint, uint>();
        var members = new Dictionary<(uint Type, uint Member), List<uint[]>>(); var selected = new HashSet<uint>();
        for (var at = 5; at < words.Length;)
        {
            var count = (int)(words[at] >> 16); if (count == 0 || count > words.Length - at) throw new ArgumentException("Truncated view-block instruction.", nameof(code));
            var item = words.Slice(at, count).ToArray(); var op = item[0] & 65535; instructions.Add(item);
            if (op is >= 19 and <= 33) types.Add(item[1], item);
            if (op == 43 && count == 4) constants[item[2]] = item[3];
            if (op == 72 && count >= 4)
            {
                if (!members.TryGetValue((item[1], item[2]), out var list)) members.Add((item[1], item[2]), list = []);
                list.Add(item);
                if (count == 5 && item[3] == 11 && item[4] == 4440) selected.Add(item[1]);
            }
            at += count;
        }
        var next = words[3]; var fields = new Dictionary<uint, uint[]>(); var declarations = new Dictionary<uint, List<uint[]>>(); var annotations = new List<uint[]>();
        foreach (var item in instructions)
        {
            if ((item[0] & 65535) != 59 || item.Length != 4 || item[3] != 1 || !types.TryGetValue(item[1], out var pointer) || !selected.Contains(pointer[3])) continue;
            var shape = types[pointer[3]]; if ((shape[0] & 65535) != 30) throw new ArgumentException("A built-in block requires a structure.", nameof(code));
            var values = new uint[shape.Length - 2]; var definitions = new List<uint[]>();
            for (var member = 0; member < values.Length; member++)
            {
                if (!members.TryGetValue((pointer[3], (uint)member), out var decorations) || !decorations.Any(d => d[3] == 11)) throw new ArgumentException("Every built-in block member needs its built-in identity.", nameof(code));
                var type = shape[member + 2]; var inputPointer = types.FirstOrDefault(p => (p.Value[0] & 65535) == 32 && p.Value.Length == 4 && p.Value[2] == 1 && p.Value[3] == type).Key;
                if (inputPointer == 0) { inputPointer = next++; definitions.Add([(4u << 16) | 32, inputPointer, 1, type]); }
                var variable = values[member] = next++; definitions.Add([(4u << 16) | 59, inputPointer, variable, 1]);
                foreach (var decoration in decorations)
                { var lifted = new uint[decoration.Length - 1]; lifted[0] = (uint)(lifted.Length << 16) | 71; lifted[1] = variable; decoration.AsSpan(3).CopyTo(lifted.AsSpan(2)); annotations.Add(lifted); }
            }
            fields.Add(item[2], values); declarations.Add(item[2], definitions);
        }
        if (fields.Count == 0) throw new ArgumentException("No scalar view input block exists.", nameof(code));
        var aliases = new Dictionary<uint, uint>();
        uint Resolve(uint id) { for (var depth = 0; aliases.TryGetValue(id, out var source); depth++) { if (depth >= 128) throw new ArgumentException("View-block aliases exceed the module depth."); id = source; } return id; }
        var removed = new HashSet<uint[]>();
        foreach (var item in instructions)
        {
            var op = item[0] & 65535;
            if (op == 83 && item.Length == 4 && fields.ContainsKey(Resolve(item[3]))) { aliases.Add(item[2], Resolve(item[3])); removed.Add(item); }
            if (op is not (65 or 66) || item.Length < 5 || !fields.TryGetValue(Resolve(item[3]), out var values)) continue;
            if (!constants.TryGetValue(item[4], out var member) || member >= values.Length) throw new ArgumentException("A view-block access requires a constant valid member.", nameof(code));
            if (item.Length == 5) { aliases.Add(item[2], values[member]); removed.Add(item); }
            else { item[3] = values[member]; Array.Copy(item, 5, item, 4, item.Length - 5); item[^1] = 0; item[0] = (uint)((item.Length - 1) << 16) | op; }
        }
        var output = new List<uint>(words.Length); output.AddRange(words[..5].ToArray()); var annotated = false;
        foreach (var item in instructions)
        {
            var op = item[0] & 65535; if (removed.Contains(item)) continue;
            if (op is 5 or 71 && (fields.ContainsKey(item[1]) || aliases.ContainsKey(item[1])) || op is 6 or 72 && selected.Contains(item[1]) || op == 71 && selected.Contains(item[1]) && item[2] == 2) continue;
            if (!annotated && op is >= 19 and <= 33) { foreach (var annotation in annotations) output.AddRange(annotation); annotated = true; }
            if (op == 59 && declarations.TryGetValue(item[2], out var definitions)) { foreach (var declaration in definitions) output.AddRange(declaration); continue; }
            if (op == 15)
            {
                var end = 3; while (end < item.Length && (item[end++] & 0xff000000) != 0) { }
                var entry = new List<uint>(item.Take(end)); foreach (var id in item.Skip(end)) { if (fields.TryGetValue(id, out var values)) entry.AddRange(values); else entry.Add(id); }
                entry[0] = (uint)(entry.Count << 16) | 15; output.AddRange(entry); continue;
            }
            if (op == 61 && item.Length >= 4)
            {
                var pointer = Resolve(item[3]);
                if (fields.TryGetValue(pointer, out var values))
                {
                    if (item.Length != 4) throw new NotSupportedException("A whole view-block load requires ordinary input access.");
                    var shape = types[item[1]]; var construct = new uint[3 + values.Length]; construct[0] = (uint)(construct.Length << 16) | 80; construct[1] = item[1]; construct[2] = item[2];
                    for (var i = 0; i < values.Length; i++) { var value = next++; output.AddRange([(4u << 16) | 61, shape[i + 2], value, values[i]]); construct[i + 3] = value; }
                    output.AddRange(construct); continue;
                }
                item[3] = pointer;
            }
            if (op is 65 or 66 or 83 && item.Length >= 4) item[3] = Resolve(item[3]);
            if (op is 67 or 70 && item.Length >= 4 && fields.ContainsKey(Resolve(item[3]))) throw new NotSupportedException("Input block pointer arithmetic requires its shader adapter.");
            if (op == 57 && item.Skip(4).Any(id => fields.ContainsKey(Resolve(id))) || op == 245 && item.Skip(3).Where((_, i) => i % 2 == 0).Any(id => fields.ContainsKey(Resolve(id)))) throw new NotSupportedException("Input block pointers cannot escape interface loads.");
            output.AddRange(item.Take((int)(item[0] >> 16)));
        }
        if (next > 1_048_576) throw new ArgumentException("View-block expansion exceeds the module identity bound.", nameof(code)); output[3] = next;
        return MemoryMarshal.AsBytes(output.ToArray().AsSpan()).ToArray();
    }
}
