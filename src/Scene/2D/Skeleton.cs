namespace Electron2D;

/// <summary>Owns the prepared bind/pose palette of a scene bone hierarchy and executes skeletal modifications.</summary>
/// <remarks>Bones and the optional modification stack are borrowed. Depth-first order includes only uninterrupted
/// direct Bone chains. Scene mutation and palette replay use the scene owner thread. Retained polygons observe
/// live poses without rebuilding their triangulation. Resource/editor and native backend ownership remain separate.</remarks>
public class Skeleton : Entity
{
    private readonly List<Bone> _bones = [], _build = [];
    private Transform[] _inverse = [], _pose = [], _skinPalette = [];
    private bool[] _valid = [];
    private Transform[] _overrides = [];
    private float[] _strength = [];
    private bool[] _persistent = [];
    private Transform[] _savedOverrides = [];
    private float[] _savedStrength = [];
    private bool[] _savedPersistent = [];
    private bool _dirty = true, _settingUp, _executing;
    private ulong _generation;
    private RID _rid;
    internal bool Executing => _executing;
    private SkeletonModificationStack? _stack;
    /// <summary>Creates a detached skeleton with an empty palette.</summary>
    public Skeleton() { SetInternalProcessing(true, true); }
    /// <summary>Occurs after committing a changed bone/rest layout and indices.</summary>
    /// <remarks>Reads during a handler observe the committed setup. Handler failure does not replay the transition.</remarks>
    public event Action? BoneSetupChanged;
    internal ulong SetupGeneration { get { EnsureSetup(); return _generation; } }
    internal void InvalidateSetup() { _dirty = true; }
    internal void EnsureSetup()
    {
        ThrowIfDisposed(); Tree?.EnsureOwnerThread(); if (!_dirty || _settingUp || !IsInsideTree) return;
        if (_executing) throw new InvalidOperationException("Bone setup changed during modification execution.");
        _settingUp = true;
        try
        {
            _build.Clear(); Collect(this);
            var count = _build.Count;
            var inverse = new Transform[count]; var valid = new bool[count]; var pose = new Transform[count]; var overrides = new Transform[count];
            for (var i = 0; i < count; i++) { var rest = _build[i].GetSkeletonRest(); valid[i] = rest.Determinant() != 0; inverse[i] = valid[i] ? rest.AffineInverse() : Transform.Identity; overrides[i] = _build[i].Rest; }
            foreach (var old in _bones) old.SkeletonIndex = -1;
            _bones.Clear(); _bones.AddRange(_build); _inverse = inverse; _valid = valid; _pose = pose; _skinPalette = new Transform[count]; _overrides = overrides; _strength = new float[count]; _persistent = new bool[count];
            for (var i = 0; i < count; i++) _bones[i].SkeletonIndex = i;
            _savedOverrides = new Transform[count]; _savedStrength = new float[count]; _savedPersistent = new bool[count];
            _dirty = false; _generation++;
        }
        finally { _settingUp = false; }
        BoneSetupChanged?.Invoke();
    }
    private void Collect(Node parent)
    {
        for (var i = 0; i < parent.GetChildCount(includeInternal: true); i++)
            if (parent.GetChild(i, includeInternal: true) is Bone bone && !bone.IsDisposed && ReferenceEquals(bone.Tree, Tree)) { _build.Add(bone); Collect(bone); }
    }
    /// <summary>Returns the number of attached bones after refreshing a changed setup.</summary><returns>Zero while detached.</returns>
    public int GetBoneCount() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); EnsureSetup(); return IsInsideTree ? _bones.Count : 0; }
    /// <summary>Returns a borrowed bone by current depth-first index.</summary><param name="index">Index in the attached hierarchy.</param><returns>The live scene-owned bone.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the current attached palette.</exception>
    public Bone GetBone(int index) { EnsureSetup(); if (!IsInsideTree || (uint)index >= (uint)_bones.Count) throw new ArgumentOutOfRangeException(nameof(index)); return _bones[index]; }
    /// <summary>Returns the borrowed stable palette identity used by retained skin commands.</summary><returns>A logical RID valid until disposal, independent of graphics startup.</returns>
    public RID GetSkeleton() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); if (!_rid.IsValid()) _rid = RenderingSkeletonRegistry.Register(this); return _rid; }
    /// <summary>Stores a finite local bone override with an interpolation strength.</summary>
    /// <param name="boneIndex">Current bone index.</param><param name="overridePose">Finite local transform.</param><param name="strength">Interpolation amount from zero through one.</param><param name="persistent">Retain the override across later process applications.</param>
    /// <remarks>Applied during idle execution, including without a modification stack. Transient overrides are consumed once.</remarks>
    public void SetBoneLocalPoseOverride(int boneIndex, Transform overridePose, float strength, bool persistent)
    {
        EnsureMutable(); EnsureSetup(); GetBone(boneIndex); if (!overridePose.IsFinite()) throw new ArgumentException("Override pose must be finite.", nameof(overridePose)); ValidateStrength(strength);
        _overrides[boneIndex] = overridePose; _strength[boneIndex] = strength; _persistent[boneIndex] = persistent;
    }
    /// <summary>Returns the latest stored local override, initially the bone's local Rest.</summary><param name="boneIndex">Current bone index.</param><returns>The stored local pose.</returns>
    public Transform GetBoneLocalPoseOverride(int boneIndex) { GetBone(boneIndex); return _overrides[boneIndex]; }
    /// <summary>Returns the borrowed modification stack.</summary><returns>The assigned live stack or null.</returns>
    public SkeletonModificationStack? GetModificationStack() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _stack; }
    /// <summary>Assigns a borrowed modification stack and prepares it while attached.</summary><param name="modificationStack">Live stack or null.</param>
    /// <remarks>One attached skeleton can bind a stack at a time. Replacement commits before setup callbacks; resources stay caller-owned.</remarks>
    public void SetModificationStack(SkeletonModificationStack? modificationStack)
    {
        EnsureMutable(); if (_executing) throw new InvalidOperationException("A skeleton stack cannot change during execution."); if (modificationStack is { IsDisposed: true }) throw new ObjectDisposedException(nameof(modificationStack));
        modificationStack?.ValidateBinding(this); if (ReferenceEquals(_stack, modificationStack)) return;
        if (_stack is { IsDisposed: false }) _stack.Bind(null); _stack = modificationStack; if (IsInsideTree) _stack?.Bind(this);
    }
    /// <summary>Executes the matching stack phase and applies idle local overrides.</summary><param name="delta">Finite nonnegative seconds.</param><param name="executionMode">Existing idle or physics phase domain.</param>
    /// <remarks>Physics prepares overrides; idle applies them. Authored bone poses are retained independently of modification writes.
    /// Reentry and structural mutation during execution reject; callback failure restores authored poses, prior override requests and execution guards.</remarks>
    public void ExecuteModifications(double delta, ProcessPhase executionMode)
    {
        EnsureMutable(); EnsureSetup(); ValidateExecution(delta, executionMode); if (_executing) throw new InvalidOperationException("Skeleton execution cannot reenter.");
        _executing = true;
        _overrides.CopyTo(_savedOverrides, 0); _strength.CopyTo(_savedStrength, 0); _persistent.CopyTo(_savedPersistent, 0);
        try
        {
            for (var i = 0; i < _bones.Count; i++) _bones[i].ApplyModifiedPose(_bones[i].AuthoredPose);
            foreach (var bone in _bones) bone.BeginModification();
            if (_stack is not null) _stack.Execute(delta, executionMode);
            if (_dirty) throw new InvalidOperationException("Bone setup changed during modification execution.");
            if (executionMode == ProcessPhase.Idle)
                for (var i = 0; i < _bones.Count; i++)
                {
                    var bone = _bones[i]; bone.ApplyModifiedPose(_strength[i] > 0 ? bone.AuthoredPose.InterpolateWith(_overrides[i], _strength[i]) : bone.AuthoredPose);
                    if (!_persistent[i]) _strength[i] = 0;
                }
            else for (var i = 0; i < _bones.Count; i++) _bones[i].ApplyModifiedPose(_bones[i].AuthoredPose);
        }
        catch (Exception executionError)
        {
            _savedOverrides.CopyTo(_overrides, 0); _savedStrength.CopyTo(_strength, 0); _savedPersistent.CopyTo(_persistent, 0);
            List<Exception>? cleanup = null;
            foreach (var bone in _bones) if (!bone.IsDisposed) try { bone.ApplyModifiedPose(bone.AuthoredPose); } catch (Exception error) { (cleanup ??= []).Add(error); }
            if (cleanup is not null) { cleanup.Insert(0, executionError); throw new AggregateException("Modification execution and pose restoration failed.", cleanup); }
            throw;
        }
        finally { foreach (var bone in _bones) bone.EndModification(); _executing = false; }
    }
    internal static void ValidateExecution(double delta, ProcessPhase phase) { if (!double.IsFinite(delta) || delta < 0) throw new ArgumentOutOfRangeException(nameof(delta)); if (phase != ProcessPhase.Idle && phase != ProcessPhase.Physics) throw new ArgumentOutOfRangeException(nameof(phase)); }
    internal static void ValidateStrength(float strength) { if (!float.IsFinite(strength) || strength < 0 || strength > 1) throw new ArgumentOutOfRangeException(nameof(strength)); }
    internal ReadOnlySpan<Transform> PrepareSkinPalette()
    {
        EnsureSetup();
        for (var i = 0; i < _bones.Count; i++)
        {
            var bone = _bones[i]; var local = bone.GetInterpolatedVisualTransform((float)Engine.PhysicsInterpolationFraction); _pose[i] = bone.TopLevel ? GetInterpolatedGlobalVisualTransform((float)Engine.PhysicsInterpolationFraction).AffineInverse() * bone.GetInterpolatedGlobalVisualTransform((float)Engine.PhysicsInterpolationFraction) : bone.Parent is Bone parent && parent.SkeletonIndex >= 0 ? _pose[parent.SkeletonIndex] * local : local;
            var result = _valid[i] ? _pose[i] * _inverse[i] : Transform.Identity; if (!result.IsFinite()) throw new InvalidOperationException("Skeleton pose overflowed."); _skinPalette[i] = result;
        }
        return _skinPalette.AsSpan(0, _bones.Count);
    }
    internal override void OnTreeMembershipChanged(bool entering)
    {
        base.OnTreeMembershipChanged(entering); _dirty = true;
        if (!entering) { if (_stack is { IsDisposed: false }) _stack.Bind(null); foreach (var bone in _bones) bone.SkeletonIndex = -1; _bones.Clear(); _generation++; }
    }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        if (what == NotificationChildOrderChanged) InvalidateSetup();
        base.OnNotification(what); if (IsDisposed) return;
        if (what is NotificationReady or NotificationPostEnterTree) { EnsureSetup(); _stack?.Bind(this); }
        else if (what == NotificationInternalProcess) ExecuteModifications(ProcessDeltaTime, ProcessPhase.Idle);
        else if (what == NotificationInternalPhysicsProcess) ExecuteModifications(PhysicsProcessDeltaTime, ProcessPhase.Physics);
    }
    private static readonly PropertyDescriptor[] SkeletonProperties =
    [new PropertyDescriptor<Skeleton, SkeletonModificationStack?>("_modification_stack", s => s.GetModificationStack(), (s, v) => s.SetModificationStack(v), _ => null, stored: true) { AlwaysDuplicateResource = true }];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(SkeletonProperties);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(Skeleton) ? CreateSkeleton : base.CreateSceneInstanceFactory();
    private static Skeleton CreateSkeleton() => new();
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    { if (disposing) { if (_stack is { IsDisposed: false }) _stack.Bind(null); _stack = null; RenderingSkeletonRegistry.Remove(_rid); BoneSetupChanged = null; _bones.Clear(); _build.Clear(); } base.Dispose(disposing); }
}
