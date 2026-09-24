namespace Electron2D;

/// <summary>A rectangular view of a borrowed texture, with optional drawing margins.</summary>
/// <remarks>Views can be nested without copying GPU pixels. Drawing uses the region; material bindings use the
/// full underlying texture. Canvas polygons remap the immediate region; short primitives use full-image UVs. The atlas is never owned or disposed by this resource. Coordinate rectangles must
/// be finite; integer image queries also require representable pixel coordinates. Atlas graph access is serialized;
/// custom source callbacks must not wait for another thread to access an atlas during a query or draw.</remarks>
public sealed class AtlasTexture : Texture
{
    // ponytail: one graph lock serializes atlas traversal/mutation; use immutable graph snapshots if contention matters.
    private static readonly object GraphGate = new();
    private Texture? _atlas;
    private Rect2 _region;
    private Rect2 _roundedRegion;
    private Rect2 _margin;
    private bool _filterClip;

    /// <summary>Creates an empty view with a logical size of one pixel on each axis.</summary>
    public AtlasTexture() { }

    /// <summary>Gets or sets the borrowed source texture.</summary>
    /// <value>Null by default. An empty source draws nothing.</value>
    /// <remarks>Only nested atlas views forward Changed. Pixel updates on other textures remain visible through
    /// retained commands, but do not automatically request a new recording for changed source dimensions.</remarks>
    /// <exception cref="ArgumentException">The assignment would create an atlas cycle.</exception>
    /// <exception cref="ObjectDisposedException">This view or a supplied source is disposed.</exception>
    public Texture? Atlas
    {
        get { lock (GraphGate) { ThrowIfDisposed(); return _atlas; } }
        set
        {
            lock (GraphGate)
            {
                ThrowIfDisposed();
                for (var next = value; next is not null; next = (next as AtlasTexture)?._atlas)
                {
                    if (ReferenceEquals(next, this)) throw new ArgumentException("Atlas textures cannot form cycles.", nameof(value));
                    if (next.IsDisposed) throw new ObjectDisposedException(nameof(value));
                }
                if (ReferenceEquals(value, _atlas)) return;
                if (_atlas is AtlasTexture previous) previous.Changed -= ForwardChanged;
                _atlas = value;
                if (_atlas is AtlasTexture current) current.Changed += ForwardChanged;
            }
            EmitChanged();
        }
    }

    /// <summary>Gets or sets the source rectangle in pixels.</summary>
    /// <value>An empty rectangle by default. Each zero size axis uses the source's full logical dimension.</value>
    /// <remarks>The stored rectangle retains fractions; calculations floor only its size, not its position.</remarks>
    /// <exception cref="ArgumentException">The rectangle is not finite.</exception>
    /// <exception cref="ObjectDisposedException">This view is disposed.</exception>
    public Rect2 Region
    {
        get { lock (GraphGate) { ThrowIfDisposed(); return _region; } }
        set
        {
            lock (GraphGate)
            {
                ThrowIfDisposed(); ValidateRectangle(value);
                if (_region == value) return;
                _region = value; _roundedRegion = new Rect2(value.Position, value.Size.Floor());
            }
            EmitChanged();
        }
    }

    /// <summary>Gets or sets the offset and total extra size around the drawn region.</summary>
    /// <value>An empty rectangle by default; Position offsets the drawing and Size adds to nonzero region axes.</value>
    /// <remarks>Margins do not add pixels to GetImage. A zero region axis uses the source size without adding margin.</remarks>
    /// <exception cref="ArgumentException">The rectangle is not finite.</exception>
    /// <exception cref="ObjectDisposedException">This view is disposed.</exception>
    public Rect2 Margin
    {
        get { lock (GraphGate) { ThrowIfDisposed(); return _margin; } }
        set
        {
            lock (GraphGate)
            {
                ThrowIfDisposed(); ValidateRectangle(value);
                if (_margin == value) return;
                _margin = value;
            }
            EmitChanged();
        }
    }

    /// <summary>Gets or sets whether sampling is constrained to region texel centers.</summary>
    /// <value>False by default.</value>
    /// <remarks>Every assignment emits Changed. This overrides the caller's clipUV flag. For nested views,
    /// the innermost atlas's value reaches the underlying texture.</remarks>
    /// <exception cref="ObjectDisposedException">This view is disposed.</exception>
    public bool FilterClip
    {
        get { lock (GraphGate) { ThrowIfDisposed(); return _filterClip; } }
        set { lock (GraphGate) { ThrowIfDisposed(); _filterClip = value; } EmitChanged(); }
    }

