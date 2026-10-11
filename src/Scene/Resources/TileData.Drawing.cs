namespace Electron2D;

public sealed partial class TileData
{
    private Material? _material;
    private int _zIndex, _ySortOrigin;
    /// <summary>Gets or sets the borrowed material used to draw this tile; null inherits the layer's material.</summary>
    /// <value>Null by default.</value>
    /// <remarks>Assignments emit Changed, including equal assignments. Disposing tile data does not dispose this resource.
    /// A disposed borrowed material remains readable but cannot be rendered until replaced. Shader materials require a capable renderer.</remarks>
    /// <exception cref="ObjectDisposedException">This data or the assigned material is disposed.</exception>
    public Material? Material { get { ThrowIfDisposed(); return _material; } set { ThrowIfDisposed(); if (value is { IsDisposed: true }) throw new ObjectDisposedException(nameof(value)); _material = value; Change(); } }
    /// <summary>Gets or sets this tile's drawing Z offset relative to its layer.</summary>
    /// <value>Zero by default, bounded by the canvas minimum and maximum Z indices.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value exceeds the canvas Z range.</exception>
    public int ZIndex { get { ThrowIfDisposed(); return _zIndex; } set { ThrowIfDisposed(); if (value is < CanvasItem.MinimumZIndex or > CanvasItem.MaximumZIndex) throw new ArgumentOutOfRangeException(nameof(value)); _zIndex = value; Change(); } }
    /// <summary>Gets or sets the tile's Y sorting anchor offset in scene units; image and physics positions are unchanged.</summary>
    /// <value>Zero by default; used when the layer enables Y sorting.</value>
    public int YSortOrigin { get { ThrowIfDisposed(); return _ySortOrigin; } set { ThrowIfDisposed(); _ySortOrigin = value; Change(); } }
    private IEnumerable<PropertyDescriptor> DrawingProperties<T>(string prefix, Func<T, TileData> data) where T : ElectronObject
    {
        yield return new PropertyDescriptor<T, Material?>(prefix + nameof(Material), n => data(n).Material, (n, v) => data(n).Material = v, _ => null, stored: true);
        yield return new PropertyDescriptor<T, int>(prefix + nameof(ZIndex), n => data(n).ZIndex, (n, v) => data(n).ZIndex = v, _ => 0, stored: true);
        yield return new PropertyDescriptor<T, int>(prefix + nameof(YSortOrigin), n => data(n).YSortOrigin, (n, v) => data(n).YSortOrigin = v, _ => 0, stored: true);
    }
}
