using static Electron2D.MaterialLightingProgram;

namespace Electron2D;

internal static partial class CanvasSamplerArrays
{
    internal readonly record struct DeviceSource(uint Set, int Binding, RenderingDevice.UniformType Type);
    internal readonly record struct DeviceShape(bool Array, bool Depth, int PixelType);
    internal readonly record struct DevicePair(uint Image, uint Sampler, uint ImageIndex, uint SamplerIndex, bool UsesSampler);
    internal sealed record DevicePlan(uint Set, uint Stride, Dictionary<uint, uint> Values, Dictionary<uint, DeviceSource> Sources, Dictionary<uint, DeviceShape> Shapes, HashSet<(uint Image, uint Sampler)> Comparisons, DevicePair[] Pairs);

    internal static (byte[] Code, DevicePlan? Plan) LowerDevice(byte[] code)
    {
        var module = new Module(code); var source = module.Instructions;
        var types = source.Where(i => Op(i) is >= 19 and <= 39).ToDictionary(i => i[1]);
        bool Opaque(uint type) => types.TryGetValue(type, out var i) && (Op(i) is 26 or 27 || Op(i) == 25 && i[7] == 1 || Op(i) == 28 && Opaque(i[2]) || Op(i) == 32 && i[2] == 0 && Opaque(i[3]));
        var resources = source.Where(i => Op(i) == 59 && i[3] == 0 && Opaque(i[1])).ToArray();
        uint Element(uint[] variable) => types[variable[1]][3];
        var images = resources.Where(i => Op(types[Element(i)]) is 25 or 27).ToArray();
        if (images.Length == 0) return (code, null);
        var locations = source.Where(i => Op(i) == 71 && i.Length == 4 && i[2] is 33 or 34).GroupBy(i => i[1]).ToDictionary(g => g.Key, g => g.ToDictionary(i => i[2], i => i[3]));
        DeviceSource Location(uint[] variable, RenderingDevice.UniformType type)
        {
            if (!locations.TryGetValue(variable[2], out var fields) || !fields.TryGetValue(34, out var set) || !fields.TryGetValue(33, out var binding) || binding > int.MaxValue) throw new ArgumentException("A sampled resource needs a valid logical set and binding.", nameof(code));
            return new(set, (int)binding, type);
        }
        var samplers = resources.Where(i => Op(types[Element(i)]) == 26).ToArray();
        var origins = new Dictionary<uint, DeviceSource>();
        foreach (var image in images)
        {
            var origin = Location(image, RenderingDevice.UniformType.Texture);
            var paired = samplers.Any(s => Location(s, RenderingDevice.UniformType.Texture) == origin);
            origins.Add(image[2], origin with { Type = paired || Op(types[Element(image)]) == 27 ? RenderingDevice.UniformType.SamplerWithTexture : RenderingDevice.UniformType.Texture });
        }
        foreach (var sampler in samplers)
        {
            var origin = Location(sampler, RenderingDevice.UniformType.Texture);
            origins.Add(sampler[2], origin with { Type = images.Any(i => origins[i[2]].Set == origin.Set && origins[i[2]].Binding == origin.Binding) ? RenderingDevice.UniformType.SamplerWithTexture : RenderingDevice.UniformType.Sampler });
        }
        var ids = resources.Select(i => i[2]).ToHashSet(); var combined = images.Where(i => Op(types[Element(i)]) == 27).Select(i => i[2]).ToHashSet();
        HashSet<(uint Image, uint Sampler)> Normalize(HashSet<(uint Image, uint Sampler)> input) => input.Select(p => (p.Image, p.Sampler == 0 && combined.Contains(p.Image) ? p.Image : p.Sampler)).ToHashSet();
        var pairs = Normalize(FindPairs(source, ids, Opaque, PairUse.Sampling, true));
        var sampling = Normalize(FindPairs(source, ids, Opaque, PairUse.Sampling));
        var comparisons = Normalize(FindPairs(source, ids, Opaque, PairUse.Comparison));
        var ordinary = Normalize(FindPairs(source, ids, Opaque, PairUse.Ordinary));
        if (comparisons.Overlaps(ordinary)) throw new NotSupportedException("A sampled pair cannot require both comparison and ordinary filtering; bind separate samplers.");
        bool UnknownDepth(uint[] image) { var type = types[Element(image)]; if (Op(type) == 27) type = types[type[2]]; return type[4] == 2; }
        if (pairs.All(p => p.Sampler != 0 && origins[p.Image] == origins[p.Sampler]) && images.All(i => origins[i[2]].Type == RenderingDevice.UniformType.SamplerWithTexture && !UnknownDepth(i))) return (code, null);
        if (resources.Any(i => Op(types[Element(i)]) == 28)) throw new NotSupportedException("Device descriptor arrays require their typed resource-array integration.");
        var actual = pairs.Where(p => p.Sampler != 0).ToHashSet();
        foreach (var query in pairs.Where(p => p.Sampler == 0)) if (!actual.Any(p => p.Image == query.Image)) actual.Add(query);
        if (actual.Count > 16) throw new NotSupportedException("Possible image/sampler pairs exceed the sixteen native stage slots.");
        var imageIndices = images.Select((r, i) => (r[2], Index: (uint)i)).ToDictionary(p => p.Item1, p => p.Index);
        var samplerIndices = resources.Where(r => Op(types[Element(r)]) is 26 or 27).Select((r, i) => (r[2], Index: (uint)i)).ToDictionary(p => p.Item1, p => p.Index);
        var stride = (uint)Math.Max(1, samplerIndices.Count + 1);
        if ((ulong)imageIndices.Count * stride > uint.MaxValue) throw new NotSupportedException("Sampled resource selectors exceed their native word range.");
        var values = new Dictionary<uint, uint>();
        foreach (var resource in resources)
        {
            var id = resource[2]; values.Add(id, Op(types[Element(resource)]) switch { 26 => samplerIndices[id], 27 => imageIndices[id] * stride + samplerIndices[id], _ => imageIndices[id] });
        }
        var occupied = locations.Values.Where(d => d.ContainsKey(34)).Select(d => d[34]).ToHashSet(); uint nativeSet = 0; while (occupied.Contains(nativeSet)) nativeSet++;
        var shapes = new Dictionary<uint, DeviceShape>();
        foreach (var image in images)
        {
            var type = types[Element(image)]; if (Op(type) == 27) type = types[type[2]];
            var scalar = types[type[2]];
            if (scalar.Length < 3 || scalar[2] != 32 || Op(scalar) is not (21 or 22)) throw new NotSupportedException("Sampled device images require 32-bit scalar components.");
            shapes.Add(image[2], new(type[5] != 0, type[4] == 1 || comparisons.Any(p => p.Image == image[2]), Op(scalar) == 22 ? 0 : scalar[3] == 1 ? 1 : 2));
        }
        var plan = new DevicePlan(nativeSet, stride, values, origins, shapes, comparisons, actual.OrderBy(p => imageIndices[p.Image]).ThenBy(p => p.Sampler).Select(p => new DevicePair(p.Image, p.Sampler, imageIndices[p.Image], p.Sampler == 0 ? stride - 1 : samplerIndices[p.Sampler], sampling.Contains(p))).ToArray());
        return (Lower(code, [], device: plan), plan);
    }
}
