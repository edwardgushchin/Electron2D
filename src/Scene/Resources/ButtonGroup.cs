namespace Electron2D;

/// <summary>Coordinates mutually exclusive toggle buttons without owning its member nodes.</summary>
/// <remarks>Membership is runtime state. Copies contain only configuration; scene-local duplication creates an
/// independent group for each scene instance. Programmatic no-signal assignments can intentionally leave more
/// than one member pressed. Button ordering is not a public guarantee.</remarks>
public class ButtonGroup : Resource
{
    private readonly object _gate = new();
    private readonly List<WeakReference<BaseButton>> _buttons = [];
    private readonly Stack<List<(BaseButton Button, ulong Generation)>> _snapshots = [];
    private bool _allowUnpress;
    private static readonly PropertyDescriptor[] GroupProperties =
    [
        new PropertyDescriptor<ButtonGroup,bool>(nameof(AllowUnpress),g=>g.AllowUnpress,(g,v)=>g.AllowUnpress=v,_=>false,stored:true),
        new PropertyDescriptor<ButtonGroup,bool>(nameof(ResourceLocalToScene),g=>g.ResourceLocalToScene,(g,v)=>g.ResourceLocalToScene=v,_=>true,stored:true)
    ];
    /// <summary>Creates a scene-local group which disallows toggling off its last active radio button.</summary>
    public ButtonGroup() { ResourceLocalToScene = true; }
    /// <summary>Occurs after peer buttons are unpressed and before the activating button's Toggled event.</summary>
    public event Action<BaseButton>? Pressed;
    /// <summary>Gets or sets whether an activation can leave every group member unpressed.</summary>
    /// <value>False initially. Assignment is silent and does not alter current button states.</value>
    /// <exception cref="ObjectDisposedException">The group is disposed.</exception>
    public bool AllowUnpress { get { lock (_gate) { ThrowIfDisposed(); return _allowUnpress; } } set { lock (_gate) { ThrowIfDisposed(); _allowUnpress = value; } } }
    /// <summary>Returns a caller-owned snapshot of live buttons assigned to this group.</summary>
    /// <returns>Borrowed member nodes; the returned array does not change with later membership changes.</returns>
    /// <exception cref="ObjectDisposedException">The group is disposed.</exception>
    public BaseButton[] GetButtons()
    {
        var snapshot = TakeSnapshot();
        try { var result = new BaseButton[snapshot.Count]; for (var i = 0; i < result.Length; i++) result[i] = snapshot[i].Button; return result; }
        finally { ReturnSnapshot(snapshot); }
    }
    /// <summary>Returns a pressed member, or null if none is pressed.</summary>
    /// <returns>A borrowed button. If silent assignments left multiple members pressed, selection is unspecified.</returns>
    /// <exception cref="InvalidOperationException">An attached member is queried off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The group is disposed.</exception>
    public BaseButton? GetPressedButton()
    {
        var snapshot = TakeSnapshot();
        try { foreach (var entry in snapshot) if (Current(entry) && entry.Button.ButtonPressed) return entry.Button; return null; }
        finally { ReturnSnapshot(snapshot); }
    }
    internal void Add(WeakReference<BaseButton> button) { lock (_gate) { ThrowIfDisposed(); if (!_buttons.Contains(button)) _buttons.Add(button); } }
    internal void Remove(WeakReference<BaseButton> button) { lock (_gate) _buttons.Remove(button); }
    internal void ValidateMembers()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            foreach (var weak in _buttons) if (weak.TryGetTarget(out var b) && !b.IsDisposed && ReferenceEquals(b.MembershipGroup, this)) b.ValidateGroupMutation();
        }
    }
    private List<(BaseButton Button, ulong Generation)> TakeSnapshot()
    {
        lock (_gate)
        {
            ThrowIfDisposed(); var result = _snapshots.Count == 0 ? [] : _snapshots.Pop();
            for (var i = 0; i < _buttons.Count;)
            {
                if (!_buttons[i].TryGetTarget(out var b) || b.IsDisposed || !ReferenceEquals(b.MembershipGroup, this)) { _buttons.RemoveAt(i); continue; }
                result.Add((b, b.MembershipGeneration)); i++;
            }
            return result;
        }
    }
    private void ReturnSnapshot(List<(BaseButton Button, ulong Generation)> snapshot) { snapshot.Clear(); lock (_gate) if (!IsDisposed) _snapshots.Push(snapshot); }
    private bool Current((BaseButton Button, ulong Generation) entry) => !entry.Button.IsDisposed && ReferenceEquals(entry.Button.MembershipGroup, this) && entry.Button.MembershipGeneration == entry.Generation;
    internal void UnpressOthers(BaseButton source)
    {
        var snapshot = TakeSnapshot(); List<Exception>? errors = null; var generation = source.InteractionGeneration;
        try
        {
            foreach (var entry in snapshot)
            {
                if (source.IsDisposed || source.InteractionGeneration != generation || !ReferenceEquals(source.MembershipGroup, this)) break;
                if (!ReferenceEquals(entry.Button, source) && Current(entry))
                    try { entry.Button.ButtonPressed = false; } catch (Exception error) { Node.CollectException(ref errors, error); }
            }
        }
        finally { ReturnSnapshot(snapshot); }
        Node.ThrowCollected("Button group callbacks failed.", errors);
    }
    internal void NotifyPressed(BaseButton button) { ThrowIfDisposed(); Pressed?.Invoke(button); }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => GetType() == typeof(ButtonGroup) ? new ButtonGroup() : base.CreateDuplicateInstance();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        ((ButtonGroup)target).AllowUnpress = AllowUnpress;
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Where(p => p.Name != nameof(ResourceLocalToScene)).Concat(GroupProperties);
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        lock (_gate) { _buttons.Clear(); _snapshots.Clear(); Pressed = null; }
        base.Dispose(disposing);
    }
}
