namespace Electron2D;

/// <summary>Typed extension point for executable skeletal pose modifications.</summary>
/// <remarks>The stack borrows modifications. Scene bindings are weak and transient; derived types opt into exact
/// resource copying and storage. Editor gizmos require the editor owner and are not runtime switches.</remarks>
public abstract class SkeletonModification : Resource
{
    internal readonly object ModificationGate = new();
    private WeakReference<SkeletonModificationStack>? _stack;
    private WeakReference<Skeleton>? _owner;
    private bool _enabled = true, _setup;
    private ProcessPhase _mode = ProcessPhase.Idle;
    /// <summary>Initializes an enabled idle-phase modification.</summary>
    protected SkeletonModification() { }
    /// <summary>Gets or sets whether this modification executes.</summary><value>True initially.</value>
    public bool Enabled { get { lock (ModificationGate) { ThrowIfDisposed(); return _enabled; } } set { lock (ModificationGate) { EnsureModificationMutable(); _enabled = value; } EmitChanged(); } }
    /// <summary>Gets or sets the matching scene phase.</summary><value>Idle initially; raw source integers project to the shared semantic ProcessPhase domain.</value>
    public ProcessPhase ExecutionMode { get { lock (ModificationGate) { ThrowIfDisposed(); return _mode; } } set { Skeleton.ValidateExecution(0, value); lock (ModificationGate) { EnsureModificationMutable(); _mode = value; } EmitChanged(); } }
    /// <summary>Returns the prepared state of this modification.</summary><returns>False before binding/setup.</returns>
    public bool GetIsSetup() { lock (ModificationGate) { ThrowIfDisposed(); return _setup; } }
    /// <summary>Authors setup state for derived initialization and retry.</summary><param name="isSetup">Whether the bound modification can execute.</param>
    public void SetIsSetup(bool isSetup) { lock (ModificationGate) { EnsureModificationMutable(); _setup = isSetup; } }
    /// <summary>Returns its borrowed live stack, if bound.</summary><returns>Null after detachment or stack disposal.</returns>
    public SkeletonModificationStack? GetModificationStack() { lock (ModificationGate) { ThrowIfDisposed(); return Stack(); } }
    private SkeletonModificationStack? Stack() => _stack is not null && _stack.TryGetTarget(out var stack) && !stack.IsDisposed ? stack : null;
    /// <summary>Validates resource lifetime and its current scene owner before derived configuration edits.</summary>
    protected void EnsureModificationMutable() { ThrowIfDisposed(); if (_owner is not null && _owner.TryGetTarget(out var skeleton) && !skeleton.IsDisposed) skeleton.Tree?.EnsureOwnerThread(); }
    internal void ValidateBinding(SkeletonModificationStack stack)
    {
        lock (ModificationGate) { EnsureModificationMutable(); if (Stack() is { } old && !ReferenceEquals(old, stack) && _owner is not null && _owner.TryGetTarget(out var owner) && owner.IsInsideTree) throw new InvalidOperationException("A modification already belongs to another attached stack."); }
    }
    internal void Bind(SkeletonModificationStack? stack)
    {
        if (stack is not null) ValidateBinding(stack);
        var owner = stack?.GetSkeleton();
        lock (ModificationGate) { ThrowIfDisposed(); _stack = stack is null ? null : new(stack); _owner = owner is null ? null : new(owner); _setup = stack is not null; }
        if (stack is not null) try { OnSetupModification(stack); } catch { lock (ModificationGate) _setup = false; throw; }
    }
    internal void Unbind(SkeletonModificationStack stack)
    {
        lock (ModificationGate) if (_stack is not null && _stack.TryGetTarget(out var old) && ReferenceEquals(old, stack)) { _stack = null; _owner = null; _setup = false; }
    }
    internal void Run(double delta)
    {
        lock (ModificationGate) { ThrowIfDisposed(); if (!_setup || Stack() is null) throw new InvalidOperationException("Modification is not setup."); if (!_enabled) return; }
        OnExecute(delta);
    }
    /// <summary>Runs this modification's concrete pose behavior on the skeleton owner thread.</summary><param name="delta">Finite nonnegative seconds.</param>
    protected abstract void OnExecute(double delta);
    /// <summary>Receives a committed binding for initialization.</summary><param name="modificationStack">Borrowed live stack.</param>
    protected virtual void OnSetupModification(SkeletonModificationStack modificationStack) { }
    /// <summary>Constrains an angle to the given circular interval or its complement.</summary>
    /// <param name="angle">Finite radians, including multiple turns.</param><param name="min">Finite lower boundary.</param><param name="max">Finite upper boundary.</param><param name="invert">Select the outside of the sorted interval.</param><returns>Normalized radians, or the nearest allowed endpoint, preferring min at a tie.</returns>
    public float ClampAngle(float angle, float min, float max, bool invert)
    {
        ThrowIfDisposed(); if (!float.IsFinite(angle) || !float.IsFinite(min) || !float.IsFinite(max)) throw new ArgumentOutOfRangeException(nameof(angle));
        static float Bound(float value) { var r = Mathf.PosMod(value, Mathf.Tau); return r == 0 && value > 0 ? Mathf.Tau : r; }
        angle = Mathf.PosMod(angle, Mathf.Tau); min = Bound(min); max = Bound(max); if (min > max) (min, max) = (max, min);
        if (!invert && (angle < min || angle > max) || invert && angle > min && angle < max)
        { var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle)); var a = new Vector2(MathF.Cos(min), MathF.Sin(min)); var b = new Vector2(MathF.Cos(max), MathF.Sin(max)); return direction.DistanceSquaredTo(a) <= direction.DistanceSquaredTo(b) ? min : max; }
        return angle;
    }
    /// <summary>Copies stored base settings without transient bindings.</summary><param name="target">Fresh exact derived target.</param>
    protected void CopyModificationState(SkeletonModification target) { lock (ModificationGate) { ThrowIfDisposed(); target._enabled = _enabled; target._mode = _mode; } }
    private static readonly PropertyDescriptor[] ModificationProperties =
    [
        new PropertyDescriptor<SkeletonModification, bool>(nameof(Enabled), m => m.Enabled, (m, v) => m.Enabled = v, _ => true, stored: true),
        new PropertyDescriptor<SkeletonModification, ProcessPhase>(nameof(ExecutionMode), m => m.ExecutionMode, (m, v) => m.ExecutionMode = v, _ => ProcessPhase.Idle, stored: true)
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(ModificationProperties);
    /// <inheritdoc />
    protected override void OnResetState() { lock (ModificationGate) { _stack = null; _owner = null; _setup = false; } }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) lock (ModificationGate) { _stack = null; _owner = null; _setup = false; } base.Dispose(disposing); }
}
