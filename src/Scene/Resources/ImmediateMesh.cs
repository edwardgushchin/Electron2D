namespace Electron2D;

/// <summary>Builds two-dimensional mesh surfaces by appending vertices with current attributes.</summary>
/// <remarks>A draft is invisible until SurfaceEnd succeeds. Completed surfaces use copied ArrayMesh
/// storage and the ordinary mesh renderer. Methods are serialized; callers must synchronize a whole
/// begin/end sequence when sharing a builder. Structural construction may allocate; replay does not
/// rebuild geometry. Materials are borrowed. Duplication copies completed surfaces, not an active draft.</remarks>
public sealed class ImmediateMesh : Mesh
{
    internal readonly ArrayMesh Surfaces = new();
    private readonly List<Vector2> _vertices = [], _uvs = [];
    private readonly List<Color> _colors = [];
    private PrimitiveType _primitive;
    private Material? _material;
    private Color _color;
    private Vector2 _uv;
    private bool _active, _usesColors, _usesUVs;

    /// <summary>Creates an empty mesh without an active draft.</summary>
    public ImmediateMesh() { }

    /// <summary>Begins an invisible surface using the selected topology and borrowed material.</summary>
    /// <param name="primitive">One of the five two-dimensional primitive topologies.</param>
    /// <param name="material">Live borrowed material, or null to use the drawing item's material.</param>
    /// <exception cref="InvalidOperationException">A surface is already being built.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The topology is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The mesh or material is disposed.</exception>
    public void SurfaceBegin(PrimitiveType primitive, Material? material = null)
    {
        lock (Surfaces.Gate)
        {
            ThrowIfDisposed();
            if (_active) throw new InvalidOperationException("A surface is already being built.");
            if (primitive is < PrimitiveType.Points or > PrimitiveType.TriangleStrip) throw new ArgumentOutOfRangeException(nameof(primitive));
            if (material is { IsDisposed: true }) throw new ObjectDisposedException(nameof(material));
            _primitive = primitive; _material = material; _active = true;
        }
    }

    /// <summary>Appends a finite local vertex using the current color and primary UV.</summary>
    /// <param name="vertex">Local two-dimensional position.</param>
    /// <exception cref="InvalidOperationException">No surface is being built.</exception>
    /// <exception cref="ArgumentException">The position is not finite.</exception>
    /// <exception cref="ObjectDisposedException">The mesh is disposed.</exception>
    public void SurfaceAddVertex2D(Vector2 vertex)
    {
        lock (Surfaces.Gate)
        {
            EnsureActive();
            if (!vertex.IsFinite()) throw new ArgumentException("Vertices must be finite.", nameof(vertex));
            _vertices.Add(vertex);
            if (_usesColors) _colors.Add(_color);
            if (_usesUVs) _uvs.Add(_uv);
        }
    }

    /// <summary>Sets the current color, enabling the color channel on its first call.</summary>
    /// <param name="color">Finite color; clamped and truncated to RGBA8 when committed.</param>
    /// <remarks>The first call also fills previously added vertices with this color.
    /// Later calls affect only subsequently appended vertices. A new surface starts without a color channel.</remarks>
    /// <exception cref="InvalidOperationException">No surface is being built.</exception>
    /// <exception cref="ArgumentException">The color is not finite.</exception>
    /// <exception cref="ObjectDisposedException">The mesh is disposed.</exception>
    public void SurfaceSetColor(Color color)
    {
        lock (Surfaces.Gate)
        {
            EnsureActive();
            if (!color.IsFinite()) throw new ArgumentException("Colors must be finite.", nameof(color));
            if (!_usesColors) { for (var i = 0; i < _vertices.Count; i++) _colors.Add(color); _usesColors = true; }
            _color = color;
        }
    }

    /// <summary>Sets the current primary UV, enabling that channel on its first call.</summary>
    /// <param name="uv">Finite texture coordinate; values outside zero through one use the sampler policy.</param>
    /// <remarks>The first call fills all previous vertices; subsequent calls affect only new vertices.
    /// New surfaces start without a UV channel.</remarks>
    /// <exception cref="InvalidOperationException">No surface is being built.</exception>
    /// <exception cref="ArgumentException">The coordinate is not finite.</exception>
    /// <exception cref="ObjectDisposedException">The mesh is disposed.</exception>
    public void SurfaceSetUV(Vector2 uv)
    {
        lock (Surfaces.Gate)
        {
            EnsureActive();
            if (!uv.IsFinite()) throw new ArgumentException("UVs must be finite.", nameof(uv));
            if (!_usesUVs) { for (var i = 0; i < _vertices.Count; i++) _uvs.Add(uv); _usesUVs = true; }
            _uv = uv;
        }
    }

