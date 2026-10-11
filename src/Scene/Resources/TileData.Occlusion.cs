namespace Electron2D;

public sealed partial class TileData
{
    internal sealed class OcclusionSlot
    {
        internal OccluderPolygon? Polygon;
        internal readonly OccluderPolygon?[] Transformed = new OccluderPolygon?[8];
        internal void ClearCache() { for (var i = 1; i < 8; i++) { Transformed[i]?.Dispose(); Transformed[i] = null; } }
    }
    internal readonly List<List<OcclusionSlot>> Occlusion = [];
    private readonly Dictionary<OccluderPolygon, int> _occlusionObservers = [];
    /// <summary>Adds a null polygon slot to a tile-set occlusion layer.</summary><param name="layerID">Zero-based layer.</param>
    public void AddOccluderPolygon(int layerID) { ThrowIfDisposed(); Occlusion[layerID].Add(new()); Change(); }
    /// <summary>Removes a polygon slot without disposing its borrowed resource.</summary><param name="layerID">Zero-based layer.</param><param name="polygonIndex">Zero-based polygon slot.</param>
    public void RemoveOccluderPolygon(int layerID, int polygonIndex)
    { ThrowIfDisposed(); var layer = Occlusion[layerID]; ReleaseOcclusionSlot(layer[polygonIndex]); layer.RemoveAt(polygonIndex); Change(); }
    /// <summary>Returns the number of polygon slots in a layer.</summary><param name="layerID">Zero-based layer.</param>
    public int GetOccluderPolygonsCount(int layerID) { ThrowIfDisposed(); return Occlusion[layerID].Count; }
    /// <summary>Resizes a layer, preserving surviving polygon slots.</summary><param name="layerID">Zero-based layer.</param><param name="polygonsCount">Nonnegative slot count.</param>
    public void SetOccluderPolygonsCount(int layerID, int polygonsCount)
    {
        ThrowIfDisposed(); if (polygonsCount < 0) throw new ArgumentOutOfRangeException(nameof(polygonsCount)); var layer = Occlusion[layerID]; if (layer.Count == polygonsCount) return;
        while (layer.Count > polygonsCount) { ReleaseOcclusionSlot(layer[^1]); layer.RemoveAt(layer.Count - 1); }
        layer.EnsureCapacity(polygonsCount); while (layer.Count < polygonsCount) layer.Add(new()); Change();
    }
    /// <summary>Assigns a borrowed contour to a polygon slot.</summary><param name="layerID">Zero-based layer.</param><param name="polygonIndex">Zero-based slot.</param><param name="polygon">Live borrowed resource, or null.</param>
    public void SetOccluderPolygon(int layerID, int polygonIndex, OccluderPolygon? polygon)
    {
        ThrowIfDisposed(); var slot = Occlusion[layerID][polygonIndex]; if (polygon is { IsDisposed: true }) throw new ObjectDisposedException(nameof(polygon)); if (ReferenceEquals(slot.Polygon, polygon)) return;
        ReleaseOcclusionSlot(slot); slot.Polygon = polygon;
        if (polygon is not null)
        {
            if (_occlusionObservers.TryGetValue(polygon, out var count)) _occlusionObservers[polygon] = count + 1;
            else { _occlusionObservers.Add(polygon, 1); polygon.Changed += OccluderChanged; polygon.Disposed += OccluderDisposed; }
        }
        Change();
    }
    /// <summary>Returns the borrowed original or a data-owned cached transformed contour.</summary>
    /// <remarks>Transpose precedes reflections. Reflections preserve winding, closedness and culling.
    /// Changed or disposed source resources invalidate transformed copies.</remarks>
    /// <param name="layerID">Zero-based layer.</param><param name="polygonIndex">Zero-based slot.</param><param name="flipH">Horizontal reflection.</param><param name="flipV">Vertical reflection.</param><param name="transpose">Exchange axes first.</param>
    public OccluderPolygon? GetOccluderPolygon(int layerID, int polygonIndex, bool flipH = false, bool flipV = false, bool transpose = false)
    {
        ThrowIfDisposed(); var slot = Occlusion[layerID][polygonIndex]; if (slot.Polygon is not { IsDisposed: false } polygon) return null;
        var key = (flipH ? 1 : 0) | (flipV ? 2 : 0) | (transpose ? 4 : 0); if (key == 0) return polygon;
        if (slot.Transformed[key] is { IsDisposed: false } cached) return cached;
        var points = polygon.Polygon;
        for (var i = 0; i < points.Length; i++) { var point = points[i]; if (transpose) point = new(point.Y, point.X); if (flipH) point.X = -point.X; if (flipV) point.Y = -point.Y; points[i] = point; }
        if (flipH ^ flipV ^ transpose) Array.Reverse(points);
        var copy = new OccluderPolygon { Closed = polygon.Closed, CullMode = polygon.CullMode, Polygon = points };
        return slot.Transformed[key] = copy;
    }
    private void ReleaseOcclusionSlot(OcclusionSlot slot)
    {
        if (slot.Polygon is { } polygon && _occlusionObservers.TryGetValue(polygon, out var count))
        {
            if (count > 1) _occlusionObservers[polygon] = count - 1;
            else { _occlusionObservers.Remove(polygon); polygon.Changed -= OccluderChanged; polygon.Disposed -= OccluderDisposed; }
        }
        slot.Polygon = null; slot.ClearCache();
    }
    private void OccluderChanged(Resource polygon)
    { foreach (var layer in Occlusion) foreach (var slot in layer) if (ReferenceEquals(slot.Polygon, polygon)) slot.ClearCache(); Change(); }
    private void OccluderDisposed(ElectronObject polygon)
    {
        foreach (var layer in Occlusion) foreach (var slot in layer) if (ReferenceEquals(slot.Polygon, polygon)) ReleaseOcclusionSlot(slot);
        Change();
    }
    internal void InsertOcclusionLayer(int index) => Occlusion.Insert(index, []);
    internal void RemoveOcclusionLayer(int index) { foreach (var slot in Occlusion[index]) ReleaseOcclusionSlot(slot); Occlusion.RemoveAt(index); }
    internal void ResizeOcclusionLayers(int count)
    { if (count < 0) throw new ArgumentOutOfRangeException(nameof(count)); while (Occlusion.Count > count) RemoveOcclusionLayer(Occlusion.Count - 1); while (Occlusion.Count < count) Occlusion.Add([]); }
    private IEnumerable<PropertyDescriptor> OcclusionProperties<T>(string prefix, Func<T, TileData> data) where T : ElectronObject
    {
        yield return new PropertyDescriptor<T, int>(prefix + "OcclusionLayerCount", n => data(n).Occlusion.Count, (n, v) => data(n).ResizeOcclusionLayers(v), _ => 0, stored: true);
        for (var i = 0; i < Occlusion.Count; i++)
        {
            var id = i;
            yield return new PropertyDescriptor<T, int>(prefix + $"Occlusion/{id}/Count", n => data(n).GetOccluderPolygonsCount(id), (n, v) => data(n).SetOccluderPolygonsCount(id, v), _ => 0, stored: true);
            for (var j = 0; j < Occlusion[i].Count; j++)
            {
                var slot = j;
                yield return new PropertyDescriptor<T, OccluderPolygon?>(prefix + $"Occlusion/{id}/{slot}/Polygon", n => data(n).GetOccluderPolygon(id, slot), (n, v) => data(n).SetOccluderPolygon(id, slot, v), _ => null, stored: true);
            }
        }
    }
}
