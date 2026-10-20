using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private sealed record SamplerLink(Binding Native, CanvasSamplerArrays.DeviceSource Image, CanvasSamplerArrays.DeviceSource? Sampler);
    private RenderHandle? _fetchSampler;
    private static (Binding[] Layout, SamplerLink[] Links) RestoreSamplerLayout(List<Binding> layout, CanvasSamplerArrays.DevicePlan? plan, ShaderStage stage)
    {
        if (plan is null) return (layout.ToArray(), []);
        var links = new List<SamplerLink>();
        foreach (var binding in layout.Where(b => b.Set == plan.Set))
        {
            var pair = plan.Pairs[binding.Slot];
            links.Add(new(binding with { Depth = plan.Comparisons.Contains((pair.Image, pair.Sampler)) }, plan.Sources[pair.Image], !pair.UsesSampler || pair.Sampler == 0 ? null : plan.Sources[pair.Sampler]));
        }
        layout.RemoveAll(b => b.Set == plan.Set);
        foreach (var group in plan.Sources.GroupBy(p => p.Value))
        {
            var origin = group.Key; var image = group.FirstOrDefault(p => plan.Shapes.ContainsKey(p.Key)); var shape = plan.Shapes.GetValueOrDefault(image.Key);
            if (layout.Any(b => b.Set == origin.Set && b.Slot == origin.Binding)) throw new ArgumentException("Duplicate logical sampled resource binding.");
            layout.Add(new(origin.Set, origin.Binding, origin.Type, -1, shape.Array, PixelType: shape.PixelType, Stage: stage, Depth: shape.Depth));
        }
        return (layout.ToArray(), links.ToArray());
    }
    private static void NormalizeSamplerSources(Binding[] layout, SamplerLink[] links)
    {
        var requirements = new Dictionary<(uint Set, int Slot), bool>();
        void Require(uint set, int slot, bool comparison)
        {
            if (requirements.TryGetValue((set, slot), out var previous) && previous != comparison) throw new ArgumentException("A shared sampler has inconsistent comparison modes.");
            requirements[(set, slot)] = comparison;
        }
        foreach (var binding in layout) if (binding.Type == UniformType.SamplerWithTexture && binding.Native >= 0) Require(binding.Set, binding.Slot, binding.Depth);
        foreach (var link in links) if (link.Sampler is { } source) Require(source.Set, source.Binding, link.Native.Depth);
        foreach (var group in layout.GroupBy(b => (b.Set, b.Slot)))
        {
            if (!group.Any(b => b.Type == UniformType.SamplerWithTexture) || group.Any(b => b.Type is not (UniformType.Texture or UniformType.Sampler or UniformType.SamplerWithTexture))) continue;
            var shape = group.First(b => b.Type != UniformType.Sampler);
            foreach (var binding in group.Where(b => b.Type != UniformType.Sampler))
                if (binding.Array != shape.Array || binding.PixelType != shape.PixelType) throw new ArgumentException("A shared sampled image has inconsistent stage types.");
            for (var i = 0; i < layout.Length; i++) if (layout[i].Set == group.Key.Set && layout[i].Slot == group.Key.Slot)
                    layout[i] = layout[i] with { Type = UniformType.SamplerWithTexture, Array = shape.Array, PixelType = shape.PixelType };
            for (var i = 0; i < links.Length; i++)
            {
                var link = links[i];
                if (link.Image.Set == group.Key.Set && link.Image.Binding == group.Key.Slot) link = link with { Image = link.Image with { Type = UniformType.SamplerWithTexture } };
                if (link.Sampler is { } sampler && sampler.Set == group.Key.Set && sampler.Binding == group.Key.Slot) link = link with { Sampler = sampler with { Type = UniformType.SamplerWithTexture } };
                links[i] = link;
            }
        }
    }
    private void PrepareFetchSampler()
    {
        if (_fetchSampler is not null) return;
        var info = new SDL.GPUSamplerCreateInfo { MinFilter = SDL.GPUFilter.Nearest, MagFilter = SDL.GPUFilter.Nearest, MipmapMode = SDL.GPUSamplerMipmapMode.Nearest, AddressModeU = SDL.GPUSamplerAddressMode.ClampToEdge, AddressModeV = SDL.GPUSamplerAddressMode.ClampToEdge, AddressModeW = SDL.GPUSamplerAddressMode.ClampToEdge, MaxLod = 1000 };
        _fetchSampler = new(SDL.CreateGPUSampler(Device, info), h => SDL.ReleaseGPUSampler(Device, h), _context.Handle);
    }
    private RID[] LinkedSource(CanvasSamplerArrays.DeviceSource source)
    {
        if (!_sets.TryGetValue(source.Set, out var set) || set.Shader != Get(_pushPipeline).Shader || !set.Bindings.TryGetValue(source.Binding, out var ids)) throw new InvalidOperationException("Bind every sampled resource set for this pipeline first.");
        return ids;
    }
    private SDL.GPUTextureSamplerBinding LinkedSampler(SamplerLink link, bool graphics)
    {
        var imageIDs = LinkedSource(link.Image); var image = Get(imageIDs[link.Image.Type == UniformType.SamplerWithTexture ? 1 : 0], Kind.Texture);
        if (graphics && ActiveAttachment(image)) throw new InvalidOperationException("An active output attachment cannot be sampled during its render pass.");
        nint sampler;
        if (link.Sampler is { } source)
        {
            var entry = Get(LinkedSource(source)[0], Kind.Sampler); sampler = entry.NativeHandle;
            if (entry.CompareSampling != link.Native.Depth || link.Native.Depth && !DepthFormat(image.Texture!.Format)) throw new ArgumentException("A comparison sampler requires a matching depth image and shader role.");
            if (link.Native.PixelType != 0 && entry.LinearSampling) throw new ArgumentException("Integer textures require nearest filtering without anisotropy.");
        }
        else sampler = _fetchSampler!.DangerousGetHandle();
        return new() { Texture = image.NativeHandle, Sampler = sampler };
    }
}
