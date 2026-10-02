namespace Electron2D;

/// <summary>Draws button states from borrowed textures and optionally tests input through a bitmap mask.</summary>
/// <remarks>The normal, pressed, hover and mask resources determine the minimum in that order. Focus is an
/// overlay. Visual flips do not flip the mask. Resource mutations invalidate drawing and layout even when
/// an earlier public change observer throws; detached queries read current resource data. Borrowed disposed
/// references remain assigned and fail when consumed. Attached controls retain known state textures in the renderer
/// cache until replacement or exit; detached configuration does not retain them. Disposing the button never disposes its resources.</remarks>
public class TextureButton : BaseButton
{
    private readonly Resource?[] _resources = new Resource?[6];
    private readonly List<ResourceState> _resourceStates = [];
    private readonly List<Texture> _residentTextures = [];
    private readonly record struct ResourceState(Resource Resource, long Revision, bool Disposed);
    private readonly Action<Resource> _resourceChanged;
    private readonly Action<ElectronObject> _resourceDisposed;
    private TextureStretchMode _stretchMode = TextureStretchMode.Keep;
    private bool _ignoreTextureSize, _flipH, _flipV;
    private Rect2 _positionRect, _textureRegion;
    private bool _tile;
    private int _stateGeneration, _treeGeneration, _resourcePending, _minimumDirty;
    private SceneTree? _resourceTree;
    private Action? _resourceDispatch;
    private static readonly PropertyDescriptor[] ButtonTextureProperties =
    [
        new PropertyDescriptor<TextureButton, Texture?>(nameof(TextureNormal), node => node.TextureNormal, (node, value) => node.TextureNormal = value, _ => null, stored: true),
        new PropertyDescriptor<TextureButton, Texture?>(nameof(TexturePressed), node => node.TexturePressed, (node, value) => node.TexturePressed = value, _ => null, stored: true),
        new PropertyDescriptor<TextureButton, Texture?>(nameof(TextureHover), node => node.TextureHover, (node, value) => node.TextureHover = value, _ => null, stored: true),
        new PropertyDescriptor<TextureButton, Texture?>(nameof(TextureDisabled), node => node.TextureDisabled, (node, value) => node.TextureDisabled = value, _ => null, stored: true),
        new PropertyDescriptor<TextureButton, Texture?>(nameof(TextureFocused), node => node.TextureFocused, (node, value) => node.TextureFocused = value, _ => null, stored: true),
        new PropertyDescriptor<TextureButton, BitMap?>(nameof(TextureClickMask), node => node.TextureClickMask, (node, value) => node.TextureClickMask = value, _ => null, stored: true),
        new PropertyDescriptor<TextureButton, TextureStretchMode>(nameof(StretchMode), node => node.StretchMode, (node, value) => node.StretchMode = value, _ => TextureStretchMode.Keep, stored: true),
        new PropertyDescriptor<TextureButton, bool>(nameof(IgnoreTextureSize), node => node.IgnoreTextureSize, (node, value) => node.IgnoreTextureSize = value, _ => false, stored: true),
        new PropertyDescriptor<TextureButton, bool>(nameof(FlipH), node => node.FlipH, (node, value) => node.FlipH = value, _ => false, stored: true),
        new PropertyDescriptor<TextureButton, bool>(nameof(FlipV), node => node.FlipV, (node, value) => node.FlipV = value, _ => false, stored: true),
    ];

    /// <summary>Creates an empty fully focusable button with natural-size texture placement.</summary>
    public TextureButton()
    {
        _resourceChanged = ResourceChanged;
        _resourceDisposed = resource => ResourceChanged((Resource)resource);
    }

    /// <summary>Gets or sets the borrowed TextureNormal resource.</summary>
    /// <value>Null initially. The normal-state image; other states may fall back to it.</value>
    /// <remarks>Equal references are silent. Assignment commits before layout invalidation.</remarks>
    /// <exception cref="ObjectDisposedException">The button or supplied resource is disposed.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or mutation occurs during scene capture.</exception>
    public Texture? TextureNormal { get { CheckTextureButton(); return (Texture?)_resources[0]; } set => SetResource(0, value); }

    /// <summary>Gets or sets the borrowed TexturePressed resource.</summary>
    /// <value>Null initially. The pressed-state image; absent uses hover then normal.</value>
    /// <remarks>Equal references are silent. Assignment commits before layout invalidation.</remarks>
    /// <exception cref="ObjectDisposedException">The button or supplied resource is disposed.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or mutation occurs during scene capture.</exception>
    public Texture? TexturePressed { get { CheckTextureButton(); return (Texture?)_resources[1]; } set => SetResource(1, value); }

