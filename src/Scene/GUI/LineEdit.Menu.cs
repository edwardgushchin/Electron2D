using System.Text;
namespace Electron2D;

public partial class LineEdit
{
    /// <summary>Executes a typed editing command without requiring popup presentation.</summary>
    /// <param name="option">The editing, text-direction or Unicode-control command.</param>
    /// <remarks>Unknown numeric values are ignored. Native symbol-picker presentation requires a separate display service.</remarks>
    /// <exception cref="NotSupportedException">EmojiAndSymbols is selected without a native symbol-picker service.</exception>
    public void MenuOption(TextMenuAction option)
    {
        EnsureMutable();
        switch (option)
        {
            case TextMenuAction.Cut: if (_editable && !_secret && _selecting) { CopySelection(); DeleteRange(_selectionFrom, _selectionTo); ChangedByUser(); } return;
            case TextMenuAction.Copy: CopySelection(); return;
            case TextMenuAction.Paste: if (_editable) UserInsert(DisplayServer.Service?.ClipboardGetCore() ?? ""); return;
            case TextMenuAction.Clear: if (_editable) Clear(); return;
            case TextMenuAction.SelectAll: SelectAll(); return;
            case TextMenuAction.Undo: RestoreHistory(-1); return;
            case TextMenuAction.Redo: RestoreHistory(1); return;
            case TextMenuAction.DirectionInherited: TextDirection = TextDirection.Inherited; return;
            case TextMenuAction.DirectionAuto: TextDirection = TextDirection.Auto; return;
            case TextMenuAction.DirectionLTR: TextDirection = TextDirection.LTR; return;
            case TextMenuAction.DirectionRTL: TextDirection = TextDirection.RTL; return;
            case TextMenuAction.DisplayUCC: DrawControlChars = !DrawControlChars; return;
            case TextMenuAction.EmojiAndSymbols: throw new NotSupportedException("Native symbol-picker presentation is not integrated.");
        }
        if (!_editable) return;
        var scalar = option switch
        {
            TextMenuAction.InsertLRM => 0x200e,
            TextMenuAction.InsertRLM => 0x200f,
            TextMenuAction.InsertLRE => 0x202a,
            TextMenuAction.InsertRLE => 0x202b,
            TextMenuAction.InsertLRO => 0x202d,
            TextMenuAction.InsertRLO => 0x202e,
            TextMenuAction.InsertPDF => 0x202c,
            TextMenuAction.InsertALM => 0x061c,
            TextMenuAction.InsertLRI => 0x2066,
            TextMenuAction.InsertRLI => 0x2067,
            TextMenuAction.InsertFSI => 0x2068,
            TextMenuAction.InsertPDI => 0x2069,
            TextMenuAction.InsertZWJ => 0x200d,
            TextMenuAction.InsertZWNJ => 0x200c,
            TextMenuAction.InsertWJ => 0x2060,
            TextMenuAction.InsertSHY => 0x00ad,
            _ => 0
        };
        if (scalar == 0) return; var before = _text;
        List<Exception>? errors = null; try { InsertTextAtCaret(new Rune(scalar).ToString()); } catch (Exception error) { CollectException(ref errors, error); }
        try { if (!IsDisposed && before != _text) ChangedByUser(); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("LineEdit control-character callbacks failed.", errors);
    }
}

/// <summary>Identifies executable text editing commands; popup submenu identifiers are not editing commands.</summary>
public enum TextMenuAction
{
    /// <summary>Cuts selected text.</summary>
    Cut = 0,
    /// <summary>Copies selected text.</summary>
    Copy = 1,
    /// <summary>Pastes clipboard text.</summary>
    Paste = 2,
    /// <summary>Clears the field.</summary>
    Clear = 3,
    /// <summary>Selects all text.</summary>
    SelectAll = 4,
    /// <summary>Restores the preceding text state.</summary>
    Undo = 5,
    /// <summary>Restores the following text state.</summary>
    Redo = 6,
    /// <summary>Uses inherited text direction.</summary>
    DirectionInherited = 8,
    /// <summary>Detects text direction.</summary>
    DirectionAuto = 9,
    /// <summary>Uses left-to-right text direction.</summary>
    DirectionLTR = 10,
    /// <summary>Uses right-to-left text direction.</summary>
    DirectionRTL = 11,
    /// <summary>Toggles Unicode control-character display.</summary>
    DisplayUCC = 12,
    /// <summary>Inserts a left-to-right mark.</summary>
    InsertLRM = 14,
    /// <summary>Inserts a right-to-left mark.</summary>
    InsertRLM = 15,
    /// <summary>Inserts a left-to-right embedding control.</summary>
    InsertLRE = 16,
    /// <summary>Inserts a right-to-left embedding control.</summary>
    InsertRLE = 17,
    /// <summary>Inserts a left-to-right override control.</summary>
    InsertLRO = 18,
    /// <summary>Inserts a right-to-left override control.</summary>
    InsertRLO = 19,
    /// <summary>Inserts a pop-directional-formatting control.</summary>
    InsertPDF = 20,
    /// <summary>Inserts an Arabic letter mark.</summary>
    InsertALM = 21,
    /// <summary>Inserts a left-to-right isolate.</summary>
    InsertLRI = 22,
    /// <summary>Inserts a right-to-left isolate.</summary>
    InsertRLI = 23,
    /// <summary>Inserts a first-strong isolate.</summary>
    InsertFSI = 24,
    /// <summary>Inserts a pop-directional-isolate control.</summary>
    InsertPDI = 25,
    /// <summary>Inserts a zero-width joiner.</summary>
    InsertZWJ = 26,
    /// <summary>Inserts a zero-width nonjoiner.</summary>
    InsertZWNJ = 27,
    /// <summary>Inserts a word joiner.</summary>
    InsertWJ = 28,
    /// <summary>Inserts a soft hyphen.</summary>
    InsertSHY = 29,
    /// <summary>Requests the currently unavailable native symbol picker.</summary>
    EmojiAndSymbols = 30
}
