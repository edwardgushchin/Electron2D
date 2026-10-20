namespace Electron2D.Examples.PrimitiveMeshes;

// A consumer-owned planar generator; the runtime keeps the shared surface contract.
internal sealed class DiskMesh : PrimitiveMesh
{
    internal static DiskMesh Create() => new();
    private float _radius = 20;
    private int _segments = 32;
    private static readonly PropertyDescriptor[] Properties =
    [
        new PropertyDescriptor<DiskMesh, float>(nameof(Radius), m => m.Radius, (m, v) => m.Radius = v, _ => 20, stored: true),
        new PropertyDescriptor<DiskMesh, int>(nameof(Segments), m => m.Segments, (m, v) => m.Segments = v, _ => 32, stored: true)
    ];
    public float Radius
    {
        get => _radius;
        set { ThrowIfDisposed(); if (!float.IsFinite(value) || value <= 0 || value > 1000000) throw new ArgumentOutOfRangeException(nameof(value)); if (_radius == value) return; _radius = value; RequestUpdate(); }
    }
    public int Segments
    {
        get => _segments;
        set { ThrowIfDisposed(); if (value is < 3 or > 4096) throw new ArgumentOutOfRangeException(nameof(value)); if (_segments == value) return; _segments = value; RequestUpdate(); }
    }
    protected override MeshSurfaceData OnCreateMeshArray()
    {
        var radius = _radius; var segments = _segments;
        var vertices = new Vector2[segments + 1]; var uvs = new Vector2[vertices.Length]; var normals = new Vector3[vertices.Length]; var indices = new int[segments * 3];
        uvs[0] = new(.5f, .5f); normals[0] = new(0, 0, 1);
        for (var i = 0; i < segments; i++)
        {
            var angle = MathF.Tau * i / segments; var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            vertices[i + 1] = radius * direction; uvs[i + 1] = (direction + Vector2.One) * .5f; normals[i + 1] = new(0, 0, 1);
            indices[i * 3] = 0; indices[i * 3 + 1] = i + 1; indices[i * 3 + 2] = (i + 1) % segments + 1;
        }
        return new() { Vertices = vertices, UVs = uvs, Normals = normals, Indices = indices };
    }
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
    protected override Resource CreateDuplicateInstance() => new DiskMesh();
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        base.CopyCustomStateTo(target, deep, subresourceMode, duplicateSubresource, forceDuplicateSubresource);
        var other = (DiskMesh)target; other._radius = _radius; other._segments = _segments;
    }
}
