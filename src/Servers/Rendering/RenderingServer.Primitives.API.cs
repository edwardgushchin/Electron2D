namespace Electron2D;

public sealed partial class RenderingServer
{
    /// <summary>Records a copied local line with finite color/width and optional antialiasing.</summary>
    /// <param name="item">Live scene or caller-owned item RID.</param>
    /// <param name="from">Finite local starting point.</param>
    /// <param name="to">Finite local ending point.</param>
    /// <param name="color">Finite command color.</param>
    /// <param name="width">Finite local width; negative produces framebuffer-width lines.</param>
    /// <param name="antialiased">Whether the retained geometry adds antialiasing feathers.</param>
    /// <remarks>Uses copied retained commands in the active owner session. Source-node getters, input and processing remain authored; source redraw replaces native command edits.</remarks>
    /// <exception cref="ArgumentException">A live identity, finite value or geometry/channel count is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static void CanvasItemAddLine(RID item, Vector2 from, Vector2 to, Color color, float width = -1f, bool antialiased = false) => RequireService().CanvasItemAddLineCore(item, from, to, color, width, antialiased);

    /// <summary>Records a connected copied polyline; colors are empty, uniform or per vertex.</summary>
    /// <param name="item">Live scene or caller-owned item RID.</param>
    /// <param name="points">Copied finite local vertices, within the supported geometry bound.</param>
    /// <param name="colors">Copied finite vertex, endpoint or uniform colors as described by the operation.</param>
    /// <param name="width">Finite local width; negative produces framebuffer-width lines.</param>
    /// <param name="antialiased">Whether the retained geometry adds antialiasing feathers.</param>
    /// <remarks>Uses copied retained commands in the active owner session. Source-node getters, input and processing remain authored; source redraw replaces native command edits.</remarks>
    /// <exception cref="ArgumentException">A live identity, finite value or geometry/channel count is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static void CanvasItemAddPolyline(RID item, ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, float width = -1f, bool antialiased = false) => RequireService().CanvasItemAddPolylineCore(item, points, colors, width, antialiased, true);

    /// <summary>Records copied endpoint pairs; colors can be empty, uniform, per segment or per endpoint.</summary>
    /// <param name="item">Live scene or caller-owned item RID.</param>
    /// <param name="points">Copied finite local vertices, within the supported geometry bound.</param>
    /// <param name="colors">Copied finite vertex, endpoint or uniform colors as described by the operation.</param>
    /// <param name="width">Finite local width; negative produces framebuffer-width lines.</param>
    /// <param name="antialiased">Whether the retained geometry adds antialiasing feathers.</param>
    /// <remarks>Uses copied retained commands in the active owner session. Source-node getters, input and processing remain authored; source redraw replaces native command edits.</remarks>
    /// <exception cref="ArgumentException">A live identity, finite value or geometry/channel count is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static void CanvasItemAddMultiline(RID item, ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, float width = -1f, bool antialiased = false) => RequireService().CanvasItemAddPolylineCore(item, points, colors, width, antialiased, false);

    /// <summary>Records a filled rectangle with optional antialias feather geometry.</summary>
    /// <param name="item">Live scene or caller-owned item RID.</param>
    /// <param name="rect">Finite local destination rectangle.</param>
    /// <param name="color">Finite command color.</param>
    /// <param name="antialiased">Whether the retained geometry adds antialiasing feathers.</param>
    /// <remarks>Uses copied retained commands in the active owner session. Source-node getters, input and processing remain authored; source redraw replaces native command edits.</remarks>
    /// <exception cref="ArgumentException">A live identity, finite value or geometry/channel count is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static void CanvasItemAddRect(RID item, Rect2 rect, Color color, bool antialiased = false) => RequireService().CanvasItemAddRectCore(item, rect, color, antialiased);

