namespace Electron2D;

/// <summary>Views a registered camera image plane through the ordinary texture API.</summary>
/// <remarks>The view borrows its feed and owns a stable rendering identity. Dimensions refer to the full source
/// frame even for a smaller chroma plane. Missing feed IDs expose an empty image. Source and registry changes
/// invalidate recorded rendering. Runtime activation is never serialized.</remarks>
public sealed class CameraTexture : Texture
{
    private readonly object _gate = new();
    private int _feedID;
    private CameraServer.FeedImage _which;
    private CameraFeed? _feed;
    private readonly Action _changed;
    /// <summary>Creates an empty view with feed ID zero and the first image plane.</summary>
    public CameraTexture() { _changed = FrameChanged; CameraServer.Watch(this); }
    private void FrameChanged() { if (!IsDisposed) EmitChanged(); }
    /// <summary>Gets or selects a process-local feed ID; zero selects no source.</summary><value>Zero initially. Unknown positive IDs remain empty until registered.</value>
    public int CameraFeedID
    {
        get { lock (_gate) { ThrowIfDisposed(); return _feedID; } }
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            lock (_gate) { ThrowIfDisposed(); if (_feedID == value) return; _feedID = value; BindSource(); }
            EmitChanged();
        }
    }
    /// <summary>Gets or selects the source image plane.</summary><value>The first RGB/Y/packed plane initially; CBCRImage selects separate chroma.</value>
    public CameraServer.FeedImage WhichFeed
    {
        get { lock (_gate) { ThrowIfDisposed(); return _which; } }
        set
        {
            if ((uint)value > 1) throw new ArgumentOutOfRangeException(nameof(value));
            lock (_gate) { ThrowIfDisposed(); if (_which == value) return; _which = value; }
            EmitChanged();
        }
    }
    /// <summary>Gets or changes the selected live feed's activation on its owner.</summary><value>False when no registered feed exists. This convenience property is not stored.</value>
    public bool CameraIsActive
    {
        get { lock (_gate) { ThrowIfDisposed(); return _feed?.FeedIsActive ?? false; } }
        set { CameraFeed? feed; lock (_gate) { ThrowIfDisposed(); feed = _feed; } if (feed is not null) feed.FeedIsActive = value; }
    }
    private bool BindSource()
    {
        var next = CameraServer.Find(_feedID);
        if (ReferenceEquals(_feed, next)) return false;
        if (_feed is not null) _feed.FrameChanged -= _changed;
        _feed = next;
        if (_feed is not null) _feed.FrameChanged += _changed;
        return true;
    }
    internal void RegistryChanged()
    {
        lock (_gate) { if (IsDisposed || !BindSource()) return; }
        EmitChanged();
    }
    /// <inheritdoc />
    public override Vector2 GetSize() { lock (_gate) { ThrowIfDisposed(); var size = _feed?.Dimensions ?? Vector2i.Zero; return new(size.X, size.Y); } }
    /// <inheritdoc />
    public override int GetWidth() { lock (_gate) { ThrowIfDisposed(); return _feed?.Dimensions.X ?? 0; } }
    /// <inheritdoc />
    public override int GetHeight() { lock (_gate) { ThrowIfDisposed(); return _feed?.Dimensions.Y ?? 0; } }
    /// <inheritdoc />
    public override bool HasAlpha { get { ThrowIfDisposed(); return false; } }
    /// <inheritdoc />
    public override Image? GetImage() { lock (_gate) { ThrowIfDisposed(); return _feed?.CopyImage(_which); } }
    internal override TexturePixels? CapturePixels() { lock (_gate) { ThrowIfDisposed(); return _feed?.Pixels(_which); } }
    private static readonly PropertyDescriptor[] Properties =
    [
        new PropertyDescriptor<CameraTexture, int>(nameof(CameraFeedID), t => t.CameraFeedID, (t, v) => t.CameraFeedID = v, _ => 0, stored: true),
        new PropertyDescriptor<CameraTexture, CameraServer.FeedImage>(nameof(WhichFeed), t => t.WhichFeed, (t, v) => t.WhichFeed = v, _ => CameraServer.FeedImage.RGBAImage, stored: true),
        new PropertyDescriptor<CameraTexture, bool>(nameof(CameraIsActive), t => t.CameraIsActive, (t, v) => t.CameraIsActive = v, _ => false)
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new CameraTexture();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var view = (CameraTexture)target;
        lock (_gate) { ThrowIfDisposed(); lock (view._gate) { view._feedID = _feedID; view._which = _which; view.BindSource(); } }
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        lock (_gate) { if (_feed is not null) _feed.FrameChanged -= _changed; _feed = null; }
        CameraServer.Unwatch(this); base.Dispose(disposing);
    }
}
