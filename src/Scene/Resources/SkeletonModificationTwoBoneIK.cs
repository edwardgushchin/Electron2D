namespace Electron2D;

/// <summary>Solves a two-bone limb using the law of cosines and optional target distance limits.</summary>
/// <remarks>Selections resolve relative to the bound Skeleton. Missing bones/targets or nonpositive segment lengths
/// skip the solve. Settings copy/store; scene bindings stay weak. Bone endpoint lengths use the minimum absolute
/// global scale, so nonuniform scale retains that conservative length policy.</remarks>
public sealed class SkeletonModificationTwoBoneIK : SkeletonModification
{
    private readonly SkeletonIKBinding _target = new(), _one = new(), _two = new();
    private string _targetPath = string.Empty, _onePath = string.Empty, _twoPath = string.Empty;
    private int _oneIndex = -1, _twoIndex = -1;
    private float _minimum, _maximum;
    private bool _flip;
    /// <summary>Creates an unselected, enabled idle-phase two-bone solver.</summary>
    public SkeletonModificationTwoBoneIK() { }
    /// <summary>Gets or sets relative scene path of the Entity target.</summary><value>Empty initially.</value>
    public string TargetNodePath { get { lock (ModificationGate) { ThrowIfDisposed(); return _targetPath; } } set { SkeletonIKBinding.Path(value); lock (ModificationGate) { EnsureModificationMutable(); _targetPath = value; } EmitChanged(); } }
    /// <summary>Gets or sets minimum root-to-target solve distance in canvas-world pixels.</summary><value>Zero disables the minimum.</value>
    public float TargetMinimumDistance { get { lock (ModificationGate) { ThrowIfDisposed(); return _minimum; } } set { SkeletonIKBinding.Scalar(value, true); lock (ModificationGate) { EnsureModificationMutable(); _minimum = value; } EmitChanged(); } }
    /// <summary>Gets or sets maximum root-to-target solve distance in canvas-world pixels.</summary><value>Zero disables the maximum.</value>
    public float TargetMaximumDistance { get { lock (ModificationGate) { ThrowIfDisposed(); return _maximum; } } set { SkeletonIKBinding.Scalar(value, true); lock (ModificationGate) { EnsureModificationMutable(); _maximum = value; } EmitChanged(); } }
    /// <summary>Gets or sets whether the limb bends toward the opposite side.</summary><value>False initially.</value>
    public bool FlipBendDirection { get { lock (ModificationGate) { ThrowIfDisposed(); return _flip; } } set { lock (ModificationGate) { EnsureModificationMutable(); _flip = value; } EmitChanged(); } }
    /// <summary>Returns the relative bone path for joint one.</summary><returns>Empty without a path selection.</returns>
    public string GetJointOneBoneNode() { lock (ModificationGate) { ThrowIfDisposed(); return _onePath; } }
    /// <summary>Selects joint one by a bone path relative to its skeleton.</summary><param name="boneNode">Bounded scene path; empty uses its stored index.</param>
    public void SetJointOneBoneNode(string boneNode) { SkeletonIKBinding.Path(boneNode); lock (ModificationGate) { EnsureModificationMutable(); _onePath = boneNode; } EmitChanged(); }
    /// <summary>Returns the current resolved or authored index of joint one.</summary><returns>Minus one when its path is missing or no index was authored.</returns>
    public int GetJointOneBoneIndex() { lock (ModificationGate) { ThrowIfDisposed(); return BoundSkeleton is { IsInsideTree: true } owner && _onePath.Length > 0 ? _one.Bone(owner, _onePath, _oneIndex)?.GetIndexInSkeleton() ?? -1 : _oneIndex; } }
    /// <summary>Selects joint one by nonnegative skeleton index, updating its path when attached.</summary><param name="boneIndex">Valid current index when bound; nonnegative deferred index otherwise.</param>
    public void SetJointOneBoneIndex(int boneIndex) { lock (ModificationGate) { EnsureModificationMutable(); var path = SkeletonIKBinding.SelectionPath(BoundSkeleton, boneIndex); _oneIndex = boneIndex; _onePath = path; } EmitChanged(); }
    /// <summary>Returns the relative bone path for joint two.</summary><returns>Empty without a path selection.</returns>
    public string GetJointTwoBoneNode() { lock (ModificationGate) { ThrowIfDisposed(); return _twoPath; } }
    /// <summary>Selects joint two by a bone path relative to its skeleton.</summary><param name="boneNode">Bounded scene path; empty uses its stored index.</param>
    public void SetJointTwoBoneNode(string boneNode) { SkeletonIKBinding.Path(boneNode); lock (ModificationGate) { EnsureModificationMutable(); _twoPath = boneNode; } EmitChanged(); }
    /// <summary>Returns the current resolved or authored index of joint two.</summary><returns>Minus one when its path is missing or no index was authored.</returns>
    public int GetJointTwoBoneIndex() { lock (ModificationGate) { ThrowIfDisposed(); return BoundSkeleton is { IsInsideTree: true } owner && _twoPath.Length > 0 ? _two.Bone(owner, _twoPath, _twoIndex)?.GetIndexInSkeleton() ?? -1 : _twoIndex; } }
    /// <summary>Selects joint two by nonnegative skeleton index, updating its path when attached.</summary><param name="boneIndex">Valid current index when bound; nonnegative deferred index otherwise.</param>
    public void SetJointTwoBoneIndex(int boneIndex) { lock (ModificationGate) { EnsureModificationMutable(); var path = SkeletonIKBinding.SelectionPath(BoundSkeleton, boneIndex); _twoIndex = boneIndex; _twoPath = path; } EmitChanged(); }
    /// <inheritdoc />
    protected override void OnExecute(double delta)
    {
        var stack = GetModificationStack(); var owner = BoundSkeleton; if (stack is null || owner is not { IsInsideTree: true }) return;
        lock (ModificationGate)
        {
            if (_target.Resolve(owner, _targetPath) is not Entity target || _one.Bone(owner, _onePath, _oneIndex) is not { } one || _two.Bone(owner, _twoPath, _twoIndex) is not { } two || ReferenceEquals(one, two)) return;
            var a = (double)SkeletonIKBinding.Length(one); var b = (double)SkeletonIKBinding.Length(two); if (a <= 0 || b <= 0) return;
            var distance = SkeletonIKBinding.Distance(one.GlobalPosition, target.GlobalPosition); var direction = SkeletonIKBinding.Direction(one.GlobalPosition, target.GlobalPosition, SkeletonIKBinding.Axis(one));
            distance = Math.Max(distance, _minimum); if (_maximum > 0) distance = Math.Min(distance, _maximum);
            var side = _flip ? -1 : 1; var handed = SkeletonIKBinding.Handed(one); var angle = direction.Angle();
            if (distance >= a + b) { SkeletonIKBinding.Aim(one, angle); SkeletonIKBinding.Aim(two, angle); }
            else
            {
                distance = Math.Max(distance, Math.Abs(a - b));
                var first = distance == 0 ? Math.PI / 2 : Math.Acos(Math.Clamp((distance * distance + a * a - b * b) / (2 * distance * a), -1, 1));
                var second = Math.Acos(Math.Clamp((a * a + b * b - distance * distance) / (2 * a * b), -1, 1));
                SkeletonIKBinding.Aim(one, (float)(angle - handed * side * first));
                two.Rotation = (float)(side * (Math.PI - second)) - two.GetBoneAngle() + one.GetBoneAngle();
            }
            SkeletonIKBinding.Request(owner, one, stack.Strength); SkeletonIKBinding.Request(owner, two, stack.Strength);
        }
    }
    private static readonly PropertyDescriptor[] IKProperties =
    [
        new PropertyDescriptor<SkeletonModificationTwoBoneIK, string>(nameof(TargetNodePath), m => m.TargetNodePath, (m, v) => m.TargetNodePath = v, _ => string.Empty, stored: true),
        new PropertyDescriptor<SkeletonModificationTwoBoneIK, float>(nameof(TargetMinimumDistance), m => m.TargetMinimumDistance, (m, v) => m.TargetMinimumDistance = v, _ => 0, stored: true),
        new PropertyDescriptor<SkeletonModificationTwoBoneIK, float>(nameof(TargetMaximumDistance), m => m.TargetMaximumDistance, (m, v) => m.TargetMaximumDistance = v, _ => 0, stored: true),
        new PropertyDescriptor<SkeletonModificationTwoBoneIK, bool>(nameof(FlipBendDirection), m => m.FlipBendDirection, (m, v) => m.FlipBendDirection = v, _ => false, stored: true),
        new PropertyDescriptor<SkeletonModificationTwoBoneIK, int>("_joint_one_index", m => m.GetJointOneBoneIndex(), (m, v) => { if (v == -1) { lock (m.ModificationGate) { m.EnsureModificationMutable(); m._oneIndex = -1; } } else m.SetJointOneBoneIndex(v); }, _ => -1, stored: true),
        new PropertyDescriptor<SkeletonModificationTwoBoneIK, string>("_joint_one_path", m => m.GetJointOneBoneNode(), (m, v) => m.SetJointOneBoneNode(v), _ => string.Empty, stored: true),
        new PropertyDescriptor<SkeletonModificationTwoBoneIK, int>("_joint_two_index", m => m.GetJointTwoBoneIndex(), (m, v) => { if (v == -1) { lock (m.ModificationGate) { m.EnsureModificationMutable(); m._twoIndex = -1; } } else m.SetJointTwoBoneIndex(v); }, _ => -1, stored: true),
        new PropertyDescriptor<SkeletonModificationTwoBoneIK, string>("_joint_two_path", m => m.GetJointTwoBoneNode(), (m, v) => m.SetJointTwoBoneNode(v), _ => string.Empty, stored: true),
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(IKProperties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new SkeletonModificationTwoBoneIK();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var copy = (SkeletonModificationTwoBoneIK)target; CopyModificationState(copy);
        lock (ModificationGate) { copy._targetPath = _targetPath; copy._onePath = _onePath; copy._twoPath = _twoPath; copy._oneIndex = _oneIndex; copy._twoIndex = _twoIndex; copy._minimum = _minimum; copy._maximum = _maximum; copy._flip = _flip; }
    }
}
