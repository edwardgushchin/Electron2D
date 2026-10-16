namespace Electron2D;

/// <summary>Describes a reusable video source and creates independent decoder state.</summary>
/// <remarks>Players borrow the resource and own their playbacks. Custom factories and playback hooks
/// run synchronously on their current owner, never on the native audio callback. Prepared detached
/// player state may follow its hierarchy to the initial scene owner before first Play.</remarks>
public abstract class VideoStream : Resource
{
    private string _file = string.Empty;
    private bool _creating;
    internal byte[]? PreparedFile;
    /// <summary>Initializes a detached video resource.</summary>
    protected VideoStream() { }
    /// <summary>Gets or sets the path or URI handled by this video source.</summary>
    /// <value>An empty string initially. The built-in decoder accepts Ogg Theora files.</value>
    /// <remarks>Changing the path invalidates prepared source bytes and reports Changed. An attached player
    /// prepares a new independent decoder before replacing its existing playback.</remarks>
    /// <exception cref="ArgumentNullException">The path is null.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public string File
    {
        get { ThrowIfDisposed(); return _file; }
        set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); if (_creating) throw new InvalidOperationException("A video factory cannot change its source path."); if (_file == value) return; _file = value; PreparedFile = null; EmitChanged(); }
    }
    /// <summary>Creates independent caller-owned playback state borrowing this resource.</summary>
    /// <returns>A fresh live playback, which the caller must dispose.</returns>
    /// <exception cref="InvalidOperationException">The factory returns null, disposed or already attached state.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public VideoStreamPlayback InstantiatePlayback()
    {
        ThrowIfDisposed();
        if (_creating) throw new InvalidOperationException("A video factory cannot reenter itself.");
        _creating = true;
        try
        {
            var result = OnInstantiatePlayback();
            if (result is null || result.IsDisposed || result.PlayerOwner is not null) throw new InvalidOperationException("A video factory must return fresh live playback state.");
            return result;
        }
        finally { _creating = false; }
    }
    /// <summary>Creates a fresh decoder for this source.</summary>
    /// <returns>New caller-owned playback state.</returns>
    protected abstract VideoStreamPlayback OnInstantiatePlayback();
    internal void ReloadFrom(VideoStream source)
    {
        source.CopyCustomStateTo(this, false, DeepDuplicateMode.Internal, static r => r, static r => r); EmitChanged();
    }
    private static readonly PropertyDescriptor[] VideoProperties =
    [new PropertyDescriptor<VideoStream, string>(nameof(File), s => s.File, (s, v) => s.File = v, _ => string.Empty, stored: true)];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var descriptor in base.GetPropertyDescriptors()) yield return descriptor;
        foreach (var descriptor in VideoProperties) yield return descriptor;
    }
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var copy = (VideoStream)target; copy._file = _file; copy.PreparedFile = PreparedFile;
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { PreparedFile = null; base.Dispose(disposing); }
    /// <inheritdoc />
    protected override void ValidateDisposal() { if (_creating) throw new InvalidOperationException("A video factory cannot dispose its source."); base.ValidateDisposal(); }
}
