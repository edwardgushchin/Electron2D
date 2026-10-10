namespace Electron2D;

public sealed partial class RenderingServer
{
    /// <summary>Creates a server-owned array from copied homogeneous image layers.</summary>
    /// <param name="layers">Nonempty images sharing size, format and mipmap state.</param>
    /// <param name="layeredType">The independent image-array sampling role.</param>
    /// <returns>A stable RID owned until FreeRID or renderer shutdown.</returns>
    /// <exception cref="ArgumentException">Image configuration is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The sampling role is invalid.</exception>
    /// <exception cref="NotSupportedException">A source format is unsupported.</exception>
    /// <exception cref="InvalidOperationException">The native service is absent, off-owner, submitting or closing.</exception>
    public static RID Texture2DLayeredCreate(ReadOnlySpan<Image> layers, TextureLayered.LayeredType layeredType) => RequireService().Texture2DLayeredCreateCore(layers, layeredType);
    /// <summary>Creates a server-owned diagnostic image-array placeholder.</summary>
    /// <param name="layeredType">The independent image-array sampling role.</param>
    /// <returns>A stable RID for one diagnostic layer until FreeRID or shutdown.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The sampling role is invalid.</exception>
    /// <exception cref="InvalidOperationException">The native service is absent, off-owner, submitting or closing.</exception>
    public static RID Texture2DLayeredPlaceholderCreate(TextureLayered.LayeredType layeredType) => RequireService().Texture2DLayeredPlaceholderCreateCore(layeredType);
    /// <summary>Copies an array layer from a live resource-owned or server-owned texture RID.</summary>
    /// <param name="texture">A layered texture or layered proxy identity.</param>
    /// <param name="layer">The zero-based image layer.</param>
    /// <returns>A caller-owned image; null for a metadata-only resource.</returns>
    /// <exception cref="ArgumentException">The RID is stale or belongs to another resource role.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The readable image layer is outside the stored range.</exception>
    /// <exception cref="InvalidOperationException">The service is absent/off-owner, or a proxy source is absent.</exception>
    public static Image? Texture2DLayerGet(RID texture, int layer) => RequireService().Texture2DLayerGetCore(texture, layer);

    internal RID Texture2DLayeredCreateCore(ReadOnlySpan<Image> layers, TextureLayered.LayeredType type)
    {
        EnsureTextureChange(); if (type != TextureLayered.LayeredType.Array) throw new ArgumentOutOfRangeException(nameof(type));
        var resource = new TextureArray();
        try { resource.CreateFromImages(layers); return OwnLayered(resource); }
        catch { resource.ServerReleased = true; resource.Dispose(); throw; }
    }
    internal RID Texture2DLayeredPlaceholderCreateCore(TextureLayered.LayeredType type)
    {
        using var image = RenderingTextureRegistry.PlaceholderPixels.CopyImage();
        return Texture2DLayeredCreateCore([image], type);
    }
    private RID OwnLayered(TextureLayered resource)
    {
        resource.ServerOwner = this; var rid = resource.GetRID(); _ownedTextureRIDs.Add(rid); return rid;
    }
    internal Image? Texture2DLayerGetCore(RID rid, int layer) { EnsureOwner(); return RenderingTextureRegistry.ResolveLayered(rid).GetLayerData(layer); }
}
