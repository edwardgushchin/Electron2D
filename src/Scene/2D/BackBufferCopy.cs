namespace Electron2D;

/// <summary>Selects the canvas image region copied for subsequent screen-reading materials.</summary>
public enum BackBufferCopyMode
{
    /// <summary>Leave the current screen snapshot unchanged.</summary>
    Disabled = 0,
    /// <summary>Copy the transformed local rectangle, clipped to the viewport.</summary>
    Rect = 1,
    /// <summary>Copy the complete viewport image.</summary>
    Viewport = 2,
}
/// <summary>Copies already drawn canvas pixels to the native screen texture at this node's draw-order position.</summary>
/// <remarks>SCREEN_TEXTURE materials consume this snapshot. Without an earlier explicit copy, the first ordinary
/// screen-reading draw automatically copies the viewport; later draws reuse it. Rect copies define only their region.
/// The node uses Entity transforms and ordering, including hidden/masked branches; it does not supply Control anchors.</remarks>
public class BackBufferCopy : Entity
{
    private BackBufferCopyMode _mode = BackBufferCopyMode.Rect;
    private Rect2 _rect = new(-100, -100, 200, 200);
    /// <summary>Creates a rectangular copy of the local -100,-100,200,200 region.</summary>
    public BackBufferCopy() { }
    /// <summary>Gets or sets the active buffering policy.</summary>
    /// <value>Rect initially.</value>
    /// <remarks>Every valid assignment commits before notifying the property list, including equal assignments.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The enumeration is undefined.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposed.</exception>
    /// <exception cref="Exception">A property-list subscriber fails after commitment.</exception>
    public BackBufferCopyMode CopyMode { get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _mode; } set { EnsureMutable(); Animation.Valid(value); _mode = value; NotifyPropertyListChanged(); } }
    /// <summary>Gets or sets the finite local rectangle used by Rect mode.</summary>
    /// <value>-100,-100,200,200 initially; signed dimensions are retained.</value>
    /// <remarks>Every assignment commits and raises ItemRectChanged. The zero-valued rectangle uses a complete
    /// viewport copy, matching the full-copy sentinel. Samples outside a nonempty copied region are unspecified.</remarks>
    /// <exception cref="ArgumentException">The rectangle is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">This node is disposed.</exception>
    /// <exception cref="Exception">An ItemRectChanged subscriber fails after commitment.</exception>
    public Rect2 Rect { get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _rect; } set { EnsureMutable(); if (!value.IsFinite()) throw new ArgumentException("Copy rectangles must be finite.", nameof(value)); _rect = value; NotifyItemRectChanged(); } }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(CopyProperties);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(BackBufferCopy) ? CreateCopy : base.CreateSceneInstanceFactory();
    private static Node CreateCopy() => new BackBufferCopy();
    private static readonly PropertyDescriptor[] CopyProperties =
    [new PropertyDescriptor<BackBufferCopy, BackBufferCopyMode>(nameof(CopyMode), n => n.CopyMode, (n,v) => n.CopyMode=v, _ => BackBufferCopyMode.Rect, stored:true),
     new PropertyDescriptor<BackBufferCopy, Rect2>(nameof(Rect), n => n.Rect, (n,v) => n.Rect=v, _ => new(-100,-100,200,200), (_,v) => v.IsFinite(), stored:true)];
}
