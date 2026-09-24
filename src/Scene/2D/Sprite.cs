namespace Electron2D;

/// <summary>Displays a texture, a sheet frame or an atlas region as a scene node.</summary>
/// <remarks>Textures are borrowed. Mutations use the scene owner thread while attached. Texture change notifications
/// request a later redraw without running scene work on the notifying thread. Rendering inherits Entity transforms,
/// visibility, modulation and materials. There is no animation clock; change Frame directly or through a Tween.
/// Geometry changes raise inherited ItemRectChanged synchronously. Exceptions stop subsequent callbacks for that
/// mutation; state and redraw remain committed. Disposal from a callback suppresses subsequent event stages.</remarks>
public class Sprite : Entity
{
    private static readonly PropertyDescriptor[] SpriteProperties =
    [
        new PropertyDescriptor<Sprite, Texture?>(nameof(Texture), s => s.Texture, (s, v) => s.Texture = v, _ => null, stored: true),
        new PropertyDescriptor<Sprite, bool>(nameof(Centered), s => s.Centered, (s, v) => s.Centered = v, _ => true, stored: true),
        new PropertyDescriptor<Sprite, Vector2>(nameof(Offset), s => s.Offset, (s, v) => s.Offset = v, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<Sprite, bool>(nameof(FlipH), s => s.FlipH, (s, v) => s.FlipH = v, _ => false, stored: true),
        new PropertyDescriptor<Sprite, bool>(nameof(FlipV), s => s.FlipV, (s, v) => s.FlipV = v, _ => false, stored: true),
        new PropertyDescriptor<Sprite, bool>(nameof(RegionEnabled), s => s.RegionEnabled, (s, v) => s.RegionEnabled = v, _ => false, stored: true),
        new PropertyDescriptor<Sprite, Rect>(nameof(RegionRect), s => s.RegionRect, (s, v) => s.RegionRect = v, _ => default, stored: true),
        new PropertyDescriptor<Sprite, bool>(nameof(RegionFilterClipEnabled), s => s.RegionFilterClipEnabled, (s, v) => s.RegionFilterClipEnabled = v, _ => false, stored: true),
        // Restore both dimensions before restoring the frame. FrameCoords is a nonstored alias.
        new PropertyDescriptor<Sprite, int>(nameof(HFrames), s => s.HFrames, (s, v) => s.HFrames = v, _ => 1, stored: true),
        new PropertyDescriptor<Sprite, int>(nameof(VFrames), s => s.VFrames, (s, v) => s.VFrames = v, _ => 1, stored: true),
        new PropertyDescriptor<Sprite, int>(nameof(Frame), s => s.Frame, (s, v) => s.Frame = v, _ => 0, stored: true),
        new PropertyDescriptor<Sprite, Vector2i>(nameof(FrameCoords), s => s.FrameCoords, (s, v) => s.FrameCoords = v, _ => Vector2i.Zero),
    ];

    private Texture? _texture;
    private bool _centered = true, _flipH, _flipV, _regionEnabled, _regionFilterClipEnabled;
    private Vector2 _offset;
    private Rect _regionRect;
    private int _hframes = 1, _vframes = 1, _frame;

    /// <summary>Creates a centered sprite with no texture and a one-by-one frame grid.</summary>
    public Sprite() { }

    /// <summary>Gets or sets the borrowed texture to display.</summary>
    /// <value>Null by default. Reassigning the same resource does nothing.</value>
    /// <remarks>Replacement unsubscribes from the old resource, requests redraw, emits TextureChanged, then ItemRectChanged.
    /// Pixel or size changes to the same resource request redraw without emitting either event.</remarks>
    /// <exception cref="ObjectDisposedException">The sprite or assigned texture is disposed.</exception>
    /// <exception cref="InvalidOperationException">Scene mutation is unavailable on this thread or during capture.</exception>
    /// <exception cref="Exception">A TextureChanged or ItemRectChanged subscriber throws after the new value is committed.</exception>
    public Texture? Texture
    {
        get { ThrowIfDisposed(); return _texture; }
        set
        {
            EnsureMutable();
            if (value is { IsDisposed: true }) throw new ObjectDisposedException(nameof(value));
            if (ReferenceEquals(_texture, value)) return;
            if (_texture is not null) _texture.Changed -= TextureContentChanged;
            _texture = value;
            if (_texture is not null) _texture.Changed += TextureContentChanged;
            InvalidateCanvas();
            TextureChanged?.Invoke();
            if (!IsDisposed) NotifyItemRectChanged();
        }
    }

    /// <summary>Gets or sets whether the frame is centered around Offset.</summary>
    /// <value>True by default; otherwise Offset is its top-left corner.</value>
    /// <remarks>An actual change requests redraw and emits ItemRectChanged.</remarks>
    /// <exception cref="Exception">An ItemRectChanged subscriber throws after the change is committed.</exception>
    /// <exception cref="InvalidOperationException">Scene mutation is unavailable on this thread or during capture.</exception>
    /// <exception cref="ObjectDisposedException">The sprite is disposed.</exception>
    public bool Centered
    {
        get { ThrowIfDisposed(); return _centered; }
        set { EnsureMutable(); if (_centered == value) return; _centered = value; InvalidateCanvas(); NotifyItemRectChanged(); }
    }

    /// <summary>Gets or sets the finite local drawing offset.</summary>
    /// <value>Zero by default; positive Y points down.</value>
    /// <remarks>An actual change requests redraw and emits ItemRectChanged.</remarks>
    /// <exception cref="Exception">An ItemRectChanged subscriber throws after the change is committed.</exception>
    /// <exception cref="ArgumentException">The offset is not finite.</exception>
    /// <exception cref="InvalidOperationException">Scene mutation is unavailable on this thread or during capture.</exception>
    /// <exception cref="ObjectDisposedException">The sprite is disposed.</exception>
    public Vector2 Offset
    {
        get { ThrowIfDisposed(); return _offset; }
        set
        {
            EnsureMutable();
            if (!value.IsFinite()) throw new ArgumentException("A sprite offset must be finite.", nameof(value));
            if (_offset == value) return;
            _offset = value; InvalidateCanvas(); NotifyItemRectChanged();
        }
    }

    /// <summary>Gets or sets horizontal image flipping without moving the destination rectangle.</summary>
    /// <value>False by default.</value>
    /// <exception cref="InvalidOperationException">Scene mutation is unavailable on this thread or during capture.</exception>
    /// <exception cref="ObjectDisposedException">The sprite is disposed.</exception>
    public bool FlipH
    {
        get { ThrowIfDisposed(); return _flipH; }
        set { EnsureMutable(); if (_flipH == value) return; _flipH = value; InvalidateCanvas(); }
    }

    /// <summary>Gets or sets vertical image flipping without moving the destination rectangle.</summary>
    /// <value>False by default.</value>
    /// <exception cref="InvalidOperationException">Scene mutation is unavailable on this thread or during capture.</exception>
    /// <exception cref="ObjectDisposedException">The sprite is disposed.</exception>
    public bool FlipV
    {
        get { ThrowIfDisposed(); return _flipV; }
        set { EnsureMutable(); if (_flipV == value) return; _flipV = value; InvalidateCanvas(); }
    }

    /// <summary>Gets or sets whether RegionRect supplies the sheet area instead of the complete texture.</summary>
    /// <value>False by default.</value>
    /// <exception cref="InvalidOperationException">Scene mutation is unavailable on this thread or during capture.</exception>
    /// <exception cref="ObjectDisposedException">The sprite is disposed.</exception>
    public bool RegionEnabled
    {
        get { ThrowIfDisposed(); return _regionEnabled; }
        set { EnsureMutable(); if (_regionEnabled == value) return; _regionEnabled = value; InvalidateCanvas(); }
    }

    /// <summary>Gets or sets the source region in logical texture pixels.</summary>
    /// <value>A zero rectangle by default. It is divided by HFrames and VFrames when enabled.</value>
    /// <remarks>Zero-area regions draw nothing. Negative sizes follow the texture drawing flip contract.
    /// An actual change requests redraw and emits ItemRectChanged only while RegionEnabled is true.</remarks>
    /// <exception cref="Exception">An ItemRectChanged subscriber throws after the change is committed.</exception>
    /// <exception cref="ArgumentException">The rectangle is not finite.</exception>
    /// <exception cref="InvalidOperationException">Scene mutation is unavailable on this thread or during capture.</exception>
    /// <exception cref="ObjectDisposedException">The sprite is disposed.</exception>
    public Rect RegionRect
    {
        get { ThrowIfDisposed(); return _regionRect; }
        set
        {
            EnsureMutable();
            if (!value.IsFinite()) throw new ArgumentException("A sprite region must be finite.", nameof(value));
            if (_regionRect == value) return;
            _regionRect = value;
            if (_regionEnabled) { InvalidateCanvas(); NotifyItemRectChanged(); }
        }
    }

    /// <summary>Gets or sets whether enabled region sampling is limited to the selected frame's texel centers.</summary>
    /// <value>False by default. This has no effect while RegionEnabled is false.</value>
    /// <exception cref="InvalidOperationException">Scene mutation is unavailable on this thread or during capture.</exception>
    /// <exception cref="ObjectDisposedException">The sprite is disposed.</exception>
    public bool RegionFilterClipEnabled
    {
        get { ThrowIfDisposed(); return _regionFilterClipEnabled; }
        set { EnsureMutable(); if (_regionFilterClipEnabled == value) return; _regionFilterClipEnabled = value; InvalidateCanvas(); }
    }

    /// <summary>Gets or sets the number of sheet columns.</summary>
    /// <value>One by default; always positive.</value>
    /// <remarks>Preserves the selected column and row when possible, otherwise resets Frame to zero.
    /// Grid changes request redraw, emit ItemRectChanged, then notify the property list, without FrameChanged for this implicit adjustment.</remarks>
    /// <exception cref="Exception">An ItemRectChanged or PropertyListChanged subscriber throws after the change is committed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The count is not positive or the grid exceeds Int32.MaxValue frames.</exception>
    /// <exception cref="InvalidOperationException">Scene mutation is unavailable on this thread or during capture.</exception>
    /// <exception cref="ObjectDisposedException">The sprite is disposed.</exception>
    public int HFrames
    {
        get { ThrowIfDisposed(); return _hframes; }
        set { SetGrid(value, _vframes); }
    }

    /// <summary>Gets or sets the number of sheet rows.</summary>
    /// <value>One by default; always positive.</value>
    /// <remarks>Preserves the selected column and row when possible, otherwise resets Frame to zero.
    /// Grid changes request redraw, emit ItemRectChanged, then notify the property list, without FrameChanged for this implicit adjustment.</remarks>
    /// <exception cref="Exception">An ItemRectChanged or PropertyListChanged subscriber throws after the change is committed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The count is not positive or the grid exceeds Int32.MaxValue frames.</exception>
    /// <exception cref="InvalidOperationException">Scene mutation is unavailable on this thread or during capture.</exception>
    /// <exception cref="ObjectDisposedException">The sprite is disposed.</exception>
    public int VFrames
    {
        get { ThrowIfDisposed(); return _vframes; }
        set { SetGrid(_hframes, value); }
    }

    /// <summary>Gets or sets the zero-based sheet frame, ordered by column then row.</summary>
    /// <value>Zero by default; less than HFrames multiplied by VFrames.</value>
    /// <remarks>An actual change requests redraw and emits ItemRectChanged before FrameChanged, even if bounds stay equal.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the current grid.</exception>
    /// <exception cref="Exception">An ItemRectChanged or FrameChanged subscriber throws after the change is committed.</exception>
    /// <exception cref="InvalidOperationException">Scene mutation is unavailable on this thread or during capture.</exception>
    /// <exception cref="ObjectDisposedException">The sprite is disposed.</exception>
    public int Frame
    {
        get { ThrowIfDisposed(); return _frame; }
        set
        {
            EnsureMutable();
            if ((uint)value >= (uint)(_hframes * _vframes)) throw new ArgumentOutOfRangeException(nameof(value));
            if (_frame == value) return;
            _frame = value; InvalidateCanvas(); NotifyItemRectChanged();
            if (!IsDisposed) FrameChanged?.Invoke();
        }
    }

    /// <summary>Gets or sets the selected column and row as an alias for Frame.</summary>
    /// <value>Zero by default.</value>
    /// <remarks>Shares Frame's redraw and ItemRectChanged/FrameChanged delivery.</remarks>
    /// <exception cref="Exception">An ItemRectChanged or FrameChanged subscriber throws after the change is committed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Either coordinate is outside the current grid.</exception>
    /// <exception cref="InvalidOperationException">Scene mutation is unavailable on this thread or during capture.</exception>
    /// <exception cref="ObjectDisposedException">The sprite is disposed.</exception>
    public Vector2i FrameCoords
    {
        get { ThrowIfDisposed(); return new(_frame % _hframes, _frame / _hframes); }
        set
        {
            EnsureMutable();
            if ((uint)value.X >= (uint)_hframes || (uint)value.Y >= (uint)_vframes) throw new ArgumentOutOfRangeException(nameof(value));
            Frame = value.Y * _hframes + value.X;
        }
    }

    /// <summary>Occurs after an explicit Frame or FrameCoords assignment changes the frame index.</summary>
    /// <remarks>Delivery follows ItemRectChanged. A throwing subscriber stops later subscribers; state and redraw remain committed.</remarks>
    public event Action? FrameChanged;

    /// <summary>Occurs after the texture reference changes, including assignment to null.</summary>
    /// <remarks>Delivery precedes ItemRectChanged. A throwing subscriber stops later subscribers and the rectangle event.
    /// Resource content changes do not emit either event.</remarks>
    public event Action? TextureChanged;

    /// <summary>Returns the frame's local bounds with integer-truncated dimensions.</summary>
    /// <returns>A one-by-one rectangle at zero without a texture. Otherwise the frame size and centered/offset origin;
    /// both zero dimensions become one by one after calculating the origin. Flipping does not alter these bounds.</returns>
    /// <remarks>Texture/region size is truncated before division and the result is truncated again. Drawing uses
    /// fractional frame sizes, so these inspection bounds may omit a fractional edge. Negative region sizes stay signed.
    /// An attached viewport with SnapTransformsToPixel rounds the local origin using floor(value + 0.5).</remarks>
    /// <exception cref="ObjectDisposedException">The sprite or its texture is disposed.</exception>
    /// <exception cref="InvalidOperationException">Texture dimensions or derived geometry are invalid.</exception>
    public Rect GetRect()
    {
        ThrowIfDisposed();
        if (_texture is null) return new(0, 0, 1, 1);
        var size = BaseRegion(_texture).Size;
        size = new(System.MathF.Truncate(System.MathF.Truncate(size.X) / _hframes), System.MathF.Truncate(System.MathF.Truncate(size.Y) / _vframes));
        var position = DrawingOffset(size);
        if (!position.IsFinite()) throw new InvalidOperationException("Sprite geometry overflowed finite coordinates.");
        return new(position, size == Vector2.Zero ? Vector2.One : size);
    }

    /// <summary>Tests source alpha at a finite point in the sprite's local drawing coordinates.</summary>
    /// <param name="position">The local point, before Entity transforms.</param>
    /// <returns>False without a nonempty texture or outside the drawn frame; otherwise the texture's opacity result.</returns>
    /// <remarks>Uses fractional drawing bounds, frame/region offsets and flipping. Explicit or inherited canvas repeat
    /// affects addressing while attached; detached queries retain the last tree cache (initially disabled).
    /// A viewport default sampler does not change this local source-alpha query.
    /// Transform snapping rounds the attached local drawing origin; vertex snapping does not affect this query.
    /// Modulation, filtering, materials and visibility do not change source opacity.</remarks>
    /// <exception cref="ArgumentException">The point is not finite.</exception>
    /// <exception cref="ObjectDisposedException">The sprite or its texture is disposed.</exception>
    /// <exception cref="InvalidOperationException">Texture dimensions or derived geometry are invalid.</exception>
    public bool IsPixelOpaque(Vector2 position)
    {
        ThrowIfDisposed();
        if (!position.IsFinite()) throw new ArgumentException("A sprite point must be finite.", nameof(position));
        if (_texture is not { } texture) return false;
        if (texture.IsDisposed) throw new ObjectDisposedException(nameof(Texture));
        var width = texture.GetWidth(); var height = texture.GetHeight();
        if (width < 0 || height < 0) throw new InvalidOperationException("Sprite texture dimensions must be nonnegative.");
        if (width == 0 || height == 0) return false;
        GetDrawRects(texture, out var source, out var destination);
        var bounds = new Rect(destination.Position, destination.Size.Abs());
        if (!bounds.HasPoint(position)) return false;
        var point = (position - bounds.Position) / bounds.Size;
        if ((destination.Size.X < 0) != (source.Size.X < 0)) point.X = 1 - point.X;
        if ((destination.Size.Y < 0) != (source.Size.Y < 0)) point.Y = 1 - point.Y;
        point = source.Position + point * source.Size.Abs();
        if (!point.IsFinite()) throw new InvalidOperationException("Sprite opacity coordinates overflowed finite values.");
        var repeat = TextureRepeatInTree;
        if (repeat is TextureRepeatEnum.Enabled or TextureRepeatEnum.Mirror)
            return texture.IsPixelOpaque(RepeatCoordinate(point.X, width, repeat == TextureRepeatEnum.Mirror),
                RepeatCoordinate(point.Y, height, repeat == TextureRepeatEnum.Mirror));
        return texture.IsPixelOpaque((int)Math.Clamp(point.X, 0, width - 1), (int)Math.Clamp(point.Y, 0, height - 1));
    }

    /// <inheritdoc />
    /// <remarks>Records the current frame through the texture's virtual drawing method. Derived overrides should
    /// call base.OnDraw to retain the sprite image before adding their own drawing commands.</remarks>
    protected override void OnDraw()
    {
        if (_texture is not { } texture) return;
        GetDrawRects(texture, out var source, out var destination);
        DrawTextureRectRegion(texture, destination, source, clipUV: _regionEnabled && _regionFilterClipEnabled);
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(SpriteProperties);

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(Sprite) ? CreateSpriteNode : base.CreateSceneInstanceFactory();

    private static Entity CreateSpriteNode() => new Sprite();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (_texture is not null) _texture.Changed -= TextureContentChanged;
            _texture = null;
            FrameChanged = TextureChanged = null;
        }
        base.Dispose(disposing);
    }

