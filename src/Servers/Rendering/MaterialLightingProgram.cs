using System.Runtime.InteropServices;

namespace Electron2D;

internal static class MaterialLightingProgram
{
    internal sealed class Module(byte[] bytes)
    {
        internal readonly uint[] Header = MemoryMarshal.Cast<byte, uint>(bytes.AsSpan(0, 20)).ToArray();
        internal readonly List<uint[]> Instructions = Read(bytes);
        internal static List<uint[]> Read(byte[] bytes)
        {
            var words = MemoryMarshal.Cast<byte, uint>(bytes); var result = new List<uint[]>();
            for (var i = 5; i < words.Length; i += (int)(words[i] >> 16)) result.Add(words.Slice(i, (int)(words[i] >> 16)).ToArray());
            return result;
        }
    }
    private sealed record Value(uint Variable, uint Type, uint[] Path);
    internal static uint Op(uint[] instruction) => instruction[0] & 65535;
    internal static uint[] Instruction(uint opcode, params uint[] operands) => [(uint)((operands.Length + 1) << 16) | opcode, .. operands];
    internal static int Category(uint opcode) => opcode switch { 17 => 0, 10 => 1, 11 => 2, 14 => 3, 15 => 4, 16 or 331 => 5, 2 or 3 or 4 or 7 => 6, 5 or 6 => 7, 330 => 8, 71 or 72 or 73 or 74 or 75 or 332 or 5632 or 5633 => 9, _ => 10 };
    private static bool UniqueType(uint opcode) => opcode is >= 19 and <= 27 or 32 or 33 or 11;
    private static string TypeKey(uint[] instruction) => instruction[0] + ":" + string.Join(',', instruction.AsSpan(2).ToArray());
    private static uint Alias(Dictionary<uint, uint> aliases, uint id) { while (aliases.TryGetValue(id, out var replacement)) id = replacement; return id; }
    internal static int StringEnd(uint[] instruction, int first)
    {
        for (var i = first; i < instruction.Length; i++)
            if ((instruction[i] & 255) == 0 || (instruction[i] & 65280) == 0 || (instruction[i] & 16711680) == 0 || (instruction[i] & 4278190080) == 0) return i + 1;
        throw new ArgumentException("The shader interface string is unterminated.");
    }

    internal static byte[] Compose(ShaderProgram program, int normal, int specular, bool masks)
    {
        program = program.Fragment ?? program;
        var extraSamplers = (normal >= 0 ? 1 : 0) + (specular >= 0 ? 1 : 0) + (masks ? 1 : 0);
        if (program.Textures.Length + extraSamplers > 16) throw new NotSupportedException("Material textures and scene-light channels exceed the native sixteen-sampler stage limit.");
        var key = ((normal + 1) * 4 + specular + 1) * 2 + (masks ? 1 : 0);
        return ComposeLibrary(program, $"LightCompose{key}");
    }

    internal static byte[] ComposeGlyph(ShaderProgram program, bool lcd) => ComposeLibrary(program.Fragment ?? program, lcd ? "GlyphLCD" : "GlyphDecode", glyph: true, inputs: !lcd);

