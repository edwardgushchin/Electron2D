using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUCanvasBackend
{
    private readonly Dictionary<TextureLayered, (RenderHandle View, Vector2i Size, uint Layers, uint Levels)> _linearArrayViews = [];
    private readonly Dictionary<TextureLayered, nint> _linearArraySources = [];
    private readonly HashSet<TextureLayered> _usedLinearArrayViews = [];
    private RenderHandle? _linearizeArrayPipeline;

    private nint LinearLayeredTexture(TextureLayered texture, nint native)
    {
        Vector2i size; uint layers, levels;
        if (texture is TextureLayeredRD { DeviceInfo: { } info })
        {
            if (!RenderingDevice.CanvasEncodedColor(info.Format)) return native;
            size = new((int)info.Width, (int)info.Height); layers = info.Layers; levels = info.Mips;
        }
        else
        {
            var pixels = _layeredTextures[texture].ArrayPixels!;
            if (pixels.First.Upload.Format != Image.Format.Rgba8) return native;
            size = new(pixels.First.Source.Width, pixels.First.Source.Height); layers = (uint)pixels.Layers.Length; levels = (uint)pixels.First.Levels;
        }
        if (!_linearArrayViews.TryGetValue(texture, out var view) || view.Size != size || view.Layers != layers || view.Levels != levels)
        {
            var usage = SDL.GPUTextureUsageFlags.ColorTarget | SDL.GPUTextureUsageFlags.Sampler;
            if (!SDL.GPUTextureSupportsFormat(Device, CanvasFormat(true), SDL.GPUTextureType.TextureType2DArray, usage))
                throw new NotSupportedException("The GPU cannot prepare linear sampled image-array views.");
            var createInfo = new SDL.GPUTextureCreateInfo
            { Type = SDL.GPUTextureType.TextureType2DArray, Format = CanvasFormat(true), Width = (uint)size.X, Height = (uint)size.Y, LayerCountOrDepth = layers, NumLevels = levels, Usage = usage };
            var replacement = new RenderHandle(SDL.CreateGPUTexture(Device, in createInfo), h => SDL.ReleaseGPUTexture(Device, h), _device);
            view.View?.Dispose(); view = (replacement, size, layers, levels); _linearArrayViews[texture] = view;
        }
        _usedLinearArrayViews.Add(texture); _linearArraySources[texture] = native;
        _linearizeArrayPipeline ??= CreatePipeline(BuiltInShaders.ColorArrayFragment, BlendMode.Mix, overwrite: true, format: CanvasFormat(true));
        _ = Sampler(TextureFilter.NearestWithMipmaps, TextureRepeat.Disabled);
        return view.View.DangerousGetHandle();
    }

    private void ConvertArrayColorSources(nint command, int vertexOffset, int first)
    {
        foreach (var source in _linearArraySources)
        {
            var view = _linearArrayViews[source.Key];
            for (uint layer = 0; layer < view.Layers; ++layer)
                for (uint mip = 0; mip < view.Levels; ++mip)
                {
                    var target = new SDL.GPUColorTargetInfo { Texture = view.View.DangerousGetHandle(), MipLevel = mip, LayerOrDepthPlane = layer, LoadOp = SDL.GPULoadOp.DontCare, StoreOp = SDL.GPUStoreOp.Store };
                    var pass = SDL.BeginGPURenderPass(command, new ReadOnlySpan<SDL.GPUColorTargetInfo>(&target, 1), 1, 0);
                    if (pass == 0) throw Failure("begin array linear color conversion");
                    try { BindColorCopy(command, pass, _linearizeArrayPipeline!, source.Value, new(mip, 1, layer, 0), vertexOffset, first); }
                    finally { SDL.EndGPURenderPass(pass); }
                }
        }
    }

    private void RetireArrayColorViews()
    {
        foreach (var pair in _linearArrayViews)
            if (!_usedLinearArrayViews.Contains(pair.Key) && (!pair.Key.RetainRendererCache || pair.Key.IsDisposed))
            { pair.Value.View.Dispose(); _linearArrayViews.Remove(pair.Key); }
        _linearArraySources.Clear();
    }

    private void DisposeArrayColorViews()
    {
        foreach (var view in _linearArrayViews.Values) view.View.Dispose();
        _linearArrayViews.Clear(); _linearArraySources.Clear(); _usedLinearArrayViews.Clear(); _linearizeArrayPipeline?.Dispose();
    }
}