    /// <summary>Records a filled circle through the retained 64-segment ellipse kernel.</summary>
    /// <param name="item">Live scene or caller-owned item RID.</param>
    /// <param name="center">Finite local center.</param>
    /// <param name="radius">Finite circle radius.</param>
    /// <param name="color">Finite command color.</param>
    /// <param name="antialiased">Whether the retained geometry adds antialiasing feathers.</param>
    /// <remarks>Uses copied retained commands in the active owner session. Source-node getters, input and processing remain authored; source redraw replaces native command edits.</remarks>
    /// <exception cref="ArgumentException">A live identity, finite value or geometry/channel count is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static void CanvasItemAddCircle(RID item, Vector2 center, float radius, Color color, bool antialiased = false) => RequireService().CanvasItemAddCircleCore(item, center, radius, color, antialiased);

    /// <summary>Records a filled local ellipse with major/minor radii and optional antialiasing.</summary>
    /// <param name="item">Live scene or caller-owned item RID.</param>
    /// <param name="center">Finite local center.</param>
    /// <param name="major">Finite horizontal ellipse radius.</param>
    /// <param name="minor">Finite vertical ellipse radius.</param>
    /// <param name="color">Finite command color.</param>
    /// <param name="antialiased">Whether the retained geometry adds antialiasing feathers.</param>
    /// <remarks>Uses copied retained commands in the active owner session. Source-node getters, input and processing remain authored; source redraw replaces native command edits.</remarks>
    /// <exception cref="ArgumentException">A live identity, finite value or geometry/channel count is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static void CanvasItemAddEllipse(RID item, Vector2 center, float major, float minor, Color color, bool antialiased = false) => RequireService().CanvasItemAddEllipseCore(item, center, major, minor, color, antialiased);

    /// <summary>Copies and triangulates a simple contour with white/uniform/per-vertex color and optional borrowed texture.</summary>
    /// <param name="item">Live scene or caller-owned item RID.</param>
    /// <param name="points">Copied finite local vertices, within the supported geometry bound.</param>
    /// <param name="colors">Copied finite vertex, endpoint or uniform colors as described by the operation.</param>
    /// <param name="uvs">Copied finite normalized texture coordinates, or empty for zero.</param>
    /// <param name="texture">Borrowed live texture RID; empty means white for rectangles or untextured geometry.</param>
    /// <remarks>Uses copied retained commands in the active owner session. Source-node getters, input and processing remain authored; source redraw replaces native command edits.</remarks>
    /// <exception cref="ArgumentException">A live identity, finite value or geometry/channel count is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static void CanvasItemAddPolygon(RID item, ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, ReadOnlySpan<Vector2> uvs = default, RID texture = default) => RequireService().CanvasItemAddPolygonCore(item, points, colors, uvs, texture, false);

    /// <summary>Copies one through four points; missing color/UV values follow the existing primitive contract.</summary>
    /// <param name="item">Live scene or caller-owned item RID.</param>
    /// <param name="points">Copied finite local vertices, within the supported geometry bound.</param>
    /// <param name="colors">Copied finite vertex, endpoint or uniform colors as described by the operation.</param>
    /// <param name="uvs">Copied finite normalized texture coordinates, or empty for zero.</param>
    /// <param name="texture">Borrowed live texture RID; empty means white for rectangles or untextured geometry.</param>
    /// <remarks>Uses copied retained commands in the active owner session. Source-node getters, input and processing remain authored; source redraw replaces native command edits.</remarks>
    /// <exception cref="ArgumentException">A live identity, finite value or geometry/channel count is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static void CanvasItemAddPrimitive(RID item, ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, ReadOnlySpan<Vector2> uvs, RID texture) => RequireService().CanvasItemAddPolygonCore(item, points, colors, uvs, texture, true);

