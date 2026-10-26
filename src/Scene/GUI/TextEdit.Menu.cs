using System.Text;
namespace Electron2D;

public partial class TextEdit
{
    /// <summary>Returns the stable borrowed context menu.</summary><returns>Required owned popup.</returns>
    public PopupMenu GetMenu() { CheckTextEdit(); return _menu; }
    /// <summary>Reports whether the context menu is presented.</summary><returns>Popup visibility.</returns>
    public bool IsMenuVisible() { CheckTextEdit(); return _menu.Visible; }
    internal void AccessibilityShowContextMenu() => ShowContextMenu(Size * .5f);
    private void ShowContextMenu(Vector2 position)
    { if (!_contextMenuEnabled || !IsInsideTree || !IsVisibleInTree) return; PrepareMenu(); _menu.PopupFromViewport(GetViewport()!, new((Vector2i)(GetGlobalTransformWithCanvas() * position), Vector2i.One)); }
    private OwnedMenu? _directionMenu, _controlMenu;
    private bool _menuBuilt;
    private void PrepareMenu()
    {
        if (!_menuBuilt)
        {
            _directionMenu = NewSubmenu("_direction"); _controlMenu = NewSubmenu("_unicode");
            foreach (var action in Enum.GetValues<TextMenuAction>())
            {
                if (action is TextMenuAction.EmojiAndSymbols or TextMenuAction.SubmenuTextDirection or TextMenuAction.SubmenuInsertUCC) continue;
                var id = (int)action; var popup = id >= 14 ? _controlMenu : id is >= 8 and <= 11 ? _directionMenu : _menu;
                var label = action switch { TextMenuAction.SelectAll => "Select All", TextMenuAction.DirectionInherited => "Layout Direction", TextMenuAction.DirectionAuto => "Automatic", TextMenuAction.DirectionLTR => "Left to Right", TextMenuAction.DirectionRTL => "Right to Left", TextMenuAction.DisplayUCC => "Show Control Characters", _ => id >= 14 ? "Insert " + action.ToString()[6..] : action.ToString() };
                if (popup == _directionMenu) popup.AddRadioCheckItem(label, id); else if (action == TextMenuAction.DisplayUCC) popup.AddCheckItem(label, id); else popup.AddItem(label, id);
            }
            _menu.AddSeparator(); _menu.AddSubmenuNodeItem("Text Direction", _directionMenu, (int)TextMenuAction.SubmenuTextDirection); _menu.AddSubmenuNodeItem("Insert Control Character", _controlMenu, (int)TextMenuAction.SubmenuInsertUCC); _menuBuilt = true;
        }
        foreach (var popup in new[] { _menu, _directionMenu!, _controlMenu! })
            for (var index = 0; index < popup.ItemCount; index++)
            {
                var id = popup.GetItemID(index); if (id is < 0 or > 29) continue; var action = (TextMenuAction)id;
                popup.SetItemDisabled(index, !_editable && (action is TextMenuAction.Cut or TextMenuAction.Paste or TextMenuAction.Clear or TextMenuAction.Undo or TextMenuAction.Redo || id >= 14));
                if (id is >= 8 and <= 11) popup.SetItemChecked(index, action == (_textDirection switch { TextDirection.Inherited => TextMenuAction.DirectionInherited, TextDirection.LTR => TextMenuAction.DirectionLTR, TextDirection.RTL => TextMenuAction.DirectionRTL, _ => TextMenuAction.DirectionAuto }));
                if (action == TextMenuAction.DisplayUCC) popup.SetItemChecked(index, _drawControlChars);
            }
    }
    private OwnedMenu NewSubmenu(string name)
    {
        var menu = new OwnedMenu(this) { Name = name }; _menu.AddChild(menu, InternalMode.Front); menu.IDPressed += value => MenuOption((TextMenuAction)value); return menu;
    }
    /// <summary>Executes an editing, direction or Unicode control command.</summary><param name="option">Typed command.</param><exception cref="NotSupportedException">Native symbol presentation is unavailable.</exception>
    public void MenuOption(TextMenuAction option)
    {
        EnsureTextMutable(); switch (option)
        {
            case TextMenuAction.Cut: Cut(); return;
            case TextMenuAction.Copy: Copy(); return;
            case TextMenuAction.Paste: Paste(); return;
            case TextMenuAction.Clear: if (_editable) Clear(); return;
            case TextMenuAction.SelectAll: SelectAll(); return;
            case TextMenuAction.Undo: if (_editable) Undo(); return;
            case TextMenuAction.Redo: if (_editable) Redo(); return;
            case TextMenuAction.DirectionInherited: TextDirection = TextDirection.Inherited; return;
            case TextMenuAction.DirectionAuto: TextDirection = TextDirection.Auto; return;
            case TextMenuAction.DirectionLTR: TextDirection = TextDirection.LTR; return;
            case TextMenuAction.DirectionRTL: TextDirection = TextDirection.RTL; return;
            case TextMenuAction.DisplayUCC: DrawControlChars = !DrawControlChars; return;
            case TextMenuAction.EmojiAndSymbols: throw new NotSupportedException("Native symbol-picker presentation is not integrated.");
        }
        if (!_editable) return; var scalar = option switch { TextMenuAction.InsertLRM => 0x200e, TextMenuAction.InsertRLM => 0x200f, TextMenuAction.InsertLRE => 0x202a, TextMenuAction.InsertRLE => 0x202b, TextMenuAction.InsertLRO => 0x202d, TextMenuAction.InsertRLO => 0x202e, TextMenuAction.InsertPDF => 0x202c, TextMenuAction.InsertALM => 0x61c, TextMenuAction.InsertLRI => 0x2066, TextMenuAction.InsertRLI => 0x2067, TextMenuAction.InsertFSI => 0x2068, TextMenuAction.InsertPDI => 0x2069, TextMenuAction.InsertZWJ => 0x200d, TextMenuAction.InsertZWNJ => 0x200c, TextMenuAction.InsertWJ => 0x2060, TextMenuAction.InsertSHY => 0xad, _ => 0 }; if (scalar != 0) InsertTextAtCaret(new Rune(scalar).ToString());
    }
    private void ValidateOwned() { if (!IsDisposed && !_disposing) throw new InvalidOperationException("Required text editor children belong to their editor."); }
    private sealed class OwnedVScroll(TextEdit owner) : VScrollBar { protected override void ValidateDisposal() { owner.ValidateOwned(); base.ValidateDisposal(); } }
    private sealed class OwnedHScroll(TextEdit owner) : HScrollBar { protected override void ValidateDisposal() { owner.ValidateOwned(); base.ValidateDisposal(); } }
    private sealed class OwnedMenu(TextEdit owner) : PopupMenu { protected override void ValidateDisposal() { owner.ValidateOwned(); base.ValidateDisposal(); } }
}
