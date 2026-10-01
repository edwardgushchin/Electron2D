namespace Electron2D;

/// <summary>Controls how a texture rectangle contributes to its minimum size.</summary>
public enum TextureRectExpandMode
{
    /// <summary>Uses the texture's logical size.</summary>
    KeepSize = 0,
    /// <summary>Contributes no texture minimum.</summary>
    IgnoreSize = 1,
    /// <summary>Uses current control height as minimum width.</summary>
    FitWidth = 2,
    /// <summary>Derives minimum width from current height and texture aspect.</summary>
    FitWidthProportional = 3,
    /// <summary>Uses current control width as minimum height.</summary>
    FitHeight = 4,
    /// <summary>Derives minimum height from current width and texture aspect.</summary>
    FitHeightProportional = 5
}

/// <summary>Controls texture placement inside an image control.</summary>
public enum TextureRectStretchMode
{
    /// <summary>Stretches to the full control rectangle.</summary>
    Scale = 0,
    /// <summary>Repeats at natural logical pixel size.</summary>
    Tile = 1,
    /// <summary>Keeps natural size at the leading top-left position.</summary>
    Keep = 2,
    /// <summary>Centers natural size.</summary>
    KeepCentered = 3,
    /// <summary>Fits aspect with integer-truncated dimensions.</summary>
    KeepAspect = 4,
    /// <summary>Centers an aspect-preserving integer fit.</summary>
    KeepAspectCentered = 5,
    /// <summary>Covers the rectangle using a centered source crop.</summary>
    KeepAspectCovered = 6
}

