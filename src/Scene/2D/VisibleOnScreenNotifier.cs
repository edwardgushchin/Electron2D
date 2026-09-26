namespace Electron2D;

/// <summary>Reports whether a local rectangle participates in the current rendered viewport.</summary>
/// <remarks>Detection uses conservative transformed bounds, visibility and viewport culling; it does not test
/// occlusion by other drawing. State is false until the first submitted canvas frame. Tree exit clears it silently.</remarks>
public class VisibleOnScreenNotifier : Entity
{
    private Rect2 _rect = new(-10, -10, 20, 20);
    private bool _onScreen;
    internal bool ScreenCandidate;
    internal ulong ScreenGeneration;
    private static readonly PropertyDescriptor[] ScreenProperties =
    [
        new PropertyDescriptor<VisibleOnScreenNotifier, Rect2>(nameof(Rect), node => node.Rect, (node, value) => node.Rect = value,
            _ => new(-10, -10, 20, 20), stored: true)
    ];

    /// <summary>Creates an initially off-screen notifier with a centered 20 by 20 rectangle.</summary>
    public VisibleOnScreenNotifier() { }

    /// <summary>Gets or sets the local rectangle used by render visibility detection.</summary>
    /// <value>(-10, -10, 20, 20) initially; finite negative extents are normalized for culling.</value>
    /// <remarks>Changes affect the next submitted canvas frame. Borders, lines and points participate in conservative culling; local bounds include the node origin.</remarks>
    /// <exception cref="ArgumentException">The rectangle or its endpoint is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The notifier is disposed.</exception>
    public Rect2 Rect
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _rect; }
        set
        {
            EnsureMutable();
            if (!value.IsFinite() || !value.End.IsFinite()) throw new ArgumentException("Screen bounds must be finite.", nameof(value));
            _rect = value;
        }
    }

    /// <summary>Returns the visibility state committed by the last submitted canvas frame.</summary>
    /// <returns>False before the first frame or while detached; otherwise the sampled screen state.</returns>
    /// <exception cref="InvalidOperationException">An attached query is off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The notifier is disposed.</exception>
    public bool IsOnScreen() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _onScreen; }

    /// <summary>Occurs after this rectangle first participates in a submitted canvas frame.</summary>
    /// <remarks>State is committed before delivery. Runs on the scene owner thread before FramePostDraw;
    /// later queued notifiers are attempted after a handler fails.</remarks>
    public event Action? ScreenEntered;
    /// <summary>Occurs when a rendered frame no longer contains any visible part of this rectangle.</summary>
    /// <remarks>Tree exit clears state without this event. Hidden or stopped render loops retain the last sample.</remarks>
    public event Action? ScreenExited;

    internal bool CommitScreenState()
    {
        if (_onScreen == ScreenCandidate) return false;
        _onScreen = ScreenCandidate;
        return true;
    }

    internal void RaiseScreenChange()
    {
        List<Exception>? errors = null;
        try { if (_onScreen) ScreenEntered?.Invoke(); else ScreenExited?.Invoke(); }
        catch (Exception error) { CollectException(ref errors, error); }
        try { if (!IsDisposed && IsInsideTree) ApplyScreenState(); }
        catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Screen visibility callbacks failed.", errors);
    }

    internal virtual void ApplyScreenState() { }

    internal override void OnTreeMembershipChanged(bool entering)
    {
        ScreenGeneration++; _onScreen = false; ScreenCandidate = false;
        base.OnTreeMembershipChanged(entering);
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(ScreenProperties);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(VisibleOnScreenNotifier) ? CreateNotifier : base.CreateSceneInstanceFactory();
    private static Node CreateNotifier() => new VisibleOnScreenNotifier();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        try { base.Dispose(disposing); }
        finally { ScreenEntered = null; ScreenExited = null; }
    }
}
