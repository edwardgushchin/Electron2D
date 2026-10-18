namespace Electron2D;

/// <summary>Edits copied two-dimensional triangle surfaces and their vertex, edge and face connectivity.</summary>
/// <remarks>Imports and topology construction are cold allocations. Relations preserve discovery and corner order,
/// including repeated incidences in degenerate triangles. Scalar edits reuse storage. Metadata is runtime-only,
/// exactly typed and borrowed. Materials and source/target meshes are borrowed; commit preserves this editor.</remarks>
public sealed partial class MeshDataTool : ElectronObject
{
    private readonly object _gate = new();
    private MeshSurfaceData _data = new();
    private Mesh.ArrayFormat _format;
    private Material? _material;
    private readonly record struct Edge(int A, int B, int[] Faces);
    private readonly record struct Face(int A, int B, int C, int EdgeA, int EdgeB, int EdgeC);
    private Edge[] _edges = [];
    private Face[] _faces = [];
    private int[][] _vertexEdges = [], _vertexFaces = [];
    private object?[] _vertexMeta = [], _edgeMeta = [], _faceMeta = [];
    /// <summary>Creates an empty editor with no topology or material.</summary>
    public MeshDataTool() { }
    /// <summary>Clears copied geometry, topology, metadata and the borrowed material.</summary>
    public void Clear() { lock (_gate) { ThrowIfDisposed(); Reset(); } }
    /// <summary>Copies and builds connectivity for an existing complete triangle surface.</summary>
    /// <param name="mesh">Live source mesh; it is neither modified nor retained.</param>
    /// <param name="surface">Existing zero-based triangle surface index.</param>
    /// <remarks>Indexed and sequential triangles are accepted; source indices become explicit face order.
    /// All vertex channels and material are captured under one source storage gate. Failure preserves prior state.</remarks>
    /// <exception cref="ArgumentException">The topology is not triangles or the stored data is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The surface index is invalid.</exception>
    public void CreateFromSurface(ArrayMesh mesh, int surface)
    {
        lock (_gate)
        {
            ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(mesh); MeshSurfaceData data; Mesh.ArrayFormat format; Material? material;
            lock (mesh.Gate)
            {
                var source = mesh.Get(surface); if (source.Primitive != Mesh.PrimitiveType.Triangles) throw new ArgumentException("Topology editing requires triangles.", nameof(surface));
                if (source.Data.IsProcedural) throw new ArgumentException("Topology editing requires position geometry.", nameof(surface));
                data = source.Data.Copy(); format = source.Format; material = source.Material;
                if (material is { IsDisposed: true }) throw new ObjectDisposedException(nameof(material)); data.Validate(Mesh.PrimitiveType.Triangles);
            }
            if (data.Indices.Length == 0) { data.Indices = new int[data.Vertices.Length]; for (var i = 0; i < data.Indices.Length; i++) data.Indices[i] = i; }
            var edgeIDs = new Dictionary<(int, int), int>(); var endpoints = new List<(int A, int B)>(); var edgeFaces = new List<List<int>>(); var vertexEdges = new List<int>[data.Vertices.Length]; var vertexFaces = new List<int>[data.Vertices.Length];
            for (var i = 0; i < data.Vertices.Length; i++) { vertexEdges[i] = []; vertexFaces[i] = []; }
            var faces = new Face[data.Indices.Length / 3]; Span<int> triangleEdges = stackalloc int[3];
            for (var face = 0; face < faces.Length; face++)
            {
                for (var corner = 0; corner < 3; corner++)
                {
                    var a = data.Indices[face * 3 + corner]; var b = data.Indices[face * 3 + (corner + 1) % 3]; var key = (Math.Min(a, b), Math.Max(a, b));
                    if (!edgeIDs.TryGetValue(key, out var edge)) { edge = endpoints.Count; edgeIDs.Add(key, edge); endpoints.Add(key); edgeFaces.Add([]); vertexEdges[a].Add(edge); vertexEdges[b].Add(edge); }
                    triangleEdges[corner] = edge; edgeFaces[edge].Add(face); vertexFaces[a].Add(face);
                }
                faces[face] = new(data.Indices[face * 3], data.Indices[face * 3 + 1], data.Indices[face * 3 + 2], triangleEdges[0], triangleEdges[1], triangleEdges[2]);
            }
            var edges = new Edge[endpoints.Count]; for (var i = 0; i < edges.Length; i++) edges[i] = new(endpoints[i].A, endpoints[i].B, edgeFaces[i].ToArray());
            var ve = new int[vertexEdges.Length][]; var vf = new int[vertexFaces.Length][]; for (var i = 0; i < ve.Length; i++) { ve[i] = vertexEdges[i].ToArray(); vf[i] = vertexFaces[i].ToArray(); }
            var vertexMeta = new object?[data.Vertices.Length]; var edgeMeta = new object?[edges.Length]; var faceMeta = new object?[faces.Length];
            _data = data; _format = format; _material = material; _edges = edges; _faces = faces; _vertexEdges = ve; _vertexFaces = vf; _vertexMeta = vertexMeta; _edgeMeta = edgeMeta; _faceMeta = faceMeta;
        }
    }
    /// <summary>Appends a validated copied triangle surface with the current borrowed material.</summary>
    /// <param name="mesh">Live target mesh.</param>
    /// <param name="compressionFlags">Supported storage policies and matching channel flags.</param>
    /// <remarks>Current face order is committed as explicit indices. Source/editor state is preserved.
    /// Geometry and material publish together before one Changed; observer failure retains the committed surface.</remarks>
    /// <exception cref="ArgumentException">Skin channels are incomplete or data is invalid.</exception>
    /// <exception cref="InvalidOperationException">The editor is empty.</exception>
    /// <exception cref="NotSupportedException">Flags require an absent encoder or attribute consumer.</exception>
    public void CommitToSurface(ArrayMesh mesh, Mesh.ArrayFormat compressionFlags = Mesh.ArrayFormat.None)
    {
        lock (_gate)
        {
            ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(mesh); if (_faces.Length == 0) throw new InvalidOperationException("Import a triangle surface before committing.");
            mesh.AddSurface(Mesh.PrimitiveType.Triangles, _data, null, null, compressionFlags | (_format & Mesh.ArrayFormat.Use8BoneWeights), _material);
        }
    }
    /// <summary>Gets the copied channel/policy mask, including newly enabled channels.</summary>
    /// <returns>Zero after construction/Clear; indices may be implicit in the original import mask.</returns>
    public Mesh.ArrayFormat GetFormat() { lock (_gate) { ThrowIfDisposed(); return _format; } }
    /// <summary>Gets the number of copied vertices, including unreferenced vertices.</summary>
    /// <returns>A nonnegative count.</returns>
    public int GetVertexCount() { lock (_gate) { ThrowIfDisposed(); return _data.Vertices.Length; } }
    /// <summary>Gets the number of unique sorted vertex-pair edges.</summary>
    /// <returns>A nonnegative count; self-loop edges are retained.</returns>
    public int GetEdgeCount() { lock (_gate) { ThrowIfDisposed(); return _edges.Length; } }
    /// <summary>Gets the number of ordered triangle faces.</summary>
    /// <returns>A nonnegative count, including degenerate/repeated faces.</returns>
    public int GetFaceCount() { lock (_gate) { ThrowIfDisposed(); return _faces.Length; } }
    /// <summary>Gets a copied local vertex position.</summary>
    /// <param name="index">Existing vertex index.</param>
    /// <returns>Finite two-dimensional position.</returns>
    public Vector2 GetVertex(int index) { lock (_gate) { Vertex(index); return _data.Vertices[index]; } }
    /// <summary>Changes a finite local vertex position without altering connectivity.</summary>
    /// <param name="index">Existing vertex index.</param>
    /// <param name="vertex">Finite local position.</param>
    /// <exception cref="ArgumentException">A component is nonfinite.</exception>
    public void SetVertex(int index, Vector2 vertex) { lock (_gate) { Vertex(index); if (!vertex.IsFinite()) throw new ArgumentException("Position must be finite.", nameof(vertex)); _data.Vertices[index] = vertex; } }
    /// <summary>Gets the primary UV or zero if that channel is absent.</summary>
    /// <param name="index">Existing vertex index.</param>
    /// <returns>Finite primary coordinate.</returns>
    public Vector2 GetVertexUV(int index) { lock (_gate) { Vertex(index); return _data.UVs.Length == 0 ? default : _data.UVs[index]; } }
    /// <summary>Changes primary UV, enabling zero values on other vertices if needed.</summary>
    /// <param name="index">Existing vertex index.</param>
    /// <param name="uv">Finite coordinate.</param>
    public void SetVertexUV(int index, Vector2 uv) { lock (_gate) { Vertex(index); if (!uv.IsFinite()) throw new ArgumentException("UV must be finite.", nameof(uv)); if (_data.UVs.Length == 0) _data.UVs = new Vector2[_data.Vertices.Length]; _data.UVs[index] = uv; _format |= Mesh.ArrayFormat.TexUV; } }
    /// <summary>Returns the copied mesh-local normal value.</summary>
    /// <param name="index">Existing vertex index.</param>
    /// <returns>The normal, or zero without the channel.</returns>
    public Vector3 GetVertexNormal(int index) { lock (_gate) { Vertex(index); return _data.Normals.Length == 0 ? default : _data.Normals[index]; } }
    /// <summary>Assigns a finite mesh-local numerical normal vector.</summary>
    /// <param name="index">Existing vertex index.</param>
    /// <param name="normal">Finite direction; target commit quantizes it.</param>
    public void SetVertexNormal(int index, Vector3 normal) { lock (_gate) { Vertex(index); if (!normal.IsFinite()) throw new ArgumentException("Normals must be finite.", nameof(normal)); if (_data.Normals.Length == 0) _data.Normals = new Vector3[_data.Vertices.Length]; _data.Normals[index] = normal; _format |= Mesh.ArrayFormat.Normal; } }
    /// <summary>Returns the copied tangent direction and orientation.</summary>
    /// <param name="index">Existing vertex index.</param>
    /// <returns>XYZ direction and W orientation, or zero without the channel.</returns>
    public Vector4 GetVertexTangent(int index) { lock (_gate) { Vertex(index); return _data.Tangents.Length == 0 ? default : _data.Tangents[index]; } }
    /// <summary>Assigns a finite tangent direction and orientation.</summary>
    /// <param name="index">Existing vertex index.</param>
    /// <param name="tangent">Finite XYZ direction and W orientation; target commit quantizes them.</param>
    public void SetVertexTangent(int index, Vector4 tangent) { lock (_gate) { Vertex(index); if (!tangent.IsFinite()) throw new ArgumentException("Tangents must be finite.", nameof(tangent)); if (_data.Tangents.Length == 0) _data.Tangents = new Vector4[_data.Vertices.Length]; _data.Tangents[index] = tangent; _format |= Mesh.ArrayFormat.Tangent; } }
    /// <summary>Gets secondary UV or zero if that channel is absent.</summary>
    /// <param name="index">Existing vertex index.</param>
    /// <returns>Finite secondary coordinate.</returns>
    public Vector2 GetVertexUV2(int index) { lock (_gate) { Vertex(index); return _data.UV2s.Length == 0 ? default : _data.UV2s[index]; } }
    /// <summary>Changes secondary UV, enabling zero values on other vertices if needed.</summary>
    /// <param name="index">Existing vertex index.</param>
    /// <param name="uv2">Finite coordinate.</param>
    public void SetVertexUV2(int index, Vector2 uv2) { lock (_gate) { Vertex(index); if (!uv2.IsFinite()) throw new ArgumentException("UV2 must be finite.", nameof(uv2)); if (_data.UV2s.Length == 0) _data.UV2s = new Vector2[_data.Vertices.Length]; _data.UV2s[index] = uv2; _format |= Mesh.ArrayFormat.TexUV2; } }
    /// <summary>Gets the authored color or white when no color channel exists.</summary>
    /// <param name="index">Existing vertex index.</param>
    /// <returns>The current finite color, before target mesh quantization.</returns>
    public Color GetVertexColor(int index) { lock (_gate) { Vertex(index); return _data.Colors.Length == 0 ? Colors.White : _data.Colors[index]; } }
    /// <summary>Changes a finite color, preserving white on other vertices when enabling the channel.</summary>
    /// <param name="index">Existing vertex index.</param>
    /// <param name="color">Finite color; target commit applies RGBA8 precision.</param>
    public void SetVertexColor(int index, Color color)
    {
        lock (_gate) { Vertex(index); if (!color.IsFinite()) throw new ArgumentException("Color must be finite.", nameof(color)); if (_data.Colors.Length == 0) { var colors = new Color[_data.Vertices.Length]; Array.Fill(colors, Colors.White); _data.Colors = colors; } _data.Colors[index] = color; _format |= Mesh.ArrayFormat.Color; }
    }
    /// <summary>Gets an independent four/eight influence index array, empty without the channel.</summary>
    /// <param name="index">Existing vertex index.</param>
    /// <returns>Caller-owned indices.</returns>
    public int[] GetVertexBones(int index) { lock (_gate) { Vertex(index); return _data.Bones.Length == 0 ? [] : _data.Bones.AsSpan(index * SkinSlots, SkinSlots).ToArray(); } }
    /// <summary>Changes exactly four/eight stored bone indices for one vertex.</summary>
    /// <param name="index">Existing vertex index.</param>
    /// <param name="bones">The imported slot count, or four for an unskinned surface; indices zero through 65535.</param>
    /// <remarks>Enabling one skin channel leaves the other absent until authored. Commit rejects incomplete pairs.</remarks>
    public void SetVertexBones(int index, ReadOnlySpan<int> bones)
    {
        lock (_gate) { Vertex(index); if (bones.Length != SkinSlots) throw new ArgumentException("Bone count must match stored skin width.", nameof(bones)); foreach (var bone in bones) if ((uint)bone > 65535) throw new ArgumentOutOfRangeException(nameof(bones)); if (_data.Bones.Length == 0) _data.Bones = new int[checked(_data.Vertices.Length * SkinSlots)]; bones.CopyTo(_data.Bones.AsSpan(index * SkinSlots)); _format |= Mesh.ArrayFormat.Bones; }
    }
    /// <summary>Gets independent current four/eight weights, empty without the channel.</summary>
    /// <param name="index">Existing vertex index.</param>
    /// <returns>Caller-owned finite weights, before target UNORM16 packing.</returns>
    public float[] GetVertexWeights(int index) { lock (_gate) { Vertex(index); return _data.Weights.Length == 0 ? [] : _data.Weights.AsSpan(index * SkinSlots, SkinSlots).ToArray(); } }
    /// <summary>Changes exactly four/eight finite weights for one vertex without normalizing their sum.</summary>
    /// <param name="index">Existing vertex index.</param>
    /// <param name="weights">The imported slot count, or four for an unskinned surface.</param>
    /// <remarks>Commit clamps/packs to UNORM16 and requires matching bones on all vertices.</remarks>
    public void SetVertexWeights(int index, ReadOnlySpan<float> weights)
    {
        lock (_gate) { Vertex(index); if (weights.Length != SkinSlots) throw new ArgumentException("Weight count must match stored skin width.", nameof(weights)); foreach (var weight in weights) if (!float.IsFinite(weight)) throw new ArgumentException("Weights must be finite.", nameof(weights)); if (_data.Weights.Length == 0) _data.Weights = new float[checked(_data.Vertices.Length * SkinSlots)]; weights.CopyTo(_data.Weights.AsSpan(index * SkinSlots)); _format |= Mesh.ArrayFormat.Weights; }
    }
    /// <summary>Gets independently copied incident edge IDs in discovery order.</summary>
    /// <param name="index">Existing vertex index.</param>
    /// <returns>Caller-owned IDs; a self-loop contributes its two endpoint incidences.</returns>
    public int[] GetVertexEdges(int index) { lock (_gate) { Vertex(index); return (int[])_vertexEdges[index].Clone(); } }
    /// <summary>Gets independently copied incident face IDs in corner traversal order.</summary>
    /// <param name="index">Existing vertex index.</param>
    /// <returns>Caller-owned IDs; repeated triangle corners retain multiplicity.</returns>
    public int[] GetVertexFaces(int index) { lock (_gate) { Vertex(index); return (int[])_vertexFaces[index].Clone(); } }
    /// <summary>Gets one of an edge's sorted vertex endpoints.</summary>
    /// <param name="index">Existing edge index.</param>
    /// <param name="vertex">Zero or one.</param>
    /// <returns>The vertex ID; endpoints may be equal for a self-loop.</returns>
    public int GetEdgeVertex(int index, int vertex) { lock (_gate) { EdgeAt(index); Corner(vertex, 2); return vertex == 0 ? _edges[index].A : _edges[index].B; } }
    /// <summary>Gets copied incident face IDs for a boundary, shared, nonmanifold or repeated edge.</summary>
    /// <param name="index">Existing edge index.</param>
    /// <returns>Caller-owned IDs in face/corner discovery order.</returns>
    public int[] GetEdgeFaces(int index) { lock (_gate) { EdgeAt(index); return (int[])_edges[index].Faces.Clone(); } }
    /// <summary>Gets a face's vertex ID in authored winding order.</summary>
    /// <param name="index">Existing face index.</param>
    /// <param name="vertex">Corner zero through two.</param>
    /// <returns>The ordered vertex ID.</returns>
    public int GetFaceVertex(int index, int vertex) { lock (_gate) { FaceAt(index); Corner(vertex, 3); var face = _faces[index]; return vertex == 0 ? face.A : vertex == 1 ? face.B : face.C; } }
    /// <summary>Gets the edge between a face corner and its next corner.</summary>
    /// <param name="index">Existing face index.</param>
    /// <param name="edge">Corner zero through two.</param>
    /// <returns>The edge ID in authored corner order.</returns>
    public int GetFaceEdge(int index, int edge) { lock (_gate) { FaceAt(index); Corner(edge, 3); var face = _faces[index]; return edge == 0 ? face.EdgeA : edge == 1 ? face.EdgeB : face.EdgeC; } }
    /// <summary>Computes a face's current numeric winding normal in the XY plane.</summary>
    /// <param name="index">Existing face index.</param>
    /// <returns>Positive/negative unit Z for counterclockwise/clockwise XY order, zero for a degenerate face.</returns>
    /// <remarks>Uses widened differences/products and actual edited positions; this does not enable a vertex normal channel.</remarks>
    public Vector3 GetFaceNormal(int index)
    {
        lock (_gate) { FaceAt(index); var face = _faces[index]; var a = _data.Vertices[face.A]; var b = _data.Vertices[face.B]; var c = _data.Vertices[face.C]; return MeshFrameCodec.FaceNormal(a, b, c); }
    }
    /// <summary>Gets the current borrowed surface material.</summary>
    /// <returns>Material, or null for the drawing item's material.</returns>
    public Material? GetMaterial() { lock (_gate) { ThrowIfDisposed(); return _material; } }
    /// <summary>Assigns a borrowed material without modifying source or target meshes.</summary>
    /// <param name="material">Live material, or null for inherited drawing.</param>
    public void SetMaterial(Material? material) { lock (_gate) { ThrowIfDisposed(); if (material is { IsDisposed: true }) throw new ObjectDisposedException(nameof(material)); _material = material; } }
    private int SkinSlots => (_format & Mesh.ArrayFormat.Use8BoneWeights) != 0 ? 8 : 4;
    private void Vertex(int index) { ThrowIfDisposed(); if ((uint)index >= (uint)_data.Vertices.Length) throw new ArgumentOutOfRangeException(nameof(index)); }
    private void EdgeAt(int index) { ThrowIfDisposed(); if ((uint)index >= (uint)_edges.Length) throw new ArgumentOutOfRangeException(nameof(index)); }
    private void FaceAt(int index) { ThrowIfDisposed(); if ((uint)index >= (uint)_faces.Length) throw new ArgumentOutOfRangeException(nameof(index)); }
    private static void Corner(int index, int count) { if ((uint)index >= (uint)count) throw new ArgumentOutOfRangeException(nameof(index)); }
    private void Reset() { _data = new(); _format = 0; _material = null; _edges = []; _faces = []; _vertexEdges = []; _vertexFaces = []; _vertexMeta = []; _edgeMeta = []; _faceMeta = []; }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { lock (_gate) Reset(); base.Dispose(disposing); }
}
