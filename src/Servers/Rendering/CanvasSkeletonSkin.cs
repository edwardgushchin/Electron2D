namespace Electron2D;

internal sealed class CanvasSkeletonSkin
{
    private WeakReference<Polygon>? _owner;
    private int[] _source = [], _indices = [];
    private float[] _weights = [];
    private Vector2[] _positions = [];
    private RID _skeleton;
    private int _count;
    private ulong _revision = ulong.MaxValue, _setup, _path;
    internal void Set(Polygon owner, ReadOnlySpan<int> source, int count)
    {
        var changed = _owner is null || !_owner.TryGetTarget(out var previous) || !ReferenceEquals(previous, owner) || _count != count;
        if (_owner is null) _owner = new(owner); else _owner.SetTarget(owner);
        if (_source.Length < count) Array.Resize(ref _source, count);
        for (var i = 0; i < count; i++) { var index = source.IsEmpty ? i : source[i]; changed |= _source[i] != index; _source[i] = index; }
        _count = count; if (changed) _revision = ulong.MaxValue;
    }
    internal bool Prepare(CanvasVertex[] vertices, int count)
    {
        if (_owner is null || !_owner.TryGetTarget(out var owner) || owner.IsDisposed || owner.ResolveSkinSkeleton() is not { } skeleton) return false;
        var rid = skeleton.GetSkeleton(); if (!ReferenceEquals(RenderingSkeletonRegistry.Resolve(rid), skeleton)) return false;
        var setup = skeleton.SetupGeneration; var path = owner.Tree!.PathRevision;
        if (_skeleton != rid || _revision != owner.SkinRevision || _setup != setup || _path != path) Build(owner, skeleton, rid, count, setup, path);
        var fraction = (float)Engine.PhysicsInterpolationFraction; var polygonGlobal = owner.GetInterpolatedGlobalVisualTransform(fraction); var skeletonGlobal = skeleton.GetInterpolatedGlobalVisualTransform(fraction); if (polygonGlobal.Determinant() == 0 || skeletonGlobal.Determinant() == 0) return false;
        var toSkeleton = skeletonGlobal.AffineInverse() * polygonGlobal; var fromSkeleton = polygonGlobal.AffineInverse() * skeletonGlobal;
        var palette = skeleton.PrepareSkinPalette(); if (_positions.Length < count) Array.Resize(ref _positions, count);
        for (var vertex = 0; vertex < count; vertex++)
        {
            var origin = toSkeleton * vertices[vertex].Position; double x = 0, y = 0, total = 0;
            for (var slot = 0; slot < 4; slot++)
            {
                var entry = vertex * 4 + slot; var weight = _weights[entry]; if (weight == 0) continue; var point = palette[_indices[entry]] * origin; x += point.X * (double)weight; y += point.Y * (double)weight; total += weight;
            }
            var result = total == 0 ? vertices[vertex].Position : fromSkeleton * new Vector2((float)(x / total), (float)(y / total));
            if (!result.IsFinite()) throw new InvalidOperationException("Skin deformation overflowed finite coordinates."); _positions[vertex] = result;
        }
        return true;
    }
    private void Build(Polygon owner, Skeleton skeleton, RID rid, int count, ulong setup, ulong path)
    {
        var length = checked(count * 4); if (_indices.Length < length) Array.Resize(ref _indices, length); if (_weights.Length < length) Array.Resize(ref _weights, length); Array.Clear(_weights, 0, length);
        for (var record = 0; record < owner.GetBoneCount(); record++)
        {
            var boneData = owner.SkinRecord(record); if (boneData.Weights.Length != owner.SkinInputLength || skeleton.GetNodeOrNull(boneData.Path) is not Bone bone || !ReferenceEquals(bone.SkeletonOwner, skeleton)) continue; var index = bone.GetIndexInSkeleton(); if (index < 0) continue;
            for (var vertex = 0; vertex < count; vertex++)
            {
                var weight = boneData.Weights[_source[vertex]]; if (weight <= 0) continue;
                for (var slot = 0; slot < 4; slot++) if (_weights[vertex * 4 + slot] < weight)
                    {
                        for (var later = 3; later > slot; later--) { _weights[vertex * 4 + later] = _weights[vertex * 4 + later - 1]; _indices[vertex * 4 + later] = _indices[vertex * 4 + later - 1]; }
                        _weights[vertex * 4 + slot] = weight; _indices[vertex * 4 + slot] = index; break;
                    }
            }
        }
        _skeleton = rid; _revision = owner.SkinRevision; _setup = setup; _path = path;
    }
    internal Vector2 Position(int index) => _positions[index];
}
