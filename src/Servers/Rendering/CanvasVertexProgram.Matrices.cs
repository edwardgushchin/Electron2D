using static Electron2D.MaterialLightingProgram;

namespace Electron2D;

internal static partial class CanvasVertexProgram
{
    private static byte[] ComposeModel(byte[] code, ShaderUniform model, int textures)
    {
        var module = new Module(code); var instructions = module.Instructions;
        var types = instructions.Where(i => Op(i) is >= 19 and <= 33).ToDictionary(i => i[1]);
        var constants = instructions.Where(i => Op(i) == 43).ToDictionary(i => i[2], i => i[3]);
        var variables = instructions.Where(i => Op(i) == 59).ToArray();
        bool Decorated(uint id, uint decoration, uint value) => instructions.Any(i => Op(i) == 71 && i[1] == id && i[2] == decoration && i[3] == value);
        var block = variables.Single(v => v[3] == 2 && Decorated(v[2], 34, 1) && Decorated(v[2], 33, (uint)model.Buffer));
        var structure = types[block[1]][3];
        var member = instructions.Single(i => Op(i) == 72 && i[1] == structure && i[3] == 35 && i[4] == model.Offset)[2];
        var matrix = types[structure][member + 2]; var vector = types[matrix][2]; var scalar = types[vector][2];
        uint next = module.Header[3]; uint ID() => checked(next++);
        var added = new List<uint[]>(); var decorations = new List<uint[]>();
        var uintType = types.Values.FirstOrDefault(i => Op(i) == 21 && i[2] == 32 && i[3] == 0)?[1] ?? 0;
        if (uintType == 0) { uintType = ID(); added.Add(Instruction(21, uintType, 32, 0)); }
        uint Pointer(uint storage, uint type)
        {
            var existing = instructions.Concat(added).FirstOrDefault(i => Op(i) == 32 && i[2] == storage && i[3] == type);
            if (existing is not null) return existing[1]; var id = ID(); added.Add(Instruction(32, id, storage, type)); return id;
        }
        uint Constant(uint value)
        { var id = ID(); added.Add(Instruction(43, uintType, id, value)); return id; }
        var zero = Constant(0); var one = Constant(1); var two = Constant(2);
        var privateMatrix = ID(); added.Add(Instruction(59, Pointer(6, matrix), privateMatrix, 6));
        var privatePointers = new HashSet<uint>(); var aliases = new HashSet<uint> { block[2] };
        foreach (var instruction in instructions)
        {
            var opcode = Op(instruction);
            if (opcode == 83 && aliases.Contains(instruction[3])) aliases.Add(instruction[2]);
            if (opcode is 65 or 66 && aliases.Contains(instruction[3]) && constants.GetValueOrDefault(instruction[4], uint.MaxValue) == member)
            {
                var element = types[instruction[1]][3]; var pointer = Pointer(6, element);
                var rewritten = instruction.Length == 5 ? Instruction(83, pointer, instruction[2], privateMatrix) : Instruction(opcode, [pointer, instruction[2], privateMatrix, .. instruction.Skip(5)]);
                Array.Copy(rewritten, instruction, Math.Min(instruction.Length, rewritten.Length));
                // The replacement may be shorter than the original access chain.
                if (rewritten.Length != instruction.Length) { instruction[0] = rewritten[0]; }
                privatePointers.Add(instruction[2]);
            }
            else if (opcode is 65 or 66 or 67 or 70 or 83 && privatePointers.Contains(instruction[3]))
            { instruction[1] = Pointer(6, types[instruction[1]][3]); privatePointers.Add(instruction[2]); }
            if (opcode is 67 or 70 && aliases.Contains(instruction[3]) || opcode == 61 && aliases.Contains(instruction[3]) || opcode == 57 && instruction.Skip(4).Any(id => privatePointers.Contains(id) || aliases.Contains(id)))
                throw new NotSupportedException("Instanced model matrices require direct uniform member access; block values and uniform pointers cannot escape into functions.");
        }
        // Trim changed instruction arrays to their encoded word count.
        for (var i = 0; i < instructions.Count; i++) if (instructions[i].Length != instructions[i][0] >> 16) instructions[i] = instructions[i][..(int)(instructions[i][0] >> 16)];
        var array = ID(); added.Add(Instruction(29, array, vector)); decorations.Add(Instruction(71, array, 6, 16));
        var storageClass = module.Header[1] >= 0x10300 ? 12u : 2u;
        var storage = ID(); added.Add(Instruction(30, storage, array)); decorations.Add(Instruction(71, storage, storageClass == 12 ? 2u : 3u)); decorations.Add(Instruction(72, storage, 0, 35, 0)); decorations.Add(Instruction(72, storage, 0, 24));
        var poses = ID(); added.Add(Instruction(59, Pointer(storageClass, storage), poses, storageClass)); decorations.Add(Instruction(71, poses, 34, 0)); decorations.Add(Instruction(71, poses, 33, (uint)textures));
        var instance = variables.SingleOrDefault(v => v[3] == 1 && Decorated(v[2], 11, 43));
        uint instanceID, instanceType;
        if (instance is null) { instanceID = ID(); instanceType = uintType; added.Add(Instruction(59, Pointer(1, uintType), instanceID, 1)); decorations.Add(Instruction(71, instanceID, 11, 43)); }
        else { instanceID = instance[2]; instanceType = types[instance[1]][3]; }
        var entry = instructions.Single(i => Op(i) == 15); var main = entry[2]; var wrapper = ID();
        var function = instructions.Single(i => Op(i) == 54 && i[2] == main);
        var frame = variables.Single(v => v[3] == 2 && Decorated(v[2], 34, 1) && Decorated(v[2], 33, 0));
        var body = new List<uint[]>();
        uint Value(uint opcode, uint type, params uint[] operands) { var id = ID(); body.Add(Instruction(opcode, [type, id, .. operands])); return id; }
        uint Load(uint type, uint variable) => Value(61, type, variable);
        var raw = Load(instanceType, instanceID); if (instanceType != uintType) raw = Value(124, uintType, raw);
        // The canvas wrapper appends the table offset at member four.
        var offset = Load(uintType, Value(65, Pointer(2, uintType), frame[2], Constant(4)));
        var index = Value(132, uintType, Value(128, uintType, raw, offset), two);
        var basis = Load(vector, Value(65, Pointer(storageClass, vector), poses, zero, index));
        var origin = Load(vector, Value(65, Pointer(storageClass, vector), poses, zero, Value(128, uintType, index, one)));
        uint Float(uint bits) { var id = ID(); added.Add(Instruction(43, scalar, id, bits)); return id; }
        var f0 = Float(0); var f1 = Float(0x3f800000);
        var x = Value(80, vector, Value(81, scalar, basis, 0), Value(81, scalar, basis, 1), f0, f0);
        var y = Value(80, vector, Value(81, scalar, basis, 2), Value(81, scalar, basis, 3), f0, f0);
        var z = Value(80, vector, f0, f0, f1, f0);
        var translation = Value(80, vector, Value(81, scalar, origin, 0), Value(81, scalar, origin, 1), f0, f1);
        var pose = Value(80, matrix, x, y, z, translation);
        var baseMatrix = Load(matrix, Value(65, Pointer(2, matrix), block[2], Constant(member)));
        body.Add(Instruction(62, privateMatrix, Value(146, matrix, baseMatrix, pose)));
        _ = Value(57, function[1], main); body.Add(Instruction(253)); body.Add(Instruction(56));
        entry[2] = wrapper;
        var interfaceStart = StringEnd(entry, 3);
        var interfaces = entry.Skip(interfaceStart).Concat(instance is null ? new[] { instanceID } : []).ToList();
        if (module.Header[1] >= 0x10400) interfaces.AddRange([poses, privateMatrix]);
        instructions[instructions.IndexOf(entry)] = Instruction(15, [entry[1], wrapper, .. entry.Skip(3).Take(interfaceStart - 3), .. interfaces]);
        foreach (var instruction in instructions) if (Op(instruction) is 16 or 331 && instruction[1] == main) instruction[1] = wrapper;
        var firstFunction = instructions.FindIndex(i => Op(i) == 54);
        var prefix = instructions.Take(firstFunction).Concat(decorations).Concat(added).OrderBy(i => Category(Op(i))).ToList();
        var functions = instructions.Skip(firstFunction).ToArray();
        module.Instructions.Clear(); module.Instructions.AddRange(prefix); module.Instructions.AddRange(functions);
        module.Instructions.Add(Instruction(54, function[1], wrapper, 0, function[4])); module.Instructions.Add(Instruction(248, ID()));
        module.Instructions.AddRange(body); module.Header[3] = next; return module.Write();
    }
}
