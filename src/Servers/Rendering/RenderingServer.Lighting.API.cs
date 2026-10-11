namespace Electron2D;

public sealed partial class RenderingServer
{
    /// <summary>Creates a retained point light owned until FreeRID or renderer shutdown.</summary>
    /// <returns>A stable caller-owned identity.</returns>
    /// <exception cref="InvalidOperationException">The active renderer, owner thread or resource ownership rules reject the operation.</exception>
    public static RID CanvasLightCreate() => RequireService().CreateLight();
    /// <summary>Attaches a retained light to a borrowed canvas or detaches it with an empty identity.</summary>
    /// <param name="light">Live server-owned light.</param>
    /// <param name="canvas">Live borrowed canvas, or empty to detach.</param>
    /// <exception cref="InvalidOperationException">The active renderer, owner thread or resource ownership rules reject the operation.</exception>
    public static void CanvasLightAttachToCanvas(RID light, RID canvas) => RequireService().AttachLight(light, canvas);
    /// <summary>Creates a retained contour placement owned until FreeRID or shutdown.</summary>
    /// <returns>A stable caller-owned identity.</returns>
    /// <exception cref="InvalidOperationException">The active renderer, owner thread or resource ownership rules reject the operation.</exception>
    public static RID CanvasLightOccluderCreate() => RequireService().CreateOccluder();
    /// <summary>Attaches an occluder to a borrowed canvas or detaches it.</summary>
    /// <param name="occluder">Live server-owned occluder.</param>
    /// <param name="canvas">Live borrowed canvas, or empty to detach.</param>
    /// <exception cref="InvalidOperationException">The active renderer, owner thread or resource ownership rules reject the operation.</exception>
    public static void CanvasLightOccluderAttachToCanvas(RID occluder, RID canvas) => RequireService().AttachOccluder(occluder, canvas);
    /// <summary>Creates an empty retained contour owned until FreeRID or shutdown.</summary>
    /// <returns>A stable caller-owned identity.</returns>
    /// <exception cref="InvalidOperationException">The active renderer, owner thread or resource ownership rules reject the operation.</exception>
    public static RID CanvasOccluderPolygonCreate() => RequireService().CreateOccluderPolygon();
    /// <summary>Creates a retained multichannel canvas texture owned until FreeRID or shutdown.</summary>
    /// <returns>A stable caller-owned identity.</returns>
    /// <exception cref="InvalidOperationException">The active renderer, owner thread or resource ownership rules reject the operation.</exception>
    public static RID CanvasTextureCreate() => RequireService().CreateCanvasTexture();
    /// <summary>Selects point-mask or directional illumination without changing the identity.</summary>
    /// <param name="light">Live server-owned light.</param>
    /// <param name="mode">Defined light mode.</param>
    /// <exception cref="InvalidOperationException">The active renderer, owner thread or resource ownership rules reject the operation.</exception>
    public static void CanvasLightSetMode(RID light, CanvasLightMode mode) => RequireService().SetLightMode(light, mode);
    /// <summary>Assigns a borrowed texture channel or clears it with an empty identity.</summary>
    /// <param name="canvasTexture">Live server-owned canvas texture.</param>
    /// <param name="channel">Defined channel.</param>
    /// <param name="texture">Live ordinary texture identity, or empty.</param>
    /// <exception cref="InvalidOperationException">The active renderer, owner thread or resource ownership rules reject the operation.</exception>
    public static void CanvasTextureSetChannel(RID canvasTexture, CanvasTextureChannel channel, RID texture) => RequireService().SetCanvasTextureChannel(canvasTexture, channel, texture);
    /// <summary>Sets the retained light's BlendMode for the next frame.</summary>
    /// <param name="light">Live server-owned light.</param>
    /// <param name="mode">Typed light setting; undefined enum or nonfinite values are rejected.</param>
    /// <exception cref="InvalidOperationException">The active renderer, owner thread or resource ownership rules reject the operation.</exception>
    public static void CanvasLightSetBlendMode(RID light, LightBlendMode mode) => RequireService().LightChange(light).BlendMode = mode;
    /// <summary>Sets the retained light's Color for the next frame.</summary>
    /// <param name="light">Live server-owned light.</param>
    /// <param name="color">Typed light setting; undefined enum or nonfinite values are rejected.</param>
    /// <exception cref="InvalidOperationException">The active renderer, owner thread or resource ownership rules reject the operation.</exception>
    public static void CanvasLightSetColor(RID light, Color color) => RequireService().LightChange(light).Color = color;
    /// <summary>Sets the retained light's Enabled for the next frame.</summary>
    /// <param name="light">Live server-owned light.</param>
    /// <param name="enabled">Typed light setting; undefined enum or nonfinite values are rejected.</param>
    /// <exception cref="InvalidOperationException">The active renderer, owner thread or resource ownership rules reject the operation.</exception>
    public static void CanvasLightSetEnabled(RID light, bool enabled) => RequireService().LightChange(light).Enabled = enabled;
    /// <summary>Sets the retained light's Energy for the next frame.</summary>
    /// <param name="light">Live server-owned light.</param>
    /// <param name="energy">Typed light setting; undefined enum or nonfinite values are rejected.</param>
    /// <exception cref="InvalidOperationException">The active renderer, owner thread or resource ownership rules reject the operation.</exception>
    public static void CanvasLightSetEnergy(RID light, float energy) => RequireService().LightChange(light).Energy = energy;
    /// <summary>Sets the retained light's Height for the next frame.</summary>
    /// <param name="light">Live server-owned light.</param>
    /// <param name="height">Typed light setting; undefined enum or nonfinite values are rejected.</param>
    /// <exception cref="InvalidOperationException">The active renderer, owner thread or resource ownership rules reject the operation.</exception>
    public static void CanvasLightSetHeight(RID light, float height) => RequireService().LightChange(light).Height = height;
    /// <summary>Sets the retained light's RangeItemCullMask for the next frame.</summary>
    /// <param name="light">Live server-owned light.</param>
    /// <param name="mask">Typed light setting; undefined enum or nonfinite values are rejected.</param>
    /// <exception cref="InvalidOperationException">The active renderer, owner thread or resource ownership rules reject the operation.</exception>
    public static void CanvasLightSetItemCullMask(RID light, uint mask) => RequireService().LightChange(light).RangeItemCullMask = mask;
    /// <summary>Sets the retained light's ShadowItemCullMask for the next frame.</summary>
    /// <param name="light">Live server-owned light.</param>
    /// <param name="mask">Typed light setting; undefined enum or nonfinite values are rejected.</param>
    /// <exception cref="InvalidOperationException">The active renderer, owner thread or resource ownership rules reject the operation.</exception>
    public static void CanvasLightSetItemShadowCullMask(RID light, uint mask) => RequireService().LightChange(light).ShadowItemCullMask = mask;
    /// <summary>Sets the retained light's ShadowColor for the next frame.</summary>
    /// <param name="light">Live server-owned light.</param>
    /// <param name="color">Typed light setting; undefined enum or nonfinite values are rejected.</param>
    /// <exception cref="InvalidOperationException">The active renderer, owner thread or resource ownership rules reject the operation.</exception>
    public static void CanvasLightSetShadowColor(RID light, Color color) => RequireService().LightChange(light).ShadowColor = color;
    /// <summary>Sets the retained light's ShadowEnabled for the next frame.</summary>
    /// <param name="light">Live server-owned light.</param>
    /// <param name="enabled">Typed light setting; undefined enum or nonfinite values are rejected.</param>
    /// <exception cref="InvalidOperationException">The active renderer, owner thread or resource ownership rules reject the operation.</exception>
    public static void CanvasLightSetShadowEnabled(RID light, bool enabled) => RequireService().LightChange(light).ShadowEnabled = enabled;
    /// <summary>Sets the retained light's ShadowFilter for the next frame.</summary>
    /// <param name="light">Live server-owned light.</param>
    /// <param name="filter">Typed light setting; undefined enum or nonfinite values are rejected.</param>
    /// <exception cref="InvalidOperationException">The active renderer, owner thread or resource ownership rules reject the operation.</exception>
    public static void CanvasLightSetShadowFilter(RID light, LightShadowFilter filter) => RequireService().LightChange(light).ShadowFilter = filter;
    /// <summary>Sets the retained light's ShadowFilterSmooth for the next frame.</summary>
    /// <param name="light">Live server-owned light.</param>
    /// <param name="smooth">Typed light setting; undefined enum or nonfinite values are rejected.</param>
    /// <exception cref="InvalidOperationException">The active renderer, owner thread or resource ownership rules reject the operation.</exception>
    public static void CanvasLightSetShadowSmooth(RID light, float smooth) => RequireService().LightChange(light).ShadowFilterSmooth = smooth;
    /// <summary>Sets the retained light's Offset for the next frame.</summary>
    /// <param name="light">Live server-owned light.</param>
    /// <param name="offset">Typed light setting; undefined enum or nonfinite values are rejected.</param>
    /// <exception cref="InvalidOperationException">The active renderer, owner thread or resource ownership rules reject the operation.</exception>
    public static void CanvasLightSetTextureOffset(RID light, Vector2 offset) => RequireService().LightChange(light).Offset = offset;
    /// <summary>Sets the retained light's TextureScale for the next frame.</summary>
    /// <param name="light">Live server-owned light.</param>
    /// <param name="scale">Typed light setting; undefined enum or nonfinite values are rejected.</param>
    /// <exception cref="InvalidOperationException">The active renderer, owner thread or resource ownership rules reject the operation.</exception>
    public static void CanvasLightSetTextureScale(RID light, float scale) => RequireService().LightChange(light).TextureScale = scale;
    /// <summary>Sets the retained light's Transform for the next frame.</summary>
    /// <param name="light">Live server-owned light.</param>
    /// <param name="transform">Typed light setting; undefined enum or nonfinite values are rejected.</param>
    /// <exception cref="InvalidOperationException">The active renderer, owner thread or resource ownership rules reject the operation.</exception>
    public static void CanvasLightSetTransform(RID light, Transform transform) => RequireService().SetLightTransform(light, transform);
    /// <summary>Sets the retained occluder's SDFCollision for the next frame.</summary>
    /// <param name="occluder">Live server-owned occluder.</param>
    /// <param name="enable">Typed contour placement setting.</param>
    /// <exception cref="InvalidOperationException">The active renderer, owner thread or resource ownership rules reject the operation.</exception>
    public static void CanvasLightOccluderSetAsSDFCollision(RID occluder, bool enable) => RequireService().OccluderChange(occluder).SDFCollision = enable;
    /// <summary>Sets the retained occluder's Visible for the next frame.</summary>
    /// <param name="occluder">Live server-owned occluder.</param>
    /// <param name="enabled">Typed contour placement setting.</param>
    /// <exception cref="InvalidOperationException">The active renderer, owner thread or resource ownership rules reject the operation.</exception>
    public static void CanvasLightOccluderSetEnabled(RID occluder, bool enabled) => RequireService().OccluderChange(occluder).Visible = enabled;
    /// <summary>Sets the retained occluder's OccluderLightMask for the next frame.</summary>
    /// <param name="occluder">Live server-owned occluder.</param>
    /// <param name="mask">Typed contour placement setting.</param>
    /// <exception cref="InvalidOperationException">The active renderer, owner thread or resource ownership rules reject the operation.</exception>
    public static void CanvasLightOccluderSetLightMask(RID occluder, uint mask) => RequireService().OccluderChange(occluder).OccluderLightMask = mask;
    /// <summary>Sets the retained occluder's Transform for the next frame.</summary>
    /// <param name="occluder">Live server-owned occluder.</param>
    /// <param name="transform">Typed contour placement setting.</param>
    /// <exception cref="InvalidOperationException">The active renderer, owner thread or resource ownership rules reject the operation.</exception>
    public static void CanvasLightOccluderSetTransform(RID occluder, Transform transform) => RequireService().SetOccluderTransform(occluder, transform);
    /// <summary>Sets retained canvas texture TextureFilter independently of the drawing item.</summary>
    /// <param name="canvasTexture">Live server-owned canvas texture.</param>
    /// <param name="filter">Shared typed setting; Max is rejected.</param>
    /// <exception cref="InvalidOperationException">The active renderer, owner thread or resource ownership rules reject the operation.</exception>
    public static void CanvasTextureSetTextureFilter(RID canvasTexture, TextureFilter filter) => RequireService().CanvasTextureChange(canvasTexture).TextureFilter = filter;
    /// <summary>Sets retained canvas texture TextureRepeat independently of the drawing item.</summary>
    /// <param name="canvasTexture">Live server-owned canvas texture.</param>
    /// <param name="repeat">Shared typed setting; Max is rejected.</param>
    /// <exception cref="InvalidOperationException">The active renderer, owner thread or resource ownership rules reject the operation.</exception>
    public static void CanvasTextureSetTextureRepeat(RID canvasTexture, TextureRepeat repeat) => RequireService().CanvasTextureChange(canvasTexture).TextureRepeat = repeat;

