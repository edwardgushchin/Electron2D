using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUCanvasBackend
{
    private RenderHandle? _glowBlurPipeline, _glowCompositePipeline;
    private static readonly byte[] GlowBlurShader = BuiltInShaders.Read("CanvasGlowBlur.frag.spv");
    private static readonly byte[] GlowCompositeShader = BuiltInShaders.Read("CanvasGlowComposite.frag.spv");

    private RenderHandle CreatePostProcessPipeline(byte[] fragmentCode)
    {
        var (vertexCode, linkedFragment) = ShaderCompiler.LinkGraphicsStages(Device, BuiltInShaders.DrawableVertex, fragmentCode);
        using var vertex = CreateShader(vertexCode, fragment: false);
        using var fragment = CreateShader(linkedFragment, fragment: true);
        var target = new SDL.GPUColorTargetDescription { Format = CanvasFormat(true) };
        var info = new SDL.GPUGraphicsPipelineCreateInfo
        {
            VertexShader = vertex.DangerousGetHandle(),
            FragmentShader = fragment.DangerousGetHandle(),
            PrimitiveType = SDL.GPUPrimitiveType.TriangleList,
            RasterizerState = new() { EnableDepthClip = true },
            TargetInfo = new() { ColorTargetDescriptions = (nint)(&target), NumColorTargets = 1 }
        };
        var result = new RenderHandle(SDL.CreateGPUGraphicsPipeline(Device, in info), h => SDL.ReleaseGPUGraphicsPipeline(Device, h), _device);
        RecordCanvasPipelineCompilation();
        return result;
    }

    private nint PrepareGlowMap(Texture? map, out Rect2 region)
    {
        region = new(Vector2.Zero, Vector2.One);
        if (map is null || map.IsDisposed) return 0;
        if (map is AtlasTexture atlas)
        {
            map = atlas.ResolvePolygonTexture(true, out var atlasRegion);
            if (map is null) return 0;
            if (atlasRegion is { } mapped) region = mapped;
        }
        if (ShaderTexture.ResourcePixelType(map) != 0) throw new NotSupportedException("Glow maps require a filterable color texture.");
        var native = PrepareTexture(map);
        return LinearTexture(map, native);
    }

    private void DrawGlow(nint command, CanvasRenderTarget output, GlowSettings glow, int levelCount, nint map, Rect2 mapRegion, in TonemapUniforms tone)
    {
        var buffers = output.PostProcess!;
        var size = output.Size;
        var raw = buffers.Raw.DangerousGetHandle();
        var next = output.Next.DangerousGetHandle();
        var sampler = Sampler(TextureFilter.Linear, TextureRepeat.Disabled, bias: 0);
        var input = raw;
        var inputSize = size;
        for (var i = 0; i < levelCount; i++)
        {
            var level = buffers.Levels![i]!.DangerousGetHandle();
            var levelSize = PostProcessBuffers.LevelSize(size, i);
            var parameters = new GlowBlurUniforms(new(1f / inputSize.X, 1f / inputSize.Y, glow.Strength, i == 0 ? tone.Options.Y : -1),
                new(glow.Threshold, glow.HDRScale, glow.Bloom, glow.LuminanceCap));
            var binding = new SDL.GPUTextureSamplerBinding { Texture = input, Sampler = sampler };
            DrawPostProcessPass(command, level, levelSize, _glowBlurPipeline!, new(in binding), in parameters);
            input = level;
            inputSize = levelSize;
        }

        Span<SDL.GPUTextureSamplerBinding> bindings = stackalloc SDL.GPUTextureSamplerBinding[GlowSettings.LevelCount + 2];
        bindings[0] = new() { Texture = raw, Sampler = sampler };
        for (var i = 0; i < GlowSettings.LevelCount; i++)
            bindings[i + 1] = new() { Texture = i < levelCount ? buffers.Levels![i]!.DangerousGetHandle() : raw, Sampler = sampler };
        // A device alias must not sample the active composite attachment.
        if (map == next) map = output.Current.DangerousGetHandle();
        bindings[^1] = new() { Texture = map != 0 ? map : raw, Sampler = sampler };
        var weights = levelCount == 0 ? default : glow.Levels;
        var sum = 0f;
        if (glow.Normalized)
            for (var i = 0; i < GlowSettings.LevelCount; i++) sum += weights[i];
        if (sum > 0)
            for (var i = 0; i < GlowSettings.LevelCount; i++) weights[i] /= sum;

        var intensity = glow.Blend == CanvasEnvironment.GlowBlendMode.Mix ? glow.Mix : glow.Intensity;
        var composite = new GlowCompositeUniforms(new(weights[0], weights[1], weights[2], weights[3]), new(weights[4], weights[5], weights[6], 0),
            new(intensity, map != 0 ? glow.MapStrength : 0, (float)glow.Blend, glow.Bicubic ? 1 : 0),
            new(mapRegion.Position.X, mapRegion.Position.Y, mapRegion.Size.X, mapRegion.Size.Y), tone);
        DrawPostProcessPass(command, next, size, _glowCompositePipeline!, bindings, in composite);
    }

    private void DrawPostProcessPass<T>(nint command, nint target, Vector2i size, RenderHandle pipeline,
        ReadOnlySpan<SDL.GPUTextureSamplerBinding> bindings, in T parameters) where T : unmanaged
    {
        var pass = BeginPass(command, target, SDL.GPULoadOp.DontCare, default);
        try
        {
            var vertexUniform = new GlowVertexUniforms(new(0, 0, size.X, size.Y), Vector4.One, new(size.X, size.Y, 0, 0));
            SDL.PushGPUVertexUniformData(command, 0, (nint)(&vertexUniform), (uint)sizeof(GlowVertexUniforms));
            var fragmentUniform = parameters;
            SDL.PushGPUFragmentUniformData(command, 0, (nint)(&fragmentUniform), (uint)sizeof(T));
            SDL.BindGPUGraphicsPipeline(pass, pipeline.DangerousGetHandle());
            SDL.BindGPUFragmentSamplers(pass, 0, bindings, (uint)bindings.Length);
            SDL.DrawGPUPrimitives(pass, 6, 1, 0, 0);
        }
        finally { SDL.EndGPURenderPass(pass); }
    }

    private readonly record struct GlowVertexUniforms(Vector4 Rectangle, Vector4 Modulation, Vector4 Size);
    private readonly record struct GlowBlurUniforms(Vector4 Sample, Vector4 Threshold);
    private readonly record struct GlowCompositeUniforms(Vector4 FirstLevels, Vector4 LastLevels, Vector4 Options, Vector4 MapRegion, TonemapUniforms Tonemap);
}

internal sealed class PostProcessBuffers(RenderHandle raw) : IDisposable
{
    internal readonly RenderHandle Raw = raw;
    internal RenderHandle?[]? Levels;
    internal bool HasRawFrame;

    internal static Vector2i LevelSize(Vector2i size, int index) =>
        new(Math.Max(1, size.X >> (index + 1)), Math.Max(1, size.Y >> (index + 1)));

    internal void ReleaseLevels()
    {
        if (Levels is not { } levels) return;
        Levels = null;
        foreach (var level in levels) level?.Dispose();
    }

    public void Dispose()
    {
        try { Raw.Dispose(); }
        finally { ReleaseLevels(); }
    }
}
