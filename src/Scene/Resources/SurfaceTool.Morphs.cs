namespace Electron2D;

public sealed partial class SurfaceTool
{
    /// <summary>Replaces geometry with the named raw target pose and the base surface's remaining channels.</summary>
    /// <param name="existing">Live shaped source mesh.</param>
    /// <param name="surface">Existing zero-based surface index.</param>
    /// <param name="blendShape">Exact ordinal shape name.</param>
    /// <remarks>Copies target positions/normals/tangents, preserving base color/UV/custom/index/skin data and
    /// borrowed material. Relative offsets remain raw; extraction does not apply a deformation weight.
    /// Failed names, schemas or source callbacks preserve the previous builder.</remarks>
    public void CreateFromBlendShape(Mesh existing, int surface, string blendShape)
    {
        lock (_gate)
        {
            Mutable(); ArgumentNullException.ThrowIfNull(existing); ArgumentNullException.ThrowIfNull(blendShape); _readingSource = true;
            try
            {
                var revision = existing.ChangeRevision; var count = existing.GetBlendShapeCount(); var index = -1;
                for (var i = 0; i < count; i++) if (string.Equals(existing.GetBlendShapeName(i), blendShape, StringComparison.Ordinal)) { index = i; break; }
                if (index < 0) throw new ArgumentException("The named blend shape does not exist.", nameof(blendShape));
                var shapes = existing.SurfaceGetBlendShapeArrays(surface); if (shapes.Length != count) throw new ArgumentException("Shape payload count must match definitions."); var shape = shapes[index].Copy(); var source = ReadSource(existing, surface);
                if (revision != existing.ChangeRevision || existing.IsDisposed) throw new InvalidOperationException("Blend shape source callbacks must retain a stable live revision.");
                var mask = Mesh.ArrayFormat.Vertex | (shape.Normals.Length != 0 ? Mesh.ArrayFormat.Normal : 0) | (shape.Tangents.Length != 0 ? Mesh.ArrayFormat.Tangent : 0); ArrayMesh.ValidateBlendShape(shape, source.Data, mask);
                source.Data.Vertices = shape.Vertices; source.Data.Normals = shape.Normals; source.Data.Tangents = shape.Tangents; source.Data.Validate(source.Primitive); Import(source.Data, source.Primitive, source.Material);
            }
            finally { _readingSource = false; }
        }
    }
}
