using System.Runtime.InteropServices;
using static Electron2D.MaterialLightingProgram;

namespace Electron2D;

internal static class CanvasVertexProgram
{
    internal static byte[] Compose(byte[] code, bool instanced, bool lighting)
    {
        var module = new Module(code); var instructions = module.Instructions;
        var declarations = instructions.Where(i => Op(i) is >= 19 and <= 33).ToDictionary(i => i[1]);
        var variables = instructions.Where(i => Op(i) == 59).ToDictionary(i => i[2]);
        uint next = module.Header[3]; uint ID() => checked(next++);
        uint Pointee(uint variable) => declarations[variables[variable][1]][3];
        uint Interface(uint storage, uint location) => variables.Values.FirstOrDefault(i => i[3] == storage && instructions.Any(d => Op(d) == 71 && d[1] == i[2] && d[2] == 30 && d[3] == location))?[2] ?? 0;
        var position = Interface(1, 0); var color = Interface(1, 1); var custom = Interface(1, 3); var outputColor = Interface(3, 0);
        var vertexID = variables.Values.SingleOrDefault(v => v[3] == 1 && instructions.Any(i => Op(i) == 71 && i[1] == v[2] && i[2] == 11 && i[3] == 42))?[2] ?? 0;
        var float2 = Pointee(position); var float4 = Pointee(color); var scalar = declarations[float2][2];
        var basic = instructions.Where(i => Op(i) is >= 19 and <= 23).ToList();
        var uintType = basic.FirstOrDefault(i => Op(i) == 21 && i[2] == 32 && i[3] == 0)?[1] ?? 0;
        if (uintType == 0) { uintType = ID(); basic.Add(Instruction(21, uintType, 32, 0)); }
        var basicIds = basic.Select(i => i[1]).ToHashSet();
        var pointers = instructions.Where(i => Op(i) == 32 && basicIds.Contains(i[3])).ToList();
        uint Pointer(uint storage, uint type)
        {
            var existing = pointers.FirstOrDefault(i => i[2] == storage && i[3] == type);
            if (existing is not null) return existing[1];
            var id = ID(); pointers.Add(Instruction(32, id, storage, type)); return id;
        }
        var added = new List<uint[]>(); var annotations = new List<uint[]>();
        var constants = new Dictionary<uint, uint>();
        uint Constant(uint value)
        { if (constants.TryGetValue(value, out var existing)) return existing; var id = ID(); added.Add(Instruction(43, uintType, id, value)); constants.Add(value, id); return id; }
        uint Variable(uint storage, uint type, uint location, bool flat = false)
        {
            var id = ID(); added.Add(Instruction(59, Pointer(storage, type), id, storage));
            annotations.Add(Instruction(71, id, 30, location)); if (flat) annotations.Add(Instruction(71, id, 14)); return id;
        }
        var frame = variables.Values.Single(v => v[3] == 2 && instructions.Any(i => Op(i) == 71 && i[1] == v[2] && i[2] == 34 && i[3] == 1) && instructions.Any(i => Op(i) == 71 && i[1] == v[2] && i[2] == 33 && i[3] == 0));
        var frameType = declarations[declarations[frame[1]][3]]; var frameId = frameType[1]; var frameIndex = instructions.IndexOf(frameType);
        instructions[frameIndex] = Instruction(30, frameId, float2, uintType, float4);
        annotations.Add(Instruction(72, frameId, 1, 35, 8)); annotations.Add(Instruction(72, frameId, 2, 35, 16));
        var entry = instructions.Single(i => Op(i) == 15); var originalMain = entry[2]; var wrapper = ID();
        var privateVariables = new HashSet<uint>(); var newInputs = new List<uint>(); var newOutputs = new List<uint>();
        void Privatize(uint id)
        {
            if (id == 0) return;
            var variable = variables[id]; var replacement = Instruction(59, Pointer(6, Pointee(id)), id, 6);
            instructions[instructions.IndexOf(variable)] = replacement; privateVariables.Add(id);
            instructions.RemoveAll(i => Op(i) == 71 && i[1] == id && i[2] is 11 or 14 or 30 or 31 or 32);
        }
        uint rawVertexID = 0, meshVertexID = 0, vertexIDType = 0, boolType = 0;
        if (vertexID != 0)
        {
            vertexIDType = Pointee(vertexID);
            if (declarations[vertexIDType] is not [_, _, 32, _] || Op(declarations[vertexIDType]) != 21)
                throw new NotSupportedException("Canvas vertex identity requires a 32-bit integer builtin.");
            boolType = basic.FirstOrDefault(i => Op(i) == 20)?[1] ?? 0;
            if (boolType == 0) { boolType = ID(); basic.Add(Instruction(20, boolType)); }
            Privatize(vertexID);
            rawVertexID = ID(); added.Add(Instruction(59, Pointer(1, vertexIDType), rawVertexID, 1));
            annotations.Add(Instruction(71, rawVertexID, 11, 42));
            meshVertexID = Variable(1, uintType, 15); newInputs.AddRange([rawVertexID, meshVertexID]);
        }
        uint rawPosition = 0, rawColor = 0, basis = 0, translation = 0, tint = 0, rawCustom = 0;
        if (instanced)
        {
            Privatize(position); Privatize(color); Privatize(custom);
            rawPosition = Variable(1, float2, 0); rawColor = Variable(1, float4, 1);
            basis = Variable(1, float4, 3); translation = Variable(1, float4, 4); tint = Variable(1, float4, 5); rawCustom = Variable(1, float4, 6);
            newInputs.AddRange([rawPosition, rawColor, basis, translation, tint, rawCustom]);
        }
        var glyphInput = Variable(1, float4, 14); var glyphOutput = Variable(3, float4, 11, flat: true);
        newInputs.Add(glyphInput); newOutputs.Add(glyphOutput);
        var lightOutput = lighting ? Variable(3, uintType, 3, flat: true) : 0; if (lighting) newOutputs.Add(lightOutput);
        // Input pointers become private along the complete access-chain, never by guessing literal operands.
        foreach (var instruction in instructions)
        {
            var opcode = Op(instruction);
            if ((opcode is 65 or 66 or 67 or 70 or 83) && privateVariables.Contains(instruction[3]))
            { instruction[1] = Pointer(6, declarations[instruction[1]][3]); privateVariables.Add(instruction[2]); }
            if (opcode == 57 && instruction.Skip(4).Any(privateVariables.Contains)) throw new NotSupportedException("Canvas vertex input pointers cannot escape into function parameters.");
            if (opcode is 16 or 331 && instruction[1] == originalMain) instruction[1] = wrapper;
        }
        var body = new List<uint[]>();
        uint Value(uint opcode, uint type, params uint[] operands)
        { var id = ID(); body.Add(Instruction(opcode, [type, id, .. operands])); return id; }
        uint Load(uint type, uint variable) => Value(61, type, variable);
        if (vertexID != 0)
        {
            var raw = Load(vertexIDType, rawVertexID); if (vertexIDType != uintType) raw = Value(124, uintType, raw);
            var mesh = Load(uintType, meshVertexID);
            var selected = Value(169, uintType, Value(171, boolType, mesh, Constant(uint.MaxValue)), mesh, raw);
            body.Add(Instruction(62, vertexID, vertexIDType == uintType ? selected : Value(124, vertexIDType, selected)));
        }
        if (instanced)
        {
            var p = Load(float2, rawPosition); var b = Load(float4, basis); var t = Load(float4, translation);
            var x = Value(81, scalar, p, 0); var y = Value(81, scalar, p, 1);
            var bx = Value(79, float2, b, b, 0, 1); var by = Value(79, float2, b, b, 2, 3); var origin = Value(79, float2, t, t, 0, 1);
            var px = Value(142, float2, bx, x); var py = Value(142, float2, by, y);
            var local = Value(129, float2, px, py); var transformed = Value(129, float2, local, origin);
            foreach (var id in new[] { px, py, local, transformed }) annotations.Add(Instruction(71, id, 42));
            body.Add(Instruction(62, position, transformed));
            var incoming = Load(float4, rawColor); var multiplier = Load(float4, tint);
            body.Add(Instruction(62, color, Value(133, float4, incoming, multiplier)));
            if (custom != 0) body.Add(Instruction(62, custom, Load(float4, rawCustom)));
        }
        var function = instructions.Single(i => Op(i) == 54 && i[2] == originalMain);
        var voidType = function[1]; var functionType = function[4];
        _ = Value(57, voidType, originalMain);
        var modulationPointer = Value(65, Pointer(2, float4), frame[2], Constant(2));
        var modulation = Load(float4, modulationPointer); var resultColor = Load(float4, outputColor);
        body.Add(Instruction(62, outputColor, Value(133, float4, resultColor, modulation)));
        body.Add(Instruction(62, glyphOutput, Load(float4, glyphInput)));
        if (lighting)
        {
            var indexPointer = Value(65, Pointer(2, uintType), frame[2], Constant(1));
            body.Add(Instruction(62, lightOutput, Load(uintType, indexPointer)));
        }
        body.Add(Instruction(253)); body.Add(Instruction(56));
        var interfaceStart = StringEnd(entry, 3);
        var oldInterfaces = entry.Skip(interfaceStart).Where(id => module.Header[1] >= 0x10400 || !privateVariables.Contains(id));
        var interfaces = oldInterfaces.Concat(newInputs).Concat(newOutputs).Distinct().ToArray();
        var replacementEntry = new uint[interfaceStart + interfaces.Length]; entry.AsSpan(0, interfaceStart).CopyTo(replacementEntry);
        replacementEntry[0] = (uint)(replacementEntry.Length << 16) | 15; replacementEntry[2] = wrapper; interfaces.CopyTo(replacementEntry, interfaceStart);
        instructions[instructions.IndexOf(entry)] = replacementEntry;
        var firstFunction = instructions.FindIndex(i => Op(i) == 54);
        var prefix = instructions.Take(firstFunction).Where(i => !basic.Contains(i) && !pointers.Contains(i)).ToList();
        var declarationStart = prefix.FindIndex(i => Category(Op(i)) == 10);
        prefix.InsertRange(declarationStart, [.. basic, .. pointers]); prefix.AddRange(added);
        prefix.AddRange(annotations); prefix = prefix.OrderBy(i => Category(Op(i))).ToList();
        var all = prefix.Concat(instructions.Skip(firstFunction)).Concat(new[] { Instruction(54, voidType, wrapper, 0, functionType), Instruction(248, ID()) }).Concat(body).ToArray();
        module.Header[3] = next;
        var words = new uint[5 + all.Sum(i => i.Length)]; module.Header.CopyTo(words, 0); var offset = 5;
        foreach (var instruction in all) { instruction.CopyTo(words, offset); offset += instruction.Length; }
        return MemoryMarshal.AsBytes(words.AsSpan()).ToArray();
    }
}
