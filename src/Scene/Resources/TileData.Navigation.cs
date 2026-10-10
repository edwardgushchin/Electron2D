namespace Electron2D;

public sealed partial class TileData
{
    internal sealed class NavigationLayer
    {
        internal NavigationPolygon? Polygon;
        internal readonly NavigationPolygon?[] Transformed = new NavigationPolygon?[8];
        internal Action<Resource>? Changed;
        internal Action<ElectronObject>? Disposed;
        internal void ClearCache() { for (var i = 1; i < 8; i++) { Transformed[i]?.Dispose(); Transformed[i] = null; } }
        internal void Release() { if (Polygon is not null) { Polygon.Changed -= Changed; Polygon.Disposed -= Disposed; } Polygon = null; ClearCache(); }
    }
    internal readonly List<NavigationLayer> Navigation = [];
    /// <summary>Assigns a borrowed navigation polygon, or null, to a navigation layer.</summary>
    /// <param name="layerID">Zero-based tile-set navigation layer.</param>
    /// <param name="navigationPolygon">Live borrowed polygon; changes and disposal invalidate transformed copies.</param>
    public void SetNavigationPolygon(int layerID, NavigationPolygon? navigationPolygon)
    {
        ThrowIfDisposed(); var layer = Navigation[layerID]; if (navigationPolygon is not null) ObjectDisposedException.ThrowIf(navigationPolygon.IsDisposed, navigationPolygon);
        if (ReferenceEquals(layer.Polygon, navigationPolygon)) return; layer.Release(); layer.Polygon = navigationPolygon;
        if (navigationPolygon is not null)
        {
            layer.Changed = _ => { layer.ClearCache(); Change(); };
            layer.Disposed = _ => { layer.Release(); Change(); };
            navigationPolygon.Changed += layer.Changed; navigationPolygon.Disposed += layer.Disposed;
        }
        Change();
    }
    /// <summary>Returns the borrowed original or cached transformed polygon, or null.</summary>
    /// <remarks>Transpose runs before reflections. Outline winding and polygon winding are preserved.
    /// Transformed resources are owned by this data and invalidated by original changes or data removal.</remarks>
    /// <param name="layerID">Zero-based tile-set navigation layer.</param>
    /// <param name="flipH">Reflects the horizontal axis.</param>
    /// <param name="flipV">Reflects the vertical axis.</param>
    /// <param name="transpose">Exchanges the axes before reflection.</param>
    public NavigationPolygon? GetNavigationPolygon(int layerID, bool flipH = false, bool flipV = false, bool transpose = false)
    {
        ThrowIfDisposed(); var layer = Navigation[layerID]; if (layer.Polygon is not { IsDisposed: false } polygon) return null;
        var key = (flipH ? 1 : 0) | (flipV ? 2 : 0) | (transpose ? 4 : 0); if (key == 0) return polygon;
        if (layer.Transformed[key] is { IsDisposed: false } cached) return cached;
        return layer.Transformed[key] = polygon.TransformedCopy(flipH, flipV, transpose);
    }
    internal void InsertNavigationLayer(int index) => Navigation.Insert(index, new());
    internal void RemoveNavigationLayer(int index) { Navigation[index].Release(); Navigation.RemoveAt(index); }
    internal void ResizeNavigationLayers(int count)
    { if (count < 0) throw new ArgumentOutOfRangeException(nameof(count)); while (Navigation.Count > count) RemoveNavigationLayer(Navigation.Count - 1); while (Navigation.Count < count) Navigation.Add(new()); }
    private IEnumerable<PropertyDescriptor> NavigationProperties<T>(string prefix, Func<T, TileData> data) where T : ElectronObject
    {
        yield return new PropertyDescriptor<T, int>(prefix + "NavigationLayerCount", n => data(n).Navigation.Count, (n, v) => data(n).ResizeNavigationLayers(v), _ => 0, stored: true);
        for (var i = 0; i < Navigation.Count; i++)
        {
            var id = i;
            yield return new PropertyDescriptor<T, NavigationPolygon?>(prefix + $"Navigation/{id}/Polygon", n => data(n).GetNavigationPolygon(id), (n, v) => data(n).SetNavigationPolygon(id, v), _ => null, stored: true);
        }
    }
}
