namespace Electron2D;

/// <summary>Stores ordered borrowed skeletal modifications with one transient scene binding.</summary>
/// <remarks>Structural edits are cold resource operations. Bound access follows the skeleton owner thread;
/// executing/setup callbacks cannot restructure or reenter the list. Cloning always copies modification resources,
/// preserving aliases through the existing resource duplication session. Bound skeletons are weak references.</remarks>
public sealed class SkeletonModificationStack : Resource
{
    private readonly object _gate = new();
    private readonly List<SkeletonModification?> _mods = [];
    private WeakReference<Skeleton>? _skeleton;
    private bool _enabled, _setup, _executing, _preparing;
    private float _strength = 1;
    /// <summary>Creates an empty disabled stack at full strength.</summary>
    public SkeletonModificationStack() { }
    /// <summary>Gets or sets whether the ordered stack executes.</summary><value>False initially.</value>
    public bool Enabled { get { lock (_gate) { Read(); return _enabled; } } set { lock (_gate) { Edit(); _enabled = value; } EmitChanged(); } }
    /// <summary>Gets or sets modification interpolation strength.</summary><value>One initially; valid range zero through one.</value>
    public float Strength { get { lock (_gate) { Read(); return _strength; } } set { Skeleton.ValidateStrength(value); lock (_gate) { Edit(); _strength = value; } EmitChanged(); } }
    /// <summary>Gets or resizes the ordered list, growing with null slots.</summary><value>Zero initially; bounded to 4096 slots.</value>
    public int ModificationCount { get { lock (_gate) { Read(); return _mods.Count; } } set { lock (_gate) { Edit(true); if (value < 0 || value > 4096) throw new ArgumentOutOfRangeException(nameof(value)); while (_mods.Count < value) _mods.Add(null); if (_mods.Count > value) { for (var i = value; i < _mods.Count; i++) if (_mods[i] is { } mod && _mods.IndexOf(mod) >= value) Detach(mod); _mods.RemoveRange(value, _mods.Count - value); } } NotifyPropertyListChanged(); EmitChanged(); } }
    private Skeleton? Owner() => _skeleton is not null && _skeleton.TryGetTarget(out var skeleton) && !skeleton.IsDisposed ? skeleton : null;
    private void Read() { ThrowIfDisposed(); Owner()?.Tree?.EnsureOwnerThread(); }
    private void Edit(bool structure = false) { Read(); if (structure && (_executing || _preparing)) throw new InvalidOperationException("A running modification list cannot restructure."); }
    internal void ValidateBinding(Skeleton skeleton) { lock (_gate) { Read(); if (Owner() is { IsInsideTree: true } old && !ReferenceEquals(old, skeleton)) throw new InvalidOperationException("A stack already belongs to another attached skeleton."); } }
    internal void Bind(Skeleton? skeleton)
    {
        lock (_gate)
        {
            Read(); if (skeleton is null ? _skeleton is null : ReferenceEquals(Owner(), skeleton) && _setup) return;
            if (_executing || _preparing) throw new InvalidOperationException("A running stack cannot rebind.");
            if (skeleton is not null) ValidateBinding(skeleton);
            _skeleton = skeleton is null ? null : new(skeleton); _setup = false;
            if (skeleton is null) { foreach (var mod in _mods) if (mod is { IsDisposed: false }) Detach(mod); }
        }
        if (skeleton is not null) Setup();
    }
    /// <summary>Returns its borrowed scene skeleton.</summary><returns>Null without an active binding.</returns>
    public Skeleton? GetSkeleton() { lock (_gate) { Read(); return Owner(); } }
    /// <summary>Returns whether binding/setup completed.</summary><returns>False initially.</returns>
    public bool GetIsSetup() { lock (_gate) { Read(); return _setup; } }
    /// <summary>Prepares all present modifications after binding to a skeleton.</summary>
    /// <remarks>Binding is required. Every setup is attempted; failures leave setup false for explicit retry.</remarks>
    public void Setup()
    {
        lock (_gate)
        {
            Read(); if (_setup) return; if (Owner() is null) throw new InvalidOperationException("Stack setup requires a bound skeleton."); if (_preparing || _executing) throw new InvalidOperationException("Stack setup cannot reenter."); _preparing = true;
            List<Exception>? errors = null;
            try { foreach (var mod in _mods) if (mod is not null) try { mod.Bind(this); } catch (Exception error) { (errors ??= []).Add(error); } _setup = errors is null; }
            finally { _preparing = false; }
            if (errors is not null) throw new AggregateException("Modification setup failed.", errors);
        }
    }
    /// <summary>Executes matching enabled modifications in index order.</summary><param name="delta">Nonnegative finite seconds.</param><param name="executionMode">Idle or physics phase.</param>
    /// <remarks>Requires an attached prepared skeleton. Null slots are skipped. Guard cleanup permits retry after user failure.</remarks>
    public void Execute(double delta, ProcessPhase executionMode)
    {
        Skeleton.ValidateExecution(delta, executionMode);
        var owner = GetSkeleton(); if (owner is { Executing: false }) { owner.ExecuteModifications(delta, executionMode); return; }
        lock (_gate)
        {
            Read(); if (!_setup || Owner() is not { IsInsideTree: true }) throw new InvalidOperationException("Stack is not prepared on an attached skeleton."); if (!_enabled) return; if (_executing || _preparing) throw new InvalidOperationException("Stack execution cannot reenter."); _executing = true;
            try { for (var i = 0; i < _mods.Count; i++) if (_mods[i] is { } mod && mod.ExecutionMode == executionMode) mod.Run(delta); }
            finally { _executing = false; }
        }
    }
    /// <summary>Enables or disables all present modifications.</summary><param name="enabled">Requested enable state.</param>
    public void EnableAllModifications(bool enabled) { lock (_gate) { Edit(); foreach (var mod in _mods) if (mod is not null) mod.Enabled = enabled; } }
    /// <summary>Returns a borrowed modification from a slot.</summary><param name="index">Valid slot.</param><returns>The modification or null.</returns>
    public SkeletonModification? GetModification(int index) { lock (_gate) { Read(); return _mods[Index(index)]; } }
    /// <summary>Appends a borrowed nonnull modification, preparing it when already bound.</summary><param name="modification">Live resource, caller-owned.</param>
    public void AddModification(SkeletonModification modification) { ArgumentNullException.ThrowIfNull(modification); lock (_gate) { Edit(true); if (modification.IsDisposed) throw new ObjectDisposedException(nameof(modification)); if (_setup) modification.ValidateBinding(this); if (_mods.Count == 4096) throw new InvalidOperationException("Modification capacity exceeded."); _mods.Add(modification); if (_setup) PrepareAdded(modification); } EmitChanged(); }
    /// <summary>Removes an indexed slot and detaches a resource no longer present in the list.</summary><param name="index">Valid slot.</param>
    public void DeleteModification(int index) { lock (_gate) { Edit(true); var old = _mods[Index(index)]; _mods.RemoveAt(index); if (old is not null && !_mods.Contains(old)) Detach(old); } EmitChanged(); }
    /// <summary>Replaces a borrowed slot, allowing null.</summary><param name="index">Valid slot.</param><param name="modification">Live resource or null.</param>
    public void SetModification(int index, SkeletonModification? modification) { lock (_gate) { Edit(true); Index(index); if (modification is { IsDisposed: true }) throw new ObjectDisposedException(nameof(modification)); if (_setup) modification?.ValidateBinding(this); var old = _mods[index]; _mods[index] = modification; if (old is not null && !_mods.Contains(old)) Detach(old); if (_setup && modification is not null) PrepareAdded(modification); } EmitChanged(); }
    private void Detach(SkeletonModification mod) => mod.Unbind(this);
    private void PrepareAdded(SkeletonModification modification) { _preparing = true; try { modification.Bind(this); } catch { _setup = false; throw; } finally { _preparing = false; } }
    private int Index(int index) { if ((uint)index >= (uint)_mods.Count) throw new ArgumentOutOfRangeException(nameof(index)); return index; }
    private SkeletonModification?[] StoredMods { get { lock (_gate) { Read(); return _mods.ToArray(); } } set { ArgumentNullException.ThrowIfNull(value); if (value.Length > 4096 || value.Any(m => m is { IsDisposed: true })) throw new ArgumentException("Invalid modification array.", nameof(value)); lock (_gate) { Edit(true); foreach (var mod in _mods) if (mod is { IsDisposed: false }) Detach(mod); _mods.Clear(); _mods.AddRange(value); _setup = false; } } }
    private static readonly PropertyDescriptor[] StackProperties =
    [
        new PropertyDescriptor<SkeletonModificationStack, int>(nameof(ModificationCount), s => s.ModificationCount, (s, v) => s.ModificationCount = v, _ => 0),
        new PropertyDescriptor<SkeletonModificationStack, bool>(nameof(Enabled), s => s.Enabled, (s, v) => s.Enabled = v, _ => false, stored: true),
        new PropertyDescriptor<SkeletonModificationStack, float>(nameof(Strength), s => s.Strength, (s, v) => s.Strength = v, _ => 1, stored: true),
        new PropertyDescriptor<SkeletonModificationStack, SkeletonModification?[]>("_modifications", s => s.StoredMods, (s, v) => s.StoredMods = v, _ => [], stored: true)
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(StackProperties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new SkeletonModificationStack();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var copy = (SkeletonModificationStack)target; SkeletonModification?[] mods; bool enabled; float strength;
        lock (_gate) { Read(); mods = _mods.ToArray(); enabled = _enabled; strength = _strength; }
        for (var i = 0; i < mods.Length; i++) if (mods[i] is { } mod) mods[i] = (SkeletonModification)forceDuplicateSubresource(mod)!;
        copy._enabled = enabled; copy._strength = strength; copy._mods.AddRange(mods);
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) lock (_gate) { foreach (var mod in _mods) if (mod is { IsDisposed: false }) Detach(mod); _mods.Clear(); _skeleton = null; _setup = false; } base.Dispose(disposing); }
}
