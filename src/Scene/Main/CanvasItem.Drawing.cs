namespace Electron2D;

public abstract partial class CanvasItem
{
    private static readonly PropertyDescriptor[] DrawingProperties =
    [
        new PropertyDescriptor<CanvasItem, Color>(nameof(Modulate), n => n.Modulate, (n, v) => n.Modulate = v, _ => Colors.White, stored: true),
        new PropertyDescriptor<CanvasItem, Color>(nameof(SelfModulate), n => n.SelfModulate, (n, v) => n.SelfModulate = v, _ => Colors.White, stored: true),
        new PropertyDescriptor<CanvasItem, Material?>(nameof(Material), n => n.Material, (n, v) => n.Material = v, _ => null, stored: true),
        new PropertyDescriptor<CanvasItem, bool>(nameof(UseParentMaterial), n => n.UseParentMaterial, (n, v) => n.UseParentMaterial = v, _ => false, stored: true),
    ];
    private List<CanvasCommand>? _canvasCommands;
    private int _redrawPending = 1;
    private bool _drawing;
    private Transform _drawTransform = Transform.Identity;
    private Color _modulate = Colors.White;
    private Color _selfModulate = Colors.White;
    private Material? _material;
    private bool _useParentMaterial;

    /// <summary>Gets or sets the color multiplier inherited by this node's descendants.</summary>
    /// <value>Opaque white by default.</value>
    /// <exception cref="ArgumentException">The color is not finite.</exception>
    /// <exception cref="InvalidOperationException">An attached node is mutated off its owner thread or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public Color Modulate
    {
        get { ThrowIfDisposed(); return _modulate; }
        set { EnsureMutable(); ValidateCanvasColor(value); _modulate = value; }
    }

    /// <summary>Gets or sets the color multiplier applied only to this node's drawing.</summary>
    /// <value>Opaque white by default.</value>
    /// <exception cref="ArgumentException">The color is not finite.</exception>
    /// <exception cref="InvalidOperationException">An attached node is mutated off its owner thread or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public Color SelfModulate
    {
        get { ThrowIfDisposed(); return _selfModulate; }
        set { EnsureMutable(); ValidateCanvasColor(value); _selfModulate = value; }
    }

    /// <summary>Gets or sets the borrowed material for this node's canvas commands.</summary>
    /// <value>Null uses ordinary source-alpha color drawing.</value>
    /// <remarks>Disposing the node does not dispose this shared resource. An assigned shader requires GPU rendering.</remarks>
    /// <exception cref="InvalidOperationException">An attached node is mutated off its owner thread or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The node or assigned material is disposed.</exception>
    public Material? Material
    {
        get { ThrowIfDisposed(); return _material; }
        set { EnsureMutable(); if (value is { IsDisposed: true }) throw new ObjectDisposedException(nameof(value)); _material = value; }
    }

    /// <summary>Gets or sets whether this node uses its parent's effective material.</summary>
    /// <value>False by default. A root using its parent material uses ordinary color drawing.</value>
    /// <exception cref="InvalidOperationException">An attached node is mutated off its owner thread or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public bool UseParentMaterial
    {
        get { ThrowIfDisposed(); return _useParentMaterial; }
        set { EnsureMutable(); _useParentMaterial = value; }
    }

    /// <summary>Requests regeneration of this node's retained drawing commands before a later visible frame.</summary>
    /// <remarks>Detached calls do nothing. Requests coalesce. Hidden nodes retain the request until visible. A request during
    /// the current recording coalesces with that recording; it does not schedule another redraw. Transforms, modulation and material changes need no redraw.</remarks>
    /// <exception cref="InvalidOperationException">An attached node is mutated off its owner thread or during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public void QueueRedraw() { EnsureMutable(); if (IsInsideTree && !_drawing) InvalidateCanvas(); }

    // Resource change notifications may arrive from a loading thread; scene work stays on the owner thread.
    internal void InvalidateCanvas() => Interlocked.Exchange(ref _redrawPending, 1);

