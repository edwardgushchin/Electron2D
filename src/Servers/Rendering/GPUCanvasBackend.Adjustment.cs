using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUCanvasBackend
{
    private (nint Texture, Vector4 Region) PrepareAdjustmentLookup(Texture? texture)
    {
        var source = ResolveEnvironmentTexture(texture, out var region);
        if (source is null) return default;
        var native = PrepareEnvironmentTexture(source);
        return native != 0 ? (native, new(region.Position.X, region.Size.X, region.Position.Y, region.Size.Y)) : default;
    }

    private static Texture? ResolveEnvironmentTexture(Texture? texture, out Rect2 normalizedRegion)
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

        normalizedRegion = default;
        texture = RenderingTextureRegistry.SampledStorage(texture) as Texture;
        if (texture is null || texture.IsDisposed || texture is ViewportTexture { Bound: null }) return null;
        var size = texture.GetSize();
        if (size.X <= 0 || size.Y <= 0) return null;
        var available = new Rect2(Vector2.Zero, size);
        if (region is { } sourceRegion) available = available.Intersection(sourceRegion);
        if (!available.HasArea()) return null;
        normalizedRegion = new(available.Position / size, available.Size / size);
        return texture;
    }

    private nint PrepareEnvironmentTexture(Texture texture)
    {
        if (ShaderTexture.ResourcePixelType(texture) != 0)
            throw new NotSupportedException("Canvas environment textures require filterable color data.");
        var native = PrepareTexture(texture);
        if (_textures.TryGetValue(texture, out var entry) && ReferenceEquals(entry.Pixels, RenderingTextureRegistry.PlaceholderPixels))
            return 0;
        return LinearTexture(texture, native);
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
