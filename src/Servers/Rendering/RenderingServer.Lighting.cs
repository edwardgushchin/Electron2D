namespace Electron2D;

public sealed partial class RenderingServer
{
    private int _canvasShadowTextureSize;

    /// <summary>Sets the resolution of point and directional canvas shadow rows.</summary>
    /// <param name="size">Requested texel count, clamped to one and rounded up to a power of two, at most 16384.</param>
    /// <remarks>The change applies to every canvas in the active rendering session on its next rendered frame.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The rounded size exceeds the supported texture axis bound.</exception>
    /// <exception cref="InvalidOperationException">There is no active renderer, or owner-thread/frame guards reject the change.</exception>
    public static void CanvasSetShadowTextureSize(int size)
    {
        var server = RequireService(); server.EnsureServerViewportChange();
        var rounded = CanvasShadowSize(size);
        if (server._canvasShadowTextureSize == rounded) return;
        server._canvasShadowTextureSize = rounded; server.MarkChanged();
    }

    private static int CanvasShadowSize(int size) => size > 16384 ? throw new ArgumentOutOfRangeException(nameof(size)) :
        (int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)Math.Max(1, size));

    private void CaptureCanvasLighting(Transform framebuffer)
    {
        _lighting.Clear();
        _lighting.ShadowSize = _canvasShadowTextureSize;
        var editedRoot = Engine.IsEditorHint ? CanvasSceneTree?.EditedSceneRoot : null;
        foreach (var node in _nodes)
            if (node is Light { Enabled: true } light &&
                (!light.EditorOnly || editedRoot is not null && (ReferenceEquals(light, editedRoot) || ReferenceEquals(light.Owner, editedRoot))) &&
                light.RenderCanvas() is { } canvas && canvas.View(_viewport, out var basis, out _, out _, _interpolationFraction))
                _lighting.AddLight(light, framebuffer * basis * light.RenderGlobal(_interpolationFraction), ViewportHDR(_viewport));
        foreach (var light in _retainedLights.Values)
            if (light.Enabled && RenderingCanvasRegistry.ResolveOrNull(light.Canvas) is { } canvas && canvas.View(_viewport, out var basis, out _, out _, _interpolationFraction))
                _lighting.AddLight(light, framebuffer * basis * light.Interpolation.Render(light.Transform, _interpolationFraction, CanvasSceneTree?.IsPhysicsInterpolationActive == true), ViewportHDR(_viewport));
        _lighting.PackMasks();
        foreach (var node in _nodes)
            if (node is LightOccluder { Occluder: { IsDisposed: false } polygon } occluder && occluder.RenderCanvas() is { } canvas && canvas.View(_viewport, out var basis, out var layer, out _, _interpolationFraction))
                _lighting.AddContour(polygon, framebuffer * basis * occluder.RenderGlobal(_interpolationFraction), occluder.OccluderLightMask, layer, occluder.SDFCollision);
        foreach (var occluder in _retainedOccluders.Values)
            if (occluder.Visible && occluder.Occluder is { IsDisposed: false } polygon && RenderingCanvasRegistry.ResolveOrNull(occluder.Canvas) is { } canvas && canvas.View(_viewport, out var basis, out var layer, out _, _interpolationFraction))
                _lighting.AddContour(polygon, framebuffer * basis * occluder.Interpolation.Render(occluder.Transform, _interpolationFraction, CanvasSceneTree?.IsPhysicsInterpolationActive == true), occluder.OccluderLightMask, layer, occluder.SDFCollision);
        foreach (var node in _nodes)
            if (node is TileMapLayer tiles && tiles.RenderCanvas() is { } canvas && canvas.View(_viewport, out var basis, out var layer, out _, _interpolationFraction))
                tiles.AppendOcclusion(_lighting, framebuffer * basis * tiles.RenderGlobal(_interpolationFraction), layer);
    }

    /// <summary>Sets point-light receiving membership for a retained or scene canvas item.</summary>
    /// <param name="item">Live canvas item identity.</param>
    /// <param name="lightMask">Receiving membership; directional lights ignore this mask.</param>
    public static void CanvasItemSetLightMask(RID item, uint lightMask)
    { var server = RequireService(); server.Change(item, out _).LightMask = lightMask; }
}
