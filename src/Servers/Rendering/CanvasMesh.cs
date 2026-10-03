namespace Electron2D;

internal sealed class CanvasMesh(Mesh mesh, Transform local, Color modulate)
{
    internal void Set(Mesh value, Transform pose, Color tint) { if (!ReferenceEquals(mesh, value)) { _revision = -1; _customCount = 0; Array.Clear(_custom); } mesh = value; local = pose; modulate = tint; }
    private (MeshSurfaceData Data, Mesh.PrimitiveType Primitive, Material? Material)[] _custom = [];
    private long _revision = -1;
    private int _customCount;
    internal Mesh Mesh => mesh;
    internal void Clear() { mesh = null!; _revision = -1; _customCount = 0; Array.Clear(_custom); }

    internal void Append(List<CanvasVertex> vertices, List<CanvasBatch> batches, Texture? texture, Transform transform, Color color,
        MaterialState? inheritedMaterial, BlendMode inheritedBlend, TextureFilter filter, TextureRepeat repeat, int anisotropy, Rect2i? clip, bool snap, MultiMesh? instances = null, float fraction = 1)
    {
        ObjectDisposedException.ThrowIf(mesh.IsDisposed, mesh);
        transform *= local; color *= modulate;
        var arrays = mesh as ArrayMesh ?? (mesh as ImmediateMesh)?.Surfaces;
        if (arrays is not null)
        {
            lock (arrays.Gate)
            {
                for (var i = 0; i < arrays.SurfaceCount; i++)
                {
                    var surface = arrays.Get(i); var first = vertices.Count;
                    AppendSurfaceInstances(vertices, surface.Data, surface.Primitive, transform, color, snap, instances, fraction);
                    AddBatch(batches, first, vertices.Count - first, surface.Material, inheritedMaterial, inheritedBlend, texture, filter, repeat, anisotropy, clip);
                }
            }
            return;
        }
        PrepareCustom();
        for (var i = 0; i < _customCount; i++)
        {
            var first = vertices.Count; var surface = _custom[i]; AppendSurfaceInstances(vertices, surface.Data, surface.Primitive, transform, color, snap, instances, fraction);
            AddBatch(batches, first, vertices.Count - first, surface.Material, inheritedMaterial, inheritedBlend, texture, filter, repeat, anisotropy, clip);
        }
    }
    private bool _preparing;
    private void PrepareCustom()
    {
        if (_preparing) throw new InvalidOperationException("Mesh preparation callbacks cannot reenter their snapshot.");
        _preparing = true;
        try
        {
            var revision = mesh.ChangeRevision;
            if (_revision != revision)
            {
                var count = mesh.GetSurfaceCount(); if (_custom.Length < count) Array.Resize(ref _custom, count);
                for (var i = 0; i < count; i++)
                {
                    var data = mesh.SurfaceGetArrays(i); var primitive = mesh.SurfaceGetPrimitiveType(i); data.Validate(primitive);
                    _custom[i] = (data, primitive, mesh.SurfaceGetMaterial(i));
                }
                ObjectDisposedException.ThrowIf(mesh.IsDisposed, mesh);
                if (mesh.ChangeRevision != revision) throw new InvalidOperationException("Mesh callbacks changed the resource while preparing its surfaces.");
                Array.Clear(_custom, count, _custom.Length - count);
                _customCount = count;
                _revision = revision;
            }
        }
        finally { _preparing = false; }
    }
    internal Rect2 GetLocalBounds()
    {
        ObjectDisposedException.ThrowIf(mesh.IsDisposed, mesh); var first = true; var minimum = Vector2.Zero; var maximum = Vector2.Zero;
        void Add(MeshSurfaceData data)
        {
            foreach (var vertex in data.Vertices) { if (first) { minimum = maximum = vertex; first = false; } else { minimum = minimum.Min(vertex); maximum = maximum.Max(vertex); } }
        }
        var arrays = mesh as ArrayMesh ?? (mesh as ImmediateMesh)?.Surfaces;
        if (arrays is not null) { lock (arrays.Gate) { for (var i = 0; i < arrays.SurfaceCount; i++) Add(arrays.Get(i).Data); } }
        else { PrepareCustom(); for (var i = 0; i < _customCount; i++) Add(_custom[i].Data); }
        var bounds = new Rect2(minimum, maximum - minimum);
        if (!bounds.IsFinite() || !bounds.End.IsFinite()) throw new ArithmeticException("Mesh bounds exceed finite rectangles.");
        return bounds;
    }
    private static void AddBatch(List<CanvasBatch> batches, int first, int count, Material? surfaceMaterial, MaterialState? inheritedMaterial, BlendMode inheritedBlend,
        Texture? texture, TextureFilter filter, TextureRepeat repeat, int anisotropy, Rect2i? clip)
    {
        if (count == 0) return;
        var material = surfaceMaterial is null ? inheritedMaterial : surfaceMaterial.GetCanvasState(); var blend = surfaceMaterial is null ? inheritedBlend : surfaceMaterial.GetCanvasBlendMode();
        if (batches.Count != 0 && batches[^1] is var last && last.Material == material && last.Texture == texture && last.Filter == filter && last.Repeat == repeat && last.MaxAnisotropy == anisotropy && last.Blend == blend && last.Clip == clip)
            batches[^1] = last with { Count = last.Count + count };
        else batches.Add(new(first, count, material, texture, filter, repeat, anisotropy, blend, clip));
    }
    private static void AppendSurfaceInstances(List<CanvasVertex> output, MeshSurfaceData data, Mesh.PrimitiveType primitive, Transform transform, Color modulation, bool snap, MultiMesh? instances, float fraction)
    {
        if (instances is null) { AppendSurface(output, data, primitive, transform, modulation, snap); return; }
        for (var index = 0; index < instances.DrawCount; index++)
        {
            instances.Presentation(index, fraction, out var pose, out var color, out var custom);
            AppendSurface(output, data, primitive, transform * pose, modulation * color, snap, custom);
        }
    }
    private static void AppendSurface(List<CanvasVertex> output, MeshSurfaceData data, Mesh.PrimitiveType primitive, Transform transform, Color modulation, bool snap, Color custom = default)
    {
        var count = data.Indices.Length == 0 ? data.Vertices.Length : data.Indices.Length;
        if (primitive is Mesh.PrimitiveType.Triangles or Mesh.PrimitiveType.TriangleStrip)
        {
            if (primitive == Mesh.PrimitiveType.Triangles)
                for (var i = 0; i < count; i++) output.Add(Vertex(data, i, transform, modulation, snap, custom));
            else
                for (var i = 0; i + 2 < count; i++) { output.Add(Vertex(data, i + (i % 2), transform, modulation, snap, custom)); output.Add(Vertex(data, i + 1 - (i % 2), transform, modulation, snap, custom)); output.Add(Vertex(data, i + 2, transform, modulation, snap, custom)); }
            return;
        }
        var step = primitive == Mesh.PrimitiveType.Lines ? 2 : 1;
        for (var i = 0; i < count - (primitive == Mesh.PrimitiveType.Points ? 0 : 1); i += step)
        {
            var a = Vertex(data, i, transform, modulation, snap, custom); var b = primitive == Mesh.PrimitiveType.Points ? a : Vertex(data, i + 1, transform, modulation, snap, custom);
            var dx = (double)b.Position.X - a.Position.X; var dy = (double)b.Position.Y - a.Position.Y;
            if (primitive != Mesh.PrimitiveType.Points && dx == 0 && dy == 0) continue;
            var length = double.Hypot(dx, dy);
            var normal = primitive == Mesh.PrimitiveType.Points ? new Vector2(.5f, 0) : new Vector2((float)(dy / length * .5), (float)(-dx / length * .5));
            if (primitive == Mesh.PrimitiveType.Points) { a = a with { Position = a.Position - new Vector2(0, .5f) }; b = b with { Position = b.Position + new Vector2(0, .5f) }; }
            var q0 = a with { Position = a.Position + normal }; var q1 = b with { Position = b.Position + normal }; var q2 = b with { Position = b.Position - normal }; var q3 = a with { Position = a.Position - normal };
            output.Add(q0); output.Add(q1); output.Add(q2); output.Add(q0); output.Add(q2); output.Add(q3);
        }
    }
    private static CanvasVertex Vertex(MeshSurfaceData data, int order, Transform transform, Color modulation, bool snap, Color custom = default)
    {
        var index = data.Indices.Length == 0 ? order : data.Indices[order]; var position = transform * data.Vertices[index]; var color = (data.Colors.Length == 0 ? Colors.White : data.Colors[index]) * modulation;
        if (!position.IsFinite() || !color.IsFinite()) throw new InvalidOperationException("Mesh canvas values overflowed finite coordinates.");
        return new(snap ? CanvasGeometry.Snap(position) : position, color, data.UVs.Length == 0 ? Vector2.Zero : data.UVs[index], custom);
    }
}
