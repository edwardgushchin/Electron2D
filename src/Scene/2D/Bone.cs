namespace Electron2D;

/// <summary>A skeletal canvas bone with an authored rest pose and an ordinary animated local transform.</summary>
/// <remarks>Bones form an uninterrupted chain below Skeleton. The skeleton borrows them through scene membership.
/// Rest starts as the zero transform, indicating an unset bind pose. Configuration is scene-owner work.</remarks>
public class Bone : Entity
{
    private Transform _rest;
    private float _length = 16, _angle;
    private bool _auto = true, _copyPose = true;
    internal Skeleton? SkeletonOwner;
    internal int SkeletonIndex = -1;
    internal Transform AuthoredPose = Transform.Identity;
    /// <summary>Creates an unset rest pose, automatic length/angle and length sixteen.</summary>
    public Bone() { NotifyLocalTransformChanges = true; }
    /// <summary>Gets or sets the finite local rest transform.</summary>
    /// <value>Zero initially. Singular rest poses remain authored but do not contribute skin deformation.</value>
    /// <exception cref="ArgumentException">The transform is nonfinite.</exception>
    public Transform Rest { get { ReadBone(); return _rest; } set { EnsureMutable(); if (!value.IsFinite()) throw new ArgumentException("Rest must be finite.", nameof(value)); _rest = value; SkeletonOwner?.InvalidateSetup(); UpdateConfigurationWarnings(); } }
    private void ReadBone() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    /// <summary>Assigns the rest transform to this bone's ordinary local transform.</summary>
    public void ApplyRest() { EnsureMutable(); Transform = _rest; }
    /// <summary>Returns the accumulated rest transform through its direct parent-bone chain.</summary>
    /// <returns>The skeleton-relative rest pose, or this Rest without a direct parent bone.</returns>
    public Transform GetSkeletonRest() { ReadBone(); var result = Parent is Bone parent ? parent.GetSkeletonRest() * _rest : _rest; if (!result.IsFinite()) throw new InvalidOperationException("Rest hierarchy overflowed."); return result; }
    /// <summary>Returns the index assigned by its attached skeleton's depth-first bone order.</summary>
    /// <returns>Minus one without an attached valid chain.</returns>
    public int GetIndexInSkeleton() { ReadBone(); SkeletonOwner?.EnsureSetup(); return SkeletonIndex; }
    /// <summary>Returns whether automatic length and angle calculation is enabled.</summary>
    /// <returns>True initially.</returns>
    public bool GetAutocalculateLengthAndAngle() { ReadBone(); return _auto; }
    /// <summary>Enables calculation from the first direct child Bone or keeps manually authored values.</summary>
    /// <param name="autoCalculate">Whether to calculate immediately and on Ready.</param>
    /// <remarks>A leaf retains its length and uses its transform rotation as the fallback angle.</remarks>
    public void SetAutocalculateLengthAndAngle(bool autoCalculate) { EnsureMutable(); _auto = autoCalculate; if (_auto) Calculate(); NotifyPropertyListChanged(); }
    private void Calculate()
    {
        for (var i = 0; i < GetChildCount(); i++) if (GetChild(i) is Bone child)
            {
                var point = child.TopLevel ? GetGlobalTransform().AffineInverse() * child.GlobalPosition : child.Position;
                var length = Math.Sqrt((double)point.X * point.X + (double)point.Y * point.Y); if (length > float.MaxValue) throw new InvalidOperationException("Bone length overflowed.");
                _length = (float)length; _angle = point.Angle(); return;
            }
        _angle = Rotation;
    }
    /// <summary>Returns the bone endpoint length in pixels.</summary><returns>Sixteen initially; signed authored lengths are retained.</returns>
    public float GetLength() { ReadBone(); return _length; }
    /// <summary>Authors a finite endpoint length; this does not change the node transform.</summary><param name="length">Finite signed pixels.</param>
    public void SetLength(float length) { EnsureMutable(); if (!float.IsFinite(length)) throw new ArgumentOutOfRangeException(nameof(length)); _length = length; }
    /// <summary>Returns endpoint direction relative to the bone's X basis.</summary><returns>Angle in radians, initially zero.</returns>
    public float GetBoneAngle() { ReadBone(); return _angle; }
    /// <summary>Authors the finite endpoint direction used by skeletal modifications.</summary><param name="angle">Radians; independent of node Rotation.</param>
    public void SetBoneAngle(float angle) { EnsureMutable(); if (!float.IsFinite(angle)) throw new ArgumentOutOfRangeException(nameof(angle)); _angle = angle; }
    internal void BeginModification() => _copyPose = false;
    internal void EndModification() => _copyPose = true;
    internal void ApplyModifiedPose(Transform pose) { _copyPose = false; try { Transform = pose; } finally { _copyPose = SkeletonOwner?.Executing != true; } }
    internal override void OnTreeMembershipChanged(bool entering)
    {
        base.OnTreeMembershipChanged(entering);
        if (entering)
        {
            var node = Parent; while (node is Bone) node = node.Parent;
            SkeletonOwner = node as Skeleton; AuthoredPose = Transform; SkeletonOwner?.InvalidateSetup();
        }
        else { var owner = SkeletonOwner; if (owner is not null && !IsDisposed) ApplyModifiedPose(AuthoredPose); SkeletonOwner = null; SkeletonIndex = -1; owner?.InvalidateSetup(); }
    }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        if (what == NotificationLocalTransformChanged && _copyPose) AuthoredPose = Transform;
        if (what == NotificationChildOrderChanged) SkeletonOwner?.InvalidateSetup();
        base.OnNotification(what);
        if (what == NotificationReady && _auto) Calculate();
    }
    /// <inheritdoc />
    public override string[] GetConfigurationWarnings()
    {
        var warnings = base.GetConfigurationWarnings();
        if (SkeletonOwner is null) warnings = [.. warnings, "Bone requires an uninterrupted Bone chain below Skeleton."];
        if (_rest.Determinant() == 0) warnings = [.. warnings, "Bone has no invertible rest pose for skinning."];
        return warnings;
    }
    private static readonly PropertyDescriptor[] BoneProperties =
    [
        new PropertyDescriptor<Bone, Transform>(nameof(Rest), b => b.Rest, (b, v) => b.Rest = v, _ => default, stored: true),
        new PropertyDescriptor<Bone, bool>("_auto_calculate_length_and_angle", b => b.GetAutocalculateLengthAndAngle(), (b, v) => b.SetAutocalculateLengthAndAngle(v), _ => true, stored: true),
        new PropertyDescriptor<Bone, float>("_length", b => b.GetLength(), (b, v) => b.SetLength(v), _ => 16, stored: true),
        new PropertyDescriptor<Bone, float>("_bone_angle", b => b.GetBoneAngle(), (b, v) => b.SetBoneAngle(v), _ => 0, stored: true)
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(BoneProperties);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(Bone) ? CreateBone : base.CreateSceneInstanceFactory();
    private static Bone CreateBone() => new();
}
