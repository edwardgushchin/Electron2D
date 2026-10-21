namespace Electron2D;

internal sealed partial class CanvasMesh
{
    private readonly List<ParticleGeometry> _particleGeometry = [];
    internal void AppendParticles(List<CanvasVertex> vertices, List<CanvasBatch> batches, RenderingParticleState source, Texture? texture,
        Transform canvas, Rect2 visibility, Color modulation, MaterialState? inheritedMaterial, BlendMode inheritedBlend, TextureFilter filter, TextureRepeat repeat,
        int anisotropy, Rect2i? clip, bool snap, bool linear, Vector4 sheet, CanvasItem owner, Texture? textureSource = null)
    {
        ObjectDisposedException.ThrowIf(Mesh.IsDisposed, Mesh);
        var arrays = Mesh as ArrayMesh ?? (Mesh as ImmediateMesh)?.Surfaces;
        if (arrays is not null)
        {
            lock (arrays.Gate)
                for (var surface = 0; surface < arrays.SurfaceCount; surface++)
                { var data = arrays.Get(surface); Append(data.Data, data.Primitive, data.Material, surface); }
        }
        else
        {
            PrepareCustom();
            for (var surface = 0; surface < _customCount; surface++)
            { var data = _custom[surface]; Append(data.Data, data.Primitive, data.Material, surface); }
        }
        void Append(MeshSurfaceData data, Mesh.PrimitiveType primitive, Material? material, int surface)
        {
            var count = data.Indices.Length == 0 ? data.VertexCount : data.Indices.Length;
            var vertexCount = data.VertexCount;
            if (vertexCount == 0 && data.Indices.Length != 0) vertexCount = data.Indices.Max() + 1;
            if (count == 0) return;
            if (source.Trails && source.BindPoses.Length > 1) foreach (var bone in data.Bones) if ((uint)bone >= (uint)source.BindPoses.Length) throw new InvalidOperationException("A particle trail bone does not resolve to a bind pose.");
            while (_particleGeometry.Count <= surface) _particleGeometry.Add(new());
            var geometry = _particleGeometry[surface]; geometry.Set(data, Mesh.ChangeRevision, vertexCount);
            var first = vertices.Count;
            if (primitive == Mesh.PrimitiveType.TriangleStrip)
            {
                for (var i = 0; i + 2 < count; i++) { Add(i + i % 2); Add(i + 1 - i % 2); Add(i + 2); }
                primitive = Mesh.PrimitiveType.Triangles;
            }
            else for (var i = 0; i < count; i++) Add(i);
            if (vertices.Count == first) return;
            var batch = Batch(first, vertices.Count - first, material, inheritedMaterial, inheritedBlend, texture, filter, repeat, anisotropy, clip, owner,
                linear: linear, visibility: visibility, procedural: data.IsProcedural, primitive: primitive, snap: snap, textureSource: textureSource);
            ApplyGlyph(vertices, batch);
            batches.Add(batch with { Particles = new(source, geometry, canvas, modulation, sheet), SnapGeneratedVertices = snap });
            void Add(int index)
            {
                var original = data.Indices.Length == 0 ? index : data.Indices[index];
                vertices.Add(VertexIndex(data, original, Transform.Identity, Colors.White, false, default, default, Transform.Identity, Transform.Identity, linear));
            }
        }
    }
}
