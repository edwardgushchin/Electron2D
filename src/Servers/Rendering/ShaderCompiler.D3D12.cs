using SDL3;
using static Electron2D.MaterialLightingProgram;

namespace Electron2D;

internal static unsafe partial class ShaderCompiler
{
    internal static (byte[] Vertex, byte[] Fragment) LinkGraphicsStages(nint device, byte[] vertex, byte[] fragment)
    {
        if (SDL.GetGPUDeviceDriver(device) != "direct3d12")
            return (vertex, fragment);
        var vertexModule = new Module(vertex);
        var fragmentModule = new Module(fragment);
        var vertexEntry = vertexModule.Instructions.Single(i => Op(i) == 15);
        var fragmentEntry = fragmentModule.Instructions.Single(i => Op(i) == 15);
        var vertexNamesRemoved = RemoveShadowedEntryNames(vertexModule, vertexEntry[2]);
        var fragmentNamesRemoved = RemoveShadowedEntryNames(fragmentModule, fragmentEntry[2]);
        var outputs = StageLocations(vertexModule, vertexEntry, storage: 3);
        var inputs = StageLocations(fragmentModule, fragmentEntry, storage: 1);
        var interpolation = new Dictionary<uint, uint[][]>();
        foreach (var (location, output) in outputs)
        {
            if (!inputs.TryGetValue(location, out var input))
                continue;
            var required = input.Decorations.Where(IsInterpolation).ToArray();
            var authored = output.Decorations.Where(IsInterpolation).Select(i => i[2]);
            if (!authored.ToHashSet().SetEquals(required.Select(i => i[2])))
                interpolation.Add(output.Variable[2], required);
        }
        if (interpolation.Count != 0)
            NormalizeD3D12Interpolation(vertexModule, interpolation);
        if (vertexNamesRemoved || interpolation.Count != 0)
            vertex = vertexModule.Write();

        var missing = outputs.Where(output => !inputs.ContainsKey(output.Key)).ToArray();
        if (missing.Length == 0)
            return (vertex, fragmentNamesRemoved ? fragmentModule.Write() : fragment);

        var types = vertexModule.Instructions.Where(i => Op(i) is 21 or 22 or 23 or 32).ToDictionary(i => i[1]);
        var declarations = fragmentModule.Instructions.Where(i => Op(i) is 21 or 22 or 23 or 32).ToList();
        var addedTypes = new List<uint[]>();
        var addedInputs = new List<uint[]>();
        var decorations = new List<uint[]>();
        var interfaceIds = new List<uint>();

        foreach (var (_, output) in missing)
        {
            var id = fragmentModule.Header[3]++;
            var type = CopyType(types[output.Variable[1]][3]);
            var pointer = DeclareType(32, 1, type);
            addedInputs.Add(Instruction(59, pointer, id, 1));
            interfaceIds.Add(id);
            foreach (var decoration in output.Decorations)
            {
                if (IsInterpolation(decoration) || decoration[2] is 30 or 31)
                    decorations.Add(CopyDecoration(decoration, id));
            }
        }

        // DXIL packs registers from declared signatures. Preserve all producer slots even
        // when the pixel shader reads only a sparse subset of its vertex outputs.
        uint[] entry = [.. fragmentEntry, .. interfaceIds];
        entry[0] = ((uint)entry.Length << 16) | 15;
        var instructions = fragmentModule.Instructions;
        instructions[instructions.IndexOf(fragmentEntry)] = entry;
        instructions.InsertRange(instructions.FindIndex(i => Category(Op(i)) == 10), decorations);
        var firstFunction = instructions.FindIndex(i => Op(i) == 54);
        instructions.InsertRange(firstFunction, addedTypes);
        instructions.InsertRange(firstFunction + addedTypes.Count, addedInputs);
        return (vertex, fragmentModule.Write());

        uint CopyType(uint id)
        {
            if (!types.TryGetValue(id, out var source))
                throw new NotSupportedException("Direct3D 12 stage linkage requires scalar or vector varyings.");
            return Op(source) switch
            {
                21 or 22 => DeclareType(Op(source), source[2..]),
                23 => DeclareType(23, CopyType(source[2]), source[3]),
                _ => throw new NotSupportedException("Direct3D 12 stage linkage requires scalar or vector varyings.")
            };
        }

        uint DeclareType(uint opcode, params uint[] operands)
        {
            foreach (var type in declarations)
                if (Op(type) == opcode && type.AsSpan(2).SequenceEqual(operands))
                    return type[1];
            var id = fragmentModule.Header[3]++;
            uint[] instruction = [((uint)(operands.Length + 2) << 16) | opcode, id, .. operands];
            declarations.Add(instruction);
            addedTypes.Add(instruction);
            return id;
        }
    }

