using System.Globalization;
namespace Electron2D;

public partial class Tree
{
    private OwnedPopup _editorPopup = null!;
    private OwnedMenu _choicePopup = null!;
    private OwnedLine _lineEditor = null!;
    private OwnedText _textEditor = null!;
    private OwnedSlider _valueEditor = null!;
    private TreeItem? _editingItem;
    private int _editingColumn;
    private bool _settingEditor;
    private void CreateEditors()
    {
        _editorPopup = new(this) { Name = "_cell_editor" }; _choicePopup = new(this) { Name = "_cell_choices", HideOnCheckableItemSelection = false }; _lineEditor = new(this) { Name = "_line" }; _textEditor = new(this) { Name = "_multiline", Visible = false }; _valueEditor = new(this) { Name = "_value", Visible = false };
        AddChild(_editorPopup, InternalMode.Front); AddChild(_choicePopup, InternalMode.Front); _editorPopup.AddChild(_lineEditor); _editorPopup.AddChild(_textEditor); _editorPopup.AddChild(_valueEditor);
        _lineEditor.TextSubmitted += _ => CommitEditor(); _valueEditor.ValueChanged += value => { if (!_settingEditor && _editingItem is { IsDisposed: false } item && item.Owner == this) { item.SetRange(_editingColumn, value); _lineEditor.Text = item.GetRange(_editingColumn).ToString("G", CultureInfo.InvariantCulture); PublishEdited(item, _editingColumn); } };
        _choicePopup.IDPressed += id => { var item = _editingItem; var column = _editingColumn; _editingItem = null; try { if (item is { IsDisposed: false } && item.Owner == this) { item.SetRange(column, id); PublishEdited(item, column); } } finally { if (!_choicePopup.IsDisposed) _choicePopup.Hide(); } };
        _editorPopup.PopupHide += () => { if (_editorPopup.Canceled) CancelEditor(); else CommitEditor(); }; _choicePopup.PopupHide += () => _editingItem = null;
    }
    /// <summary>Activates the current cell's built-in or application editor.</summary><param name="forceEdit">Overrides the cell Editable flag.</param><returns>Whether this mode can be edited.</returns>
    public bool EditSelected(bool forceEdit = false)
    {
        EnsureTreeMutable(); var item = GetSelected(); if (item == null) return false; var cell = item.At(_selectedColumn); if (!cell.Editable && !forceEdit) return false; EnsureCursorIsVisible(); CancelEditor(); var rect = GetItemAreaRect(item, _selectedColumn); _editingItem = item; _editingColumn = _selectedColumn;
        if (cell.Mode == TreeItem.TreeCellMode.Check) { item.SetChecked(_selectedColumn, !cell.Checked); try { PublishEdited(item, _selectedColumn); } finally { _editingItem = null; } return true; }
        if (cell.Mode == TreeItem.TreeCellMode.Custom)
        {
            _customPopupRect = new(GetGlobalTransformWithCanvas() * rect.Position, rect.Size); _edited = item; _editedColumn = _selectedColumn; _editingItem = null;
            List<Exception>? errors = null;
            try { CustomPopupEdited?.Invoke(false); } catch (Exception error) { CollectException(ref errors, error); }
            if (!IsDisposed && !item.IsDisposed && item.Owner == this) try { PublishEdited(item, _editedColumn); } catch (Exception error) { CollectException(ref errors, error); }
            ThrowCollected("Tree custom editor callbacks failed.", errors); return true;
        }
        if (cell.Mode == TreeItem.TreeCellMode.Icon) { _editingItem = null; return false; }
        var screen = GetViewport()?.GetScreenTransform() ?? Transform.Identity; var position = screen * GetGlobalTransformWithCanvas() * rect.Position; var width = Math.Max(100, rect.Size.X);
        if (cell.Mode == TreeItem.TreeCellMode.Range && cell.Text.Length > 0)
        {
            _choicePopup.Clear(); var choices = cell.Text.Split(','); for (var i = 0; i < choices.Length; i++) { var split = choices[i].LastIndexOf(':'); var id = split >= 0 && int.TryParse(choices[i].AsSpan(split + 1), out var parsed) ? parsed : i; _choicePopup.AddRadioCheckItem(split >= 0 ? choices[i][..split] : choices[i], id); _choicePopup.SetItemChecked(i, id == cell.Value); }
            _choicePopup.Popup(new((Vector2i)(position + new Vector2(0, rect.Size.Y)), new((int)width, 1))); return true;
        }
        _settingEditor = true; try
        {
            _editorPopup.Canceled = false; _lineEditor.Visible = !cell.Multiline || cell.Mode == TreeItem.TreeCellMode.Range; _textEditor.Visible = !_lineEditor.Visible; _valueEditor.Visible = cell.Mode == TreeItem.TreeCellMode.Range;
            var height = _textEditor.Visible ? Math.Max(120, rect.Size.Y) : Math.Max(32, rect.Size.Y) + (_valueEditor.Visible ? 24 : 0); _editorPopup.Size = new((int)width, (int)height); _editorPopup.Position = (Vector2i)position;
            if (_textEditor.Visible) { _textEditor.Text = cell.Text; _textEditor.Size = new(width, height); _textEditor.SelectAll(); }
            else { _lineEditor.Text = cell.Mode == TreeItem.TreeCellMode.Range ? cell.Value.ToString("G", CultureInfo.InvariantCulture) : cell.Text; _lineEditor.Size = new(width, height - (_valueEditor.Visible ? 24 : 0)); _lineEditor.SelectAll(); if (_valueEditor.Visible) { _valueEditor.MinValue = cell.Min; _valueEditor.MaxValue = cell.Max; _valueEditor.Step = Math.Max(0, cell.Step); _valueEditor.ExpEdit = cell.Exponential; _valueEditor.SetValueNoSignal(cell.Value); _valueEditor.Position = new(0, height - 24); _valueEditor.Size = new(width, 24); } }
            _editorPopup.Popup(); if (_textEditor.Visible) _textEditor.GrabFocus(); else _lineEditor.GrabFocus(); return true;
        }
        finally { _settingEditor = false; }
    }
    private void CommitEditor()
    {
        var item = _editingItem; if (item == null) return; var column = _editingColumn; _editingItem = null; if (item.IsDisposed || item.Owner != this || column >= item.Cells.Count) return;
        var cell = item.Cells[column]; if (cell.Mode == TreeItem.TreeCellMode.Range) { if (!NumericExpression.TryEvaluate(_lineEditor.Text, out var value) || !double.IsFinite(value)) { if (_editorPopup.Visible) _editingItem = item; return; } item.SetRange(column, value); } else item.SetText(column, cell.Multiline ? _textEditor.Text : _lineEditor.Text);
        try { PublishEdited(item, column); } finally { if (!_editorPopup.IsDisposed) _editorPopup.Hide(); if (IsInsideTree && !IsDisposed) GrabFocus(); }
    }
    private void CancelEditor() { _editingItem = null; if (_editorPopup is { IsDisposed: false, Visible: true }) _editorPopup.Hide(); if (_choicePopup is { IsDisposed: false, Visible: true }) _choicePopup.Hide(); }
    private void PublishEdited(TreeItem item, int column) { _edited = item; _editedColumn = column; ItemEdited?.Invoke(); }
    private sealed class OwnedPopup(Tree owner) : PopupPanel
    {
        internal bool Canceled;
        internal override void HandlePopupInput(InputEvent input) { if (InputMap.HasAction("ui_cancel") && input.IsActionPressed("ui_cancel")) Canceled = true; base.HandlePopupInput(input); }
        protected override void ValidateDisposal() { owner.ValidateOwned(); base.ValidateDisposal(); }
    }
    private sealed class OwnedMenu(Tree owner) : PopupMenu { protected override void ValidateDisposal() { owner.ValidateOwned(); base.ValidateDisposal(); } }
    private sealed class OwnedLine(Tree owner) : LineEdit { protected override void ValidateDisposal() { owner.ValidateOwned(); base.ValidateDisposal(); } }
    private sealed class OwnedText(Tree owner) : TextEdit
    { protected override void OnGUIInput(InputEvent input) { if (input is InputEventKey { Pressed: true } key && key.Keycode == Key.Enter && (key.ControlPressed || key.MetaPressed)) { owner.CommitEditor(); AcceptEvent(); return; } base.OnGUIInput(input); } protected override void ValidateDisposal() { owner.ValidateOwned(); base.ValidateDisposal(); } }
    private sealed class OwnedSlider(Tree owner) : HSlider { protected override void ValidateDisposal() { owner.ValidateOwned(); base.ValidateDisposal(); } }
}
