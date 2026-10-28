using static Electron2D.MaterialLightingProgram;

namespace Electron2D;

internal static partial class CanvasSamplerArrays
{
    private static bool UnnormalizedUnsafe(uint[] instruction)
    {
        var op = Op(instruction);
        if (op is 87 or 89 or 90 or 91 or 92 or 93 or 94 or 97 or >= 305 and <= 312 and not 306 or 315) return true;
        var mask = op is 88 or 306 ? 5 : op is 96 or 314 ? 6 : -1;
        // Vulkan allows gradients and gather; image bias/offset operands require normalized coordinates.
        return mask >= 0 && instruction.Length > mask && (instruction[mask] & 0x10039u) != 0;
    }

    internal static Dictionary<(uint Set, int Binding), bool> CoordinateSamplingRequirements(byte[] code)
    {
        var module = new Module(code); var source = module.Instructions;
        var types = source.Where(i => Op(i) is >= 19 and < 39).ToDictionary(i => i[1]);
        bool Opaque(uint type) => types.TryGetValue(type, out var i) && (Op(i) is 26 or 27 || Op(i) == 25 && i[7] == 1 || Op(i) is 28 or 29 && Opaque(i[2]) || Op(i) == 32 && i[2] == 0 && Opaque(i[3]));
        var resources = source.Where(i => Op(i) == 59 && i[3] == 0 && Opaque(i[1])).Select(i => i[2]).ToHashSet();
        if (resources.Count == 0) return [];
        var sampling = FindPairs(source, resources, Opaque, PairUse.Sampling);
        var unsafePairs = FindPairs(source, resources, Opaque, PairUse.UnnormalizedUnsafe);
        var locations = source.Where(i => Op(i) == 71 && i.Length == 4 && i[2] is 33 or 34).GroupBy(i => i[1]).ToDictionary(g => g.Key, g => g.ToDictionary(i => i[2], i => i[3]));
        foreach (var group in source.Where(i => Op(i) == 74))
            if (locations.TryGetValue(group[1], out var values))
                foreach (var target in group.AsSpan(2))
                {
                    if (!locations.TryGetValue(target, out var own)) locations.Add(target, own = []);
                    foreach (var value in values) own[value.Key] = value.Value;
                }
        var result = new Dictionary<(uint Set, int Binding), bool>();
        foreach (var pair in sampling)
            foreach (var id in new[] { pair.Image, pair.Sampler })
                if (locations.TryGetValue(id, out var location) && location.TryGetValue(34, out var set) && location.TryGetValue(33, out var slot)) result[(set, checked((int)slot))] = result.GetValueOrDefault((set, checked((int)slot))) || unsafePairs.Contains(pair);
        return result;
    }
}
