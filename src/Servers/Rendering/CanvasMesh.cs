namespace Electron2D;

internal sealed class CanvasMesh(Mesh mesh, Transform local, Color modulate)
{
    internal void Set(Mesh value, Transform pose, Color tint) { if (!ReferenceEquals(mesh, value)) { _revision = -1; _customCount = 0; Array.Clear(_custom); } mesh = value; local = pose; modulate = tint; }
    private (MeshSurfaceData Data, Mesh.PrimitiveType Primitive, Material? Material, MeshLOD[] LODs)[] _custom = [];
    private long _revision = -1;
    private int _customCount;
    internal Mesh Mesh => mesh;
    internal void Clear() { mesh = null!; _revision = -1; _customCount = 0; Array.Clear(_custom); }

    internal void Append(List<CanvasVertex> vertices, List<CanvasBatch> batches, Texture? texture, Transform transform, Color color,
        MaterialState? inheritedMaterial, BlendMode inheritedBlend, TextureFilter filter, TextureRepeat repeat, int anisotropy, Rect2i? clip, bool snap, MultiMesh? instances = null, float fraction = 1, CanvasItem? skinOwner = null, bool linear = false)
    {
        ObjectDisposedException.ThrowIf(mesh.IsDisposed, mesh);
        transform *= local; color *= modulate; if (linear) color = CanvasGeometry.LinearColor(color);
        RenderingSkeletonRegistry.Palette? lease = null;
        try
        {
            PrepareSkin(skinOwner, out var palette, out var toPalette, out var fromPalette, out lease);
            var arrays = mesh as ArrayMesh ?? (mesh as ImmediateMesh)?.Surfaces;
            if (arrays is not null)
            {
                lock (arrays.Gate)
                {
                    Rect2? visibility = arrays.CustomAABB is var manual && manual != default ? transform * (instances is null ? manual : instances.GetPresentationBounds(fraction)) : null;
                    for (var i = 0; i < arrays.SurfaceCount; i++)
                    {
                        var surface = arrays.Get(i); var first = vertices.Count;
                        var instanceCount = 0; var firstInstance = 0;
                        if (surface.LODs.Length == 0 && instances is not null && !snap && palette.IsEmpty && surface.Data.Bones.Length == 0 &&
                            surface.Primitive is Mesh.PrimitiveType.Triangles or Mesh.PrimitiveType.TriangleStrip &&
                            RenderingServer.Service is { } renderer && renderer.CanInstance(skinOwner) &&
                            TryInstanceBounds(surface.Data, out var bounds) && renderer.TryAppendInstances(instances, transform, color, arrays.CustomAABB != default ? arrays.CustomAABB : surface.PackedAABB ?? bounds, fraction, out firstInstance, linear, recordBounds: visibility is null))
                        {
                            instanceCount = instances.DrawCount;
                            AppendSurface(vertices, surface.Data, surface.Primitive, Transform.Identity, Colors.White, false, default, default, Transform.Identity, Transform.Identity, linear);
                        }
                        else AppendSurfaceInstances(vertices, surface.Data, surface.Primitive, transform, color, snap, instances, fraction, palette, toPalette, fromPalette, surface.LODs, linear);
                        AddBatch(batches, first, vertices.Count - first, surface.Material, inheritedMaterial, inheritedBlend, texture, filter, repeat, anisotropy, clip, skinOwner, firstInstance, instanceCount, linear, visibility);
                    }
                }
                return;
            }
            PrepareCustom();
            for (var i = 0; i < _customCount; i++)
            {
                var first = vertices.Count; var surface = _custom[i]; AppendSurfaceInstances(vertices, surface.Data, surface.Primitive, transform, color, snap, instances, fraction, palette, toPalette, fromPalette, surface.LODs, linear);
                AddBatch(batches, first, vertices.Count - first, surface.Material, inheritedMaterial, inheritedBlend, texture, filter, repeat, anisotropy, clip, skinOwner, linear: linear);
            }
        }
        finally { if (lease is not null) lease.ReplayReaders--; }
    }
    private static void PrepareSkin(CanvasItem? owner, out ReadOnlySpan<Transform> palette, out Transform toPalette, out Transform fromPalette, out RenderingSkeletonRegistry.Palette? lease)
    {
        palette = default; toPalette = fromPalette = Transform.Identity; lease = null;
        if (owner is null || !owner.AttachedSkeleton.IsValid() || !RenderingSkeletonRegistry.Prepare(owner.AttachedSkeleton, owner, out var prepared, out var basis, out lease)) return;
        var global = owner.RenderGlobal((float)Engine.PhysicsInterpolationFraction);
        if (basis.Determinant() == 0 || global.Determinant() == 0) return;
        palette = prepared; toPalette = basis.AffineInverse() * global; fromPalette = global.AffineInverse() * basis;
    }
    internal static void AppendTriangleData(List<CanvasVertex> output, MeshSurfaceData data, int drawCount, Transform transform, Color color, bool snap, CanvasItem? owner, bool linear = false)
    {
        RenderingSkeletonRegistry.Palette? lease = null;
        try
        {
            if (linear) color = CanvasGeometry.LinearColor(color);
            PrepareSkin(owner, out var palette, out var toPalette, out var fromPalette, out lease);
            if (!palette.IsEmpty) foreach (var bone in data.Bones) if ((uint)bone >= (uint)palette.Length) throw new InvalidOperationException("A triangle skin index does not resolve in its palette.");
            for (var i = 0; i < drawCount; i++) output.Add(Vertex(data, i, transform, color, snap, default, palette, toPalette, fromPalette, linear));
        }
        finally { if (lease is not null) lease.ReplayReaders--; }
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
                    var data = mesh.SurfaceGetArrays(i); var primitive = mesh.SurfaceGetPrimitiveType(i); data.Validate(primitive); data.QuantizeSkin(); if ((mesh.SurfaceGetFormat(i) & Mesh.ArrayFormat.CompressAttributes) != 0) _ = new MeshCompression(data); else MeshFrameCodec.Capture(data); data.PrepareCustoms();
                    _custom[i] = (data, primitive, mesh.SurfaceGetMaterial(i), MeshLOD.Capture(mesh.SurfaceGetLODs(i), data, primitive));
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
    internal Rect2 GetDrawBounds(CanvasItem owner) => local * (mesh is ArrayMesh { CustomAABB: var manual } && manual != default ? manual : GetLocalBounds(owner));
    internal Rect2 GetLocalBounds(CanvasItem? owner = null)
    {
        ObjectDisposedException.ThrowIf(mesh.IsDisposed, mesh); var first = true; var minimum = Vector2.Zero; var maximum = Vector2.Zero;
        void Add(MeshSurfaceData data)
        {
            foreach (var vertex in data.Vertices) { if (first) { minimum = maximum = vertex; first = false; } else { minimum = minimum.Min(vertex); maximum = maximum.Max(vertex); } }
        }
        void AddBounds(Rect2 bounds)
        {
            if (first) { minimum = bounds.Position; maximum = bounds.End; first = false; }
            else { minimum = minimum.Min(bounds.Position); maximum = maximum.Max(bounds.End); }
        }
        var arrays = mesh as ArrayMesh ?? (mesh as ImmediateMesh)?.Surfaces;
        if (arrays is not null)
        {
            RenderingSkeletonRegistry.Palette? lease = null;
            try
            {
                PrepareSkin(owner, out var palette, out var toPalette, out var fromPalette, out lease);
                lock (arrays.Gate) for (var i = 0; i < arrays.SurfaceCount; i++)
                    {
                        var surface = arrays.Get(i);
                        if (surface.PackedAABB is not { } authored) { Add(surface.Data); continue; }
                        var bones = surface.PackedBoneAABBs; var foundBone = false;
                        if (!palette.IsEmpty && bones.Length != 0)
                        {
                            if (bones.Length > palette.Length) throw new InvalidOperationException("Packed bone bounds exceed the attached palette.");
                            for (var bone = 0; bone < bones.Length; bone++) if (bones[bone] is { } boneRect) { AddBounds(fromPalette * (palette[bone] * (toPalette * boneRect))); foundBone = true; }
                        }
                        if (!foundBone) AddBounds(authored);
                    }
            }
            finally { if (lease is not null) lease.ReplayReaders--; }
        }
        else { PrepareCustom(); for (var i = 0; i < _customCount; i++) Add(_custom[i].Data); }
        var bounds = new Rect2(minimum, maximum - minimum);
        if (!bounds.IsFinite() || !bounds.End.IsFinite()) throw new ArithmeticException("Mesh bounds exceed finite rectangles.");
        return bounds;
    }
    private static void AddBatch(List<CanvasBatch> batches, int first, int count, Material? surfaceMaterial, MaterialState? inheritedMaterial, BlendMode inheritedBlend,
        Texture? texture, TextureFilter filter, TextureRepeat repeat, int anisotropy, Rect2i? clip, CanvasItem? owner, int firstInstance = 0, int instanceCount = 0, bool linear = false, Rect2? visibility = null)
    {
        if (count == 0) return;
        if (visibility is { } bounds && (!bounds.IsFinite() || !bounds.End.IsFinite())) throw new ArithmeticException("Mesh visibility bounds exceed finite canvas coordinates.");
        var material = surfaceMaterial is null ? inheritedMaterial : surfaceMaterial.GetCanvasState(); var blend = surfaceMaterial is null ? inheritedBlend : surfaceMaterial.GetCanvasBlendMode();
        var instanceOwner = material?.Program.HasInstanceUniforms == true ? owner : null;
        if (instanceCount == 0 && batches.Count != 0 && batches[^1] is var last && last.VisibilityBounds == visibility && last.InstanceCount == 0 && last.Operation == CanvasOperation.Draw && last.Glyph.Mode == CanvasGlyphMode.None && last.Material == material && last.InstanceOwner == instanceOwner && last.Texture == texture && last.Filter == filter && last.Repeat == repeat && last.MaxAnisotropy == anisotropy && last.Blend == blend && last.Clip == clip && last.LinearColors == linear)
            batches[^1] = last with { Count = last.Count + count };
        else batches.Add(new(first, count, material, texture, filter, repeat, anisotropy, blend, clip, FirstInstance: firstInstance, InstanceCount: instanceCount, InstanceOwner: instanceOwner, LinearColors: linear, VisibilityBounds: visibility));
    }
    private static bool TryInstanceBounds(MeshSurfaceData data, out Rect2 bounds)
    {
        bounds = default;
        if (data.Vertices.Length == 0) return false;
        foreach (var color in data.Colors)
            if (color.R is < 0 or > 1 || color.G is < 0 or > 1 || color.B is < 0 or > 1 || color.A is < 0 or > 1) return false;
        var minimum = data.Vertices[0]; var maximum = minimum;
        foreach (var vertex in data.Vertices) { minimum = minimum.Min(vertex); maximum = maximum.Max(vertex); }
        bounds = new(minimum, maximum - minimum);
        return bounds.IsFinite() && bounds.End.IsFinite();
    }
    private static void AppendSurfaceInstances(List<CanvasVertex> output, MeshSurfaceData data, Mesh.PrimitiveType primitive, Transform transform, Color modulation, bool snap, MultiMesh? instances, float fraction, ReadOnlySpan<Transform> palette, Transform toPalette, Transform fromPalette, MeshLOD[] lods, bool linear)
    {
        if (!palette.IsEmpty && data.Bones.Length != 0) foreach (var bone in data.Bones) if ((uint)bone >= (uint)palette.Length) throw new InvalidOperationException("A mesh skin index does not resolve in its palette.");
        if (instances is null) { AppendSurface(output, MeshLOD.Select(data, lods, transform), primitive, transform, modulation, snap, default, palette, toPalette, fromPalette, linear); return; }
        for (var index = 0; index < instances.DrawCount; index++)
        {
            instances.Presentation(index, fraction, out var pose, out var color, out var custom);
            AppendSurface(output, MeshLOD.Select(data, lods, transform * pose), primitive, transform * pose, modulation * (linear ? CanvasGeometry.LinearColor(color) : color), snap, custom, palette, toPalette, fromPalette, linear);
        }
    }
    private static void AppendSurface(List<CanvasVertex> output, MeshSurfaceData data, Mesh.PrimitiveType primitive, Transform transform, Color modulation, bool snap, Color custom, ReadOnlySpan<Transform> palette, Transform toPalette, Transform fromPalette, bool linear)
    {
        var count = data.Indices.Length == 0 ? data.Vertices.Length : data.Indices.Length;
        if (primitive is Mesh.PrimitiveType.Triangles or Mesh.PrimitiveType.TriangleStrip)
        {
            if (primitive == Mesh.PrimitiveType.Triangles)
                for (var i = 0; i < count; i++) output.Add(Vertex(data, i, transform, modulation, snap, custom, palette, toPalette, fromPalette, linear));
            else
                for (var i = 0; i + 2 < count; i++) { output.Add(Vertex(data, i + (i % 2), transform, modulation, snap, custom, palette, toPalette, fromPalette, linear)); output.Add(Vertex(data, i + 1 - (i % 2), transform, modulation, snap, custom, palette, toPalette, fromPalette, linear)); output.Add(Vertex(data, i + 2, transform, modulation, snap, custom, palette, toPalette, fromPalette, linear)); }
            return;
        }
        var step = primitive == Mesh.PrimitiveType.Lines ? 2 : 1;
        for (var i = 0; i < count - (primitive == Mesh.PrimitiveType.Points ? 0 : 1); i += step)
        {
            var a = Vertex(data, i, transform, modulation, snap, custom, palette, toPalette, fromPalette, linear); var b = primitive == Mesh.PrimitiveType.Points ? a : Vertex(data, i + 1, transform, modulation, snap, custom, palette, toPalette, fromPalette, linear);
            var dx = (double)b.Position.X - a.Position.X; var dy = (double)b.Position.Y - a.Position.Y;
            if (primitive != Mesh.PrimitiveType.Points && dx == 0 && dy == 0) continue;
            var length = double.Hypot(dx, dy);
            var normal = primitive == Mesh.PrimitiveType.Points ? new Vector2(.5f, 0) : new Vector2((float)(dy / length * .5), (float)(-dx / length * .5));
            if (primitive == Mesh.PrimitiveType.Points) { a = a with { Position = a.Position - new Vector2(0, .5f) }; b = b with { Position = b.Position + new Vector2(0, .5f) }; }
            var q0 = a with { Position = a.Position + normal }; var q1 = b with { Position = b.Position + normal }; var q2 = b with { Position = b.Position - normal }; var q3 = a with { Position = a.Position - normal };
            output.Add(q0); output.Add(q1); output.Add(q2); output.Add(q0); output.Add(q2); output.Add(q3);
        }
    }
    private static Vector2 Deform(MeshSurfaceData data, int index, ReadOnlySpan<Transform> palette, Transform toPalette, Transform fromPalette)
    {
        var point = data.Vertices[index]; if (palette.IsEmpty || data.Bones.Length == 0) return point;
        var source = toPalette * point; double x = 0, y = 0, total = 0; var slots = data.SkinSlots;
        for (var slot = 0; slot < slots; slot++) { var entry = index * slots + slot; var weight = data.Weights[entry]; if (weight == 0) continue; var transformed = palette[data.Bones[entry]] * source; x += transformed.X * (double)weight; y += transformed.Y * (double)weight; total += weight; }
        var result = fromPalette.BasisXform(new((float)x, (float)y)) + fromPalette.Origin * (float)total; if (!result.IsFinite()) throw new InvalidOperationException("Mesh skin deformation overflowed finite coordinates."); return result;
    }
    private static CanvasVertex Vertex(MeshSurfaceData data, int order, Transform transform, Color modulation, bool snap, Color custom, ReadOnlySpan<Transform> palette, Transform toPalette, Transform fromPalette, bool linear)
    {
        var index = data.Indices.Length == 0 ? order : data.Indices[order]; var position = transform * Deform(data, index, palette, toPalette, fromPalette); var sourceColor = data.Colors.Length == 0 ? Colors.White : data.Colors[index]; var color = (linear ? CanvasGeometry.LinearColor(sourceColor) : sourceColor) * modulation;
        if (!position.IsFinite() || !color.IsFinite()) throw new InvalidOperationException("Mesh canvas values overflowed finite coordinates.");
        return new(snap ? CanvasGeometry.Snap(position) : position, color, data.UVs.Length == 0 ? Vector2.Zero : data.UVs[index], custom, data.UV2s.Length == 0 ? Vector2.Zero : data.UV2s[index], data.Normals.Length == 0 ? default : data.Normals[index], data.Tangents.Length == 0 ? default : data.Tangents[index], data.Custom0 is null ? default : data.Custom0.Values[index], data.Custom1 is null ? default : data.Custom1.Values[index], data.Custom2 is null ? default : data.Custom2.Values[index], data.Custom3 is null ? default : data.Custom3.Values[index]);
    }
}
