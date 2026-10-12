namespace Electron2D;

/// <summary>Owns immutable caller-defined geometry with a stable physics identity and live revision tracking.</summary>
public sealed class CustomShape : Shape
{
    private ShapeGeometry _data;
    /// <summary>Creates an initialized custom collision resource.</summary>
    /// <param name="geometry">Immutable Custom geometry, including canonical data, mass, drawing and exact pair algorithm.</param>
    /// <exception cref="ArgumentException">Geometry is null or represents a built-in primitive.</exception>
    public CustomShape(ShapeGeometry geometry) { Validate(geometry); _data = geometry; }
    /// <summary>Gets or replaces immutable custom geometry while retaining the resource RID.</summary>
    /// <value>The complete custom geometry data. Replacement invalidates compilation and authored replay compatibility.</value>
    /// <exception cref="ArgumentException">Data is null or represents a built-in primitive.</exception>
    /// <exception cref="InvalidOperationException">A borrower owner, phase or geometry/authoring hook rejects the edit before mutation.</exception>
    public ShapeGeometry Geometry
    {
        get { ThrowIfDisposed(); return _data; }
        set { ThrowIfDisposed(); Validate(value); EnsureGeometryChangeAccess(); if (ReferenceEquals(_data, value)) return; _data = value; EmitGeometryChanged(); }
    }
    private static void Validate(ShapeGeometry geometry)
    {
        ArgumentNullException.ThrowIfNull(geometry);
        if (geometry.Type != PhysicsServer.ShapeType.Custom) throw new ArgumentException("CustomShape requires custom geometry.", nameof(geometry));
    }
    /// <inheritdoc />
    protected override ShapeGeometry GetGeometryCore() => _data;
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new CustomShape(_data);
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        base.CopyCustomStateTo(target, deep, subresourceMode, duplicateSubresource, forceDuplicateSubresource);
        ((CustomShape)target).Geometry = _data;
    }
}
