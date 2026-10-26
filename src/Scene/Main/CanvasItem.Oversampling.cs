namespace Electron2D;

/// <summary>Selects whether scalable canvas rasters follow an item's logical global scale.</summary>
public enum OversamplingWithScale
{
    /// <summary>Inherits the direct canvas parent; a canvas root disables scale oversampling.</summary>
    ParentNode = 0,
    /// <summary>Uses only the viewport raster factor.</summary>
    Disabled = 1,
    /// <summary>Multiplies the viewport raster factor by the largest global transform scale component.</summary>
    Enabled = 2,
    /// <summary>Sentinel; not an assignable policy.</summary>
    Max = 3,
}

public abstract partial class CanvasItem
{
    private OversamplingWithScale _oversamplingWithScale;
    private int _itemRasterScaleKey = 64;
    private float _itemRasterScale = 1;
    private int _itemRasterReferenceKey = 64;

    /// <summary>Gets or sets whether scalable fonts and SVG textures follow the item's scale.</summary>
    /// <value>ParentNode initially. TopLevel and non-canvas parents end inheritance.</value>
    /// <remarks>Uses the current logical global transform, excluding camera, canvas-layer and drawing transforms.
    /// Explicit font raster factors and MSDF fonts retain their own policies. Scale variants preserve logical sizes;
    /// changing scale can allocate raster storage. Stable retained replay reuses that storage.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The value is invalid or Max.</exception>
    /// <exception cref="InvalidOperationException">A read is outside the owner/active processing groups, or a write is outside the owner/current group or occurs during capture.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public OversamplingWithScale OversamplingWithScale
    {
        get { ThrowIfDisposed(); Tree?.EnsureProcessingReadAccess(); return _oversamplingWithScale; }
        set
        {
            EnsureCanvasMetadataMutable();
            if ((uint)value >= (uint)OversamplingWithScale.Max) throw new ArgumentOutOfRangeException(nameof(value));
            if (_oversamplingWithScale == value) return;
            _oversamplingWithScale = value;
            InvalidateOversamplingPolicy();
        }
    }

    private bool UsesScaleOversampling => _oversamplingWithScale == OversamplingWithScale.Enabled ||
        _oversamplingWithScale == OversamplingWithScale.ParentNode && GetParentItem()?.UsesScaleOversampling == true;

    private void InvalidateOversamplingPolicy()
    {
        InvalidateCanvas();
        for (var i = 0; i < GetChildCount(includeInternal: true); i++)
            if (GetChild(i, includeInternal: true) is CanvasItem { TopLevel: false, _oversamplingWithScale: OversamplingWithScale.ParentNode } child)
                child.InvalidateOversamplingPolicy();
    }

    internal float FontRasterScale => ServerState?.TileOwner?.FontRasterScale ?? _itemRasterScale;
    internal int ItemRasterScaleKey => ServerState?.TileOwner?.ItemRasterScaleKey ?? _itemRasterScaleKey;

    private void SyncItemRasterScale()
    {
        var viewport = CanvasViewport;
        _ = viewport?.DPIScaleKey;
        var factor = viewport?.GetOversampling() ?? 1;
        var scaled = false;
        if (UsesScaleOversampling)
        {
            var scale = GetGlobalTransform().Scale;
            var largest = MathF.Max(scale.X, scale.Y);
            if (largest > 0 && float.IsFinite(largest)) { factor *= largest; scaled = true; }
        }
        var key = CanvasRasterScale.Quantize(factor);
        var referenceKey = scaled ? key : 64;
        if (referenceKey != _itemRasterReferenceKey)
        {
            CanvasRasterScale.ChangeReference(_itemRasterReferenceKey, referenceKey);
            _itemRasterReferenceKey = referenceKey;
        }
        _itemRasterScaleKey = key;
        if (_itemRasterScale == factor) return;
        _itemRasterScale = factor;
        InvalidateCanvas();
    }

    private void ReleaseItemRasterScale()
    {
        CanvasRasterScale.ChangeReference(_itemRasterReferenceKey, 64);
        _itemRasterReferenceKey = 64;
        _itemRasterScaleKey = 64;
        _itemRasterScale = 1;
    }

    private static readonly PropertyDescriptor[] ItemOversamplingProperties =
    [
        new PropertyDescriptor<CanvasItem, OversamplingWithScale>(nameof(OversamplingWithScale), n => n.OversamplingWithScale, (n, v) => n.OversamplingWithScale = v, _ => OversamplingWithScale.ParentNode, stored: true),
    ];
}
