namespace Electron2D;

/// <summary>Executes one authored-order cyclic coordinate descent pass over a bone chain and explicit tip.</summary>
/// <remarks>Joint settings are copied scene-independent resource data. Paths and indices resolve against the bound
/// Skeleton using weak revision-aware caches. Resizing/storage/copying are cold operations; prepared execution
/// retains scratch arrays and borrows scene nodes. Missing selections skip execution without authoring a pose.</remarks>
public sealed class SkeletonModificationCCDIK : SkeletonModification
{
    private struct Joint
    {
        internal string Path;
        internal int Index;
        internal SkeletonIKBinding Binding;
        internal bool RotateFromJoint, EnableConstraint, Invert, Local;
        internal float Minimum, Maximum;
        public Joint() { Path = string.Empty; Index = -1; Binding = new(); Local = true; Maximum = Mathf.Tau; }
    }
    private Joint[] _joints = [];
    private readonly SkeletonIKBinding _target = new();
    private string _targetPath = string.Empty;
    /// <summary>Creates an empty enabled idle-phase solver.</summary>
    public SkeletonModificationCCDIK() { }
    /// <summary>Gets or resizes the number of authored joints, preserving the retained prefix.</summary><value>Zero initially; zero through 4096. New slots have no bone selection.</value>
    public int CCDIKDataChainLength { get { lock (ModificationGate) { ThrowIfDisposed(); return _joints.Length; } } set { lock (ModificationGate) { EnsureModificationMutable(); if (value < 0 || value > 4096) throw new ArgumentOutOfRangeException(nameof(value)); var old = _joints.Length; Array.Resize(ref _joints, value); for (var i = old; i < value; i++) _joints[i] = new(); } NotifyPropertyListChanged(); EmitChanged(); } }
    /// <summary>Gets or sets the Entity target path relative to its skeleton.</summary><value>Empty initially; missing targets skip execution.</value>
    public string TargetNodePath { get { lock (ModificationGate) { ThrowIfDisposed(); return _targetPath; } } set { SkeletonIKBinding.Path(value); lock (ModificationGate) { EnsureModificationMutable(); _targetPath = value; } EmitChanged(); } }
    private readonly SkeletonIKBinding _tip = new();
    private string _tipPath = string.Empty;
    /// <summary>Gets or sets the explicit endpoint path relative to its skeleton.</summary><value>Empty initially. Typically an Entity child of the final bone.</value>
    public string TipNodePath { get { lock (ModificationGate) { ThrowIfDisposed(); return _tipPath; } } set { SkeletonIKBinding.Path(value); lock (ModificationGate) { EnsureModificationMutable(); _tipPath = value; } EmitChanged(); } }
    private int Index(int jointIndex) { ThrowIfDisposed(); if ((uint)jointIndex >= (uint)_joints.Length) throw new ArgumentOutOfRangeException(nameof(jointIndex)); return jointIndex; }
    /// <summary>Returns a joint's authored relative bone path.</summary><param name="jointIndex">Valid joint slot.</param><returns>Empty for deferred numeric selection; attached numeric setters author a path.</returns>
    public string GetCCDIKJointBoneNode(int jointIndex) { lock (ModificationGate) return _joints[Index(jointIndex)].Path; }
    /// <summary>Selects a joint by relative bone path.</summary><param name="jointIndex">Valid joint slot.</param><param name="boneNode">Path relative to the skeleton; missing paths do not fall back to an old index.</param>
    public void SetCCDIKJointBoneNode(int jointIndex, string boneNode) { SkeletonIKBinding.Path(boneNode); lock (ModificationGate) { EnsureModificationMutable(); _joints[Index(jointIndex)].Path = boneNode; } EmitChanged(); }
    /// <summary>Returns a joint's resolved or authored bone index.</summary><param name="jointIndex">Valid joint slot.</param><returns>Minus one for unselected/missing paths.</returns>
    public int GetCCDIKJointBoneIndex(int jointIndex) { lock (ModificationGate) { var joint = _joints[Index(jointIndex)]; return BoundSkeleton is { IsInsideTree: true } owner && joint.Path.Length > 0 ? joint.Binding.Bone(owner, joint.Path, joint.Index)?.GetIndexInSkeleton() ?? -1 : joint.Index; } }
    /// <summary>Selects by nonnegative index and authors its path when attached.</summary><param name="jointIndex">Valid joint slot.</param><param name="boneIndex">Current valid skeleton index, or nonnegative deferred selection.</param>
    public void SetCCDIKJointBoneIndex(int jointIndex, int boneIndex) { lock (ModificationGate) { EnsureModificationMutable(); Index(jointIndex); var path = SkeletonIKBinding.SelectionPath(BoundSkeleton, boneIndex); _joints[jointIndex].Index = boneIndex; _joints[jointIndex].Path = path; } EmitChanged(); }
    /// <summary>Returns whether to aim the bone endpoint directly instead of rotating the tip-to-target offset.</summary><param name="jointIndex">Valid joint slot.</param><returns>Authored setting; default false.</returns>
    public bool GetCCDIKJointRotateFromJoint(int jointIndex) { lock (ModificationGate) return _joints[Index(jointIndex)].RotateFromJoint; }
    /// <summary>Sets whether to aim the bone endpoint directly instead of rotating the tip-to-target offset.</summary><param name="jointIndex">Valid joint slot.</param><param name="value">Requested policy.</param>
    public void SetCCDIKJointRotateFromJoint(int jointIndex, bool value) { lock (ModificationGate) { EnsureModificationMutable(); _joints[Index(jointIndex)].RotateFromJoint = value; } EmitChanged(); }
    /// <summary>Returns whether joint angle limits apply.</summary><param name="jointIndex">Valid joint slot.</param><returns>Authored setting; default false.</returns>
    public bool GetCCDIKJointEnableConstraint(int jointIndex) { lock (ModificationGate) return _joints[Index(jointIndex)].EnableConstraint; }
    /// <summary>Sets whether joint angle limits apply.</summary><param name="jointIndex">Valid joint slot.</param><param name="value">Requested policy.</param>
    public void SetCCDIKJointEnableConstraint(int jointIndex, bool value) { lock (ModificationGate) { EnsureModificationMutable(); _joints[Index(jointIndex)].EnableConstraint = value; } EmitChanged(); }
    /// <summary>Returns lower angular boundary in radians.</summary><param name="jointIndex">Valid joint slot.</param><returns>Authored setting; default 0.</returns>
    public float GetCCDIKJointConstraintAngleMin(int jointIndex) { lock (ModificationGate) return _joints[Index(jointIndex)].Minimum; }
    /// <summary>Sets lower angular boundary in radians.</summary><param name="jointIndex">Valid joint slot.</param><param name="value">Finite radians.</param>
    public void SetCCDIKJointConstraintAngleMin(int jointIndex, float value) { SkeletonIKBinding.Scalar(value); lock (ModificationGate) { EnsureModificationMutable(); _joints[Index(jointIndex)].Minimum = value; } EmitChanged(); }
    /// <summary>Returns upper angular boundary in radians.</summary><param name="jointIndex">Valid joint slot.</param><returns>Authored setting; default Tau.</returns>
    public float GetCCDIKJointConstraintAngleMax(int jointIndex) { lock (ModificationGate) return _joints[Index(jointIndex)].Maximum; }
    /// <summary>Sets upper angular boundary in radians.</summary><param name="jointIndex">Valid joint slot.</param><param name="value">Finite radians.</param>
    public void SetCCDIKJointConstraintAngleMax(int jointIndex, float value) { SkeletonIKBinding.Scalar(value); lock (ModificationGate) { EnsureModificationMutable(); _joints[Index(jointIndex)].Maximum = value; } EmitChanged(); }
    /// <summary>Returns whether the permitted angle interval is inverted.</summary><param name="jointIndex">Valid joint slot.</param><returns>Authored setting; default false.</returns>
    public bool GetCCDIKJointConstraintAngleInvert(int jointIndex) { lock (ModificationGate) return _joints[Index(jointIndex)].Invert; }
    /// <summary>Sets whether the permitted angle interval is inverted.</summary><param name="jointIndex">Valid joint slot.</param><param name="value">Requested policy.</param>
    public void SetCCDIKJointConstraintAngleInvert(int jointIndex, bool value) { lock (ModificationGate) { EnsureModificationMutable(); _joints[Index(jointIndex)].Invert = value; } EmitChanged(); }
    /// <summary>Returns whether limits use parent-relative rotation rather than canvas-world rotation.</summary><param name="jointIndex">Valid joint slot.</param><returns>Authored setting; default true.</returns>
    public bool GetCCDIKJointConstraintInLocalSpace(int jointIndex) { lock (ModificationGate) return _joints[Index(jointIndex)].Local; }
    /// <summary>Sets whether limits use parent-relative rotation rather than canvas-world rotation.</summary><param name="jointIndex">Valid joint slot.</param><param name="value">Requested policy.</param>
    public void SetCCDIKJointConstraintInLocalSpace(int jointIndex, bool value) { lock (ModificationGate) { EnsureModificationMutable(); _joints[Index(jointIndex)].Local = value; } EmitChanged(); }
    /// <inheritdoc />
    protected override void OnExecute(double delta)
    {
        var stack = GetModificationStack(); var owner = BoundSkeleton; if (stack is null || owner is not { IsInsideTree: true }) return;
        lock (ModificationGate)
        {
            if (_target.Resolve(owner, _targetPath) is not Entity target || _tip.Resolve(owner, _tipPath) is not Entity tip) return;
            for (var i = 0; i < _joints.Length; i++)
            {
                var joint = _joints[i]; if (joint.Binding.Bone(owner, joint.Path, joint.Index) is not { } bone || bone.GlobalTransform.Determinant() == 0) continue;
                if (joint.RotateFromJoint) SkeletonIKBinding.Aim(bone, SkeletonIKBinding.Direction(bone.GlobalPosition, target.GlobalPosition, SkeletonIKBinding.Axis(bone)).Angle());
                else
                {
                    if (SkeletonIKBinding.Distance(bone.GlobalPosition, tip.GlobalPosition) == 0 || SkeletonIKBinding.Distance(bone.GlobalPosition, target.GlobalPosition) == 0) continue;
                    var toTip = SkeletonIKBinding.Direction(bone.GlobalPosition, tip.GlobalPosition, Vector2.Right).Angle(); var toTarget = SkeletonIKBinding.Direction(bone.GlobalPosition, target.GlobalPosition, Vector2.Right).Angle(); bone.GlobalRotation += toTarget - toTip;
                }
                if (joint.EnableConstraint) { if (joint.Local) bone.Rotation = ClampAngle(bone.Rotation, joint.Minimum, joint.Maximum, joint.Invert); else bone.GlobalRotation = ClampAngle(bone.GlobalRotation, joint.Minimum, joint.Maximum, joint.Invert); }
                SkeletonIKBinding.Request(owner, bone, stack.Strength);
            }
        }
    }
    private byte[] SaveJoints()
    {
        lock (ModificationGate)
        {
            ThrowIfDisposed(); using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream); writer.Write(1); writer.Write(_joints.Length);
            foreach (var joint in _joints)
            {
                writer.Write(joint.Path); writer.Write(joint.Index);
                writer.Write(joint.RotateFromJoint); writer.Write(joint.EnableConstraint); writer.Write(joint.Minimum); writer.Write(joint.Maximum); writer.Write(joint.Invert); writer.Write(joint.Local);
                if (stream.Length > 64 * 1024 * 1024) throw new InvalidOperationException("IK archive exceeds its budget.");
            }
            return stream.ToArray();
        }
    }
    private void LoadJoints(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes); if (bytes.Length > 64 * 1024 * 1024) throw new InvalidDataException("IK archive exceeds its budget.");
        lock (ModificationGate)
        {
            EnsureModificationMutable(); if (bytes.Length == 0) { _joints = []; return; }
            using var stream = new MemoryStream(bytes, false); using var reader = new BinaryReader(stream); if (reader.ReadInt32() != 1) throw new InvalidDataException("Invalid IK archive version."); var count = reader.ReadInt32(); if (count < 0 || count > 4096) throw new InvalidDataException("Invalid IK joint count."); var joints = new Joint[count];
            for (var i = 0; i < count; i++)
            {
                var joint = new Joint { Path = reader.ReadString(), Index = reader.ReadInt32() }; SkeletonIKBinding.Path(joint.Path); if (joint.Index < -1) throw new InvalidDataException("Invalid IK bone selection.");
                joint.RotateFromJoint = reader.ReadBoolean(); joint.EnableConstraint = reader.ReadBoolean(); joint.Minimum = reader.ReadSingle(); joint.Maximum = reader.ReadSingle(); joint.Invert = reader.ReadBoolean(); joint.Local = reader.ReadBoolean(); SkeletonIKBinding.Scalar(joint.Minimum); SkeletonIKBinding.Scalar(joint.Maximum);
                joints[i] = joint;
            }
            if (stream.Position != stream.Length) throw new InvalidDataException("Trailing IK joint bytes."); _joints = joints;
        }
    }
    private static readonly PropertyDescriptor[] IKProperties =
    [
        new PropertyDescriptor<SkeletonModificationCCDIK, int>(nameof(CCDIKDataChainLength), m => m.CCDIKDataChainLength, (m, v) => m.CCDIKDataChainLength = v, _ => 0),
        new PropertyDescriptor<SkeletonModificationCCDIK, string>(nameof(TargetNodePath), m => m.TargetNodePath, (m, v) => m.TargetNodePath = v, _ => string.Empty, stored: true),
        new PropertyDescriptor<SkeletonModificationCCDIK, string>(nameof(TipNodePath), m => m.TipNodePath, (m, v) => m.TipNodePath = v, _ => string.Empty, stored: true),
        new PropertyDescriptor<SkeletonModificationCCDIK, byte[]>("_joints", m => m.SaveJoints(), (m, v) => m.LoadJoints(v), _ => [], stored: true)
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(IKProperties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new SkeletonModificationCCDIK();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var copy = (SkeletonModificationCCDIK)target; CopyModificationState(copy); lock (ModificationGate) { copy._targetPath = _targetPath; copy._tipPath = _tipPath; copy.LoadJoints(SaveJoints()); }
    }
}
