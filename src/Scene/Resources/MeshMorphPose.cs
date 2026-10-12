namespace Electron2D;

internal sealed class MeshMorphPose : IDisposable
{
    private readonly record struct Source(MeshSurfaceData Basis, uint[] FrameWords, MeshSurfaceData[] Shapes, Mesh.ArrayFormat Mask, Mesh.PrimitiveType Primitive, Material? Material, string Name, MeshLOD[] LODs, MeshCompression? Compression);
    private Source[] _sources = [];
    private int _count;
    private Mesh? _source;
    private long _revision = -1;
    private ArrayMesh? _output;
    internal bool IsPreparing { get; private set; }
    internal ArrayMesh Prepare(Mesh source, float[] weights, int candidate = -1, float value = 0)
    {
        if (IsPreparing) throw new InvalidOperationException("Morph preparation callbacks cannot reenter their snapshot.");
        IsPreparing = true;
        try
        {
            var arrays = source as ArrayMesh ?? (source as ImmediateMesh)?.Surfaces;
            if (arrays is not null) { lock (arrays.Gate) return PrepareCore(source, arrays, weights, candidate, value); }
            return PrepareCore(source, null, weights, candidate, value);
        }
        finally { IsPreparing = false; }
    }
    private ArrayMesh PrepareCore(Mesh source, ArrayMesh? arrays, float[] weights, int candidate, float value)
    {
        ObjectDisposedException.ThrowIf(source.IsDisposed, source); var revision = source.ChangeRevision;
        var refresh = !ReferenceEquals(source, _source) || revision != _revision;
        if (!refresh && candidate < 0) return _output!;
        if (refresh)
        {
            var count = arrays?.SurfaceCount ?? source.GetSurfaceCount(); if (count < 0) throw new InvalidOperationException("Surface count must be nonnegative.");
            if (_sources.Length < count) Array.Resize(ref _sources, count);
            var shapeCount = source.GetBlendShapeCount(); if (shapeCount != weights.Length) throw new ArgumentException("Weight count must match named blend shapes.");
            for (var i = 0; i < count; i++)
            {
                if (arrays is not null) { var s = arrays.Get(i); _sources[i] = new(s.Data, s.FrameWords, s.BlendShapes, s.BlendMask, s.Primitive, s.Material, s.Name, s.LODs, s.Compression); }
                else
                {
                    var data = source.SurfaceGetArrays(i).Copy(); var shapes = source.SurfaceGetBlendShapeArrays(i).Select(shape => shape.Copy()).ToArray(); var primitive = source.SurfaceGetPrimitiveType(i); data.Validate(primitive);
                    var mask = Mesh.ArrayFormat.Vertex | (data.Normals.Length != 0 ? Mesh.ArrayFormat.Normal : 0) | (data.Tangents.Length != 0 ? Mesh.ArrayFormat.Tangent : 0);
                    if (shapes.Length != shapeCount) throw new ArgumentException("Surface shape count must match named shapes."); foreach (var shape in shapes) ArrayMesh.ValidateBlendShape(shape, data, mask);
                    data.QuantizeSkin(); var compression = (source.SurfaceGetFormat(i) & Mesh.ArrayFormat.CompressAttributes) != 0 ? new MeshCompression(data) : null; var words = compression is null ? MeshFrameCodec.Capture(data) : []; data.PrepareCustoms(); _sources[i] = new(data, words, shapes, mask, primitive, source.SurfaceGetMaterial(i), "", MeshLOD.Capture(source.SurfaceGetLODs(i), data, primitive), compression);
                }
            }
            if (source.IsDisposed || source.ChangeRevision != revision) throw new InvalidOperationException("Morph source callbacks must retain a stable live revision.");
            Array.Clear(_sources, count, _sources.Length - count); _count = count;
        }
        var mode = arrays?.BlendShapeMode ?? BlendShapeMode.Relative;
        double sum = 0; for (var i = 0; i < weights.Length; i++) sum += Effective(i == candidate ? value : weights[i]);
        var basisWeight = mode == BlendShapeMode.Normalized ? 1 - sum : 1;
        var first = true; Vector2 minimum = default, maximum = default;
        for (var surface = 0; surface < _count; surface++)
            for (var vertex = 0; vertex < _sources[surface].Basis.Vertices.Length; vertex++)
            {
                Position(_sources[surface], vertex, weights, candidate, value, basisWeight, out var point);
                if (first) { minimum = maximum = point; first = false; } else { minimum = minimum.Min(point); maximum = maximum.Max(point); }
            }
        if (!(maximum - minimum).IsFinite()) throw new ArgumentException("Morph output exceeds finite local bounds.", nameof(weights));
        var rebuild = _output is null || _output.SurfaceCount != _count;
        if (!rebuild) for (var i = 0; i < _count; i++) if (!SameLayout(_output!.Get(i), _sources[i])) { rebuild = true; break; }
        ArrayMesh? next = null;
        try
        {
            if (rebuild)
            {
                next = new();
                for (var i = 0; i < _count; i++) { var s = _sources[i]; next.AddSurface(s.Primitive, s.Basis, null, MeshLOD.Copy(s.LODs), (s.Basis.SkinSlots == 8 ? Mesh.ArrayFormat.Use8BoneWeights : 0) | (s.Compression is null ? 0 : Mesh.ArrayFormat.CompressAttributes), s.Material, s.Compression); }
            }
            var output = next ?? _output!;
            for (var surface = 0; surface < _count; surface++)
            {
                var s = _sources[surface]; var target = output.Get(surface); CopyAttributes(s.Basis, target.Data); target.Material = s.Material; target.Name = s.Name;
                s.FrameWords.CopyTo(target.FrameWords, 0); if (s.Compression is { } encoded) { encoded.Frames.CopyTo(target.Compression!.Frames, 0); encoded.UVs.CopyTo(target.Compression.UVs, 0); encoded.UV2s.CopyTo(target.Compression.UV2s, 0); }
                s.Basis.Normals.CopyTo(target.Data.Normals, 0); s.Basis.Tangents.CopyTo(target.Data.Tangents, 0);
                for (var vertex = 0; vertex < s.Basis.Vertices.Length; vertex++) Position(s, vertex, weights, candidate, value, basisWeight, out target.Data.Vertices[vertex]);
            }
            if (next is not null) { _output?.Dispose(); _output = next; next = null; }
            _source = source; _revision = revision; output.EmitChanged(); return output;
        }
        finally { next?.Dispose(); }
    }
    private static bool SameLayout(ArrayMesh.Surface target, Source source)
    {
        var a = target.Data; var b = source.Basis;
        return target.Primitive == source.Primitive && a.Vertices.Length == b.Vertices.Length && a.Normals.Length == b.Normals.Length && a.Tangents.Length == b.Tangents.Length && a.Colors.Length == b.Colors.Length && a.UVs.Length == b.UVs.Length && a.UV2s.Length == b.UV2s.Length && a.Indices.Length == b.Indices.Length && a.Bones.Length == b.Bones.Length && a.Weights.Length == b.Weights.Length && CustomMatches(a.Custom0, b.Custom0) && CustomMatches(a.Custom1, b.Custom1) && CustomMatches(a.Custom2, b.Custom2) && CustomMatches(a.Custom3, b.Custom3) && SameLODs(target.LODs, source.LODs) && (target.Compression is null ? source.Compression is null : source.Compression is not null && target.Compression.UVScale == source.Compression.UVScale);
    }
    private static bool SameLODs(MeshLOD[] a, MeshLOD[] b)
    {
        if (a.Length != b.Length) return false;
        for (var i = 0; i < a.Length; i++) if (a[i].Error != b[i].Error || !a[i].Data.Indices.AsSpan().SequenceEqual(b[i].Data.Indices)) return false;
        return true;
    }
    private static bool CustomMatches(MeshCustomData? a, MeshCustomData? b) => a is null ? b is null : b is not null && a.Format == b.Format && a.Data.Length == b.Data.Length;
    private static void CopyAttributes(MeshSurfaceData source, MeshSurfaceData target)
    {
        source.Colors.CopyTo(target.Colors, 0); source.UVs.CopyTo(target.UVs, 0); source.UV2s.CopyTo(target.UV2s, 0); source.Indices.CopyTo(target.Indices, 0); source.Bones.CopyTo(target.Bones, 0); source.Weights.CopyTo(target.Weights, 0);
        for (var i = 0; i < 4; i++) if (source.Custom(i) is { } custom) { custom.Data.CopyTo(target.Custom(i)!.Data, 0); custom.Values.CopyTo(target.Custom(i)!.Values, 0); }
    }
    private static void Position(Source source, int vertex, float[] weights, int candidate, float value, double basisWeight, out Vector2 point)
    {
        var basis = source.Basis.Vertices[vertex]; double x = basis.X * basisWeight, y = basis.Y * basisWeight;
        for (var i = 0; i < weights.Length; i++) { var weight = Effective(i == candidate ? value : weights[i]); if (weight == 0) continue; var target = source.Shapes[i].Vertices[vertex]; x += target.X * (double)weight; y += target.Y * (double)weight; }
        point = new((float)x, (float)y); if (!point.IsFinite()) throw new ArgumentException("Morph positions must remain finite.", nameof(weights));
    }
    private static double Effective(float value) => Math.Abs(value) > .0001f ? value : 0;
    public void Dispose() { _output?.Dispose(); _output = null; _source = null; _sources = []; _count = 0; _revision = -1; }
}
