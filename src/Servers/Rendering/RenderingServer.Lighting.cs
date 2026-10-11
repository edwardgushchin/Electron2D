namespace Electron2D;

public sealed partial class RenderingServer
{
    private void CaptureCanvasLighting(Transform framebuffer)
    {
        _lighting.Clear();
        foreach (var node in _nodes)
            if (node is Light { Enabled: true } light && light.RenderCanvas() is { } canvas && canvas.View(_viewport, out var basis, out _, out _, _interpolationFraction))
                _lighting.AddLight(light, framebuffer * basis * light.RenderGlobal(_interpolationFraction));
        foreach (var light in _retainedLights.Values)
            if (light.Enabled && RenderingCanvasRegistry.ResolveOrNull(light.Canvas) is { } canvas && canvas.View(_viewport, out var basis, out _, out _, _interpolationFraction))
                _lighting.AddLight(light, framebuffer * basis * light.Interpolation.Render(light.Transform, _interpolationFraction, CanvasSceneTree?.IsPhysicsInterpolationActive == true));
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
