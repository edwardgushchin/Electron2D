namespace Electron2D;

public sealed partial class RenderingServer
{
    /// <summary>Color storage formats supported by writable texture targets.</summary>
    public enum TextureDrawableFormat
    {
        /// <summary>Normalized eight-bit RGBA.</summary>
        RGBA8 = 0,
        /// <summary>Eight-bit sRGB color with linear shader sampling and blending.</summary>
        RGBA8SRGB = 1,
        /// <summary>Sixteen-bit floating-point RGBA.</summary>
        RGBAH = 2,
        /// <summary>Thirty-two-bit floating-point RGBA.</summary>
        RGBAF = 3,
    }

    private readonly DrawableTexture?[] _drawableTargets = new DrawableTexture?[4];
    private readonly Resource?[] _drawableSamples = new Resource?[16];
    private bool _preparingDrawable, _preparingNativeDrawable;
    private RID _drawableWhite, _defaultBlitMaterial;

    /// <summary>Creates an owned writable texture with cleared GPU storage.</summary>
    /// <param name="width">Width from one through 16384.</param><param name="height">Height from one through 16384.</param>
    /// <param name="format">Writable color format.</param><param name="color">Finite clear color; null selects white.</param>
    /// <param name="withMIPMaps">Allocates a complete mip chain.</param>
    /// <returns>A session-owned texture RID released by FreeRID or renderer shutdown.</returns>
    /// <exception cref="ArgumentException">The configuration is invalid.</exception>
    /// <exception cref="InvalidOperationException">An active GPU renderer or owner-thread access is absent.</exception>
    /// <exception cref="NotSupportedException">The GPU cannot allocate the requested format.</exception>
    public static RID TextureDrawableCreate(int width, int height, TextureDrawableFormat format, Color? color = null, bool withMIPMaps = false)
    {
        var server = RequireService(); server.EnsureTextureChange();
        if (server.GetRenderingDeviceCore() is null) throw new InvalidOperationException("Writable textures require the GPU renderer.");
        var texture = new DrawableTexture();
        try
        {
            texture.Setup(width, height, (DrawableTexture.DrawableFormat)format, color, withMIPMaps);
            var rid = RenderingTextureRegistry.Register(texture, server); texture.BindRenderingRID(rid);
            texture.ServerOwner = server; server._ownedTextureRIDs.Add(rid); return rid;
        }
        catch { texture.Dispose(); throw; }
    }

    /// <summary>Draws a compiled blit pass into one through four writable texture identities.</summary>
    /// <param name="textures">Distinct writable targets of equal actual size, format and mip allocation.</param>
    /// <param name="rect">Destination pixels at the selected mip; clipping preserves source UVs and negative sizes flip.</param>
    /// <param name="material">Borrowed blit material RID, or empty for built-in mixing.</param>
    /// <param name="modulate">Finite fragment modulation.</param>
    /// <param name="sourceTextures">Up to four source RIDs; missing/empty sources select opaque white.</param>
    /// <param name="toMipmap">Allocated destination mip level.</param>
    /// <remarks>Resource-owned targets are accepted; mutation owns only pixels, never the resource lifetime.
    /// Aliases through logical proxies and native image wrappers reject before the pass.</remarks>
    /// <exception cref="ArgumentException">A RID, target configuration, source alias or color is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A count or mip is invalid.</exception>
    /// <exception cref="InvalidOperationException">The GPU renderer, setup or valid owner access is absent.</exception>
    /// <exception cref="NotSupportedException">The material belongs to another shader domain.</exception>
    public static void TextureDrawableBlitRect(ReadOnlySpan<RID> textures, Rect2i rect, RID material, Color modulate, ReadOnlySpan<RID> sourceTextures, int toMipmap = 0)
    {
        var server = RequireService(); server.EnsureDeviceAccess();
        if (textures.Length is < 1 or > 4 || sourceTextures.Length > 4) throw new ArgumentOutOfRangeException(nameof(textures));
        server.BeginDrawablePreparation();
        try
        {
            for (var i = 0; i < textures.Length; i++) server._drawableTargets[i] = RenderingTextureRegistry.ResolveResource(textures[i]) as DrawableTexture ?? throw new ArgumentException("A target is not a writable texture.");
            for (var i = 0; i < sourceTextures.Length; i++) server._drawableSamples[i] = sourceTextures[i].IsValid() ? RenderingTextureRegistry.ResolveResource(sourceTextures[i]) as Texture ?? throw new ArgumentException("A blit source requires an ordinary texture.") : null;
            server.BlitDrawableCore(textures.Length, rect, sourceTextures.Length, modulate, toMipmap, material.IsValid() ? RenderingProgramRegistry<Material>.Resolve(material) : null);
        }
        finally { server.EndDrawablePreparation(); }
    }

