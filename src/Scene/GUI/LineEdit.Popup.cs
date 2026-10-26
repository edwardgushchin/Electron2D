namespace Electron2D;

public partial class LineEdit
{
    private OwnedMenu? _menu, _directionMenu, _controlMenu;
    private bool _contextMenuEnabled = true, _disposingLineEdit;
    /// <summary>Gets or sets whether right click and native accessibility may present the context menu.</summary>
    /// <value>True initially. Disabling prevents presentation requests.</value>
    /// <exception cref="ObjectDisposedException">The field is disposed.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    public bool ContextMenuEnabled
    {
        get { CheckLineEdit(); return _contextMenuEnabled; }
        set { EnsureMutable(); if (_contextMenuEnabled == value) return; _contextMenuEnabled = value; QueueAccessibilityUpdate(); }
    }
    /// <summary>Returns the stable borrowed context menu, creating its ordinary commands lazily.</summary>
    /// <returns>The field-owned menu. Custom entries and removals persist between presentations.</returns>
    /// <exception cref="ObjectDisposedException">The field is disposed.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner or capture-owned.</exception>
    public PopupMenu GetMenu()
    {
        CheckLineEdit(); if (_menu is not null) return _menu; EnsureMutable();
        _menu = new(this) { Name = "_line_menu" }; AddChild(_menu, InternalMode.Front);
        _directionMenu = NewMenu("_direction"); _controlMenu = NewMenu("_unicode");
        foreach (var action in Enum.GetValues<TextMenuAction>())
        {
            if (action is TextMenuAction.EmojiAndSymbols or TextMenuAction.SubmenuTextDirection or TextMenuAction.SubmenuInsertUCC) continue;
            var id = (int)action; var popup = id >= 14 ? _controlMenu : id is >= 8 and <= 11 ? _directionMenu : _menu;
            var label = action switch { TextMenuAction.SelectAll => "Select All", TextMenuAction.DirectionInherited => "Layout Direction", TextMenuAction.DirectionAuto => "Automatic", TextMenuAction.DirectionLTR => "Left to Right", TextMenuAction.DirectionRTL => "Right to Left", TextMenuAction.DisplayUCC => "Show Control Characters", _ => id >= 14 ? "Insert " + action.ToString()[6..] : action.ToString() };
            if (popup == _directionMenu) popup.AddRadioCheckItem(label, id); else if (action == TextMenuAction.DisplayUCC) popup.AddCheckItem(label, id); else popup.AddItem(label, id);
        }
        _menu.AddSeparator(); _menu.AddSubmenuNodeItem("Text Direction", _directionMenu, (int)TextMenuAction.SubmenuTextDirection); _menu.AddSubmenuNodeItem("Insert Control Character", _controlMenu, (int)TextMenuAction.SubmenuInsertUCC);
        RefreshMenu(); return _menu;
    }
    /// <summary>Reports context-menu visibility without creating a menu.</summary>
    /// <returns>True while the owned menu is visible.</returns>
    /// <exception cref="ObjectDisposedException">The field is disposed.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner.</exception>
    public bool IsMenuVisible() { CheckLineEdit(); return _menu?.Visible == true; }
    private OwnedMenu NewMenu(string name)
    { var menu = new OwnedMenu(this) { Name = name }; _menu!.AddChild(menu, InternalMode.Front); return menu; }
    private void RefreshMenu()
    {
        if (_menu is null) return;
        foreach (var popup in new[] { _menu, _directionMenu!, _controlMenu! })
            for (var index = 0; index < popup.ItemCount; index++)
            {
                var id = popup.GetItemID(index); if (id is < 0 or > 29) continue; var action = (TextMenuAction)id;
                var disabled = action switch { TextMenuAction.Cut => !_editable || _secret || !_selecting, TextMenuAction.Copy => _secret || !_selecting, TextMenuAction.Paste or TextMenuAction.Clear => !_editable, TextMenuAction.SelectAll => !_selectingEnabled || _text.Length == 0, TextMenuAction.Undo => !_editable || _historyPosition <= 0, TextMenuAction.Redo => !_editable || _historyPosition + 1 >= _history.Count, _ => id >= 14 && !_editable };
                popup.SetItemDisabled(index, disabled);
                if (id is >= 8 and <= 11) popup.SetItemChecked(index, action == (_textDirection switch { TextDirection.Inherited => TextMenuAction.DirectionInherited, TextDirection.LTR => TextMenuAction.DirectionLTR, TextDirection.RTL => TextMenuAction.DirectionRTL, _ => TextMenuAction.DirectionAuto }));
                if (action == TextMenuAction.DisplayUCC) popup.SetItemChecked(index, _drawControlChars);
            }
    }
    private void ShowContextMenu(Vector2 position)
    { if (!_contextMenuEnabled || !IsInsideTree || !IsVisibleInTree) return; _ = GetMenu(); RefreshMenu(); _menu!.PopupFromViewport(GetViewport()!, new((Vector2i)(GetGlobalTransformWithCanvas() * position), Vector2i.One)); }
    internal void AccessibilityShowContextMenu() => ShowContextMenu(Size * .5f);
    private sealed class OwnedMenu : PopupMenu
    {
        private readonly LineEdit _owner;
        internal OwnedMenu(LineEdit owner) { _owner = owner; IDPressed += id => owner.MenuOption((TextMenuAction)id); }
        protected override void ValidateDisposal() { if (!_owner.IsDisposed && !_owner._disposingLineEdit) throw new InvalidOperationException("Menu belongs to LineEdit."); base.ValidateDisposal(); }
    }
}