    /// <summary>Records a borrowed texture rectangle with tiling, modulation, flips and transpose.</summary>
    /// <param name="item">Live scene or caller-owned item RID.</param>
    /// <param name="rect">Finite local destination rectangle.</param>
    /// <param name="texture">Borrowed live texture RID; empty means white for rectangles or untextured geometry.</param>
    /// <param name="tile">Whether the texture repeats across the destination.</param>
    /// <param name="modulate">Finite command tint; null means white.</param>
    /// <param name="transpose">Whether source axes transpose.</param>
    /// <remarks>Uses copied retained commands in the active owner session. Source-node getters, input and processing remain authored; source redraw replaces native command edits.</remarks>
    /// <exception cref="ArgumentException">A live identity, finite value or geometry/channel count is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static void CanvasItemAddTextureRect(RID item, Rect2 rect, RID texture, bool tile = false, Color? modulate = null, bool transpose = false) => RequireService().CanvasItemAddTextureRectCore(item, rect, texture, tile, modulate ?? Colors.White, transpose);

    /// <summary>Records a borrowed texture source region with optional transpose and UV clipping.</summary>
    /// <param name="item">Live scene or caller-owned item RID.</param>
    /// <param name="rect">Finite local destination rectangle.</param>
    /// <param name="texture">Borrowed live texture RID; empty means white for rectangles or untextured geometry.</param>
    /// <param name="source">Finite texture source rectangle.</param>
    /// <param name="modulate">Finite command tint; null means white.</param>
    /// <param name="transpose">Whether source axes transpose.</param>
    /// <param name="clipUV">Whether UVs stay inside source-region texel centers.</param>
    /// <remarks>Uses copied retained commands in the active owner session. Source-node getters, input and processing remain authored; source redraw replaces native command edits.</remarks>
    /// <exception cref="ArgumentException">A live identity, finite value or geometry/channel count is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static void CanvasItemAddTextureRectRegion(RID item, Rect2 rect, RID texture, Rect2 source, Color? modulate = null, bool transpose = false, bool clipUV = true) => RequireService().CanvasItemAddTextureRectRegionCore(item, rect, texture, source, modulate ?? Colors.White, transpose, clipUV);

    /// <summary>Copies indexed triangles and optional four-slot skin channels; count selects an index prefix in triangles.</summary>
    /// <param name="item">Live scene or caller-owned item RID.</param>
    /// <param name="indices">Copied vertex indices; only the selected complete-triangle prefix is consumed.</param>
    /// <param name="points">Copied finite local vertices, within the supported geometry bound.</param>
    /// <param name="colors">Copied finite vertex, endpoint or uniform colors as described by the operation.</param>
    /// <param name="uvs">Copied finite normalized texture coordinates, or empty for zero.</param>
    /// <param name="bones">Empty or four unsigned-16-compatible indices per vertex; an absent channel defaults to zero.</param>
    /// <param name="weights">Empty or four finite weights per vertex, clamped/truncated to UNORM16; absent weights default to zero.</param>
    /// <param name="texture">Borrowed live texture RID; empty means white for rectangles or untextured geometry.</param>
    /// <param name="count">Negative selects all indices; nonnegative selects count times three indices. Without indices, count must be nonpositive and complete vertex triples draw.</param>
    /// <remarks>Uses copied retained commands in the active owner session. Source-node getters, input and processing remain authored; source redraw replaces native command edits.</remarks>
    /// <exception cref="ArgumentException">A live identity, finite value or geometry/channel count is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static void CanvasItemAddTriangleArray(RID item, ReadOnlySpan<int> indices, ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, ReadOnlySpan<Vector2> uvs = default, ReadOnlySpan<int> bones = default, ReadOnlySpan<float> weights = default, RID texture = default, int count = -1) => RequireService().CanvasItemAddTriangleArrayCore(item, indices, points, colors, uvs, bones, weights, texture, count);