    /// <summary>Recalculates the mip chain of a writable texture.</summary>
    /// <param name="texture">An initialized owned or borrowed writable texture identity.</param>
    /// <exception cref="ArgumentException">The RID is not a writable texture.</exception>
    /// <exception cref="InvalidOperationException">The GPU renderer or valid owner access is absent.</exception>
    public static void TextureDrawableGenerateMIPMaps(RID texture)
    {
        var server = RequireService(); server.EnsureDeviceAccess();
        (RenderingTextureRegistry.ResolveResource(texture) as DrawableTexture ?? throw new ArgumentException("The target is not a writable texture.")).GenerateMIPMaps();
    }

    /// <summary>Returns the retained built-in mixing material for writable texture passes.</summary>
    /// <returns>A session-owned material RID, recreated if explicitly freed.</returns>
    /// <exception cref="InvalidOperationException">The renderer or owner-thread access is absent.</exception>
    public static RID TextureDrawableGetDefaultMaterial()
    {
        var server = RequireService(); server.EnsureTextureChange();
        if (RenderingProgramRegistry<Material>.ResolveOrNull(server._defaultBlitMaterial) is not null) return server._defaultBlitMaterial;
        var material = new BlitMaterial(); var rid = RenderingProgramRegistry<Material>.Register(material, server);
        material.BindRenderingRID(rid); server._ownedMaterialRIDs.Add(rid); return server._defaultBlitMaterial = rid;
    }

    internal static void BlitDrawableTextures(ReadOnlySpan<DrawableTexture> targets, Rect2i rect, ReadOnlySpan<Texture?> sources, Color modulate, int mip, Material? material)
    {
        var server = RequireService(); server.EnsureDeviceAccess(); server.BeginDrawablePreparation();
        try
        {
            for (var i = 0; i < targets.Length; i++) server._drawableTargets[i] = targets[i];
            if (sources.Length > 4) throw new ArgumentOutOfRangeException(nameof(sources));
            for (var i = 0; i < sources.Length; i++) server._drawableSamples[i] = sources[i];
            server.BlitDrawableCore(targets.Length, rect, sources.Length, modulate, mip, material);
        }
        finally { server.EndDrawablePreparation(); }
    }
    internal static void BlitDrawableTextures(DrawableTexture first, ReadOnlySpan<DrawableTexture> extraTargets, Rect2i rect, ReadOnlySpan<Texture?> sources, Color modulate, int mip, Material? material)
    {
        var server = RequireService(); server.EnsureDeviceAccess(); server.BeginDrawablePreparation();
        try
        {
            server._drawableTargets[0] = first;
            for (var i = 0; i < extraTargets.Length; i++) server._drawableTargets[i + 1] = extraTargets[i];
            if (sources.Length > 4) throw new ArgumentOutOfRangeException(nameof(sources));
            for (var i = 0; i < sources.Length; i++) server._drawableSamples[i] = sources[i];
            server.BlitDrawableCore(extraTargets.Length + 1, rect, sources.Length, modulate, mip, material);
        }
        finally { server.EndDrawablePreparation(); }
    }
    private void BeginDrawablePreparation()
    { if (_preparingDrawable) throw new InvalidOperationException("Writable texture preparation cannot reenter."); _preparingDrawable = true; }
    private void EndDrawablePreparation()
    { Array.Clear(_drawableTargets); Array.Clear(_drawableSamples); _preparingDrawable = false; }