    /// <summary>Validates and commits the draft, then emits Changed outside the storage lock.</summary>
    /// <remarks>An empty or incomplete primitive rejects without losing the draft; append the missing
    /// vertices and retry, or cancel with ClearSurfaces. A throwing Changed listener leaves the surface
    /// committed and the builder idle. Material disposal before commit rejects without consuming the draft.</remarks>
    /// <exception cref="InvalidOperationException">No surface is being built.</exception>
    /// <exception cref="ArgumentException">The draft is empty or its topology is incomplete.</exception>
    /// <exception cref="ObjectDisposedException">The mesh or draft material is disposed.</exception>
    public void SurfaceEnd()
    {
        lock (Surfaces.Gate)
        {
            EnsureActive();
            if (_material is { IsDisposed: true }) throw new ObjectDisposedException(nameof(_material));
            var index = Surfaces.GetSurfaceCount();
            Surfaces.AddSurfaceFromArrays(_primitive, new() { Vertices = _vertices.ToArray(), Colors = _colors.ToArray(), UVs = _uvs.ToArray() });
            Surfaces.Get(index).Material = _material;
            ResetDraft();
        }
        EmitChanged();
    }

    /// <summary>Removes completed surfaces and cancels any draft, then emits Changed.</summary>
    /// <remarks>Borrowed materials remain live. Retained drawing observes the emptied storage.</remarks>
    /// <exception cref="ObjectDisposedException">The mesh is disposed.</exception>
    public void ClearSurfaces()
    {
        lock (Surfaces.Gate) { ThrowIfDisposed(); Surfaces.ClearSurfaces(); ResetDraft(); }
        EmitChanged();
    }

    private void EnsureActive() { ThrowIfDisposed(); if (!_active) throw new InvalidOperationException("Begin a surface before editing it."); }
    private void ResetDraft() { _active = _usesColors = _usesUVs = false; _vertices.Clear(); _colors.Clear(); _uvs.Clear(); _material = null; }
    /// <inheritdoc />
    protected override int OnGetSurfaceCount() => Surfaces.GetSurfaceCount();
    /// <inheritdoc />
    protected override MeshSurfaceData OnSurfaceGetArrays(int surfaceIndex) => Surfaces.SurfaceGetArrays(surfaceIndex);
    /// <inheritdoc />
    protected override int OnSurfaceGetArrayLen(int surfaceIndex) => Surfaces.SurfaceGetArrayLen(surfaceIndex);
    /// <inheritdoc />
    protected override int OnSurfaceGetArrayIndexLen(int surfaceIndex) => Surfaces.SurfaceGetArrayIndexLen(surfaceIndex);
    /// <inheritdoc />
    protected override PrimitiveType OnSurfaceGetPrimitiveType(int surfaceIndex) => Surfaces.SurfaceGetPrimitiveType(surfaceIndex);
    /// <inheritdoc />
    protected override ArrayFormat OnSurfaceGetFormat(int surfaceIndex) => Surfaces.SurfaceGetFormat(surfaceIndex);
    /// <inheritdoc />
    protected override Material? OnSurfaceGetMaterial(int surfaceIndex) => Surfaces.SurfaceGetMaterial(surfaceIndex);
    /// <inheritdoc />
    protected override void OnSurfaceSetMaterial(int surfaceIndex, Material? material)
    {
        lock (Surfaces.Gate) { ThrowIfDisposed(); Surfaces.SurfaceSetMaterial(surfaceIndex, material); }
        EmitChanged();
    }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new ImmediateMesh();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        (MeshSurfaceData Data, PrimitiveType Primitive, Material? Material)[] copies;
        lock (Surfaces.Gate)
        {
            ThrowIfDisposed(); copies = new (MeshSurfaceData, PrimitiveType, Material?)[Surfaces.SurfaceCount];
            for (var i = 0; i < copies.Length; i++) { var surface = Surfaces.Get(i); copies[i] = (surface.Data.Copy(), surface.Primitive, surface.Material); }
        }
        var other = (ImmediateMesh)target;
        foreach (var copy in copies)
        {
            var material = (Material?)duplicateSubresource(copy.Material);
            lock (other.Surfaces.Gate)
            {
                other.ThrowIfDisposed(); var index = other.Surfaces.GetSurfaceCount();
                other.Surfaces.AddSurfaceFromArrays(copy.Primitive, copy.Data);
                other.Surfaces.SurfaceSetMaterial(index, material);
            }
        }
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        lock (Surfaces.Gate) { ResetDraft(); Surfaces.Dispose(); }
        base.Dispose(disposing);
    }
}
