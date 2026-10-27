namespace Electron2D;

/// <summary>Reports whether a local rectangle participates in the current rendered viewport.</summary>
/// <remarks>Detection uses conservative transformed bounds, visibility and viewport culling; it does not test
/// occlusion by other drawing. Editor-role frames retain the last runtime sample without delivering screen events.
/// State is false until the first runtime canvas frame. Tree exit clears it silently.</remarks>
public class VisibleOnScreenNotifier : Entity
{
    private Rect2 _rect = new(-10, -10, 20, 20);
    private bool _onScreen;
    private bool _showRect = true;
    internal bool ScreenCandidate;
    internal ulong ScreenGeneration;
    private static readonly PropertyDescriptor[] ScreenProperties =
    [
        new PropertyDescriptor<VisibleOnScreenNotifier, Rect2>(nameof(Rect), node => node.Rect, (node, value) => node.Rect = value,
            _ => new(-10, -10, 20, 20), stored: true),
        new PropertyDescriptor<VisibleOnScreenNotifier, bool>(nameof(ShowRect), node => node.ShowRect, (node, value) => node.ShowRect = value,
            _ => true, stored: true)
    ];

    /// <summary>Creates an initially off-screen notifier with a centered 20 by 20 rectangle.</summary>
    public VisibleOnScreenNotifier() { }

    /// <summary>Gets or sets the local rectangle used by render visibility detection.</summary>
    /// <value>(-10, -10, 20, 20) initially; finite negative extents are normalized for culling.</value>
    /// <remarks>Detection changes affect the next submitted runtime canvas frame. Borders, lines and points participate in conservative culling; local bounds include the node origin.</remarks>
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
            _rect = value; QueueRedraw();
        }
    }

    /// <summary>Gets or sets whether the editor draws the local detection rectangle.</summary>
    /// <value>True initially. Drawing requires <see cref="Engine.IsEditorHint"/> and uses translucent magenta.</value>
    /// <remarks>This affects drawing only; detection bounds and screen events are independent of the flag.
    /// Inherited canvas transforms, visibility and modulation apply to the fill.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The notifier is disposed.</exception>
    public bool ShowRect
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _showRect; }
        set { EnsureMutable(); if (_showRect == value) return; _showRect = value; QueueRedraw(); }
    }

    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        var reversed = IsNotificationReversed;
        if (!reversed) base.OnNotification(what);
        if (what == NotificationDraw && _showRect && Engine.IsEditorHint) DrawRect(_rect, new(1, .5f, 1, .2f));
        if (reversed) base.OnNotification(what);
    }

    /// <summary>Returns the visibility state committed by the last submitted runtime canvas frame.</summary>
    /// <returns>False before the first runtime frame or while detached; otherwise the sampled screen state.</returns>
    /// <exception cref="InvalidOperationException">An attached query is off-owner.</exception>
    /// <exception cref="ObjectDisposedException">The notifier is disposed.</exception>
    public bool IsOnScreen() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _onScreen; }

    /// <summary>Occurs after this rectangle first participates in a submitted runtime canvas frame.</summary>
    /// <remarks>State is committed before delivery. Runs on the scene owner thread before FramePostDraw;
    /// later queued notifiers are attempted after a handler fails. Entering the editor role suppresses later queued delivery without replay.</remarks>
    public event Action? ScreenEntered { add => _publisherScreenEntered.Add(value); remove => _publisherScreenEntered.Remove(value); }
    private EventPublisher<Action> _publisherScreenEntered;
    /// <summary>Occurs when a runtime frame no longer contains any visible part of this rectangle.</summary>
    /// <remarks>Tree exit clears state without this event. Hidden or stopped render loops and editor-role frames retain the last sample.</remarks>
    public event Action? ScreenExited { add => _publisherScreenExited.Add(value); remove => _publisherScreenExited.Remove(value); }
    private EventPublisher<Action> _publisherScreenExited;

    internal bool CommitScreenState()
    {
        if (Engine.IsEditorHint || _onScreen == ScreenCandidate) return false;
        _onScreen = ScreenCandidate;
        return true;
    }

    internal void RaiseScreenChange()
    {
        if (Engine.IsEditorHint) return;
        List<Exception>? errors = null;
        try { if (_onScreen) BeginEventEmission(_publisherScreenEntered.Handlers)?.Invoke(); else BeginEventEmission(_publisherScreenExited.Handlers)?.Invoke(); }
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
        finally { _publisherScreenEntered.Handlers = null; _publisherScreenExited.Handlers = null; }
    }
}
