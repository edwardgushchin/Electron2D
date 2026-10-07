namespace Electron2D;

public sealed partial class RenderingServer
{
    private CanvasItem CommandTarget(RID rid) { var state = Change(rid, out var item); return item.ServerCommandTarget(state); }
    internal void CanvasItemAddLineCore(RID rid, Vector2 from, Vector2 to, Color color, float width, bool antialiased) { var item = CommandTarget(rid); using var scope = item.StartServerDrawing(); item.DrawLine(from, to, color, width, antialiased); }
    internal void CanvasItemAddPolylineCore(RID rid, ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, float width, bool antialiased, bool connected) { var item = CommandTarget(rid); using var scope = item.StartServerDrawing(); item.RecordServerStroke(points, colors, width, antialiased, connected); }
    internal void CanvasItemAddRectCore(RID rid, Rect2 rect, Color color, bool antialiased) { var item = CommandTarget(rid); using var scope = item.StartServerDrawing(); item.DrawRect(rect, color, antialiased: antialiased); }
    internal void CanvasItemAddCircleCore(RID rid, Vector2 center, float radius, Color color, bool antialiased) { var item = CommandTarget(rid); using var scope = item.StartServerDrawing(); item.DrawCircle(center, radius, color, antialiased: antialiased); }
    internal void CanvasItemAddEllipseCore(RID rid, Vector2 center, float major, float minor, Color color, bool antialiased) { var item = CommandTarget(rid); using var scope = item.StartServerDrawing(); item.DrawEllipse(center, major, minor, color, antialiased: antialiased); }
    internal void CanvasItemAddPolygonCore(RID rid, ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, ReadOnlySpan<Vector2> uvs, RID texture, bool primitive) { var item = CommandTarget(rid); var image = texture.IsValid() ? RenderingTextureRegistry.Resolve(texture) : null; using var scope = item.StartServerDrawing(); if (primitive) item.DrawPrimitive(points, colors, uvs, image); else item.DrawPolygon(points, colors, uvs, image); }
    internal void CanvasItemAddTextureRectCore(RID rid, Rect2 rect, RID texture, bool tile, Color color, bool transpose) { var item = CommandTarget(rid); var image = texture.IsValid() ? RenderingTextureRegistry.Resolve(texture) : RenderingTextureRegistry.WhiteTexture; using var scope = item.StartServerDrawing(); item.RecordTexture(image, rect, null, color, tile, transpose, false); }
    internal void CanvasItemAddTextureRectRegionCore(RID rid, Rect2 rect, RID texture, Rect2 source, Color color, bool transpose, bool clipUV) { var item = CommandTarget(rid); var image = texture.IsValid() ? RenderingTextureRegistry.Resolve(texture) : RenderingTextureRegistry.WhiteTexture; using var scope = item.StartServerDrawing(); item.RecordTexture(image, rect, source, color, false, transpose, clipUV); }
    internal void CanvasItemAddTriangleArrayCore(RID rid, ReadOnlySpan<int> indices, ReadOnlySpan<Vector2> points, ReadOnlySpan<Color> colors, ReadOnlySpan<Vector2> uvs, ReadOnlySpan<int> bones, ReadOnlySpan<float> weights, RID texture, int count) { var item = CommandTarget(rid); var image = texture.IsValid() ? RenderingTextureRegistry.Resolve(texture) : null; using var scope = item.StartServerDrawing(); item.RecordServerTriangles(indices, points, colors, uvs, bones, weights, image, count); }
    internal void CanvasItemAddSetTransformCore(RID rid, Transform transform) { var item = CommandTarget(rid); using var scope = item.StartServerDrawing(); item.DrawSetTransformMatrix(transform); }
    internal void CanvasItemAddAnimationSliceCore(RID rid, double length, double begin, double end, double offset) { var item = CommandTarget(rid); using var scope = item.StartServerDrawing(); item.DrawAnimationSlice(length, begin, end, offset); }
    internal void CanvasItemAddClipIgnoreCore(RID rid, bool ignore) => CommandTarget(rid).RecordServerClipIgnore(ignore);
    internal void CanvasItemSetDrawIndexCore(RID rid, int index) => Change(rid, out _).DrawIndex = index;
    internal void CanvasItemSetVisibilityLayerCore(RID rid, uint layer) => Change(rid, out _).VisibilityLayer = layer;
    internal void CanvasItemSetUseParentMaterialCore(RID rid, bool enabled) => Change(rid, out _).UseParentMaterial = enabled;
    private readonly List<(CanvasItem Node, int Index, int Stable)> _canvasChildOrder = [];
    private readonly Dictionary<RID, int> _canvasRootOrder = [];
    private static void SortCanvasOrder(List<(CanvasItem Node, int Index, int Stable)> entries, int first)
    {
        System.Runtime.InteropServices.CollectionsMarshal.AsSpan(entries)[first..].Sort(static (left, right) =>
        { var result = (left.Node.ServerState?.DrawIndex ?? left.Index).CompareTo(right.Node.ServerState?.DrawIndex ?? right.Index); return result != 0 ? result : left.Stable.CompareTo(right.Stable); });
    }
    private int CollectCanvasChildren(CanvasItem parent)
    {
        var first = _canvasChildOrder.Count;
        for (var i = 0; i < parent.GetChildCount(includeInternal: true); i++)
            if (parent.GetChild(i, includeInternal: true) is CanvasItem child && ReferenceEquals(child.RenderParent, parent)) _canvasChildOrder.Add((child, i, _canvasChildOrder.Count));
        if (parent.ServerState is { } state) foreach (var weak in state.Children)
                if (weak.TryGetTarget(out var child) && !child.IsDisposed && ReferenceEquals(child.RenderParent, parent) && !ReferenceEquals(child.GetParentItem(), parent)) _canvasChildOrder.Add((child, child.ServerState is { Owned: true } ? 0 : child.GetIndex(includeInternal: true), _canvasChildOrder.Count));
        SortCanvasOrder(_canvasChildOrder, first); return first;
    }
    internal void PublishSceneDrawOrder(Node parent)
    {
        if (!ReferenceEquals(parent.Tree, _window.Tree)) return;
        // ponytail: only registered source overrides are scanned on structural edits; index descendants if measured authoring cost warrants it.
        foreach (var weak in _serverSceneItems) if (weak.TryGetTarget(out var item) && !item.IsDisposed && item.ServerState?.DrawIndex is not null && parent.IsAncestorOf(item)) item.ServerState.DrawIndex = null;
    }
}
