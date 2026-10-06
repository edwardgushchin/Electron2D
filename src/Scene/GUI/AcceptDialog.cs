namespace Electron2D;

/// <summary>An embedded notification dialog with a message, an OK button and optional custom actions.</summary>
/// <remarks>Attach below a viewport with GUIEmbedSubwindows enabled, then use Window.Popup methods.
/// Internal controls are owned by the dialog; returned controls are borrowed. Native child windows remain unavailable.</remarks>
public partial class AcceptDialog : Window
{
    private readonly Panel _panel;
    private readonly Label _label;
    private readonly HBoxContainer _buttons;
    private readonly Button _ok;
    private readonly Dictionary<Button, (Control Spacer, Action? Press)> _bindings = [];
    private readonly HashSet<LineEdit> _textEntries = [];
    private string _okText = "", _defaultOKText = "OK";
    private bool _hideOnOK = true, _closeOnEscape = true, _disposing, _cancelPending;
    private int _visibilityVersion, _buttonSerial;
    private Window? _parentWindow;

    /// <summary>Creates a hidden, exclusive, transient dialog with wrapped controls and the title Alert!.</summary>
    public AcceptDialog()
    {
        Visible = false; Transient = true; Exclusive = true; MinimizeDisabled = true; MaximizeDisabled = true;
        _panel = new Panel { Name = "_dialog_panel", MouseFilter = MouseFilter.Ignore };
        _label = new Label { Name = "_dialog_text", FocusMode = FocusMode.Accessibility };
        _buttons = new HBoxContainer { Name = "_dialog_buttons" };
        AddChild(_panel, InternalMode.Front); AddChild(_label, InternalMode.Front); AddChild(_buttons, InternalMode.Back);
        _buttons.AddSpacer(false);
        _ok = new Button { Name = "OK", Text = _defaultOKText }; _buttons.AddChild(_ok);
        BindButton(_ok, _buttons.AddSpacer(false), Confirm);
        _label.MinimumSizeChanged += Arrange; _buttons.MinimumSizeChanged += Arrange;
        SizeChanged += Arrange; CloseRequested += Cancel;
        ChildEnteredTree += ChildEntered; ChildExitingTree += ChildLeaving;
        InputEnabled = true; WrapControls = true; KeepTitleVisible = true; Title = "Alert!";
        ClampToEmbedder = true; Arrange();
    }
    /// <summary>Gets or sets whether the message wraps at word boundaries.</summary>
    /// <value>False initially.</value>
    public bool DialogAutowrap { get { CheckDialog(); return _label.AutowrapMode != TextAutowrapMode.Off; } set { EnsureMutable(); _label.AutowrapMode = value ? TextAutowrapMode.Word : TextAutowrapMode.Off; Arrange(); } }
    /// <summary>Gets or sets cancellation by the independently configurable ui_close_dialog action.</summary>
    /// <value>True initially; echoed keys are ignored.</value>
    public bool DialogCloseOnEscape { get { CheckDialog(); return _closeOnEscape; } set { EnsureMutable(); _closeOnEscape = value; } }
    /// <summary>Gets or sets whether accepting hides this dialog before the hook and Confirmed event.</summary>
    /// <value>True initially; false permits validation in the confirmation handler.</value>
    public bool DialogHideOnOK { get { CheckDialog(); return _hideOnOK; } set { EnsureMutable(); _hideOnOK = value; } }
    /// <summary>Gets or sets the message displayed by the owned label.</summary>
    /// <value>An empty string initially.</value>
    /// <exception cref="ArgumentNullException">The message is null.</exception>
    public string DialogText { get { CheckDialog(); return _label.Text; } set { EnsureMutable(); _label.Text = value; Arrange(); } }
    /// <summary>Gets or sets the explicit OK caption; empty uses the subclass default.</summary>
    /// <value>An empty string initially, displaying OK.</value>
    /// <exception cref="ArgumentNullException">The caption is null.</exception>
    public string OKButtonText { get { CheckDialog(); return _okText; } set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); _okText = value; _ok.Text = value.Length == 0 ? _defaultOKText : value; } }
    /// <summary>Occurs after cancellation is scheduled, before the cancellation hook and deferred hiding.</summary>
    public event Action? Canceled;
    /// <summary>Occurs after optional hiding and the confirmation hook.</summary>
    public event Action? Confirmed;
    /// <summary>Occurs before the custom action hook; custom actions do not automatically hide.</summary>
    public event Action<string>? CustomAction;
    /// <summary>Returns the borrowed, required message label; hide it instead of removing or disposing it.</summary>
    /// <returns>The stable internal message label.</returns>
    public Label GetLabel() { CheckDialog(); return _label; }
    /// <summary>Returns the borrowed, required OK button; its Disabled state also gates registered text submission.</summary>
    /// <returns>The stable internal OK button.</returns>
    public Button GetOKButton() { CheckDialog(); return _ok; }
    /// <summary>Adds an owned button and optional custom action on either side of the existing buttons.</summary>
    /// <param name="text">The button caption.</param>
    /// <param name="right">True appends on the right; false prepends on the left before direction mirroring.</param>
    /// <param name="action">An action identifier; empty emits no custom action.</param>
    /// <returns>The borrowed button; RemoveButton transfers its ownership to the caller.</returns>
    /// <exception cref="ArgumentNullException">A string is null.</exception>
    /// <exception cref="AggregateException">An insertion callback fails after the live button and action commit.</exception>
    public Button AddButton(string text, bool right = false, string action = "")
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(text); ArgumentNullException.ThrowIfNull(action);
        return InsertButton(text, right, action.Length == 0 ? null : () => InvokeCustom(action));
    }
    private Button InsertButton(string text, bool right, Action? press)
    {
        var serial = ++_buttonSerial;
        var button = new Button { Text = text, Name = "_action_" + serial };
        var spacer = new Control { Name = "_action_spacer_" + serial, MouseFilter = MouseFilter.Pass, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        BindButton(button, spacer, press);
        List<Exception>? errors = null;
        try { _buttons.AddChild(button); } catch (Exception e) { CollectException(ref errors, e); }
        try { if (!button.IsDisposed && button.Parent == _buttons && !right) _buttons.MoveChild(button, 0); } catch (Exception e) { CollectException(ref errors, e); }
        try { if (!IsDisposed && !button.IsDisposed && button.Parent == _buttons) _buttons.AddChild(spacer); else if (!IsDisposed) UnbindButton(button); } catch (Exception e) { CollectException(ref errors, e); }
        try { if (!spacer.IsDisposed && spacer.Parent == _buttons && !right) _buttons.MoveChild(spacer, 0); } catch (Exception e) { CollectException(ref errors, e); }
        try { if (!IsDisposed) Arrange(); } catch (Exception e) { CollectException(ref errors, e); }
        ThrowCollected("Dialog button insertion callbacks failed.", errors); return button;
    }
    /// <summary>Adds an owned cancellation button using the configured or platform OK/Cancel order.</summary>
    /// <param name="name">The caption; empty displays Cancel.</param>
    /// <returns>The borrowed cancellation button; RemoveButton transfers ownership.</returns>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="AggregateException">An insertion callback fails after the live cancel button commits.</exception>
    public Button AddCancelButton(string name)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(name); var order = ProjectSettings.GetWithOverride(ProjectSettings.SwapCancelOK); return InsertButton(name.Length == 0 ? "Cancel" : name, order == 2 || order == 0 && DisplayServer.PlatformSwapCancelOK, Cancel);
    }
    /// <summary>Detaches a custom button without disposing it and disconnects all dialog-owned callbacks.</summary>
    /// <param name="button">A live button added by AddButton or AddCancelButton.</param>
    /// <exception cref="ArgumentException">The button is not owned by this row, or is the required OK button.</exception>
    /// <exception cref="ArgumentNullException">The button is null.</exception>
    /// <exception cref="AggregateException">A removal callback fails after the ownership transfer is completed.</exception>
    public void RemoveButton(Button button)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(button);
        if (button == _ok || button.IsDisposed || button.Parent != _buttons || !_bindings.ContainsKey(button)) throw new ArgumentException("Expected a live custom dialog button.", nameof(button));
        List<Exception>? errors = null;
        try { UnbindButton(button); } catch (Exception e) { CollectException(ref errors, e); }
        try { if (!button.IsDisposed && button.Parent == _buttons) _buttons.RemoveChild(button); } catch (Exception e) { CollectException(ref errors, e); }
        try { if (!IsDisposed) Arrange(); } catch (Exception e) { CollectException(ref errors, e); }
        ThrowCollected("Dialog button removal callbacks failed.", errors);
    }
    /// <summary>Connects a borrowed text field's submission to confirmation; repeated registration is idempotent.</summary>
    /// <param name="lineEdit">A live text field, which need not be a direct child.</param>
    /// <exception cref="ArgumentNullException">The field is null.</exception>
    /// <exception cref="ObjectDisposedException">The field is disposed.</exception>
    /// <exception cref="InvalidOperationException">The field belongs to a different scene owner thread.</exception>
    public void RegisterTextEnter(LineEdit lineEdit)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(lineEdit); ObjectDisposedException.ThrowIf(lineEdit.IsDisposed, lineEdit); lineEdit.Tree?.EnsureOwnerThread();
        if (_textEntries.Add(lineEdit)) { lineEdit.TextSubmitted += TextSubmitted; lineEdit.Disposed += EntryDisposed; }
    }
    /// <summary>Runs after optional hiding and before Confirmed.</summary>
    protected virtual void OnOKPressed() { }
    /// <summary>Runs after Canceled and before deferred hiding.</summary>
    protected virtual void OnCancelPressed() { }
    /// <summary>Runs after CustomAction without automatic hiding.</summary>
    /// <param name="action">The custom button's action identifier.</param>
    protected virtual void OnCustomAction(string action) { }
    /// <summary>Changes the default OK caption used when OKButtonText is empty.</summary>
    /// <param name="text">The subclass default caption.</param>
    /// <exception cref="ArgumentNullException">The caption is null.</exception>
    protected void SetDefaultOKText(string text) { EnsureMutable(); ArgumentNullException.ThrowIfNull(text); _defaultOKText = text; if (_okText.Length == 0) _ok.Text = text; }
    private void CheckDialog() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    private void BindButton(Button button, Control spacer, Action? press)
    {
        _bindings.Add(button, (spacer, press)); if (press != null) button.Pressed += press;
        button.VisibilityChanged += ButtonVisibility; button.Disposed += ButtonDisposed; button.MinimumSizeChanged += Arrange; spacer.Visible = button.Visible;
    }
    private void UnbindButton(Button button)
    {
        if (!_bindings.Remove(button, out var binding)) return;
        button.Pressed -= binding.Press; button.VisibilityChanged -= ButtonVisibility; button.Disposed -= ButtonDisposed; button.MinimumSizeChanged -= Arrange;
        binding.Spacer.Dispose();
    }
    private void ButtonDisposed(ElectronObject button) { UnbindButton((Button)button); if (!_disposing) Arrange(); }
    private void ButtonVisibility(CanvasItem button) { if (_bindings.TryGetValue((Button)button, out var binding)) binding.Spacer.Visible = button.Visible; Arrange(); }
    private void EntryDisposed(ElectronObject entry) { var edit = (LineEdit)entry; _textEntries.Remove(edit); edit.TextSubmitted -= TextSubmitted; edit.Disposed -= EntryDisposed; }
    private void TextSubmitted(string _) { if (!_disposing && !IsDisposed && !_ok.IsDisposed && !_ok.Disabled) Confirm(); }
    private void HandleInput() { if (!IsDisposed && Tree is not null) SetInputAsHandled(); }
    private void Confirm()
    {
        if (_disposing || IsDisposed) return; List<Exception>? errors = null;
        try { if (_hideOnOK) Hide(); } catch (Exception e) { CollectException(ref errors, e); }
        try { if (!IsDisposed) OnOKPressed(); } catch (Exception e) { CollectException(ref errors, e); }
        try { if (!IsDisposed) Confirmed?.Invoke(); } catch (Exception e) { CollectException(ref errors, e); }
        HandleInput(); ThrowCollected("Dialog confirmation callbacks failed.", errors);
    }
    private void Cancel()
    {
        if (_disposing || IsDisposed || _cancelPending) return; _cancelPending = true; UnwatchParent(); var version = _visibilityVersion; var tree = Tree;
        if (tree != null) tree.Defer(() => { if (!IsDisposed && _cancelPending && version == _visibilityVersion) { _cancelPending = false; Hide(); } });
        List<Exception>? errors = null;
        try { Canceled?.Invoke(); } catch (Exception e) { CollectException(ref errors, e); }
        try { if (!IsDisposed) OnCancelPressed(); } catch (Exception e) { CollectException(ref errors, e); }
        try { if (tree == null && !IsDisposed && version == _visibilityVersion) { _cancelPending = false; Hide(); } } catch (Exception e) { CollectException(ref errors, e); }
        HandleInput(); ThrowCollected("Dialog cancellation callbacks failed.", errors);
    }
    private void InvokeCustom(string action)
    {
        List<Exception>? errors = null; try { CustomAction?.Invoke(action); } catch (Exception e) { CollectException(ref errors, e); }
        try { if (!IsDisposed) OnCustomAction(action); } catch (Exception e) { CollectException(ref errors, e); }
        ThrowCollected("Dialog custom action callbacks failed.", errors);
    }
    /// <inheritdoc />
    protected override void OnInput(InputEvent input) { base.OnInput(input); if (Visible && _closeOnEscape && input.IsActionPressed("ui_close_dialog", false, true)) Cancel(); }
    internal override void PreparePopup() { _visibilityVersion++; _cancelPending = false; Arrange(); }
    internal override void AfterVisibilityChanged(bool visible)
    {
        _visibilityVersion++; _cancelPending = false; UnwatchParent(); Arrange();
        if (visible && IsInsideTree) { if (!_ok.IsDisposed && _ok.Visible && !_ok.Disabled) _ok.GrabFocus(); for (var node = Parent; node != null; node = node.Parent) if (node is Window { Visible: true } parent) { _parentWindow = parent; parent.FocusEntered += ParentFocused; break; } }
    }
    private void ParentFocused() { if (Visible && !Exclusive && PopupWindow) Cancel(); }
    private void UnwatchParent() { if (_parentWindow != null) _parentWindow.FocusEntered -= ParentFocused; _parentWindow = null; }
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(AcceptDialog) ? CreateDialog : base.CreateSceneInstanceFactory();
    private static Node CreateDialog() => new AcceptDialog();
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _disposing = true; UnwatchParent(); foreach (var edit in _textEntries) { edit.TextSubmitted -= TextSubmitted; edit.Disposed -= EntryDisposed; }
            _textEntries.Clear();
            foreach (var entry in _bindings) { entry.Key.Pressed -= entry.Value.Press; entry.Key.VisibilityChanged -= ButtonVisibility; entry.Key.Disposed -= ButtonDisposed; entry.Key.MinimumSizeChanged -= Arrange; }
            _bindings.Clear();
            Canceled = null; Confirmed = null; CustomAction = null;
        }
        base.Dispose(disposing);
    }
}