    /// <summary>Records a filled rectangle or a centered rectangular outline during canvas recording.</summary>
    /// <param name="rect">A finite local rectangle; negative dimensions are normalized.</param>
    /// <param name="color">The finite drawing color.</param>
    /// <param name="filled">Whether to fill the rectangle; true by default.</param>
    /// <param name="width">Outline width in local units; a negative value uses one framebuffer pixel.</param>
    /// <param name="antialiased">Whether to feather the boundary over one framebuffer pixel.</param>
    /// <remarks>Zero-area rectangles and zero-width outlines draw nothing. Outline widths larger than the rectangle
    /// collapse its hole. Drawing obeys this node's transform, visibility, Z order and modulation.</remarks>
    /// <exception cref="ArgumentException">Geometry, color or width is not finite.</exception>
    /// <exception cref="InvalidOperationException">Called outside this item's recording scope or off its owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public void DrawRect(Rect rect, Color color, bool filled = true, float width = -1f, bool antialiased = false)
    {
        EnsureDrawing();
        if (!rect.IsFinite() || !float.IsFinite(width)) throw new ArgumentException("Drawing geometry must be finite.");
        ValidateCanvasColor(color);
        (_canvasCommands ??= []).Add(new CanvasCommand(false, rect.Position, rect.Size, color, filled, width, antialiased, _drawTransform));
    }

    /// <summary>Records a straight line during canvas recording.</summary>
    /// <param name="from">The finite starting point in local coordinates.</param>
    /// <param name="to">The finite ending point in local coordinates.</param>
    /// <param name="color">The finite drawing color.</param>
    /// <param name="width">Width in local units; a negative value uses one framebuffer pixel.</param>
    /// <param name="antialiased">Whether to add a local antialias feather with compensated core width.</param>
    /// <remarks>Lines use flat caps. Coincident endpoints or zero width draw nothing. Feather widths scale with local transforms;
    /// negative-width lines retain a one-pixel core and can still add local feathers, unlike thin polylines.</remarks>
    /// <exception cref="ArgumentException">Geometry, color or width is not finite.</exception>
    /// <exception cref="InvalidOperationException">Called outside this item's recording scope or off its owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public void DrawLine(Vector2 from, Vector2 to, Color color, float width = -1f, bool antialiased = false)
    {
        EnsureDrawing();
        if (!from.IsFinite() || !to.IsFinite() || !float.IsFinite(width)) throw new ArgumentException("Drawing geometry must be finite.");
        ValidateCanvasColor(color);
        (_canvasCommands ??= []).Add(new CanvasCommand(true, from, to, color, false, width, antialiased, _drawTransform));
    }

    /// <summary>Draws a borrowed texture at its logical size during this item's canvas recording.</summary>
    /// <param name="texture">The live texture; its virtual Draw implementation supplies the command.</param>
    /// <param name="position">The finite local top-left position.</param>
    /// <param name="modulate">The finite color multiplier, or null for white.</param>
    /// <exception cref="ArgumentNullException">The texture is null.</exception>
    /// <exception cref="ArgumentException">Position or modulation is not finite.</exception>
    /// <exception cref="InvalidOperationException">Called outside this item's recording scope or off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node or texture is disposed.</exception>
    public void DrawTexture(Texture texture, Vector2 position, Color? modulate = null)
    {
        ValidateTextureDraw(texture, new Rect(position, Vector2.Zero), modulate ?? Colors.White);
        texture.Draw(this, position, modulate);
    }

    /// <summary>Stretches or repeats a borrowed texture over a local rectangle during canvas recording.</summary>
    /// <param name="texture">The live texture; its virtual DrawRect implementation supplies the command.</param>
    /// <param name="rect">The finite destination. Negative dimensions flip without moving its origin.</param>
    /// <param name="tile">Whether to repeat at the texture's logical pixel size.</param>
    /// <param name="modulate">The finite color multiplier, or null for white.</param>
    /// <param name="transpose">Whether to exchange texture axes and destination dimensions.</param>
    /// <exception cref="ArgumentNullException">The texture is null.</exception>
    /// <exception cref="ArgumentException">Geometry or modulation is not finite.</exception>
    /// <exception cref="InvalidOperationException">Called outside this item's recording scope or off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node or texture is disposed.</exception>
    public void DrawTextureRect(Texture texture, Rect rect, bool tile, Color? modulate = null, bool transpose = false)
    {
        ValidateTextureDraw(texture, rect, modulate ?? Colors.White);
        texture.DrawRect(this, rect, tile, modulate, transpose);
    }

