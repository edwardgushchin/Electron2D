namespace Electron2D;

/// <summary>Advances retained joint force/velocity toward a target and requests spring-like bone rotations.</summary>
/// <remarks>Joint data is copied resource configuration; scene selections are weak. Force uses finite seconds,
/// stiffness and optional gravity; mass must be positive and damping ranges from zero to one. Physics-only
/// collision queries revert a candidate to its last clear dynamic point. Reset/copy/binding reseed history.</remarks>
public sealed class SkeletonModificationJiggle : SkeletonModification
{
    private struct Joint
    {
        internal string Path;
        internal int Index;
        internal SkeletonIKBinding Binding;
        internal bool Override, UseGravity;
        internal float Stiffness, Mass, Damping;
        internal Vector2 Gravity;
        public Joint() { Path = string.Empty; Index = -1; Binding = new(); Stiffness = 3; Mass = .75f; Damping = .75f; Gravity = new(0, 6); }
    }
    private struct State
    {
        internal ulong BoneID, Setup;
        internal Vector2 Dynamic, LastOrigin, LastClear, Velocity, Acceleration, Force;
        internal bool Valid;
    }
    private Joint[] _joints = [];
    private State[] _states = [];
    private readonly SkeletonIKBinding _target = new();
    private string _targetPath = string.Empty;
    private float _stiffness = 3, _mass = .75f, _damping = .75f;
    private Vector2 _gravity = new(0, 6);
    private bool _useGravity, _colliders;
    private uint _mask = 1;
    /// <summary>Creates an empty enabled idle-phase jiggle resource with documented joint defaults.</summary>
    public SkeletonModificationJiggle() { }
    private static void PositiveMass(float value) { SkeletonIKBinding.Scalar(value); if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value), "Jiggle mass must be positive."); }
    private static void DampingValue(float value) { SkeletonIKBinding.Scalar(value, true); if (value > 1) throw new ArgumentOutOfRangeException(nameof(value)); }
    private static void GravityValue(Vector2 value) { if (!value.IsFinite()) throw new ArgumentException("Gravity must be finite.", nameof(value)); }
    private void Defaults(ref Joint joint) { if (!joint.Override) { joint.Stiffness = _stiffness; joint.Mass = _mass; joint.Damping = _damping; joint.Gravity = _gravity; joint.UseGravity = _useGravity; } }
    private void PropagateDefaults() { for (var i = 0; i < _joints.Length; i++) Defaults(ref _joints[i]); }
    /// <summary>Gets or resizes the joint list, retaining the prefix and seeding new slots from current defaults.</summary><value>Zero initially; zero through 4096. Structural changes clear simulation history.</value>
    public int JiggleDataChainLength { get { lock (ModificationGate) { ThrowIfDisposed(); return _joints.Length; } } set { lock (ModificationGate) { EnsureModificationMutable(); if (value < 0 || value > 4096) throw new ArgumentOutOfRangeException(nameof(value)); var old = _joints.Length; Array.Resize(ref _joints, value); _states = new State[value]; for (var i = old; i < value; i++) { _joints[i] = new(); Defaults(ref _joints[i]); } } NotifyPropertyListChanged(); EmitChanged(); } }
    /// <summary>Gets or sets the Entity target path relative to its skeleton.</summary><value>Empty initially. Missing targets freeze history and restore ordinary authored poses.</value>
    public string TargetNodePath { get { lock (ModificationGate) { ThrowIfDisposed(); return _targetPath; } } set { SkeletonIKBinding.Path(value); lock (ModificationGate) { EnsureModificationMutable(); _targetPath = value; } EmitChanged(); } }
    /// <summary>Gets or sets the default joint stiffness. Nonnegative force multiplier.</summary><value>3 initially; changes copy all defaults to nonoverriding joints.</value>
    public float Stiffness { get { lock (ModificationGate) { ThrowIfDisposed(); return _stiffness; } } set { SkeletonIKBinding.Scalar(value, true); lock (ModificationGate) { EnsureModificationMutable(); _stiffness = value; PropagateDefaults(); } EmitChanged(); } }
    /// <summary>Gets or sets the default joint mass. Positive force-to-acceleration divisor.</summary><value>.75 initially; changes copy all defaults to nonoverriding joints.</value>
    public float Mass { get { lock (ModificationGate) { ThrowIfDisposed(); return _mass; } } set { PositiveMass(value); lock (ModificationGate) { EnsureModificationMutable(); _mass = value; PropagateDefaults(); } EmitChanged(); } }
    /// <summary>Gets or sets the default joint damping. Acceleration contribution attenuation; velocity increments use one minus this value.</summary><value>.75 initially; changes copy all defaults to nonoverriding joints.</value>
    public float Damping { get { lock (ModificationGate) { ThrowIfDisposed(); return _damping; } } set { DampingValue(value); lock (ModificationGate) { EnsureModificationMutable(); _damping = value; PropagateDefaults(); } EmitChanged(); } }
    /// <summary>Gets or sets the default joint gravity. Optional finite canvas-world force vector.</summary><value>new Vector2(0, 6) initially; changes copy all defaults to nonoverriding joints.</value>
    public Vector2 Gravity { get { lock (ModificationGate) { ThrowIfDisposed(); return _gravity; } } set { GravityValue(value); lock (ModificationGate) { EnsureModificationMutable(); _gravity = value; PropagateDefaults(); } EmitChanged(); } }
    /// <summary>Gets or sets the default joint usegravity. Whether gravity contributes to each force step.</summary><value>false initially; changes copy all defaults to nonoverriding joints.</value>
    public bool UseGravity { get { lock (ModificationGate) { ThrowIfDisposed(); return _useGravity; } } set { lock (ModificationGate) { EnsureModificationMutable(); _useGravity = value; PropagateDefaults(); } EmitChanged(); } }
    /// <summary>Returns whether collider rejection is enabled.</summary><returns>False initially.</returns>
    public bool GetUseColliders() { lock (ModificationGate) { ThrowIfDisposed(); return _colliders; } }
    /// <summary>Enables physics-only collider rejection.</summary><param name="useColliders">Whether to ray-test candidate dynamic points.</param>
    /// <remarks>Execution with colliders in Idle rejects before advancing state. Select ProcessPhase.Physics.</remarks>
    public void SetUseColliders(bool useColliders) { lock (ModificationGate) { EnsureModificationMutable(); _colliders = useColliders; } NotifyPropertyListChanged(); EmitChanged(); }
    /// <summary>Returns the collider layer mask.</summary><returns>One initially; zero selects no collider.</returns>
    public uint GetCollisionMask() { lock (ModificationGate) { ThrowIfDisposed(); return _mask; } }
    /// <summary>Sets the unsigned 32-bit physics collision mask.</summary><param name="collisionMask">Layer bits, including zero.</param>
    public void SetCollisionMask(uint collisionMask) { lock (ModificationGate) { EnsureModificationMutable(); _mask = collisionMask; } EmitChanged(); }
    private int Index(int jointIndex) { ThrowIfDisposed(); if ((uint)jointIndex >= (uint)_joints.Length) throw new ArgumentOutOfRangeException(nameof(jointIndex)); return jointIndex; }
    /// <summary>Returns a joint's relative bone path.</summary><param name="jointIndex">Valid joint slot.</param><returns>Empty for deferred numeric selection; attached numeric setters author a path.</returns>
    public string GetJiggleJointBoneNode(int jointIndex) { lock (ModificationGate) return _joints[Index(jointIndex)].Path; }
    /// <summary>Selects a joint by relative bone path and discards its previous history.</summary><param name="jointIndex">Valid joint slot.</param><param name="boneNode">Path relative to its skeleton.</param>
    public void SetJiggleJointBoneNode(int jointIndex, string boneNode) { SkeletonIKBinding.Path(boneNode); lock (ModificationGate) { EnsureModificationMutable(); _joints[Index(jointIndex)].Path = boneNode; _states[jointIndex] = default; } EmitChanged(); }
    /// <summary>Returns the resolved or authored bone index.</summary><param name="jointIndex">Valid joint slot.</param><returns>Minus one for a missing/unselected path.</returns>
    public int GetJiggleJointBoneIndex(int jointIndex) { lock (ModificationGate) { var joint = _joints[Index(jointIndex)]; return BoundSkeleton is { IsInsideTree: true } owner && joint.Path.Length > 0 ? joint.Binding.Bone(owner, joint.Path, joint.Index)?.GetIndexInSkeleton() ?? -1 : joint.Index; } }
    /// <summary>Selects a bone by nonnegative index, authoring its path when attached and discarding history.</summary><param name="jointIndex">Valid joint slot.</param><param name="boneIndex">Current index when attached; nonnegative deferred index otherwise.</param>
    public void SetJiggleJointBoneIndex(int jointIndex, int boneIndex) { lock (ModificationGate) { EnsureModificationMutable(); Index(jointIndex); var path = SkeletonIKBinding.SelectionPath(BoundSkeleton, boneIndex); _joints[jointIndex].Index = boneIndex; _joints[jointIndex].Path = path; _states[jointIndex] = default; } EmitChanged(); }
    /// <summary>Returns whether this joint is protected from default propagation.</summary><param name="jointIndex">Valid joint slot.</param><returns>False initially.</returns>
    public bool GetJiggleJointOverride(int jointIndex) { lock (ModificationGate) return _joints[Index(jointIndex)].Override; }
    /// <summary>Authors default override policy; disabling copies current defaults immediately.</summary><param name="jointIndex">Valid joint slot.</param><param name="value">Whether to retain independent settings through default edits.</param>
    public void SetJiggleJointOverride(int jointIndex, bool value) { lock (ModificationGate) { EnsureModificationMutable(); _joints[Index(jointIndex)].Override = value; PropagateDefaults(); } NotifyPropertyListChanged(); EmitChanged(); }
    /// <summary>Returns the actual joint stiffness used by simulation.</summary><param name="jointIndex">Valid joint slot.</param><returns>Nonnegative force multiplier. Default 3.</returns>
    public float GetJiggleJointStiffness(int jointIndex) { lock (ModificationGate) return _joints[Index(jointIndex)].Stiffness; }
    /// <summary>Sets the actual joint stiffness; enable Override to retain it through default edits.</summary><param name="jointIndex">Valid joint slot.</param><param name="value">Nonnegative force multiplier.</param>
    public void SetJiggleJointStiffness(int jointIndex, float value) { SkeletonIKBinding.Scalar(value, true); lock (ModificationGate) { EnsureModificationMutable(); _joints[Index(jointIndex)].Stiffness = value; } EmitChanged(); }
    /// <summary>Returns the actual joint mass used by simulation.</summary><param name="jointIndex">Valid joint slot.</param><returns>Positive force-to-acceleration divisor. Default .75.</returns>
    public float GetJiggleJointMass(int jointIndex) { lock (ModificationGate) return _joints[Index(jointIndex)].Mass; }
    /// <summary>Sets the actual joint mass; enable Override to retain it through default edits.</summary><param name="jointIndex">Valid joint slot.</param><param name="value">Positive force-to-acceleration divisor.</param>
    public void SetJiggleJointMass(int jointIndex, float value) { PositiveMass(value); lock (ModificationGate) { EnsureModificationMutable(); _joints[Index(jointIndex)].Mass = value; } EmitChanged(); }
    /// <summary>Returns the actual joint damping used by simulation.</summary><param name="jointIndex">Valid joint slot.</param><returns>Acceleration contribution attenuation; velocity increments use one minus this value. Default .75.</returns>
    public float GetJiggleJointDamping(int jointIndex) { lock (ModificationGate) return _joints[Index(jointIndex)].Damping; }
    /// <summary>Sets the actual joint damping; enable Override to retain it through default edits.</summary><param name="jointIndex">Valid joint slot.</param><param name="value">Acceleration contribution attenuation; velocity increments use one minus this value.</param>
    public void SetJiggleJointDamping(int jointIndex, float value) { DampingValue(value); lock (ModificationGate) { EnsureModificationMutable(); _joints[Index(jointIndex)].Damping = value; } EmitChanged(); }
    /// <summary>Returns the actual joint gravity used by simulation.</summary><param name="jointIndex">Valid joint slot.</param><returns>Optional finite canvas-world force vector. Default new Vector2(0, 6).</returns>
    public Vector2 GetJiggleJointGravity(int jointIndex) { lock (ModificationGate) return _joints[Index(jointIndex)].Gravity; }
    /// <summary>Sets the actual joint gravity; enable Override to retain it through default edits.</summary><param name="jointIndex">Valid joint slot.</param><param name="value">Optional finite canvas-world force vector.</param>
    public void SetJiggleJointGravity(int jointIndex, Vector2 value) { GravityValue(value); lock (ModificationGate) { EnsureModificationMutable(); _joints[Index(jointIndex)].Gravity = value; } EmitChanged(); }
    /// <summary>Returns the actual joint usegravity used by simulation.</summary><param name="jointIndex">Valid joint slot.</param><returns>Whether gravity contributes to each force step. Default false.</returns>
    public bool GetJiggleJointUseGravity(int jointIndex) { lock (ModificationGate) return _joints[Index(jointIndex)].UseGravity; }
    /// <summary>Sets the actual joint usegravity; enable Override to retain it through default edits.</summary><param name="jointIndex">Valid joint slot.</param><param name="value">Whether gravity contributes to each force step.</param>
    public void SetJiggleJointUseGravity(int jointIndex, bool value) { lock (ModificationGate) { EnsureModificationMutable(); _joints[Index(jointIndex)].UseGravity = value; } EmitChanged(); }
    private static State Seed(Bone bone, ulong setup) { var position = bone.GlobalPosition; return new State { Valid = true, BoneID = bone.InstanceID, Setup = setup, Dynamic = position, LastOrigin = position, LastClear = position }; }
    /// <summary>Reseeds every live selected joint at its current bone origin, clearing force, acceleration and velocity.</summary>
    /// <remarks>Unbound/missing joints remain invalid and seed on their next valid execution. Existing poses are not rewritten.</remarks>
    public void Reset()
    {
        lock (ModificationGate)
        {
            EnsureModificationMutable(); Array.Clear(_states); if (BoundSkeleton is not { IsInsideTree: true } owner) return; var setup = owner.SetupGeneration;
            for (var i = 0; i < _joints.Length; i++) if (_joints[i].Binding.Bone(owner, _joints[i].Path, _joints[i].Index) is { } bone) _states[i] = Seed(bone, setup);
        }
    }
    /// <inheritdoc />
    protected override void OnSetupModification(SkeletonModificationStack modificationStack) => Reset();
    internal override void OnUnbindStack() { lock (ModificationGate) Array.Clear(_states); }
    private static Vector2 Finite(double x, double y) { var point = new Vector2((float)x, (float)y); if (!point.IsFinite()) throw new InvalidOperationException("Jiggle state overflowed."); return point; }
    /// <inheritdoc />
    protected override void OnExecute(double delta)
    {
        var stack = GetModificationStack(); var owner = BoundSkeleton; if (stack is null || owner is not { IsInsideTree: true }) return;
        lock (ModificationGate)
        {
            if (_colliders && ExecutionMode != ProcessPhase.Physics) throw new InvalidOperationException("Jiggle collision queries require Physics execution.");
            if (_target.Resolve(owner, _targetPath) is not Entity target) return; var setup = owner.SetupGeneration; var destination = target.GlobalPosition;
            var direct = _colliders ? owner.GetWorld()!.DirectSpaceState : null;
            for (var i = 0; i < _joints.Length; i++)
            {
                var joint = _joints[i]; var bone = joint.Binding.Bone(owner, joint.Path, joint.Index); if (bone is null || bone.GlobalTransform.Determinant() == 0) { _states[i] = default; continue; }
                var state = _states[i]; if (!state.Valid || state.BoneID != bone.InstanceID || state.Setup != setup) state = Seed(bone, setup);
                var gravity = joint.UseGravity ? joint.Gravity : Vector2.Zero;
                state.Force = Finite(((double)destination.X - state.Dynamic.X) * joint.Stiffness * delta + gravity.X * delta, ((double)destination.Y - state.Dynamic.Y) * joint.Stiffness * delta + gravity.Y * delta);
                state.Acceleration = Finite(state.Force.X / (double)joint.Mass, state.Force.Y / (double)joint.Mass);
                state.Velocity = Finite(state.Velocity.X + state.Acceleration.X * (double)(1 - joint.Damping), state.Velocity.Y + state.Acceleration.Y * (double)(1 - joint.Damping));
                var origin = bone.GlobalPosition;
                state.Dynamic = Finite(state.Dynamic.X + (double)state.Velocity.X + state.Force.X + origin.X - state.LastOrigin.X, state.Dynamic.Y + (double)state.Velocity.Y + state.Force.Y + origin.Y - state.LastOrigin.Y); state.LastOrigin = origin;
                if (direct is not null && direct.IntersectRay(origin, state.Dynamic, _mask, [], false, true, false) is not null) { state.Dynamic = state.LastClear; state.Velocity = Vector2.Zero; state.Acceleration = Vector2.Zero; }
                else if (direct is not null) state.LastClear = state.Dynamic;
                if (SkeletonIKBinding.Distance(origin, state.Dynamic) > 0) SkeletonIKBinding.Aim(bone, SkeletonIKBinding.Direction(origin, state.Dynamic, SkeletonIKBinding.Axis(bone)).Angle());
                SkeletonIKBinding.Request(owner, bone, stack.Strength); _states[i] = state;
            }
        }
    }
    internal (Vector2 Dynamic, Vector2 Velocity, Vector2 Force) GetSimulationState(int index) { lock (ModificationGate) { var state = _states[Index(index)]; return (state.Dynamic, state.Velocity, state.Force); } }
    private byte[] SaveJoints()
    {
        lock (ModificationGate)
        {
            ThrowIfDisposed(); using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream); writer.Write(1); writer.Write(_joints.Length);
            foreach (var joint in _joints) { writer.Write(joint.Path); writer.Write(joint.Index); writer.Write(joint.Override); writer.Write(joint.Stiffness); writer.Write(joint.Mass); writer.Write(joint.Damping); writer.Write(joint.UseGravity); writer.Write(joint.Gravity.X); writer.Write(joint.Gravity.Y); if (stream.Length > 64 * 1024 * 1024) throw new InvalidOperationException("Jiggle archive exceeds its budget."); }
            return stream.ToArray();
        }
    }
    private void LoadJoints(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes); if (bytes.Length > 64 * 1024 * 1024) throw new InvalidDataException("Jiggle archive exceeds its budget.");
        lock (ModificationGate)
        {
            EnsureModificationMutable(); if (bytes.Length == 0) { _joints = []; _states = []; return; }
            using var stream = new MemoryStream(bytes, false); using var reader = new BinaryReader(stream); if (reader.ReadInt32() != 1) throw new InvalidDataException("Invalid Jiggle archive version."); var count = reader.ReadInt32(); if (count < 0 || count > 4096) throw new InvalidDataException("Invalid Jiggle count."); var joints = new Joint[count];
            for (var i = 0; i < count; i++)
            {
                var joint = new Joint { Path = reader.ReadString(), Index = reader.ReadInt32(), Override = reader.ReadBoolean(), Stiffness = reader.ReadSingle(), Mass = reader.ReadSingle(), Damping = reader.ReadSingle(), UseGravity = reader.ReadBoolean(), Gravity = new(reader.ReadSingle(), reader.ReadSingle()) };
                SkeletonIKBinding.Path(joint.Path); if (joint.Index < -1) throw new InvalidDataException("Invalid Jiggle selection."); SkeletonIKBinding.Scalar(joint.Stiffness, true); PositiveMass(joint.Mass); DampingValue(joint.Damping); GravityValue(joint.Gravity); joints[i] = joint;
            }
            if (stream.Position != stream.Length) throw new InvalidDataException("Trailing Jiggle bytes."); _joints = joints; _states = new State[count];
        }
    }
    private static readonly PropertyDescriptor[] JiggleProperties =
    [
        new PropertyDescriptor<SkeletonModificationJiggle, int>(nameof(JiggleDataChainLength), m => m.JiggleDataChainLength, (m, v) => m.JiggleDataChainLength = v, _ => 0),
        new PropertyDescriptor<SkeletonModificationJiggle, string>(nameof(TargetNodePath), m => m.TargetNodePath, (m, v) => m.TargetNodePath = v, _ => string.Empty, stored: true),
        new PropertyDescriptor<SkeletonModificationJiggle, float>(nameof(Stiffness), m => m.Stiffness, (m, v) => m.Stiffness = v, _ => 3f, stored: true),
        new PropertyDescriptor<SkeletonModificationJiggle, float>(nameof(Mass), m => m.Mass, (m, v) => m.Mass = v, _ => .75f, stored: true),
        new PropertyDescriptor<SkeletonModificationJiggle, float>(nameof(Damping), m => m.Damping, (m, v) => m.Damping = v, _ => .75f, stored: true),
        new PropertyDescriptor<SkeletonModificationJiggle, Vector2>(nameof(Gravity), m => m.Gravity, (m, v) => m.Gravity = v, _ => new Vector2(0, 6), stored: true),
        new PropertyDescriptor<SkeletonModificationJiggle, bool>(nameof(UseGravity), m => m.UseGravity, (m, v) => m.UseGravity = v, _ => false, stored: true),
        new PropertyDescriptor<SkeletonModificationJiggle, bool>("_use_colliders", m => m.GetUseColliders(), (m, v) => m.SetUseColliders(v), _ => false, stored: true),
        new PropertyDescriptor<SkeletonModificationJiggle, uint>("_collision_mask", m => m.GetCollisionMask(), (m, v) => m.SetCollisionMask(v), _ => 1, stored: true),
        new PropertyDescriptor<SkeletonModificationJiggle, byte[]>("_joints", m => m.SaveJoints(), (m, v) => m.LoadJoints(v), _ => [], stored: true)
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(JiggleProperties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new SkeletonModificationJiggle();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var copy = (SkeletonModificationJiggle)target; CopyModificationState(copy);
        lock (ModificationGate) { copy._targetPath = _targetPath; copy._stiffness = _stiffness; copy._mass = _mass; copy._damping = _damping; copy._gravity = _gravity; copy._useGravity = _useGravity; copy._colliders = _colliders; copy._mask = _mask; copy.LoadJoints(SaveJoints()); }
    }
}
