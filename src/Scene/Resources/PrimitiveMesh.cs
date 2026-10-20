namespace Electron2D;

/// <summary>Generates one retained triangle surface from a typed C# geometry callback.</summary>
/// <remarks>The first geometry query generates the surface; subsequent RequestUpdate calls rebuild synchronously.
/// Generator arrays are copied and validated before publication. Failed updates preserve the last surface.
/// Generation is cold allocation-capable work; prepared canvas replay reuses its ordinary mesh snapshot.
/// Materials are borrowed. Derived resources implement their own duplication factory and stored parameter copying.</remarks>
public abstract class PrimitiveMesh : Mesh
{
    private readonly object _gate = new();
    private ArrayMesh.Surface? _surface;
    private Rect2 _bounds, _customAABB;
    private bool _building, _flipFaces, _addUV2;
    private float _uv2Padding = 2;
    private Material? _material;
    private static readonly PropertyDescriptor[] Properties =
    [
        new PropertyDescriptor<PrimitiveMesh, Rect2>(nameof(CustomAABB), m => m.CustomAABB, (m, v) => m.CustomAABB = v, _ => default, stored: true),
        new PropertyDescriptor<PrimitiveMesh, bool>(nameof(FlipFaces), m => m.FlipFaces, (m, v) => m.FlipFaces = v, _ => false, stored: true),
        new PropertyDescriptor<PrimitiveMesh, bool>(nameof(AddUV2), m => m.AddUV2, (m, v) => m.AddUV2 = v, _ => false, stored: true),
        new PropertyDescriptor<PrimitiveMesh, float>(nameof(UV2Padding), m => m.UV2Padding, (m, v) => m.UV2Padding = v, _ => 2, stored: true),
        new PropertyDescriptor<PrimitiveMesh, Material?>(nameof(Material), m => m.Material, (m, v) => m.Material = v, _ => null, stored: true)
    ];

    /// <summary>Initializes an ungenerated triangle mesh.</summary>
    protected PrimitiveMesh() { }

    /// <summary>Gets or sets an authored local visibility rectangle, separate from source geometry bounds.</summary>
    /// <value>Zero selects automatic bounds. Other finite nonnegative rectangles override visibility only.</value>
    /// <remarks>Assignments commit and emit Changed, including equal assignments; geometry is not regenerated.</remarks>
    /// <exception cref="ArgumentException">The rectangle has a nonfinite coordinate/end or negative size.</exception>
    public Rect2 CustomAABB
    {
        get { lock (_gate) { ThrowIfDisposed(); return _customAABB; } }
        set
        {
            lock (_gate) { CheckMutation(); if (!value.IsFinite() || !value.End.IsFinite() || value.Size.X < 0 || value.Size.Y < 0) throw new ArgumentException("Mesh visibility bounds must be finite nonnegative rectangles.", nameof(value)); _customAABB = value; }
            EmitChanged();
        }
    }
    internal override Rect2 VisibilityOverride => CustomAABB;

