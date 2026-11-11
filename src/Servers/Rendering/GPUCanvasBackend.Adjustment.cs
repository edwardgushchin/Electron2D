using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUCanvasBackend
{
    private (nint Texture, Vector4 Region) PrepareAdjustmentLookup(Texture? texture)
    {
        Rect2? region = null;
        while (texture is { IsDisposed: false })
        {
            if (texture is AtlasTexture atlas)
            {
                var source = region ?? new Rect2(Vector2.Zero, atlas.GetSize());
                var destination = new Rect2(Vector2.Zero, source.Size);
                texture = atlas.ResolveDrawRegion(ref destination, ref source);
                region = source;
            }
            else if (texture is CanvasTexture channels) texture = channels.DiffuseTexture;
            else if (texture is ServerTexture { IsProxy: true } proxy) texture = RenderingTextureRegistry.ResolveProxySource(proxy.ProxyTarget);
            else break;
        }

        texture = RenderingTextureRegistry.SampledStorage(texture) as Texture;
        if (texture is null || texture.IsDisposed || texture is ViewportTexture { Bound: null }) return default;
        var size = texture.GetSize();
        if (size.X <= 0 || size.Y <= 0) return default;
        var row = new Rect2(Vector2.Zero, size);
        if (region is { } sourceRegion) row = row.Intersection(sourceRegion);
        if (!row.HasArea()) return default;
        if (ShaderTexture.ResourcePixelType(texture) != 0)
            throw new NotSupportedException("Color correction requires a filterable color texture.");

        var native = PrepareTexture(texture);
        if (_textures.TryGetValue(texture, out var entry) && ReferenceEquals(entry.Pixels, RenderingTextureRegistry.PlaceholderPixels))
            return default;
        var lookup = LinearTexture(texture, native);
        return (lookup, new(row.Position.X / size.X, row.Size.X / size.X,
            row.Position.Y / size.Y, row.Size.Y / size.Y));
    }

    private SDL.GPUTextureSamplerBinding AdjustmentBinding(CanvasRenderTarget output, nint lookup)
    {
        // Reading a device alias of the active attachment would create a feedback loop.
        if (lookup == output.Next.DangerousGetHandle()) lookup = output.Current.DangerousGetHandle();
        return new()
        {
            Texture = lookup != 0 ? lookup : output.PostProcess!.Raw.DangerousGetHandle(),
            Sampler = Sampler(TextureFilter.Linear, TextureRepeat.Disabled, bias: 0)
        };
    }

    private readonly record struct AdjustmentUniforms(Vector4 Options, Vector4 Region);
}