    private static byte[] ComposeLibrary(ShaderProgram program, string name, bool glyph = false, bool inputs = false)
    {
        program = program.Fragment ?? program;
        var original = new Module(program.NativeCode);
        var libraryBytes = BuiltInShaders.Read(name + ".frag.spv");
        var relocations = MemoryMarshal.Cast<byte, uint>(BuiltInShaders.Read(name + ".frag.reloc"));
        var libraryWords = MemoryMarshal.Cast<byte, uint>(libraryBytes.AsSpan()); var shift = original.Header[3];
        foreach (var wordIndex in relocations) libraryWords[(int)wordIndex] = checked(libraryWords[(int)wordIndex] + shift);
        var library = new Module(libraryBytes);
        var canvasBinding = Array.FindIndex(program.Textures, texture => texture.IsCanvasTexture);
        if (glyph && canvasBinding >= 0)
        {
            var sourceTypes = original.Instructions.Where(i => Op(i) is >= 19 and <= 33).ToDictionary(i => i[1]);
            var imageVariable = original.Instructions.First(i => Op(i) == 59 && i[3] == 0 &&
                Op(sourceTypes[sourceTypes[i[1]][3]]) is 25 or 27 &&
                original.Instructions.Any(d => Op(d) == 71 && d[1] == i[2] && d[2] == 33 && d[3] == (uint)canvasBinding));
            var imageType = sourceTypes[sourceTypes[imageVariable[1]][3]];
            if (Op(imageType) == 27) imageType = sourceTypes[imageType[2]];
            foreach (var type in library.Instructions.Where(i => Op(i) == 25)) type[4] = imageType[4];
        }
        if (original.Header[1] >= 0x10400)
            foreach (var instruction in library.Instructions)
            {
                if (Op(instruction) == 32 && instruction[2] == 2) instruction[2] = 12;
                if (Op(instruction) == 59 && instruction[3] == 2) instruction[3] = 12;
                if (Op(instruction) == 71 && instruction[2] == 3) instruction[2] = 2;
            }
        var next = checked(shift + library.Header[3]);
        var aliases = new Dictionary<uint, uint>(); var duplicateTypes = new HashSet<uint[]>();
        var typeKeys = new Dictionary<string, uint>(StringComparer.Ordinal);
        foreach (var instruction in original.Instructions) if (UniqueType(Op(instruction))) typeKeys.TryAdd(TypeKey(instruction), instruction[1]);
        var position = 5;
        foreach (var instruction in library.Instructions)
        {
            for (var i = 1; i < instruction.Length; i++) if (relocations.BinarySearch((uint)(position + i)) >= 0) instruction[i] = Alias(aliases, instruction[i]);
            if (UniqueType(Op(instruction)))
            {
                var id = instruction[1]; var signature = TypeKey(instruction);
                if (typeKeys.TryGetValue(signature, out var existing)) { aliases[id] = existing; duplicateTypes.Add(instruction); }
                else typeKeys.Add(signature, id);
            }
            position += instruction.Length;
        }
        // Remap forward uses as well as declarations, using generated ID positions rather than guessing literals.
        position = 5;
        foreach (var instruction in library.Instructions)
        {
            for (var i = 1; i < instruction.Length; i++) if (relocations.BinarySearch((uint)(position + i)) >= 0) instruction[i] = Alias(aliases, instruction[i]);
            position += instruction.Length;
        }
        var libEntry = library.Instructions.Single(i => Op(i) == 15)[2];
        var originalEntry = original.Instructions.Single(i => Op(i) == 15)[2];
        var libraryIO = library.Instructions.Where(i => Op(i) == 59 && i[3] is 1 or 3).ToDictionary(i => i[2], i => i);
        var libOutput = libraryIO.Single(p => p.Value[3] == 3).Key;
        var types = original.Instructions.Concat(library.Instructions).Where(i => Op(i) is >= 19 and <= 33).GroupBy(i => i[1]).ToDictionary(g => g.Key, g => g.First());
        uint Pointee(uint variable) => types[libraryIO[variable][1]][3];
        var colorType = Pointee(libOutput);
        var globals = new List<uint[]>(); var decorations = new List<uint[]>(); var interfaces = new List<uint>();
        uint Pointer(uint storage, uint value)
        {
            var found = types.Values.FirstOrDefault(i => Op(i) == 32 && i[2] == storage && i[3] == value);
            if (found is not null) return found[1];
            var id = next++; var instruction = Instruction(32, id, storage, value); types.Add(id, instruction); globals.Add(instruction); return id;
        }
        Value Input(uint type, uint decoration, uint value)
        {
            var existing = FindValue(original, types, 1, decoration, value);
            if (existing is not null) return existing;
            var id = next++; globals.Add(Instruction(59, Pointer(1, type), id, 1)); decorations.Add(Instruction(71, id, decoration, value)); interfaces.Add(id);
            return new(id, type, []);
        }
        var output = FindValue(original, types, 3, 30, 0) ?? throw new NotSupportedException("The material has no composed color output.");
        var arguments = new Dictionary<uint, Value> { [libOutput] = output };
        foreach (var pair in libraryIO)
        {
            if (pair.Value[3] != 1) continue;
            var location = library.Instructions.Single(i => Op(i) == 71 && i[1] == pair.Key && i[2] is 11 or 30);
            var input = Input(Pointee(pair.Key), location[2], location[3]); arguments.Add(pair.Key, input);
            foreach (var decoration in library.Instructions.Where(i => Op(i) == 71 && i[1] == pair.Key && i[2] is 13 or 14 or 16 or 17))
            {
                uint[] mapped = [decoration[0], input.Variable, .. decoration.AsSpan(2).ToArray()];
                if (!original.Instructions.Any(i => i.SequenceEqual(mapped)) && !decorations.Any(i => i.SequenceEqual(mapped))) decorations.Add(mapped);
            }
        }
        var indexType = types.Values.First(i => Op(i) == 21 && i[2] == 32 && i[3] == 0)[1];
        var reused = new HashSet<uint>(); var canvasSlot = Array.FindIndex(program.Textures, texture => texture.IsCanvasTexture); var reuseCanvas = glyph && canvasSlot >= 0;
        uint Binding(uint id) => original.Instructions.Single(i => Op(i) == 71 && i[1] == id && i[2] == 33)[3];
        uint PrimaryImage()
        {
            return original.Instructions.First(i => Op(i) == 59 && i[3] == 0 && Op(types[types[i[1]][3]]) is 25 or 27 && Binding(i[2]) == (uint)canvasSlot)[2];
        }
        var primary = reuseCanvas ? PrimaryImage() : 0;
        if (reuseCanvas)
        {
            var variable = library.Instructions.First(i => Op(i) == 59 && i[3] == 0 && Op(types[types[i[1]][3]]) == 27);
            aliases[variable[2]] = primary; reused.Add(primary);
            position = 5;
            foreach (var instruction in library.Instructions)
            {
                for (var i = 1; i < instruction.Length; i++) if (relocations.BinarySearch((uint)(position + i)) >= 0) instruction[i] = Alias(aliases, instruction[i]);
                position += instruction.Length;
            }
        }
        var extra = glyph && !reuseCanvas ? 1 : 0;
        if (glyph && program.Textures.Length + extra > 16) throw new NotSupportedException("Glyph sampling exceeds the sixteen-sampler stage limit.");
        var separateImages = original.Instructions.Any(i => Op(i) == 59 && i[3] == 0 && Op(types[types[i[1]][3]]) == 25);
        var splitSamplers = new Dictionary<uint, (uint Image, uint Sampler, uint SamplerType)>();
        if (separateImages)
        {
            var samplerType = types.Values.First(i => Op(i) == 26)[1];
            foreach (var variable in library.Instructions.TakeWhile(i => Op(i) != 54).Where(i => Op(i) == 59 && i[3] == 0))
            {
                var sampled = types[types[variable[1]][3]]; if (Op(sampled) != 27) continue;
                var imageType = sampled[2]; var sampler = reused.Contains(variable[2]) ? original.Instructions.First(i => Op(i) == 59 && i[3] == 0 && Op(types[types[i[1]][3]]) == 26 && Binding(i[2]) == (uint)canvasSlot)[2] : next++; variable[1] = Pointer(0, imageType);
                if (!reused.Contains(variable[2])) globals.Add(Instruction(59, Pointer(0, samplerType), sampler, 0));
                var binding = library.Instructions.Single(i => Op(i) == 71 && i[1] == variable[2] && i[2] == 33)[3];
                if (!reused.Contains(variable[2]))
                {
                    decorations.Add(Instruction(71, sampler, 34, 2));
                    decorations.Add(Instruction(71, sampler, 33, binding + (uint)program.Textures.Length));
                }
                splitSamplers.Add(variable[2], (imageType, sampler, samplerType));
                if (original.Header[1] >= 0x10400) interfaces.Add(sampler);
            }
        }
        var parameters = arguments.Keys.ToDictionary(id => id, _ => next++);
        var helperType = next++; globals.Add(Instruction(33, [helperType, colorType, .. arguments.Values.Select(value => value.Type)]));
        var constants = new Dictionary<uint, uint>();
        uint Constant(uint value)
        {
            if (constants.TryGetValue(value, out var existing)) return existing;
            var id = next++; globals.Add(Instruction(43, indexType, id, value)); constants.Add(value, id); return id;
        }
        uint Load(List<uint[]> body, Value value, out uint pointer)
        {
            pointer = value.Variable;
            if (value.Path.Length != 0)
            {
                pointer = next++; body.Add(Instruction(65, [Pointer(value == output ? 3u : 1u, value.Type), pointer, value.Variable, .. value.Path.Select(Constant)]));
            }
            var result = next++; body.Add(Instruction(61, value.Type, result, pointer)); return result;
        }
        var originalFunctions = new List<uint[]>(); var inEntry = false;
        var sourceColor = inputs ? FindValue(original, types, 1, 30, 0) : null;
        var colorPointers = new Dictionary<uint, uint[]>(); var colorStructures = new HashSet<uint>();
        var dynamicComponents = new Dictionary<uint, uint>();
        if (sourceColor is { Path.Length: 0 }) colorPointers.Add(sourceColor.Variable, []);
        var literalValues = original.Instructions.Where(i => Op(i) == 43).ToDictionary(i => i[2], i => i[3]);
        foreach (var instruction in original.Instructions.SkipWhile(i => Op(i) != 54))
        {
            if (Op(instruction) == 54) inEntry = instruction[2] == originalEntry;
            if (!inputs && inEntry && Op(instruction) == 253)
            {
                var color = Load(originalFunctions, output, out var outputPointer); var call = arguments.Skip(1).Select(pair => Load(originalFunctions, pair.Value, out _)).ToArray();
                var result = next++; originalFunctions.Add(Instruction(57, [colorType, result, libEntry, color, .. call]));
                originalFunctions.Add(Instruction(62, outputPointer, result));
            }
            if (inputs && sourceColor is not null)
            {
                if (Op(instruction) == 65 && instruction[3] == sourceColor.Variable)
                {
                    var path = instruction.AsSpan(4).ToArray().Select(id => literalValues.GetValueOrDefault(id, uint.MaxValue)).ToArray();
                    if (path.AsSpan().StartsWith(sourceColor.Path))
                    {
                        var componentPath = path.AsSpan(sourceColor.Path.Length).ToArray(); colorPointers.Add(instruction[2], componentPath);
                        if (componentPath is [uint.MaxValue]) dynamicComponents.Add(instruction[2], instruction[^1]);
                    }
                }
                if (Op(instruction) == 61 && instruction[3] == sourceColor.Variable && sourceColor.Path.Length != 0) colorStructures.Add(instruction[2]);
                uint[]? components = null;
                if (Op(instruction) == 61) colorPointers.TryGetValue(instruction[3], out components);
                else if (Op(instruction) == 81 && colorStructures.Contains(instruction[3]) && instruction.AsSpan(4).StartsWith(sourceColor.Path)) components = instruction.AsSpan(4 + sourceColor.Path.Length).ToArray();
                var colorLoad = components is not null;
                if (colorLoad)
                {
                    var result = instruction[2]; uint raw;
                    if (components!.Length == 0) { instruction[2] = next++; originalFunctions.Add(instruction); raw = instruction[2]; }
                    else raw = Load(originalFunctions, sourceColor, out _);
                    var call = arguments.Skip(1).Select(pair => Load(originalFunctions, pair.Value, out _)).ToArray();
                    var decoded = components.Length == 0 ? result : next++;
                    originalFunctions.Add(Instruction(57, [colorType, decoded, libEntry, raw, .. call]));
                    if (components.Length != 0)
                        originalFunctions.Add(Op(instruction) == 61 && dynamicComponents.TryGetValue(instruction[3], out var dynamicIndex)
                            ? Instruction(77, instruction[1], result, decoded, dynamicIndex)
                            : Instruction(81, [instruction[1], result, decoded, .. components]));
                    continue;
                }
            }
            originalFunctions.Add(instruction);
        }
        var libraryFunctions = new List<uint[]>(); var inHelper = false; uint returnValue = 0;
        var inputComponents = new Dictionary<uint, (uint Argument, uint[] Indices)>();
        var libraryLiterals = library.Instructions.Where(i => Op(i) == 43).ToDictionary(i => i[2], i => i[3]);
        foreach (var instruction in library.Instructions.SkipWhile(i => Op(i) != 54))
        {
            var opcode = Op(instruction);
            if (opcode == 54)
            {
                inHelper = instruction[2] == libEntry;
                if (inHelper)
                {
                    instruction[1] = colorType; instruction[4] = helperType; libraryFunctions.Add(instruction);
                    foreach (var pair in parameters) libraryFunctions.Add(Instruction(55, arguments[pair.Key].Type, pair.Value));
                    continue;
                }
            }
            if (opcode == 61 && splitSamplers.TryGetValue(instruction[3], out var split))
            {
                var image = next++; var sampler = next++;
                libraryFunctions.Add(Instruction(61, split.Image, image, instruction[3])); libraryFunctions.Add(Instruction(61, split.SamplerType, sampler, split.Sampler));
                libraryFunctions.Add(Instruction(86, instruction[1], instruction[2], image, sampler)); continue;
            }
            if (inHelper && opcode == 65 && parameters.TryGetValue(instruction[3], out var componentArgument))
            {
                inputComponents.Add(instruction[2], (componentArgument, instruction.AsSpan(4).ToArray().Select(id => libraryLiterals[id]).ToArray())); continue;
            }
            if (inHelper && opcode == 61 && inputComponents.TryGetValue(instruction[3], out var component))
            { libraryFunctions.Add(Instruction(81, [instruction[1], instruction[2], component.Argument, .. component.Indices])); continue; }
            if (inHelper && opcode == 61 && parameters.TryGetValue(instruction[3], out var argument)) { libraryFunctions.Add(Instruction(83, instruction[1], instruction[2], argument)); continue; }
            if (inHelper && opcode == 62 && instruction[1] == libOutput) { returnValue = instruction[2]; continue; }
            if (inHelper && opcode == 253)
            {
                if (returnValue == 0) throw new InvalidOperationException("The lighting function has no result.");
                libraryFunctions.Add(Instruction(254, returnValue)); continue;
            }
            libraryFunctions.Add(instruction);
        }
        if (extra != 0)
        {
            var storage = original.Instructions.Where(i => Op(i) == 59 && i[3] is 2 or 12 &&
                original.Instructions.Any(d => Op(d) == 71 && d[1] == i[2] && d[2] == 34 && d[3] == 2)).Select(i => i[2]).ToHashSet();
            foreach (var instruction in original.Instructions)
                if (Op(instruction) == 71 && instruction[2] == 33 && storage.Contains(instruction[1])) instruction[3] += (uint)extra;
        }
        var prefix = original.Instructions.TakeWhile(i => Op(i) != 54).ToList();
        foreach (var instruction in library.Instructions.TakeWhile(i => Op(i) != 54))
        {
            var opcode = Op(instruction);
            if (opcode is 14 or 15 or 16 || UniqueType(opcode) && duplicateTypes.Contains(instruction)) continue;
            if (opcode == 59 && (libraryIO.ContainsKey(instruction[2]) || reused.Contains(instruction[2])) || opcode is 5 or 71 && (libraryIO.ContainsKey(instruction[1]) || reused.Contains(instruction[1]))) continue;
            if (opcode == 17 && prefix.Any(i => i.SequenceEqual(instruction))) continue;
            if (opcode == 71 && instruction[2] == 33) instruction[3] += (uint)program.Textures.Length;
            prefix.Add(instruction);
        }
        if (original.Header[1] >= 0x10400)
            interfaces.AddRange(library.Instructions.TakeWhile(i => Op(i) != 54).Where(i => Op(i) == 59 && !libraryIO.ContainsKey(i[2])).Select(i => i[2]));
        prefix.AddRange(decorations); prefix.AddRange(globals);
        var entry = prefix.Single(i => Op(i) == 15); var entryEnd = StringEnd(entry, 3);
        var entryInterfaces = entry.AsSpan(entryEnd).ToArray().Concat(interfaces).Distinct().ToArray();
        prefix[prefix.IndexOf(entry)] = [((uint)(entryEnd + entryInterfaces.Length) << 16) | 15, .. entry.AsSpan(1, entryEnd - 1).ToArray(), .. entryInterfaces];
        var all = prefix.OrderBy(i => Category(Op(i))).ThenBy(i => Category(Op(i)) == 10 && Op(i) == 59 ? 1 : 0).Concat(originalFunctions).Concat(libraryFunctions).ToArray();
        var header = original.Header; header[3] = next;
        var resultWords = new uint[5 + all.Sum(i => i.Length)]; header.CopyTo(resultWords, 0); var at = 5;
        foreach (var instruction in all) { instruction.CopyTo(resultWords, at); at += instruction.Length; }
        return MemoryMarshal.AsBytes(resultWords.AsSpan()).ToArray();
    }

