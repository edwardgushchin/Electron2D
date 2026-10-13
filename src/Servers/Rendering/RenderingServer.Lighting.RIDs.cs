namespace Electron2D;

/// <summary>Selects point-mask or directional canvas illumination.</summary>
public enum CanvasLightMode
{
    /// <summary>Uses a transformed point mask.</summary>
    Point = 0,
    /// <summary>Uses a directional source.</summary>
    Directional = 1,
}
/// <summary>Selects a channel of a retained canvas texture.</summary>
public enum CanvasTextureChannel
{
    /// <summary>Color pixels and logical dimensions.</summary>
    Diffuse = 0,
    /// <summary>Normal vectors.</summary>
    Normal = 1,
    /// <summary>Specular reflection colors.</summary>
    Specular = 2,
}

internal sealed class RetainedCanvasLight : PointLight
{
    internal CanvasLightMode Mode;
    internal RID Canvas;
    internal CanvasTransformInterpolation Interpolation;
}
internal sealed class RetainedCanvasOccluder : LightOccluder
{
    internal RID Canvas;
    internal CanvasTransformInterpolation Interpolation;
}


public sealed partial class RenderingServer
{
    private readonly Dictionary<RID, RetainedCanvasLight> _retainedLights = [];
    private readonly Dictionary<RID, RetainedCanvasOccluder> _retainedOccluders = [];
    private readonly List<RID> _ownedOccluderPolygons = [];
    private RetainedCanvasLight LightChange(RID rid) { EnsureTextureChange(); return _retainedLights.TryGetValue(rid, out var light) ? light : throw new ArgumentException("The light identity is absent or belongs to another rendering session.", nameof(rid)); }
    private RetainedCanvasOccluder OccluderChange(RID rid) { EnsureTextureChange(); return _retainedOccluders.TryGetValue(rid, out var occluder) ? occluder : throw new ArgumentException("The occluder identity is absent or belongs to another rendering session.", nameof(rid)); }
    private CanvasTexture CanvasTextureChange(RID rid) { EnsureTextureChange(); return RenderingTextureRegistry.OwnedResource(rid, this) as CanvasTexture ?? throw new ArgumentException("The identity is not a server-owned canvas texture.", nameof(rid)); }
    private OccluderPolygon PolygonChange(RID rid) { EnsureTextureChange(); return RenderingProgramRegistry<OccluderPolygon>.Owned(rid, this); }
    private bool ReleaseLightingRID(RID rid)
    {
        if (_retainedLights.Remove(rid, out var light)) { light.Dispose(); return true; }
        if (_retainedOccluders.Remove(rid, out var occluder)) { occluder.Dispose(); return true; }
        if (!RenderingProgramRegistry<OccluderPolygon>.Contains(rid)) return false;
        var polygon = PolygonChange(rid); _ownedOccluderPolygons.Remove(rid); RenderingProgramRegistry<OccluderPolygon>.Remove(rid); polygon.Dispose(); return true;
    }
    private void ReleaseOwnedLighting()
    {
        foreach (var light in _retainedLights.Values) light.Dispose(); _retainedLights.Clear();
        foreach (var occluder in _retainedOccluders.Values) occluder.Dispose(); _retainedOccluders.Clear();
        foreach (var rid in _ownedOccluderPolygons) { var polygon = RenderingProgramRegistry<OccluderPolygon>.Owned(rid, this); RenderingProgramRegistry<OccluderPolygon>.Remove(rid); polygon.Dispose(); }
        _ownedOccluderPolygons.Clear();
    }
    private void AttachLight(RID rid, RID canvas) { var light = LightChange(rid); if (canvas.IsValid()) _ = CanvasChange(canvas); light.Canvas = canvas; }
    private void AttachOccluder(RID rid, RID canvas) { var occluder = OccluderChange(rid); if (canvas.IsValid()) _ = CanvasChange(canvas); occluder.Canvas = canvas; }
    private RID CreateLight() { EnsureTextureChange(); var rid = RID.Allocate(); var light = new RetainedCanvasLight(); light.Interpolation.Reset(light.Transform); _retainedLights.Add(rid, light); return rid; }
    private RID CreateOccluder() { EnsureTextureChange(); var rid = RID.Allocate(); var occluder = new RetainedCanvasOccluder(); occluder.Interpolation.Reset(occluder.Transform); _retainedOccluders.Add(rid, occluder); return rid; }
    private RID CreateOccluderPolygon()
    {
        EnsureTextureChange(); var polygon = new OccluderPolygon(); var rid = RenderingProgramRegistry<OccluderPolygon>.Register(polygon, this); polygon.BindRenderingRID(rid); _ownedOccluderPolygons.Add(rid); return rid;
    }
    private RID CreateCanvasTexture()
    {
        EnsureTextureChange(); var texture = new CanvasTexture(); var rid = RenderingTextureRegistry.Register(texture, this); texture.BindRenderingRID(rid); _ownedTextureRIDs.Add(rid); return rid;
    }
    internal void LightingPhysicsTick(SceneTree tree, bool start)
    {
        if (!ReferenceEquals(CanvasSceneTree, tree)) return;
        foreach (var light in _retainedLights.Values) light.Interpolation.Tick(light.Transform, start);
        foreach (var occluder in _retainedOccluders.Values) occluder.Interpolation.Tick(occluder.Transform, start);
    }
    internal void ResetLightingInterpolation(SceneTree tree)
    {
        if (!ReferenceEquals(CanvasSceneTree, tree)) return;
        foreach (var light in _retainedLights.Values) light.Interpolation.Reset(light.Transform);
        foreach (var occluder in _retainedOccluders.Values) occluder.Interpolation.Reset(occluder.Transform);
    }
    private void SetLightTransform(RID light, Transform transform)
    {
        var data = LightChange(light); data.Transform = transform;
        if (CanvasSceneTree?.IsInPhysicsFrame != true) data.Interpolation.Reset(transform);
    }
    private void SetOccluderTransform(RID occluder, Transform transform)
    {
        var data = OccluderChange(occluder); data.Transform = transform;
        if (CanvasSceneTree?.IsInPhysicsFrame != true) data.Interpolation.Reset(transform);
    }
    private void SetLightMode(RID light, CanvasLightMode mode) { var data = LightChange(light); if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode)); data.Mode = mode; }
    private void SetCanvasTextureChannel(RID rid, CanvasTextureChannel channel, RID texture)
    {
        var canvas = CanvasTextureChange(rid); var source = texture.IsValid() ? RenderingTextureRegistry.Resolve(texture) : null;
        switch (channel) { case CanvasTextureChannel.Diffuse: canvas.DiffuseTexture = source; break; case CanvasTextureChannel.Normal: canvas.NormalTexture = source; break; case CanvasTextureChannel.Specular: canvas.SpecularTexture = source; break; default: throw new ArgumentOutOfRangeException(nameof(channel)); }
    }
}