    /// <summary>Stretches a source region of a borrowed texture over a local rectangle during canvas recording.</summary>
    /// <param name="texture">The live texture; its virtual DrawRectRegion implementation supplies the command.</param>
    /// <param name="rect">The finite destination. Negative dimensions flip without moving its origin.</param>
    /// <param name="sourceRect">The finite region in logical texture pixels; negative dimensions toggle flipping.</param>
    /// <param name="modulate">The finite color multiplier, or null for white.</param>
    /// <param name="transpose">Whether to exchange texture axes and destination dimensions.</param>
    /// <param name="clipUV">Whether to constrain sampling to texel centers inside the source region.</param>
    /// <exception cref="ArgumentNullException">The texture is null.</exception>
    /// <exception cref="ArgumentException">Geometry or modulation is not finite.</exception>
    /// <exception cref="InvalidOperationException">Called outside this item's recording scope or off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node or texture is disposed.</exception>
    public void DrawTextureRectRegion(Texture texture, Rect rect, Rect sourceRect, Color? modulate = null, bool transpose = false, bool clipUV = true)
    {
        ValidateTextureDraw(texture, rect, modulate ?? Colors.White);
        if (!sourceRect.IsFinite()) throw new ArgumentException("The texture region must be finite.", nameof(sourceRect));
        texture.DrawRectRegion(this, rect, sourceRect, modulate, transpose, clipUV);
    }

    internal void ValidateTextureDraw(Texture texture, Rect rect, Color color)
    {
        EnsureDrawing(); ArgumentNullException.ThrowIfNull(texture);
        if (texture.IsDisposed) throw new ObjectDisposedException(nameof(texture));
        if (!rect.IsFinite()) throw new ArgumentException("Drawing geometry must be finite.", nameof(rect));
        ValidateCanvasColor(color);
    }

    internal void RecordTexture(Texture texture, Rect rect, Rect? source, Color color, bool tile, bool transpose, bool clipUV)
    {
        ValidateTextureDraw(texture, rect, color);
        if (source is { } region && !region.IsFinite()) throw new ArgumentException("The texture region must be finite.", nameof(source));
        var size = texture.GetSize();
        if (!size.IsFinite() || size.X < 0 || size.Y < 0) throw new InvalidOperationException("Texture dimensions must be finite and nonnegative.");
        if (size.X == 0 || size.Y == 0) return;
        var src = source ?? new Rect(Vector2.Zero, tile ? rect.Size.Abs() : size);
        src = new Rect(src.Position / size, src.Size / size);
        if (!src.IsFinite()) throw new ArgumentException("Texture coordinates overflowed.", nameof(source));
        (_canvasCommands ??= []).Add(new CanvasCommand(false, rect.Position, rect.Size, color, true, 0, false,
            _drawTransform, texture, src, transpose, clipUV, tile));
    }

    /// <summary>Sets an additional transform for subsequent commands in this canvas recording.</summary>
    /// <param name="position">Translation in local units.</param>
    /// <param name="rotation">Rotation in radians, zero by default.</param>
    /// <param name="scale">Scale, or null for one on both axes.</param>
    /// <exception cref="ArgumentException">The transform is not finite.</exception>
    /// <exception cref="InvalidOperationException">Called outside this item's recording scope or off its owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public void DrawSetTransform(Vector2 position, float rotation = 0f, Vector2? scale = null) =>
        DrawSetTransformMatrix(new Transform(rotation, scale ?? Vector2.One, 0f, position));

    /// <summary>Sets the full additional transform for subsequent commands in this canvas recording.</summary>
    /// <param name="transform">The finite local drawing transform.</param>
    /// <exception cref="ArgumentException">The transform is not finite.</exception>
    /// <exception cref="InvalidOperationException">Called outside this item's recording scope or off its owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public void DrawSetTransformMatrix(Transform transform)
    {
        EnsureDrawing();
        if (!transform.IsFinite()) throw new ArgumentException("The drawing transform must be finite.", nameof(transform));
        _drawTransform = transform;
    }

