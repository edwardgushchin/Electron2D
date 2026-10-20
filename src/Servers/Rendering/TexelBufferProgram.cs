using System.Runtime.InteropServices;
using System.Text;
using static Electron2D.MaterialLightingProgram;

namespace Electron2D;

internal static partial class TexelBufferProgram
{
    internal readonly record struct Source(uint Set, int Binding, RenderingDevice.UniformType Type, uint Format, int PixelType);
    private sealed record Buffer(uint ImageType, uint Variable, uint Selector, Source Source);

    internal static (byte[] Code, Source[] Sources) Lower(byte[] code)
    {
        var module = new Module(code);
        if (!module.Instructions.Any(i => Op(i) == 25 && i[3] == 5)) return (code, []);
        var library = ImportLibrary(module);
        var source = module.Instructions;
        var types = source.Where(i => Op(i) is >= 19 and <= 39).ToDictionary(i => i[1]);
        var declarations = new List<uint[]>(); var annotations = new List<uint[]>(); var helpers = new List<uint[]>(); var next = module.Header[3];
        uint Type(uint op, params uint[] operands)
        {
            var found = types.Values.FirstOrDefault(i => Op(i) == op && i.AsSpan(2).SequenceEqual(operands));
            if (found is not null) return found[1];
            var instruction = Instruction(op, [next++, .. operands]); types.Add(instruction[1], instruction); declarations.Add(instruction); return instruction[1];
        }
        var integer = Type(21, 32, 0); var signed = Type(21, 32, 1); var boolean = Type(20); var vector = Type(23, integer, 4); var nothing = Type(19);
        var integerDeclaration = types[integer]; source.Remove(integerDeclaration); declarations.Remove(integerDeclaration);
        source.Insert(source.FindIndex(i => Category(Op(i)) == 10), integerDeclaration);
        bool Opaque(uint id)
        {
            for (var depth = 0; depth < 32; depth++)
            {
                if (!types.TryGetValue(id, out var i)) return false;
                var op = Op(i); if (op == 25) return i[3] == 5;
                if (op is not (27 or 32)) return false;
                id = i[op == 32 ? 3 : 2];
            }
            throw new ArgumentException("The texel resource type graph is cyclic or exceeds its supported pointer depth.", nameof(code));
        }
        if (source.Any(i => Op(i) is 28 or 29 && Opaque(i[2]))) throw new NotSupportedException("Texel descriptor arrays require their resource-array integration.");
        uint Image(uint id) { var i = types[id]; return Op(i) == 25 ? id : Image(i[Op(i) == 32 ? 3 : 2]); }
        var valueTypes = source.Where(i => i.Length > 2 && (Op(i) == 12 || Op(i) >= 41) && Category(Op(i)) != 9 && types.ContainsKey(i[1])).ToDictionary(i => i[2], i => i[1]);
        var constants = source.Where(i => Op(i) == 43 && i[1] == integer).GroupBy(i => i[3]).ToDictionary(g => g.Key, g => g.First()[2]);
        uint Constant(uint value) { if (constants.TryGetValue(value, out var id)) return id; id = next++; constants.Add(value, id); declarations.Add(Instruction(43, integer, id, value)); return id; }
        var nulls = new Dictionary<uint, uint>();
        uint Zero(uint type) { if (nulls.TryGetValue(type, out var id)) return id; id = next++; nulls.Add(type, id); declarations.Add(Instruction(46, type, id)); return id; }
        var locations = source.Where(i => Op(i) == 71 && i.Length == 4 && i[2] is 33 or 34).GroupBy(i => i[1]).ToDictionary(g => g.Key, g => g.ToDictionary(i => i[2], i => i[3]));
        var resources = source.Where(i => Op(i) == 59 && i[3] == 0 && Opaque(i[1])).ToArray();
        var ids = resources.Select(i => i[2]).ToHashSet(); var buffers = new List<Buffer>();
        var storage = module.Header[1] >= 0x10300 ? 12u : 2u;
        var array = next++; declarations.Add(Instruction(29, array, integer)); var block = next++; declarations.Add(Instruction(30, block, array)); var pointer = Type(32, storage, block); var wordPointer = Type(32, storage, integer);
        annotations.Add(Instruction(71, array, 6, 4)); annotations.Add(Instruction(71, block, storage == 12 ? 2u : 3u)); annotations.Add(Instruction(72, block, 0, 35, 0));
        foreach (var resource in resources)
        {
            if (!locations.TryGetValue(resource[2], out var location) || !location.TryGetValue(34, out var set) || !location.TryGetValue(33, out var slot) || slot > int.MaxValue) throw new ArgumentException("A texel buffer needs a valid logical set and binding.", nameof(code));
            var image = types[Image(resource[1])]; var scalar = types[image[2]];
            if (scalar[2] != 32 || Op(scalar) is not (21 or 22) || image[4] is not (0 or 2) || image[5] != 0 || image[6] != 0 || image[7] is not (1 or 2)) throw new NotSupportedException("Texel buffers require ordinary 32-bit scalar image operations.");
            var element = types[types[resource[1]][3]];
            var type = image[7] == 2 ? RenderingDevice.UniformType.ImageBuffer : Op(element) == 27 ? RenderingDevice.UniformType.SamplerWithTextureBuffer : RenderingDevice.UniformType.TextureBuffer;
            var variable = next++; var selector = (uint)buffers.Count;
            buffers.Add(new(image[1], variable, selector, new(set, (int)slot, type, image[7] == 2 ? image[8] : 0, Op(scalar) == 22 ? 0 : scalar[3] == 1 ? 1 : 2)));
            declarations.Add(Instruction(43, integer, resource[2], selector)); declarations.Add(Instruction(59, pointer, variable, storage));
            annotations.Add(Instruction(71, variable, 33, slot)); annotations.Add(Instruction(71, variable, 34, set));
            foreach (var decoration in source.Where(i => Op(i) == 71 && i[1] == resource[2] && i[2] is 19 or 21 or 23)) annotations.Add(Instruction(71, [variable, .. decoration.AsSpan(2).ToArray()]));
            if (type != RenderingDevice.UniformType.ImageBuffer || source.Any(i => Op(i) == 71 && i[1] == resource[2] && i[2] == 24)) annotations.Add(Instruction(71, variable, 24));
        }
        var functionAliases = new Dictionary<uint, uint>(); var signatures = new Dictionary<string, uint>();
        foreach (var function in source.Where(i => Op(i) == 33))
        {
            for (var i = 2; i < function.Length; i++) if (Opaque(function[i])) function[i] = integer;
            var key = string.Join(',', function.AsSpan(2).ToArray());
            if (signatures.TryGetValue(key, out var alias)) functionAliases.Add(function[1], alias); else signatures.Add(key, function[1]);
        }
        var functions = new List<uint[]>(); var originals = new List<uint[]>(); var inFunction = false;
        var operations = new Dictionary<string, uint>();
        var atomicPointers = new Dictionary<uint, (uint Image, uint Selector, uint Coordinate)>();
        uint Emit(List<uint[]> body, uint op, uint type, params uint[] arguments) { var id = next++; body.Add(Instruction(op, [type, id, .. arguments])); return id; }
        uint Load(List<uint[]> body, Buffer buffer, uint index) => Emit(body, 61, integer, Emit(body, 65, wordPointer, buffer.Variable, Constant(0), index));

        uint Operation(uint opcode, uint imageType)
        {
            var key = opcode + ":" + imageType;
            if (operations.TryGetValue(key, out var existing)) return existing;
            var write = opcode == 99; var query = opcode == 104; var resultType = write ? nothing : query ? integer : vector;
            var function = next++; operations.Add(key, function);
            var body = new List<uint[]> { Instruction(54, resultType, function, 0, Type(33, write ? [resultType, integer, signed, vector] : [resultType, integer, signed])) };
            var selector = next++; var coordinate = next++; var value = write ? next++ : 0;
            body.Add(Instruction(55, integer, selector)); body.Add(Instruction(55, signed, coordinate)); if (write) body.Add(Instruction(55, vector, value));
            body.Add(Instruction(248, next++));
            var argument = next++; var widthsArgument = next++; var kindArgument = next++;
            body.Add(Instruction(59, Type(32, 7, vector), argument, 7)); body.Add(Instruction(59, Type(32, 7, integer), widthsArgument, 7)); body.Add(Instruction(59, Type(32, 7, integer), kindArgument, 7));
            var choices = buffers.Where(b => b.ImageType == imageType).ToArray();
            var merge = next++; var fallback = next++; var labels = choices.Select(_ => next++).ToArray();
            body.Add(Instruction(247, merge, 0)); body.Add(Instruction(251, [selector, fallback, .. choices.SelectMany((b, i) => new uint[] { b.Selector, labels[i] }).ToArray()]));
            var incoming = new List<uint>();
            for (var choice = 0; choice < choices.Length; choice++)
            {
                var buffer = choices[choice]; body.Add(Instruction(248, labels[choice]));
                var count = Load(body, buffer, Constant(0));
                if (query) { incoming.Add(count); incoming.Add(labels[choice]); body.Add(Instruction(249, merge)); continue; }
                var index = Emit(body, 124, integer, coordinate); var inRange = Emit(body, 176, boolean, index, count);
                var valid = next++; var invalid = next++; var complete = next++;
                body.Add(Instruction(247, complete, 0)); body.Add(Instruction(250, inRange, valid, invalid)); body.Add(Instruction(248, valid));
                var stride = Load(body, buffer, Constant(1)); var widths = Load(body, buffer, Constant(2)); var offsets = Load(body, buffer, Constant(3)); var kind = Load(body, buffer, Constant(4));
                var sharedFormat = Emit(body, 170, boolean, kind, Constant(8));
                body.Add(Instruction(62, widthsArgument, widths)); body.Add(Instruction(62, kindArgument, kind));
                var firstByte = Emit(body, 132, integer, index, stride);
                var firstWord = Emit(body, 194, integer, firstByte, Constant(2));
                var firstBit = Emit(body, 132, integer, Emit(body, 199, integer, firstByte, Constant(3)), Constant(8));
                var parts = new uint[4]; var addresses = new uint[4]; var bitOffsets = new uint[4]; var bitWidths = new uint[4];
                for (var lane = 0; lane < 4; lane++)
                {
                    bitWidths[lane] = Emit(body, 199, integer, Emit(body, 194, integer, widths, Constant((uint)(lane * 8))), Constant(255));
                    var componentBit = Emit(body, 199, integer, Emit(body, 194, integer, offsets, Constant((uint)(lane * 8))), Constant(255));
                    var location = Emit(body, 128, integer, firstBit, componentBit);
                    addresses[lane] = Emit(body, 128, integer, Emit(body, 128, integer, firstWord, Emit(body, 194, integer, location, Constant(5))), Constant(8));
                    bitOffsets[lane] = Emit(body, 199, integer, location, Constant(31));
                    if (!write)
                    {
                        var raw = Load(body, buffer, addresses[lane]);
                        parts[lane] = Emit(body, 203, integer, raw, bitOffsets[lane], bitWidths[lane]);
                        if (lane == 0) parts[lane] = Emit(body, 169, integer, sharedFormat, raw, parts[lane]);
                    }
                }
                uint result = 0;
                if (!write)
                {
                    body.Add(Instruction(62, argument, Emit(body, 80, vector, parts)));
                    result = Emit(body, 57, vector, library.Decode, argument, widthsArgument, kindArgument);
                }
                else
                {
                    body.Add(Instruction(62, argument, value));
                    var encoded = Emit(body, 57, vector, library.Encode, argument, widthsArgument, kindArgument);
                    for (var lane = 0; lane < 4; lane++)
                    {
                        var part = Emit(body, 81, integer, encoded, (uint)lane);
                        var width = Emit(body, 169, integer, sharedFormat, Constant(lane == 0 ? 32u : 0u), bitWidths[lane]);
                        var offset = Emit(body, 169, integer, sharedFormat, Constant(0), bitOffsets[lane]);
                        var address = Emit(body, 169, integer, sharedFormat, addresses[0], addresses[lane]);
                        var mask = Emit(body, 201, integer, Constant(0), Constant(uint.MaxValue), offset, width);
                        var target = Emit(body, 65, wordPointer, buffer.Variable, Constant(0), address);
                        Emit(body, 240, integer, target, Constant(1), Constant(0), Emit(body, 200, integer, mask));
                        Emit(body, 241, integer, target, Constant(1), Constant(0), Emit(body, 196, integer, part, offset));
                    }
                }
                var validEnd = next++; body.Add(Instruction(249, validEnd)); body.Add(Instruction(248, validEnd)); body.Add(Instruction(249, complete));
                body.Add(Instruction(248, invalid)); body.Add(Instruction(249, complete)); body.Add(Instruction(248, complete));
                if (!write) { result = Emit(body, 245, vector, result, validEnd, Zero(vector), invalid); incoming.Add(result); incoming.Add(complete); }
                body.Add(Instruction(249, merge));
            }
            body.Add(Instruction(248, fallback)); body.Add(Instruction(249, merge)); body.Add(Instruction(248, merge));
            if (write) body.Add(Instruction(253));
            else { incoming.Add(Zero(resultType)); incoming.Add(fallback); var result = Emit(body, 245, resultType, incoming.ToArray()); body.Add(Instruction(254, result)); }
            body.Add(Instruction(56)); helpers.AddRange(body); return function;
        }

        uint AtomicOperation(uint opcode, uint imageType, uint[] memory, int values)
        {
            var key = "atomic:" + opcode + ":" + imageType + ":" + string.Join(',', memory);
            if (operations.TryGetValue(key, out var existing)) return existing;
            if (types[imageType][8] is not (24 or 33)) throw new NotSupportedException("Texel atomics require an explicit signed or unsigned single-component 32-bit image format.");
            var store = opcode == 228; var resultType = store ? nothing : integer; var function = next++; operations.Add(key, function);
            var parameterTypes = new uint[] { integer, signed }.Concat(Enumerable.Repeat(integer, values)).ToArray();
            var body = new List<uint[]> { Instruction(54, resultType, function, 0, Type(33, [resultType, .. parameterTypes])) };
            var parameters = parameterTypes.Select(type => { var id = next++; body.Add(Instruction(55, type, id)); return id; }).ToArray();
            body.Add(Instruction(248, next++));
            var choices = buffers.Where(b => b.ImageType == imageType).ToArray(); var labels = choices.Select(_ => next++).ToArray();
            var merge = next++; var fallback = next++; var incoming = new List<uint>();
            body.Add(Instruction(247, merge, 0)); body.Add(Instruction(251, [parameters[0], fallback, .. choices.SelectMany((b, i) => new uint[] { b.Selector, labels[i] }).ToArray()]));
            for (var choice = 0; choice < choices.Length; choice++)
            {
                var buffer = choices[choice]; body.Add(Instruction(248, labels[choice]));
                var count = Load(body, buffer, Constant(0)); var coordinate = Emit(body, 124, integer, parameters[1]); var valid = next++; var invalid = next++; var complete = next++;
                var condition = Emit(body, 176, boolean, coordinate, count);
                body.Add(Instruction(247, complete, 0)); body.Add(Instruction(250, condition, valid, invalid)); body.Add(Instruction(248, valid));
                var address = Emit(body, 128, integer, coordinate, Constant(8)); var pointerID = Emit(body, 65, wordPointer, buffer.Variable, Constant(0), address);
                var operands = new uint[] { pointerID }.Concat(memory).Concat(parameters.Skip(2)).ToArray();
                var result = 0u;
                if (store) body.Add(Instruction(opcode, operands)); else result = Emit(body, opcode, integer, operands);
                body.Add(Instruction(249, complete)); body.Add(Instruction(248, invalid)); body.Add(Instruction(249, complete)); body.Add(Instruction(248, complete));
                if (!store) { incoming.Add(Emit(body, 245, integer, result, valid, Constant(0), invalid)); incoming.Add(complete); }
                body.Add(Instruction(249, merge));
            }
            body.Add(Instruction(248, fallback)); body.Add(Instruction(249, merge)); body.Add(Instruction(248, merge));
            if (store) body.Add(Instruction(253));
            else { incoming.Add(Constant(0)); incoming.Add(fallback); var result = Emit(body, 245, integer, incoming.ToArray()); body.Add(Instruction(254, result)); }
            body.Add(Instruction(56)); helpers.AddRange(body); return function;
        }

        uint MemorySemantics(uint id)
        {
            var value = source.FirstOrDefault(i => Op(i) == 43 && i[2] == id);
            return value is not null && (value[3] & 2048) != 0 ? Constant(value[3] | 64) : id;
        }
        foreach (var original in source)
        {
            var i = (uint[])original.Clone(); var op = Op(i); if (op == 54) inFunction = true;
            var output = inFunction ? functions : originals;
            if (op is >= 19 and <= 39 && Opaque(i[1]) || op == 33 && functionAliases.ContainsKey(i[1]) || op == 59 && ids.Contains(i[2]) || op is 5 or 71 && (ids.Contains(i[1]) || Opaque(i[1])) || op == 17 && i[1] is 46 or 47) continue;
            if (op == 15)
            {
                var end = StringEnd(i, 3); var io = i.AsSpan(end).ToArray().Where(id => !ids.Contains(id));
                if (module.Header[1] >= 0x10400) io = io.Concat(buffers.Select(b => b.Variable));
                i = Instruction(15, [.. i.AsSpan(1, end - 1).ToArray(), .. io]);
            }
            if (op == 54 && functionAliases.TryGetValue(i[4], out var alias)) i[4] = alias;
            if (op == 5 && functionAliases.TryGetValue(i[1], out var named)) i[1] = named;
            if (op == 61 && Opaque(i[1]) || op is 86 or 100 && Opaque(i[1])) { output.Add(Instruction(83, integer, i[2], i[3])); continue; }
            var imageOperand = op == 99 ? 1 : 3;
            if (op is 95 or 98 or 99 or 104 && valueTypes.TryGetValue(i[imageOperand], out var imageValue) && Opaque(imageValue))
            {
                var function = Operation(op, Image(imageValue)); var coordinate = op == 104 ? Zero(signed) : i[imageOperand + 1];
                if (op != 104 && valueTypes[coordinate] != signed) coordinate = Emit(output, 124, signed, coordinate);
                var arguments = new List<uint> { i[imageOperand], coordinate };
                if (op == 99) arguments.Add(valueTypes[i[3]] == vector ? i[3] : Emit(output, 124, vector, i[3]));
                var resultType = op == 99 ? nothing : op == 104 ? integer : vector;
                var result = Emit(output, 57, resultType, [function, .. arguments]);
                if (op != 99) output.Add(Instruction(i[1] == resultType ? 83u : 124u, i[1], i[2], result));
                continue;
            }
            if (op == 60 && valueTypes.TryGetValue(i[3], out var atomicImage) && Opaque(atomicImage))
            {
                var coordinate = valueTypes[i[4]] == signed ? i[4] : Emit(output, 124, signed, i[4]);
                atomicPointers.Add(i[2], (Image(atomicImage), i[3], coordinate)); continue;
            }
            if (op == 83 && atomicPointers.TryGetValue(i[3], out var copied)) { atomicPointers.Add(i[2], copied); continue; }
            var atomicPointer = op == 228 ? 1 : 3;
            if (op is >= 227 and <= 242 && atomicPointers.TryGetValue(i[atomicPointer], out var atomic))
            {
                var memoryCount = op is 230 or 231 ? 3 : 2;
                var memory = i.AsSpan(atomicPointer + 1, memoryCount).ToArray();
                for (var m = 1; m < memory.Length; m++) memory[m] = MemorySemantics(memory[m]);
                var arguments = i.AsSpan(atomicPointer + 1 + memoryCount).ToArray().Select(value => valueTypes[value] == integer ? value : Emit(output, 124, integer, value)).ToArray();
                var function = AtomicOperation(op, atomic.Image, memory, arguments.Length);
                var result = Emit(output, 57, op == 228 ? nothing : integer, [function, atomic.Selector, atomic.Coordinate, .. arguments]);
                if (op != 228) output.Add(Instruction(i[1] == integer ? 83u : 124u, i[1], i[2], result));
                continue;
            }
            if (op is 224 or 225) i[^1] = MemorySemantics(i[^1]);
            if (inFunction && i.Length > 2 && Opaque(i[1])) i[1] = integer;
            output.Add(i);
        }
        module.Header[3] = next;
        var all = originals.Concat(annotations).OrderBy(i => Category(Op(i))).Concat(declarations).Concat(functions).Concat(helpers).ToArray();
        var words = new uint[checked(5 + all.Sum(i => i.Length))]; module.Header.CopyTo(words, 0); var position = 5;
        foreach (var i in all) { i.CopyTo(words, position); position += i.Length; }
        return (MemoryMarshal.AsBytes(words.AsSpan()).ToArray(), buffers.Select(b => b.Source).ToArray());
    }
}