    /// <summary>Gets or sets whether triangle winding and supplied normals are reversed.</summary>
    /// <value>False initially. Sequential triangles acquire reversed indices without reordering their channels.</value>
    public bool FlipFaces
    {
        get { lock (_gate) { ThrowIfDisposed(); return _flipFaces; } }
        set { lock (_gate) { CheckMutation(); if (_flipFaces == value) return; _flipFaces = value; } RequestUpdate(); }
    }
    /// <summary>Gets or sets fallback generation of absent secondary UVs from supplied primary UVs.</summary>
    /// <value>False initially. Authored secondary UVs are retained independently of this setting.</value>
    public bool AddUV2
    {
        get { lock (_gate) { ThrowIfDisposed(); return _addUV2; } }
        set { lock (_gate) { CheckMutation(); if (_addUV2 == value) return; _addUV2 = value; } RequestUpdate(); }
    }
    /// <summary>Gets or sets finite right/bottom fallback UV padding in pixels of a 1024-square reference image.</summary>
    /// <value>Two initially. Finite negative values expand the generated UV range; no editor-hint clamping is applied.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is nonfinite.</exception>
    public float UV2Padding
    {
        get { lock (_gate) { ThrowIfDisposed(); return _uv2Padding; } }
        set { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); lock (_gate) { CheckMutation(); if (_uv2Padding == value) return; _uv2Padding = value; } RequestUpdate(); }
    }
    /// <summary>Gets or sets the borrowed material for the single surface.</summary>
    /// <value>Null initially. Live changes update the published material without regenerating geometry.</value>
    /// <exception cref="ObjectDisposedException">The mesh or assigned material is disposed.</exception>
    public Material? Material
    {
        get { lock (_gate) { ThrowIfDisposed(); return _material; } }
        set
        {
            bool notify;
            lock (_gate)
            {
                CheckMutation(); if (value is { IsDisposed: true }) throw new ObjectDisposedException(nameof(value));
                if (ReferenceEquals(value, _material)) return;
                _material = value; notify = _surface is not null; if (_surface is { } surface) surface.Material = value;
            }
            if (notify) EmitChanged();
        }
    }

    /// <summary>Returns an independent copy of the generated single-surface channels.</summary>
    /// <returns>Caller-owned typed arrays; the first call generates them.</returns>
    public MeshSurfaceData GetMeshArrays() => SurfaceGetArrays(0);

    /// <summary>Rebuilds an already generated surface synchronously, or leaves initial generation pending.</summary>
    /// <remarks>Changed runs after successful publication. A throwing observer does not roll back the new surface.
    /// Callback/validation failures preserve previous geometry; correct the source and request another update.</remarks>
    /// <exception cref="InvalidOperationException">A generation callback attempts a nested update.</exception>
    public void RequestUpdate()
    {
        lock (_gate) { CheckMutation(); if (_surface is null) return; }
        Rebuild();
    }

    /// <summary>Produces finite positioned triangle data from the derived resource's parameters.</summary>
    /// <returns>Non-null triangle channels with at least one position, copied before any postprocessing.</returns>
    /// <remarks>Called outside the mesh lock. Settings may be read. Initial surface queries and mutation during
    /// generation reject; an existing published surface remains readable. Native and scene mutations retain their guards.</remarks>
    protected abstract MeshSurfaceData OnCreateMeshArray();

    /// <inheritdoc />
    public override RID GetRID() { EnsureGenerated(); return base.GetRID(); }

    internal void EnsureGenerated()
    {
        lock (_gate) { ThrowIfDisposed(); if (_surface is not null) return; if (_building) throw new InvalidOperationException("Primitive generation cannot query its initial surface recursively."); }
        Rebuild();
    }
    private void CheckMutation()
    { ThrowIfDisposed(); if (_building) throw new InvalidOperationException("Primitive settings cannot change during generation."); }
    private void Rebuild()
    {
        bool flip, uv2; float padding; Material? material;
        lock (_gate) { CheckMutation(); _building = true; flip = _flipFaces; uv2 = _addUV2; padding = _uv2Padding; material = _material; }
        try
        {
            var supplied = OnCreateMeshArray(); ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(supplied);
            var data = supplied.Copy(); data.Validate(PrimitiveType.Triangles);
            if (data.Vertices.Length == 0) throw new ArgumentException("Primitive generators must supply positions.");
            if (flip)
            {
                for (var i = 0; i < data.Normals.Length; i++) data.Normals[i] = -data.Normals[i];
                if (data.Indices.Length == 0) { data.Indices = new int[data.Vertices.Length]; for (var i = 0; i < data.Indices.Length; i++) data.Indices[i] = i; }
                for (var i = 0; i < data.Indices.Length; i += 3) (data.Indices[i], data.Indices[i + 1]) = (data.Indices[i + 1], data.Indices[i]);
            }
            if (uv2 && data.UV2s.Length == 0 && data.UVs.Length != 0)
            {
                var scale = 1 - padding / 1024; data.UV2s = new Vector2[data.UVs.Length];
                for (var i = 0; i < data.UV2s.Length; i++) data.UV2s[i] = data.UVs[i] * scale;
            }
            data.Validate(PrimitiveType.Triangles); data.QuantizeSkin();
            var bounds = new Rect2(data.Vertices[0], Vector2.Zero); foreach (var vertex in data.Vertices) bounds = bounds.Expand(vertex);
            if (!bounds.IsFinite() || !bounds.End.IsFinite()) throw new ArgumentException("Primitive bounds exceed the finite coordinate range.");
            var surface = new ArrayMesh.Surface(data, PrimitiveType.Triangles, data.SkinSlots == 8 ? ArrayFormat.Use8BoneWeights : ArrayFormat.None) { Material = material };
            lock (_gate) { ThrowIfDisposed(); _surface = surface; _bounds = bounds; }
        }
        finally { lock (_gate) _building = false; }
        EmitChanged();
    }

    private ArrayMesh.Surface Get(int index)
    { ArgumentOutOfRangeException.ThrowIfNotEqual(index, 0); EnsureGenerated(); lock (_gate) { ThrowIfDisposed(); return _surface!; } }
    /// <inheritdoc />
    protected sealed override int OnGetSurfaceCount() { EnsureGenerated(); return 1; }
    /// <inheritdoc />
    protected sealed override MeshSurfaceData OnSurfaceGetArrays(int surfaceIndex) => Get(surfaceIndex).Data.Copy();
    /// <inheritdoc />
    protected sealed override int OnSurfaceGetArrayLen(int surfaceIndex) => Get(surfaceIndex).Data.VertexCount;
    /// <inheritdoc />
    protected sealed override int OnSurfaceGetArrayIndexLen(int surfaceIndex) => Get(surfaceIndex).Data.Indices.Length;
    /// <inheritdoc />
    protected sealed override PrimitiveType OnSurfaceGetPrimitiveType(int surfaceIndex) { Get(surfaceIndex); return PrimitiveType.Triangles; }
    /// <inheritdoc />
    protected sealed override ArrayFormat OnSurfaceGetFormat(int surfaceIndex) => Get(surfaceIndex).Format;
    /// <inheritdoc />
    protected sealed override Material? OnSurfaceGetMaterial(int surfaceIndex) { ArgumentOutOfRangeException.ThrowIfNotEqual(surfaceIndex, 0); return Material; }
    /// <inheritdoc />
    protected sealed override void OnSurfaceSetMaterial(int surfaceIndex, Material? material) { ArgumentOutOfRangeException.ThrowIfNotEqual(surfaceIndex, 0); Material = material; }
    /// <inheritdoc />
    protected sealed override Rect2 OnGetAABB() { EnsureGenerated(); lock (_gate) { ThrowIfDisposed(); return _bounds; } }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        bool flip, uv2; float padding; Material? material; Rect2 custom;
        lock (_gate) { ThrowIfDisposed(); custom = _customAABB; flip = _flipFaces; uv2 = _addUV2; padding = _uv2Padding; material = _material; }
        var copiedMaterial = deep ? (Material?)duplicateSubresource(material) : material;
        var other = (PrimitiveMesh)target;
        lock (other._gate) { other.CheckMutation(); other._flipFaces = flip; other._addUV2 = uv2; other._uv2Padding = padding; other._material = copiedMaterial; other._customAABB = custom; other._surface = null; other._bounds = default; }
    }
    /// <inheritdoc />
    protected override void OnResetState()
    { lock (_gate) { CheckMutation(); _surface = null; _bounds = default; } }
    internal override void ValidateStateMutation() { lock (_gate) CheckMutation(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    { lock (_gate) { _surface = null; _material = null; } base.Dispose(disposing); }
}
