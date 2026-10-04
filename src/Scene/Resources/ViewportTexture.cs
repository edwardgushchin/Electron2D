namespace Electron2D;

/// <summary>A scene-local live view of a viewport's completed native canvas image.</summary>
/// <remarks>Viewport.GetTexture supplies a direct view. Authored ViewportPath resolves from GetLocalScene
/// after scene reconstruction. The texture borrows its viewport; disposing either never owns the other.
/// Native sampling uses completed render targets. GetImage is an explicit caller-owned readback.</remarks>
public sealed class ViewportTexture : Texture
{
    private string _path = "";
    private WeakReference<Viewport>? _viewport;
    private bool _direct;
    /// <summary>Creates an unresolved scene-local viewport texture.</summary>
    public ViewportTexture() { ResourceLocalToScene = true; }
    internal ViewportTexture(Viewport viewport) : this() { _direct = true; Bind(viewport); }
    /// <summary>Gets or sets the viewport path relative to the resource's local scene root.</summary>
    /// <value>Empty initially.</value>
    /// <remarks>An equal write is ignored. A committed different path releases the old binding and resolves
    /// within the local scene when available. Missing or incompatible paths fail local setup explicitly.</remarks>
    /// <exception cref="ArgumentNullException">The path is null.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    /// <exception cref="InvalidOperationException">A bound scene is mutated off-owner.</exception>
    public string ViewportPath
    {
        get { ThrowIfDisposed(); return _path; }
        set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); Bound?.Tree?.EnsureOwnerThread(); if (_path == value) return; _path = value; _direct = false; Bind(null); Resolve(false); EmitChanged(); }
    }
    internal Viewport? Bound
    {
        get
        {
            ThrowIfDisposed();
            if (_viewport?.TryGetTarget(out var viewport) == true && !viewport.IsDisposed) return viewport;
            return _direct ? null : Resolve(false);
        }
    }
    private Viewport? Resolve(bool required)
    {
        var root = GetLocalScene();
        if (root is null || root.IsDisposed || _path.Length == 0) return null;
        var target = root.GetNodeOrNull(_path) as Viewport;
        if (target is null && required) throw new InvalidOperationException("Viewport texture path does not resolve to a viewport.");
        Bind(target); return target;
    }
    private void Bind(Viewport? viewport)
    {
        if (_viewport?.TryGetTarget(out var old) == true) old.TextureSizeUpdated -= SizeChanged;
        _viewport = viewport is null ? null : new(viewport);
        if (viewport is not null) viewport.TextureSizeUpdated += SizeChanged;
    }
    private void SizeChanged() { if (!IsDisposed) EmitChanged(); }
    /// <inheritdoc />
    public override int GetWidth() => Bound?.TextureDimensions.X ?? 0;
    /// <inheritdoc />
    public override int GetHeight() => Bound?.TextureDimensions.Y ?? 0;
    /// <inheritdoc />
    public override Image.Format PixelFormat { get { ThrowIfDisposed(); return Image.Format.Rgba8; } }
    /// <inheritdoc />
    public override bool HasAlpha { get { ThrowIfDisposed(); return false; } }
    /// <inheritdoc />
    public override bool HasMipmaps { get { ThrowIfDisposed(); return false; } }
    /// <inheritdoc />
    public override int MipmapCount { get { ThrowIfDisposed(); return 0; } }
    /// <summary>Copies the completed native image, or returns null before rendering or after target loss.</summary>
    /// <returns>A caller-owned RGBA8 image; null when no active completed target exists.</returns>
    /// <remarks>This explicit cold readback synchronizes native execution and allocates. It is not part of ordinary drawing.</remarks>
    /// <exception cref="ObjectDisposedException">The texture is disposed.</exception>
    /// <exception cref="InvalidOperationException">Readback is off-owner or occurs during native submission.</exception>
    public override Image? GetImage() => Bound is { } viewport ? viewport.RenderingOwner?.ReadbackViewport(viewport) : null;
    internal override TexturePixels? CapturePixels() { ThrowIfDisposed(); return null; }
    /// <inheritdoc />
    protected override void OnSetupLocalToScene() { _direct = false; Bind(null); Resolve(true); EmitChanged(); }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new ViewportTexture();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force)
    {
        var copy = (ViewportTexture)target; copy._path = _path;
        if (_direct && Bound is { } viewport)
        {
            var root = viewport.Owner ?? viewport; if (viewport.Owner is null) while (root.Parent is not null) root = root.Parent;
            copy._path = root.GetPathTo(viewport); copy._direct = true; copy.Bind(viewport);
        }
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
    [new PropertyDescriptor<ViewportTexture, string>(nameof(ViewportPath), t => t.ViewportPath, (t, v) => t.ViewportPath = v, _ => "", stored: true)]);
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) Bind(null); base.Dispose(disposing); }
}