    private void BlitDrawableCore(int count, Rect2i rect, int sourceCount, Color color, int mip, Material? material)
    {
        var device = GetRenderingDeviceCore() ?? throw new InvalidOperationException("Writable textures require the GPU renderer.");
        device.ValidateCanvasMutation(); DrawableTexture.ValidateColor(color);
        var first = _drawableTargets[0] ?? throw new ArgumentNullException("targets"); var guarded = 0;
        Span<RID> targets = stackalloc RID[4]; Span<RID> sources = stackalloc RID[4]; Span<RID> samples = stackalloc RID[16];
        try
        {
            for (var i = 0; i < count; i++)
            {
                var target = _drawableTargets[i] ?? throw new ArgumentNullException("targets");
                for (var j = 0; j < i; j++) if (ReferenceEquals(target, _drawableTargets[j])) throw new ArgumentException("Blit targets must be distinct.");
                target.BeginBlit(); guarded++;
                if (target.NativeSize != first.NativeSize || target.NativeFormat != first.NativeFormat || target.MIPMapCount != first.MIPMapCount) throw new ArgumentException("Blit targets require equal actual dimensions, format and mip allocation.");
                if (mip < 0 || mip > target.MIPMapCount) throw new ArgumentOutOfRangeException(nameof(mip));
                targets[i] = target.EnsureImage();
            }
            var state = material?.GetCanvasState();
            var blend = material is BlitMaterial builtIn ? builtIn.BlendMode : BlitBlendMode.Mix;
            if (material is not null && material is not BlitMaterial && state?.Program.Mode != Shader.Mode.TextureBlit) throw new NotSupportedException("Writable texture passes require a TextureBlit material.");
            if (state is not null && state.Program.ColorOutputs != count) throw new ArgumentException("The shader color outputs must match the supplied targets.");
            for (var i = 0; i < 4; i++) sources[i] = ExportDrawableSource(i < sourceCount ? _drawableSamples[i] : null);
            var sampleCount = 4;
            if (state is not null)
            {
                state.CopyTextures(_drawableSamples); sampleCount = state.Program.Textures.Length;
                for (var i = 0; i < sampleCount; i++)
                {
                    var descriptor = state.Program.Textures[i];
                    var resource = _drawableSamples[i];
                    if (!descriptor.BlitSource && resource is null && descriptor.ImagePolicy is { } policy)
                        resource = ((GPUCanvasBackend)_backend).ImageFallback(descriptor.IsArray, policy.Fallback);
                    samples[i] = descriptor.BlitSource ? sources[descriptor.Name[^1] - '0'] : ExportDrawableSource(resource, suppliedSource: false);
                }
                if (material!.IsDisposed || state.Shader?.IsDisposed == true || !ReferenceEquals(state.Shader?.GetProgram(), state.Program)) throw new InvalidOperationException("The blit material changed during preparation.");
            }
            else sources.CopyTo(samples);
            device.BlitDrawable(targets[..count], rect, samples[..sampleCount], color, mip, state, blend, (float)CanvasTime);
            MarkChanged();
        }
        finally { for (var i = 0; i < guarded; i++) _drawableTargets[i]!.EndBlit(); }
    }
    private RID ExportDrawableSource(Resource? source, bool suppliedSource = true)
    {
        if (source is { IsDisposed: true }) throw new ObjectDisposedException(nameof(source));
        if (suppliedSource && source is AtlasTexture) source = null;
        if (source is CanvasTexture channels) source = ShaderTexture.Source(channels) ?? RenderingTextureRegistry.PlaceholderTexture;
        if (source is AtlasTexture atlas) source = atlas.RenderingTexture ?? throw new InvalidOperationException("A sampled atlas has no source texture.");
        if (source is null)
        {
            if (!_drawableWhite.IsValid()) { using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); image.Fill(Colors.White); _drawableWhite = Texture2DCreateCore(image); }
            return TextureGetRDTextureCore(_drawableWhite, false);
        }
        var rid = source.GetRID();
        if (source.IsDisposed) throw new ObjectDisposedException(nameof(source));
        var result = TextureGetRDTextureCore(rid, false);
        if (!result.IsValid()) throw new InvalidOperationException("A blit source has no sampled native image.");
        return result;
    }
}
