namespace Electron2D;

/// <summary>Stores the visual transform and collision polygons of one atlas alternative.</summary>
/// <remarks>Instances are owned by an atlas source. Polygon arrays are copied on reads and writes.
/// Removing an alternative invalidates its borrowed data. Runtime update callbacks receive a separate owned copy.</remarks>
public sealed class TileData : ElectronObject
{
    internal sealed class Polygon
    {
        internal Vector2[] Points = [];
        internal bool OneWay;
        internal float Margin = 1;
        internal Polygon Copy() => new() { Points = (Vector2[])Points.Clone(), OneWay = OneWay, Margin = Margin };
    }
    internal sealed class PhysicsLayer
    {
        internal readonly List<Polygon> Polygons = [];
        internal Vector2 LinearVelocity;
        internal float AngularVelocity;
        internal PhysicsLayer Copy()
        {
            var copy = new PhysicsLayer { LinearVelocity = LinearVelocity, AngularVelocity = AngularVelocity };
            foreach (var polygon in Polygons) copy.Polygons.Add(polygon.Copy());
            return copy;
        }
    }
    internal readonly List<PhysicsLayer> Layers = [];
    private readonly bool _allowTransform;
    private int _releaseThread;
    private bool _flipH, _flipV, _transpose;
    private Vector2i _textureOrigin;
    private Color _modulate = Colors.White;
    internal TileData(bool allowTransform) => _allowTransform = allowTransform;

