namespace Electron2D;

internal sealed partial class ShaderProgram
{
    private byte[][]? _vertexVariants;
    internal byte[]? VertexCode(bool instanced, bool lighting, bool procedural = false) => _vertexVariants?[(instanced ? 1 : 0) | (lighting ? 2 : 0) | (procedural ? 4 : 0)];
    internal void PrepareVertexVariants()
    {
        var vertex = Vertex ?? throw new InvalidOperationException("No authored vertex stage.");
        var variants = new byte[8][];
        for (var i = 0; i < variants.Length; i++) variants[i] = CanvasVertexProgram.Compose(vertex.NativeCode, (i & 1) != 0, (i & 2) != 0, (i & 4) != 0);
        _vertexVariants = variants;
    }

    internal ShaderProgram? Vertex { get; private init; }
    internal ShaderProgram? Fragment { get; private init; }
    internal int FragmentBufferCount { get; private init; } = bufferSizes.Length;
    internal int FragmentTextureCount { get; private init; } = (textures ?? []).Length;
    internal int[] VertexTextureIndices { get; private init; } = [];
    internal (ShaderUniform Source, ShaderUniform Target)[] UniformCopies { get; private init; } = [];

    internal void ValidateVertexFrame()
    {
        var frame = Uniforms.Values.Where(u => u.Buffer == 0).ToArray();
        if (BufferSizes.Length == 0 || BufferSizes[0] != 16 || frame.Length != 1 || frame[0].Type != typeof(Vector2) || frame[0].Offset != 0 || frame[0].ArrayLength != 0 || frame[0].DefaultWords is not null ||
            InstanceUniforms.Values.Any(u => u.Buffer == 0) || TimeUniform?.Buffer == 0 || ScreenPixelSizeUniform?.Buffer == 0 || SDFCoordsUniform?.Buffer == 0 || NormalMapFlagsUniform?.Buffer == 0)
            throw new NotSupportedException("The vertex Frame block at binding zero contains only a float2 client size at offset zero.");
    }

