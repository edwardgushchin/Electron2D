using static Electron2D.MaterialLightingProgram;

namespace Electron2D;

internal static partial class CanvasSamplerArrays
{
    // Propagate binding equalities through call arguments instead of rejecting reusable sampler helpers.
    private static void ValidatePairs(List<uint[]> source, HashSet<uint> resources, Dictionary<uint, Dictionary<uint, uint>> decorations, Func<uint, bool> opaque)
    {
        if (!source.Any(i => Op(i) == 86)) return;
        var bodies = new Dictionary<uint, List<uint[]>>(); uint function = 0;
        foreach (var instruction in source)
        {
            if (Op(instruction) == 54) { function = instruction[2]; bodies.Add(function, []); }
            if (function != 0) bodies[function].Add(instruction);
            if (Op(instruction) == 56) function = 0;
        }
        var parameters = bodies.ToDictionary(p => p.Key, p => p.Value.Where(i => Op(i) == 55).Select(i => i[2]).ToArray());
        var origins = resources.ToDictionary(id => id, id => new HashSet<uint> { id });
        foreach (var parameter in bodies.Values.SelectMany(i => i).Where(i => Op(i) == 55 && opaque(i[1]))) origins.Add(parameter[2], [parameter[2]]);
        var returns = bodies.Keys.ToDictionary(id => id, _ => new HashSet<uint>());
        var constraints = bodies.Keys.ToDictionary(id => id, _ => new HashSet<(uint Image, uint Sampler)>());
        HashSet<uint> empty = [];
        HashSet<uint> Read(uint id) => origins.GetValueOrDefault(id) ?? empty;
        var changed = true;
        while (changed)
        {
            changed = false;
            void Add(HashSet<uint> destination, IEnumerable<uint> values)
            {
                var count = destination.Count; destination.UnionWith(values); changed |= destination.Count != count;
            }
            void Copy(uint id, IEnumerable<uint> values)
            {
                if (!origins.TryGetValue(id, out var destination)) origins.Add(id, destination = []);
                Add(destination, values);
            }
            foreach (var body in bodies)
                foreach (var instruction in body.Value)
                {
                    var op = Op(instruction);
                    if (op is 61 or 65 or 66 or 83 or 86 or 100 && opaque(instruction[1])) Copy(instruction[2], Read(instruction[3]));
                    if (op == 169 && opaque(instruction[1])) { Copy(instruction[2], Read(instruction[4])); Copy(instruction[2], Read(instruction[5])); }
                    if (op == 245 && opaque(instruction[1]))
                        for (var i = 3; i < instruction.Length; i += 2) Copy(instruction[2], Read(instruction[i]));
                    if (op == 254) Add(returns[body.Key], Read(instruction[1]));
                    if (op == 86)
                        foreach (var image in Read(instruction[3]))
                            foreach (var sampler in Read(instruction[4])) changed |= constraints[body.Key].Add((image, sampler));
                    if (op == 57)
                    {
                        var called = instruction[3];
                        HashSet<uint> Substitute(uint symbol)
                        {
                            var parameter = Array.IndexOf(parameters[called], symbol);
                            return parameter < 0 ? Read(symbol) : Read(instruction[4 + parameter]);
                        }
                        if (opaque(instruction[1])) foreach (var symbol in returns[called]) Copy(instruction[2], Substitute(symbol));
                        foreach (var pair in constraints[called].ToArray())
                            foreach (var image in Substitute(pair.Image))
                                foreach (var sampler in Substitute(pair.Sampler)) changed |= constraints[body.Key].Add((image, sampler));
                    }
                }
        }
        var entry = source.Single(i => Op(i) == 15)[2];
        foreach (var pair in constraints[entry])
            if (!decorations.TryGetValue(pair.Image, out var image) || !decorations.TryGetValue(pair.Sampler, out var sampler) ||
                !image.TryGetValue(33, out var imageBinding) || !sampler.TryGetValue(33, out var samplerBinding) || imageBinding != samplerBinding)
                throw new NotSupportedException("An image must be sampled with a sampler at the same binding.");
    }
}