/// <summary>Draws a borrowed texture with configurable stretching, tiling, aspect and reflection.</summary>
/// <remarks>Mouse input passes through by default. Texture assignment is stored; resources remain caller-owned.
/// Resource revisions are polled so failed or off-thread observers cannot lose redraw/minimum invalidation.
/// Size-dependent expand modes participate in deferred container layout; multi-wrap flow retains current fitting.</remarks>
public class TextureRect : Control
{
    private Texture? _texture;
    private TextureRectExpandMode _expandMode;
    private TextureRectStretchMode _stretchMode;
    private bool _flipH, _flipV;
    private int _generation, _pending;
    private SceneTree? _resourceTree;
    private Action? _resourceDispatch;
    private ulong _membership;
    private readonly List<SourceState> _sources = [];
    private readonly record struct SourceState(Texture Texture, long Revision, bool Disposed);
    private static readonly PropertyDescriptor[] TextureProperties =
    [
        new PropertyDescriptor<TextureRect, Texture?>(nameof(Texture), node => node.Texture, (node, value) => node.Texture = value, _ => null, stored: true),
        new PropertyDescriptor<TextureRect, TextureRectExpandMode>(nameof(ExpandMode), node => node.ExpandMode, (node, value) => node.ExpandMode = value, _ => TextureRectExpandMode.KeepSize, stored: true),
        new PropertyDescriptor<TextureRect, TextureRectStretchMode>(nameof(StretchMode), node => node.StretchMode, (node, value) => node.StretchMode = value, _ => TextureRectStretchMode.Scale, stored: true),
        new PropertyDescriptor<TextureRect, bool>(nameof(FlipH), node => node.FlipH, (node, value) => node.FlipH = value, _ => false, stored: true),
        new PropertyDescriptor<TextureRect, bool>(nameof(FlipV), node => node.FlipV, (node, value) => node.FlipV = value, _ => false, stored: true),
        new PropertyDescriptor<TextureRect, MouseFilter>(nameof(MouseFilter), node => node.MouseFilter, (node, value) => node.MouseFilter = value, _ => MouseFilter.Pass, stored: true)
    ];
    /// <summary>Creates an empty KeepSize/Scale image control with passing pointer input.</summary>
    public TextureRect() => MouseFilter = MouseFilter.Pass;
    private void Check() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    /// <summary>Gets or sets the borrowed image texture.</summary>
    /// <value>Null initially. Equal references are silent; resource disposal clears the binding on the owner thread.</value>
    /// <exception cref="ObjectDisposedException">The control or assigned texture is disposed.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="Exception">Warning/minimum callbacks fail after assignment commits.</exception>
    public Texture? Texture
    {
        get { Check(); return _texture; }
        set
        {
            EnsureMutable(); if (value is { IsDisposed: true }) throw new ObjectDisposedException(nameof(value));
            if (ReferenceEquals(value, _texture)) return;
            if (_texture is { } old) { old.Changed -= TextureChanged; old.Disposed -= TextureDisposed; }
            _texture = value; _sources.Clear(); Interlocked.Increment(ref _generation);
            if (value is not null) { value.Changed += TextureChanged; value.Disposed += TextureDisposed; }
            Invalidate(true);
        }
    }
    /// <summary>Gets or sets the texture-derived minimum-size policy.</summary>
    /// <value>KeepSize initially. Changes redraw and refresh minimum size.</value>
    /// <exception cref="ArgumentOutOfRangeException">The mode is undefined.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public TextureRectExpandMode ExpandMode
    {
        get { Check(); return _expandMode; }
        set { EnsureMutable(); if (value is < TextureRectExpandMode.KeepSize or > TextureRectExpandMode.FitHeightProportional) throw new ArgumentOutOfRangeException(nameof(value)); if (_expandMode == value) return; _expandMode = value; Interlocked.Increment(ref _generation); Invalidate(false); }
    }
    /// <summary>Gets or sets image placement within the control.</summary>
    /// <value>Scale initially; changing the mode redraws and refreshes configuration warnings.</value>
    /// <exception cref="ArgumentOutOfRangeException">The mode is undefined.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public TextureRectStretchMode StretchMode
    {
        get { Check(); return _stretchMode; }
        set { EnsureMutable(); if (value is < TextureRectStretchMode.Scale or > TextureRectStretchMode.KeepAspectCovered) throw new ArgumentOutOfRangeException(nameof(value)); if (_stretchMode == value) return; _stretchMode = value; Interlocked.Increment(ref _generation); QueueRedraw(); UpdateConfigurationWarnings(); }
    }
    /// <summary>Gets or sets horizontal visual reflection without moving the occupied rectangle.</summary>
    /// <value>False initially.</value>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public bool FlipH { get { Check(); return _flipH; } set { EnsureMutable(); if (_flipH == value) return; _flipH = value; Interlocked.Increment(ref _generation); QueueRedraw(); } }
    /// <summary>Gets or sets vertical visual reflection without changing minimum size.</summary>
    /// <value>False initially.</value>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    /// <exception cref="ObjectDisposedException">The control is disposed.</exception>
    public bool FlipV { get { Check(); return _flipV; } set { EnsureMutable(); if (_flipV == value) return; _flipV = value; Interlocked.Increment(ref _generation); QueueRedraw(); } }
    internal bool HasSizeDependentMinimum => _expandMode >= TextureRectExpandMode.FitWidth;
    private void Invalidate(bool warnings)
    {
        QueueRedraw(); UpdateMinimumSize(); if (warnings) UpdateConfigurationWarnings();
    }
    private void TextureChanged(Resource source)
    {
        if (IsDisposed || !ReferenceEquals(source, _texture)) return;
        InvalidateCanvas(); Interlocked.Increment(ref _generation);
        var tree = Volatile.Read(ref _resourceTree); var dispatch = Volatile.Read(ref _resourceDispatch);
        var mutable = false;
        try { EnsureMutable(); mutable = true; }
        catch (ObjectDisposedException) when (IsDisposed) { return; }
        catch (InvalidOperationException) { }
        if (mutable) { PollSources(); return; }
        if (tree is null || dispatch is null || Interlocked.Exchange(ref _pending, 1) != 0) return;
        try { tree.Defer(dispatch); } catch (ObjectDisposedException) { Interlocked.Exchange(ref _pending, 0); }
    }
    private void TextureDisposed(ElectronObject source) => TextureChanged((Resource)source);
    private void PollSources()
    {
        var count = 0; var changed = false; var source = _texture;
        while (source is not null)
        {
            var repeated = false; for (var i = 0; i < count; i++) if (ReferenceEquals(_sources[i].Texture, source)) { repeated = true; break; }
            if (repeated) break;
            var state = new SourceState(source, source.ChangeRevision, source.IsDisposed);
            if (count < _sources.Count)
            {
                var old = _sources[count]; changed |= !ReferenceEquals(old.Texture, source) || old.Revision != state.Revision || old.Disposed != state.Disposed; _sources[count] = state;
            }
            else { _sources.Add(state); changed = true; }
            count++;
            if (state.Disposed) { if (ReferenceEquals(source, _texture)) Texture = null; break; }
            if (source is not AtlasTexture atlas) break;
            try { source = atlas.Atlas; }
            catch (ObjectDisposedException) { changed = true; break; }
        }
        if (count < _sources.Count) { _sources.RemoveRange(count, _sources.Count - count); changed = true; }
        if (changed) { Interlocked.Increment(ref _generation); InvalidateCanvas(); Invalidate(true); }
    }
    /// <inheritdoc />
    /// <returns>The current texture-derived intrinsic minimum; proportional modes return zero for an empty denominator.</returns>
    /// <exception cref="InvalidOperationException">Texture dimensions or derived geometry are invalid, or size callbacks do not settle.</exception>
    protected override Vector2 OnGetMinimumSize()
    {
        for (var attempt = 0; attempt < 64; attempt++)
        {
            Check(); PollSources();
            var texture = _texture; var mode = _expandMode; var controlSize = Size; var generation = Volatile.Read(ref _generation);
            if (texture is null || mode == TextureRectExpandMode.IgnoreSize) return Vector2.Zero;
            var natural = NaturalSize(texture); ThrowIfDisposed();
            if (generation != Volatile.Read(ref _generation) || !ReferenceEquals(texture, _texture) || controlSize != Size) continue;
            var result = mode switch
            {
                TextureRectExpandMode.KeepSize => natural,
                TextureRectExpandMode.FitWidth => new(controlSize.Y, 0),
                TextureRectExpandMode.FitWidthProportional => natural.Y == 0 ? Vector2.Zero : new(controlSize.Y * (natural.X / natural.Y), 0),
                TextureRectExpandMode.FitHeight => new(0, controlSize.X),
                _ => natural.X == 0 ? Vector2.Zero : new Vector2(0, controlSize.X * (natural.Y / natural.X))
            };
            if (!result.IsFinite()) throw new InvalidOperationException("Texture-derived minimum size exceeds finite geometry range.");
            return result;
        }
        throw new InvalidOperationException("Texture rectangle minimum-size callbacks did not settle.");
    }

