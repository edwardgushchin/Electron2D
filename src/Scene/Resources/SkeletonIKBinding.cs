namespace Electron2D;

internal sealed class SkeletonIKBinding
{
    private WeakReference<Skeleton>? _owner;
    private WeakReference<Node>? _node;
    private string? _path;
    private int _index;
    private ulong _setup, _revision;
    internal Node? Resolve(Skeleton owner, string path, int index = -1)
    {
        var setup = owner.SetupGeneration; var revision = owner.Tree!.PathRevision;
        if (_owner is null || !_owner.TryGetTarget(out var previous) || !ReferenceEquals(previous, owner) || _path != path || _index != index || _setup != setup || _revision != revision)
        {
            var node = path.Length > 0 ? owner.GetNodeOrNull(path) : index >= 0 && index < owner.GetBoneCount() ? owner.GetBone(index) : null;
            if (ReferenceEquals(node, owner)) node = null;
            if (_owner is null) _owner = new(owner); else _owner.SetTarget(owner);
            if (node is null) _node = null; else if (_node is null) _node = new(node); else _node.SetTarget(node);
            _path = path; _index = index; _setup = setup; _revision = revision;
        }
        return _node is not null && _node.TryGetTarget(out var result) && !result.IsDisposed && ReferenceEquals(result.Tree, owner.Tree) ? result : null;
    }
    internal Bone? Bone(Skeleton owner, string path, int index) => Resolve(owner, path, index) is Bone bone && ReferenceEquals(bone.SkeletonOwner, owner) ? bone : null;
    internal static void Path(string value) { ArgumentNullException.ThrowIfNull(value); if (value.Length > 65536 || value.Contains('\0')) throw new ArgumentException("Invalid IK path.", nameof(value)); }
    internal static string SelectionPath(Skeleton? owner, int index) { if (index < 0) throw new ArgumentOutOfRangeException(nameof(index)); return owner is { IsInsideTree: true } ? owner.GetPathTo(owner.GetBone(index)) : string.Empty; }
    internal static void Scalar(float value, bool nonnegative = false) { if (!float.IsFinite(value) || nonnegative && value < 0) throw new ArgumentOutOfRangeException(nameof(value)); }
    internal static double Distance(Vector2 a, Vector2 b) { var x = (double)b.X - a.X; var y = (double)b.Y - a.Y; return Math.Sqrt(x * x + y * y); }
    internal static Vector2 Direction(Vector2 from, Vector2 to, Vector2 fallback) { var distance = Distance(from, to); return distance > 0 ? new((float)(((double)to.X - from.X) / distance), (float)(((double)to.Y - from.Y) / distance)) : fallback == Vector2.Zero ? Vector2.Right : fallback.Normalized(); }
    internal static Vector2 Point(Vector2 origin, Vector2 direction, double length) { var point = new Vector2((float)(origin.X + direction.X * length), (float)(origin.Y + direction.Y * length)); if (!point.IsFinite()) throw new InvalidOperationException("IK coordinates overflowed."); return point; }
    internal static float Length(Bone bone) { var scale = bone.GlobalScale.Abs(); var length = (double)bone.GetLength() * Math.Min(scale.X, scale.Y); if (!double.IsFinite(length) || length > float.MaxValue) throw new InvalidOperationException("IK length overflowed."); return length > 0 ? (float)length : 0; }
    internal static Vector2 Axis(Bone bone) { var angle = bone.GetBoneAngle(); return Direction(Vector2.Zero, bone.GlobalTransform.BasisXform(new(MathF.Cos(angle), MathF.Sin(angle))), Vector2.Right); }
    internal static float Handed(Bone bone) => bone.GlobalTransform.Determinant() < 0 ? -1 : 1;
    internal static void Aim(Bone bone, float endpointAngle) => bone.GlobalRotation = endpointAngle - Handed(bone) * bone.GetBoneAngle();
    internal static void Request(Skeleton owner, Bone bone, float strength) => owner.SetBoneLocalPoseOverride(bone.GetIndexInSkeleton(), bone.Transform, strength, persistent: false);
}
