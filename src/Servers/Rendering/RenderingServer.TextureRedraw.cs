using System.Runtime.CompilerServices;

namespace Electron2D;

public sealed partial class RenderingServer
{
    private ConditionalWeakTable<Resource, object>? _forcedTextureRedraw;

    /// <summary>Requests continuous low-processor canvas frames while an image texture is visibly submitted.</summary>
    /// <param name="texture">A live borrowed or server-owned ordinary/layered image or proxy RID.</param>
    /// <param name="enable">Whether visible sampling keeps subsequent frames pending.</param>
    /// <remarks>The policy belongs to this rendering session, preserves resource ownership and does not change pixels.
    /// Submitted aliases, canvas channels and shader image bindings observe their image-source policies. Hidden,
    /// clipped, culled and unused bindings do not independently request frames. Disable or stop submitting the image
    /// to return to idle after a settling frame. Drawing-only MeshTexture identities use their sampled BaseTexture
    /// image instead. RenderLoopEnabled and viewport update policies still apply. Visibility is conservative for
    /// generated/authored vertices without explicit bounds, shader branches and pixel occlusion.</remarks>
    /// <exception cref="ArgumentException">The RID is stale, has another role or identifies drawing-only mesh data.</exception>
    /// <exception cref="InvalidOperationException">The renderer is absent, off-owner or submitting native work.</exception>
    public static void TextureSetForceRedrawIfVisible(RID texture, bool enable)
    {
        var server = RequireService(); server.EnsureTextureChange();
        var resource = RenderingTextureRegistry.ResolveResource(texture);
        if (resource is MeshTexture) throw new ArgumentException("Use the mesh texture's sampled base image identity.", nameof(texture));
        if (enable) { server._forcedTextureRedraw ??= new(); if (!server._forcedTextureRedraw.TryGetValue(resource, out _)) server._forcedTextureRedraw.Add(resource, server); }
        else server._forcedTextureRedraw?.Remove(resource);
    }

    private bool NeedsTextureRedraw(Resource? texture)
    {
        while (texture is not null && !texture.IsDisposed)
        {
            if (_forcedTextureRedraw!.TryGetValue(texture, out _)) return true;
            switch (texture)
            {
                case ServerTexture { IsProxy: true } proxy: texture = RenderingTextureRegistry.TryResolveResource(proxy.ProxyTarget); break;
                case LayeredTextureProxy proxy: texture = RenderingTextureRegistry.TryResolveResource(proxy.ProxyTarget); break;
                case AtlasTexture atlas: texture = atlas.Atlas; break;
                case AnimatedTexture animated: texture = animated.GetFrameTexture(animated.CurrentFrame); break;
                case CanvasTexture channels: return NeedsTextureRedraw(channels.DiffuseTexture) || NeedsTextureRedraw(channels.NormalTexture) || NeedsTextureRedraw(channels.SpecularTexture);
                case ViewportTexture view: return view.Bound is { } viewport && _canvasFrames.TryGetValue(viewport, out var frame) && FrameNeedsTextureRedraw(frame);
                default: return false;
            }
        }
        return false;
    }
    private bool HasVisibleTextureRedraw()
    {
        if (_forcedTextureRedraw is null) return false;
        foreach (var frame in _submittedFrames)
        {
            if (!ScreenAttachment(frame.Viewport, out var rect, out var id) || NativeWindow(id) is not { Visible: true, NativeDisplay: { } display }) continue;
            if (rect == default) rect = new(Vector2.Zero, _backend.FindTarget(frame.Viewport)!.Size);
            if (rect.Intersects(new Rect2(Vector2.Zero, display.WindowGetSizeCore())) && FrameNeedsTextureRedraw(frame)) return true;
        }
        return false;
    }
    private bool FrameNeedsTextureRedraw(CanvasFrame frame)
    {
        if (!frame.Drawn || frame.TextureRedrawVisited) return false;
        frame.TextureRedrawVisited = true; var lighting = false;
        foreach (var batch in frame.Batches)
        {
            if (!TextureBatchVisible(frame, batch)) continue;
            lighting |= batch.Lighting is { Mode: not LightMode.Unshaded };
            if (NeedsTextureRedraw(batch.TextureSource ?? batch.Texture)) return true;
            if (batch.Material is not { } material) continue;
            material.CopyTextures(frame.TextureScratch);
            try { for (var i = 0; i < material.Textures.Length; i++) if (NeedsTextureRedraw(frame.TextureScratch[i])) return true; }
            finally { Array.Clear(frame.TextureScratch); }
        }
        if (lighting) foreach (var mask in frame.Lighting.Masks) if (NeedsTextureRedraw(mask.Texture)) return true;
        return false;
    }
    private bool TextureBatchVisible(CanvasFrame frame, CanvasBatch batch)
    {
        if (batch.Operation != CanvasOperation.Draw || batch.Count <= 0) return false;
        var size = _backend.FindTarget(frame.Viewport)!.Size;
        var clip = new Rect2(Vector2.Zero, size);
        if (batch.Clip is { } scissor) clip = clip.Intersection(new Rect2(scissor.Position, scissor.Size));
        if (!clip.HasArea()) return false;
        if (batch.VisibilityBounds is { } manual) return manual.Intersects(clip, includeBorders: true);
        // shortcut: generated vertices without explicit bounds use conservative visibility; tighten when GPU bounds are available.
        if (batch.Material?.Program.Vertex is not null || batch.InstanceCount > 0 || batch.Procedural || batch.Particles is not null) return true;
        var bounds = new Rect2(frame.Vertices[batch.First].Position, Vector2.Zero);
        for (var i = batch.First + 1; i < batch.First + batch.Count; i++) bounds = bounds.Expand(frame.Vertices[i].Position);
        return bounds.Intersects(clip, includeBorders: true);
    }

}
