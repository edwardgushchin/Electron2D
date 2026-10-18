namespace Electron2D;

/// <summary>Draws a borrowed mesh wherever a texture supplies canvas geometry.</summary>
/// <remarks>This drawing-only view has no raster image or alpha metadata. Its logical RID retains virtual drawing;
/// it does not supply sampled image storage. Region clipping and tiling draw the complete mesh once.
/// Changes notify consumers, while source disposal clears the borrowed edge without disposing other resources.</remarks>
public sealed class MeshTexture : Texture
{
    private Mesh? _mesh;
    private Texture? _baseTexture;
    private Vector2 _imageSize;

    /// <summary>Creates an empty view with zero logical size.</summary>
    public MeshTexture() { }

    /// <summary>Gets or sets the borrowed mesh supplying two-dimensional geometry, UVs and surface materials.</summary>
    /// <value>Null initially. Equal assignments are silent; missing geometry draws nothing.</value>
    /// <exception cref="ObjectDisposedException">This view or the supplied mesh is disposed.</exception>
    public Mesh? Mesh
    {
        get { lock (TextureGraphGate) { ThrowIfDisposed(); return _mesh; } }
        set
        {
            lock (TextureGraphGate)
            {
                ThrowIfDisposed(); if (value is { IsDisposed: true }) throw new ObjectDisposedException(nameof(value));
                if (ReferenceEquals(_mesh, value)) return;
                if (_mesh is not null) { _mesh.Changed -= SourceChanged; _mesh.Disposed -= SourceDisposed; }
                _mesh = value;
                if (value is not null) { value.Changed += SourceChanged; value.Disposed += SourceDisposed; }
            }
            EmitChanged();
        }
    }

    /// <summary>Gets or sets the borrowed image sampled using the mesh's UVs.</summary>
    /// <value>Null initially. Equal assignments are silent; a missing base texture draws nothing.</value>
    /// <remarks>Atlas views use their full backing texture, as for ordinary mesh drawing. Texture graphs cannot form cycles.</remarks>
    /// <exception cref="ArgumentException">The assignment creates a texture graph cycle or exceeds its depth bound.</exception>
    /// <exception cref="ObjectDisposedException">This view or the supplied texture is disposed.</exception>
    public Texture? BaseTexture
    {
        get { lock (TextureGraphGate) { ThrowIfDisposed(); return _baseTexture; } }
        set
        {
            lock (TextureGraphGate)
            {
                ThrowIfDisposed(); if (value is { IsDisposed: true }) throw new ObjectDisposedException(nameof(value));
                if (value?.ReferencesTexture(this, 0) == true) throw new ArgumentException("Texture views cannot form cycles.", nameof(value));
                if (ReferenceEquals(_baseTexture, value)) return;
                if (_baseTexture is not null) { _baseTexture.Changed -= SourceChanged; _baseTexture.Disposed -= SourceDisposed; }
                _baseTexture = value;
                if (value is not null) { value.Changed += SourceChanged; value.Disposed += SourceDisposed; }
            }
            EmitChanged();
        }
    }

    /// <summary>Gets or sets the logical floating-point image size used to scale rectangle drawing.</summary>
    /// <value>Zero initially. Signed and fractional components are retained; integer size queries truncate toward zero.</value>
    /// <remarks>Point drawing keeps the mesh's authored size. A zero logical axis skips rectangle drawing.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">A component is nonfinite or outside the signed 32-bit pixel range.</exception>
    /// <exception cref="ObjectDisposedException">This view is disposed.</exception>
    public Vector2 ImageSize
    {
        get { lock (TextureGraphGate) { ThrowIfDisposed(); return _imageSize; } }
        set
        {
            lock (TextureGraphGate) { ThrowIfDisposed(); PlaceholderTexture.ToPixelSize(value); if (_imageSize == value) return; _imageSize = value; }
            EmitChanged();
        }
    }

    /// <inheritdoc />
    public override int GetWidth() { lock (TextureGraphGate) { ThrowIfDisposed(); return (int)_imageSize.X; } }
    /// <inheritdoc />
    public override int GetHeight() { lock (TextureGraphGate) { ThrowIfDisposed(); return (int)_imageSize.Y; } }

    /// <inheritdoc />
    /// <remarks>Translates the complete mesh at its authored size; transpose has no effect on the identity basis.</remarks>
    public override void Draw(CanvasItem canvasItem, Vector2 position, Color? modulate = null, bool transpose = false)
    {
        lock (TextureGraphGate)
        {
            ValidateDraw(canvasItem, new(position, Vector2.Zero), modulate);
            if (_mesh is not null && _baseTexture is not null) canvasItem.DrawMesh(_mesh, _baseTexture, new Transform(0, position), modulate);
        }
    }

