namespace Electron2D;

/// <summary>Records typed reversible actions with cursor history, merging and conditional object ownership.</summary>
/// <remarks>The creating thread owns all access. Callbacks and property values are retained using ordinary
/// C# assignment; assets remain borrowed. Reference registrations explicitly transfer conditional ownership
/// of non-Resource objects. Operations are attempted independently and errors aggregate after history state
/// commits. No automatic rollback of user side effects, reflection or delegate serialization is provided.
/// Prepared Undo/Redo reuse operation storage; authoring, merging and retirement are cold transitions.</remarks>
public class UndoRedo : ElectronObject
{
    /// <summary>Selects how a matching recent action combines with its predecessor.</summary>
    public enum MergeMode
    {
        /// <summary>Always append an independent action.</summary>
        Disable = 0,
        /// <summary>Keep the earliest undo and latest do state, plus forced operations.</summary>
        Ends = 1,
        /// <summary>Append all operations; the merging commit executes only the newly added do suffix.</summary>
        All = 2
    }
    private abstract class Operation(bool forceKeep)
    {
        internal readonly bool ForceKeep = forceKeep;
        internal abstract void Execute();
    }
    private sealed class MethodOperation(Action callback, bool forceKeep) : Operation(forceKeep)
    {
        internal override void Execute() { if (callback.Target is not ElectronObject { IsDisposed: true }) callback(); }
    }
    private sealed class PropertyOperation<TOwner, TValue>(TOwner owner, PropertyDescriptor<TOwner, TValue> property, TValue value, bool forceKeep) : Operation(forceKeep)
        where TOwner : ElectronObject
    {
        internal override void Execute() { if (!owner.IsDisposed) property.SetValue(owner, value); }
    }
    private sealed class ReferenceOperation(ElectronObject owner, bool forceKeep) : Operation(forceKeep)
    {
        internal readonly ElectronObject Owner = owner;
        internal override void Execute() { }
    }
    private sealed class HistoryAction(string name, long tick, bool backwards)
    {
        internal readonly string Name = name;
        internal long Tick = tick;
        internal readonly bool Backwards = backwards;
        internal readonly List<Operation> Do = [], Undo = [];
    }
    private readonly int _thread = Environment.CurrentManagedThreadId;
    private readonly Func<long> _clock;
    private readonly List<HistoryAction> _history = [];
    private readonly Dictionary<ElectronObject, int> _references = new(ReferenceEqualityComparer.Instance);
    private HistoryAction? _pending;
    private MergeMode _merge;
    private int _cursor = -1, _depth, _maxSteps;
    private bool _forceKeep, _busy, _committing;
    private ulong _version = 1;
    /// <summary>Creates an empty history owned by the current thread, with version one and unlimited steps.</summary>
    public UndoRedo() : this(ReadClock) { }
    internal UndoRedo(Func<long> clock) => _clock = clock;
    private static long ReadClock() => Environment.TickCount64;
    private EventHandlers<Action>? _versionHandlers;
    /// <summary>Occurs after a committed version transition, including merged commits that retain the version.</summary>
    /// <remarks>Observers run in registration order. Mutation/disposal is rejected during dispatch;
    /// errors do not suppress later observers or revert cursor/version.</remarks>
    public event Action? VersionChanged
    {
        add { CheckMutation(); EventHandlers<Action>.Add(ref _versionHandlers, value); }
        remove { CheckMutation(); _versionHandlers?.Remove(value); }
    }
    /// <summary>Gets or sets the maximum retained action count, enforced after top-level commit.</summary>
    /// <value>Zero initially. Nonpositive values mean unlimited and are retained unchanged.</value>
    /// <exception cref="InvalidOperationException">Access is off-owner or mutation occurs during dispatch.</exception>
    /// <exception cref="ObjectDisposedException">The history is disposed.</exception>
    public int MaxSteps { get { Check(); return _maxSteps; } set { CheckMutation(); _maxSteps = value; } }
    /// <summary>Starts an action, or adds a nesting level to the existing outer action.</summary>
    /// <param name="name">Action name; empty is valid.</param>
    /// <param name="mergeMode">Merge policy for a same-name/same-undo-order action created less than 800 ms earlier.</param>
    /// <param name="backwardUndoOps">Runs undo operations in reverse registration order when true.</param>
    /// <remarks>Top-level creation discards redo immediately. Nested calls keep the outer name/policy.
    /// The committed history stays intact while authoring. Creation resets force keeping.</remarks>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The merge mode is undefined.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner or dispatch-owned.</exception>
    /// <exception cref="AggregateException">Retiring redo-owned objects fails after the branch is discarded.</exception>
    /// <exception cref="ObjectDisposedException">The history is disposed.</exception>
    public void CreateAction(string name, MergeMode mergeMode = MergeMode.Disable, bool backwardUndoOps = false)
    {
        CheckMutation(); ArgumentNullException.ThrowIfNull(name); if ((uint)mergeMode > 2) throw new ArgumentOutOfRangeException(nameof(mergeMode));
        if (_depth != 0) { _depth = checked(_depth + 1); _forceKeep = false; return; }
        List<Exception>? errors = null; _busy = true;
        try { DiscardRedo(ref errors); } finally { _busy = false; }
        ThrowErrors(errors);
        var tick = _clock(); _merge = MergeMode.Disable;
        if (mergeMode != MergeMode.Disable && _history.Count != 0)
        {
            var previous = _history[^1];
            if (previous.Name == name && previous.Backwards == backwardUndoOps && unchecked((ulong)(tick - previous.Tick)) < 800) _merge = mergeMode;
        }
        _pending = new(name, tick, backwardUndoOps); _depth = 1; _forceKeep = false;
    }
    /// <summary>Registers a do callback, retained until its action retires.</summary>
    /// <param name="callback">Callback invoked without arguments. Disposed direct engine targets are skipped.</param>
    /// <exception cref="ArgumentNullException">The callback is null.</exception>
    /// <exception cref="InvalidOperationException">No action is open, or access is off-owner/dispatch-owned.</exception>
    /// <exception cref="ObjectDisposedException">The history is disposed.</exception>
    public void AddDoMethod(Action callback) { CheckAuthoring(); ArgumentNullException.ThrowIfNull(callback); _pending!.Do.Add(new MethodOperation(callback, _forceKeep)); }
    /// <summary>Registers an undo callback, unless an unforced Ends merge preserves the previous undo state.</summary>
    /// <param name="callback">Borrowed callback; ordinary multicast delegate behavior applies within one operation.</param>
    /// <exception cref="ArgumentNullException">The callback is null.</exception>
    /// <exception cref="InvalidOperationException">No action is open, or access is off-owner/dispatch-owned.</exception>
    /// <exception cref="ObjectDisposedException">The history is disposed.</exception>
    public void AddUndoMethod(Action callback) { CheckAuthoring(); ArgumentNullException.ThrowIfNull(callback); if (_merge != MergeMode.Ends || _forceKeep) _pending!.Undo.Add(new MethodOperation(callback, _forceKeep)); }
    /// <summary>Registers a typed do-property write; registration does not execute its validator or setter.</summary>
    /// <typeparam name="TOwner">Concrete property owner type.</typeparam><typeparam name="TValue">Captured value type.</typeparam>
    /// <param name="owner">Borrowed live target.</param><param name="property">Writable typed descriptor.</param><param name="value">Value captured by ordinary assignment.</param>
    /// <exception cref="ArgumentNullException">Owner or descriptor is null.</exception>
    /// <exception cref="InvalidOperationException">No action is open, descriptor is read-only, or access is off-owner/dispatch-owned.</exception>
    /// <exception cref="ObjectDisposedException">History or target is disposed.</exception>
    public void AddDoProperty<TOwner, TValue>(TOwner owner, PropertyDescriptor<TOwner, TValue> property, TValue value) where TOwner : ElectronObject
    { CheckAuthoring(); ValidatePropertyTarget(owner, property); _pending!.Do.Add(new PropertyOperation<TOwner, TValue>(owner, property, value, _forceKeep)); }
    /// <summary>Registers a typed undo-property write, respecting Ends merge force keeping.</summary>
    /// <typeparam name="TOwner">Concrete property owner type.</typeparam><typeparam name="TValue">Captured value type.</typeparam>
    /// <param name="owner">Borrowed live target.</param><param name="property">Writable typed descriptor.</param><param name="value">Value captured by ordinary assignment.</param>
    /// <exception cref="ArgumentNullException">Owner or descriptor is null.</exception>
    /// <exception cref="InvalidOperationException">No action is open, descriptor is read-only, or access is off-owner/dispatch-owned.</exception>
    /// <exception cref="ObjectDisposedException">History or target is disposed.</exception>
    public void AddUndoProperty<TOwner, TValue>(TOwner owner, PropertyDescriptor<TOwner, TValue> property, TValue value) where TOwner : ElectronObject
    { CheckAuthoring(); ValidatePropertyTarget(owner, property); if (_merge != MergeMode.Ends || _forceKeep) _pending!.Undo.Add(new PropertyOperation<TOwner, TValue>(owner, property, value, _forceKeep)); }
    private static void ValidatePropertyTarget<TOwner, TValue>(TOwner owner, PropertyDescriptor<TOwner, TValue> property) where TOwner : ElectronObject
    { ArgumentNullException.ThrowIfNull(owner); ArgumentNullException.ThrowIfNull(property); ObjectDisposedException.ThrowIf(owner.IsDisposed, owner); if (property.IsReadOnly) throw new InvalidOperationException("Undo history requires a writable property."); }
    /// <summary>Transfers conditional object ownership to the do side, disposed when that redo branch retires.</summary>
    /// <param name="owner">Live non-Resource object. Resource assets remain borrowed through normal value/callback operations.</param>
    /// <exception cref="ArgumentException">The object is a Resource.</exception><exception cref="ArgumentNullException">The object is null.</exception>
    /// <exception cref="InvalidOperationException">No action is open, or access is off-owner/dispatch-owned.</exception>
    /// <exception cref="ObjectDisposedException">History or object is disposed.</exception>
    public void AddDoReference(ElectronObject owner) { CheckAuthoring(); AddReference(owner, _pending!.Do); }
    /// <summary>Transfers conditional object ownership to the undo side, disposed when its applied history retires.</summary>
    /// <param name="owner">Live non-Resource object; unforced Ends registrations are ignored after validation.</param>
    /// <exception cref="ArgumentException">The object is a Resource.</exception><exception cref="ArgumentNullException">The object is null.</exception>
    /// <exception cref="InvalidOperationException">No action is open, or access is off-owner/dispatch-owned.</exception>
    /// <exception cref="ObjectDisposedException">History or object is disposed.</exception>
    public void AddUndoReference(ElectronObject owner) { CheckAuthoring(); ValidateReference(owner); if (_merge != MergeMode.Ends || _forceKeep) AddReference(owner, _pending!.Undo); }
    private static void ValidateReference(ElectronObject owner)
    { ArgumentNullException.ThrowIfNull(owner); ObjectDisposedException.ThrowIf(owner.IsDisposed, owner); if (owner is Resource) throw new ArgumentException("Resource assets cannot transfer ownership to undo history.", nameof(owner)); }
    private void AddReference(ElectronObject owner, List<Operation> operations)
    { ValidateReference(owner); var count = _references.GetValueOrDefault(owner); var next = checked(count + 1); var operation = new ReferenceOperation(owner, _forceKeep); operations.Add(operation); _references[owner] = next; }
    /// <summary>Marks subsequently registered do and undo operations as retained by Ends merges.</summary>
    /// <exception cref="InvalidOperationException">No action is open, or access is off-owner/dispatch-owned.</exception>
    /// <exception cref="ObjectDisposedException">The history is disposed.</exception>
    public void StartForceKeepInMergeEnds() { CheckAuthoring(); _forceKeep = true; }
    /// <summary>Stops force keeping subsequent registrations.</summary>
    /// <exception cref="InvalidOperationException">No action is open, or access is off-owner/dispatch-owned.</exception>
    /// <exception cref="ObjectDisposedException">The history is disposed.</exception>
    public void EndForceKeepInMergeEnds() { CheckAuthoring(); _forceKeep = false; }
    /// <summary>Closes one action nesting level; top-level closure records and optionally executes the edit.</summary>
    /// <param name="execute">False records already-applied state without invoking do operations.</param>
    /// <remarks>Errors do not roll back user side effects or cursor/version. Each operation, notification and
    /// required retirement is attempted. A merged commit retains its version. MaxSteps trimming follows notification.</remarks>
    /// <exception cref="InvalidOperationException">No action is open, or access is off-owner/dispatch-owned.</exception>
    /// <exception cref="AggregateException">Operations, observers or retirement fail.</exception>
    /// <exception cref="ObjectDisposedException">The history is disposed.</exception>
    public void CommitAction(bool execute = true)
    {
        CheckAuthoring(); if (--_depth != 0) return;
        var pending = _pending!; _pending = null; var merged = _merge != MergeMode.Disable;
        List<Exception>? errors = null; _busy = _committing = true;
        try
        {
            var start = 0; HistoryAction action;
            if (!merged) { action = pending; _history.Add(action); _cursor = _history.Count - 1; }
            else
            {
                action = _history[^1];
                if (_merge == MergeMode.All) start = action.Do.Count;
                else
                    for (var i = action.Do.Count - 1; i >= 0; i--)
                        if (!action.Do[i].ForceKeep) { var old = action.Do[i]; action.Do.RemoveAt(i); Retire(old, true, ref errors); }
                action.Do.AddRange(pending.Do); action.Undo.AddRange(pending.Undo); action.Tick = pending.Tick;
            }
            if (execute) Execute(action.Do, start, false, ref errors);
            if (!merged) _version = unchecked(_version + 1);
            NotifyVersion(ref errors);
            if (_maxSteps > 0 && _history.Count > _maxSteps) TrimApplied(_history.Count - _maxSteps, ref errors);
        }
        finally { _busy = _committing = false; _merge = MergeMode.Disable; _forceKeep = false; }
        ThrowErrors(errors);
    }
    /// <summary>Undoes the current action and decrements the version.</summary>
    /// <returns>False when no undo is available, true after successful playback.</returns>
    /// <exception cref="InvalidOperationException">An action is open, or access is off-owner/dispatch-owned.</exception>
    /// <exception cref="AggregateException">Operations or observers fail after the cursor/version transition.</exception>
    /// <exception cref="ObjectDisposedException">The history is disposed.</exception>
    public bool Undo()
    {
        CheckPlayback(); if (_cursor < 0) return false; List<Exception>? errors = null; _busy = true;
        try { var action = _history[_cursor]; Execute(action.Undo, 0, action.Backwards, ref errors); _cursor--; _version = unchecked(_version - 1); NotifyVersion(ref errors); }
        finally { _busy = false; }
        ThrowErrors(errors); return true;
    }
    /// <summary>Redoes the next action and increments the version.</summary>
    /// <returns>False when no redo is available, true after successful playback.</returns>
    /// <exception cref="InvalidOperationException">An action is open, or access is off-owner/dispatch-owned.</exception>
    /// <exception cref="AggregateException">Operations or observers fail after the cursor/version transition.</exception>
    /// <exception cref="ObjectDisposedException">The history is disposed.</exception>
    public bool Redo()
    {
        CheckPlayback(); if (_cursor + 1 >= _history.Count) return false; List<Exception>? errors = null; _busy = true;
        try { _cursor++; Execute(_history[_cursor].Do, 0, false, ref errors); _version = unchecked(_version + 1); NotifyVersion(ref errors); }
        finally { _busy = false; }
        ThrowErrors(errors); return true;
    }
    /// <summary>Clears completed history and releases conditional ownership according to applied state.</summary>
    /// <param name="increaseVersion">Increments and notifies when true, even for an empty history.</param>
    /// <exception cref="InvalidOperationException">An action is open, or access is off-owner/dispatch-owned.</exception>
    /// <exception cref="AggregateException">Object retirement or observers fail after history removal.</exception>
    /// <exception cref="ObjectDisposedException">The history is disposed.</exception>
    public void ClearHistory(bool increaseVersion = true)
    {
        CheckPlayback(); List<Exception>? errors = null; _busy = true;
        try { Clear(ref errors); if (increaseVersion) { _version = unchecked(_version + 1); NotifyVersion(ref errors); } }
        finally { _busy = false; }
        ThrowErrors(errors);
    }
    /// <summary>Reports whether committed history has an undo action; authoring does not change this committed cursor.</summary>
    /// <returns>True when the committed cursor is nonnegative.</returns><exception cref="InvalidOperationException">Access is off-owner.</exception><exception cref="ObjectDisposedException">The history is disposed.</exception>
    public bool HasUndo() { Check(); return _cursor >= 0; }
    /// <summary>Reports whether committed history has a redo action.</summary>
    /// <returns>True when a committed action follows the cursor.</returns><exception cref="InvalidOperationException">Access is off-owner.</exception><exception cref="ObjectDisposedException">The history is disposed.</exception>
    public bool HasRedo() { Check(); return _cursor + 1 < _history.Count; }
    /// <summary>Returns the committed history count.</summary>
    /// <returns>Number of retained actions.</returns><exception cref="InvalidOperationException">An action is open or access is off-owner.</exception><exception cref="ObjectDisposedException">The history is disposed.</exception>
    public int GetHistoryCount() { CheckCompleted(); return _history.Count; }
    /// <summary>Returns the committed current action index.</summary>
    /// <returns>Minus one before the first action.</returns><exception cref="InvalidOperationException">An action is open or access is off-owner.</exception><exception cref="ObjectDisposedException">The history is disposed.</exception>
    public int GetCurrentAction() { CheckCompleted(); return _cursor; }
    /// <summary>Returns the name at a committed history index.</summary>
    /// <param name="id">Zero-based committed index.</param><returns>The retained name.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside committed history.</exception><exception cref="InvalidOperationException">Access is off-owner.</exception><exception cref="ObjectDisposedException">The history is disposed.</exception>
    public string GetActionName(int id) { Check(); if ((uint)id >= (uint)_history.Count) throw new ArgumentOutOfRangeException(nameof(id)); return _history[id].Name; }
    /// <summary>Returns the committed current action name.</summary>
    /// <returns>Empty before the first action.</returns><exception cref="InvalidOperationException">An action is open or access is off-owner.</exception><exception cref="ObjectDisposedException">The history is disposed.</exception>
    public string GetCurrentActionName() { CheckCompleted(); return _cursor < 0 ? string.Empty : _history[_cursor].Name; }
    /// <summary>Returns the current unsigned history version, initially one.</summary>
    /// <returns>The committed version; merged commits preserve it.</returns><exception cref="InvalidOperationException">Access is off-owner.</exception><exception cref="ObjectDisposedException">The history is disposed.</exception>
    public ulong GetVersion() { Check(); return _version; }
    /// <summary>Reports initial commit execution, notification and retirement dispatch.</summary>
    /// <returns>True during top-level CommitAction dispatch, false during ordinary Undo/Redo.</returns><exception cref="InvalidOperationException">Access is off-owner.</exception><exception cref="ObjectDisposedException">The history is disposed.</exception>
    public bool IsCommittingAction() { Check(); return _committing; }
    private void Check() { ThrowIfDisposed(); if (Environment.CurrentManagedThreadId != _thread) throw new InvalidOperationException("Undo history belongs to its creating thread."); }
    private void CheckMutation() { Check(); ObjectDisposedException.ThrowIf(IsDisposed, this); if (_busy) throw new InvalidOperationException("Undo history cannot mutate during dispatch."); }
    private void CheckAuthoring() { CheckMutation(); if (_depth == 0) throw new InvalidOperationException("Create an action before registering or committing operations."); }
    private void CheckCompleted() { Check(); if (_depth != 0) throw new InvalidOperationException("Complete the open action before querying committed metadata."); }
    private void CheckPlayback() { CheckMutation(); if (_depth != 0) throw new InvalidOperationException("Complete the open action before playback or clearing."); }
    private static void Execute(List<Operation> operations, int start, bool backwards, ref List<Exception>? errors)
    {
        if (backwards) for (var i = operations.Count - 1; i >= start; i--) { try { operations[i].Execute(); } catch (Exception e) { (errors ??= []).Add(e); } }
        else for (var i = start; i < operations.Count; i++) { try { operations[i].Execute(); } catch (Exception e) { (errors ??= []).Add(e); } }
    }
    private void NotifyVersion(ref List<Exception>? errors)
    {
        using var handlers = BeginBufferedEvent(_versionHandlers);
        for (var i = 0; i < handlers.Count; i++)
            try { handlers[i](); } catch (Exception e) { (errors ??= []).Add(e); }
    }
    private static void ThrowErrors(List<Exception>? errors) { if (errors is { Count: > 0 }) throw new AggregateException("Undo history operations failed after state publication.", errors); }
    private void Retire(Operation operation, bool dispose, ref List<Exception>? errors)
    {
        if (operation is not ReferenceOperation reference) return;
        var owner = reference.Owner; var count = _references[owner];
        if (count > 1) { _references[owner] = count - 1; return; }
        _references.Remove(owner);
        if (dispose && !owner.IsDisposed) try { owner.Dispose(); } catch (Exception e) { (errors ??= []).Add(e); }
    }
    private void Retire(HistoryAction action, bool applied, ref List<Exception>? errors)
    {
        foreach (var operation in action.Do) Retire(operation, !applied, ref errors);
        foreach (var operation in action.Undo) Retire(operation, applied, ref errors);
        action.Do.Clear(); action.Undo.Clear();
    }
    private void DiscardRedo(ref List<Exception>? errors)
    {
        var first = _cursor + 1; var count = _history.Count - first;
        if (count == 0) return;
        var retired = _history.GetRange(first, count); _history.RemoveRange(first, count);
        foreach (var action in retired) Retire(action, false, ref errors);
    }
    private void TrimApplied(int count, ref List<Exception>? errors)
    {
        var retired = _history.GetRange(0, count); _history.RemoveRange(0, count); _cursor -= count;
        foreach (var action in retired) Retire(action, true, ref errors);
    }
    private void Clear(ref List<Exception>? errors)
    {
        var retired = _history.ToArray(); var applied = _cursor; _history.Clear(); _cursor = -1;
        // Preserve branch retirement before applied-prefix retirement, with linear storage movement.
        for (var i = applied + 1; i < retired.Length; i++) Retire(retired[i], false, ref errors);
        for (var i = 0; i <= applied; i++) Retire(retired[i], true, ref errors);
    }
    /// <inheritdoc />
    protected override void ValidateMutation() { base.ValidateMutation(); CheckMutation(); }
    /// <inheritdoc />
    protected override void ValidateDisposal() { base.ValidateDisposal(); if (!IsDisposed) { Check(); if (_busy) throw new InvalidOperationException("Undo history cannot dispose during dispatch."); } }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        yield return MaximumProperty;
    }
    private static readonly PropertyDescriptor MaximumProperty = new PropertyDescriptor<UndoRedo, int>(nameof(MaxSteps), h => h.MaxSteps, (h, v) => h.MaxSteps = v, _ => 0);
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        List<Exception>? errors = null;
        if (disposing)
        {
            _versionHandlers = null; _busy = true;
            if (_pending is { } pending) { _pending = null; _depth = 0; Retire(pending, false, ref errors); }
            Clear(ref errors); _busy = false;
        }
        try { base.Dispose(disposing); } catch (Exception e) { (errors ??= []).Add(e); }
        ThrowErrors(errors);
    }
}
