using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private sealed record GraphicsRecipe(SDL.GPUGraphicsPipelineCreateInfo Description, SDL.GPUVertexAttribute[] Attributes, SDL.GPUVertexBufferDescription[] Buffers, SDL.GPUColorTargetDescription[] Colors, bool Discard, float LineWidth, NativeMultisampleState Samples, NativeDepthStencilState Depth, uint Logic);
    private sealed record SwizzleVariant(Entry[] Programs, RenderHandle[] Pipelines);

    private uint BoundSwizzle(uint set, int binding, int element, UniformType type, RID shader, int count)
    {
        if (!_sets.TryGetValue(set, out var resources) || resources.Shader != shader || !resources.Bindings.TryGetValue(binding, out var ids)) throw new InvalidOperationException("Bind every sampled resource set before selecting its component maps.");
        if (ids.Length != count * ResourceWidth(type)) throw new ArgumentException("Incorrect sampled descriptor resource count.", nameof(binding));
        return Get(ids[element * ResourceWidth(type) + (type == UniformType.SamplerWithTexture ? 1 : 0)], Kind.Texture).Texture!.Swizzle;
    }
    private TextureSwizzleKey BoundSwizzles(Entry pipeline, Entry program, Dictionary<(uint Set, int Binding, int Element), uint>? descriptions = null)
    {
        Span<uint> values = stackalloc uint[32]; values.Clear(); var count = 0;
        foreach (var descriptor in program.Layout)
        {
            if (descriptor.Type != UniformType.SamplerWithTexture || descriptor.Native < 0) continue;
            var value = BoundSwizzle(descriptor.Set, descriptor.Slot, descriptor.Element, descriptor.Type, pipeline.Shader, descriptor.Count);
            if (count == values.Length) throw new NotSupportedException("A graphics program exceeds its native sampled capacity.");
            values[count++] = value; if (descriptions is not null) descriptions[(descriptor.Set, descriptor.Slot, descriptor.Element)] = value;
        }
        foreach (var link in program.Samplers)
        {
            var source = link.Image; var value = BoundSwizzle(source.Set, source.Binding, source.Element, source.Type, pipeline.Shader, source.Count);
            if (count == values.Length) throw new NotSupportedException("A graphics program exceeds its native sampled capacity.");
            values[count++] = value; if (descriptions is not null) descriptions[(source.Set, source.Binding, source.Element)] = value;
        }
        return TextureSwizzleKey.Create(values[..count]);
    }
    private SwizzleVariant? SampledVariant(Entry pipeline)
    {
        var source = PipelineProgram(pipeline); var key = BoundSwizzles(pipeline, source);
        if (key == default) return null;
        if (pipeline.SwizzleVariants is { } existing && existing.TryGetValue(key, out var cached)) return cached;
        var descriptions = new Dictionary<(uint Set, int Binding, int Element), uint>(); BoundSwizzles(pipeline, source, descriptions);
        var count = pipeline.Kind == Kind.GraphicsPipeline ? pipeline.ViewPrograms.Length + 1 : 1;
        var programs = new Entry[count]; var handles = pipeline.Kind == Kind.GraphicsPipeline ? new RenderHandle[count] : [];
        try
        {
            for (var view = 0; view < count; view++)
            {
                var origin = view == 0 ? source : pipeline.ViewPrograms[view - 1];
                programs[view] = CreateDeviceProgram(origin.Sources, origin.Name, origin.Constants, outputSlots: origin.OutputSlots, viewIndex: origin.ViewIndex, swizzles: descriptions);
                if (pipeline.Kind == Kind.GraphicsPipeline) handles[view] = CreateSwizzledGraphics(programs[view], pipeline.Recipe!);
            }
            var variant = new SwizzleVariant(programs, handles); var variants = pipeline.SwizzleVariants ??= [];
            if (variants.Count >= 64)
            {
                var oldest = default(TextureSwizzleKey); foreach (var pair in variants) { oldest = pair.Key; break; }
                var retired = variants[oldest]; variants.Remove(oldest); ReleaseSwizzle(retired);
            }
            variants.Add(key, variant); return variant;
        }
        catch { foreach (var handle in handles) handle?.Dispose(); foreach (var program in programs) ReleaseProgram(program); throw; }
    }
    private RenderHandle CreateSwizzledGraphics(Entry program, GraphicsRecipe recipe)
    {
        fixed (SDL.GPUVertexAttribute* attributes = recipe.Attributes)
        fixed (SDL.GPUVertexBufferDescription* buffers = recipe.Buffers)
        fixed (SDL.GPUColorTargetDescription* colors = recipe.Colors)
        {
            var info = recipe.Description; info.VertexShader = program.Handle!.DangerousGetHandle(); info.FragmentShader = program.FragmentHandle!.DangerousGetHandle();
            info.VertexInputState.VertexAttributes = (nint)attributes; info.VertexInputState.VertexBufferDescriptions = (nint)buffers;
            info.TargetInfo.ColorTargetDescriptions = (nint)colors;
            return new(CreateGraphicsPipeline(in info, recipe.Discard, recipe.LineWidth, recipe.Samples, recipe.Depth, program.GraphicsImages, recipe.Logic), h => SDL.ReleaseGPUGraphicsPipeline(Device, h), _context.Handle);
        }
    }
    private static void ReleaseSwizzle(SwizzleVariant variant)
    { foreach (var pipeline in variant.Pipelines) pipeline.Dispose(); foreach (var program in variant.Programs) ReleaseProgram(program); }
    private static void ReleaseSwizzles(Entry entry)
    {
        if (entry.SwizzleVariants is not { } variants) return;
        foreach (var variant in variants.Values) ReleaseSwizzle(variant);
        entry.SwizzleVariants = null;
    }
}
