namespace Electron2D;

/// <summary>Selects the positional audio listening origin and orientation for its viewport.</summary>
/// <remarks>One listener is current per root viewport. Without one, spatial players listen from the client center.
/// A current listener retains its request across tree exit and reentry.</remarks>
public sealed class AudioListener : Entity
{
    private bool _requestedCurrent;
    private static readonly PropertyDescriptor[] ListenerProperties =
    [new PropertyDescriptor<AudioListener, bool>(nameof(Current), listener => listener.Current,
        (listener, value) => listener.Current = value, _ => false, stored: true)];

    /// <summary>Creates a listener without claiming a viewport.</summary>
    public AudioListener() { }

    /// <summary>Gets or sets whether this listener is or should become current.</summary>
    /// <value>False initially; detached current requests activate on tree entry.</value>
    /// <exception cref="InvalidOperationException">Mutation occurs off the scene owner thread or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The listener is disposed.</exception>
    public bool Current
    {
        get => IsCurrent();
        set { if (value) MakeCurrent(); else ClearCurrent(); }
    }

    /// <summary>Claims its containing viewport, clearing the previous listener.</summary>
    /// <remarks>Detached calls remember the request until entry.</remarks>
    /// <exception cref="InvalidOperationException">Mutation occurs off the scene owner thread or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The listener is disposed.</exception>
    public void MakeCurrent()
    {
        EnsureMutable(); _requestedCurrent = true;
        if (IsInsideTree) GetViewport()?.SetCurrentAudioListener(this);
    }

    /// <summary>Releases this listener and its pending current request.</summary>
    /// <exception cref="InvalidOperationException">Mutation occurs off the scene owner thread or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The listener is disposed.</exception>
    public void ClearCurrent()
    {
        EnsureMutable(); _requestedCurrent = false;
        if (IsInsideTree) GetViewport()?.ReleaseAudioListener(this);
    }

    /// <summary>Reports whether this listener owns the viewport, or is requested while detached.</summary>
    /// <returns>The current/requested state.</returns>
    /// <exception cref="InvalidOperationException">An attached listener is queried off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The listener is disposed.</exception>
    public bool IsCurrent()
    {
        ThrowIfDisposed(); Tree?.EnsureOwnerThread();
        return IsInsideTree ? ReferenceEquals(GetViewport()?.GetAudioListener2D(), this) : _requestedCurrent;
    }

    /// <inheritdoc />
    protected override void OnEnterTree()
    {
        base.OnEnterTree();
        if (_requestedCurrent) GetViewport()?.SetCurrentAudioListener(this);
    }

    /// <inheritdoc />
    protected override void OnExitTree()
    {
        if (IsCurrent()) GetViewport()?.ReleaseAudioListener(this);
        else _requestedCurrent = false;
        base.OnExitTree();
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(ListenerProperties);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => CreateListener;
    private static Node CreateListener() => new AudioListener();
}
