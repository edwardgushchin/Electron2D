namespace Electron2D;

/// <summary>Describes indexed two-dimensional surfaces consumed by retained canvas drawing.</summary>
/// <remarks>Surfaces use typed copied arrays. Materials and textures are borrowed. Implementations provide the
/// surface callbacks; geometry, naming and storage changes publish Resource.Changed after committing.</remarks>
public abstract class Mesh : Resource
{
    /// <summary>Defines the ordering of a surface's indexed or sequential vertices.</summary>
    public enum PrimitiveType
    {
        /// <summary>Independent one-framebuffer-pixel points.</summary>
        Points = 0,
        /// <summary>Independent pairs of one-framebuffer-pixel line vertices.</summary>
        Lines = 1,
        /// <summary>Connected one-framebuffer-pixel line segments.</summary>
        LineStrip = 2,
        /// <summary>Independent triangles.</summary>
        Triangles = 3,
        /// <summary>Connected triangles with alternating winding.</summary>
        TriangleStrip = 4
    }
    /// <summary>Reports the channels and policy flags present in a mesh surface.</summary>
    [Flags]
    public enum ArrayFormat : ulong
    {
        /// <summary>No channels or policies.</summary>
        None = 0,
        /// <summary>Vertex positions.</summary>
        Vertex = 1,
        /// <summary>Vertex colors.</summary>
        Color = 8,
        /// <summary>Primary texture coordinates.</summary>
        TexUV = 16,
        /// <summary>Vertex ordering indices.</summary>
        Index = 4096,
        /// <summary>Flattened unsigned 16-bit bone indices.</summary>
        Bones = 1024,
        /// <summary>Flattened unsigned normalized 16-bit skin weights.</summary>
        Weights = 2048,
        /// <summary>Skin records contain eight indices and eight weights instead of four.</summary>
        Use8BoneWeights = 134217728,
        /// <summary>Vertex positions use two floating components.</summary>
        Use2DVertices = 33554432,
        /// <summary>Surface buffers accept explicit region updates.</summary>
        UseDynamicUpdate = 67108864
    }
    private readonly object _ridGate = new();
    private RID _rid;
    private CanvasMesh? _boundsCache;
    /// <summary>Returns the stable borrowed identity of this mesh.</summary>
    /// <returns>A renderer-independent logical identity valid until resource disposal.</returns>
    /// <exception cref="ObjectDisposedException">The mesh is disposed.</exception>
    public override RID GetRID() { lock (_ridGate) { ThrowIfDisposed(); return _rid.IsValid() ? _rid : _rid = RenderingMeshRegistry.Register(this); } }
    internal RID RegisterOwned(RenderingServer owner) { lock (_ridGate) { ThrowIfDisposed(); return _rid = RenderingMeshRegistry.Register(this, owner); } }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { lock (_ridGate) { if (_rid.IsValid()) RenderingMeshRegistry.Remove(_rid); _rid = default; _boundsCache?.Clear(); _boundsCache = null; } base.Dispose(disposing); }
    /// <summary>Initializes the reusable mesh resource contract.</summary>
    protected Mesh() { }
    /// <summary>Gets the number of currently authored surfaces.</summary>
    /// <returns>The nonnegative surface count.</returns>
    /// <exception cref="ObjectDisposedException">The mesh is disposed.</exception>
    public int GetSurfaceCount() { ThrowIfDisposed(); var count = OnGetSurfaceCount(); if (count < 0) throw new InvalidOperationException("Mesh surface count must be nonnegative."); return count; }
    /// <summary>Returns the axis-aligned visibility bounds of this two-dimensional mesh.</summary>
    /// <returns>A finite local rectangle, empty without vertices.</returns>
    /// <remarks>Default bounds include stored vertices from every topology, including unreferenced vertices.
    /// Prepared custom surface snapshots are reused; live ArrayMesh positions are read without copied queries.</remarks>
    /// <exception cref="ArgumentException">A custom bounds callback supplies an invalid rectangle.</exception>
    public Rect2 GetAABB()
    {
        ThrowIfDisposed(); var bounds = OnGetAABB(); ThrowIfDisposed();
        if (!bounds.IsFinite() || !bounds.End.IsFinite() || bounds.Size.X < 0 || bounds.Size.Y < 0) throw new ArgumentException("Mesh bounds must be finite nonnegative rectangles.");
        return bounds;
    }
    /// <summary>Supplies the local two-dimensional visibility rectangle.</summary>
    /// <returns>Finite nonnegative local bounds; the default derives them from prepared surface positions.</returns>
    protected virtual Rect2 OnGetAABB()
    {
        lock (_ridGate) { ThrowIfDisposed(); return (_boundsCache ??= new(this, Transform.Identity, Colors.White)).GetLocalBounds(); }
    }
    /// <summary>Gets copied local vertices for every triangle face.</summary>
    /// <returns>Three vertices per face; point and line surfaces contribute no vertices.</returns>
    /// <remarks>Indexed and sequential triangles and triangle strips are expanded in surface order.
    /// Strip winding alternates to preserve face orientation. The result is caller-owned cold query storage.</remarks>
    /// <exception cref="ObjectDisposedException">The mesh is disposed.</exception>
    /// <exception cref="ArgumentException">Custom surface arrays are malformed.</exception>
    public Vector2[] GetFaces()
    {
        var faces = new List<Vector2>();
        var count = GetSurfaceCount();
        for (var surface = 0; surface < count; surface++)
        {
            var primitive = SurfaceGetPrimitiveType(surface);
            if (primitive is not (PrimitiveType.Triangles or PrimitiveType.TriangleStrip)) continue;
            var data = SurfaceGetArrays(surface); data.Validate(primitive);
            var length = data.Indices.Length == 0 ? data.Vertices.Length : data.Indices.Length;
            if (primitive == PrimitiveType.Triangles)
                for (var i = 0; i < length; i++) faces.Add(data.Vertices[data.Indices.Length == 0 ? i : data.Indices[i]]);
            else
                for (var i = 0; i + 2 < length; i++)
                {
                    var a = i + i % 2; var b = i + 1 - i % 2;
                    faces.Add(data.Vertices[data.Indices.Length == 0 ? a : data.Indices[a]]);
                    faces.Add(data.Vertices[data.Indices.Length == 0 ? b : data.Indices[b]]);
                    faces.Add(data.Vertices[data.Indices.Length == 0 ? i + 2 : data.Indices[i + 2]]);
                }
        }
        return faces.ToArray();
    }
    /// <summary>Supplies the authored surface count.</summary>
    /// <returns>A nonnegative count.</returns>
    protected abstract int OnGetSurfaceCount();
    /// <summary>Gets independent typed arrays for one surface.</summary>
    /// <param name="surfaceIndex">Existing zero-based surface index.</param>
    /// <returns>Caller-owned copies of the surface channels.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the mesh.</exception>
    /// <exception cref="ObjectDisposedException">The mesh is disposed.</exception>
    public MeshSurfaceData SurfaceGetArrays(int surfaceIndex) { ThrowIfDisposed(); return OnSurfaceGetArrays(surfaceIndex); }
    /// <summary>Supplies copied typed arrays for one surface.</summary>
    /// <param name="surfaceIndex">Existing zero-based index.</param>
    /// <returns>Independent channel arrays.</returns>
    protected abstract MeshSurfaceData OnSurfaceGetArrays(int surfaceIndex);
    /// <summary>Gets one surface's vertex count.</summary>
    /// <param name="surfaceIndex">Existing zero-based surface index.</param>
    /// <returns>The number of vertex positions.</returns>
    public int SurfaceGetArrayLen(int surfaceIndex) { ThrowIfDisposed(); return OnSurfaceGetArrayLen(surfaceIndex); }
    /// <summary>Supplies a surface's vertex count.</summary>
    /// <param name="surfaceIndex">Existing zero-based index.</param>
    /// <returns>The vertex count.</returns>
    protected abstract int OnSurfaceGetArrayLen(int surfaceIndex);
    /// <summary>Gets one surface's explicit index count.</summary>
    /// <param name="surfaceIndex">Existing zero-based surface index.</param>
    /// <returns>Zero for sequential vertices; otherwise the index count.</returns>
    public int SurfaceGetArrayIndexLen(int surfaceIndex) { ThrowIfDisposed(); return OnSurfaceGetArrayIndexLen(surfaceIndex); }
    /// <summary>Supplies the explicit index count.</summary>
    /// <param name="surfaceIndex">Existing zero-based index.</param>
    /// <returns>The index count.</returns>
    protected abstract int OnSurfaceGetArrayIndexLen(int surfaceIndex);
    /// <summary>Gets one surface's primitive topology.</summary>
    /// <param name="surfaceIndex">Existing zero-based surface index.</param>
    /// <returns>The primitive ordering policy.</returns>
    public PrimitiveType SurfaceGetPrimitiveType(int surfaceIndex) { ThrowIfDisposed(); return OnSurfaceGetPrimitiveType(surfaceIndex); }
    /// <summary>Supplies the primitive ordering policy.</summary>
    /// <param name="surfaceIndex">Existing zero-based index.</param>
    /// <returns>The topology.</returns>
    protected abstract PrimitiveType OnSurfaceGetPrimitiveType(int surfaceIndex);
    /// <summary>Gets one surface's typed channel and policy mask.</summary>
    /// <param name="surfaceIndex">Existing zero-based surface index.</param>
    /// <returns>The format mask.</returns>
    public ArrayFormat SurfaceGetFormat(int surfaceIndex) { ThrowIfDisposed(); return OnSurfaceGetFormat(surfaceIndex); }
    /// <summary>Supplies a surface format mask.</summary>
    /// <param name="surfaceIndex">Existing zero-based index.</param>
    /// <returns>The format mask.</returns>
    protected abstract ArrayFormat OnSurfaceGetFormat(int surfaceIndex);
    /// <summary>Gets a surface's borrowed material.</summary>
    /// <param name="surfaceIndex">Existing zero-based surface index.</param>
    /// <returns>The material or null for the drawing item's material.</returns>
    public Material? SurfaceGetMaterial(int surfaceIndex) { ThrowIfDisposed(); return OnSurfaceGetMaterial(surfaceIndex); }
    /// <summary>Supplies a surface's borrowed material.</summary>
    /// <param name="surfaceIndex">Existing zero-based index.</param>
    /// <returns>The borrowed material or null.</returns>
    protected abstract Material? OnSurfaceGetMaterial(int surfaceIndex);
    /// <summary>Assigns a borrowed material to one surface.</summary>
    /// <param name="surfaceIndex">Existing zero-based surface index.</param>
    /// <param name="material">Borrowed live material or null.</param>
    /// <exception cref="ObjectDisposedException">The mesh or material is disposed.</exception>
    public void SurfaceSetMaterial(int surfaceIndex, Material? material) { ThrowIfDisposed(); if (material is { IsDisposed: true }) throw new ObjectDisposedException(nameof(material)); OnSurfaceSetMaterial(surfaceIndex, material); }
    /// <summary>Commits a borrowed surface material and reports its change.</summary>
    /// <param name="surfaceIndex">Existing zero-based index.</param>
    /// <param name="material">Borrowed live material or null.</param>
    protected abstract void OnSurfaceSetMaterial(int surfaceIndex, Material? material);
}