    /// <summary>Gets or sets the borrowed TextureHover resource.</summary>
    /// <value>Null initially. The hover image; absent uses the pressed image while pressed, otherwise normal.</value>
    /// <remarks>Equal references are silent. Assignment commits before layout invalidation.</remarks>
    /// <exception cref="ObjectDisposedException">The button or supplied resource is disposed.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or mutation occurs during scene capture.</exception>
    public Texture? TextureHover { get { CheckTextureButton(); return (Texture?)_resources[2]; } set => SetResource(2, value); }

    /// <summary>Gets or sets the borrowed TextureDisabled resource.</summary>
    /// <value>Null initially. The disabled-state image; absent uses normal.</value>
    /// <remarks>Equal references are silent. Assignment commits before layout invalidation.</remarks>
    /// <exception cref="ObjectDisposedException">The button or supplied resource is disposed.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or mutation occurs during scene capture.</exception>
    public Texture? TextureDisabled { get { CheckTextureButton(); return (Texture?)_resources[3]; } set => SetResource(3, value); }

    /// <summary>Gets or sets the borrowed TextureFocused resource.</summary>
    /// <value>Null initially. The full-image focus overlay, stretched over the selected state rectangle.</value>
    /// <remarks>Equal references are silent. Assignment commits before layout invalidation.</remarks>
    /// <exception cref="ObjectDisposedException">The button or supplied resource is disposed.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or mutation occurs during scene capture.</exception>
    public Texture? TextureFocused { get { CheckTextureButton(); return (Texture?)_resources[4]; } set => SetResource(4, value); }

    /// <summary>Gets or sets the borrowed TextureClickMask resource.</summary>
    /// <value>Null initially. The input mask; unset uses the ordinary control rectangle.</value>
    /// <remarks>Equal references are silent. Assignment commits before layout invalidation.</remarks>
    /// <exception cref="ObjectDisposedException">The button or supplied resource is disposed.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or mutation occurs during scene capture.</exception>
    public BitMap? TextureClickMask { get { CheckTextureButton(); return (BitMap?)_resources[5]; } set => SetResource(5, value); }

    /// <summary>Gets or sets whether textures and the bitmap are excluded from the intrinsic minimum.</summary>
    /// <value>False initially; true supplies a zero intrinsic minimum.</value>
    /// <exception cref="ObjectDisposedException">The button is disposed.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or mutation occurs during scene capture.</exception>
    public bool IgnoreTextureSize
    {
        get { CheckTextureButton(); return _ignoreTextureSize; }
        set { EnsureMutable(); if (_ignoreTextureSize == value) return; _ignoreTextureSize = value; InvalidateState(true); }
    }
    /// <summary>Gets or sets image placement inside the control rectangle.</summary>
    /// <value>Keep initially. Undefined values are retained and use natural top-left placement.</value>
    /// <exception cref="ObjectDisposedException">The button is disposed.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or mutation occurs during scene capture.</exception>
    public TextureStretchMode StretchMode
    {
        get { CheckTextureButton(); return _stretchMode; }
        set { EnsureMutable(); if (_stretchMode == value) return; _stretchMode = value; InvalidateState(false); }
    }
    /// <summary>Gets or sets horizontal reflection of the drawn images, without reflecting the input mask.</summary>
    /// <value>False initially.</value>
    /// <exception cref="ObjectDisposedException">The button is disposed.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or mutation occurs during scene capture.</exception>
    public bool FlipH
    {
        get { CheckTextureButton(); return _flipH; }
        set { EnsureMutable(); if (_flipH == value) return; _flipH = value; InvalidateState(false); }
    }
    /// <summary>Gets or sets vertical reflection of the drawn images, without reflecting the input mask.</summary>
    /// <value>False initially.</value>
    /// <exception cref="ObjectDisposedException">The button is disposed.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off-owner or mutation occurs during scene capture.</exception>
    public bool FlipV
    {
        get { CheckTextureButton(); return _flipV; }
        set { EnsureMutable(); if (_flipV == value) return; _flipV = value; InvalidateState(false); }
    }

