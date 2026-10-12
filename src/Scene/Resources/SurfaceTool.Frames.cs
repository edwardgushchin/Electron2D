using MikkTSpaceSharp;

namespace Electron2D;

public sealed partial class SurfaceTool
{
    /// <summary>Assigns the normal captured by subsequent vertices.</summary>
    /// <param name="normal">Finite mesh-local numerical direction.</param>
    /// <remarks>Activate this channel before capturing the first vertex.</remarks>
    public void SetNormal(Vector3 normal)
    {
        lock (_gate) { Active(); if (!normal.IsFinite()) throw new ArgumentException("Normals must be finite.", nameof(normal)); CanActivate(Mesh.ArrayFormat.Normal); _current = _current with { Normal = normal }; _format |= Mesh.ArrayFormat.Normal; }
    }
    /// <summary>Assigns the tangent captured by subsequent vertices.</summary>
    /// <param name="tangent">Finite XYZ direction and W orientation.</param>
    /// <remarks>Activate this channel before capturing the first vertex.</remarks>
    public void SetTangent(Vector4 tangent)
    {
        lock (_gate) { Active(); if (!tangent.IsFinite()) throw new ArgumentException("Tangents must be finite.", nameof(tangent)); CanActivate(Mesh.ArrayFormat.Tangent); _current = _current with { Tangent = tangent }; _format |= Mesh.ArrayFormat.Tangent; }
    }
    /// <summary>Generates planar triangle normals, respecting captured smooth groups.</summary>
    /// <param name="flip">Reverses the clockwise-front convention when true.</param>
    /// <remarks>Flat group minus one retains each face's normal; other groups share equal positions.
    /// Indexed input expands before generation and is indexed again afterward, retaining attribute seams.
    /// Degenerate triangles contribute zero. Structural generation may allocate.</remarks>
    /// <exception cref="InvalidOperationException">The surface does not use triangle topology.</exception>
    /// <exception cref="ArgumentException">Triangle order is incomplete or contains invalid indices.</exception>
    public void GenerateNormals(bool flip = false)
    {
        lock (_gate)
        {
            Mutable(); if (_primitive != Mesh.PrimitiveType.Triangles) throw new InvalidOperationException("Normal generation requires triangle topology.");
            ValidateIndices(); var indexed = _indices.Count != 0; var count = indexed ? _indices.Count : _vertices.Count;
            if (count % 3 != 0) throw new ArgumentException("Normal generation requires complete triangle order.");
            var vertices = new List<Vertex>(count);
            if (indexed) foreach (var index in _indices) vertices.Add(_vertices[index]); else vertices.AddRange(_vertices);
            var sums = new Dictionary<(Vector2 Position, uint Group), long>(count);
            for (var i = 0; i < count; i += 3)
            {
                var normal = MeshFrameCodec.FaceNormal(vertices[i].Position, vertices[i + 1].Position, vertices[i + 2].Position, flip);
                for (var corner = 0; corner < 3; corner++)
                {
                    var vertex = vertices[i + corner];
                    if (vertex.Group == uint.MaxValue) vertices[i + corner] = vertex with { Normal = normal };
                    else { var key = (vertex.Position, vertex.Group); sums.TryGetValue(key, out var sum); sums[key] = sum + (long)normal.Z; }
                }
            }
            for (var i = 0; i < count; i++)
            {
                var vertex = vertices[i]; if (vertex.Group != uint.MaxValue) vertices[i] = vertex with { Normal = new(0, 0, Math.Sign(sums[(vertex.Position, vertex.Group)])) };
            }
            var indices = new List<int>();
            if (indexed)
            {
                var unique = new Dictionary<Vertex, int>(count); var compact = new List<Vertex>(count); indices.EnsureCapacity(count);
                foreach (var vertex in vertices) { if (!unique.TryGetValue(vertex, out var index)) { index = compact.Count; unique.Add(vertex, index); compact.Add(vertex); } indices.Add(index); }
                vertices = compact;
            }
            _vertices = vertices; _indices = indices; _format |= Mesh.ArrayFormat.Normal;
        }
    }
    /// <summary>Generates tangent directions from normal/UV channels using MikkTSpace.</summary>
    /// <remarks>Faces use successive triples of the current index or vertex order. Shared indexed vertices
    /// receive the last corresponding face-corner result; author duplicated seam vertices where needed.
    /// Generation preserves the input when validation or generation fails. Temporary native blocks are owned
    /// by the generator call and released after errors. Structural generation may allocate.</remarks>
    /// <exception cref="InvalidOperationException">Normals/primary UVs are missing or no face can be generated.</exception>
    public void GenerateTangents()
    {
        lock (_gate)
        {
            Mutable(); ValidateIndices();
            if ((_format & (Mesh.ArrayFormat.Normal | Mesh.ArrayFormat.TexUV)) != (Mesh.ArrayFormat.Normal | Mesh.ArrayFormat.TexUV)) throw new InvalidOperationException("Tangent generation requires normals and primary UVs.");
            var order = _indices.Count != 0 ? _indices.Count : _vertices.Count; var faces = order / 3;
            if (faces == 0) throw new InvalidOperationException("Tangent generation requires at least one complete face.");
            _ = checked(faces * 3 * 128);
            var tangents = new Vector4[_vertices.Count]; Array.Fill(tangents, new Vector4(0, 0, 0, 1)); var positions = RescaleExtreme(false); var coordinates = RescaleExtreme(true);
            int Index(int face, int corner) => _indices.Count == 0 ? face * 3 + corner : _indices[face * 3 + corner];
            var context = new MikkTSpace.SMikkTSpaceContext
            {
                m_getNumFaces = () => faces,
                m_getNumVerticesOfFace = _ => 3,
                m_getPosition = (face, corner) => { var index = Index(face, corner); var p = positions is null ? _vertices[index].Position : positions[index]; return new(p.X, p.Y, 0); },
                m_getNormal = (face, corner) => { var n = MeshFrameCodec.Unit(_vertices[Index(face, corner)].Normal); return new(n.X, n.Y, n.Z); },
                m_getTexCoord = (face, corner) => { var index = Index(face, corner); var uv = coordinates is null ? _vertices[index].UV : coordinates[index]; return new(uv.X, uv.Y); },
                m_setTSpace = (t, b, _, _, _, face, corner) =>
                {
                    var index = Index(face, corner); var n = MeshFrameCodec.Unit(_vertices[index].Normal); var direction = new Vector3(t.x, t.y, t.z); var binormal = new Vector3(-b.x, -b.y, -b.z);
                    tangents[index] = new(t.x, t.y, t.z, n.Dot(direction.Cross(binormal)) < 0 ? -1 : 1);
                }
            };
            if (!MikkTSpace.genTangSpaceDefault(context)) throw new InvalidOperationException("Tangent generation failed.");
            foreach (var tangent in tangents) if (!tangent.IsFinite()) throw new InvalidOperationException("Tangent generation produced a nonfinite result.");
            for (var i = 0; i < _vertices.Count; i++) _vertices[i] = _vertices[i] with { Tangent = tangents[i] };
            _format |= Mesh.ArrayFormat.Tangent;
        }
    }
    private Vector2[]? RescaleExtreme(bool uv)
    {
        double minX = double.PositiveInfinity, minY = double.PositiveInfinity, maxX = double.NegativeInfinity, maxY = double.NegativeInfinity;
        foreach (var vertex in _vertices) { var p = uv ? vertex.UV : vertex.Position; minX = Math.Min(minX, p.X); minY = Math.Min(minY, p.Y); maxX = Math.Max(maxX, p.X); maxY = Math.Max(maxY, p.Y); }
        var span = Math.Max(maxX - minX, maxY - minY); if (span == 0 || span is >= 1e-10 and <= 1e10) return null;
        var values = new Vector2[_vertices.Count];
        for (var i = 0; i < values.Length; i++) { var p = uv ? _vertices[i].UV : _vertices[i].Position; values[i] = new((float)((p.X - minX) / span), (float)((p.Y - minY) / span)); }
        return values;
    }

}
