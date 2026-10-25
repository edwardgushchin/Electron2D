namespace Electron2D;

internal sealed partial class CanvasMesh(Mesh mesh, Transform local, Color modulate, bool captureCustomBounds = true)
{
    internal void Set(Mesh value, Transform pose, Color tint) { if (!ReferenceEquals(mesh, value)) { _revision = -1; _customCount = 0; Array.Clear(_custom); } mesh = value; local = pose; modulate = tint; }
    private (MeshSurfaceData Data, Mesh.PrimitiveType Primitive, Material? Material, MeshLOD[] LODs)[] _custom = [];
    private long _revision = -1;
    private int _customCount;
    private Rect2 _customBounds;
    private bool _customProcedural;
    private readonly List<int[]> _deviceIndices = [];
    internal Mesh Mesh => mesh;
    internal void Clear() { mesh = null!; _revision = -1; _customCount = 0; Array.Clear(_custom); }

    internal void Append(List<CanvasVertex> vertices, List<CanvasBatch> batches, Texture? texture, Transform transform, Color color,
        MaterialState? inheritedMaterial, BlendMode inheritedBlend, TextureFilter filter, TextureRepeat repeat, int anisotropy, Rect2i? clip, bool snap, MultiMesh? instances = null, float fraction = 1, CanvasItem? skinOwner = null, bool linear = false, Texture? textureSource = null, CanvasMatrices? matrices = null)
    {
        ObjectDisposedException.ThrowIf(mesh.IsDisposed, mesh);
        transform *= local; matrices = matrices?.Local(local); color *= modulate; if (linear) color = CanvasGeometry.LinearColor(color);
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
                        if (instances?.GPUState is { } gpu)
                        {
                            var batch = AppendGPU(vertices, surface.Data, surface.Primitive, gpu, instances, i, transform, color, fraction, palette, toPalette, fromPalette, linear, surface.LODs);
                            var template = Batch(first, vertices.Count - first, surface.Material, inheritedMaterial, inheritedBlend, texture, filter, repeat, anisotropy, clip, skinOwner, instanceCount: instances.InstanceCount, linear: linear, visibility: transform * instances.GetPresentationBounds(fraction), procedural: surface.Data.IsProcedural, primitive: surface.Primitive, snap: snap, textureSource: textureSource, matrices: matrices);
                            AddBatch(vertices, batches, template with { GPU = batch, SnapGeneratedVertices = snap }); continue;
                        }
                        var surfaceVisibility = visibility ?? (surface.Data.IsProcedural ? transform * (instances is null ? surface.PackedAABB ?? default : instances.GetPresentationBounds(fraction)) : null);
                        var instanceCount = 0; var firstInstance = 0;
                        if (surface.LODs.Length == 0 && instances is not null && !snap && palette.IsEmpty && surface.Data.Bones.Length == 0 &&
                            (surface.Data.IsProcedural || UsesNativePoints(surface.Primitive) || surface.Primitive is Mesh.PrimitiveType.Triangles or Mesh.PrimitiveType.TriangleStrip) &&
                            RenderingServer.Service is { } renderer && renderer.CanInstance(skinOwner) &&
                            TryInstanceBounds(surface.Data, arrays.CustomAABB != default ? arrays.CustomAABB : surface.PackedAABB, out var bounds) && renderer.TryAppendInstances(instances, transform, color, arrays.CustomAABB != default ? arrays.CustomAABB : surface.PackedAABB ?? bounds, fraction, out firstInstance, linear, recordBounds: surfaceVisibility is null, models: (surface.Material?.RequireCanvasState() ?? inheritedMaterial)?.Program.ModelMatrixUniform is not null))
                        {
                            instanceCount = instances.DrawCount;
                            AppendSurface(vertices, surface.Data, surface.Primitive, Transform.Identity, Colors.White, false, default, default, Transform.Identity, Transform.Identity, linear);
                        }
                        else if (instances is not null && (surface.Data.IsProcedural || (surface.Material?.RequireCanvasState() ?? inheritedMaterial)?.Program.ModelMatrixUniform is not null))
                        {
                            if (instances.DrawCount == 0) continue;
                            var template = Batch(0, 0, surface.Material, inheritedMaterial, inheritedBlend, texture, filter, repeat, anisotropy, clip, skinOwner, linear: linear, visibility: surfaceVisibility, procedural: surface.Data.IsProcedural, primitive: NativePrimitive(surface.Data, surface.Primitive), snap: snap, textureSource: textureSource, matrices: matrices);
                            AppendSurfaceInstances(vertices, surface.Data, surface.Primitive, transform, color, snap, instances, fraction, palette, toPalette, fromPalette, surface.LODs, linear, batches, template);
                            continue;
                        }
                        else AppendSurfaceInstances(vertices, surface.Data, surface.Primitive, transform, color, snap, instances, fraction, palette, toPalette, fromPalette, surface.LODs, linear);
                        if (vertices.Count != first) AddBatch(vertices, batches, Batch(first, vertices.Count - first, surface.Material, inheritedMaterial, inheritedBlend, texture, filter, repeat, anisotropy, clip, skinOwner, firstInstance, instanceCount, linear, surfaceVisibility, surface.Data.IsProcedural, NativePrimitive(surface.Data, surface.Primitive), snap, textureSource, matrices));
                    }
                }
                return;
            }
            PrepareCustom();
            for (var i = 0; i < _customCount; i++)
            {
                var first = vertices.Count; var surface = _custom[i];
                if (instances?.GPUState is { } gpu)
                {
                    var batch = AppendGPU(vertices, surface.Data, surface.Primitive, gpu, instances, i, transform, color, fraction, palette, toPalette, fromPalette, linear, surface.LODs);
                    var template = Batch(first, vertices.Count - first, surface.Material, inheritedMaterial, inheritedBlend, texture, filter, repeat, anisotropy, clip, skinOwner, instanceCount: instances.InstanceCount, linear: linear, visibility: transform * instances.GetPresentationBounds(fraction), procedural: surface.Data.IsProcedural, primitive: surface.Primitive, snap: snap, textureSource: textureSource, matrices: matrices);
                    AddBatch(vertices, batches, template with { GPU = batch, SnapGeneratedVertices = snap }); continue;
                }
                Rect2? visibility = mesh.VisibilityOverride is var manual && manual != default ? transform * (instances is null ? manual : instances.GetPresentationBounds(fraction)) : surface.Data.IsProcedural ? transform * (instances is null ? _customBounds : instances.GetPresentationBounds(fraction)) : null;
                if (instances is not null && (surface.Data.IsProcedural || (surface.Material?.RequireCanvasState() ?? inheritedMaterial)?.Program.ModelMatrixUniform is not null))
                {
                    if (instances.DrawCount == 0) continue;
                    var template = Batch(0, 0, surface.Material, inheritedMaterial, inheritedBlend, texture, filter, repeat, anisotropy, clip, skinOwner, linear: linear, visibility: visibility, procedural: surface.Data.IsProcedural, primitive: NativePrimitive(surface.Data, surface.Primitive), snap: snap, textureSource: textureSource, matrices: matrices);
                    AppendSurfaceInstances(vertices, surface.Data, surface.Primitive, transform, color, snap, instances, fraction, palette, toPalette, fromPalette, surface.LODs, linear, batches, template);
                    continue;
                }
                AppendSurfaceInstances(vertices, surface.Data, surface.Primitive, transform, color, snap, instances, fraction, palette, toPalette, fromPalette, surface.LODs, linear);
                if (vertices.Count != first) AddBatch(vertices, batches, Batch(first, vertices.Count - first, surface.Material, inheritedMaterial, inheritedBlend, texture, filter, repeat, anisotropy, clip, skinOwner, linear: linear, visibility: visibility, procedural: surface.Data.IsProcedural, primitive: NativePrimitive(surface.Data, surface.Primitive), snap: snap, textureSource: textureSource));
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
            if (mesh is PrimitiveMesh generatedMesh) generatedMesh.EnsureGenerated();
            var revision = mesh.ChangeRevision;
            if (_revision != revision)
            {
                var hasProcedural = false; var count = mesh.GetSurfaceCount(); if (_custom.Length < count) Array.Resize(ref _custom, count);
                for (var i = 0; i < count; i++)
                {
                    var data = mesh.SurfaceGetArrays(i); var primitive = mesh.SurfaceGetPrimitiveType(i); data.Validate(primitive); if (data.IsProcedural && (mesh.SurfaceGetFormat(i) & Mesh.ArrayFormat.UseEmptyVertexArray) == 0) throw new ArgumentException("Position-free custom surfaces require UseEmptyVertexArray."); data.QuantizeSkin(); if ((mesh.SurfaceGetFormat(i) & Mesh.ArrayFormat.CompressAttributes) != 0) _ = new MeshCompression(data); else MeshFrameCodec.Capture(data); data.PrepareCustoms();
                    hasProcedural |= data.IsProcedural;
                    _custom[i] = (data, primitive, mesh.SurfaceGetMaterial(i), MeshLOD.Capture(mesh.SurfaceGetLODs(i), data, primitive));
                }
                var customBounds = hasProcedural && captureCustomBounds ? mesh.GetAABB() : default;
                ObjectDisposedException.ThrowIf(mesh.IsDisposed, mesh);
                if (mesh.ChangeRevision != revision) throw new InvalidOperationException("Mesh callbacks changed the resource while preparing its surfaces.");
                Array.Clear(_custom, count, _custom.Length - count);
                _customCount = count; _customProcedural = hasProcedural; _customBounds = customBounds;
                _revision = revision;
            }
        }
        finally { _preparing = false; }
    }
    internal Rect2 GetDrawBounds(CanvasItem owner) => local * (mesh.VisibilityOverride is var manual && manual != default ? manual : GetLocalBounds(owner));
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
        else { PrepareCustom(); if (_customProcedural) AddBounds(_customBounds); for (var i = 0; i < _customCount; i++) Add(_custom[i].Data); }
        var bounds = new Rect2(minimum, maximum - minimum);
        if (!bounds.IsFinite() || !bounds.End.IsFinite()) throw new ArithmeticException("Mesh bounds exceed finite rectangles.");
        return bounds;
    }
    private static bool UsesNativePoints(Mesh.PrimitiveType primitive) => primitive == Mesh.PrimitiveType.Points && RenderingServer.Service?.NativePoints == true;
    private static Mesh.PrimitiveType NativePrimitive(MeshSurfaceData data, Mesh.PrimitiveType primitive) => data.IsProcedural || UsesNativePoints(primitive) ? primitive : Mesh.PrimitiveType.Triangles;
    private static CanvasBatch Batch(int first, int count, Material? surfaceMaterial, MaterialState? inheritedMaterial, BlendMode inheritedBlend,
        Texture? texture, TextureFilter filter, TextureRepeat repeat, int anisotropy, Rect2i? clip, CanvasItem? owner, int firstInstance = 0, int instanceCount = 0, bool linear = false, Rect2? visibility = null, bool procedural = false, Mesh.PrimitiveType primitive = Mesh.PrimitiveType.Triangles, bool snap = false, Texture? textureSource = null, CanvasMatrices? matrices = null)
    {
        if (visibility is { } bounds && (!bounds.IsFinite() || !bounds.End.IsFinite())) throw new ArithmeticException("Mesh visibility bounds exceed finite canvas coordinates.");
        var material = surfaceMaterial is null ? inheritedMaterial : surfaceMaterial.RequireCanvasState(); var blend = surfaceMaterial is null ? inheritedBlend : surfaceMaterial.GetCanvasBlendMode();
        if (procedural && material?.Program.Vertex is null) throw new NotSupportedException("Position-free mesh drawing requires an authored vertex stage.");
        var instanceOwner = material?.Program.HasInstanceUniforms == true ? owner : null;
        return new(first, count, material, texture, filter, repeat, anisotropy, blend, clip, FirstInstance: firstInstance, InstanceCount: instanceCount, InstanceOwner: instanceOwner, LinearColors: linear, VisibilityBounds: visibility, Procedural: procedural, Primitive: primitive, SnapGeneratedVertices: procedural && snap, TextureSource: textureSource, Matrices: matrices?.For(material), Glyph: owner?.ResolveCanvasGlyph(texture) ?? default);
    }
    private static void AddBatch(List<CanvasVertex> vertices, List<CanvasBatch> batches, CanvasBatch batch)
    {
        if (batch.Count == 0) return;
        ApplyGlyph(vertices, batch);
        if (!batch.Procedural && batch.InstanceCount == 0 && batches.Count != 0 && batches[^1] is var last && !last.Procedural && last.VisibilityBounds == batch.VisibilityBounds && last.InstanceCount == 0 && last.Operation == CanvasOperation.Draw && last.Glyph == batch.Glyph && last.Material == batch.Material && last.Matrices == batch.Matrices && last.NormalMapFlags == batch.NormalMapFlags && last.TextureRegion == batch.TextureRegion && last.InstanceOwner == batch.InstanceOwner && last.Texture == batch.Texture && last.TextureSource == batch.TextureSource && last.Filter == batch.Filter && last.Repeat == batch.Repeat && last.MaxAnisotropy == batch.MaxAnisotropy && last.Blend == batch.Blend && last.Clip == batch.Clip && last.LinearColors == batch.LinearColors && last.Primitive == batch.Primitive && last.SnapNativeLines == batch.SnapNativeLines)
            batches[^1] = last with { Count = last.Count + batch.Count, ObjectCount = checked(last.ObjectCount + batch.ObjectCount) };
        else batches.Add(batch);
    }
    private static void ApplyGlyph(List<CanvasVertex> vertices, CanvasBatch batch)
    {
        if (batch.Glyph.Mode == CanvasGlyphMode.None) return;
        var parameters = batch.Glyph.Parameters;
        for (var i = batch.First; i < batch.First + batch.Count; i++) vertices[i] = vertices[i] with { Glyph = parameters };
    }
    private static bool TryInstanceBounds(MeshSurfaceData data, Rect2? authored, out Rect2 bounds)
    {
        bounds = default;
        if (data.IsProcedural) { bounds = authored ?? default; return bounds.HasArea(); }
        foreach (var color in data.Colors)
            if (color.R is < 0 or > 1 || color.G is < 0 or > 1 || color.B is < 0 or > 1 || color.A is < 0 or > 1) return false;
        var minimum = data.Vertices[0]; var maximum = minimum;
        foreach (var vertex in data.Vertices) { minimum = minimum.Min(vertex); maximum = maximum.Max(vertex); }
        bounds = new(minimum, maximum - minimum);
        return bounds.IsFinite() && bounds.End.IsFinite();
    }
    private static void AppendSurfaceInstances(List<CanvasVertex> output, MeshSurfaceData data, Mesh.PrimitiveType primitive, Transform transform, Color modulation, bool snap, MultiMesh? instances, float fraction, ReadOnlySpan<Transform> palette, Transform toPalette, Transform fromPalette, MeshLOD[] lods, bool linear, List<CanvasBatch>? splitBatches = null, CanvasBatch template = default)
    {
        if (!palette.IsEmpty && data.Bones.Length != 0) foreach (var bone in data.Bones) if ((uint)bone >= (uint)palette.Length) throw new InvalidOperationException("A mesh skin index does not resolve in its palette.");
        if (instances is null) { AppendSurface(output, MeshLOD.Select(data, lods, transform), primitive, transform, modulation, snap, default, palette, toPalette, fromPalette, linear); return; }
        for (var index = 0; index < instances.DrawCount; index++)
        {
            instances.Presentation(index, fraction, out var pose, out var color, out var custom);
            var first = output.Count;
            AppendSurface(output, MeshLOD.Select(data, lods, transform * pose), primitive, transform * pose, modulation * (linear ? CanvasGeometry.LinearColor(color) : color), snap, custom, palette, toPalette, fromPalette, linear, (uint)index);
            if (splitBatches is not null) AddBatch(output, splitBatches, template with { First = first, Count = output.Count - first, Matrices = template.Matrices?.Instance(pose) });
        }
    }
    private static void AppendSurface(List<CanvasVertex> output, MeshSurfaceData data, Mesh.PrimitiveType primitive, Transform transform, Color modulation, bool snap, Color custom, ReadOnlySpan<Transform> palette, Transform toPalette, Transform fromPalette, bool linear, uint instanceID = uint.MaxValue)
    {
        var count = data.Indices.Length == 0 ? data.VertexCount : data.Indices.Length;
        if (data.IsProcedural || UsesNativePoints(primitive)) { for (var i = 0; i < count; i++) output.Add(Vertex(data, i, transform, modulation, !data.IsProcedural && snap, custom, palette, toPalette, fromPalette, linear, instanceID)); return; }
        if (primitive is Mesh.PrimitiveType.Triangles or Mesh.PrimitiveType.TriangleStrip)
        {
            if (primitive == Mesh.PrimitiveType.Triangles)
                for (var i = 0; i < count; i++) output.Add(Vertex(data, i, transform, modulation, snap, custom, palette, toPalette, fromPalette, linear, instanceID));
            else
                for (var i = 0; i + 2 < count; i++) { output.Add(Vertex(data, i + (i % 2), transform, modulation, snap, custom, palette, toPalette, fromPalette, linear, instanceID)); output.Add(Vertex(data, i + 1 - (i % 2), transform, modulation, snap, custom, palette, toPalette, fromPalette, linear, instanceID)); output.Add(Vertex(data, i + 2, transform, modulation, snap, custom, palette, toPalette, fromPalette, linear, instanceID)); }
            return;
        }
        var step = primitive == Mesh.PrimitiveType.Lines ? 2 : 1;
        for (var i = 0; i < count - (primitive == Mesh.PrimitiveType.Points ? 0 : 1); i += step)
        {
            var a = Vertex(data, i, transform, modulation, snap, custom, palette, toPalette, fromPalette, linear, instanceID); var b = primitive == Mesh.PrimitiveType.Points ? a : Vertex(data, i + 1, transform, modulation, snap, custom, palette, toPalette, fromPalette, linear, instanceID);
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
        var point = data.Vertices[index]; return DeformPoint(data, index, point, palette, toPalette, fromPalette);
    }
    private static Vector2 DeformPoint(MeshSurfaceData data, int index, Vector2 point, ReadOnlySpan<Transform> palette, Transform toPalette, Transform fromPalette)
    {
        if (palette.IsEmpty || data.Bones.Length == 0) return point;
        var source = toPalette * point; double x = 0, y = 0, total = 0; var slots = data.SkinSlots;
        for (var slot = 0; slot < slots; slot++) { var entry = index * slots + slot; var weight = data.Weights[entry]; if (weight == 0) continue; var transformed = palette[data.Bones[entry]] * source; x += transformed.X * (double)weight; y += transformed.Y * (double)weight; total += weight; }
        var result = fromPalette.BasisXform(new((float)x, (float)y)) + fromPalette.Origin * (float)total; if (!result.IsFinite()) throw new InvalidOperationException("Mesh skin deformation overflowed finite coordinates."); return result;
    }
    private static CanvasVertex Vertex(MeshSurfaceData data, int order, Transform transform, Color modulation, bool snap, Color custom, ReadOnlySpan<Transform> palette, Transform toPalette, Transform fromPalette, bool linear, uint instanceID = uint.MaxValue)
    {
        var index = data.Indices.Length == 0 ? order : data.Indices[order];
        return VertexIndex(data, index, transform, modulation, snap, custom, palette, toPalette, fromPalette, linear, instanceID);
    }
    private MultiMeshGPUDraw AppendGPU(List<CanvasVertex> vertices, MeshSurfaceData data, Mesh.PrimitiveType primitive, MultiMeshGPUState state, MultiMesh instances, int surface, Transform canvas, Color modulation, float fraction, ReadOnlySpan<Transform> palette, Transform toPalette, Transform fromPalette, bool linear, MeshLOD[] lods)
    {
        state.Prepare(); var count = data.VertexCount;
        if (count == 0) { count = data.Indices.Max() + 1; foreach (var lod in lods) count = Math.Max(count, lod.Data.Indices.Max() + 1); }
        if (!palette.IsEmpty) foreach (var bone in data.Bones) if ((uint)bone >= (uint)palette.Length) throw new InvalidOperationException("A mesh skin index does not resolve in its palette.");
        for (var i = 0; i < count; i++) vertices.Add(VertexIndex(data, i, Transform.Identity, Colors.White, false, default, palette, toPalette, fromPalette, linear));
        int[]? indices = null;
        if (data.Indices.Length != 0)
        {
            while (_deviceIndices.Count <= surface) _deviceIndices.Add([]);
            var length = data.Indices.Length; foreach (var lod in lods) length = checked(length + lod.Data.Indices.Length);
            if (_deviceIndices[surface].Length != length) _deviceIndices[surface] = new int[length];
            indices = _deviceIndices[surface]; data.Indices.CopyTo(indices, 0); var offset = data.Indices.Length;
            foreach (var lod in lods) { lod.Data.Indices.CopyTo(indices, offset); offset += lod.Data.Indices.Length; }
        }
        return new(state, surface, indices, data.Indices.Length, lods, canvas, modulation, instances.Stride, instances.InstanceCount, instances.DrawCount, instances.UseColors, instances.UseCustomData, instances.UseIndirect, instances.InterpolationEnabled ? Math.Clamp(fraction, 0, 1) : 1, instances.PhysicsInterpolationQualityMode);
    }
    private static CanvasVertex VertexIndex(MeshSurfaceData data, int index, Transform transform, Color modulation, bool snap, Color custom, ReadOnlySpan<Transform> palette, Transform toPalette, Transform fromPalette, bool linear, uint instanceID = uint.MaxValue)
    {
        var affine = transform;
        if (data.IsProcedural && !palette.IsEmpty && data.Bones.Length != 0)
        {
            var origin = DeformPoint(data, index, Vector2.Zero, palette, toPalette, fromPalette);
            affine *= new Transform(DeformPoint(data, index, Vector2.Right, palette, toPalette, fromPalette) - origin, DeformPoint(data, index, Vector2.Down, palette, toPalette, fromPalette) - origin, origin);
        }
        if (data.IsProcedural && (!affine.X.IsFinite() || !affine.Y.IsFinite())) throw new InvalidOperationException("Procedural mesh transforms exceed finite coordinates.");
        var position = data.IsProcedural ? affine.Origin : transform * Deform(data, index, palette, toPalette, fromPalette); var sourceColor = data.Colors.Length == 0 ? Colors.White : data.Colors[index]; var color = (linear ? CanvasGeometry.LinearColor(sourceColor) : sourceColor) * modulation;
        if (!position.IsFinite() || !color.IsFinite()) throw new InvalidOperationException("Mesh canvas values overflowed finite coordinates.");
        return new(snap ? CanvasGeometry.Snap(position) : position, color, data.UVs.Length == 0 ? Vector2.Zero : data.UVs[index], custom, data.UV2s.Length == 0 ? Vector2.Zero : data.UV2s[index], data.Normals.Length == 0 ? default : data.Normals[index], data.Tangents.Length == 0 ? default : data.Tangents[index], data.Custom0 is null ? default : data.Custom0.Values[index], data.Custom1 is null ? default : data.Custom1.Values[index], data.Custom2 is null ? default : data.Custom2.Values[index], data.Custom3 is null ? default : data.Custom3.Values[index], Glyph: data.IsProcedural ? new(affine.X.X, affine.X.Y, affine.Y.X, affine.Y.Y) : default, VertexID: (uint)index, InstanceID: instanceID);
    }
}