    private void CheckTextureButton() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    private bool Contains(Resource resource)
    {
        for (var index = 0; index < _resources.Length; index++)
            if (ReferenceEquals(_resources[index], resource)) return true;
        return false;
    }
    private void SetResource(int slot, Resource? resource)
    {
        EnsureMutable();
        if (resource?.IsDisposed == true) throw new ObjectDisposedException(nameof(resource));
        var previous = _resources[slot];
        if (ReferenceEquals(previous, resource)) return;
        var alreadyWatched = resource is not null && Contains(resource);
        _resources[slot] = resource;
        if (previous is not null && !Contains(previous)) { previous.Changed -= _resourceChanged; previous.Disposed -= _resourceDisposed; }
        if (resource is not null && !alreadyWatched) { resource.Changed += _resourceChanged; resource.Disposed += _resourceDisposed; }
        _resourceStates.Clear(); PollResources();
        InvalidateState(true);
    }
    private void InvalidateState(bool minimum)
    {
        Interlocked.Increment(ref _stateGeneration);
        InvalidateCanvas();
        if (minimum) UpdateMinimumSize();
    }
    private void PollResources()
    {
        var changed = false; var count = 0;
        for (var slot = 0; slot < _resources.Length; slot++)
        {
            var chainStart = count;
            for (var resource = _resources[slot]; resource is not null;)
            {
                var repeated = -1;
                for (var index = 0; index < count; index++)
                    if (ReferenceEquals(_resourceStates[index].Resource, resource)) { repeated = index; break; }
                // Shared slots reuse one entry; concurrent atlas edits may expose a transient path cycle.
                if (repeated >= 0) { changed |= repeated >= chainStart; break; }
                var state = new ResourceState(resource, resource.ChangeRevision, resource.IsDisposed);
                if (count < _resourceStates.Count)
                {
                    var previous = _resourceStates[count];
                    if (!ReferenceEquals(previous.Resource, state.Resource) || previous.Revision != state.Revision || previous.Disposed != state.Disposed) { _resourceStates[count] = state; changed = true; }
                }
                else { _resourceStates.Add(state); changed = true; }
                count++;
                if (state.Disposed || resource is not AtlasTexture atlas) break;
                try { resource = atlas.Atlas; }
                catch (ObjectDisposedException) { changed = true; break; }
            }
        }
        if (count < _resourceStates.Count) { _resourceStates.RemoveRange(count, _resourceStates.Count - count); changed = true; }
        SynchronizeResidency();
        if (!changed) return;
        Interlocked.Increment(ref _stateGeneration); InvalidateCanvas(); Interlocked.Exchange(ref _minimumDirty, 1);
    }
    private void SynchronizeResidency()
    {
        if (_resourceTree is null || !ReferenceEquals(Tree, _resourceTree)) { ReleaseResidency(); return; }
        for (var index = _residentTextures.Count - 1; index >= 0; index--)
        {
            var texture = _residentTextures[index]; var retained = false;
            for (var state = 0; state < _resourceStates.Count; state++)
                if (ReferenceEquals(_resourceStates[state].Resource, texture) && !_resourceStates[state].Disposed) { retained = true; break; }
            if (retained) continue;
            texture.ReleaseRendererCacheResidency(); _residentTextures.RemoveAt(index);
        }
        for (var index = 0; index < _resourceStates.Count; index++)
        {
            var state = _resourceStates[index];
            if (state.Disposed || state.Resource is not Texture texture) continue;
            var retained = false;
            for (var current = 0; current < _residentTextures.Count; current++)
                if (ReferenceEquals(_residentTextures[current], texture)) { retained = true; break; }
            if (retained) continue;
            _residentTextures.Add(texture);
            try { texture.AcquireRendererCacheResidency(); }
            catch (ObjectDisposedException) { _residentTextures.RemoveAt(_residentTextures.Count - 1); }
            catch { _residentTextures.RemoveAt(_residentTextures.Count - 1); throw; }
        }
    }
    private void ReleaseResidency()
    {
        for (var index = _residentTextures.Count - 1; index >= 0; index--)
            _residentTextures[index].ReleaseRendererCacheResidency();
        _residentTextures.Clear();
    }
    private void ResourceChanged(Resource resource)
    {
        if (IsDisposed || !Contains(resource)) return;
        InvalidateCanvas(); Interlocked.Exchange(ref _minimumDirty, 1); Interlocked.Increment(ref _stateGeneration);
        var tree = Volatile.Read(ref _resourceTree); var dispatch = Volatile.Read(ref _resourceDispatch);
        if (tree is null || dispatch is null) return;
        try { EnsureMutable(); FlushResources(); return; }
        catch (ObjectDisposedException) when (IsDisposed) { return; }
        catch (InvalidOperationException) { }
        if (Interlocked.Exchange(ref _resourcePending, 1) != 0) return;
        try { tree.Defer(dispatch); }
        catch (ObjectDisposedException) { Interlocked.Exchange(ref _resourcePending, 0); }
    }
    private void FlushResources()
    {
        Interlocked.Exchange(ref _resourcePending, 0); PollResources();
        if (Interlocked.Exchange(ref _minimumDirty, 0) != 0) UpdateMinimumSize();
    }