    /// <inheritdoc />
    /// <remarks>A zero region width uses the source width, or one without a source. Otherwise returns the
    /// floored region width plus the margin width, truncated to an integer. Unrepresentable sums throw OverflowException.</remarks>
    public override int GetWidth() { lock (GraphGate) { ThrowIfDisposed(); return _roundedRegion.Size.X == 0 ? _atlas?.GetWidth() ?? 1 : checked((int)(_roundedRegion.Size.X + _margin.Size.X)); } }
    /// <inheritdoc />
    /// <remarks>Uses the same zero-axis fallback and checked integer conversion as GetWidth.</remarks>
    public override int GetHeight() { lock (GraphGate) { ThrowIfDisposed(); return _roundedRegion.Size.Y == 0 ? _atlas?.GetHeight() ?? 1 : checked((int)(_roundedRegion.Size.Y + _margin.Size.Y)); } }
    /// <inheritdoc />
    public override Vector2 GetSize() { lock (GraphGate) { ThrowIfDisposed(); return LogicalSize(_atlas, _roundedRegion, _margin); } }
    /// <inheritdoc />
    public override bool HasAlpha { get { lock (GraphGate) { ThrowIfDisposed(); return _atlas?.HasAlpha ?? false; } } }

    /// <inheritdoc />
    /// <remarks>An atlas view does not declare its own image format and returns Image.Format.Max.
    /// The returned image describes the crop format; Atlas.PixelFormat describes the immediate source.</remarks>
    public override Image.Format PixelFormat { get { ThrowIfDisposed(); return Image.Format.Max; } }
    /// <inheritdoc />
    /// <remarks>Always false: this view owns no mipmap chain. Rendering can still sample the source's mipmaps.</remarks>
    public override bool HasMipmaps { get { ThrowIfDisposed(); return false; } }
    /// <inheritdoc />
    /// <remarks>Always zero for the view, independently of the source's rendered mip levels.</remarks>
    public override int MipmapCount { get { ThrowIfDisposed(); return 0; } }

    /// <inheritdoc />
    /// <remarks>Returns only the effective source region, with integer truncation and image-bound clipping,
    /// without margin padding or mipmaps. Nested views crop the image returned by their immediate source.
    /// Coordinates outside the integer range throw OverflowException.</remarks>
    public override Image? GetImage()
    {
        lock (GraphGate)
        {
            ThrowIfDisposed();
            var atlas = _atlas;
            var region = EffectiveRegion(atlas, _roundedRegion);
            using var image = atlas?.GetImage();
            if (image is null) return null;
            return image.GetRegion(new Rect2i(checked((int)region.Position.X), checked((int)region.Position.Y),
                checked((int)region.Size.X), checked((int)region.Size.Y)));
        }
    }

    /// <inheritdoc />
    /// <remarks>Translates by region position minus margin position and truncates the resulting coordinate.
    /// Tests the full immediate source bounds, not the view's region: margins can therefore sample neighboring
    /// pixels. Returns false outside the source bounds and true when no source is assigned.</remarks>
    public override bool IsPixelOpaque(int x, int y)
    {
        lock (GraphGate)
        {
            ThrowIfDisposed();
            var atlas = _atlas;
            if (atlas is null) return true;
            var px = Math.Truncate((double)x + _roundedRegion.Position.X - _margin.Position.X);
            var py = Math.Truncate((double)y + _roundedRegion.Position.Y - _margin.Position.Y);
            return px >= 0 && px < atlas.GetWidth() && py >= 0 && py < atlas.GetHeight() && atlas.IsPixelOpaque((int)px, (int)py);
        }
    }

    /// <inheritdoc />
    /// <remarks>Draws the effective source region offset by Margin.Position; Margin.Size does not stretch it.</remarks>
    public override void Draw(CanvasItem canvasItem, Vector2 position, Color? modulate = null, bool transpose = false)
    {
        lock (GraphGate)
        {
            ValidateDraw(canvasItem, new Rect2(position, Vector2.Zero), modulate);
            var atlas = _atlas; var region = _roundedRegion; var margin = _margin; var clip = _filterClip;
            if (atlas is null) return;
            region = EffectiveRegion(atlas, region);
            var destination = new Rect2(position + margin.Position, region.Size);
            ValidateRectangle(destination);
            atlas.DrawRectRegion(canvasItem, destination, region, modulate, transpose, clip);
        }
    }

    /// <inheritdoc />
    /// <remarks>Scales the region and margins into the destination. Atlas views ignore tile and always stretch.</remarks>
    public override void DrawRect(CanvasItem canvasItem, Rect2 rect, bool tile, Color? modulate = null, bool transpose = false)
    {
        lock (GraphGate)
        {
            ValidateDraw(canvasItem, rect, modulate);
            if (_atlas is not null) DrawRegion(canvasItem, rect, null, modulate, transpose);
        }
    }

    /// <inheritdoc />
    /// <remarks>Clips geometry to the effective region after translating source coordinates by region position
    /// minus margin position. Both zero source dimensions select the rounded region size, then the atlas size
    /// if both remain zero. A remaining zero axis draws nothing. FilterClip overrides clipUV.</remarks>
    public override void DrawRectRegion(CanvasItem canvasItem, Rect2 rect, Rect2 sourceRect, Color? modulate = null, bool transpose = false, bool clipUV = true)
    {
        lock (GraphGate)
        {
            ValidateDraw(canvasItem, rect, modulate); ValidateRectangle(sourceRect);
            if (_atlas is not null) DrawRegion(canvasItem, rect, sourceRect, modulate, transpose);
        }
    }