    private static Vector2 NaturalSize(Texture texture)
    {
        var size = texture.GetSize();
        if (!size.IsFinite() || size.X < 0 || size.Y < 0) throw new InvalidOperationException("Texture dimensions must be finite and nonnegative.");
        return size;
    }
    private void DrawImage()
    {
        for (var attempt = 0; attempt < 64; attempt++)
        {
            PollSources(); var texture = _texture; if (texture is null) return;
            var generation = Volatile.Read(ref _generation); var controlSize = Size; var mode = _stretchMode;
            var natural = NaturalSize(texture);
            ThrowIfDisposed(); if (generation != Volatile.Read(ref _generation) || !ReferenceEquals(texture, _texture) || controlSize != Size) continue;
            if (natural.X == 0 || natural.Y == 0) return;
            var size = controlSize; var offset = Vector2.Zero; var region = default(Rect2);
            switch (mode)
            {
                case TextureRectStretchMode.Keep: size = natural; break;
                case TextureRectStretchMode.KeepCentered: size = natural; offset = (controlSize - size) / 2; break;
                case TextureRectStretchMode.KeepAspect:
                case TextureRectStretchMode.KeepAspectCentered:
                    var width = Pixel(natural.X * controlSize.Y / natural.Y); var height = Pixel(controlSize.Y);
                    if (width > controlSize.X) { width = Pixel(controlSize.X); height = Pixel(natural.Y * width / natural.X); }
                    size = new(width, height); if (mode == TextureRectStretchMode.KeepAspectCentered) offset = (controlSize - size) / 2;
                    break;
                case TextureRectStretchMode.KeepAspectCovered:
                    if (size.X == 0 || size.Y == 0) return;
                    var scale = MathF.Max(size.X / natural.X, size.Y / natural.Y);
                    region = new(((natural * scale - size) / scale).Abs() / 2, size / scale); break;
            }
            if (_flipH) size.X = -size.X; if (_flipV) size.Y = -size.Y;
            if (!size.IsFinite() || !offset.IsFinite() || !region.IsFinite()) throw new InvalidOperationException("Texture rectangle geometry overflowed.");
            if (region.HasArea()) DrawTextureRectRegion(texture, new(offset, size), region);
            else if (mode == TextureRectStretchMode.Tile && texture is AtlasTexture) RecordNinePatch(texture, new(offset, size), new(Vector2.Zero, natural), new(Vector2.Zero, Vector2.Zero, NinePatchRect.AxisStretchMode.Tile, NinePatchRect.AxisStretchMode.Tile, true));
            else DrawTextureRect(texture, new(offset, size), mode == TextureRectStretchMode.Tile);
            return;
        }
        throw new InvalidOperationException("Texture rectangle size callbacks did not settle.");
    }
    private static int Pixel(float value)
    {
        if (!float.IsFinite(value) || (double)value > int.MaxValue || (double)value < int.MinValue) throw new InvalidOperationException("Texture fit exceeds integer pixel range.");
        return (int)value;
    }
    /// <inheritdoc />
    /// <returns>A caller-owned array with inherited warnings and one warning for tiled atlas margins.</returns>
    /// <remarks>Any nonzero margin in the nested atlas chain makes Tile unsupported. Other modes add no warning.</remarks>
    public override string[] GetConfigurationWarnings()
    {
        var warnings = base.GetConfigurationWarnings();
        if (_stretchMode != TextureRectStretchMode.Tile) return warnings;
        var visited = new HashSet<AtlasTexture>();
        for (var source = _texture as AtlasTexture; source is not null && visited.Add(source); source = source.Atlas as AtlasTexture)
            if (source.Margin != default) return warnings.Append("Tiled atlas textures with non-zero margins are unsupported.").ToArray();
        return warnings;
    }
    private void EnterResourceTree()
    {
        var tree = Tree!; var membership = ++_membership; _resourceTree = tree;
        _resourceDispatch = () => { if (!IsDisposed && ReferenceEquals(Tree, tree) && membership == _membership) { Interlocked.Exchange(ref _pending, 0); PollSources(); } };
        SetInternalProcessing(true, false); PollSources();
    }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        List<Exception>? errors = null;
        try { base.OnNotification(what); } catch (Exception error) { CollectException(ref errors, error); }
        if (!IsDisposed) try
            {
                if (what == NotificationEnterTree)
                {
                    EnterResourceTree();
                }
                else if (what == NotificationExitTree) { _membership++; _resourceTree = null; _resourceDispatch = null; Interlocked.Exchange(ref _pending, 0); SetInternalProcessing(false, false); }
                else if (what == NotificationInternalProcess) PollSources();
                else if (what == NotificationResized) UpdateMinimumSize();
                else if (what == NotificationDraw) DrawImage();
            }
            catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Texture rectangle notification callbacks failed.", errors);
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Where(p => p.Name != nameof(MouseFilter)).Concat(TextureProperties);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(TextureRect) ? CreateRect : base.CreateSceneInstanceFactory();
    private static Node CreateRect() => new TextureRect();
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) { if (_texture is { } texture) { texture.Changed -= TextureChanged; texture.Disposed -= TextureDisposed; } _texture = null; _sources.Clear(); _resourceTree = null; _resourceDispatch = null; _membership++; }
        base.Dispose(disposing);
    }
}
