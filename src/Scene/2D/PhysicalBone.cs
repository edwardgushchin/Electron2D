namespace Electron2D;

/// <summary>A rigid physics body that follows or simulates one scene bone under a skeleton.</summary>
/// <remarks>Only Skeleton and PhysicalBone parent chains supply rig ownership. The physical body and scene
/// Bone remain separate nodes; SkeletonModificationPhysicalBones transfers solved poses into the rig.
/// Child joints and collision shapes are authored by the caller; no joint or shape is generated.
/// Logical metadata reads allow the owner or an active processing group. Binding and role edits remain owner work.
/// Current-group notifications follow the bone and align child joints before returning. Changed native participation
/// publishes on the owner after the group joins, before numerical use; failed publication retries on the current attachment.
/// Attached binding and role edits reject while a numerical physics implementation hook is borrowed.</remarks>
public class PhysicalBone : RigidBody
{
    private int _boneIndex = -1;
    private string _bonePath = string.Empty;
    private bool _simulate, _active, _follow, _auto = true, _reconciling;
    private readonly object _bindingGate = new();
    private bool _activationPending, _publishingActivation;
    private PhysicsSpace? _activationSpace;
    private long _activationAttachment;
    private WeakReference<Skeleton>? _rig;
    private WeakReference<Bone>? _bone;
    private WeakReference<Joint>? _joint;
    private ulong _pathRevision, _setup, _jointRevision = ulong.MaxValue;
    /// <summary>Creates an unselected static noncolliding follower with automatic child joint configuration.</summary>
    public PhysicalBone() { SetInternalProcessing(true, true); }
    /// <summary>Gets or selects the bone index, authoring its path when attached.</summary><value>Minus one initially; minus one clears numeric selection.</value>
    public int BoneIndex
    {
        get { ReadPhysical(); return ResolveBone() is { } bone ? bone.GetIndexInSkeleton() : _bonePath.Length > 0 ? -1 : _boneIndex; }
        set { EnsureMutable(); EnsurePhysicsParticipationChange(); Space?.EnsureImplementationWriteAccess(); if (value < -1) throw new ArgumentOutOfRangeException(nameof(value)); var rig = ResolveRig(); var bone = value >= 0 && rig is { IsInsideTree: true } ? rig.GetBone(value) : null; var path = bone is null ? string.Empty : GetPathTo(bone); _boneIndex = value; _bonePath = path; InvalidateBinding(); Reconcile(); NotifyPropertyListChanged(); }
    }
    /// <summary>Gets or sets the Bone path relative to this physical body.</summary><value>Empty initially; an authored path takes precedence over the numeric index.</value>
    public string BoneNodePath
    { get { ReadPhysical(); return _bonePath; } set { EnsureMutable(); EnsurePhysicsParticipationChange(); Space?.EnsureImplementationWriteAccess(); SkeletonIKBinding.Path(value); _bonePath = value; InvalidateBinding(); Reconcile(); NotifyPropertyListChanged(); } }
    /// <summary>Gets or requests rigid simulation; disabled bodies follow their bone without collision response.</summary><value>False initially. Detached or missing bindings retain the request but cannot simulate.</value>
    /// <remarks>A selected role-publication error retains the committed request. Complete role preparation retries before numerical use or a repeated assignment.</remarks>
    public bool SimulatePhysics
    { get { ReadPhysical(); return _simulate; } set { EnsureMutable(); EnsurePhysicsParticipationChange(); Space?.EnsureImplementationWriteAccess(); PreparePendingServerMode(writing: true); if (_simulate == value) return; _simulate = value; Reconcile(); } }
    /// <summary>Gets or sets whether each physics callback aligns even an active simulator to its bone.</summary><value>False initially; switching this reseeds the active body from the current bone.</value>
    /// <remarks>Inherited forces/contacts may move it during the solver interval. Physical pose transfer skips this follower.</remarks>
    public bool FollowBoneWhenSimulating
    { get { ReadPhysical(); return _follow; } set { EnsureMutable(); EnsurePhysicsParticipationChange(); Space?.EnsureImplementationWriteAccess(); if (_follow == value) return; _follow = value; if (_active) PositionAtBone(); } }
    /// <summary>Gets or sets automatic configuration of the first authored child Joint.</summary><value>True initially.</value>
    /// <remarks>For a PhysicalBone parent, assigns NodeA/NodeB and aligns the joint origin. With a Skeleton parent, existing endpoints remain authored. No joint is created.</remarks>
    public bool AutoConfigureJoint
    { get { ReadPhysical(); return _auto; } set { EnsureMutable(); EnsurePhysicsParticipationChange(); Space?.EnsureImplementationWriteAccess(); _auto = value; _jointRevision = ulong.MaxValue; ConfigureJoint(); } }
    private void ReadPhysical() { ThrowIfDisposed(); Tree?.EnsureProcessingReadAccess(); }
    private void InvalidateBinding() { _bone = null; _pathRevision = ulong.MaxValue; _jointRevision = ulong.MaxValue; UpdateConfigurationWarnings(); }
    private Skeleton? ResolveRig()
    {
        var parent = Parent; while (parent is PhysicalBone) parent = parent.Parent;
        var rig = parent as Skeleton;
        lock (_bindingGate)
        {
            if (rig is null || rig.IsDisposed || !ReferenceEquals(rig.Tree, Tree)) { _rig = null; _bone = null; return null; }
            if (_rig is null || !_rig.TryGetTarget(out var old) || !ReferenceEquals(old, rig)) { _rig = new(rig); _bone = null; _pathRevision = ulong.MaxValue; }
            return rig;
        }
    }
    internal Bone? ResolveBone()
    {
        var tree = Tree; var rig = ResolveRig(); if (rig is not { IsInsideTree: true } || tree is null) return null;
        var setup = rig.SetupGeneration;
        if (IsDisposed || !ReferenceEquals(Tree, tree) || !ReferenceEquals(rig.Tree, tree)) return null;
        lock (_bindingGate)
        {
            if (_setup == setup && _pathRevision == tree.PathRevision) return CachedBone(rig);
        }
        var bone = _bonePath.Length > 0 ? GetNodeOrNull(_bonePath) as Bone : _boneIndex >= 0 && _boneIndex < rig.GetBoneCount() ? rig.GetBone(_boneIndex) : null;
        lock (_bindingGate)
        {
            if (bone is not null && ReferenceEquals(bone.SkeletonOwner, rig)) { if (_bone is null) _bone = new(bone); else _bone.SetTarget(bone); } else _bone = null;
            _setup = setup; _pathRevision = tree.PathRevision;
            return CachedBone(rig);
        }
    }
    private Bone? CachedBone(Skeleton rig) => _bone is not null && _bone.TryGetTarget(out var result) && !result.IsDisposed && ReferenceEquals(result.SkeletonOwner, rig) ? result : null;
    /// <summary>Returns the first live direct Joint child, independent of simulation state.</summary><returns>The borrowed scene joint, or null.</returns>
    public Joint? GetJoint()
    {
        ReadPhysical();
        lock (_bindingGate)
        {
            if (_joint is not null && _joint.TryGetTarget(out var old) && !old.IsDisposed && ReferenceEquals(old.Parent, this)) return old;
            for (var i = 0; i < GetChildCount(); i++) if (GetChild(i) is Joint joint && !joint.IsDisposed) { _joint = new(joint); return joint; }
            _joint = null; return null;
        }
    }
    /// <summary>Returns whether an attached valid bone binding has activated the simulation request.</summary><returns>False detached or unselected. Inherited Freeze/Sleep/disable policies still determine actual motion.</returns>
    /// <remarks>This reads logical activation without a numerical query. Grouped native mode/filter changes publish after joining; selected publication failures retry before numerical use.</remarks>
    public bool IsSimulatingPhysics() { ReadPhysical(); return _active && HasBackend && !PhysicsRemoved && ResolveBone() is not null; }
    private void PositionAtBone() { if (ResolveBone() is { } bone && GlobalTransform != bone.GlobalTransform) GlobalTransform = bone.GlobalTransform; }
    internal void Reconcile()
    {
        if (_reconciling || !IsInsideTree || IsDisposed) return; _reconciling = true;
        try
        {
            Tree!.EnsureProcessingAccess(this);
            if (_activationPending)
            {
                if (Tree.IsProcessingWorkerStage) Tree.QueueScenePublication(this, SceneTree.ScenePublicationKind.PhysicalBoneActivation);
                else PreparePendingActivation();
            }
            if (Parent is PhysicalBone { IsNodeReady: false } parent) parent.Reconcile();
            var bone = ResolveBone(); var active = _simulate && bone is not null;
            if (!IsInsideTree || IsDisposed) return;
            if (_active != active)
            {
                if (Tree!.IsProcessingWorkerStage)
                {
                    EnsureTransformMutable(); Space?.EnsureSceneTransformWriteAccess(Tree);
                    if (bone is not null) PositionAtBone();
                    _active = active; _activationPending = true; _activationSpace = Space; _activationAttachment = Backend.AttachmentVersion;
                    MarkShapesDirty(); Space?.MarkColliderMetadataDirty();
                    Tree.QueueScenePublication(this, SceneTree.ScenePublicationKind.PhysicalBoneActivation);
                }
                else
                {
                    EnsurePhysicsParticipationChange(); if (bone is not null) PositionAtBone();
                    RestoreSceneMode(); _active = active; UpdatePhysicsParticipation(); MarkShapesDirty();
                }
                _jointRevision = ulong.MaxValue;
            }
            if (!HasServerMode && (!_active || _follow)) PositionAtBone();
            ConfigureJoint();
        }
        finally { _reconciling = false; }
    }
    internal void PreparePendingActivation()
    {
        if (!_activationPending || _publishingActivation) return;
        if (IsDisposed || !IsInsideTree || !ReferenceEquals(Space, _activationSpace) || Backend.AttachmentVersion != _activationAttachment)
        { _activationPending = false; _activationSpace = null; return; }
        Tree!.EnsureOwnerThread(); EnsurePhysicsParticipationChange();
        _publishingActivation = true;
        try
        {
            RestoreSceneMode(); UpdatePhysicsParticipation(); MarkShapesDirty(); PrepareBackend();
            _activationPending = false; _activationSpace = null;
        }
        catch { Space?.MarkColliderMetadataDirty(); throw; }
        finally { _publishingActivation = false; }
    }
    internal void DiscardPendingActivation() { _activationPending = false; _activationSpace = null; }
    private void ConfigureJoint()
    {
        if (!_auto || !IsInsideTree || GetJoint() is not { } joint) return;
        var revision = Tree!.PathRevision;
        if (_jointRevision != revision)
        {
            if (Parent is PhysicalBone parent) { joint.NodeA = joint.GetPathTo(parent); joint.NodeB = joint.GetPathTo(this); }
            _jointRevision = revision;
        }
        if (joint.GlobalPosition != GlobalPosition) joint.GlobalPosition = GlobalPosition;
    }
    internal override bool CollisionResponseEnabled => _active;
    internal override PhysicsServer.BodyMode RequestedBodyMode => _active ? base.RequestedBodyMode : PhysicsServer.BodyMode.Static;
    internal override bool MovesWithSimulation => _active && base.MovesWithSimulation;
    internal override void OnTreeMembershipChanged(bool entering)
    { base.OnTreeMembershipChanged(entering); lock (_bindingGate) { _rig = null; _bone = null; _joint = null; _pathRevision = _jointRevision = ulong.MaxValue; } DiscardPendingActivation(); if (!entering) _active = false; }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        Tree?.EnsureProcessingAccess(this);
        if (what == NotificationChildOrderChanged) lock (_bindingGate) { _joint = null; _jointRevision = ulong.MaxValue; }
        base.OnNotification(what); if (IsDisposed) return;
        if (what is NotificationReady or NotificationInternalPhysicsProcess) Reconcile();
        else if (what == NotificationInternalProcess && (!_active || _follow)) Reconcile();
    }
    /// <inheritdoc />
    public override string[] GetConfigurationWarnings()
    {
        var warnings = base.GetConfigurationWarnings(); var rig = ResolveRig();
        if (rig is null) warnings = [.. warnings, "PhysicalBone requires a Skeleton or PhysicalBone parent chain."];
        else if (IsInsideTree && ResolveBone() is null) warnings = [.. warnings, "PhysicalBone requires a valid Bone selection in its rig."];
        if (Parent is PhysicalBone && GetJoint() is null) warnings = [.. warnings, "Add an authored Joint child to connect physical bones."];
        return warnings;
    }
    private static readonly PropertyDescriptor[] PhysicalProperties =
    [
        new PropertyDescriptor<PhysicalBone, int>(nameof(BoneIndex), b => b.BoneIndex, (b, v) => b.BoneIndex = v, _ => -1, stored: true),
        new PropertyDescriptor<PhysicalBone, string>(nameof(BoneNodePath), b => b.BoneNodePath, (b, v) => b.BoneNodePath = v, _ => string.Empty, stored: true),
        new PropertyDescriptor<PhysicalBone, bool>(nameof(AutoConfigureJoint), b => b.AutoConfigureJoint, (b, v) => b.AutoConfigureJoint = v, _ => true, stored: true),
        new PropertyDescriptor<PhysicalBone, bool>(nameof(FollowBoneWhenSimulating), b => b.FollowBoneWhenSimulating, (b, v) => b.FollowBoneWhenSimulating = v, _ => false, stored: true),
        new PropertyDescriptor<PhysicalBone, bool>(nameof(SimulatePhysics), b => b.SimulatePhysics, (b, v) => b.SimulatePhysics = v, _ => false, stored: true)
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(PhysicalProperties);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(PhysicalBone) ? CreatePhysicalBone : base.CreateSceneInstanceFactory();
    private static PhysicalBone CreatePhysicalBone() => new();
}
