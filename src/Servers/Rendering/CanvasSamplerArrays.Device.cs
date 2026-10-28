using static Electron2D.MaterialLightingProgram;

namespace Electron2D;

internal static partial class CanvasSamplerArrays
{
    internal readonly record struct DeviceSource(uint Set, int Binding, RenderingDevice.UniformType Type, int Count = 1, int Element = 0, bool Dynamic = false);
    internal readonly record struct DeviceShape(bool Array, bool Depth, int PixelType);
    internal readonly record struct DevicePair(uint Image, uint Sampler, uint ImageIndex, uint SamplerIndex, bool UsesSampler, int ImageElement = 0, int SamplerElement = 0);
    internal sealed record DevicePlan(uint Set, uint Stride, uint ImageCount, Dictionary<uint, uint> Values, Dictionary<uint, DeviceSource> Sources, Dictionary<uint, DeviceShape> Shapes, HashSet<(uint Image, uint Sampler)> Comparisons, DevicePair[] Pairs, uint[]? Swizzles = null);

    internal static (byte[] Code, DevicePlan? Plan) LowerDevice(byte[] code, bool symbolic = false, IReadOnlyDictionary<(uint Set, int Binding, int Element), uint>? swizzles = null)
    {
        var module = new Module(code); var source = module.Instructions;
        var types = source.Where(i => Op(i) is >= 19 and < 39).ToDictionary(i => i[1]);
        bool Opaque(uint type) => types.TryGetValue(type, out var i) && (Op(i) is 26 or 27 || Op(i) == 25 && i[7] == 1 || Op(i) is 28 or 29 && Opaque(i[2]) || Op(i) == 32 && i[2] == 0 && Opaque(i[3]));
        var resources = source.Where(i => Op(i) == 59 && i[3] == 0 && Opaque(i[1])).ToArray();
        var lengths = source.Where(i => Op(i) == 43 && i.Length == 4).ToDictionary(i => i[2], i => i[3]);
        (uint Element, int Count, bool Dynamic) Shape(uint[] variable) => DescriptorShape(variable[1], types, lengths, symbolic);
        uint Element(uint[] variable) => Shape(variable).Element;
        var images = resources.Where(i => Op(types[Element(i)]) is 25 or 27).ToArray();
        if (images.Length == 0) return (code, null);
        var locations = source.Where(i => Op(i) == 71 && i.Length == 4 && i[2] is 33 or 34).GroupBy(i => i[1]).ToDictionary(g => g.Key, g => g.ToDictionary(i => i[2], i => i[3]));
        DeviceSource Location(uint[] variable, RenderingDevice.UniformType type)
        {
            if (!locations.TryGetValue(variable[2], out var fields) || !fields.TryGetValue(34, out var set) || !fields.TryGetValue(33, out var binding) || binding > int.MaxValue) throw new ArgumentException("A sampled resource needs a valid logical set and binding.", nameof(code));
            var shape = Shape(variable); return new(set, (int)binding, type, shape.Count, Dynamic: shape.Dynamic);
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
        if ((swizzles is null || swizzles.Values.All(v => v == 0)) && resources.All(i => Op(types[types[i[1]][3]]) is not (28 or 29)) && pairs.All(p => p.Sampler != 0 && origins[p.Image] == origins[p.Sampler]) && images.All(i => origins[i[2]].Type == RenderingDevice.UniformType.SamplerWithTexture && !UnknownDepth(i))) return (code, null);
        var actual = pairs.Where(p => p.Sampler != 0).ToHashSet();
        foreach (var query in pairs.Where(p => p.Sampler == 0)) if (!actual.Any(p => p.Image == query.Image)) actual.Add(query);
        var imageIndices = new Dictionary<uint, uint>(); uint imageTotal = 0;
        foreach (var resource in images) { imageIndices.Add(resource[2], imageTotal); imageTotal += (uint)Shape(resource).Count; }
        var samplerIndices = new Dictionary<uint, uint>(); uint samplerTotal = 0;
        foreach (var resource in resources.Where(r => Op(types[Element(r)]) is 26 or 27)) { samplerIndices.Add(resource[2], samplerTotal); samplerTotal += (uint)Shape(resource).Count; }
        var stride = Math.Max(1u, samplerTotal + 1);
        if ((ulong)imageTotal * stride > uint.MaxValue) throw new NotSupportedException("Sampled resource selectors exceed their native word range.");
        var values = new Dictionary<uint, uint>();
        foreach (var resource in resources)
        {
            var id = resource[2]; values.Add(id, Op(types[Element(resource)]) switch { 26 => samplerIndices[id], 27 => imageIndices[id] * stride + samplerIndices[id], _ => imageIndices[id] });
        }
        var expanded = new List<DevicePair>();
        foreach (var pair in actual.OrderBy(p => imageIndices[p.Image]).ThenBy(p => p.Sampler))
        {
            var imageCount = origins[pair.Image].Count; var samplerCount = pair.Sampler == 0 ? 1 : origins[pair.Sampler].Count;
            for (var imageElement = 0; imageElement < imageCount; imageElement++)
                for (var samplerElement = 0; samplerElement < samplerCount; samplerElement++)
                {
                    if (combined.Contains(pair.Image) && pair.Image == pair.Sampler && imageElement != samplerElement) continue;
                    expanded.Add(new(pair.Image, pair.Sampler, imageIndices[pair.Image] + (uint)imageElement, pair.Sampler == 0 ? stride - 1 : samplerIndices[pair.Sampler] + (uint)samplerElement, sampling.Contains(pair), imageElement, samplerElement));
                    if (expanded.Count > 16) throw new NotSupportedException("Possible image/sampler pairs exceed the sixteen native stage slots.");
                }
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
        var plan = new DevicePlan(nativeSet, stride, imageTotal, values, origins, shapes, comparisons, expanded.ToArray(), swizzles is null ? null : expanded.Select(p => { var origin = origins[p.Image]; return swizzles.GetValueOrDefault((origin.Set, origin.Binding, p.ImageElement)); }).ToArray());
        return (Lower(code, [], device: plan), plan);
    }
}