    private static bool IsInterpolation(uint[] decoration) => decoration[2] is 13 or 14 or 16 or 17;

    private static bool RemoveShadowedEntryNames(Module module, uint entry)
    {
        // Canvas wrappers can turn the authored main into a helper. DXC reserves
        // that name for the generated entry, regardless of the helper's parameters.
        var functions = module.Instructions.Where(i => Op(i) == 54 && i[2] != entry).Select(i => i[2]).ToHashSet();
        const uint main = 0x6e69616d; // The first little-endian word of the SPIR-V string "main".
        return module.Instructions.RemoveAll(i => Op(i) == 5 && i is [_, _, main, 0] && functions.Contains(i[1])) != 0;
    }

    private static uint[] CopyDecoration(uint[] decoration, uint target)
    {
        var copy = (uint[])decoration.Clone();
        copy[1] = target;
        return copy;
    }

    private static void NormalizeD3D12Interpolation(Module module, Dictionary<uint, uint[][]> interpolation)
    {
        // D3D12 packs compatible interpolation modes together. The consumer determines
        // interpolation, so use its qualifiers on the native vertex signature too.
        var instructions = module.Instructions;
        var grouped = instructions.Where(i => Op(i) == 71).ToLookup(i => i[1]);
        var additions = new List<uint[]>();
        for (var index = instructions.Count - 1; index >= 0; index--)
        {
            var instruction = instructions[index];
            if (Op(instruction) == 71 && interpolation.ContainsKey(instruction[1]) && IsInterpolation(instruction))
                instructions.RemoveAt(index);
            else if (Op(instruction) == 74 && instruction.Skip(2).Any(interpolation.ContainsKey))
            {
                var retained = new List<uint> { instruction[1] };
                foreach (var target in instruction.AsSpan(2))
                {
                    if (!interpolation.ContainsKey(target))
                        retained.Add(target);
                    else
                        foreach (var decoration in grouped[instruction[1]])
                            if (!IsInterpolation(decoration))
                                additions.Add(CopyDecoration(decoration, target));
                }
                if (retained.Count == 1)
                    instructions.RemoveAt(index);
                else
                    instructions[index] = Instruction(74, retained.ToArray());
            }
        }
        foreach (var (target, required) in interpolation)
            foreach (var decoration in required)
                additions.Add(CopyDecoration(decoration, target));
        instructions.InsertRange(instructions.FindIndex(i => Category(Op(i)) == 10), additions);
    }

    private static Dictionary<uint, (uint[] Variable, List<uint[]> Decorations)> StageLocations(Module module, uint[] entry, uint storage)
    {
        var interfaceIds = entry[StringEnd(entry, 3)..].ToHashSet();
        var annotations = module.Instructions.Where(i => Op(i) == 71).ToLookup(i => i[1]);
        var decorations = interfaceIds.ToDictionary(id => id, id => annotations[id].ToList());
        foreach (var group in module.Instructions.Where(i => Op(i) == 74))
            foreach (var target in group.AsSpan(2))
                if (decorations.TryGetValue(target, out var values))
                    values.AddRange(annotations[group[1]]);
        return module.Instructions
            .Where(i => Op(i) == 59 && i[3] == storage && interfaceIds.Contains(i[2]))
            .Select(i => (Variable: i, Decorations: decorations[i[2]], Location: decorations[i[2]].FirstOrDefault(d => d[2] == 30)))
            .Where(i => i.Location != null)
            .ToDictionary(i => i.Location![3], i => (i.Variable, i.Decorations));
    }
}
