using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUCanvasBackend
{
    private bool _currentHDR;
    private readonly Dictionary<Texture, (RenderHandle View, Vector2i Size, uint Levels)> _linearViews = [];
    private readonly Dictionary<Texture, nint> _linearSources = [];
    private readonly HashSet<Texture> _usedLinearViews = [];
    private RenderHandle? _linearizePipeline, _presentationPipeline;

    private nint LinearTexture(Texture texture, nint native)
    {
        if (ReferenceEquals(texture, _whiteTexture)) return native;
        Vector2i size; uint levels;
        if (texture is ViewportTexture viewport)
        {
            var target = FindTarget(viewport.Bound!)!;
            if (target.HDR) return native;
            size = target.Size; levels = 1;
        }
        else if (texture is DrawableTexture drawable)
        {
            if (drawable.NativeFormat != RenderingDevice.DataFormat.R8G8B8A8UNORM) return native;
            size = drawable.NativeSize; levels = (uint)(drawable.MIPMapCount + 1);
        }
        else if (texture is TextureRD { DeviceInfo: { } info })
        {
            if (!RenderingDevice.CanvasEncodedColor(info.Format)) return native;
            size = new((int)info.Width, (int)info.Height); levels = info.Mips;
        }
        else
        {
            var pixels = _textures[texture].Pixels;
            if (pixels.Upload.Format != Image.Format.Rgba8) return native;
            size = new(pixels.Source.Width, pixels.Source.Height); levels = (uint)pixels.Levels;
        }
        if (!_linearViews.TryGetValue(texture, out var view) || view.Size != size || view.Levels != levels)
        {
            var replacement = CreateGPUTarget(size, default, false, SDL.GPUSampleCount.SampleCount1, true, levels);
            view.View?.Dispose(); view = (replacement, size, levels); _linearViews[texture] = view;
        }
        _usedLinearViews.Add(texture); _linearSources[texture] = native;
        _linearizePipeline ??= CreatePipeline(BuiltInShaders.ColorFragment, BlendMode.Mix, overwrite: true, format: CanvasFormat(true));
        _ = Sampler(TextureFilter.NearestWithMipmaps, TextureRepeat.Disabled);
        return view.View.DangerousGetHandle();
    }

    private void BindColorCopy(nint command, nint pass, RenderHandle pipeline, nint source, Vector4 operation, int vertexOffset, int first)
    {
        SDL.BindGPUGraphicsPipeline(pass, pipeline.DangerousGetHandle());
        var dimensions = new Vector4(1, 1, 0, 0);
        PushFrame(command, dimensions, Colors.White);
        SDL.PushGPUFragmentUniformData(command, 0, (nint)(&operation), (uint)sizeof(Vector4));
        var binding = new SDL.GPUBufferBinding { Buffer = _vertexBuffer!.DangerousGetHandle(), Offset = (uint)vertexOffset };
        SDL.BindGPUVertexBuffers(pass, 0, new ReadOnlySpan<SDL.GPUBufferBinding>(&binding, 1), 1);
        var sampler = new SDL.GPUTextureSamplerBinding { Texture = source, Sampler = Sampler(TextureFilter.NearestWithMipmaps, TextureRepeat.Disabled) };
        SDL.BindGPUFragmentSamplers(pass, 0, new ReadOnlySpan<SDL.GPUTextureSamplerBinding>(&sampler, 1), 1);
        SDL.DrawGPUPrimitives(pass, 6, 1, (uint)first, 0);
    }

    private void ConvertColorSources(nint command, int vertexOffset, int first)
    {
        foreach (var source in _linearSources)
        {
            var view = _linearViews[source.Key];
            // Refresh from native storage so device writes remain visible without CPU readback.
            for (uint mip = 0; mip < view.Levels; ++mip)
            {
                var target = new SDL.GPUColorTargetInfo { Texture = view.View.DangerousGetHandle(), MipLevel = mip, LoadOp = SDL.GPULoadOp.DontCare, StoreOp = SDL.GPUStoreOp.Store };
                var pass = SDL.BeginGPURenderPass(command, new ReadOnlySpan<SDL.GPUColorTargetInfo>(&target, 1), 1, 0);
                if (pass == 0) throw Failure("begin linear color conversion");
                try { BindColorCopy(command, pass, _linearizePipeline!, source.Value, new(mip, 1, 0, 0), vertexOffset, first); }
                finally { SDL.EndGPURenderPass(pass); }
            }
        }
    }

    private void RetireColorViews()
    {
        foreach (var pair in _linearViews)
            if (!_usedLinearViews.Contains(pair.Key) && (!pair.Key.RetainRendererCache || pair.Key.IsDisposed))
            { pair.Value.View.Dispose(); _linearViews.Remove(pair.Key); }
        _linearSources.Clear(); _currentHDR = false;
    }

    private void DisposeColorViews()
    {
        foreach (var view in _linearViews.Values) view.View.Dispose();
        _linearViews.Clear(); _linearSources.Clear(); _usedLinearViews.Clear();
        _linearizePipeline?.Dispose(); _presentationPipeline?.Dispose();
    }

    private static void PushFrame(nint command, Vector4 dimensions, Color modulation)
    {
        var frame = stackalloc Vector4[2] { dimensions, new(modulation.R, modulation.G, modulation.B, modulation.A) };
        SDL.PushGPUVertexUniformData(command, 0, (nint)frame, (uint)(2 * sizeof(Vector4)));
    }

    private static Color LinearColor(Color value) => CanvasGeometry.LinearColor(value);
}
