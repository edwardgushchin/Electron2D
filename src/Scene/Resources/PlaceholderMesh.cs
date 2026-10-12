namespace Electron2D;

/// <summary>Retains two-dimensional bounds for an absent mesh without inventing drawable surfaces.</summary>
public sealed class PlaceholderMesh : Mesh
{
    private Rect2 _aabb;
    internal MissingResourceData Data = new();
    /// <summary>Creates an empty surface-free mesh with zero bounds.</summary>
    public PlaceholderMesh() { }
    /// <summary>Gets or sets the mesh-local visibility rectangle.</summary>
    /// <value>Finite coordinates and nonnegative size. Zero initially.</value>
    public Rect2 AABB { get { ThrowIfDisposed(); return _aabb; } set { ThrowIfDisposed(); if (!value.IsFinite() || !value.End.IsFinite() || value.Size.X < 0 || value.Size.Y < 0) throw new ArgumentException("Placeholder bounds must be finite and nonnegative.", nameof(value)); _aabb = value; EmitChanged(); } }
    /// <inheritdoc />
    protected override Rect2 OnGetAABB() => _aabb;
    /// <inheritdoc />
    protected override int OnGetSurfaceCount() => 0;
    /// <inheritdoc />
    protected override MeshSurfaceData OnSurfaceGetArrays(int surfaceIndex) => throw new ArgumentOutOfRangeException(nameof(surfaceIndex));
    /// <inheritdoc />
    protected override int OnSurfaceGetArrayLen(int surfaceIndex) => throw new ArgumentOutOfRangeException(nameof(surfaceIndex));
    /// <inheritdoc />
    protected override int OnSurfaceGetArrayIndexLen(int surfaceIndex) => throw new ArgumentOutOfRangeException(nameof(surfaceIndex));
    /// <inheritdoc />
    protected override PrimitiveType OnSurfaceGetPrimitiveType(int surfaceIndex) => throw new ArgumentOutOfRangeException(nameof(surfaceIndex));
    /// <inheritdoc />
    protected override ArrayFormat OnSurfaceGetFormat(int surfaceIndex) => throw new ArgumentOutOfRangeException(nameof(surfaceIndex));
    /// <inheritdoc />
    protected override Material? OnSurfaceGetMaterial(int surfaceIndex) => throw new ArgumentOutOfRangeException(nameof(surfaceIndex));
    /// <inheritdoc />
    protected override void OnSurfaceSetMaterial(int surfaceIndex, Material? material) => throw new ArgumentOutOfRangeException(nameof(surfaceIndex));
    private static readonly PropertyDescriptor[] Properties = [new PropertyDescriptor<PlaceholderMesh, Rect2>(nameof(AABB), r => r.AABB, (r, v) => r.AABB = v, _ => default, stored: true)];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new PlaceholderMesh();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource) { ThrowIfDisposed(); var other = (PlaceholderMesh)target; other._aabb = _aabb; other.Data = Data.Copy(duplicateSubresource); }
    /// <inheritdoc />
    protected override void RemapResourceReferences(Func<Resource, Resource> remap) { base.RemapResourceReferences(remap); Data.Remap(remap); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { Data.Fields.Clear(); base.Dispose(disposing); }
}
