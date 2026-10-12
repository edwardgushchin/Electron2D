namespace Electron2D;

public sealed partial class ImporterMesh
{
    /// <summary>Creates transformed raw surfaces, optionally joining compatible unskinned names.</summary>
    /// <param name="importerMeshes">One or more live source producers.</param>
    /// <param name="relativeTransforms">One finite local two-dimensional transform per producer.</param>
    /// <param name="deduplicateSurfaces">Joins equal names, flags, channel schemas and topologies except line strips; skin surfaces remain separate.</param>
    /// <returns>A new caller-owned producer in normalized mode.</returns>
    /// <remarks>Named targets and authored/generated levels are discarded. Joined surfaces retain the first material.
    /// Unnamed input surfaces use surface_N; incompatible collisions use the first free underscore suffix starting at two.
    /// Reflections retain complete geometry/winding, including strips, mixed indexing and all four/eight skin records.
    /// Schema mismatches and independent line strips stay separate rather than producing malformed channels or connecting segments.</remarks>
    public static ImporterMesh MergeImporterMeshes(IReadOnlyList<ImporterMesh> importerMeshes, IReadOnlyList<Transform> relativeTransforms, bool deduplicateSurfaces = true)
    {
        ArgumentNullException.ThrowIfNull(importerMeshes); ArgumentNullException.ThrowIfNull(relativeTransforms);
        var sources = importerMeshes.ToArray(); var transforms = relativeTransforms.ToArray(); if (sources.Length == 0 || sources.Length != transforms.Length) throw new ArgumentException("Sources and transforms require the same nonzero count.");
        var revisions = new long[sources.Length]; var groups = new Dictionary<string, Surface>(StringComparer.Ordinal);
        for (var i = 0; i < sources.Length; i++)
        {
            var source = sources[i] ?? throw new ArgumentNullException(nameof(importerMeshes)); var transform = transforms[i]; if (!transform.X.IsFinite() || !transform.Y.IsFinite() || !transform.Origin.IsFinite()) throw new ArgumentException("Merge transforms must be finite.", nameof(relativeTransforms));
            Surface[] surfaces; lock (source._gate) { source.ThrowIfDisposed(); revisions[i] = source.ChangeRevision; surfaces = source._surfaces.Select(s => s.Copy()).ToArray(); }
            for (var j = 0; j < surfaces.Length; j++)
            {
                var s = surfaces[j]; TransformData(s.Data, s.Primitive, transform); var name = s.Name.Length == 0 ? "surface_" + j.ToString(System.Globalization.CultureInfo.InvariantCulture) : s.Name;
                if (groups.TryGetValue(name, out var existing))
                {
                    if (deduplicateSurfaces && s.Data.Bones.Length == 0 && existing.Data.Bones.Length == 0 && s.Primitive != Mesh.PrimitiveType.LineStrip && existing.Primitive == s.Primitive && existing.Flags == s.Flags && (ArrayMesh.Surface.Channels(existing.Data) & ~Mesh.ArrayFormat.Index) == (ArrayMesh.Surface.Channels(s.Data) & ~Mesh.ArrayFormat.Index))
                    {
                        groups[name] = new(Join(existing.Data, s.Data, s.Primitive), [], [], s.Primitive, s.Flags, name, existing.Material); continue;
                    }
                    var original = name; var suffix = 2; do { name = original + "_" + (suffix++).ToString(System.Globalization.CultureInfo.InvariantCulture); } while (groups.ContainsKey(name));
                }
                groups.Add(name, new(s.Data, [], [], s.Primitive, s.Flags, name, s.Material));
            }
        }
        var result = new ImporterMesh();
        try
        {
            foreach (var s in groups.Values) result.AddSurface(s.Primitive, s.Data, material: s.Material, name: s.Name, flags: s.Flags);
            for (var i = 0; i < sources.Length; i++) if (sources[i].IsDisposed || sources[i].ChangeRevision != revisions[i]) throw new InvalidOperationException("Merge requires stable live sources."); return result;
        }
        catch { result.Dispose(); throw; }
    }
    private static void TransformData(MeshSurfaceData data, Mesh.PrimitiveType primitive, Transform transform)
    {
        for (var i = 0; i < data.Vertices.Length; i++)
        {
            var point = transform * data.Vertices[i]; if (!point.IsFinite()) throw new ArgumentException("Merge positions must remain finite.", nameof(transform)); data.Vertices[i] = point;
            if (data.Normals.Length != 0) { var n = data.Normals[i]; data.Normals[i] = MeshFrameCodec.Unit(transform.X.X * (double)n.X + transform.Y.X * (double)n.Y, transform.X.Y * (double)n.X + transform.Y.Y * (double)n.Y, n.Z); }
            if (data.Tangents.Length != 0) { var t = data.Tangents[i]; var v = MeshFrameCodec.Unit(transform.X.X * (double)t.X + transform.Y.X * (double)t.Y, transform.X.Y * (double)t.X + transform.Y.Y * (double)t.Y, t.Z); data.Tangents[i] = new(v.X, v.Y, v.Z, t.W); }
        }
        var determinant = (double)transform.X.X * transform.Y.Y - (double)transform.X.Y * transform.Y.X;
        if (determinant < 0 && primitive == Mesh.PrimitiveType.Triangles)
        {
            if (data.Indices.Length != 0) for (var i = 0; i < data.Indices.Length; i += 3) (data.Indices[i + 1], data.Indices[i + 2]) = (data.Indices[i + 2], data.Indices[i + 1]);
            else for (var i = 0; i < data.Vertices.Length; i += 3) Swap(data, i + 1, i + 2);
        }
        else if (determinant < 0 && primitive == Mesh.PrimitiveType.TriangleStrip)
        {
            var order = Order(data); var flipped = new int[order.Length + 1]; flipped[0] = order[0]; order.CopyTo(flipped, 1); data.Indices = flipped;
        }
        data.Validate(primitive);
    }
    private static void Swap(MeshSurfaceData d, int a, int b)
    {
        static void Values<T>(T[] values, int a, int b) { if (values.Length != 0) (values[a], values[b]) = (values[b], values[a]); }
        Values(d.Vertices, a, b); Values(d.Normals, a, b); Values(d.Tangents, a, b); Values(d.Colors, a, b); Values(d.UVs, a, b); Values(d.UV2s, a, b);
        for (var slot = 0; slot < d.SkinSlots; slot++) { var x = a * d.SkinSlots + slot; var y = b * d.SkinSlots + slot; Values(d.Bones, x, y); Values(d.Weights, x, y); }
        for (var c = 0; c < 4; c++) if (d.Custom(c) is { } custom) { var stride = MeshCustomCodec.Stride(custom.Format); for (var k = 0; k < stride; k++) Values(custom.Data, a * stride + k, b * stride + k); }
    }
    private static int[] Order(MeshSurfaceData data) => data.Indices.Length == 0 ? Enumerable.Range(0, data.Vertices.Length).ToArray() : data.Indices;
    private static MeshSurfaceData Join(MeshSurfaceData a, MeshSurfaceData b, Mesh.PrimitiveType primitive)
    {
        static T[] Concat<T>(T[] x, T[] y) { var result = new T[checked(x.Length + y.Length)]; x.CopyTo(result, 0); y.CopyTo(result, x.Length); return result; }
        var d = new MeshSurfaceData { Vertices = Concat(a.Vertices, b.Vertices), Normals = Concat(a.Normals, b.Normals), Tangents = Concat(a.Tangents, b.Tangents), Colors = Concat(a.Colors, b.Colors), UVs = Concat(a.UVs, b.UVs), UV2s = Concat(a.UV2s, b.UV2s) };
        for (var c = 0; c < 4; c++) if (a.Custom(c) is { } x) d.SetCustom(c, new() { Format = x.Format, Data = Concat(x.Data, b.Custom(c)!.Data) });
        if (a.Indices.Length != 0 || b.Indices.Length != 0 || primitive == Mesh.PrimitiveType.TriangleStrip)
        {
            var x = Order(a); var y = Order(b); var bridge = primitive == Mesh.PrimitiveType.TriangleStrip ? 2 + x.Length % 2 : 0; d.Indices = new int[checked(x.Length + bridge + y.Length)]; x.CopyTo(d.Indices, 0);
            if (bridge != 0) { d.Indices[x.Length] = x[^1]; if (bridge == 3) d.Indices[x.Length + 1] = x[^1]; d.Indices[x.Length + bridge - 1] = checked(a.Vertices.Length + y[0]); }
            for (var i = 0; i < y.Length; i++) d.Indices[x.Length + bridge + i] = checked(a.Vertices.Length + y[i]);
        }
        d.Validate(primitive); return d;
    }
}