    /// <inheritdoc />
    /// <exception cref="ObjectDisposedException">The selected minimum-size resource is disposed.</exception>
    /// <exception cref="InvalidOperationException">A virtual texture size query continuously changes the button.</exception>
    protected override Vector2 OnGetMinimumSize()
    {
        for (var attempt = 0; attempt < 64; attempt++)
        {
            CheckTextureButton(); PollResources();
            if (_ignoreTextureSize) return Vector2.Zero;
            var generation = Volatile.Read(ref _stateGeneration);
            var resource = _resources[0] ?? _resources[1] ?? _resources[2] ?? _resources[5];
            var size = resource is Texture texture ? texture.GetSize() : resource is BitMap mask ? (Vector2)mask.GetSize() : Vector2.Zero;
            ThrowIfDisposed();
            if (generation == Volatile.Read(ref _stateGeneration)) return size.Abs();
        }
        throw new InvalidOperationException("Texture-button minimum-size queries did not settle.");
    }

    /// <inheritdoc />
    /// <remarks>Uses the most recently recorded image rectangle; before a recording or for an empty rectangle,
    /// mask pixels use their natural coordinates. Texture reflection does not reflect the mask.</remarks>
    /// <exception cref="ObjectDisposedException">The assigned input mask is disposed.</exception>
    protected override bool HasPoint(Vector2 point)
    {
        CheckTextureButton();
        if (_resources[5] is not BitMap mask) return base.HasPoint(point);
        var maskSize = (Vector2)mask.GetSize();
        if (!point.IsFinite() || maskSize.X <= 0 || maskSize.Y <= 0) return false;
        var rect = new Rect2(Vector2.Zero, maskSize);
        if (_positionRect.HasArea())
        {
            if (_tile)
            {
                if (_positionRect.HasPoint(point)) { point.X %= maskSize.X; point.Y %= maskSize.Y; }
            }
            else
            {
                var offset = _positionRect.Position; var scale = maskSize / _positionRect.Size;
                if (_stretchMode == TextureStretchMode.KeepAspectCovered)
                {
                    var minimum = MathF.Min(scale.X, scale.Y); scale = new(minimum, minimum);
                    offset -= _textureRegion.Position / minimum;
                }
                point = (point - offset) * scale;
                rect = new(_textureRegion.Position.Max(0), maskSize.Min(_textureRegion.Size));
            }
        }
        if (!point.IsFinite() || !rect.HasPoint(point) || point.X < 0 || point.Y < 0 || point.X >= maskSize.X || point.Y >= maskSize.Y) return false;
        try { return mask.GetBit((int)point.X, (int)point.Y); }
        catch (ArgumentOutOfRangeException) { return false; }
    }

    private Texture? SelectedTexture(DrawMode mode) => mode switch
    {
        DrawMode.Pressed or DrawMode.HoverPressed => (Texture?)(_resources[1] ?? _resources[2] ?? _resources[0]),
        DrawMode.Hover => (Texture?)(_resources[2] ?? (ButtonPressed ? _resources[1] : null) ?? _resources[0]),
        DrawMode.Disabled => (Texture?)(_resources[3] ?? _resources[0]),
        _ => (Texture?)_resources[0]
    };

