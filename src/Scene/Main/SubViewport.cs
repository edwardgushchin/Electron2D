namespace Electron2D;

/// <summary>Selects how an offscreen canvas retains its previous pixels.</summary>
public enum ViewportClearMode
{
    /// <summary>Clear before every update.</summary>
    Always = 0,
    /// <summary>Retain completed pixels before drawing the next update.</summary>
    Never = 1,
    /// <summary>Clear the next successful update, then select Never.</summary>
    Once = 2,
}
/// <summary>Selects when an offscreen canvas submits a new image.</summary>
public enum ViewportUpdateMode
{
    /// <summary>Retain the completed image without rendering.</summary>
    Disabled = 0,
    /// <summary>Render the next frame, then select Disabled.</summary>
    Once = 1,
    /// <summary>Update when sampled by submitted canvas geometry or a material.</summary>
    WhenVisible = 2,
    /// <summary>Update when its nearest parent viewport updates.</summary>
    WhenParentVisible = 3,
    /// <summary>Update every enabled rendering frame while attached.</summary>
    Always = 4,
}
/// <summary>Renders an independent scene canvas into a live viewport texture without creating a window.</summary>
/// <remarks>Scene processing remains on the existing SceneTree clock. Native input is isolated from this
/// viewport; explicit PushInput supplies its local events. Targets belong to the active rendering server.
/// Ordinary texture drawing and materials sample completed native images without per-frame CPU copies.</remarks>
public sealed class SubViewport : Viewport
{
    private Vector2i _size = new(512, 512), _override;
    private bool _stretch;
    private ViewportClearMode _clear;
    private ViewportUpdateMode _update = ViewportUpdateMode.WhenVisible;
    /// <summary>Creates a 512 by 512 offscreen viewport with Always clear and WhenVisible updates.</summary>
    public SubViewport() { }
    private void Check() { EnsureMutable(); RenderingOwner?.EnsureViewportMutation(); }
    /// <summary>Gets or sets native pixel dimensions, clamping each axis to at least two.</summary>
    /// <value>512 by 512 initially.</value>
    /// <remarks>Native target recreation is a cold operation. Backend size limits are checked before allocation.
    /// A committed size change notifies texture consumers and SizeChanged even after a failing observer.</remarks>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    /// <exception cref="InvalidOperationException">Mutation occurs off-owner or during native submission.</exception>
    public Vector2i Size { get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _size; } set { Check(); value = new(Math.Max(2, value.X), Math.Max(2, value.Y)); if (_size == value) return; _size = value; ChangedSize(); } }
    /// <summary>Gets or sets logical canvas size metadata; a zero vector uses native size.</summary>
    /// <value>Zero initially; finite signed integer axes retain their values.</value>
    /// <remarks>Stretch requires both override axes to be positive. Geometry, controls and camera bounds use the visible logical rectangle.</remarks>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    /// <exception cref="InvalidOperationException">Mutation occurs off-owner or during native submission.</exception>
    public Vector2i Size2DOverride { get { ThrowIfDisposed(); return _override; } set { Check(); if (_override == value) return; _override = value; ChangedSize(); } }
    /// <summary>Gets or sets whether positive logical override axes scale rendering to native dimensions.</summary>
    /// <value>False initially.</value>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    /// <exception cref="InvalidOperationException">Mutation occurs off-owner or during native submission.</exception>
    public bool Size2DOverrideStretch { get { ThrowIfDisposed(); return _stretch; } set { Check(); if (_stretch == value) return; _stretch = value; ChangedSize(); } }
    /// <summary>Gets or sets clear policy; Once becomes Never only after a successful update.</summary>
    /// <value>Always initially.</value>
    /// <exception cref="ArgumentOutOfRangeException">The enumeration is undefined.</exception>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    /// <exception cref="InvalidOperationException">Mutation occurs off-owner or during native submission.</exception>
    public ViewportClearMode RenderTargetClearMode { get { ThrowIfDisposed(); return _clear; } set { Check(); Animation.Valid(value); _clear = value; } }
    /// <summary>Gets or sets update policy; Once becomes Disabled only after a successful update.</summary>
    /// <value>WhenVisible initially.</value>
    /// <exception cref="ArgumentOutOfRangeException">The enumeration is undefined.</exception>
    /// <exception cref="ObjectDisposedException">The viewport is disposed.</exception>
    /// <exception cref="InvalidOperationException">Mutation occurs off-owner or during native submission.</exception>
    public ViewportUpdateMode RenderTargetUpdateMode { get { ThrowIfDisposed(); return _update; } set { Check(); Animation.Valid(value); _update = value; } }
    internal void Submitted() { if (_clear == ViewportClearMode.Once) _clear = ViewportClearMode.Never; if (_update == ViewportUpdateMode.Once) _update = ViewportUpdateMode.Disabled; }
    internal override Transform StretchTransform => _stretch && _override.X > 0 && _override.Y > 0 ? new(new Vector2((float)_size.X / _override.X, 0), new(0, (float)_size.Y / _override.Y), Vector2.Zero) : Transform.Identity;
    private void ChangedSize()
    {
        InvalidateViewportRecording(); NotifySizeChanged();
    }
    /// <inheritdoc />
    public override Rect2 GetVisibleRect() { ThrowIfDisposed(); return new(Vector2.Zero, _override == Vector2i.Zero ? _size : _override); }
    private static readonly PropertyDescriptor[] Properties =
    [
        new PropertyDescriptor<SubViewport, Vector2i>(nameof(Size), n => n.Size, (n, v) => n.Size = v, _ => new(512, 512), stored: true),
        new PropertyDescriptor<SubViewport, Vector2i>(nameof(Size2DOverride), n => n.Size2DOverride, (n, v) => n.Size2DOverride = v, _ => default, stored: true),
        new PropertyDescriptor<SubViewport, bool>(nameof(Size2DOverrideStretch), n => n.Size2DOverrideStretch, (n, v) => n.Size2DOverrideStretch = v, _ => false, stored: true),
        new PropertyDescriptor<SubViewport, ViewportClearMode>(nameof(RenderTargetClearMode), n => n.RenderTargetClearMode, (n, v) => n.RenderTargetClearMode = v, _ => ViewportClearMode.Always, stored: true),
        new PropertyDescriptor<SubViewport, ViewportUpdateMode>(nameof(RenderTargetUpdateMode), n => n.RenderTargetUpdateMode, (n, v) => n.RenderTargetUpdateMode = v, _ => ViewportUpdateMode.WhenVisible, stored: true),
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => CreateDefaultSubViewport;
    private static Node CreateDefaultSubViewport() => new SubViewport();
}
