using static Electron2D.MaterialLightingProgram;

namespace Electron2D;

internal static class DeviceImageArrays
{
    internal readonly record struct Source(uint Set, int Binding, int Count, int Element, bool Dynamic);
    private sealed record Slot(uint Image, uint Variable, uint Selector);

    internal static (byte[] Code, Dictionary<(uint Set, int Binding), Source> Sources) Lower(byte[] code, bool symbolic)
    {
        var module = new Module(code); var source = module.Instructions;
        var types = source.Where(i => Op(i) is >= 19 and <= 39).ToDictionary(i => i[1]);
        bool Opaque(uint id)
        {
            for (var depth = 0; depth < 32; depth++)
            {
                if (!types.TryGetValue(id, out var i)) return false;
                var op = Op(i); if (op == 25) return i[3] != 5 && i[7] == 2;
                if (op is not (28 or 29 or 32)) return false; id = i[op == 32 ? 3 : 2];
            }
            throw new ArgumentException("The image descriptor type graph is cyclic.", nameof(code));
        }
        var resources = source.Where(i => Op(i) == 59 && i[3] == 0 && Opaque(i[1])).ToArray();
        if (!resources.Any(i => Op(types[types[i[1]][3]]) is 28 or 29)) return (code, []);
        uint Image(uint id) { var i = types[id]; return Op(i) == 25 ? id : Image(i[Op(i) == 32 ? 3 : 2]); }
        var lengths = source.Where(i => Op(i) == 43 && i.Length == 4).ToDictionary(i => i[2], i => i[3]);
        var valueTypes = source.Where(i => i.Length > 2 && (Op(i) == 12 || Op(i) >= 41) && Category(Op(i)) != 9 && types.ContainsKey(i[1])).ToDictionary(i => i[2], i => i[1]);
        var locations = source.Where(i => Op(i) == 71 && i.Length == 4 && i[2] is 33 or 34).GroupBy(i => i[1]).ToDictionary(g => g.Key, g => g.ToDictionary(i => i[2], i => i[3]));
        var usedSets = locations.Values.Where(d => d.ContainsKey(34)).Select(d => d[34]).ToHashSet(); uint privateSet = 0; while (usedSets.Contains(privateSet)) privateSet++;
        var declarations = new List<uint[]>(); var annotations = new List<uint[]>(); var functions = new List<uint[]>(); var helpers = new List<uint[]>(); var next = module.Header[3];
        uint Type(uint op, params uint[] operands)
        {
            var existing = types.Values.FirstOrDefault(i => Op(i) == op && i.AsSpan(2).SequenceEqual(operands)); if (existing is not null) return existing[1];
            var i = Instruction(op, [next++, .. operands]); types.Add(i[1], i); declarations.Add(i); return i[1];
        }
        var integer = Type(21, 32, 0); var boolean = Type(20); var nothing = Type(19);
        var integerDeclaration = types[integer]; source.Remove(integerDeclaration); declarations.Remove(integerDeclaration); source.Insert(source.FindIndex(i => Category(Op(i)) == 10), integerDeclaration);
        var constants = new Dictionary<uint, uint>(); var zeroes = new Dictionary<uint, uint>();
        uint Constant(uint value) { if (constants.TryGetValue(value, out var id)) return id; id = next++; constants.Add(value, id); declarations.Add(Instruction(43, integer, id, value)); return id; }
        uint Zero(uint type) { if (zeroes.TryGetValue(type, out var id)) return id; id = next++; zeroes.Add(type, id); declarations.Add(Instruction(46, type, id)); return id; }
        uint Emit(List<uint[]> body, uint op, uint type, params uint[] operands) { var id = next++; body.Add(Instruction(op, [type, id, .. operands])); return id; }
        var origins = new Dictionary<(uint, int), Source>(); var slots = new List<Slot>(); var ids = resources.Select(i => i[2]).ToHashSet();
        foreach (var resource in resources)
        {
            var shape = DescriptorShape(resource[1], types, lengths, symbolic);
            if (!locations.TryGetValue(resource[2], out var location) || !location.TryGetValue(34, out var set) || !location.TryGetValue(33, out var binding) || binding > int.MaxValue) throw new ArgumentException("The image descriptor has no valid logical location.", nameof(code));
            declarations.Add(Instruction(43, integer, resource[2], (uint)slots.Count));
            for (var element = 0; element < shape.Count; element++)
            {
                var selector = (uint)slots.Count; var variable = next++; var image = Image(resource[1]); slots.Add(new(image, variable, selector));
                declarations.Add(Instruction(59, Type(32, 0, image), variable, 0)); annotations.Add(Instruction(71, variable, 33, selector)); annotations.Add(Instruction(71, variable, 34, privateSet));
                origins.Add((privateSet, (int)selector), new(set, (int)binding, shape.Count, element, shape.Dynamic));
                foreach (var d in source.Where(i => Op(i) == 71 && i[1] == resource[2] && i[2] is not (33 or 34)))
                {
                    var copy = (uint[])d.Clone(); copy[1] = variable; if (copy[2] == 43) copy[3] = checked(copy[3] + (uint)element); annotations.Add(copy);
                }
            }
        }
        var aliases = new Dictionary<uint, uint>(); var signatures = new Dictionary<string, uint>();
        foreach (var i in source.Where(i => Op(i) == 33))
        {
            for (var operand = 2; operand < i.Length; operand++) if (Opaque(i[operand])) i[operand] = integer;
            var key = string.Join(',', i.AsSpan(2).ToArray()); if (signatures.TryGetValue(key, out var other)) aliases.Add(i[1], other); else signatures.Add(key, i[1]);
        }
        var operations = new Dictionary<string, uint>(); var globals = new List<uint[]>(); var inFunction = false;
        var atomicPointers = new Dictionary<uint, (uint Image, uint Selector, uint Coordinate, uint Sample, uint PointerType)>();
        uint Atomic(uint[] operation, (uint Image, uint Selector, uint Coordinate, uint Sample, uint PointerType) pointer)
        {
            var opcode = Op(operation); var store = opcode == 228; var resultType = store ? nothing : operation[1]; var operand = store ? 1 : 3;
            var memoryCount = opcode is 230 or 231 ? 3 : 2; var memory = operation.AsSpan(operand + 1, memoryCount).ToArray(); var values = operation.AsSpan(operand + 1 + memoryCount).ToArray();
            var parameterTypes = new[] { integer, valueTypes[pointer.Coordinate] }.Concat(values.Select(v => valueTypes[v])).ToArray();
            var key = "atomic:" + opcode + ":" + pointer.Image + ":" + resultType + ":" + pointer.PointerType + ":" + pointer.Sample + ":" + string.Join(',', memory) + ":" + string.Join(',', parameterTypes);
            if (!operations.TryGetValue(key, out var function))
            {
                function = next++; operations.Add(key, function); var helper = new List<uint[]> { Instruction(54, resultType, function, 0, Type(33, [resultType, .. parameterTypes])) };
                var parameters = parameterTypes.Select(type => { var id = next++; helper.Add(Instruction(55, type, id)); return id; }).ToArray(); helper.Add(Instruction(248, next++));
                var choices = slots.Where(s => s.Image == pointer.Image).ToArray(); var labels = choices.Select(_ => next++).ToArray(); var merge = next++; var fallback = next++; var incoming = new List<uint>();
                helper.Add(Instruction(247, merge, 0)); helper.Add(Instruction(251, [parameters[0], fallback, .. choices.SelectMany((s, n) => new uint[] { s.Selector, labels[n] }).ToArray()]));
                for (var n = 0; n < choices.Length; n++)
                {
                    helper.Add(Instruction(248, labels[n])); var address = Emit(helper, 60, pointer.PointerType, choices[n].Variable, parameters[1], pointer.Sample);
                    var operands = new uint[] { address }.Concat(memory).Concat(parameters.Skip(2)).ToArray();
                    if (store) helper.Add(Instruction(opcode, operands)); else { incoming.Add(Emit(helper, opcode, resultType, operands)); incoming.Add(labels[n]); }
                    helper.Add(Instruction(249, merge));
                }
                helper.Add(Instruction(248, fallback)); helper.Add(Instruction(249, merge)); helper.Add(Instruction(248, merge));
                if (store) helper.Add(Instruction(253)); else { incoming.Add(Zero(resultType)); incoming.Add(fallback); helper.Add(Instruction(254, Emit(helper, 245, resultType, incoming.ToArray()))); }
                helper.Add(Instruction(56)); helpers.AddRange(helper);
            }
            return function;
        }
        foreach (var original in source)
        {
            var i = (uint[])original.Clone(); var op = Op(i); if (op == 54) inFunction = true; var body = inFunction ? functions : globals;
            var removedType = op is 28 or 29 && Opaque(i[1]) || op == 32 && Opaque(i[1]) && Op(types[i[3]]) is 28 or 29;
            if (removedType || op == 33 && aliases.ContainsKey(i[1]) || op == 59 && ids.Contains(i[2]) || op is 5 or 71 && ids.Contains(i[1])) continue;
            if (op == 15)
            {
                var end = StringEnd(i, 3); var io = i.AsSpan(end).ToArray().Where(id => !ids.Contains(id)); if (module.Header[1] >= 0x10400) io = io.Concat(slots.Select(s => s.Variable));
                i = Instruction(15, [.. i.AsSpan(1, end - 1).ToArray(), .. io]);
            }
            if (op == 54 && aliases.TryGetValue(i[4], out var signature)) i[4] = signature;
            if (op == 5 && aliases.TryGetValue(i[1], out var named)) i[1] = named;
            if (op is 65 or 66 && Opaque(i[1]))
            {
                if (i.Length != 5) throw new NotSupportedException("Image descriptors require one index.");
                var index = valueTypes[i[4]] == integer ? i[4] : Emit(body, 124, integer, i[4]); var count = DescriptorShape(valueTypes[i[3]], types, lengths, symbolic).Count;
                var added = Emit(body, 128, integer, i[3], index); var valid = Emit(body, 176, boolean, index, Constant((uint)count)); body.Add(Instruction(169, integer, i[2], valid, added, Constant(uint.MaxValue))); continue;
            }
            if (op == 61 && Opaque(i[1])) { body.Add(Instruction(83, integer, i[2], i[3])); continue; }
            var imageOperand = op == 99 ? 1 : 3;
            if (op is 98 or 99 or 104 && valueTypes.TryGetValue(i[imageOperand], out var resourceType) && Opaque(resourceType))
            {
                var image = Image(resourceType); var resultType = op == 99 ? nothing : i[1]; var mask = op == 99 ? 4 : op == 98 ? 5 : int.MaxValue;
                var arguments = Enumerable.Range(imageOperand + 1, i.Length - imageOperand - 1).Where(n => n != mask).ToArray(); var parameterTypes = arguments.Select(n => valueTypes[i[n]]).ToArray();
                var key = op + ":" + image + ":" + resultType + ":" + (mask < i.Length ? i[mask] : 0) + ":" + string.Join(',', parameterTypes);
                if (!operations.TryGetValue(key, out var function))
                {
                    function = next++; operations.Add(key, function); var helper = new List<uint[]> { Instruction(54, resultType, function, 0, Type(33, [resultType, integer, .. parameterTypes])) };
                    var selector = next++; helper.Add(Instruction(55, integer, selector)); var parameters = parameterTypes.Select(type => { var id = next++; helper.Add(Instruction(55, type, id)); return id; }).ToArray(); helper.Add(Instruction(248, next++));
                    var choices = slots.Where(s => s.Image == image).ToArray(); var labels = choices.Select(_ => next++).ToArray(); var merge = next++; var fallback = next++; var incoming = new List<uint>();
                    helper.Add(Instruction(247, merge, 0)); helper.Add(Instruction(251, [selector, fallback, .. choices.SelectMany((s, n) => new uint[] { s.Selector, labels[n] }).ToArray()]));
                    for (var n = 0; n < choices.Length; n++)
                    {
                        helper.Add(Instruction(248, labels[n])); var loaded = Emit(helper, 61, image, choices[n].Variable); var operation = (uint[])i.Clone(); operation[imageOperand] = loaded;
                        for (var a = 0; a < arguments.Length; a++) operation[arguments[a]] = parameters[a];
                        if (op != 99) { operation[2] = next++; incoming.Add(operation[2]); incoming.Add(labels[n]); }
                        helper.Add(operation); helper.Add(Instruction(249, merge));
                    }
                    helper.Add(Instruction(248, fallback)); helper.Add(Instruction(249, merge)); helper.Add(Instruction(248, merge));
                    if (op == 99) helper.Add(Instruction(253)); else { incoming.Add(Zero(resultType)); incoming.Add(fallback); helper.Add(Instruction(254, Emit(helper, 245, resultType, incoming.ToArray()))); }
                    helper.Add(Instruction(56)); helpers.AddRange(helper);
                }
                var result = op == 99 ? next++ : i[2]; body.Add(Instruction(57, [resultType, result, function, i[imageOperand], .. arguments.Select(n => i[n])])); continue;
            }
            if (op == 60 && valueTypes.TryGetValue(i[3], out var atomic) && Opaque(atomic))
            {
                var image = Image(atomic); if (types[image][3] != 1 || types[image][5] != 0 || types[image][8] is not (24 or 33)) throw new NotSupportedException("Image atomics require ordinary R32SINT or R32UINT storage images.");
                atomicPointers.Add(i[2], (image, i[3], i[4], i[5], i[1])); continue;
            }
            if (op == 83 && atomicPointers.TryGetValue(i[3], out var copied)) { atomicPointers.Add(i[2], copied); continue; }
            var pointerOperand = op == 228 ? 1 : 3;
            if (op is >= 227 and <= 242 && atomicPointers.TryGetValue(i[pointerOperand], out var pointer))
            {
                var function = Atomic(i, pointer); var resultType = op == 228 ? nothing : i[1]; var result = op == 228 ? next++ : i[2]; var memoryCount = op is 230 or 231 ? 3 : 2;
                body.Add(Instruction(57, [resultType, result, function, pointer.Selector, pointer.Coordinate, .. i.AsSpan(pointerOperand + 1 + memoryCount).ToArray()])); continue;
            }
            if (i.Length > 3 && atomicPointers.ContainsKey(i[3])) throw new NotSupportedException("This image atomic operation requires its native precision or pointer adapter.");
            if (inFunction && i.Length > 2 && Opaque(i[1])) i[1] = integer;
            body.Add(i);
        }
        globals.RemoveAll(i => Op(i) is 5 or 71 && atomicPointers.ContainsKey(i[1]));
        module.Header[3] = next; module.Instructions.Clear(); module.Instructions.AddRange(globals.Concat(annotations).OrderBy(i => Category(Op(i))).Concat(declarations).Concat(functions).Concat(helpers));
        return (module.Write(), origins);
    }
}
