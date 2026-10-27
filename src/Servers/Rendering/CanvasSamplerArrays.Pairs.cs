using static Electron2D.MaterialLightingProgram;

namespace Electron2D;

internal static partial class CanvasSamplerArrays
{
    private enum PairUse { Construction, Sampling, Comparison, Ordinary, UnnormalizedUnsafe }
    // Keep symbolic pairs through reusable helpers, returns and control flow until the entry point resolves them.
    private static HashSet<(uint Image, uint Sampler)> FindPairs(List<uint[]> source, HashSet<uint> resources, Func<uint, bool> opaque, PairUse use = PairUse.Construction, bool queries = false)
    {
        var entries = source.Where(i => Op(i) == 15).Take(2).ToArray();
        if (entries.Length != 1) throw new NotSupportedException("One sampled-resource entry point is required.");
        var entry = entries[0][2]; var bodies = new Dictionary<uint, List<uint[]>>(); uint function = 0;
        foreach (var instruction in source)
        {
            if (Op(instruction) == 54) { function = instruction[2]; bodies.Add(function, []); }
            if (function != 0) bodies[function].Add(instruction);
            if (Op(instruction) == 56) function = 0;
        }
        var parameters = bodies.ToDictionary(p => p.Key, p => p.Value.Where(i => Op(i) == 55).Select(i => i[2]).ToArray());
        var origins = resources.ToDictionary(id => id, id => new HashSet<(uint Image, uint Sampler)> { (id, 0) });
        foreach (var parameter in bodies.Values.SelectMany(i => i).Where(i => Op(i) == 55 && opaque(i[1]))) origins.Add(parameter[2], [(parameter[2], 0)]);
        var returns = bodies.Keys.ToDictionary(id => id, _ => new HashSet<(uint Image, uint Sampler)>());
        var constraints = bodies.Keys.ToDictionary(id => id, _ => new HashSet<(uint Image, uint Sampler)>());
        HashSet<(uint Image, uint Sampler)> empty = [];
        HashSet<(uint Image, uint Sampler)> Read(uint id) => origins.GetValueOrDefault(id) ?? empty;
        var changed = true;
        bool Constraint(uint owner, (uint Image, uint Sampler) pair)
        {
            var added = constraints[owner].Add(pair);
            if (queries && owner == entry && constraints[owner].Count > 32) throw new NotSupportedException("Sampled pair/query requirements exceed the sixteen native stage slots.");
            return added;
        }
        while (changed)
        {
            changed = false;
            void Add(HashSet<(uint Image, uint Sampler)> destination, IEnumerable<(uint Image, uint Sampler)> values)
            {
                var count = destination.Count; destination.UnionWith(values); changed |= destination.Count != count;
            }
            void Copy(uint id, IEnumerable<(uint Image, uint Sampler)> values)
            {
                if (!origins.TryGetValue(id, out var destination)) origins.Add(id, destination = []);
                Add(destination, values);
            }
            foreach (var body in bodies)
                foreach (var instruction in body.Value)
                {
                    var op = Op(instruction);
                    if (op is 61 or 65 or 66 or 83 && opaque(instruction[1])) Copy(instruction[2], Read(instruction[3]));
                    if (op == 100 && opaque(instruction[1])) Copy(instruction[2], Read(instruction[3]).Select(p => (p.Image, 0u)));
                    if (op == 169 && opaque(instruction[1])) { Copy(instruction[2], Read(instruction[4])); Copy(instruction[2], Read(instruction[5])); }
                    if (op == 245 && opaque(instruction[1])) for (var i = 3; i < instruction.Length; i += 2) Copy(instruction[2], Read(instruction[i]));
                    if (op == 254) Add(returns[body.Key], Read(instruction[1]));
                    if (op == 86)
                    {
                        var produced = Read(instruction[3]).SelectMany(image => Read(instruction[4]).Select(sampler => (Image: image.Image, Sampler: sampler.Image))).ToArray();
                        Copy(instruction[2], produced);
                        if (use == PairUse.Construction) foreach (var pair in produced) changed |= Constraint(body.Key, pair);
                    }
                    var comparison = op is 89 or 90 or 93 or 94 or 97 or 307 or 308 or 311 or 312 or 315;
                    var ordinary = op is 87 or 88 or 91 or 92 or 96 or 105 or 305 or 306 or 309 or 310 or 314;
                    if (use == PairUse.Sampling && (comparison || ordinary) || use == PairUse.Comparison && comparison || use == PairUse.Ordinary && ordinary || use == PairUse.UnnormalizedUnsafe && UnnormalizedUnsafe(instruction))
                        foreach (var pair in Read(instruction[3])) changed |= Constraint(body.Key, pair);
                    if (queries && op is 95 or 101 or 102 or 103 or 104 or 106 or 107)
                        foreach (var image in Read(instruction[3])) changed |= Constraint(body.Key, (image.Image, 0));
                    if (op == 57)
                    {
                        var called = instruction[3];
                        HashSet<(uint Image, uint Sampler)> Resolve(uint symbol)
                        {
                            var parameter = Array.IndexOf(parameters[called], symbol);
                            return Read(parameter < 0 ? symbol : instruction[4 + parameter]);
                        }
                        IEnumerable<(uint Image, uint Sampler)> Substitute((uint Image, uint Sampler) symbol) => symbol.Sampler == 0 ? Resolve(symbol.Image) : Resolve(symbol.Image).SelectMany(image => Resolve(symbol.Sampler).Select(sampler => (image.Image, sampler.Image)));
                        if (opaque(instruction[1])) foreach (var symbol in returns[called].ToArray()) Copy(instruction[2], Substitute(symbol));
                        foreach (var symbol in constraints[called].ToArray()) foreach (var pair in Substitute(symbol)) changed |= Constraint(body.Key, pair);
                    }
                }
        }
        return constraints[entry];
    }
    private static void ValidatePairs(List<uint[]> source, HashSet<uint> resources, Dictionary<uint, Dictionary<uint, uint>> decorations, Func<uint, bool> opaque)
    {
        foreach (var pair in FindPairs(source, resources, opaque))
            if (!decorations.TryGetValue(pair.Image, out var image) || !decorations.TryGetValue(pair.Sampler, out var sampler) ||
                !image.TryGetValue(33, out var imageBinding) || !sampler.TryGetValue(33, out var samplerBinding) || imageBinding != samplerBinding)
                throw new NotSupportedException("An image must be sampled with a sampler at the same binding.");
    }
}
