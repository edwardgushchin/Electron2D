namespace Electron2D;

public partial class Control
{
    /// <summary>Specifies how a container allocates and aligns one control axis.</summary>
    [Flags]
    public enum SizeFlags
    {
        /// <summary>Uses bound minimum size at the beginning of its allocation.</summary>
        ShrinkBegin = 0,
        /// <summary>Fills the available allocation.</summary>
        Fill = 1,
        /// <summary>Participates in weighted surplus allocation.</summary>
        Expand = 2,
        /// <summary>Expands and fills its allocation.</summary>
        ExpandFill = 3,
        /// <summary>Centers a non-filled control within its allocation.</summary>
        ShrinkCenter = 4,
        /// <summary>Aligns a non-filled control to the end of its allocation.</summary>
        ShrinkEnd = 8
    }
    private SizeFlags _sizeFlagsHorizontal = SizeFlags.Fill, _sizeFlagsVertical = SizeFlags.Fill;
    private float _sizeFlagsStretchRatio = 1;
    internal Vector2? ContainerMaximum;
    internal void SetContainerRect(Rect2 rect) => SetLayoutRect(rect.Position, rect.Size, false, resetAnchors: true);
    private static readonly PropertyDescriptor[] SizeFlagProperties =
    [
        new PropertyDescriptor<Control, SizeFlags>(nameof(SizeFlagsHorizontal), node => node.SizeFlagsHorizontal, (node, value) => node.SizeFlagsHorizontal = value, _ => SizeFlags.Fill, stored: true),
        new PropertyDescriptor<Control, SizeFlags>(nameof(SizeFlagsVertical), node => node.SizeFlagsVertical, (node, value) => node.SizeFlagsVertical = value, _ => SizeFlags.Fill, stored: true),
        new PropertyDescriptor<Control, float>(nameof(SizeFlagsStretchRatio), node => node.SizeFlagsStretchRatio, (node, value) => node.SizeFlagsStretchRatio = value, _ => 1, stored: true)
    ];
    /// <summary>Gets or sets the horizontal container sizing/alignment bits.</summary>
    /// <value>Fill initially; unknown bits are retained and ignored by current consumers.</value>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public SizeFlags SizeFlagsHorizontal
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _sizeFlagsHorizontal; }
        set { EnsureMutable(); if (_sizeFlagsHorizontal == value) return; _sizeFlagsHorizontal = value; SizeFlagsChanged?.Invoke(); }
    }
    /// <summary>Gets or sets the vertical container sizing/alignment bits.</summary>
    /// <value>Fill initially; Range supplies ShrinkBegin, texture progress supplies Fill.</value>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public SizeFlags SizeFlagsVertical
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _sizeFlagsVertical; }
        set { EnsureMutable(); if (_sizeFlagsVertical == value) return; _sizeFlagsVertical = value; SizeFlagsChanged?.Invoke(); }
    }
    /// <summary>Gets or sets the finite weighted expansion ratio.</summary>
    /// <value>One initially. Zero/negative ratios are retained; positive total weights govern allocation.</value>
    /// <exception cref="ArgumentOutOfRangeException">The ratio is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public float SizeFlagsStretchRatio
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _sizeFlagsStretchRatio; }
        set { EnsureMutable(); if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_sizeFlagsStretchRatio == value) return; _sizeFlagsStretchRatio = value; SizeFlagsChanged?.Invoke(); }
    }
    /// <summary>Occurs after an actual size flag or stretch ratio change.</summary>
    /// <remarks>Container listeners coalesce the next layout pass. State is committed when a subscriber throws.</remarks>
    public event Action? SizeFlagsChanged;
}
