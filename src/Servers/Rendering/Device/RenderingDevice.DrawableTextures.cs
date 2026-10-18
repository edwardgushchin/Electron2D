using SDL3;

namespace Electron2D;

public sealed unsafe partial class RenderingDevice
{
    private readonly Dictionary<(byte[] Code, DataFormat Format, int Count, BlitBlendMode Blend), RenderHandle> _drawablePipelines = [];
    private RenderHandle? _drawableLinearSampler, _drawableNearestSampler;

    internal void GenerateDrawableMIPMaps(RID texture)
    {
        IdleList(); var image = Get(texture, Kind.Texture);
        if (image.Texture!.Mips > 1) SDL.GenerateMipmapsForGPUTexture(Commands(), image.NativeHandle);
    }

    internal void BlitDrawable(ReadOnlySpan<RID> targets, Rect2i rect, ReadOnlySpan<RID> samples, Color color, int mip, MaterialState? material, BlitBlendMode blend, float time)
    {
        IdleList();
        var first = Get(targets[0], Kind.Texture); var info = first.Texture!;
        var width = Math.Max(1u, info.Width >> mip); var height = Math.Max(1u, info.Height >> mip);
        var colors = stackalloc SDL.GPUColorTargetInfo[4];
        for (var i = 0; i < targets.Length; i++)
            colors[i] = new() { Texture = Get(targets[i], Kind.Texture).NativeHandle, MipLevel = (uint)mip, LoadOp = SDL.GPULoadOp.Load, StoreOp = SDL.GPUStoreOp.Store };
        var bindings = stackalloc SDL.GPUTextureSamplerBinding[16];
        var sampleCount = material is null ? targets.Length : samples.Length;
        for (var i = 0; i < sampleCount; i++)
        {
            var sample = Get(samples[i], Kind.Texture); PrepareRendererTexture(sample);
            for (var j = 0; j < targets.Length; j++) if (sample.NativeHandle == colors[j].Texture) throw new ArgumentException("A blit source cannot alias any target allocation.");
            var descriptor = material?.Program.Textures[i];
            if (PixelType(sample.Texture!.Format) != (descriptor?.PixelType ?? 0) || (sample.Texture.Type == TextureType.TextureArray) != (descriptor?.IsArray ?? false))
                throw new ArgumentException("A blit image does not match its sampled scalar type or shape.");
            var integer = PixelType(sample.Texture.Format) != 0;
            bindings[i] = new() { Texture = sample.NativeHandle, Sampler = DrawableSampler(integer) };
        }
        if (rect.Size.X == 0 || rect.Size.Y == 0) return;
        var fragment = material?.Program.NativeCode ?? BuiltInShaders.DrawableFragment(targets.Length);
        var key = (fragment, info.Format, targets.Length, blend);
        if (!_drawablePipelines.TryGetValue(key, out var pipeline))
        {
            if (_drawablePipelines.Count == 32) { foreach (var old in _drawablePipelines.Values) old.Dispose(); _drawablePipelines.Clear(); }
            pipeline = CreateDrawablePipeline(fragment, info.Format, targets.Length, blend); _drawablePipelines.Add(key, pipeline);
        }
        var command = Commands();
        Span<Vector4> frame = stackalloc Vector4[3];
        frame[0] = new(rect.Position.X, rect.Position.Y, rect.Size.X, rect.Size.Y);
        frame[1] = new(color.R, color.G, color.B, color.A); frame[2] = new(width, height, 0, 0);
        SDL.PushGPUVertexUniformData(command, 0, System.Runtime.InteropServices.MemoryMarshal.AsBytes(frame), 48);
        material?.PushUniforms(command, time, default);
        var pass = SDL.BeginGPURenderPass(command, new ReadOnlySpan<SDL.GPUColorTargetInfo>(colors, targets.Length), (uint)targets.Length, 0);
        if (pass == 0) throw GPUPhysicsDevice.Failure("begin writable texture pass");
        try
        {
            SDL.BindGPUGraphicsPipeline(pass, pipeline.DangerousGetHandle());
            SDL.SetGPUViewport(pass, new() { W = width, H = height, MinDepth = 0, MaxDepth = 1 });
            SDL.SetGPUScissor(pass, new() { W = (int)width, H = (int)height });
            if (sampleCount != 0) SDL.BindGPUFragmentSamplers(pass, 0, new ReadOnlySpan<SDL.GPUTextureSamplerBinding>(bindings, sampleCount), (uint)sampleCount);
            SDL.DrawGPUPrimitives(pass, 6, 1, 0, 0);
        }
        finally { SDL.EndGPURenderPass(pass); }
    }
    private nint DrawableSampler(bool nearest)
    {
        ref var handle = ref nearest ? ref _drawableNearestSampler : ref _drawableLinearSampler;
        return (handle ??= new RenderHandle(SDL.CreateGPUSampler(Device, new()
        {
            MinFilter = nearest ? SDL.GPUFilter.Nearest : SDL.GPUFilter.Linear,
            MagFilter = nearest ? SDL.GPUFilter.Nearest : SDL.GPUFilter.Linear,
            MipmapMode = SDL.GPUSamplerMipmapMode.Nearest,
            AddressModeU = SDL.GPUSamplerAddressMode.ClampToEdge,
            AddressModeV = SDL.GPUSamplerAddressMode.ClampToEdge,
            AddressModeW = SDL.GPUSamplerAddressMode.ClampToEdge,
            MaxLod = 0
        }), h => SDL.ReleaseGPUSampler(Device, h), _context.Handle)).DangerousGetHandle();
    }
    private RenderHandle CreateDrawablePipeline(byte[] fragmentCode, DataFormat format, int count, BlitBlendMode blend)
    {
        using var vertex = new RenderHandle(ShaderCompiler.CreateShader(Device, BuiltInShaders.DrawableVertex, false), h => SDL.ReleaseGPUShader(Device, h), _context.Handle);
        using var fragment = new RenderHandle(ShaderCompiler.CreateShader(Device, fragmentCode, true), h => SDL.ReleaseGPUShader(Device, h), _context.Handle);
        var state = blend switch
        {
            BlitBlendMode.Mix or BlitBlendMode.Disabled => (SDL.GPUBlendFactor.SrcAlpha, SDL.GPUBlendFactor.OneMinusSrcAlpha, SDL.GPUBlendFactor.One, SDL.GPUBlendFactor.OneMinusSrcAlpha, SDL.GPUBlendOp.Add),
            BlitBlendMode.Add => (SDL.GPUBlendFactor.SrcAlpha, SDL.GPUBlendFactor.One, SDL.GPUBlendFactor.SrcAlpha, SDL.GPUBlendFactor.One, SDL.GPUBlendOp.Add),
            BlitBlendMode.Sub => (SDL.GPUBlendFactor.SrcAlpha, SDL.GPUBlendFactor.One, SDL.GPUBlendFactor.SrcAlpha, SDL.GPUBlendFactor.One, SDL.GPUBlendOp.ReverseSubtract),
            BlitBlendMode.Mul => (SDL.GPUBlendFactor.DstColor, SDL.GPUBlendFactor.Zero, SDL.GPUBlendFactor.DstAlpha, SDL.GPUBlendFactor.Zero, SDL.GPUBlendOp.Add),
            _ => throw new ArgumentOutOfRangeException(nameof(blend))
        };
        var targets = stackalloc SDL.GPUColorTargetDescription[4];
        for (var i = 0; i < count; i++) targets[i] = new()
        {
            Format = NativeFormat(format),
            BlendState = new()
            {
                EnableBlend = blend != BlitBlendMode.Disabled,
                SrcColorBlendFactor = state.Item1,
                DstColorBlendFactor = state.Item2,
                SrcAlphaBlendFactor = state.Item3,
                DstAlphaBlendFactor = state.Item4,
                ColorBlendOp = state.Item5,
                AlphaBlendOp = state.Item5
            }
        };
        var description = new SDL.GPUGraphicsPipelineCreateInfo
        {
            VertexShader = vertex.DangerousGetHandle(),
            FragmentShader = fragment.DangerousGetHandle(),
            PrimitiveType = SDL.GPUPrimitiveType.TriangleList,
            RasterizerState = new() { EnableDepthClip = true },
            TargetInfo = new() { ColorTargetDescriptions = (nint)targets, NumColorTargets = (uint)count }
        };
        return new(SDL.CreateGPUGraphicsPipeline(Device, in description), h => SDL.ReleaseGPUGraphicsPipeline(Device, h), _context.Handle);
    }
    private void ReleaseDrawablePipelines()
    {
        foreach (var pipeline in _drawablePipelines.Values) pipeline.Dispose(); _drawablePipelines.Clear();
        _drawableLinearSampler?.Dispose(); _drawableNearestSampler?.Dispose();
    }
}
