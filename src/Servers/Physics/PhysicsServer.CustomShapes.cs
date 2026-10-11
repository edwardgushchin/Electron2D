namespace Electron2D;

public sealed partial class PhysicsServer
{
    /// <summary>Creates a server-owned initialized custom shape with immutable authoring data.</summary>
    /// <param name="geometry">Complete Custom geometry understood by a registered world implementation.</param>
    /// <returns>A caller-owned shape RID. ShapeGetData returns an independent CustomShape wrapper; release with FreeRID.</returns>
    /// <exception cref="ArgumentException">Geometry is null or represents a built-in primitive.</exception>
    public static RID CustomShapeCreate(ShapeGeometry geometry) => Service.CreateShape(new CustomShape(geometry));
}
