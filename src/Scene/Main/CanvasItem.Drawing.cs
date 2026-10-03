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
    [ThreadStatic] private static CanvasItem? _currentDrawingItem;
    internal static CanvasItem? CurrentDrawingItem => _currentDrawingItem;
    private Color _modulate = Colors.White;
    private Color _selfModulate = Colors.White;
    private Material? _material;
    private bool _useParentMaterial;

    /// <summary>Gets or sets the color multiplier inherited by this node's descendants.</summary>
    /// <value>Opaque white by default.</value>
    /// <exception cref="ArgumentException">The color is not finite.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off the owner thread, or mutation occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public Color Modulate
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _modulate; }
        set { EnsureMutable(); ValidateCanvasColor(value); _modulate = value; }
    }

    /// <summary>Gets or sets the color multiplier applied only to this node's drawing.</summary>
    /// <value>Opaque white by default.</value>
    /// <exception cref="ArgumentException">The color is not finite.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off the owner thread, or mutation occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public Color SelfModulate
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _selfModulate; }
        set { EnsureMutable(); ValidateCanvasColor(value); _selfModulate = value; }
    }

    /// <summary>Gets or sets the borrowed material for this node's canvas commands.</summary>
    /// <value>Null uses ordinary source-alpha color drawing.</value>
    /// <remarks>Disposing the node does not dispose this shared resource. ShaderMaterial with an assigned shader requires GPU rendering.
    /// CanvasItemMaterial selects fixed blending; unsupported software modes fail before drawing. An externally
    /// disposed borrowed material remains readable but cannot be used for rendering until replaced. Every assignment,
    /// including an equal one, raises PropertyListChanged after storing the borrowed reference.</remarks>
    /// <exception cref="InvalidOperationException">Attached access is off the owner thread, or mutation occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed, or the assigned value is already disposed.</exception>
    /// <exception cref="Exception">A property-list subscriber throws after the material changes.</exception>
    public Material? Material
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _material; }
        set
        {
            EnsureMutable();
            if (value is { IsDisposed: true }) throw new ObjectDisposedException(nameof(value));
            _material = value;
            NotifyPropertyListChanged();
        }
    }

    /// <summary>Gets or sets whether this node uses its parent's effective material.</summary>
    /// <value>False by default. A root using its parent material uses ordinary color drawing.</value>
    /// <exception cref="InvalidOperationException">Attached access is off the owner thread, or mutation occurs during scene capture.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public bool UseParentMaterial
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _useParentMaterial; }
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
    /// <param name="antialiased">Whether to add compensated local feather geometry; ignored for negative-width outlines.</param>
    /// <remarks>Negative sizes are normalized. Outlines at least as wide as either dimension become an expanded fill.
    /// Other outlines use joined closed strips; ordinary zero-width outlines draw nothing. Filled antialiasing shrinks
    /// the core by 0.3125 local units and adds 1.25-unit feathers, scaled down for subpixel cores. Drawing obeys this node's transform, visibility, Z order and modulation.</remarks>
    /// <exception cref="ArgumentException">Geometry, color or width is not finite.</exception>
    /// <exception cref="InvalidOperationException">Called outside this item's recording scope or off its owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public void DrawRect(Rect2 rect, Color color, bool filled = true, float width = -1f, bool antialiased = false)
    {
        EnsureDrawing();
        if (!rect.IsFinite() || !float.IsFinite(width)) throw new ArgumentException("Drawing geometry must be finite.");
        ValidateCanvasColor(color);
        var stroke = NextStroke(); stroke.SetRect(rect, color, filled, width, antialiased); CommitStroke(stroke);
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
        (_canvasCommands ??= []).Add(new CanvasCommand(true, from, to, color, width, antialiased, Transform.Identity));
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
        ValidateTextureDraw(texture, new Rect2(position, Vector2.Zero), modulate ?? Colors.White);
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
    public void DrawTextureRect(Texture texture, Rect2 rect, bool tile, Color? modulate = null, bool transpose = false)
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
    public void DrawTextureRectRegion(Texture texture, Rect2 rect, Rect2 sourceRect, Color? modulate = null, bool transpose = false, bool clipUV = true)
    {
        ValidateTextureDraw(texture, rect, modulate ?? Colors.White);
        if (!sourceRect.IsFinite()) throw new ArgumentException("The texture region must be finite.", nameof(sourceRect));
        texture.DrawRectRegion(this, rect, sourceRect, modulate, transpose, clipUV);
    }

    internal void ValidateTextureDraw(Texture texture, Rect2 rect, Color color)
    {
        EnsureDrawing(); ArgumentNullException.ThrowIfNull(texture);
        if (texture.IsDisposed) throw new ObjectDisposedException(nameof(texture));
        if (!rect.IsFinite()) throw new ArgumentException("Drawing geometry must be finite.", nameof(rect));
        ValidateCanvasColor(color);
    }

    internal void RecordTexture(Texture texture, Rect2 rect, Rect2? source, Color color, bool tile, bool transpose, bool clipUV)
    {
        ValidateTextureDraw(texture, rect, color);
        if (source is { } region && !region.IsFinite()) throw new ArgumentException("The texture region must be finite.", nameof(source));
        var size = texture.GetSize();
        if (!size.IsFinite() || size.X < 0 || size.Y < 0) throw new InvalidOperationException("Texture dimensions must be finite and nonnegative.");
        if (size.X == 0 || size.Y == 0) return;
        var src = source ?? new Rect2(Vector2.Zero, tile ? rect.Size.Abs() : size);
        src = new Rect2(src.Position / size, src.Size / size);
        if (!src.IsFinite()) throw new ArgumentException("Texture coordinates overflowed.", nameof(source));
        (_canvasCommands ??= []).Add(new CanvasCommand(false, rect.Position, rect.Size, color, 0, false,
            Transform.Identity, texture, src, transpose, clipUV, tile));
    }

    internal void RecordNinePatch(Texture texture, Rect2 destination, Rect2 source, CanvasNinePatch patch, Color? modulate = null)
    {
        ValidateTextureDraw(texture, destination, modulate ?? Colors.White);
        if (!source.IsFinite()) throw new ArgumentException("Nine-patch source geometry must be finite.", nameof(source));
        (_canvasCommands ??= []).Add(new(false, destination.Position, destination.Size, modulate ?? Colors.White, 0, false,
            Transform.Identity, texture, source, NinePatch: patch));
    }

    /// <summary>Records a reusable style decoration during this item's canvas recording.</summary>
    /// <param name="styleBox">The live style whose draw hook supplies the retained commands.</param>
    /// <param name="rect">The finite local destination rectangle.</param>
    /// <remarks>Style changes require QueueRedraw from the owning control. Commands capture geometry while
    /// borrowing any texture resources; the canvas does not own the style or textures.</remarks>
    /// <exception cref="ArgumentNullException">The style is null.</exception>
    /// <exception cref="ArgumentException">The rectangle is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The item is not recording on its owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The item, style or drawing resource is disposed.</exception>
    public void DrawStyleBox(StyleBox styleBox, Rect2 rect)
    {
        ValidateStyleDraw(rect); ArgumentNullException.ThrowIfNull(styleBox); styleBox.Draw(this, rect);
    }

    internal void ValidateStyleDraw(Rect2 rect)
    {
        EnsureDrawing(); if (!rect.IsFinite()) throw new ArgumentException("Style rectangles must be finite.", nameof(rect));
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
    /// <remarks>Recorded as a state command; ignored during frames where its animation interval is hidden.</remarks>
    /// <exception cref="ArgumentException">The transform is not finite.</exception>
    /// <exception cref="InvalidOperationException">Called outside this item's recording scope or off its owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public void DrawSetTransformMatrix(Transform transform)
    {
        EnsureDrawing();
        if (!transform.IsFinite()) throw new ArgumentException("The drawing transform must be finite.", nameof(transform));
        (_canvasCommands ??= []).Add(new CanvasCommand(false, default, default, Colors.White, 0, false, transform, SetTransform: true));
    }

    /// <summary>Restricts subsequent drawing commands to a repeating time interval.</summary>
    /// <param name="animationLength">Finite period in seconds. Zero hides the interval; a negative period produces a negative phase.</param>
    /// <param name="sliceBegin">Finite inclusive phase boundary in seconds.</param>
    /// <param name="sliceEnd">Finite exclusive phase boundary in seconds.</param>
    /// <param name="offset">Finite phase origin in seconds, zero by default.</param>
    /// <remarks>The interval is tested every submitted frame without rerecording. It replaces the previous interval,
    /// including when that interval is hidden. Commands, including drawing transforms, are skipped until another interval
    /// permits them or DrawEndAnimation restores drawing. Intervals do not wrap their boundaries or affect other items.
    /// Render time uses scaled process steps, continues while the scene is paused, and wraps at the configured render time limit.</remarks>
    /// <exception cref="ArgumentException">An argument is not finite.</exception>
    /// <exception cref="InvalidOperationException">Called outside this item's recording scope or off its owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public void DrawAnimationSlice(double animationLength, double sliceBegin, double sliceEnd, double offset = 0d)
    {
        EnsureDrawing();
        if (!double.IsFinite(animationLength) || !double.IsFinite(sliceBegin) || !double.IsFinite(sliceEnd) || !double.IsFinite(offset))
            throw new ArgumentException("Animation slice values must be finite.");
        (_canvasCommands ??= []).Add(new CanvasCommand(false, default, default, Colors.White, 0, false, Transform.Identity,
            AnimationSlice: new(animationLength, sliceBegin, sliceEnd, offset)));
    }

    /// <summary>Restores unrestricted drawing for subsequent commands in this recording.</summary>
    /// <remarks>Does not reset the last executed drawing transform. Interval state resets before each item submission.</remarks>
    /// <exception cref="InvalidOperationException">Called outside this item's recording scope or off its owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public void DrawEndAnimation() => DrawAnimationSlice(1, 0, 2);

    /// <summary>Occurs during recording, after NotificationDraw and before OnDraw.</summary>
    /// <remarks>Handlers run synchronously on the scene owner thread and may issue drawing commands for this item.
    /// Deferred handlers run outside the recording scope and cannot draw. A handler failure aborts recording.</remarks>
    public event Action<CanvasItem>? Draw;

    /// <summary>Records this node's retained canvas commands before its first visible frame and after QueueRedraw.</summary>
    /// <remarks>Runs on the scene owner thread during rendering. Geometry, texture and drawing-transform calls
    /// are valid during NotificationDraw, synchronous Draw handlers and this callback. The command list is cleared before entry. Transform and interval state start fresh on each replay.</remarks>
    protected virtual void OnDraw() { }

    internal void PrepareCanvas()
    {
        EnsureMutable();
        if (_drawing) throw new InvalidOperationException("Canvas recording cannot be re-entered.");
        if (Interlocked.Exchange(ref _redrawPending, 0) == 0) return;
        _canvasCommands?.Clear();
        _polygonCount = 0; _strokeCount = 0; _meshCount = 0; _multiMeshCount = 0;
        _drawing = true;
        var previousDrawingItem = _currentDrawingItem; _currentDrawingItem = this;
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
        finally { _drawing = false; _currentDrawingItem = previousDrawingItem; if (_meshes is not null) for (var i = _meshCount; i < _meshes.Count; i++) _meshes[i].Clear(); if (_multiMeshes is not null) for (var i = _multiMeshCount; i < _multiMeshes.Count; i++) _multiMeshes[i].Clear(); }
    }

    internal virtual Rect2? CanvasClipRect => null;

    internal Material? CanvasMaterial => _useParentMaterial ? GetParentItem()?.CanvasMaterial : _material;
    internal Color InheritedModulate => GetParentItem() is not { } parent ? _modulate : parent.InheritedModulate * _modulate;

    internal void AppendCanvas(List<CanvasVertex> vertices, List<CanvasBatch> batches, Transform transform, double time = 0, Rect2i? clip = null, Vector2i? outputSize = null)
    {
        if (_canvasCommands is null) return;
        var color = InheritedModulate * _selfModulate;
        var viewport = GetViewport();
        var filter = TextureFilterInTree;
        if (filter == TextureFilter.ParentNode) filter = viewport?.TextureFilterInTree ?? TextureFilter.Linear;
        var inheritedRepeat = TextureRepeatInTree;
        if (inheritedRepeat == TextureRepeat.ParentNode) inheritedRepeat = viewport?.TextureRepeatInTree ?? TextureRepeat.Disabled;
        var anisotropy = filter >= TextureFilter.NearestWithMipmapsAnisotropic
            ? 1 << (int)(viewport?.AnisotropicFilteringLevel ?? Viewport.AnisotropicFiltering.Anisotropy4X) : 1;
        MaterialState? material = null;
        var blend = BlendMode.Mix;
        var capturedMaterial = false;
        var drawingTransform = Transform.Identity;
        var skipping = false;
        foreach (var command in _canvasCommands)
        {
            if (command.AnimationSlice is { } slice) { skipping = !slice.Includes(time); continue; }
            if (skipping || command.Texture is ServerTexture { IsDisposed: true }) continue;
            if (command.SetTransform) { drawingTransform = command.Transform; continue; }
            var replay = command;
            if (command.Texture is ServerTexture { IsProxy: true } proxy)
            {
                var source = RenderingTextureRegistry.ResolveProxySource(proxy.ProxyTarget);
                if (source is null) continue;
                if (source.CapturePixels() is null) source = RenderingTextureRegistry.PlaceholderTexture;
                replay = command with { Texture = source };
            }
            if (replay.MultiMesh is { } instances)
            {
                if (!capturedMaterial) { var current = CanvasMaterial; material = current?.GetCanvasState(); blend = current?.GetCanvasBlendMode() ?? BlendMode.Mix; capturedMaterial = true; }
                var fraction = IsPhysicsInterpolatedAndEnabled() ? (float)Engine.Instance.PhysicsInterpolationFraction : 1f;
                instances.Append(vertices, batches, replay.Texture, transform * drawingTransform, color, material, blend, filter, inheritedRepeat, anisotropy, clip, viewport?.SnapVerticesToPixel == true, fraction, outputSize);
                continue;
            }
            if (replay.Mesh is { } mesh)
            {
                if (!capturedMaterial) { var current = CanvasMaterial; material = current?.GetCanvasState(); blend = current?.GetCanvasBlendMode() ?? BlendMode.Mix; capturedMaterial = true; }
                mesh.Append(vertices, batches, replay.Texture, transform * drawingTransform, color, material, blend, filter, inheritedRepeat, anisotropy, clip, viewport?.SnapVerticesToPixel == true);
                continue;
            }
            var first = vertices.Count;
            CanvasGeometry.Append(vertices, replay, transform * drawingTransform, color, viewport?.SnapVerticesToPixel == true);
            var count = vertices.Count - first;
            if (count == 0) continue;
            if (!capturedMaterial)
            {
                var canvasMaterial = CanvasMaterial;
                material = canvasMaterial?.GetCanvasState();
                blend = canvasMaterial?.GetCanvasBlendMode() ?? BlendMode.Mix;
                capturedMaterial = true;
            }
            var repeat = command.Tile ? TextureRepeat.Enabled : inheritedRepeat;
            if (batches.Count != 0 && batches[^1] is var last && last.Material == material && last.Texture == replay.Texture &&
                last.Filter == filter && last.Repeat == repeat && last.MaxAnisotropy == anisotropy && last.Blend == blend && last.Clip == clip)
                batches[^1] = last with { Count = last.Count + count };
            else batches.Add(new(first, count, material, replay.Texture, filter, repeat, anisotropy, blend, clip));
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