    /// <inheritdoc />
    /// <remarks>Draws once, ignoring tile. Negative destination axes shift the origin by ImageSize on that axis.
    /// Transpose exchanges the diagonal scale components; UVs remain authored by the mesh.</remarks>
    public override void DrawRect(CanvasItem canvasItem, Rect2 rect, bool tile, Color? modulate = null, bool transpose = false)
    {
        lock (TextureGraphGate)
        {
            ValidateDraw(canvasItem, rect, modulate);
            if (_mesh is null || _baseTexture is null || _imageSize.X == 0 || _imageSize.Y == 0) return;
            var origin = rect.Position + new Vector2(rect.Size.X < 0 ? _imageSize.X : 0, rect.Size.Y < 0 ? _imageSize.Y : 0);
            var scale = rect.Size / _imageSize;
            if (transpose) scale = new(scale.Y, scale.X);
            canvasItem.DrawMesh(_mesh, _baseTexture, new Transform(new(scale.X, 0), new(0, scale.Y), origin), modulate);
        }
    }

    /// <inheritdoc />
    /// <remarks>Validates the requested source but draws the entire mesh with DrawRect's scaling; source/clip do not crop geometry.</remarks>
    public override void DrawRectRegion(CanvasItem canvasItem, Rect2 rect, Rect2 sourceRect, Color? modulate = null, bool transpose = false, bool clipUV = true)
    {
        if (!sourceRect.IsFinite()) throw new ArgumentException("The texture region must be finite.", nameof(sourceRect));
        DrawRect(canvasItem, rect, false, modulate, transpose);
    }

    private void ValidateDraw(CanvasItem canvasItem, Rect2 rect, Color? modulate)
    {
        ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(canvasItem);
        canvasItem.ValidateTextureDraw(this, rect, modulate ?? Colors.White);
    }
    internal override bool ReferencesTexture(Texture target, int depth) => base.ReferencesTexture(target, depth) || _baseTexture?.ReferencesTexture(target, depth + 1) == true;
    private void SourceChanged(Resource source)
    {
        lock (TextureGraphGate) { if (IsDisposed || !ReferenceEquals(source, _mesh) && !ReferenceEquals(source, _baseTexture)) return; }
        EmitChanged();
    }
    private void SourceDisposed(ElectronObject source)
    {
        lock (TextureGraphGate)
        {
            if (!ReferenceEquals(source, _mesh) && !ReferenceEquals(source, _baseTexture)) return;
            if (source is Resource resource) resource.Changed -= SourceChanged;
            source.Disposed -= SourceDisposed;
            if (ReferenceEquals(source, _mesh)) _mesh = null;
            if (ReferenceEquals(source, _baseTexture)) _baseTexture = null;
            if (IsDisposed) return;
        }
        EmitChanged();
    }

    private static readonly PropertyDescriptor[] Properties = [
        new PropertyDescriptor<MeshTexture, Mesh?>(nameof(Mesh), t => t.Mesh, (t, v) => t.Mesh = v, _ => null, stored: true),
        new PropertyDescriptor<MeshTexture, Texture?>(nameof(BaseTexture), t => t.BaseTexture, (t, v) => t.BaseTexture = v, _ => null, stored: true),
        new PropertyDescriptor<MeshTexture, Vector2>(nameof(ImageSize), t => t.ImageSize, (t, v) => t.ImageSize = v, _ => Vector2.Zero, stored: true)];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new MeshTexture();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        Mesh? mesh; Texture? texture; Vector2 size;
        lock (TextureGraphGate) { ThrowIfDisposed(); mesh = _mesh; texture = _baseTexture; size = _imageSize; }
        var copy = (MeshTexture)target;
        copy.Mesh = deep ? (Mesh?)duplicateSubresource(mesh) : mesh;
        copy.BaseTexture = deep ? (Texture?)duplicateSubresource(texture) : texture;
        copy.ImageSize = size;
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) lock (TextureGraphGate)
            {
                if (_mesh is not null) { _mesh.Changed -= SourceChanged; _mesh.Disposed -= SourceDisposed; }
                if (_baseTexture is not null) { _baseTexture.Changed -= SourceChanged; _baseTexture.Disposed -= SourceDisposed; }
                _mesh = null; _baseTexture = null;
            }
        base.Dispose(disposing);
    }
}
