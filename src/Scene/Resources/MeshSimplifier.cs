using System.Runtime.InteropServices;

namespace Electron2D;

internal static partial class MeshSimplifier
{
    [LibraryImport(NativeLibraries.MeshLibrary, EntryPoint = "e2d_mesh_scale")]
    private static unsafe partial float Scale(float* positions, nuint count);
    [LibraryImport(NativeLibraries.MeshLibrary, EntryPoint = "e2d_mesh_simplify")]
    private static unsafe partial nuint Simplify(int* output, int* indices, nuint count, float* positions, nuint vertices, float* attributes, float* weights, nuint attributeCount, nuint target, uint options, float* error);
    internal static unsafe MeshLOD[] Generate(MeshSurfaceData data, MeshSurfaceData[] shapes, Mesh.PrimitiveType primitive, MeshLOD[] old, float mergeAngle, IReadOnlyList<Transform>? boneTransforms)
    {
        if (primitive != Mesh.PrimitiveType.Triangles || data.IsProcedural) return old;
        if (data.Indices.Length == 0) return [];
        var count = data.Vertices.Length; var points = (Vector2[])data.Vertices.Clone();
        if (boneTransforms is { Count: > 0 } && data.Bones.Length != 0)
        {
            var slots = data.SkinSlots;
            for (var vertex = 0; vertex < count; vertex++)
            {
                double x = 0, y = 0;
                for (var slot = 0; slot < slots; slot++) { var index = vertex * slots + slot; var bone = data.Bones[index]; if ((uint)bone >= (uint)boneTransforms.Count) throw new ArgumentOutOfRangeException(nameof(boneTransforms)); var weight = Math.Clamp(data.Weights[index], 0, 1); if (weight == 0) continue; var t = boneTransforms[bone]; var p = data.Vertices[vertex]; x += ((double)t.X.X * p.X + (double)t.Y.X * p.Y + t.Origin.X) * weight; y += ((double)t.X.Y * p.X + (double)t.Y.Y * p.Y + t.Origin.Y) * weight; }
                points[vertex] = new((float)x, (float)y); if (!points[vertex].IsFinite()) throw new ArgumentException("Skin pose exceeds finite simplification coordinates.", nameof(boneTransforms));
            }
        }
        if (data.Indices.Length <= 24) return [];
        var normals = new Vector3[count];
        if (data.Normals.Length != 0) for (var i = 0; i < count; i++) normals[i] = MeshFrameCodec.Unit(data.Normals[i]);
        else
        {
            for (var i = 0; i < data.Indices.Length; i += 3) { var a = data.Indices[i]; var b = data.Indices[i + 1]; var c = data.Indices[i + 2]; var normal = MeshFrameCodec.FaceNormal(points[a], points[b], points[c]); normals[a] += normal; normals[b] += normal; normals[c] += normal; }
            for (var i = 0; i < count; i++) normals[i] = MeshFrameCodec.Unit(normals[i]);
        }
        var threshold = Math.Cos(mergeAngle * (Math.PI / 180));
        var groups = new Dictionary<Vector2, List<int>>(); var remap = new int[count]; var inverse = new List<int>(); var mergedNormals = new List<Vector3>(); var samples = new List<int>();
        for (var i = 0; i < count; i++)
        {
            if (!groups.TryGetValue(points[i], out var candidates)) { candidates = []; groups.Add(points[i], candidates); }
            var selected = -1;
            foreach (var candidate in candidates) if (Equivalent(data, shapes, normals, i, inverse[candidate], threshold)) { selected = candidate; break; }
            if (selected < 0) { selected = inverse.Count; candidates.Add(selected); inverse.Add(i); mergedNormals.Add(normals[i]); samples.Add(1); }
            else { mergedNormals[selected] += normals[i]; samples[selected]++; }
            remap[i] = selected;
        }
        var vertices = inverse.Count; var positions = new float[checked(vertices * 3)]; var attributes = new float[checked(vertices * 6)]; var current = new int[data.Indices.Length];
        var minimum = points[inverse[0]]; var maximum = minimum;
        for (var i = 0; i < vertices; i++)
        {
            var point = points[inverse[i]]; minimum = minimum.Min(point); maximum = maximum.Max(point); positions[i * 3] = point.X; positions[i * 3 + 1] = point.Y;
            var normal = mergedNormals[i] / samples[i]; attributes[i * 6] = normal.X; attributes[i * 6 + 1] = normal.Y; attributes[i * 6 + 2] = normal.Z;
            if (data.Colors.Length != 0) { var color = data.Colors[inverse[i]]; attributes[i * 6 + 3] = Math.Clamp(color.R, 0, 1); attributes[i * 6 + 4] = Math.Clamp(color.G, 0, 1); attributes[i * 6 + 5] = Math.Clamp(color.B, 0, 1); }
        }
        if (!(maximum - minimum).IsFinite()) throw new ArgumentException("Simplification bounds must remain finite.");
        for (var i = 0; i < current.Length; i++) current[i] = remap[data.Indices[i]];
        float[] weights = [1, 1, 1, data.Colors.Length == 0 ? 0 : 1, data.Colors.Length == 0 ? 0 : 1, data.Colors.Length == 0 ? 0 : 1];
        var levels = new Dictionary<float, int[]>(); var error = 0f; var previous = 0f; var prune = true;
        fixed (float* p = positions, a = attributes, w = weights)
        {
            var scale = Scale(p, (nuint)vertices); if (!float.IsFinite(scale)) throw new ArgumentException("Simplifier scale must remain finite."); if (scale <= 0) return [];
            while (current.Length > 24)
            {
                var target = Math.Max(current.Length / 3 / 2 * 3, 12); var output = new int[current.Length]; var step = 0f; var options = 1u | 2u | (prune ? 8u : 0) | (shapes.Length != 0 || data.Bones.Length != 0 ? 16u : 0);
                nuint length; fixed (int* source = current, destination = output) length = Simplify(destination, source, (nuint)current.Length, p, (nuint)vertices, a, w, 6, (nuint)target, options, &step);
                if (length == nuint.MaxValue) throw new OutOfMemoryException("Native mesh simplification allocation failed.");
                if (length > (nuint)output.Length || length % 3 != 0 || !float.IsFinite(step) || step < 0) throw new InvalidOperationException("The native simplifier returned malformed geometry/error.");
                if (length == 0 && prune) { prune = false; continue; }
                if (length == 0 || length >= current.Length * .75) break;
                error = Math.Max(error * 1.5f, step); if (error > 1) break;
                Array.Resize(ref output, (int)length); current = output; var order = new int[output.Length]; for (var i = 0; i < order.Length; i++) { if ((uint)output[i] >= (uint)inverse.Count) throw new InvalidOperationException("The native simplifier returned an invalid vertex index."); order[i] = inverse[output[i]]; }
                var distance = Math.Max(error * scale, .0000000001f); if (distance <= previous) distance = MathF.BitIncrement(previous); if (!float.IsFinite(distance)) throw new ArgumentException("LOD threshold overflows."); levels.Add(distance, order); previous = distance;
            }
        }
        return MeshLOD.Capture(levels, data, primitive);
    }
    private static bool Equivalent(MeshSurfaceData data, MeshSurfaceData[] shapes, Vector3[] normals, int a, int b, double threshold)
    {
        if (normals[a].Dot(normals[b]) <= threshold || data.UVs.Length != 0 && data.UVs[a].DistanceSquaredTo(data.UVs[b]) >= .0000000001f || data.UV2s.Length != 0 && data.UV2s[a].DistanceSquaredTo(data.UV2s[b]) >= .0000000001f || data.Tangents.Length != 0 && (data.Tangents[a].W < 0) != (data.Tangents[b].W < 0) || data.Colors.Length != 0 && !data.Colors[a].IsEqualApprox(data.Colors[b])) return false;
        for (var channel = 0; channel < 4; channel++) if (data.Custom(channel) is { } custom) { var stride = MeshCustomCodec.Stride(custom.Format); if (!custom.Data.AsSpan(a * stride, stride).SequenceEqual(custom.Data.AsSpan(b * stride, stride))) return false; }
        for (var slot = 0; slot < data.SkinSlots; slot++) if (data.Bones[a * data.SkinSlots + slot] != data.Bones[b * data.SkinSlots + slot] || data.Weights[a * data.SkinSlots + slot] != data.Weights[b * data.SkinSlots + slot]) return false;
        foreach (var shape in shapes) if (shape.Vertices[a] != shape.Vertices[b] || shape.Normals.Length != 0 && shape.Normals[a] != shape.Normals[b] || shape.Tangents.Length != 0 && shape.Tangents[a] != shape.Tangents[b]) return false;
        return true;
    }
}