    private void DrawRegion(CanvasItem canvasItem, Rect2 rect, Rect2? requestedSource, Color? modulate, bool transpose)
    {
        var atlas = _atlas!; var region = _roundedRegion; var margin = _margin; var clip = _filterClip;
        var source = requestedSource ?? new Rect2(Vector2.Zero, LogicalSize(atlas, region, margin));
        if (source.Size == Vector2.Zero) source.Size = region.Size;
        if (source.Size == Vector2.Zero) source.Size = atlas.GetSize();
        if (source.Size.X == 0 || source.Size.Y == 0) return;
        var scale = rect.Size / source.Size;
        source.Position += region.Position - margin.Position;
        ValidateRectangle(source);
        if (!scale.IsFinite()) throw new ArgumentException("Texture scaling overflowed.", nameof(rect));
        var clipped = EffectiveRegion(atlas, region).Intersection(source);
        if (clipped.Size == Vector2.Zero) return;
        var offset = clipped.Position - source.Position;
        if (scale.X < 0) offset.X += clipped.Size.X - source.Size.X;
        if (scale.Y < 0) offset.Y += clipped.Size.Y - source.Size.Y;
        var destination = new Rect2(rect.Position + offset * scale, clipped.Size * scale);
        ValidateRectangle(destination); ValidateRectangle(clipped);
        atlas.DrawRectRegion(canvasItem, destination, clipped, modulate, transpose, clip);
    }

    private static Rect2 EffectiveRegion(Texture? atlas, Rect2 region) => new(region.Position, new Vector2(
        region.Size.X == 0 ? atlas?.GetWidth() ?? 0 : region.Size.X,
        region.Size.Y == 0 ? atlas?.GetHeight() ?? 0 : region.Size.Y));

    private static Vector2 LogicalSize(Texture? atlas, Rect2 region, Rect2 margin) => new(
        region.Size.X == 0 ? atlas?.GetWidth() ?? 1 : checked((int)(region.Size.X + margin.Size.X)),
        region.Size.Y == 0 ? atlas?.GetHeight() ?? 1 : checked((int)(region.Size.Y + margin.Size.Y)));

    private void ValidateDraw(CanvasItem canvasItem, Rect2 rect, Color? modulate)
    {
        ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(canvasItem);
        canvasItem.ValidateTextureDraw(this, rect, modulate ?? Colors.White);
    }

    private static void ValidateRectangle(Rect2 rect)
    {
        if (!rect.IsFinite()) throw new ArgumentException("Texture rectangles must be finite.", nameof(rect));
    }

    private void ForwardChanged(Resource source)
    {
        lock (GraphGate) { if (IsDisposed || !ReferenceEquals(source, _atlas)) return; }
        EmitChanged();
    }

    internal Texture? RenderingTexture
    {
        get
        {
            lock (GraphGate)
            {
                Texture? source = this;
                while (source is AtlasTexture atlas) { atlas.ThrowIfDisposed(); source = atlas._atlas; }
                return source;
            }
        }
    }

    internal Texture? ResolvePolygonTexture(bool remap, out Rect2? uvMapping)
    {
        lock (GraphGate)
        {
            ThrowIfDisposed();
            var atlas = _atlas; var region = _region; var source = RenderingTexture;
            uvMapping = null;
            if (remap && atlas is not null)
            {
                var size = atlas.GetSize();
                var mapping = new Rect2(region.Position / size, region.Size / size);
                if (!mapping.IsFinite()) throw new ArgumentException("Atlas texture coordinates overflowed.");
                uvMapping = mapping;
            }
            return source;
        }
    }

    internal override TexturePixels? CapturePixels() => RenderingTexture?.CapturePixels();

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AtlasTexture();

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        Texture? atlas; Rect2 region, margin; bool filterClip;
        lock (GraphGate) { ThrowIfDisposed(); atlas = _atlas; region = _region; margin = _margin; filterClip = _filterClip; }
        var copy = (AtlasTexture)target;
        copy.Atlas = deep ? (Texture?)duplicateSubresource(atlas) : atlas;
        copy.Region = region; copy.Margin = margin; copy.FilterClip = filterClip;
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
    [
        new PropertyDescriptor<AtlasTexture, Texture?>(nameof(Atlas), t => t.Atlas, (t, v) => t.Atlas = v, _ => null, stored: true),
        new PropertyDescriptor<AtlasTexture, Rect2>(nameof(Region), t => t.Region, (t, v) => t.Region = v, _ => default, stored: true),
        new PropertyDescriptor<AtlasTexture, Rect2>(nameof(Margin), t => t.Margin, (t, v) => t.Margin = v, _ => default, stored: true),
        new PropertyDescriptor<AtlasTexture, bool>(nameof(FilterClip), t => t.FilterClip, (t, v) => t.FilterClip = v, _ => false, stored: true),
    ]);

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) lock (GraphGate)
            {
                if (_atlas is AtlasTexture atlas) atlas.Changed -= ForwardChanged;
                _atlas = null;
            }
        base.Dispose(disposing);
    }
}
