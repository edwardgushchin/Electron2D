using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUCanvasBackend
{
    private readonly Resource?[,] _imageFallbacks = new Resource?[2, 5];

    internal Resource ImageFallback(bool array, int kind)
    {
        var shape = array ? 1 : 0;
        if (_imageFallbacks[shape, kind] is { } retained) return retained;
        using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8);
        image.Fill(kind switch
        {
            1 => new(0, 0, 0, 1),
            2 => new(0, 0, 0, 0),
            3 => new(128f / 255, 128f / 255, 1, 1),
            4 => new(1, 128f / 255, 1, 1),
            _ => Colors.White
        });
        if (array)
        {
            var texture = new TextureArray();
            try { texture.CreateFromImages([image]); texture.AcquireRendererCacheResidency(); }
            catch { texture.Dispose(); throw; }
            return _imageFallbacks[shape, kind] = texture;
        }
        var ordinary = ImageTexture.CreateFromImage(image); ordinary.RetainRendererCache = true;
        return _imageFallbacks[shape, kind] = ordinary;
    }

    private nint ImageSampler(ShaderImagePolicy policy, int anisotropy) => Sampler(
        policy.Filter == 0 ? TextureFilter.Linear : (TextureFilter)policy.Filter,
        policy.Repeat == 1 ? TextureRepeat.Enabled : TextureRepeat.Disabled, anisotropy);

    private SDL.GPUTextureSamplerBinding ChannelBinding(CanvasBatch batch, bool normal)
    {
        var channels = Channels(batch.Texture);
        var texture = normal ? channels?.NormalTexture : channels?.SpecularTexture;
        if (ShaderTexture.ResourcePixelType(texture) != 0) throw new ArgumentException("Canvas graph normal and specular inputs require float sampling.");
        texture ??= (Texture)ImageFallback(false, normal ? 3 : 0);
        return CanvasBinding(batch with { Material = null, Texture = texture }, color: false);
    }

    private void PrepareGraphChannels(CanvasBatch batch)
    {
        if (batch.Material is not { } material) return;
        foreach (var texture in material.Program.Textures)
            if (texture.IsNormalTexture || texture.IsSpecularTexture) _ = ChannelBinding(batch, texture.IsNormalTexture);
    }

    private void DisposeImageFallbacks()
    {
        foreach (var resource in _imageFallbacks)
        {
            if (resource is TextureLayered layered) layered.ReleaseRendererCacheResidency();
            resource?.Dispose();
        }
        Array.Clear(_imageFallbacks);
    }
}
