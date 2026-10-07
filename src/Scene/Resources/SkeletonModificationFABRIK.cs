namespace Electron2D;

/// <summary>Solves a bone chain through bounded forward/backward reaching with magnets and optional tip orientation.</summary>
/// <remarks>Joint settings are copied scene-independent resource data. Paths and indices resolve against the bound
/// Skeleton using weak revision-aware caches. Resizing/storage/copying are cold operations; prepared execution
/// retains scratch arrays and borrows scene nodes. Missing selections skip execution without authoring a pose.</remarks>
public sealed class SkeletonModificationFABRIK : SkeletonModification
{
    private struct Joint
    {
        internal string Path;
        internal int Index;
        internal SkeletonIKBinding Binding;
        internal Vector2 Magnet;
        internal bool TargetRotation;
        public Joint() { Path = string.Empty; Index = -1; Binding = new(); }
    }
    private Joint[] _joints = [];
    private readonly SkeletonIKBinding _target = new();
    private string _targetPath = string.Empty;
    /// <summary>Creates an empty enabled idle-phase solver.</summary>
    public SkeletonModificationFABRIK() { }
    /// <summary>Gets or resizes the number of authored joints, preserving the retained prefix.</summary><value>Zero initially; zero through 4096. New slots have no bone selection.</value>
    public int FABRIKDataChainLength { get { lock (ModificationGate) { ThrowIfDisposed(); return _joints.Length; } } set { lock (ModificationGate) { EnsureModificationMutable(); if (value < 0 || value > 4096) throw new ArgumentOutOfRangeException(nameof(value)); var old = _joints.Length; Array.Resize(ref _joints, value); for (var i = old; i < value; i++) _joints[i] = new(); } NotifyPropertyListChanged(); EmitChanged(); } }
    /// <summary>Gets or sets the Entity target path relative to its skeleton.</summary><value>Empty initially; missing targets skip execution.</value>
    public string TargetNodePath { get { lock (ModificationGate) { ThrowIfDisposed(); return _targetPath; } } set { SkeletonIKBinding.Path(value); lock (ModificationGate) { EnsureModificationMutable(); _targetPath = value; } EmitChanged(); } }
    private int Index(int jointIndex) { ThrowIfDisposed(); if ((uint)jointIndex >= (uint)_joints.Length) throw new ArgumentOutOfRangeException(nameof(jointIndex)); return jointIndex; }
    /// <summary>Returns a joint's authored relative bone path.</summary><param name="jointIndex">Valid joint slot.</param><returns>Empty for deferred numeric selection; attached numeric setters author a path.</returns>
    public string GetFABRIKJointBoneNode(int jointIndex) { lock (ModificationGate) return _joints[Index(jointIndex)].Path; }
    /// <summary>Selects a joint by relative bone path.</summary><param name="jointIndex">Valid joint slot.</param><param name="boneNode">Path relative to the skeleton; missing paths do not fall back to an old index.</param>
    public void SetFABRIKJointBoneNode(int jointIndex, string boneNode) { SkeletonIKBinding.Path(boneNode); lock (ModificationGate) { EnsureModificationMutable(); _joints[Index(jointIndex)].Path = boneNode; } EmitChanged(); }
    /// <summary>Returns a joint's resolved or authored bone index.</summary><param name="jointIndex">Valid joint slot.</param><returns>Minus one for unselected/missing paths.</returns>
    public int GetFABRIKJointBoneIndex(int jointIndex) { lock (ModificationGate) { var joint = _joints[Index(jointIndex)]; return BoundSkeleton is { IsInsideTree: true } owner && joint.Path.Length > 0 ? joint.Binding.Bone(owner, joint.Path, joint.Index)?.GetIndexInSkeleton() ?? -1 : joint.Index; } }
    /// <summary>Selects by nonnegative index and authors its path when attached.</summary><param name="jointIndex">Valid joint slot.</param><param name="boneIndex">Current valid skeleton index, or nonnegative deferred selection.</param>
    public void SetFABRIKJointBoneIndex(int jointIndex, int boneIndex) { lock (ModificationGate) { EnsureModificationMutable(); Index(jointIndex); var path = SkeletonIKBinding.SelectionPath(BoundSkeleton, boneIndex); _joints[jointIndex].Index = boneIndex; _joints[jointIndex].Path = path; } EmitChanged(); }
    /// <summary>Returns world-space positional bias applied on each backward pass; root remains anchored.</summary><param name="jointIndex">Valid joint slot.</param><returns>Authored setting; default Vector2.Zero.</returns>
    public Vector2 GetFABRIKJointMagnetPosition(int jointIndex) { lock (ModificationGate) return _joints[Index(jointIndex)].Magnet; }
    /// <summary>Sets world-space positional bias applied on each backward pass; root remains anchored.</summary><param name="jointIndex">Valid joint slot.</param><param name="value">Finite canvas-world pixel vector.</param>
    public void SetFABRIKJointMagnetPosition(int jointIndex, Vector2 value) { if (!value.IsFinite()) throw new ArgumentException("Magnet must be finite.", nameof(value)); lock (ModificationGate) { EnsureModificationMutable(); _joints[Index(jointIndex)].Magnet = value; } EmitChanged(); }
    /// <summary>Returns whether the final joint uses target orientation; other slots retain the value for later chain edits.</summary><param name="jointIndex">Valid joint slot.</param><returns>Authored setting; default false.</returns>
    public bool GetFABRIKJointUseTargetRotation(int jointIndex) { lock (ModificationGate) return _joints[Index(jointIndex)].TargetRotation; }
    /// <summary>Sets whether the final joint uses target orientation; other slots retain the value for later chain edits.</summary><param name="jointIndex">Valid joint slot.</param><param name="value">Requested policy.</param>
    public void SetFABRIKJointUseTargetRotation(int jointIndex, bool value) { lock (ModificationGate) { EnsureModificationMutable(); _joints[Index(jointIndex)].TargetRotation = value; } EmitChanged(); }
    private Vector2[] _points = [], _axes = [];
    private float[] _lengths = [];
    private Bone?[] _bones = [];
    private bool[] _seen = [];
    /// <inheritdoc />
    protected override void OnExecute(double delta)
    {
        var stack = GetModificationStack(); var owner = BoundSkeleton; if (stack is null || owner is not { IsInsideTree: true }) return;
        lock (ModificationGate)
        {
            var count = _joints.Length; if (count < 2 || _target.Resolve(owner, _targetPath) is not Entity target) return;
            if (_points.Length < count + 1) { _points = new Vector2[count + 1]; _axes = new Vector2[count]; _lengths = new float[count]; _bones = new Bone?[count]; }
            try
            {
                var boneCount = owner.GetBoneCount(); if (_seen.Length < boneCount) Array.Resize(ref _seen, boneCount); Array.Clear(_seen, 0, boneCount);
                for (var i = 0; i < count; i++)
                {
                    var joint = _joints[i]; var bone = joint.Binding.Bone(owner, joint.Path, joint.Index); if (bone is null || bone.GlobalTransform.Determinant() == 0) return;
                    var index = bone.GetIndexInSkeleton(); if (_seen[index]) return; _seen[index] = true;
                    _bones[i] = bone; _lengths[i] = SkeletonIKBinding.Length(bone); if (_lengths[i] == 0) return; _points[i] = bone.GlobalPosition; _axes[i] = SkeletonIKBinding.Axis(bone);
                }
                var origin = _points[0]; var goal = target.GlobalPosition; var last = count - 1; var oriented = _joints[last].TargetRotation; var tipDirection = new Vector2(MathF.Cos(target.GlobalRotation), MathF.Sin(target.GlobalRotation));
                _points[count] = SkeletonIKBinding.Point(_points[last], _axes[last], _lengths[last]);
                for (var iteration = 0; iteration < 10 && SkeletonIKBinding.Distance(_points[count], goal) > .01; iteration++)
                {
                    _points[count] = goal;
                    for (var i = last; i >= 0; i--)
                    {
                        var biased = i == 0 ? _points[i] : SkeletonIKBinding.Point(_points[i], _joints[i].Magnet, 1);
                        var backwards = i == last && oriented ? -tipDirection : SkeletonIKBinding.Direction(_points[i + 1], biased, -_axes[i]);
                        _points[i] = SkeletonIKBinding.Point(_points[i + 1], backwards, _lengths[i]);
                    }
                    _points[0] = origin;
                    for (var i = 0; i < count; i++)
                    {
                        var forwards = i == last && oriented ? tipDirection : SkeletonIKBinding.Direction(_points[i], _points[i + 1], _axes[i]);
                        _points[i + 1] = SkeletonIKBinding.Point(_points[i], forwards, _lengths[i]);
                    }
                }
                for (var i = 0; i < count; i++)
                {
                    var bone = _bones[i]!; bone.GlobalPosition = _points[i]; var direction = i == last && oriented ? tipDirection : SkeletonIKBinding.Direction(_points[i], _points[i + 1], _axes[i]); SkeletonIKBinding.Aim(bone, direction.Angle()); SkeletonIKBinding.Request(owner, bone, stack.Strength);
                }
            }
            finally { Array.Clear(_bones); }
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
                writer.Write(joint.Magnet.X); writer.Write(joint.Magnet.Y); writer.Write(joint.TargetRotation);
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
                joint.Magnet = new(reader.ReadSingle(), reader.ReadSingle()); joint.TargetRotation = reader.ReadBoolean(); if (!joint.Magnet.IsFinite()) throw new InvalidDataException("Invalid IK magnet.");
                joints[i] = joint;
            }
            if (stream.Position != stream.Length) throw new InvalidDataException("Trailing IK joint bytes."); _joints = joints;
        }
    }
    private static readonly PropertyDescriptor[] IKProperties =
    [
        new PropertyDescriptor<SkeletonModificationFABRIK, int>(nameof(FABRIKDataChainLength), m => m.FABRIKDataChainLength, (m, v) => m.FABRIKDataChainLength = v, _ => 0),
        new PropertyDescriptor<SkeletonModificationFABRIK, string>(nameof(TargetNodePath), m => m.TargetNodePath, (m, v) => m.TargetNodePath = v, _ => string.Empty, stored: true),
        new PropertyDescriptor<SkeletonModificationFABRIK, byte[]>("_joints", m => m.SaveJoints(), (m, v) => m.LoadJoints(v), _ => [], stored: true)
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(IKProperties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new SkeletonModificationFABRIK();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var copy = (SkeletonModificationFABRIK)target; CopyModificationState(copy); lock (ModificationGate) { copy._targetPath = _targetPath; copy.LoadJoints(SaveJoints()); }
    }
}
