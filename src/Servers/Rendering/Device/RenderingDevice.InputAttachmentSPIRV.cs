using System.Runtime.InteropServices;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private readonly record struct InputSlot(int Index, bool Multisample);
    private static (byte[] Code, Dictionary<uint, InputSlot> Inputs) LowerInputAttachments(byte[] code, ShaderStage stage)
    {
        var words = MemoryMarshal.Cast<byte, uint>(code.AsSpan());
        if (words[0] != 0x07230203 || words[3] is 0 or > 1_048_576 || words[4] != 0) throw new ArgumentException("Invalid SPIR-V header.", nameof(code));
        var instructions = new List<uint[]>();
        for (var i = 5; i < words.Length;)
        {
            var count = (int)(words[i] >> 16); if (count == 0 || count > words.Length - i) throw new ArgumentException("Truncated SPIR-V instruction.", nameof(code));
            instructions.Add(words.Slice(i, count).ToArray()); i += count;
        }
        static uint Op(uint[] instruction) => instruction[0] & 65535;
        var images = instructions.Where(i => Op(i) == 25 && i.Length >= 9 && i[3] == 6).ToDictionary(i => i[1]);
        if (images.Count == 0) return (code, []);
        if (stage != ShaderStage.Fragment) throw new NotSupportedException("Input attachments require a fragment stage.");
        var types = instructions.Where(i => Op(i) is >= 19 and <= 33).ToDictionary(i => i[1]);
        var inputs = new Dictionary<uint, InputSlot>();
        foreach (var variable in instructions.Where(i => Op(i) == 59 && i.Length >= 4 && i[3] == 0))
        {
            if (!types.TryGetValue(variable[1], out var pointer) || Op(pointer) != 32 || !images.TryGetValue(pointer[3], out var image)) continue;
            var index = instructions.Where(i => Op(i) == 71 && i.Length == 4 && i[1] == variable[2] && i[2] == 43).ToArray();
            if (index.Length != 1 || index[0][3] > int.MaxValue || image[4] != 0 || image[5] != 0 || image[6] > 1) throw new ArgumentException("Invalid input attachment index or image role.", nameof(code));
            inputs.Add(variable[2], new((int)index[0][3], image[6] != 0));
        }
        var valueTypes = new Dictionary<uint, uint>();
        foreach (var instruction in instructions) if (instruction.Length >= 3 && images.ContainsKey(instruction[1])) valueTypes[instruction[2]] = instruction[1];
        var declarations = new List<uint[]>(); var decorations = new List<uint[]>(); var next = words[3];
        uint Fresh() => next < 1_048_576 ? next++ : throw new ArgumentException("Translated shader identities exceed the module limit.");
        static uint[] Instruction(uint op, params uint[] operands) => [(uint)(operands.Length + 1) << 16 | op, .. operands];
        uint Type(uint op, params uint[] operands)
        {
            var found = instructions.Concat(declarations).FirstOrDefault(i => Op(i) == op && i.AsSpan(2).SequenceEqual(operands));
            if (found is not null) return found[1]; var id = Fresh(); declarations.Add(Instruction(op, [id, .. operands])); return id;
        }
        var scalar = Type(22, 32); var float2 = Type(23, scalar, 2); var float4 = Type(23, scalar, 4); var integer = Type(21, 32, 1); var int2 = Type(23, integer, 2);
        var position = instructions.FirstOrDefault(i => Op(i) == 71 && i.Length == 4 && i[2] == 11 && i[3] == 15)?[1] ?? 0;
        if (position == 0)
        {
            var pointer = Type(32, 1, float4); position = Fresh(); declarations.Add(Instruction(59, pointer, position, 1)); decorations.Add(Instruction(71, position, 11, 15));
        }
        else
        {
            var variable = instructions.SingleOrDefault(i => Op(i) == 59 && i.Length >= 4 && i[2] == position);
            if (variable is null || !types.TryGetValue(variable[1], out var pointer) || Op(pointer) != 32 || pointer[2] != 1 || pointer[3] != float4) throw new ArgumentException("Fragment position must be a float4 input.", nameof(code));
        }
        var zero = Fresh(); declarations.Add(Instruction(43, integer, zero, 0));
        var body = new List<uint[]>();
        foreach (var instruction in instructions)
        {
            var op = Op(instruction);
            if (op == 17 && instruction.Length == 2 && instruction[1] == 40) continue;
            if (op == 71 && instruction.Length == 4 && instruction[2] == 43 && inputs.ContainsKey(instruction[1])) continue;
            if (op == 15 && instruction[1] == 4)
            {
                var end = MaterialLightingProgram.StringEnd(instruction, 3);
                body.Add(instruction.AsSpan(end).Contains(position) ? instruction : Instruction(15, [.. instruction.AsSpan(1).ToArray(), position])); continue;
            }
            if (op == 25 && images.ContainsKey(instruction[1]))
            { var image = (uint[])instruction.Clone(); image[3] = 1; image[7] = 1; image[8] = 0; body.Add(image); continue; }
            if (op == 98 && instruction.Length >= 5 && valueTypes.TryGetValue(instruction[3], out var imageType))
            {
                var multi = images[imageType][6] != 0;
                if (multi ? (instruction.Length < 7 || (instruction[5] & 64) == 0) : (instruction.Length != 5 && !(instruction.Length == 6 && instruction[5] == 0))) throw new NotSupportedException("Input reads require the ordinary subpass load/sample operands.");
                var coordinate = Fresh(); var xy = Fresh(); var pixel = Fresh();
                body.Add(Instruction(61, float4, coordinate, position)); body.Add(Instruction(79, float2, xy, coordinate, coordinate, 0, 1)); body.Add(Instruction(110, int2, pixel, xy));
                body.Add(Instruction(95, [instruction[1], instruction[2], instruction[3], pixel, .. (multi ? instruction.AsSpan(5).ToArray() : new uint[] { 2, zero })])); continue;
            }
            body.Add(instruction);
        }
        var annotation = body.FindIndex(i => Op(i) is >= 19 and <= 59); if (annotation < 0) throw new ArgumentException("The module has no declarations.", nameof(code)); body.InsertRange(annotation, decorations);
        var function = body.FindIndex(i => Op(i) == 54); if (function < 0) throw new ArgumentException("The module has no function.", nameof(code)); body.InsertRange(function, declarations);
        var result = new List<uint>(words.Length + declarations.Sum(i => i.Length) + 128); result.AddRange(words[..5].ToArray()); result[3] = next; foreach (var instruction in body) result.AddRange(instruction);
        return (MemoryMarshal.AsBytes(result.ToArray().AsSpan()).ToArray(), inputs);
    }
}