    /// <summary>Occurs after authored data changes.</summary>
    public event Action? Changed;
    /// <summary>Gets or sets horizontal reflection; base alternatives cannot be reflected in their data.</summary>
    public bool FlipH { get { ThrowIfDisposed(); return _flipH; } set { TransformAllowed(value); if (_flipH == value) return; _flipH = value; Change(); } }
    /// <summary>Gets or sets vertical reflection; base alternatives cannot be reflected in their data.</summary>
    public bool FlipV { get { ThrowIfDisposed(); return _flipV; } set { TransformAllowed(value); if (_flipV == value) return; _flipV = value; Change(); } }
    /// <summary>Gets or sets whether tile axes are exchanged before reflection.</summary>
    public bool Transpose { get { ThrowIfDisposed(); return _transpose; } set { TransformAllowed(value); if (_transpose == value) return; _transpose = value; Change(); } }
    /// <summary>Gets or sets the texture origin offset in pixels; collision geometry is unaffected.</summary>
    public Vector2i TextureOrigin { get { ThrowIfDisposed(); return _textureOrigin; } set { ThrowIfDisposed(); if (_textureOrigin == value) return; _textureOrigin = value; Change(); } }
    /// <summary>Gets or sets the finite texture tint, white by default.</summary>
    public Color Modulate { get { ThrowIfDisposed(); return _modulate; } set { ThrowIfDisposed(); if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value)); if (_modulate == value) return; _modulate = value; Change(); } }

    /// <summary>Adds an empty collision polygon to a physics layer.</summary>
    /// <param name="layerID">Zero-based tile-set physics layer index.</param>
    public void AddCollisionPolygon(int layerID) { Layer(layerID).Polygons.Add(new()); Change(); }
    /// <summary>Removes a polygon and shifts later indices down.</summary>
    /// <param name="layerID">Zero-based tile-set physics layer index.</param>
    /// <param name="polygonIndex">Zero-based polygon index in the physics layer.</param>
    public void RemoveCollisionPolygon(int layerID, int polygonIndex) { Layer(layerID).Polygons.RemoveAt(polygonIndex); Change(); }
    /// <summary>Returns a physics layer's polygon count.</summary>
    /// <param name="layerID">Zero-based tile-set physics layer index.</param>
    /// <returns>Returns a physics layer's polygon count.</returns>
    public int GetCollisionPolygonsCount(int layerID) => Layer(layerID).Polygons.Count;
    /// <summary>Resizes a physics layer's polygon list, preserving surviving entries.</summary>
    /// <param name="layerID">Zero-based tile-set physics layer index.</param>
    /// <param name="polygonsCount">Nonnegative polygon count.</param>
    public void SetCollisionPolygonsCount(int layerID, int polygonsCount)
    {
        if (polygonsCount < 0) throw new ArgumentOutOfRangeException(nameof(polygonsCount));
        var polygons = Layer(layerID).Polygons;
        if (polygons.Count == polygonsCount) return;
        if (polygonsCount < polygons.Count) polygons.RemoveRange(polygonsCount, polygons.Count - polygonsCount);
        else { polygons.EnsureCapacity(polygonsCount); while (polygons.Count < polygonsCount) polygons.Add(new()); }
        Change();
    }
    /// <summary>Returns an independent polygon contour in tile-local scene units.</summary>
    /// <param name="layerID">Zero-based tile-set physics layer index.</param>
    /// <param name="polygonIndex">Zero-based polygon index in the physics layer.</param>
    /// <returns>Returns an independent polygon contour in tile-local scene units.</returns>
    public Vector2[] GetCollisionPolygonPoints(int layerID, int polygonIndex) => (Vector2[])Layer(layerID).Polygons[polygonIndex].Points.Clone();
    /// <summary>Copies a finite closed contour; empty or invalid topology contributes no fixtures.</summary>
    /// <param name="layerID">Zero-based tile-set physics layer index.</param>
    /// <param name="polygonIndex">Zero-based polygon index in the physics layer.</param>
    /// <param name="polygon">Copied finite tile-local contour, empty or containing at least three points.</param>
    public void SetCollisionPolygonPoints(int layerID, int polygonIndex, Vector2[] polygon)
    {
        var target = Layer(layerID).Polygons[polygonIndex];
        ArgumentNullException.ThrowIfNull(polygon);
        if (polygon.Length is 1 or 2) throw new ArgumentException("A contour is empty or has at least three points.", nameof(polygon));
        var copy = (Vector2[])polygon.Clone();
        foreach (var point in copy) if (!point.IsFinite()) throw new ArgumentException("Polygon points must be finite.", nameof(polygon));
        if (copy.Length > 0)
        {
            var min = copy[0]; var max = min;
            foreach (var point in copy) { min = min.Min(point); max = max.Max(point); }
            if (!(max - min).IsFinite()) throw new ArgumentException("Polygon bounds must be finite.", nameof(polygon));
        }
        target.Points = copy; Change();
    }
    /// <summary>Tests whether a polygon accepts contacts only from above the layer.</summary>
    /// <param name="layerID">Zero-based tile-set physics layer index.</param>
    /// <param name="polygonIndex">Zero-based polygon index in the physics layer.</param>
    /// <returns>Tests whether a polygon accepts contacts only from above the layer.</returns>
    public bool IsCollisionPolygonOneWay(int layerID, int polygonIndex) => Layer(layerID).Polygons[polygonIndex].OneWay;
    /// <summary>Sets a polygon's one-way contact policy.</summary>
    /// <param name="layerID">Zero-based tile-set physics layer index.</param>
    /// <param name="polygonIndex">Zero-based polygon index in the physics layer.</param>
    /// <param name="oneWay">Whether to enable the one-way contact policy.</param>
    public void SetCollisionPolygonOneWay(int layerID, int polygonIndex, bool oneWay) { var p = Layer(layerID).Polygons[polygonIndex]; if (p.OneWay == oneWay) return; p.OneWay = oneWay; Change(); }
    /// <summary>Returns the one-way recovery margin in scene units.</summary>
    /// <param name="layerID">Zero-based tile-set physics layer index.</param>
    /// <param name="polygonIndex">Zero-based polygon index in the physics layer.</param>
    /// <returns>Returns the one-way recovery margin in scene units.</returns>
    public float GetCollisionPolygonOneWayMargin(int layerID, int polygonIndex) => Layer(layerID).Polygons[polygonIndex].Margin;
    /// <summary>Sets a finite nonnegative one-way recovery margin.</summary>
    /// <param name="layerID">Zero-based tile-set physics layer index.</param>
    /// <param name="polygonIndex">Zero-based polygon index in the physics layer.</param>
    /// <param name="oneWayMargin">Finite nonnegative recovery margin in scene units.</param>
    public void SetCollisionPolygonOneWayMargin(int layerID, int polygonIndex, float oneWayMargin)
    { var p = Layer(layerID).Polygons[polygonIndex]; Finite(oneWayMargin); if (oneWayMargin < 0) throw new ArgumentOutOfRangeException(nameof(oneWayMargin)); if (p.Margin == oneWayMargin) return; p.Margin = oneWayMargin; Change(); }
    /// <summary>Returns the constant world-space surface velocity for this physics layer.</summary>
    /// <param name="layerID">Zero-based tile-set physics layer index.</param>
    /// <returns>Returns the constant world-space surface velocity for this physics layer.</returns>
    public Vector2 GetConstantLinearVelocity(int layerID) => Layer(layerID).LinearVelocity;
    /// <summary>Sets a finite world-space surface velocity without moving static tile geometry.</summary>
    /// <param name="layerID">Zero-based tile-set physics layer index.</param>
    /// <param name="velocity">Finite surface velocity in scene units per second, or radians per second for angular velocity.</param>
    public void SetConstantLinearVelocity(int layerID, Vector2 velocity) { var layer = Layer(layerID); if (!velocity.IsFinite()) throw new ArgumentOutOfRangeException(nameof(velocity)); if (layer.LinearVelocity == velocity) return; layer.LinearVelocity = velocity; Change(); }
    /// <summary>Returns the constant angular surface velocity in radians per second.</summary>
    /// <param name="layerID">Zero-based tile-set physics layer index.</param>
    /// <returns>Returns the constant angular surface velocity in radians per second.</returns>
    public float GetConstantAngularVelocity(int layerID) => Layer(layerID).AngularVelocity;
    /// <summary>Sets a finite angular surface velocity in radians per second.</summary>
    /// <param name="layerID">Zero-based tile-set physics layer index.</param>
    /// <param name="velocity">Finite surface velocity in scene units per second, or radians per second for angular velocity.</param>
    public void SetConstantAngularVelocity(int layerID, float velocity) { var layer = Layer(layerID); Finite(velocity); if (layer.AngularVelocity == velocity) return; layer.AngularVelocity = velocity; Change(); }

    private PhysicsLayer Layer(int index) { ThrowIfDisposed(); return Layers[index]; }
    private static void Finite(float value) { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); }
    private void TransformAllowed(bool value) { ThrowIfDisposed(); if (value && !_allowTransform) throw new InvalidOperationException("Transform flags belong to alternative tiles or cells."); }
    private void Change() { NotifyPropertyListChanged(); Changed?.Invoke(); }
    internal void ResizeLayers(int count)
    {
        ThrowIfDisposed(); if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
        if (count < Layers.Count) Layers.RemoveRange(count, Layers.Count - count);
        else { Layers.EnsureCapacity(count); while (Layers.Count < count) Layers.Add(new()); }
    }
    internal TileData Copy()
    {
        ThrowIfDisposed(); var copy = new TileData(_allowTransform) { _flipH = _flipH, _flipV = _flipV, _transpose = _transpose, _textureOrigin = _textureOrigin, _modulate = _modulate };
        foreach (var layer in Layers) copy.Layers.Add(layer.Copy());
        return copy;
    }
    internal void Release()
    {
        _releaseThread = Environment.CurrentManagedThreadId;
        try { Dispose(); } finally { _releaseThread = 0; }
    }
    /// <summary>Prevents disposal of data borrowed from an atlas or a runtime update.</summary>
    /// <exception cref="InvalidOperationException">The caller does not own this data.</exception>
    protected override void ValidateDisposal()
    {
        if (_releaseThread != Environment.CurrentManagedThreadId) throw new InvalidOperationException("Tile data is owned by its atlas source or runtime layer.");
        base.ValidateDisposal();
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { Changed = null; Layers.Clear(); } base.Dispose(disposing); }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(StoredProperties<TileData>("", static p => p));

    internal IEnumerable<PropertyDescriptor> StoredProperties<T>(string prefix, Func<T, TileData> data) where T : ElectronObject
    {
        yield return new PropertyDescriptor<T, bool>(prefix + nameof(FlipH), n => data(n).FlipH, (n, v) => data(n).FlipH = v, _ => false, stored: true);
        yield return new PropertyDescriptor<T, bool>(prefix + nameof(FlipV), n => data(n).FlipV, (n, v) => data(n).FlipV = v, _ => false, stored: true);
        yield return new PropertyDescriptor<T, bool>(prefix + nameof(Transpose), n => data(n).Transpose, (n, v) => data(n).Transpose = v, _ => false, stored: true);
        yield return new PropertyDescriptor<T, Vector2i>(prefix + nameof(TextureOrigin), n => data(n).TextureOrigin, (n, v) => data(n).TextureOrigin = v, _ => default, stored: true);
        yield return new PropertyDescriptor<T, Color>(prefix + nameof(Modulate), n => data(n).Modulate, (n, v) => data(n).Modulate = v, _ => Colors.White, stored: true);
        yield return new PropertyDescriptor<T, int>(prefix + "PhysicsLayerCount", n => data(n).Layers.Count, (n, v) => data(n).ResizeLayers(v), _ => 0, stored: true);
        for (var i = 0; i < Layers.Count; i++)
        {
            var layer = i; var key = prefix + $"Physics/{i}/";
            yield return new PropertyDescriptor<T, Vector2>(key + "LinearVelocity", n => data(n).GetConstantLinearVelocity(layer), (n, v) => data(n).SetConstantLinearVelocity(layer, v), _ => default, stored: true);
            yield return new PropertyDescriptor<T, float>(key + "AngularVelocity", n => data(n).GetConstantAngularVelocity(layer), (n, v) => data(n).SetConstantAngularVelocity(layer, v), _ => 0, stored: true);
            yield return new PropertyDescriptor<T, int>(key + "PolygonCount", n => data(n).GetCollisionPolygonsCount(layer), (n, v) => data(n).SetCollisionPolygonsCount(layer, v), _ => 0, stored: true);
            for (var j = 0; j < Layers[i].Polygons.Count; j++)
            {
                var polygon = j; var p = key + $"Polygon/{j}/";
                yield return new PropertyDescriptor<T, Vector2[]>(p + "Points", n => data(n).GetCollisionPolygonPoints(layer, polygon), (n, v) => data(n).SetCollisionPolygonPoints(layer, polygon, v), _ => [], stored: true);
                yield return new PropertyDescriptor<T, bool>(p + "OneWay", n => data(n).IsCollisionPolygonOneWay(layer, polygon), (n, v) => data(n).SetCollisionPolygonOneWay(layer, polygon, v), _ => false, stored: true);
                yield return new PropertyDescriptor<T, float>(p + "Margin", n => data(n).GetCollisionPolygonOneWayMargin(layer, polygon), (n, v) => data(n).SetCollisionPolygonOneWayMargin(layer, polygon, v), _ => 1, stored: true);
            }
        }
    }
}
