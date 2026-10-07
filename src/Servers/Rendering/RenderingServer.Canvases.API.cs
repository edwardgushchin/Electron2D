namespace Electron2D;

public sealed partial class RenderingServer
{
    /// <summary>Creates an empty caller-owned canvas, attachable to multiple active viewports.</summary>
    /// <returns>A stable identity owned until FreeRID or renderer shutdown.</returns>
    /// <exception cref="ArgumentException">An identity or typed value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/capture/lifetime rules reject the operation.</exception>
    public static RID CanvasCreate() => RequireService().CanvasCreateCore();
    /// <summary>Creates a caller-owned retained canvas item with no parent or commands.</summary>
    /// <returns>A stable identity owned until FreeRID or renderer shutdown.</returns>
    /// <exception cref="ArgumentException">An identity or typed value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/capture/lifetime rules reject the operation.</exception>
    public static RID CanvasItemCreate() => RequireService().CanvasItemCreateCore();
    /// <summary>Sets the native render parent to a live canvas/item RID or detaches with an empty identity; scene Node.Parent is unchanged.</summary>
    /// <param name="item">Live scene or caller-owned canvas item identity.</param>
    /// <param name="parent">Live canvas or item RID, or empty to detach without freeing children.</param>
    /// <exception cref="ArgumentException">An identity or typed value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/capture/lifetime rules reject the operation.</exception>
    public static void CanvasItemSetParent(RID item, RID parent) => RequireService().CanvasItemSetParentCore(item, parent);
    /// <summary>Sets a finite native local render transform without changing the scene authoring transform.</summary>
    /// <param name="item">Live scene or caller-owned canvas item identity.</param>
    /// <param name="transform">Finite local transform; null for mesh commands means identity.</param>
    /// <exception cref="ArgumentException">An identity or typed value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/capture/lifetime rules reject the operation.</exception>
    public static void CanvasItemSetTransform(RID item, Transform transform) => RequireService().CanvasItemSetTransformCore(item, transform);
    /// <summary>Sets native visibility, inherited through render parents.</summary>
    /// <param name="item">Live scene or caller-owned canvas item identity.</param>
    /// <param name="visible">Native visibility, inherited by render descendants.</param>
    /// <exception cref="ArgumentException">An identity or typed value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/capture/lifetime rules reject the operation.</exception>
    public static void CanvasItemSetVisible(RID item, bool visible) => RequireService().CanvasItemSetVisibleCore(item, visible);
    /// <summary>Sets finite native modulation inherited by render descendants.</summary>
    /// <param name="item">Live scene or caller-owned canvas item identity.</param>
    /// <param name="color">Finite modulation color; null for mesh commands means opaque white.</param>
    /// <exception cref="ArgumentException">An identity or typed value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/capture/lifetime rules reject the operation.</exception>
    public static void CanvasItemSetModulate(RID item, Color color) => RequireService().CanvasItemSetModulateCore(item, color);
    /// <summary>Sets finite native modulation for this item only.</summary>
    /// <param name="item">Live scene or caller-owned canvas item identity.</param>
    /// <param name="color">Finite modulation color; null for mesh commands means opaque white.</param>
    /// <exception cref="ArgumentException">An identity or typed value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/capture/lifetime rules reject the operation.</exception>
    public static void CanvasItemSetSelfModulate(RID item, Color color) => RequireService().CanvasItemSetSelfModulateCore(item, color);
    /// <summary>Selects drawing before the render parent at equal Z.</summary>
    /// <param name="item">Live scene or caller-owned canvas item identity.</param>
    /// <param name="enabled">Whether the named renderer behavior is enabled.</param>
    /// <exception cref="ArgumentException">An identity or typed value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/capture/lifetime rules reject the operation.</exception>
    public static void CanvasItemSetDrawBehindParent(RID item, bool enabled) => RequireService().CanvasItemSetDrawBehindParentCore(item, enabled);
    /// <summary>Selects stable ascending Y order for render children.</summary>
    /// <param name="item">Live scene or caller-owned canvas item identity.</param>
    /// <param name="enabled">Whether the named renderer behavior is enabled.</param>
    /// <exception cref="ArgumentException">An identity or typed value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/capture/lifetime rules reject the operation.</exception>
    public static void CanvasItemSetSortChildrenByY(RID item, bool enabled) => RequireService().CanvasItemSetSortChildrenByYCore(item, enabled);
    /// <summary>Sets native Z within the canvas item supported range.</summary>
    /// <param name="item">Live scene or caller-owned canvas item identity.</param>
    /// <param name="zIndex">Native Z from -4096 through 4096, inclusive.</param>
    /// <exception cref="ArgumentException">An identity or typed value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/capture/lifetime rules reject the operation.</exception>
    public static void CanvasItemSetZIndex(RID item, int zIndex) => RequireService().CanvasItemSetZIndexCore(item, zIndex);
    /// <summary>Selects clamped render-parent Z accumulation instead of absolute Z.</summary>
    /// <param name="item">Live scene or caller-owned canvas item identity.</param>
    /// <param name="enabled">Whether the named renderer behavior is enabled.</param>
    /// <exception cref="ArgumentException">An identity or typed value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/capture/lifetime rules reject the operation.</exception>
    public static void CanvasItemSetZAsRelativeToParent(RID item, bool enabled) => RequireService().CanvasItemSetZAsRelativeToParentCore(item, enabled);
    /// <summary>Clips this item and its render descendants to its local drawing or custom rectangle.</summary>
    /// <param name="item">Live scene or caller-owned canvas item identity.</param>
    /// <param name="clip">Whether local bounds clip this item and its render descendants.</param>
    /// <exception cref="ArgumentException">An identity or typed value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/capture/lifetime rules reject the operation.</exception>
    public static void CanvasItemSetClip(RID item, bool clip) => RequireService().CanvasItemSetClipCore(item, clip);
    /// <summary>Selects finite custom local bounds or restores automatic command bounds.</summary>
    /// <param name="item">Live scene or caller-owned canvas item identity.</param>
    /// <param name="useCustomRect">True to replace automatic drawing bounds; false to restore them.</param>
    /// <param name="rect">Finite local rectangle used for custom bounds.</param>
    /// <exception cref="ArgumentException">An identity or typed value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/capture/lifetime rules reject the operation.</exception>
    public static void CanvasItemSetCustomRect(RID item, bool useCustomRect, Rect2 rect = default) => RequireService().CanvasItemSetCustomRectCore(item, useCustomRect, rect);
    /// <summary>Returns current custom or automatic local command bounds.</summary>
    /// <param name="item">Live scene or caller-owned canvas item identity.</param>
    /// <returns>The finite local rectangle; empty without drawing commands.</returns>
    /// <exception cref="ArgumentException">An identity or typed value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/capture/lifetime rules reject the operation.</exception>
    public static Rect2 DebugCanvasItemGetRect(RID item) => RequireService().DebugCanvasItemGetRectCore(item);
    /// <summary>Sets native filtering with the shared typed canvas sampler contract.</summary>
    /// <param name="item">Live scene or caller-owned canvas item identity.</param>
    /// <param name="filter">Shared typed filter; ParentNode inherits and Max is rejected.</param>
    /// <exception cref="ArgumentException">An identity or typed value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/capture/lifetime rules reject the operation.</exception>
    public static void CanvasItemSetDefaultTextureFilter(RID item, TextureFilter filter) => RequireService().CanvasItemSetDefaultTextureFilterCore(item, filter);
    /// <summary>Sets native addressing with the shared typed canvas sampler contract.</summary>
    /// <param name="item">Live scene or caller-owned canvas item identity.</param>
    /// <param name="repeat">Shared typed repeat mode; ParentNode inherits and Max is rejected.</param>
    /// <exception cref="ArgumentException">An identity or typed value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/capture/lifetime rules reject the operation.</exception>
    public static void CanvasItemSetDefaultTextureRepeat(RID item, TextureRepeat repeat) => RequireService().CanvasItemSetDefaultTextureRepeatCore(item, repeat);
    /// <summary>Clears retained native commands without freeing resources or the item.</summary>
    /// <param name="item">Live scene or caller-owned canvas item identity.</param>
    /// <exception cref="ArgumentException">An identity or typed value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/capture/lifetime rules reject the operation.</exception>
    public static void CanvasItemClear(RID item) => RequireService().CanvasItemClearCore(item);
    /// <summary>Appends a retained borrowed mesh command with identity transform and white tint by default.</summary>
    /// <param name="item">Live scene or caller-owned canvas item identity.</param>
    /// <param name="mesh">Live borrowed or caller-owned geometry identity.</param>
    /// <param name="transform">Finite local transform; null for mesh commands means identity.</param>
    /// <param name="modulate">Typed finite or bounded value described by this operation.</param>
    /// <param name="texture">Optional live texture identity.</param>
    /// <exception cref="ArgumentException">An identity or typed value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/capture/lifetime rules reject the operation.</exception>
    public static void CanvasItemAddMesh(RID item, RID mesh, Transform? transform = null, Color? modulate = null, RID texture = default) => RequireService().CanvasItemAddMeshCore(item, mesh, transform ?? Transform.Identity, modulate ?? Colors.White, texture);
    /// <summary>Appends a retained borrowed repeated-mesh command, consuming live instance storage.</summary>
    /// <param name="item">Live scene or caller-owned canvas item identity.</param>
    /// <param name="mesh">Live borrowed or caller-owned geometry identity.</param>
    /// <param name="texture">Optional live texture identity.</param>
    /// <exception cref="ArgumentException">An identity or typed value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/capture/lifetime rules reject the operation.</exception>
    public static void CanvasItemAddMultiMesh(RID item, RID mesh, RID texture = default) => RequireService().CanvasItemAddMultiMeshCore(item, mesh, texture);
    /// <summary>Sets finite canvas-wide native modulation.</summary>
    /// <param name="canvas">Live borrowed or caller-owned canvas identity.</param>
    /// <param name="color">Finite modulation color; null for mesh commands means opaque white.</param>
    /// <exception cref="ArgumentException">An identity or typed value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/capture/lifetime rules reject the operation.</exception>
    public static void CanvasSetModulate(RID canvas, Color color) => RequireService().CanvasSetModulateCore(canvas, color);
    /// <summary>Repeats a direct canvas child and its render descendants once along each nonzero finite axis.</summary>
    /// <param name="canvas">Live borrowed or caller-owned canvas identity.</param>
    /// <param name="item">Live scene or caller-owned canvas item identity.</param>
    /// <param name="mirroring">Finite canvas-space period; each nonzero axis adds one repeated root subtree.</param>
    /// <exception cref="ArgumentException">An identity or typed value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/capture/lifetime rules reject the operation.</exception>
    public static void CanvasSetItemMirroring(RID canvas, RID item, Vector2 mirroring) => RequireService().CanvasSetItemMirroringCore(canvas, item, mirroring);
    /// <summary>Attaches a live canvas to an active scene viewport; the same canvas can be attached to multiple viewports.</summary>
    /// <param name="viewport">Live scene viewport identity.</param>
    /// <param name="canvas">Live borrowed or caller-owned canvas identity.</param>
    /// <exception cref="ArgumentException">An identity or typed value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/capture/lifetime rules reject the operation.</exception>
    public static void ViewportAttachCanvas(RID viewport, RID canvas) => RequireService().ViewportAttachCanvasCore(viewport, canvas);
    /// <summary>Removes one view attachment while retaining canvas/item ownership and other attachments.</summary>
    /// <param name="viewport">Live scene viewport identity.</param>
    /// <param name="canvas">Live borrowed or caller-owned canvas identity.</param>
    /// <exception cref="ArgumentException">An identity or typed value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/capture/lifetime rules reject the operation.</exception>
    public static void ViewportRemoveCanvas(RID viewport, RID canvas) => RequireService().ViewportRemoveCanvasCore(viewport, canvas);
    /// <summary>Sets one attached canvas view matrix without rewriting scene viewport or layer authoring.</summary>
    /// <param name="viewport">Live scene viewport identity.</param>
    /// <param name="canvas">Live borrowed or caller-owned canvas identity.</param>
    /// <param name="transform">Finite local transform; null for mesh commands means identity.</param>
    /// <exception cref="ArgumentException">An identity or typed value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/capture/lifetime rules reject the operation.</exception>
    public static void ViewportSetCanvasTransform(RID viewport, RID canvas, Transform transform) => RequireService().ViewportSetCanvasTransformCore(viewport, canvas, transform);
    /// <summary>Sets signed layer/sublayer order for one canvas view before item Z/Y sorting.</summary>
    /// <param name="viewport">Live scene viewport identity.</param>
    /// <param name="canvas">Live borrowed or caller-owned canvas identity.</param>
    /// <param name="layer">Signed canvas stacking layer for this destination.</param>
    /// <param name="sublayer">Signed order within the destination canvas layer.</param>
    /// <exception cref="ArgumentException">An identity or typed value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/capture/lifetime rules reject the operation.</exception>
    public static void ViewportSetCanvasStacking(RID viewport, RID canvas, int layer, int sublayer) => RequireService().ViewportSetCanvasStackingCore(viewport, canvas, layer, sublayer);
    /// <summary>Records a nine-patch using the existing stretch/tile geometry and borrowed texture lifetime.</summary>
    /// <param name="item">Live owned or scene canvas item.</param>
    /// <param name="rect">Finite destination rectangle; negative dimensions reflect the drawing.</param>
    /// <param name="source">Finite texture source rectangle; zero size selects the full texture.</param>
    /// <param name="texture">Borrowed texture identity, or empty for opaque white.</param>
    /// <param name="topLeft">Finite left/top source-pixel margins.</param>
    /// <param name="bottomRight">Finite right/bottom source-pixel margins.</param>
    /// <param name="xAxisMode">Typed horizontal stretch, tile or tile-fit mode.</param>
    /// <param name="yAxisMode">Typed vertical stretch, tile or tile-fit mode.</param>
    /// <param name="drawCenter">Whether the center region draws.</param>
    /// <param name="modulate">Finite command tint; null means opaque white.</param>
    /// <exception cref="ArgumentException">An identity, rectangle, margin or color is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An axis mode is invalid.</exception>
    /// <exception cref="InvalidOperationException">Owner, submission or bounded tiling rules reject the operation.</exception>
    public static void CanvasItemAddNinePatch(RID item, Rect2 rect, Rect2 source, RID texture, Vector2 topLeft, Vector2 bottomRight, AxisStretchMode xAxisMode = AxisStretchMode.Stretch, AxisStretchMode yAxisMode = AxisStretchMode.Stretch, bool drawCenter = true, Color? modulate = null) =>
        RequireService().CanvasItemAddNinePatchCore(item, rect, source, texture, topLeft, bottomRight, xAxisMode, yAxisMode, drawCenter, modulate ?? Colors.White);
}