    internal static ShaderProgram WithVertex(ShaderProgram fragment, ShaderProgram vertex)
    {
        if (fragment.Vertex is not null || vertex.Vertex is not null) throw new ArgumentException("Canvas stages must be independent modules.");
        vertex.ValidateVertexFrame();
        var bufferOffset = fragment.BufferSizes.Length - 1;
        ShaderUniform Shift(ShaderUniform value) => value with { Buffer = value.Buffer + bufferOffset };
        var uniforms = new Dictionary<string, ShaderUniform>(fragment.Uniforms, StringComparer.Ordinal);
        var instances = new Dictionary<string, ShaderUniform>(fragment.InstanceUniforms, StringComparer.Ordinal);
        var globals = new Dictionary<string, ShaderUniform>(fragment.GlobalUniforms, StringComparer.Ordinal);
        var samplers = fragment.Textures.ToList();
        var copies = new List<(ShaderUniform, ShaderUniform)>();
        void AddUniform(Dictionary<string, ShaderUniform> target, ShaderUniform value)
        {
            if (samplers.Any(t => t.Name == value.Name || t.IsGlobal && t.GlobalName == value.Name) || (!ReferenceEquals(target, uniforms) && uniforms.ContainsKey(value.Name) || !ReferenceEquals(target, instances) && instances.ContainsKey(value.Name) || !ReferenceEquals(target, globals) && globals.ContainsKey(value.Name)))
                throw new ArgumentException($"Shader parameter '{value.Name}' has conflicting material, instance or sampled-image roles across stages.");
            var mapped = Shift(value);
            if (target.TryGetValue(value.Name, out var canonical))
            {
                if (!canonical.SameType(mapped)) throw new ArgumentException($"Shader parameter '{value.Name}' has different types across stages.");
                if (canonical.DefaultWords is not null && mapped.DefaultWords is not null && !canonical.HasSameDefault(mapped.DefaultWords))
                    throw new ArgumentException($"Shader parameter '{value.Name}' has conflicting defaults across stages.");
                if (canonical.DefaultWords is null && mapped.DefaultWords is not null) target[value.Name] = canonical = canonical with { DefaultWords = mapped.DefaultWords };
                if (canonical.HintMetadata is { } firstHint && mapped.HintMetadata is { } secondHint && firstHint != secondHint)
                    throw new ArgumentException($"Shader parameter '{value.Name}' has conflicting tooling metadata across stages.");
                if (canonical.HintMetadata is null && mapped.HintMetadata is not null) target[value.Name] = canonical = canonical with { HintMetadata = mapped.HintMetadata };
                copies.Add((canonical, mapped));
            }
            else target.Add(mapped.Name, mapped);
        }
        foreach (var uniform in vertex.Uniforms.Values) if (uniform.Buffer != 0) AddUniform(uniforms, uniform);
        foreach (var uniform in vertex.InstanceUniforms.Values) AddUniform(instances, uniform);
        foreach (var uniform in vertex.GlobalUniforms.Values) AddUniform(globals, uniform);
        if (instances.Count > 16) throw new NotSupportedException("A canvas program supports at most sixteen instance parameters across both stages.");
        var textureIndices = new int[vertex.Textures.Length];
        for (var i = 0; i < textureIndices.Length; i++)
        {
            var sampler = vertex.Textures[i];
            if (uniforms.ContainsKey(sampler.Name) || instances.ContainsKey(sampler.Name) || globals.ContainsKey(sampler.Name) || sampler.IsGlobal && (uniforms.ContainsKey(sampler.GlobalName) || instances.ContainsKey(sampler.GlobalName) || globals.ContainsKey(sampler.GlobalName)))
                throw new ArgumentException($"Shader parameter '{sampler.Name}' has conflicting uniform and sampled-image roles across stages.");
            if (samplers.FirstOrDefault(t => t.Name == sampler.Name) is { } existing && (!existing.SameType(sampler) || existing.ImagePolicy != sampler.ImagePolicy))
                throw new ArgumentException($"Sampled image '{sampler.Name}' has different types across stages.");
            var index = samplers.FindIndex(t => t.Name == sampler.Name && t.Element == sampler.Element);
            if (index < 0) { index = samplers.Count; samplers.Add(sampler); }
            else if (!samplers[index].SameType(sampler) || samplers[index].ImagePolicy != sampler.ImagePolicy) throw new ArgumentException($"Sampled image '{sampler.Name}' has different types across stages.");
            textureIndices[i] = index;
        }
        ShaderUniform? Builtin(ShaderUniform? first, ShaderUniform? second)
        {
            if (second is null) return first;
            var mapped = Shift(second);
            if (first is null) return mapped;
            if (!first.SameType(mapped)) throw new ArgumentException($"Renderer uniform '{first.Name}' has different types across stages.");
            copies.Add((first, mapped)); return first;
        }
        var time = Builtin(fragment.TimeUniform, vertex.TimeUniform);
        var pixels = Builtin(fragment.ScreenPixelSizeUniform, vertex.ScreenPixelSizeUniform);
        var sdf = Builtin(fragment.SDFCoordsUniform, vertex.SDFCoordsUniform);
        return new(fragment.Code, [.. fragment.BufferSizes, .. vertex.BufferSizes.Skip(1)], uniforms, samplers.ToArray(), time, pixels, sdf, instances, globals)
        {
            NativeCode = fragment.NativeCode,
            NormalMapFlagsUniform = fragment.NormalMapFlagsUniform,
            Vertex = vertex,
            Fragment = fragment,
            FragmentBufferCount = fragment.BufferSizes.Length,
            FragmentTextureCount = fragment.Textures.Length,
            VertexTextureIndices = textureIndices,
            UniformCopies = copies.ToArray()
        };
    }
}
