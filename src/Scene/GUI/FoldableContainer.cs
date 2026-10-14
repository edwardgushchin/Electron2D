namespace Electron2D;

/// <summary>Arranges ordinary child controls beneath or above a collapsible themed title bar.</summary>
/// <remarks>Folding hides ordinary non-top-level controls; expanding shows them. Internal title controls remain visible.
/// Group resources borrow members and are scene-local by default. Attached access follows scene ownership.</remarks>
public partial class FoldableContainer : Container
{
    private bool _folded, _hovered, _changingGroup;
    private bool? _pendingFold;
    private ulong _foldRevision, _membership;
    private string _title = "", _language = "";
    private HorizontalAlignment _alignment;
    private BarPosition _titlePosition;
    private TextDirection _direction;
    private TextOverrunBehavior _overrun;
    private FoldableGroup? _group;
    private readonly WeakReference<FoldableContainer> _groupReference;
    private readonly List<Control> _titleControls = [];
    internal FoldableGroup? MembershipGroup => _group;
    internal ulong MembershipGeneration => _membership;
    internal ulong InteractionGeneration => _foldRevision;
    internal void ValidateGroupMutation() => EnsureMutable();
    private void CheckFoldable() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }

    /// <summary>Creates an expanded empty panel with stopping pointer input and keyboard focus.</summary>
    public FoldableContainer() { _groupReference = new(this); MouseFilter = MouseFilter.Stop; FocusMode = FocusMode.All; ChildRemoved += TitleControlRemoved; }
    /// <summary>Creates an expanded panel with untranslated title text.</summary>
    /// <param name="title">Initial title; cannot be null.</param>
    public FoldableContainer(string title) : this() { Title = title; }
    /// <summary>Occurs after Fold, Expand or user activation, including an unchanged or group-refused request.</summary>
    /// <remarks>Property assignments are silent. The event supplies the actual folded state.</remarks>
    public event Action<bool>? FoldingChanged;
    /// <summary>Gets or sets whether the content is folded; a group can refuse closing its last expanded member.</summary>
    /// <value>False initially. Assignments do not emit FoldingChanged.</value>
    public bool Folded { get { CheckFoldable(); return _folded; } set { EnsureMutable(); SetFoldedCore(value); } }
    /// <summary>Gets or assigns a borrowed scene-local group coordinating mutually exclusive expansion.</summary>
    /// <value>Null initially. Assignment reconciles this member against existing expansion without emitting group events.</value>
    public FoldableGroup? FoldableGroup
    {
        get { CheckFoldable(); return _group; }
        set
        {
            EnsureMutable(); value?.ValidateMembers(); _group?.Remove(_groupReference); _group = value; _membership++; _foldRevision++;
            if (value is not null)
            {
                _changingGroup = true;
                try
                {
                    var selected = value.GetExpandedContainer();
                    if (_folded && selected is null && !value.AllowFoldingAll) SetFoldedCore(false, true);
                    else if (!_folded && selected is not null) SetFoldedCore(true, true);
                }
                finally { _changingGroup = false; if (!IsDisposed && ReferenceEquals(_group, value)) value.Add(_groupReference); }
            }
            QueueRedraw();
        }
    }
    /// <summary>Gets or sets untranslated title text; equal assignments are silent.</summary>
    /// <value>Empty initially.</value>
    public string Title { get { CheckFoldable(); return _title; } set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); if (_title == value) return; _title = value; InvalidateTitle(); } }
    /// <summary>Gets or sets an explicit shaping language; empty uses the translation domain locale.</summary>
    /// <value>Empty initially.</value>
    public string Language { get { CheckFoldable(); return _language; } set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); if (_language == value) return; _language = value; InvalidateTitle(); } }
    /// <summary>Gets or sets left, center or right title alignment, mirrored for RTL layout.</summary>
    /// <value>Left initially; Fill is not selectable.</value>
    public HorizontalAlignment TitleAlignment { get { CheckFoldable(); return _alignment; } set { EnsureMutable(); if (value is < HorizontalAlignment.Left or > HorizontalAlignment.Right) throw new ArgumentOutOfRangeException(nameof(value)); _alignment = value; InvalidateTitle(); } }
    /// <summary>Gets or selects title placement above or below content.</summary>
    /// <value>Top initially; Max is nonselectable.</value>
    public BarPosition TitlePosition { get { CheckFoldable(); return _titlePosition; } set { EnsureMutable(); if (value is < BarPosition.Top or >= BarPosition.Max) throw new ArgumentOutOfRangeException(nameof(value)); if (_titlePosition == value) return; _titlePosition = value; InvalidateTitle(); } }
    /// <summary>Gets or sets the title's bidirectional text policy.</summary>
    /// <value>Auto initially.</value>
    public TextDirection TitleTextDirection { get { CheckFoldable(); return _direction; } set { EnsureMutable(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); _direction = value; InvalidateTitle(); } }
    /// <summary>Gets or sets how title text exceeding available width is trimmed.</summary>
    /// <value>NoTrimming initially.</value>
    public TextOverrunBehavior TitleTextOverrunBehavior { get { CheckFoldable(); return _overrun; } set { EnsureMutable(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); _overrun = value; InvalidateTitle(); } }
    /// <summary>Requests folding and emits the actual resulting state.</summary>
    public void Fold() => RequestFold(true);
    /// <summary>Requests expansion and emits the actual resulting state.</summary>
    public void Expand() => RequestFold(false);
    private void RequestFold(bool value)
    {
        EnsureMutable(); List<Exception>? errors = null;
        try { SetFoldedCore(value); } catch (Exception error) { CollectException(ref errors, error); }
        try { if (!IsDisposed) FoldingChanged?.Invoke(_folded); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Folding callbacks failed.", errors);
    }
    internal void SetFoldedCore(bool value, bool forced = false)
    {
        EnsureMutable(); if (_pendingFold == value) return;
        if (_folded == value) { if (_pendingFold is not null) { _foldRevision++; _pendingFold = null; } return; }
        var group = _group is { IsDisposed: false } ? _group : null;
        if (!forced && value && group is not null && !group.AllowFoldingAll && ReferenceEquals(group.GetExpandedContainer(), this)) return;
        group?.ValidateMembers(); var revision = ++_foldRevision; var membership = _membership; _pendingFold = value;
        var selection = !value && !_changingGroup && group is not null ? group.BeginSelection() : 0; List<Exception>? errors = null;
        try
        {
            if (selection != 0)
            {
                try { group!.FoldOthers(this); } catch (Exception error) { CollectException(ref errors, error); }
                try { if (!IsDisposed && revision == _foldRevision && membership == _membership && group!.CurrentSelection(selection)) group.NotifyExpanded(this); } catch (Exception error) { CollectException(ref errors, error); }
            }
            if (!IsDisposed && revision == _foldRevision && membership == _membership && (selection == 0 || group!.CurrentSelection(selection)))
            {
                _folded = value;
                try { InvalidateTitle(shaping: false); } catch (Exception error) { CollectException(ref errors, error); }
            }
        }
        finally { if (revision == _foldRevision) _pendingFold = null; }
        ThrowCollected("Foldable group change callbacks failed.", errors);
    }
    /// <summary>Adopts a control as an internal front title-bar child.</summary>
    /// <param name="control">Live control; reparented from its previous parent.</param>
    /// <exception cref="ArgumentException">The control is already registered here or would create a hierarchy cycle.</exception>
    public void AddTitleBarControl(Control control)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(control); control.EnsureSceneActivationAvailable(); ObjectDisposedException.ThrowIf(control.IsDisposed, control);
        if (_titleControls.Contains(control) || ReferenceEquals(control, this) || control.IsAncestorOf(this)) throw new ArgumentException("Invalid title control membership.", nameof(control));
        if (GetNodeOrNull(control.Name) is { } conflict && !ReferenceEquals(conflict, control)) throw new ArgumentException("A child already has this name.", nameof(control));
        List<Exception>? errors = null;
        try { control.Parent?.RemoveChild(control); } catch (Exception error) { CollectException(ref errors, error); }
        if (!control.IsDisposed && control.Parent is null) try { AddChild(control, InternalMode.Front); } catch (Exception error) { CollectException(ref errors, error); }
        if (!control.IsDisposed && control.IsInternalChild && ReferenceEquals(control.Parent, this) && !_titleControls.Contains(control)) _titleControls.Add(control);
        try { InvalidateTitle(); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Title control adoption callbacks failed.", errors);
    }
    /// <summary>Detaches a previously registered title control without disposing it.</summary>
    /// <param name="control">A live title-bar member.</param>
    public void RemoveTitleBarControl(Control control)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(control);
        if (!_titleControls.Remove(control)) throw new ArgumentException("Control is not in the title bar.", nameof(control));
        try { if (ReferenceEquals(control.Parent, this)) RemoveChild(control); } finally { InvalidateTitle(); }
    }
    internal bool IsTitleControl(Node node) => node is Control control && _titleControls.Contains(control);
    private void TitleControlRemoved(Node _, Node child) { if (child is Control control && _titleControls.Remove(control) && !IsDisposed) InvalidateTitle(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        _group?.Remove(_groupReference); _membership++; _foldRevision++; _group = null; ReleaseIcons();
        try { base.Dispose(disposing); } finally { FoldingChanged = null; _titleControls.Clear(); }
    }
}
