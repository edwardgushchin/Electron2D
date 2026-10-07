namespace Electron2D;

/// <summary>Aims a selected skeletal bone at a scene target using endpoint direction and optional angular limits.</summary>
/// <remarks>Paths resolve relative to the bound Skeleton and cache by scene-path/setup revision. Missing targets
/// skip execution until the binding becomes valid. Configuration is copied and stored; scene references are weak.</remarks>
public sealed class SkeletonModificationLookAt : SkeletonModification
{
    private int _boneIndex = -1;
    private string _boneNode = string.Empty, _targetPath = string.Empty;
    private float _additional, _minimum, _maximum = Mathf.Tau;
    private bool _constraint, _invert, _local = true, _cacheDirty = true;
    private ulong _pathRevision, _setupRevision;
    private WeakReference<Entity>? _target;
    private WeakReference<Bone>? _bone;
    /// <summary>Creates an unselected idle look-at modification.</summary>
    public SkeletonModificationLookAt() { }
    /// <summary>Gets or sets the explicit bone index when BoneNode is empty.</summary><value>Minus one initially.</value>
    public int BoneIndex { get { lock (ModificationGate) { ThrowIfDisposed(); return _boneIndex; } } set { lock (ModificationGate) { EnsureModificationMutable(); if (value < -1) throw new ArgumentOutOfRangeException(nameof(value)); _boneIndex = value; _boneNode = string.Empty; _cacheDirty = true; } EmitChanged(); } }
    /// <summary>Gets or sets the relative bone path, overriding a numeric selection.</summary><value>Empty initially.</value>
    public string BoneNode { get { lock (ModificationGate) { ThrowIfDisposed(); return _boneNode; } } set { Path(value); lock (ModificationGate) { EnsureModificationMutable(); _boneNode = value; _cacheDirty = true; } EmitChanged(); } }
    /// <summary>Gets or sets the target path relative to its skeleton.</summary><value>Empty initially; target must be a live Entity.</value>
    public string TargetNodePath { get { lock (ModificationGate) { ThrowIfDisposed(); return _targetPath; } } set { Path(value); lock (ModificationGate) { EnsureModificationMutable(); _targetPath = value; _cacheDirty = true; } EmitChanged(); } }
    /// <summary>Gets or sets whether limits are relative to the bone's parent or world basis.</summary><value>True initially; typed projection of the stored constraint_in_localspace setting.</value>
    public bool ConstraintInLocalSpace { get { lock (ModificationGate) { ThrowIfDisposed(); return _local; } } set { lock (ModificationGate) { EnsureModificationMutable(); _local = value; } EmitChanged(); } }
    private static void Path(string path) { ArgumentNullException.ThrowIfNull(path); if (path.Length > 65536 || path.Contains('\0')) throw new ArgumentException("Invalid scene path.", nameof(path)); }
    private void Scalar(float value) { EnsureModificationMutable(); if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); }
    /// <summary>Returns additional aiming rotation.</summary><returns>Radians, zero initially.</returns>
    public float GetAdditionalRotation() { lock (ModificationGate) { ThrowIfDisposed(); return _additional; } }
    /// <summary>Sets additional finite aiming rotation.</summary><param name="rotation">Radians.</param>
    public void SetAdditionalRotation(float rotation) { lock (ModificationGate) { Scalar(rotation); _additional = rotation; } EmitChanged(); }
    /// <summary>Returns whether angle constraints are enabled.</summary><returns>False initially.</returns>
    public bool GetEnableConstraint() { lock (ModificationGate) { ThrowIfDisposed(); return _constraint; } }
    /// <summary>Enables angle constraints.</summary><param name="enableConstraint">Whether to clamp the resulting angle.</param>
    public void SetEnableConstraint(bool enableConstraint) { lock (ModificationGate) { EnsureModificationMutable(); _constraint = enableConstraint; } EmitChanged(); }
    /// <summary>Returns the lower angle boundary.</summary><returns>Zero radians initially.</returns>
    public float GetConstraintAngleMin() { lock (ModificationGate) { ThrowIfDisposed(); return _minimum; } }
    /// <summary>Sets the finite lower angle boundary.</summary><param name="angle">Radians.</param>
    public void SetConstraintAngleMin(float angle) { lock (ModificationGate) { Scalar(angle); _minimum = angle; } EmitChanged(); }
    /// <summary>Returns the upper angle boundary.</summary><returns>One full turn initially.</returns>
    public float GetConstraintAngleMax() { lock (ModificationGate) { ThrowIfDisposed(); return _maximum; } }
    /// <summary>Sets the finite upper angle boundary.</summary><param name="angle">Radians.</param>
    public void SetConstraintAngleMax(float angle) { lock (ModificationGate) { Scalar(angle); _maximum = angle; } EmitChanged(); }
    /// <summary>Returns whether the permitted interval is inverted.</summary><returns>False initially.</returns>
    public bool GetConstraintAngleInvert() { lock (ModificationGate) { ThrowIfDisposed(); return _invert; } }
    /// <summary>Selects the complement of the angle interval.</summary><param name="invert">Whether to invert its interior.</param>
    public void SetConstraintAngleInvert(bool invert) { lock (ModificationGate) { EnsureModificationMutable(); _invert = invert; } EmitChanged(); }
    /// <inheritdoc />
    protected override void OnSetupModification(SkeletonModificationStack modificationStack) { _cacheDirty = true; _target = null; _bone = null; }
    /// <inheritdoc />
    protected override void OnExecute(double delta)
    {
        var stack = GetModificationStack(); var skeleton = stack?.GetSkeleton(); if (skeleton is null || skeleton.Tree is null) return;
        lock (ModificationGate)
        {
            var setup = skeleton.SetupGeneration; var path = skeleton.Tree.PathRevision;
            if (_cacheDirty || setup != _setupRevision || path != _pathRevision)
            {
                var selected = _boneNode.Length > 0 ? skeleton.GetNodeOrNull(_boneNode) as Bone : _boneIndex >= 0 && _boneIndex < skeleton.GetBoneCount() ? skeleton.GetBone(_boneIndex) : null;
                var target = _targetPath.Length > 0 ? skeleton.GetNodeOrNull(_targetPath) as Entity : null;
                _bone = selected is null ? null : new(selected); _target = target is null ? null : new(target); _cacheDirty = false; _setupRevision = setup; _pathRevision = path;
            }
            if (_bone is null || !_bone.TryGetTarget(out var bone) || bone.IsDisposed || !ReferenceEquals(bone.SkeletonOwner, skeleton) || _target is null || !_target.TryGetTarget(out var targetNode) || targetNode.IsDisposed || !ReferenceEquals(targetNode.Tree, skeleton.Tree)) return;
            var angle = bone.GetAngleTo(targetNode.GlobalPosition) - bone.GetBoneAngle() + _additional;
            if (_constraint)
            {
                var result = ClampAngle(angle + (_local ? bone.Rotation : bone.GlobalRotation), _minimum, _maximum, _invert);
                if (_local) bone.Rotation = result; else bone.GlobalRotation = result;
            }
            else bone.Rotation += angle;
            skeleton.SetBoneLocalPoseOverride(bone.GetIndexInSkeleton(), bone.Transform, stack!.Strength, persistent: false);
        }
    }
    private static readonly PropertyDescriptor[] LookAtProperties =
    [
        new PropertyDescriptor<SkeletonModificationLookAt, int>(nameof(BoneIndex), m => m.BoneIndex, (m, v) => m.BoneIndex = v, _ => -1, stored: true),
        new PropertyDescriptor<SkeletonModificationLookAt, string>(nameof(BoneNode), m => m.BoneNode, (m, v) => m.BoneNode = v, _ => string.Empty, stored: true),
        new PropertyDescriptor<SkeletonModificationLookAt, string>(nameof(TargetNodePath), m => m.TargetNodePath, (m, v) => m.TargetNodePath = v, _ => string.Empty, stored: true),
        new PropertyDescriptor<SkeletonModificationLookAt, bool>(nameof(ConstraintInLocalSpace), m => m.ConstraintInLocalSpace, (m, v) => m.ConstraintInLocalSpace = v, _ => true, stored: true),
        new PropertyDescriptor<SkeletonModificationLookAt, float>("additional_rotation", m => m.GetAdditionalRotation(), (m, v) => m.SetAdditionalRotation(v), _ => 0, stored: true),
        new PropertyDescriptor<SkeletonModificationLookAt, bool>("enable_constraint", m => m.GetEnableConstraint(), (m, v) => m.SetEnableConstraint(v), _ => false, stored: true),
        new PropertyDescriptor<SkeletonModificationLookAt, float>("constraint_angle_min", m => m.GetConstraintAngleMin(), (m, v) => m.SetConstraintAngleMin(v), _ => 0, stored: true),
        new PropertyDescriptor<SkeletonModificationLookAt, float>("constraint_angle_max", m => m.GetConstraintAngleMax(), (m, v) => m.SetConstraintAngleMax(v), _ => Mathf.Tau, stored: true),
        new PropertyDescriptor<SkeletonModificationLookAt, bool>("constraint_angle_invert", m => m.GetConstraintAngleInvert(), (m, v) => m.SetConstraintAngleInvert(v), _ => false, stored: true)
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(LookAtProperties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new SkeletonModificationLookAt();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var copy = (SkeletonModificationLookAt)target; CopyModificationState(copy);
        lock (ModificationGate) { copy._boneIndex = _boneIndex; copy._boneNode = _boneNode; copy._targetPath = _targetPath; copy._additional = _additional; copy._minimum = _minimum; copy._maximum = _maximum; copy._constraint = _constraint; copy._invert = _invert; copy._local = _local; }
    }
    /// <inheritdoc />
    protected override void OnResetState() { base.OnResetState(); _cacheDirty = true; _target = null; _bone = null; }
}