    private static Value? FindValue(Module module, Dictionary<uint, uint[]> types, uint storage, uint decoration, uint value)
    {
        var fields = module.Instructions.Where(i => Op(i) == 72 && i[3] == decoration && i.Length >= 5).ToDictionary(i => (i[1], i[2]), i => i[4]);
        Value? Walk(uint variable, uint type, List<uint> path, bool selected)
        {
            var definition = types[type]; var opcode = Op(definition);
            if (opcode == 28) { path.Add(0); var found = Walk(variable, definition[2], path, selected); path.RemoveAt(path.Count - 1); return found; }
            if (opcode == 30)
            {
                for (var member = 2; member < definition.Length; member++)
                {
                    path.Add((uint)member - 2); var found = Walk(variable, definition[member], path, selected || fields.TryGetValue((type, (uint)member - 2), out var field) && field == value); path.RemoveAt(path.Count - 1);
                    if (found is not null) return found;
                }
            }
            else if (selected) return new(variable, type, path.ToArray());
            return null;
        }
        foreach (var variable in module.Instructions.Where(i => Op(i) == 59 && i[3] == storage))
        {
            var selected = module.Instructions.Any(i => Op(i) == 71 && i[1] == variable[2] && i[2] == decoration && i.Length >= 4 && i[3] == value);
            var result = Walk(variable[2], types[variable[1]][3], [], selected); if (result is not null) return result;
        }
        return null;
    }
}
