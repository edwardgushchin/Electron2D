namespace Electron2D;

public abstract partial class Viewport
{
    private AudioListener? _audioListener;
    private bool _audioListenerEnable2D;
    private static readonly PropertyDescriptor[] ViewportAudioProperties =
    [new PropertyDescriptor<Viewport, bool>(nameof(AudioListenerEnable2D), viewport => viewport.AudioListenerEnable2D,
        (viewport, value) => viewport.AudioListenerEnable2D = value, _ => false, stored: true)];

    /// <summary>Gets or sets whether this viewport participates in two-dimensional audio listening.</summary>
    /// <value>False on a detached viewport; SceneTree enables its root viewport on entry.</value>
    /// <exception cref="InvalidOperationException">An attached viewport is accessed off-owner or mutated during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    public bool AudioListenerEnable2D
    {
        get { CheckTransformQuery(); return _audioListenerEnable2D; }
        set { EnsureMutable(); _audioListenerEnable2D = value; }
    }

    /// <summary>Returns the active explicit two-dimensional listener, if any.</summary>
    /// <returns>A borrowed listener, or null when the viewport uses its client center.</returns>
    /// <exception cref="InvalidOperationException">An attached viewport is queried off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    public AudioListener? GetAudioListener2D() { CheckTransformQuery(); return _audioListener; }

    internal void SetCurrentAudioListener(AudioListener listener)
    {
        if (ReferenceEquals(_audioListener, listener)) return;
        _audioListener?.ClearCurrent();
        _audioListener = listener;
    }
    internal void ReleaseAudioListener(AudioListener listener)
    {
        if (ReferenceEquals(_audioListener, listener)) _audioListener = null;
    }
}