    /// <summary>Assigns the borrowed light mask or clears it.</summary><param name="light">Owned light.</param><param name="texture">Ordinary borrowed texture, or empty.</param>
    public static void CanvasLightSetTexture(RID light, RID texture)
    { var server = RequireService(); var data = server.LightChange(light); data.Texture = texture.IsValid() ? RenderingTextureRegistry.Resolve(texture) : null; }
    /// <summary>Sets inclusive receiving canvas-layer bounds; reversed ranges receive no illumination.</summary><param name="light">Owned light.</param><param name="minLayer">Minimum layer.</param><param name="maxLayer">Maximum layer.</param>
    public static void CanvasLightSetLayerRange(RID light, int minLayer, int maxLayer)
    { var data = RequireService().LightChange(light); data.RangeLayerMin = minLayer; data.RangeLayerMax = maxLayer; }
    /// <summary>Sets inclusive receiving item Z bounds.</summary><param name="light">Owned light.</param><param name="minZ">Minimum Z.</param><param name="maxZ">Maximum Z.</param>
    public static void CanvasLightSetZRange(RID light, int minZ, int maxZ)
    { var data = RequireService().LightChange(light); data.RangeZMin = minZ; data.RangeZMax = maxZ; }
    /// <summary>Assigns a borrowed contour or clears the placement.</summary><param name="occluder">Owned placement.</param><param name="polygon">Live borrowed contour identity, or empty.</param>
    public static void CanvasLightOccluderSetPolygon(RID occluder, RID polygon)
    { var server = RequireService(); var data = server.OccluderChange(occluder); data.Occluder = polygon.IsValid() ? RenderingProgramRegistry<OccluderPolygon>.Resolve(polygon) : null; }
    /// <summary>Sets which directed edges of a retained polygon cast shadows.</summary><param name="occluderPolygon">Owned polygon.</param><param name="mode">Defined shared culling mode.</param>
    public static void CanvasOccluderPolygonSetCullMode(RID occluderPolygon, OccluderCullMode mode) => RequireService().PolygonChange(occluderPolygon).CullMode = mode;
    /// <summary>Copies a finite contour and its closing-edge policy.</summary><param name="occluderPolygon">Owned polygon.</param><param name="shape">Copied finite local points.</param><param name="closed">Whether to connect last and first points.</param>
    public static void CanvasOccluderPolygonSetShape(RID occluderPolygon, Vector2[] shape, bool closed)
    { var polygon = RequireService().PolygonChange(occluderPolygon); polygon.Polygon = shape; polygon.Closed = closed; }
    /// <summary>Sets reflection color and exponent controls of a retained canvas texture.</summary><param name="canvasTexture">Owned canvas texture.</param><param name="baseColor">Finite specular multiplier.</param><param name="shininess">Finite exponent control.</param>
    public static void CanvasTextureSetShadingParameters(RID canvasTexture, Color baseColor, float shininess)
    { var texture = RequireService().CanvasTextureChange(canvasTexture); if (!baseColor.IsFinite() || !float.IsFinite(shininess)) throw new ArgumentOutOfRangeException(nameof(shininess)); texture.SpecularColor = baseColor; texture.SpecularShininess = shininess; }
    /// <summary>Sets the sampled area and resolution of a live viewport's canvas distance field.</summary><param name="viewport">Live viewport in this rendering tree.</param><param name="oversize">Defined sampled-area preset.</param><param name="scale">Defined resolution preset.</param>
    public static void ViewportSetSDFOversizeAndScale(RID viewport, CanvasSDFOversize oversize, CanvasSDFScale scale)
    {
        var server = RequireService(); server.EnsureTextureChange();
        if (oversize is < CanvasSDFOversize.Percent100 or >= CanvasSDFOversize.Max || scale is < CanvasSDFScale.Percent100 or >= CanvasSDFScale.Max) throw new ArgumentOutOfRangeException(nameof(oversize));
        var view = Viewport.ResolveViewportRID(viewport); if (!ReferenceEquals(view.Tree, server._window.Tree)) throw new InvalidOperationException("Viewport belongs to another scene tree.");
        view.SDFOversize = oversize; view.SDFScale = scale;
    }
    /// <summary>Selects presentation interpolation between scene physics snapshots.</summary><param name="light">Owned identity.</param><param name="interpolated">Whether to interpolate while the owning tree enables physics interpolation.</param>
    public static void CanvasLightSetInterpolated(RID light, bool interpolated)
    { var data = RequireService().LightChange(light); data.Interpolation.Enabled = interpolated; data.Interpolation.Reset(data.Transform); }
    /// <summary>Resets previous and current presentation snapshots to the canonical transform.</summary><param name="light">Owned identity.</param>
    public static void CanvasLightResetPhysicsInterpolation(RID light)
    { var data = RequireService().LightChange(light); data.Interpolation.Reset(data.Transform); }
    /// <summary>Rebases both presentation snapshots without changing the canonical transform.</summary><param name="light">Owned identity.</param><param name="transform">Finite transform applied to both snapshots.</param>
    public static void CanvasLightTransformPhysicsInterpolation(RID light, Transform transform)
    { var data = RequireService().LightChange(light); data.Interpolation.Rebase(transform); }

    /// <summary>Selects presentation interpolation between scene physics snapshots.</summary><param name="occluder">Owned identity.</param><param name="interpolated">Whether to interpolate while the owning tree enables physics interpolation.</param>
    public static void CanvasLightOccluderSetInterpolated(RID occluder, bool interpolated)
    { var data = RequireService().OccluderChange(occluder); data.Interpolation.Enabled = interpolated; data.Interpolation.Reset(data.Transform); }
    /// <summary>Resets previous and current presentation snapshots to the canonical transform.</summary><param name="occluder">Owned identity.</param>
    public static void CanvasLightOccluderResetPhysicsInterpolation(RID occluder)
    { var data = RequireService().OccluderChange(occluder); data.Interpolation.Reset(data.Transform); }
    /// <summary>Rebases both presentation snapshots without changing the canonical transform.</summary><param name="occluder">Owned identity.</param><param name="transform">Finite transform applied to both snapshots.</param>
    public static void CanvasLightOccluderTransformPhysicsInterpolation(RID occluder, Transform transform)
    { var data = RequireService().OccluderChange(occluder); data.Interpolation.Rebase(transform); }

}