    private void DrawButtonTextures()
    {
        for (var attempt = 0; attempt < 64; attempt++)
        {
            PollResources(); var generation = Volatile.Read(ref _stateGeneration);
            var mode = GetDrawMode(); var hasFocus = HasFocus(true); var controlSize = Size; var tree = Tree;
            var texture = SelectedTexture(mode); var focused = hasFocus ? (Texture?)_resources[4] : null;
            var focusOnly = texture is null && focused is not null; texture ??= focused;
            var mask = (BitMap?)_resources[5]; var offset = Vector2.Zero; var size = Vector2.Zero; var region = default(Rect2); var tile = false;
            if (texture is not null || mask is not null)
            {
                var natural = texture is not null ? texture.GetSize() : (Vector2)mask!.GetSize();
                ThrowIfDisposed(); if (!IsCurrentDrawState(generation, mode, hasFocus, controlSize, tree)) continue;
                if (!natural.IsFinite() || natural.X < 0 || natural.Y < 0) throw new InvalidOperationException("Texture dimensions must be finite and nonnegative.");
                size = natural; region = new(Vector2.Zero, natural);
                switch (_stretchMode)
                {
                    case TextureStretchMode.Scale: size = Size; break;
                    case TextureStretchMode.Tile: size = Size; tile = true; break;
                    case TextureStretchMode.KeepCentered: offset = (Size - natural) / 2; break;
                    case TextureStretchMode.KeepAspect:
                    case TextureStretchMode.KeepAspectCentered:
                        if (natural.X == 0 || natural.Y == 0) { size = Vector2.Zero; break; }
                        var width = natural.X * Size.Y / natural.Y; var height = Size.Y;
                        if (width > Size.X) { width = Size.X; height = natural.Y * width / natural.X; }
                        size = new(width, height);
                        if (_stretchMode == TextureStretchMode.KeepAspectCentered) offset = (Size - size) / 2;
                        break;
                    case TextureStretchMode.KeepAspectCovered:
                        size = Size;
                        if (natural.X == 0 || natural.Y == 0 || size.X == 0 || size.Y == 0) { size = Vector2.Zero; region = default; break; }
                        var ratios = size / natural; var scale = MathF.Max(ratios.X, ratios.Y);
                        region = new(((natural * scale - size) / scale).Abs() / 2, size / scale);
                        break;
                }
                if (!size.IsFinite() || !offset.IsFinite() || !region.IsFinite()) throw new InvalidOperationException("Texture-button geometry overflowed.");
            }
            _positionRect = new(offset, size); _textureRegion = region; _tile = tile;
            if (_flipH) size.X = -size.X;
            if (_flipV) size.Y = -size.Y;
            if (!focusOnly && texture is not null)
            {
                if (tile) DrawTextureRect(texture, new(offset, size), true);
                else DrawTextureRectRegion(texture, new(offset, size), region);
            }
            if (!IsCurrentDrawState(generation, mode, hasFocus, controlSize, tree)) { InvalidateCanvas(); return; }
            if (focused is not null) DrawTextureRect(focused, new(offset, size), false);
            if (!IsCurrentDrawState(generation, mode, hasFocus, controlSize, tree)) InvalidateCanvas();
            return;
        }
        throw new InvalidOperationException("Texture-button drawing queries did not settle.");
    }

    private bool IsCurrentDrawState(int generation, DrawMode mode, bool hasFocus, Vector2 size, SceneTree? tree) =>
        !IsDisposed && generation == Volatile.Read(ref _stateGeneration) && ReferenceEquals(Tree, tree) &&
        GetDrawMode() == mode && HasFocus(true) == hasFocus && Size == size;

    private void EnterResourceTree()
    {
        var tree = Tree!; var generation = ++_treeGeneration;
        _resourceTree = tree;
        _resourceDispatch = () => { if (!IsDisposed && ReferenceEquals(Tree, tree) && _treeGeneration == generation) FlushResources(); };
        SetButtonResourceProcessing(true); PollResources(); InvalidateState(true);
    }

    /// <inheritdoc />
    /// <exception cref="Exception">Inherited notifications or borrowed texture drawing fail.</exception>
    protected override void OnNotification(int what)
    {
        if (what == NotificationDraw) { base.OnNotification(what); DrawButtonTextures(); return; }
        List<Exception>? errors = null;
        try { base.OnNotification(what); } catch (Exception error) { CollectException(ref errors, error); }
        if (!IsDisposed)
            try
            {
                switch (what)
                {
                    case NotificationEnterTree: EnterResourceTree(); break;
                    case NotificationExitTree:
                        _treeGeneration++; _resourceTree = null; _resourceDispatch = null; Interlocked.Exchange(ref _resourcePending, 0);
                        ReleaseResidency(); _resourceStates.Clear(); SetButtonResourceProcessing(false); break;
                    case NotificationInternalProcess: FlushResources(); break;
                    case NotificationResized:
                    case NotificationFocusEnter:
                    case NotificationFocusExit: InvalidateState(false); break;
                }
            }
            catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Texture-button notification callbacks failed.", errors);
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var descriptor in base.GetPropertyDescriptors()) yield return descriptor;
        foreach (var descriptor in ButtonTextureProperties) yield return descriptor;
    }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(TextureButton) ? CreateTextureButton : base.CreateSceneInstanceFactory();
    private static Node CreateTextureButton() => new TextureButton();
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _resourceTree = null; _resourceDispatch = null; _treeGeneration++; ReleaseResidency(); _resourceStates.Clear();
            for (var index = 0; index < _resources.Length; index++)
            {
                var resource = _resources[index]; _resources[index] = null;
                if (resource is not null && !Contains(resource)) { resource.Changed -= _resourceChanged; resource.Disposed -= _resourceDisposed; }
            }
        }
        base.Dispose(disposing);
    }
}
