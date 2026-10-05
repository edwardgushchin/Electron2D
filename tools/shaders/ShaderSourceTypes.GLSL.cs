using System.Runtime.InteropServices;
using System.Text;
using Electron2D;

internal static unsafe partial class ShaderSourceTypes
{
    internal static Dictionary<(int Buffer, string Name), (int BooleanWidth, int ArrayLength)> GLSL(byte[] debugCode, bool fragment)
    {
        var words = MemoryMarshal.Cast<byte, uint>(debugCode);
        var strings = new Dictionary<uint, string>();
        var constants = new Dictionary<uint, uint>();
        var instructions = new Dictionary<uint, uint[]>();
        var variables = new HashSet<uint>();
        var sets = new Dictionary<uint, uint>(); var bindings = new Dictionary<uint, uint>();
        uint debugSet = 0;
        for (var at = 5; at < words.Length;)
        {
            var count = (int)(words[at] >> 16); var op = words[at] & 0xffff;
            if (count == 0 || count > words.Length - at) throw new ArgumentException("Malformed compiler reflection module.");
            var instruction = words.Slice(at, count);
            if (op is 7 or 11)
            {
                var bytes = MemoryMarshal.AsBytes(instruction[2..]); var end = bytes.IndexOf((byte)0);
                if (end < 0) throw new ArgumentException("Malformed compiler reflection string.");
                var value = Encoding.UTF8.GetString(bytes[..end]);
                if (op == 7) strings.Add(instruction[1], value);
                else if (value is "NonSemantic.Shader.DebugInfo.100" or "NonSemantic.Shader.DebugInfo.101" or "NonSemantic.Shader.DebugInfo.102") debugSet = instruction[1];
            }
            if (op == 43 && count == 4) constants[instruction[2]] = instruction[3];
            if (op == 59 && instruction[3] == 2) variables.Add(instruction[2]);
            if (op == 71 && count == 4)
            {
                if (instruction[2] == 33) bindings[instruction[1]] = instruction[3];
                if (instruction[2] == 34) sets[instruction[1]] = instruction[3];
            }
            if (op == 12 && debugSet != 0 && instruction[3] == debugSet) instructions.Add(instruction[2], instruction.ToArray());
            at += count;
        }
        if (debugSet == 0) throw new ArgumentException("GLSL compilation did not retain source type information.");
        (int Width, int Length) Shape(uint id, int depth = 0)
        {
            if (depth > 8) throw new NotSupportedException("Nested source type information exceeds the material interface.");
            var type = instructions[id];
            if (type[4] == 2) return (constants[type[7]] == 2 ? 1 : 0, 0); // Boolean encoding.
            if (type[4] is 3 or 4) return Shape(type[5], depth + 1);
            if (type[4] == 7) return Shape(type[6], depth + 1);
            if (type[4] is 5 or 6)
            {
                var element = Shape(type[5], depth + 1);
                if (element.Width == 0) return default;
                if (type.Length != 7 || element.Length != 0) throw new NotSupportedException("Boolean arrays require one fixed dimension.");
                var count = checked((int)constants[type[6]]);
                return type[4] == 5 ? (element.Width, count) : (count, 0);
            }
            return default;
        }
        var result = new Dictionary<(int, string), (int, int)>();
        foreach (var global in instructions.Values)
        {
            if (global[4] != 18 || !variables.Contains(global[12]) ||
                !sets.TryGetValue(global[12], out var set) || set != (fragment ? 3 : 1) || !bindings.TryGetValue(global[12], out var binding)) continue;
            var composite = instructions[global[6]];
            if (composite[4] != 10) throw new NotSupportedException("GLSL uniform buffers require reflected structure types.");
            foreach (var memberID in composite.AsSpan(14))
            {
                var member = instructions[memberID];
                if (member[4] != 11 || !result.TryAdd((checked((int)binding), strings[member[5]]), Shape(member[6])))
                    throw new ArgumentException("Ambiguous source uniform type information.");
            }
        }
        return result;
    }

    internal static byte[] Annotate(byte[] code, bool fragment, IReadOnlyDictionary<(int Buffer, string Name), (int BooleanWidth, int ArrayLength)> types)
    {
        var program = ShaderCompiler.ValidateInterface(code, fragment, types);
        var booleans = program.Uniforms.Values.Where(u => u.BooleanWidth != 0).OrderBy(u => u.Name, StringComparer.Ordinal).ToArray();
        if (booleans.Length == 0) return code;
        var words = MemoryMarshal.Cast<byte, uint>(code).ToArray();
        var at = 5;
        while (at < words.Length && (words[at] & 0xffff) is 10 or 11 or 14 or 15 or 16 or 17 or 331) at += (int)(words[at] >> 16);
        using var output = new MemoryStream();
        using var writer = new BinaryWriter(output, Encoding.UTF8, leaveOpen: true);
        var nextID = words[3]; words[3] = checked(nextID + (uint)booleans.Length);
        writer.Write(MemoryMarshal.AsBytes(words.AsSpan(0, at)));
        foreach (var uniform in booleans)
        {
            var name = Convert.ToBase64String(Encoding.UTF8.GetBytes(uniform.Name));
            var bytes = Encoding.UTF8.GetBytes(FormattableString.Invariant($"Electron2D:bool:1:{uniform.BooleanWidth}:{uniform.ArrayLength}:{name}") + '\0');
            var padded = checked((bytes.Length + 3) / 4 * 4);
            var count = padded / 4 + 2;
            if (count > ushort.MaxValue) throw new ArgumentException("Shader type metadata exceeds the SPIR-V instruction limit.");
            writer.Write((uint)count << 16 | 7u); writer.Write(nextID++); writer.Write(bytes);
            for (var i = bytes.Length; i < padded; i++) writer.Write((byte)0);
        }
        writer.Write(MemoryMarshal.AsBytes(words.AsSpan(at)));
        return output.ToArray();
    }
}