    /// <summary>Records a finite drawing transform for subsequent visible commands.</summary>
    /// <param name="item">Live scene or caller-owned item RID.</param>
    /// <param name="transform">Finite command-local drawing transform.</param>
    /// <remarks>Uses copied retained commands in the active owner session. Source-node getters, input and processing remain authored; source redraw replaces native command edits.</remarks>
    /// <exception cref="ArgumentException">A live identity, finite value or geometry/channel count is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static void CanvasItemAddSetTransform(RID item, Transform transform) => RequireService().CanvasItemAddSetTransformCore(item, transform);

    /// <summary>Records a finite repeating half-open interval controlling subsequent commands.</summary>
    /// <param name="item">Live scene or caller-owned item RID.</param>
    /// <param name="animationLength">Finite repeating phase length in seconds.</param>
    /// <param name="sliceBegin">Finite inclusive phase boundary.</param>
    /// <param name="sliceEnd">Finite exclusive phase boundary.</param>
    /// <param name="offset">Finite phase origin in seconds.</param>
    /// <remarks>Uses copied retained commands in the active owner session. Source-node getters, input and processing remain authored; source redraw replaces native command edits.</remarks>
    /// <exception cref="ArgumentException">A live identity, finite value or geometry/channel count is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static void CanvasItemAddAnimationSlice(RID item, double animationLength, double sliceBegin, double sliceEnd, double offset = 0d) => RequireService().CanvasItemAddAnimationSliceCore(item, animationLength, sliceBegin, sliceEnd, offset);

    /// <summary>Records whether subsequent commands bypass rectangular inherited/own clipping; alpha masks still apply.</summary>
    /// <param name="item">Live scene or caller-owned item RID.</param>
    /// <param name="ignore">True to bypass rectangular clipping, false to restore it.</param>
    /// <remarks>Uses copied retained commands in the active owner session. Source-node getters, input and processing remain authored; source redraw replaces native command edits.</remarks>
    /// <exception cref="ArgumentException">A live identity, finite value or geometry/channel count is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static void CanvasItemAddClipIgnore(RID item, bool ignore) => RequireService().CanvasItemAddClipIgnoreCore(item, ignore);

    /// <summary>Sets signed stable order among equal-Y/equal-Z render siblings without changing Node order.</summary>
    /// <param name="item">Live scene or caller-owned item RID.</param>
    /// <param name="index">Signed renderer order, with stable ties.</param>
    /// <remarks>Uses copied retained commands in the active owner session. Source-node getters, input and processing remain authored; source redraw replaces native command edits.</remarks>
    /// <exception cref="ArgumentException">A live identity, finite value or geometry/channel count is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static void CanvasItemSetDrawIndex(RID item, int index) => RequireService().CanvasItemSetDrawIndexCore(item, index);

    /// <summary>Sets the native 32-bit visibility mask used by each destination viewport without changing authored getters.</summary>
    /// <param name="item">Live scene or caller-owned item RID.</param>
    /// <param name="layer">Native 32-bit mask; zero culls geometry.</param>
    /// <remarks>Uses copied retained commands in the active owner session. Source-node getters, input and processing remain authored; source redraw replaces native command edits.</remarks>
    /// <exception cref="ArgumentException">A live identity, finite value or geometry/channel count is invalid.</exception>
    /// <exception cref="InvalidOperationException">The service is unavailable or owner/submission/lifetime rules reject the operation.</exception>
    public static void CanvasItemSetVisibilityLayer(RID item, uint layer) => RequireService().CanvasItemSetVisibilityLayerCore(item, layer);

    /// <summary>Selects material inheritance from the native render parent without changing the authored flag.</summary>
    /// <param name="item">Live owned or source canvas item RID.</param>
    /// <param name="enabled">True to inherit the native parent's borrowed material; false selects the item's own material.</param>
    /// <exception cref="ArgumentException">The identity is absent or disposed.</exception>
    /// <exception cref="InvalidOperationException">Ownership, unavailable service or submission rules reject the operation.</exception>
    public static void CanvasItemSetUseParentMaterial(RID item, bool enabled) => RequireService().CanvasItemSetUseParentMaterialCore(item, enabled);
}
