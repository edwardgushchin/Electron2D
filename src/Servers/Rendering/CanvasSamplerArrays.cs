using System.Runtime.InteropServices;
using System.Text;
using static Electron2D.MaterialLightingProgram;

namespace Electron2D;

// SDL exposes scalar descriptor slots; opaque values become slot indices only in the derived native module.
internal static partial class CanvasSamplerArrays
{
    private sealed record Slot(uint Type, uint Variable, int Index, uint Selector, uint ImageSelector);

    internal static byte[] Lower(byte[] code, ShaderTexture[] textures, bool needsSampler = false, DevicePlan? device = null)
    {
        if (device is null && !needsSampler && !textures.Any(t => t.ArrayLength != 0)) return code;
        var module = new Module(code);
        var source = module.Instructions;
        var types = source.Where(i => Op(i) is >= 19 and <= 39).ToDictionary(i => i[1]);
        uint next = module.Header[3];
        var declarations = new List<uint[]>(); var annotations = new List<uint[]>(); var helpers = new List<uint[]>();
        uint Type(uint op, params uint[] operands)
        {
            var existing = types.Values.FirstOrDefault(i => Op(i) == op && i.AsSpan(2).SequenceEqual(operands));
            if (existing is not null) return existing[1];
            var instruction = Instruction(op, [next++, .. operands]); types.Add(instruction[1], instruction); declarations.Add(instruction); return instruction[1];
        }
        var integer = Type(21, 32, 0); var boolean = Type(20);
        // Rewritten signatures and interleaved constants need uint before every global declaration.
        var integerDeclaration = types[integer]; source.Remove(integerDeclaration); declarations.Remove(integerDeclaration);
        source.Insert(source.FindIndex(i => Category(Op(i)) == 10), integerDeclaration);
        bool Opaque(uint type) => types.TryGetValue(type, out var i) && (Op(i) is 26 or 27 || Op(i) == 25 && (device is null || i[7] == 1) || Op(i) == 28 && Opaque(i[2]) || Op(i) == 32 && i[2] == 0 && Opaque(i[3]));
        uint Element(uint type)
        {
            var i = types[type]; return Op(i) == 32 ? Element(i[3]) : Op(i) == 28 ? Element(i[2]) : type;
        }
        uint Image(uint type) { type = Element(type); return Op(types[type]) == 27 ? types[type][2] : type; }
        var valueTypes = source.Where(i => i.Length > 2 && (Op(i) == 12 || Op(i) >= 41) && Category(Op(i)) != 9 && types.ContainsKey(i[1])).ToDictionary(i => i[2], i => i[1]);
        var constants = source.Where(i => Op(i) is >= 41 and <= 52).Select(i => i[2]).ToHashSet();
        var unsigned = source.Where(i => Op(i) == 43 && i[1] == integer).GroupBy(i => i[3]).ToDictionary(g => g.Key, g => g.First()[2]);
        uint Constant(uint value)
        {
            if (unsigned.TryGetValue(value, out var id)) return id;
            id = next++; unsigned.Add(value, id); declarations.Add(Instruction(43, integer, id, value)); return id;
        }
        var nulls = new Dictionary<uint, uint>();
        uint Zero(uint type)
        {
            if (nulls.TryGetValue(type, out var id)) return id;
            id = next++; nulls.Add(type, id); declarations.Add(Instruction(46, type, id)); return id;
        }
        var decorations = source.Where(i => Op(i) == 71 && i.Length == 4).GroupBy(i => i[1]).ToDictionary(g => g.Key, g => g.ToDictionary(i => i[2], i => i[3]));
        foreach (var group in source.Where(i => Op(i) == 74))
            if (decorations.TryGetValue(group[1], out var values))
                foreach (var target in group.AsSpan(2))
                {
                    if (!decorations.TryGetValue(target, out var own)) decorations.Add(target, own = []);
                    foreach (var pair in values) own[pair.Key] = pair.Value;
                }
        var resources = source.Where(i => Op(i) == 59 && i[3] == 0 && Opaque(i[1])).ToArray();
        var resourceIDs = resources.Select(i => i[2]).ToHashSet();
        if (device is null) ValidatePairs(source, resourceIDs, decorations, Opaque);
        var slots = new List<Slot>(); var samplers = new Dictionary<int, uint>(); var interfaces = new List<uint>();
        void Name(uint variable, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value + '\0'); Array.Resize(ref bytes, (bytes.Length + 3) & ~3);
            annotations.Add(Instruction(5, [variable, .. MemoryMarshal.Cast<byte, uint>(bytes).ToArray()]));
        }
        var names = source.Where(i => Op(i) == 5).ToDictionary(i => i[1], i => Encoding.UTF8.GetString(MemoryMarshal.AsBytes(i.AsSpan(2))).TrimEnd('\0'));
        if (device is not null)
        {
            foreach (var resource in resources) declarations.Add(Instruction(43, integer, resource[2], device.Values[resource[2]]));
            for (var index = 0; index < device.Pairs.Length; index++)
            {
                var pair = device.Pairs[index]; var resource = resources.Single(r => r[2] == pair.Image); var element = Element(resource[1]);
                var variable = next++; declarations.Add(Instruction(59, Type(32, 0, element), variable, 0)); interfaces.Add(variable);
                annotations.Add(Instruction(71, variable, 33, (uint)index)); annotations.Add(Instruction(71, variable, 34, device.Set));
                slots.Add(new(element, variable, index, pair.ImageIndex * device.Stride + pair.SamplerIndex, pair.ImageIndex));
                if (Op(types[element]) == 25)
                {
                    var sampler = next++; declarations.Add(Instruction(59, Type(32, 0, Type(26)), sampler, 0)); interfaces.Add(sampler); samplers.Add(index, sampler);
                    annotations.Add(Instruction(71, sampler, 33, (uint)index)); annotations.Add(Instruction(71, sampler, 34, device.Set));
                }
            }
        }
        else foreach (var resource in resources)
            {
                var element = Element(resource[1]);
                var binding = decorations.TryGetValue(resource[2], out var d) && d.TryGetValue(33, out var b) ? b : uint.MaxValue;
                var first = Array.FindIndex(textures, t => t.Binding == binding);
                declarations.Add(Instruction(43, integer, resource[2], (uint)Math.Max(0, first)));
                if (first < 0 || Op(types[element]) == 26 || names.GetValueOrDefault(resource[2]) != textures[first].Name) continue;
                for (var index = first; index < textures.Length && textures[index].Binding == binding; index++)
                {
                    var variable = next++; declarations.Add(Instruction(59, Type(32, 0, element), variable, 0)); interfaces.Add(variable);
                    annotations.Add(Instruction(71, variable, 33, (uint)index));
                    annotations.Add(Instruction(71, variable, 34, d![34])); Name(variable, $"EDSampler{index}_{element}");
                    slots.Add(new(element, variable, index, (uint)index, (uint)index));
                    if (Op(types[element]) == 25)
                    {
                        var sampler = next++; declarations.Add(Instruction(59, Type(32, 0, Type(26)), sampler, 0)); interfaces.Add(sampler); samplers.Add(index, sampler);
                        annotations.Add(Instruction(71, sampler, 33, (uint)index)); annotations.Add(Instruction(71, sampler, 34, d[34])); Name(sampler, $"EDState{index}");
                    }
                }
            }
        var functionAliases = new Dictionary<uint, uint>(); var signatures = new Dictionary<string, uint>(StringComparer.Ordinal);
        foreach (var function in source.Where(i => Op(i) == 33))
        {
            for (var i = 2; i < function.Length; i++) if (Opaque(function[i])) function[i] = integer;
            var key = string.Join(',', function.AsSpan(2).ToArray());
            if (signatures.TryGetValue(key, out var alias)) functionAliases.Add(function[1], alias); else signatures.Add(key, function[1]);
        }
        uint FunctionType(uint result, uint[] parameters)
        {
            var operands = new uint[] { result }.Concat(parameters).ToArray(); var key = string.Join(',', operands);
            if (signatures.TryGetValue(key, out var id)) return id;
            id = Type(33, operands); signatures.Add(key, id); return id;
        }
        var functions = new List<uint[]>(); var originals = new List<uint[]>(); var inFunction = false;
        var samples = new Dictionary<string, uint>(StringComparer.Ordinal);
        uint Load(Slot slot, uint wanted, List<uint[]> body)
        {
            var loaded = next++; body.Add(Instruction(61, slot.Type, loaded, slot.Variable));
            if (slot.Type == wanted && Op(types[slot.Type]) == 27) return loaded;
            if (Op(types[slot.Type]) == 25)
            {
                var loadedSampler = next++; body.Add(Instruction(61, Type(26), loadedSampler, samplers[slot.Index]));
                var combined = next++; var combinedType = Type(27, slot.Type); body.Add(Instruction(86, combinedType, combined, loaded, loadedSampler));
                loaded = combined;
                if (wanted == combinedType) return loaded;
            }
            var converted = next++; body.Add(Instruction(100, wanted, converted, loaded));
            return converted;
        }
        void Sample(uint[] instruction, List<uint[]> body)
        {
            var op = Op(instruction); var resourceType = Element(valueTypes[instruction[3]]);
            uint Selector(Slot slot) => device is not null && Op(types[resourceType]) == 25 ? slot.ImageSelector : slot.Selector;
            var choices = slots.Where(s => Image(s.Type) == Image(resourceType)).GroupBy(Selector).Select(g => g.First()).ToArray();
            if (choices.Length == 0) { body.Add(Instruction(83, instruction[1], instruction[2], Zero(instruction[1]))); return; }
            var mask = op is 87 or 88 or 91 or 92 or 95 ? 5 : op is 89 or 90 or 93 or 94 or 96 or 97 ? 6 : int.MaxValue;
            var operands = Enumerable.Range(4, instruction.Length - 4).Where(i => i != mask && !constants.Contains(instruction[i])).ToArray();
            var parameterTypes = operands.Select(i => valueTypes[instruction[i]]).ToArray();
            var key = op + ":" + instruction[1] + ":" + resourceType + ":" + string.Join(',', Enumerable.Range(4, instruction.Length - 4).Select(i => operands.Contains(i) ? $"p{valueTypes[instruction[i]]}" : $"c{instruction[i]}"));
            if (!samples.TryGetValue(key, out var function))
            {
                function = next++; samples.Add(key, function);
                var functionType = FunctionType(instruction[1], [integer, .. parameterTypes]);
                var helper = new List<uint[]> { Instruction(54, instruction[1], function, 0, functionType) };
                var selector = next++; helper.Add(Instruction(55, integer, selector));
                var template = (uint[])instruction.Clone();
                for (var j = 0; j < operands.Length; j++) { var parameter = next++; helper.Add(Instruction(55, parameterTypes[j], parameter)); template[operands[j]] = parameter; }
                helper.Add(Instruction(248, next++));
                uint Invoke(Slot slot)
                {
                    var sample = (uint[])template.Clone(); sample[2] = next++; sample[3] = Load(slot, resourceType, helper); helper.Add(sample); return sample[2];
                }
                uint result;
                // Eager implicit sampling preserves quad derivatives even for a nonuniform selected descriptor.
                if (op is 87 or 89 or 91 or 93 or 96 or 97 or 105)
                {
                    result = Zero(instruction[1]);
                    foreach (var slot in choices)
                    {
                        var sample = Invoke(slot); var condition = next++; helper.Add(Instruction(170, boolean, condition, selector, Constant(Selector(slot))));
                        if (Op(types[instruction[1]]) == 23)
                        {
                            var width = types[instruction[1]][3]; var splat = next++;
                            helper.Add(Instruction(80, [Type(23, boolean, width), splat, .. Enumerable.Repeat(condition, (int)width).ToArray()])); condition = splat;
                        }
                        var selected = next++; helper.Add(Instruction(169, instruction[1], selected, condition, sample, result)); result = selected;
                    }
                }
                else
                {
                    var merge = next++; var fallback = next++; var labels = choices.Select(_ => next++).ToArray();
                    helper.Add(Instruction(247, merge, 0)); helper.Add(Instruction(251, [selector, fallback, .. choices.SelectMany((slot, i) => new uint[] { Selector(slot), labels[i] }).ToArray()]));
                    var incoming = new List<uint>();
                    for (var i = 0; i < choices.Length; i++)
                    {
                        helper.Add(Instruction(248, labels[i])); incoming.Add(Invoke(choices[i])); incoming.Add(labels[i]); helper.Add(Instruction(249, merge));
                    }
                    helper.Add(Instruction(248, fallback)); helper.Add(Instruction(249, merge)); incoming.Add(Zero(instruction[1])); incoming.Add(fallback);
                    helper.Add(Instruction(248, merge)); result = next++; helper.Add(Instruction(245, [instruction[1], result, .. incoming]));
                }
                helper.Add(Instruction(254, result)); helper.Add(Instruction(56)); helpers.AddRange(helper);
            }
            body.Add(Instruction(57, [instruction[1], instruction[2], function, instruction[3], .. operands.Select(i => instruction[i])]));
        }
        foreach (var original in source)
        {
            var instruction = (uint[])original.Clone(); var op = Op(instruction);
            if (op == 54) inFunction = true;
            var output = inFunction ? functions : originals;
            if (op == 59 && resourceIDs.Contains(instruction[2]) || op == 33 && functionAliases.ContainsKey(instruction[1]) || op == 71 && (resourceIDs.Contains(instruction[1]) || instruction[2] == 5300) || op == 5 && resourceIDs.Contains(instruction[1]) || op == 17 && instruction[1] is 29 or 5301 or 5307) continue;
            if (op == 74)
            {
                instruction = Instruction(74, [instruction[1], .. instruction.AsSpan(2).ToArray().Where(id => !resourceIDs.Contains(id))]);
                if (instruction.Length == 2) continue;
            }
            if (op == 5 && functionAliases.TryGetValue(instruction[1], out var named)) instruction[1] = named;
            if (op == 15)
            {
                var end = StringEnd(instruction, 3); var io = instruction.AsSpan(end).ToArray().Where(id => !resourceIDs.Contains(id));
                if (module.Header[1] >= 0x10400) io = io.Concat(interfaces);
                instruction = Instruction(15, [.. instruction.AsSpan(1, end - 1).ToArray(), .. io]);
            }
            if (op == 54 && functionAliases.TryGetValue(instruction[4], out var signature)) instruction[4] = signature;
            if (op is >= 87 and <= 97 or >= 101 and <= 107 && valueTypes.TryGetValue(instruction[3], out var resourceType) && Opaque(resourceType)) { Sample(instruction, output); continue; }
            if (device is not null && op == 86)
            {
                var multiplied = next++; output.Add(Instruction(132, integer, multiplied, instruction[3], Constant(device.Stride)));
                var combined = next++; output.Add(Instruction(128, integer, combined, multiplied, instruction[4]));
                var imageValid = next++; output.Add(Instruction(176, boolean, imageValid, instruction[3], Constant(device.ImageCount)));
                var samplerValid = next++; output.Add(Instruction(176, boolean, samplerValid, instruction[4], Constant(device.Stride - 1)));
                var valid = next++; output.Add(Instruction(167, boolean, valid, imageValid, samplerValid));
                output.Add(Instruction(169, integer, instruction[2], valid, combined, Constant(uint.MaxValue))); continue;
            }
            if (device is not null && op == 100) { output.Add(Instruction(134, integer, instruction[2], instruction[3], Constant(device.Stride))); continue; }
            if (op is 86 or 100) { output.Add(Instruction(83, integer, instruction[2], instruction[3])); continue; }
            if (op == 61 && Opaque(instruction[1])) { output.Add(Instruction(83, integer, instruction[2], instruction[3])); continue; }
            if (op is 65 or 66 && Opaque(instruction[1]))
            {
                if (instruction.Length != 5) throw new NotSupportedException("Sampled descriptor access requires a one-dimensional array index.");
                var index = instruction[4];
                if (valueTypes[index] != integer) { var converted = next++; output.Add(Instruction(124, integer, converted, index)); index = converted; }
                if (device is not null)
                {
                    var declaration = types[valueTypes[instruction[3]]]; if (Op(declaration) == 32) declaration = types[declaration[3]];
                    if (Op(declaration) != 28) throw new NotSupportedException("Sampled descriptors require an array index at their declaring layer.");
                    var origin = resources.FirstOrDefault(r => r[2] == instruction[3]);
                    var count = origin is not null ? device.Sources[origin[2]].Count : DescriptorShape(valueTypes[instruction[3]], types, source.Where(i => Op(i) == 43 && i.Length == 4).ToDictionary(i => i[2], i => i[3]), symbolic: true).Count;
                    var step = Op(types[Element(valueTypes[instruction[3]])]) == 27 ? device.Stride + 1 : 1;
                    var offset = next++; output.Add(Instruction(132, integer, offset, index, Constant(step)));
                    var added = next++; output.Add(Instruction(128, integer, added, instruction[3], offset));
                    var valid = next++; output.Add(Instruction(176, boolean, valid, index, Constant((uint)count)));
                    output.Add(Instruction(169, integer, instruction[2], valid, added, Constant(uint.MaxValue)));
                }
                else output.Add(Instruction(128, integer, instruction[2], instruction[3], index));
                continue;
            }
            if (inFunction && instruction.Length > 2 && Opaque(instruction[1])) instruction[1] = integer;
            output.Add(instruction);
        }
        if (next > 0x3fffff) throw new NotSupportedException("Expanded canvas bytecode exceeds the supported ID bound.");
        module.Header[3] = next;
        var all = originals.Concat(annotations).OrderBy(i => Category(Op(i))).Concat(declarations).Concat(functions).Concat(helpers).ToArray();
        var wordCount = checked(5 + all.Sum(i => i.Length));
        if (wordCount > 4 * 1024 * 1024) throw new NotSupportedException("Expanded canvas bytecode exceeds 16 MiB.");
        var words = new uint[wordCount]; module.Header.CopyTo(words, 0); var position = 5;
        foreach (var instruction in all) { instruction.CopyTo(words, position); position += instruction.Length; }
        return MemoryMarshal.AsBytes(words.AsSpan()).ToArray();
    }
}