    /// <summary>Occurs during recording, after NotificationDraw and before OnDraw.</summary>
    /// <remarks>Handlers run synchronously on the scene owner thread and may issue drawing commands for this item.
    /// Deferred handlers run outside the recording scope and cannot draw. A handler failure aborts recording.</remarks>
    public event Action<CanvasItem>? Draw;

    /// <summary>Records this node's retained canvas commands before its first visible frame and after QueueRedraw.</summary>
    /// <remarks>Runs on the scene owner thread during rendering. Geometry, texture and drawing-transform calls
    /// are valid during NotificationDraw, synchronous Draw handlers and this callback. The command list is cleared and the drawing transform reset to identity before entry.</remarks>
    protected virtual void OnDraw() { }

    internal void PrepareCanvas()
    {
        EnsureMutable();
        if (_drawing) throw new InvalidOperationException("Canvas recording cannot be re-entered.");
        if (Interlocked.Exchange(ref _redrawPending, 0) == 0) return;
        _canvasCommands?.Clear();
        _polygonCount = 0; _strokeCount = 0;
        _drawTransform = Transform.Identity;
        _drawing = true;
        try
        {
            DispatchNotification(NotificationDraw);
            if (IsDisposed) return;
            Draw?.Invoke(this);
            if (!IsDisposed) OnDraw();
        }
        catch
        {
            _canvasCommands?.Clear();
            InvalidateCanvas();
            throw;
        }
        finally { _drawing = false; }
    }

    internal Material? CanvasMaterial => _useParentMaterial ? GetParentItem()?.CanvasMaterial : _material;
    private Color InheritedModulate => GetParentItem() is not { } parent ? _modulate : parent.InheritedModulate * _modulate;

    internal void AppendCanvas(List<CanvasVertex> vertices, List<CanvasBatch> batches, Transform transform)
    {
        if (_canvasCommands is null) return;
        var color = InheritedModulate * _selfModulate;
        var viewport = GetViewport();
        var filter = TextureFilterInTree;
        if (filter == TextureFilterEnum.ParentNode) filter = viewport?.TextureFilterInTree ?? TextureFilterEnum.Linear;
        var inheritedRepeat = TextureRepeatInTree;
        if (inheritedRepeat == TextureRepeatEnum.ParentNode) inheritedRepeat = viewport?.TextureRepeatInTree ?? TextureRepeatEnum.Disabled;
        var anisotropy = filter >= TextureFilterEnum.NearestWithMipmapsAnisotropic
            ? 1 << (int)(viewport?.AnisotropicFilteringLevel ?? Viewport.AnisotropicFiltering.Anisotropy4X) : 1;
        MaterialState? material = null;
        var capturedMaterial = false;
        foreach (var command in _canvasCommands)
        {
            var first = vertices.Count;
            CanvasGeometry.Append(vertices, command, transform * command.Transform, color, viewport?.SnapVerticesToPixel == true);
            var count = vertices.Count - first;
            if (count == 0) continue;
            if (!capturedMaterial) { material = CanvasMaterial?.GetCanvasState(); capturedMaterial = true; }
            var repeat = command.Tile ? TextureRepeatEnum.Enabled : inheritedRepeat;
            if (batches.Count != 0 && batches[^1] is var last && last.Material == material && last.Texture == command.Texture &&
                last.Filter == filter && last.Repeat == repeat && last.MaxAnisotropy == anisotropy)
                batches[^1] = last with { Count = last.Count + count };
            else batches.Add(new(first, count, material, command.Texture, filter, repeat, anisotropy));
        }
    }

    private void EnsureDrawing()
    {
        EnsureMutable();
        if (!_drawing) throw new InvalidOperationException("Drawing commands require this item's active recording scope.");
    }

    private static void ValidateCanvasColor(Color color)
    {
        if (!color.IsFinite()) throw new ArgumentException("Drawing colors must be finite.", nameof(color));
    }
}
