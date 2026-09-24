namespace Electron2D;

public abstract partial class Viewport
{
    internal Transform GetCanvasRenderTransform(CanvasLayer? layer, float interpolationFraction = 1f)
    {
        var result = GetFinalTransform();
        var cameraTransform = GetInterpolatedCanvasTransform(interpolationFraction);
        var size = GetVisibleRect().Size;
        var snapOffset = new Vector2(size.X % 2 == 0 ? -0.5f : 0, size.Y % 2 == 0 ? -0.5f : 0);
        var followScale = layer is { FollowViewportEnabled: true } ? layer.FollowViewportScale : 1;
        if (layer is { FollowViewportEnabled: true })
        {
            var parent = cameraTransform;
            if (_snapTransformsToPixel && followScale != 0) parent.Origin = (parent.Origin * followScale + snapOffset).Ceil() / followScale;
            result *= parent;
        }
        var local = layer?.Transform ?? cameraTransform;
        if (_snapTransformsToPixel) local.Origin = (local.Origin + snapOffset).Ceil();
        result *= local;
        if (followScale != 1)
        {
            var pivot = size * 0.5f;
            result = new Transform(0, pivot) * new Transform(0, Vector2.One * followScale, 0, Vector2.Zero) * new Transform(0, -pivot) * result;
        }
        if (!result.IsFinite()) throw new InvalidOperationException("Canvas rendering transform overflowed finite coordinates.");
        return result;
    }

    private bool _snapTransformsToPixel;
    private bool _snapVerticesToPixel;
    private uint _canvasCullMask = uint.MaxValue;
    private static readonly PropertyDescriptor[] CanvasRenderingProperties =
    [
        new PropertyDescriptor<Viewport, bool>(nameof(SnapTransformsToPixel), n => n.SnapTransformsToPixel, (n, v) => n.SnapTransformsToPixel = v, _ => false, stored: true),
        new PropertyDescriptor<Viewport, bool>(nameof(SnapVerticesToPixel), n => n.SnapVerticesToPixel, (n, v) => n.SnapVerticesToPixel = v, _ => false, stored: true),
        new PropertyDescriptor<Viewport, uint>(nameof(CanvasCullMask), n => n.CanvasCullMask, (n, v) => n.CanvasCullMask = v, _ => uint.MaxValue, stored: true),
    ];

    /// <summary>Gets or sets the 32-bit mask selecting canvas items for this viewport's rendering.</summary>
    /// <value>All bits set initially. Zero suppresses canvas geometry while the viewport still clears and submits frames.</value>
    /// <remarks>Each item and its direct canvas ancestors must independently have a VisibilityLayer bit in this mask.
    /// Applies to the default canvas and CanvasLayer groups. Changes reuse retained commands and do not affect
    /// logical visibility, drawing callbacks, processing, input or scene order. Stored by PackedScene.</remarks>
    /// <exception cref="InvalidOperationException">An attached viewport is accessed off-owner or mutated during capture.</exception>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    public uint CanvasCullMask
    {
        get { CheckTransformQuery(); return _canvasCullMask; }
        set { EnsureMutable(); _canvasCullMask = value; }
    }

    /// <summary>Returns whether a zero-based canvas cull-mask bit is enabled.</summary>
    /// <param name="layer">Bit index from zero through 31, inclusive.</param>
    /// <returns>True when the selected bit in CanvasCullMask is set.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The bit index is outside zero through 31.</exception>
    /// <exception cref="InvalidOperationException">An attached viewport is queried off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    public bool GetCanvasCullMaskBit(int layer)
    {
        var mask = CanvasCullMask;
        if ((uint)layer >= 32) throw new ArgumentOutOfRangeException(nameof(layer));
        return (mask & (1u << layer)) != 0;
    }

    /// <summary>Changes one canvas cull-mask bit without changing the other bits.</summary>
    /// <param name="layer">Bit index from zero through 31, inclusive.</param>
    /// <param name="enable">True to enable the bit; false to clear it.</param>
    /// <remarks>The next submission uses the new mask without invalidating retained commands.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The bit index is outside zero through 31.</exception>
    /// <exception cref="InvalidOperationException">An attached viewport is mutated off-owner or during capture.</exception>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    public void SetCanvasCullMaskBit(int layer, bool enable)
    {
        EnsureMutable();
        if ((uint)layer >= 32) throw new ArgumentOutOfRangeException(nameof(layer));
        var bit = 1u << layer;
        _canvasCullMask = enable ? _canvasCullMask | bit : _canvasCullMask & ~bit;
    }

    /// <summary>Gets or sets whether canvas transform translations round to whole pixels during rendering.</summary>
    /// <value>False by default; a newly constructed Window reads the corresponding project setting.</value>
    /// <remarks>Rounds local translations and, outside flattened Y groups, accumulated parent translations before composition.
    /// Y sorting uses the snapped local transforms.
    /// Canvas translations first use ceil with a -0.5 bias on even viewport dimensions and zero on odd dimensions;
    /// following layer parents are rounded in follow-scale units. Public node transforms remain fractional. Sprite bounds and opacity queries also round their local drawing offset.
    /// Changes affect subsequent submissions without requesting redraw; existing Sprite commands retain the offset
    /// recorded by their last draw until QueueRedraw or another invalidation. Half values round toward positive infinity.</remarks>
    /// <exception cref="InvalidOperationException">An attached viewport is mutated off-owner or during capture.</exception>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    public bool SnapTransformsToPixel
    {
        get { ThrowIfDisposed(); return _snapTransformsToPixel; }
        set { EnsureMutable(); _snapTransformsToPixel = value; }
    }

    /// <summary>Gets or sets whether final canvas primitive vertices round to framebuffer pixels.</summary>
    /// <value>False by default; a newly constructed Window reads the corresponding project setting.</value>
    /// <remarks>Applies after node, drawing and framebuffer transforms. Does not modify public transforms, Sprite
    /// bounds or source-opacity queries. Changes affect the next submission without rerecording commands.
    /// Half values round toward positive infinity. Using both snapping modes may make motion less smooth.</remarks>
    /// <exception cref="InvalidOperationException">An attached viewport is mutated off-owner or during capture.</exception>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    public bool SnapVerticesToPixel
    {
        get { ThrowIfDisposed(); return _snapVerticesToPixel; }
        set { EnsureMutable(); _snapVerticesToPixel = value; }
    }
}
