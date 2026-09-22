namespace Electron2D;

public abstract partial class Viewport
{
    private bool _snapTransformsToPixel;
    private bool _snapVerticesToPixel;
    private static readonly PropertyDescriptor[] PixelSnapProperties =
    [
        new PropertyDescriptor<Viewport, bool>(nameof(SnapTransformsToPixel), n => n.SnapTransformsToPixel, (n, v) => n.SnapTransformsToPixel = v, _ => false, stored: true),
        new PropertyDescriptor<Viewport, bool>(nameof(SnapVerticesToPixel), n => n.SnapVerticesToPixel, (n, v) => n.SnapVerticesToPixel = v, _ => false, stored: true),
    ];

    /// <summary>Gets or sets whether canvas transform translations round to whole pixels during rendering.</summary>
    /// <value>False by default; a newly constructed Window reads the corresponding project setting.</value>
    /// <remarks>Rounds local translations and, outside flattened Y groups, accumulated parent translations before composition.
    /// Y sorting uses the snapped local transforms.
    /// Public node transforms remain fractional. Sprite bounds and opacity queries also round their local drawing offset.
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