    private void TextureContentChanged(Resource _) => InvalidateCanvas();

    private void SetGrid(int columns, int rows)
    {
        EnsureMutable();
        if (columns < 1 || rows < 1 || (long)columns * rows > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(columns), "The positive frame grid must fit Int32.MaxValue frames.");
        if (columns == _hframes && rows == _vframes) return;
        var column = _frame % _hframes;
        var row = _frame / _hframes;
        _frame = column < columns && row < rows ? row * columns + column : 0;
        _hframes = columns; _vframes = rows;
        InvalidateCanvas(); NotifyItemRectChanged();
        if (!IsDisposed) NotifyPropertyListChanged();
    }

    private static int RepeatCoordinate(float point, int size, bool mirror)
    {
        var tile = Math.Truncate((double)point / size);
        var coordinate = point % size;
        if (mirror && tile % 2 == 1) coordinate = size - coordinate - 1;
        return (int)coordinate;
    }

    private Rect BaseRegion(Texture texture)
    {
        if (texture.IsDisposed) throw new ObjectDisposedException(nameof(Texture));
        var region = _regionEnabled ? _regionRect : new Rect(Vector2.Zero, texture.GetSize());
        if (!region.IsFinite() || !_regionEnabled && (region.Size.X < 0 || region.Size.Y < 0))
            throw new InvalidOperationException("Sprite texture dimensions must be finite and nonnegative.");
        return region;
    }

    private Vector2 DrawingOffset(Vector2 size)
    {
        var offset = _centered ? _offset - size / 2 : _offset;
        return IsInsideTree && GetViewport()?.SnapTransformsToPixel == true ? CanvasGeometry.Snap(offset) : offset;
    }

    private void GetDrawRects(Texture texture, out Rect source, out Rect destination)
    {
        var area = BaseRegion(texture);
        var size = area.Size / new Vector2(_hframes, _vframes);
        source = new(area.Position + new Vector2(_frame % _hframes, _frame / _hframes) * size, size);
        var position = DrawingOffset(size);
        destination = new(position, new Vector2(_flipH ? -size.X : size.X, _flipV ? -size.Y : size.Y));
        if (!source.IsFinite() || !destination.IsFinite()) throw new InvalidOperationException("Sprite geometry overflowed finite coordinates.");
    }
}
