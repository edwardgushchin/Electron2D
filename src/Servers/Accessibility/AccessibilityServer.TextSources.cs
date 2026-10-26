namespace Electron2D;

public sealed partial class AccessibilityServer
{
    internal static void UpdateSourceText(Element e)
    {
        if (e.Source is not Control control || control is not (LineEdit or TextEdit) || control.IsDisposed || !control.IsInsideTree) return;
        var r = Runtime; var count = control is TextEdit edit ? edit.GetLineCount() : 1;
        var changed = false;
        var oldValue = e.TextValue ?? "";
        r.EnableText(e);
        var version = control is TextEdit editor ? editor.AccessibilityTextVersion : 0;
        if (control is not TextEdit || !e.SourceTextPrepared || e.SourceTextVersion != version)
        {
            while (e.SourceTextLines.Count > count) { var last = e.SourceTextLines[^1]; e.SourceTextLines.RemoveAt(e.SourceTextLines.Count - 1); r.Free(last); changed = true; }
            while (e.SourceTextLines.Count < count)
            {
                var line = r.Add(e.WindowID, AccessibilityRole.TextRun); line.OwnedSubelement = line.IsTextLine = true; line.TextSource = control;
                e.SourceTextLines.Add(line); r.Attach(e, line, -1); r.EnableText(line); changed = true;
            }
            var wasProtected = e.TextProtected; e.TextProtected = control is LineEdit { Secret: true };
            if (wasProtected != e.TextProtected) r._native!.RoleChanged(e);
            for (var i = 0; i < count; i++)
            {
                var line = e.SourceTextLines[i]; line.TextLine = i; var newline = i + 1 < count;
                var value = control is LineEdit field ? field.AccessibilityTextValue : ((TextEdit)control).GetLine(i);
                if (line.SourceLineValue == value && line.TextLineHasNewline == newline) continue;
                line.SourceLineValue = value; line.TextLineHasNewline = newline; var old = line.TextValue ?? ""; line.TextValue = newline ? value + "\n" : value; line.TextScalarCount = TextLength(value) + (newline ? 1 : 0);
                r._native!.TextChanged(line, old, line.TextValue); changed = true;
            }
            if (changed || !e.SourceTextPrepared)
            {
                var value = control is LineEdit field ? field.AccessibilityTextValue : ((TextEdit)control).Text;
                e.TextValue = value; e.TextScalarCount = 0; foreach (var line in e.SourceTextLines) e.TextScalarCount += line.TextScalarCount; r._native!.TextChanged(e, oldValue, value); r._native.PropertyChanged(e, "accessible-value", value);
            }
            e.SourceTextVersion = version;
        }
        var editable = control is LineEdit le ? le.Editable : ((TextEdit)control).Editable;
        var selectable = control is LineEdit ls ? ls.SelectingEnabled : ((TextEdit)control).SelectingEnabled;
        if (!e.SourceTextPrepared || editable != e.SourceTextEditable || selectable != e.SourceTextSelectable)
        {
            e.SourceTextEditable = editable; e.SourceTextSelectable = selectable;
            e.SetTextSelectionAction = selectable ? e.SourceTextSelectionAction : null; e.ReplaceTextAction = editable ? e.SourceTextReplaceAction : null;
            r.EnableEditableText(e); r._native!.InterfacesChanged(e); r._native.StateChanged(e, "read-only", !editable || e.Flag(AccessibilityFlags.ReadOnly)); r._native.StateChanged(e, "editable", editable); r._native.StateChanged(e, "selectable-text", selectable);
        }
        var anchorLine = 0; var focusLine = 0; int anchor, focus;
        if (control is LineEdit single) { focus = single.CaretColumn; anchor = single.HasSelection() ? single.GetSelectionFromColumn() : focus; if (single.HasSelection()) focus = single.GetSelectionToColumn(); }
        else { var multi = (TextEdit)control; anchorLine = multi.GetSelectionOriginLine(); anchor = multi.GetSelectionOriginColumn(); focusLine = multi.GetCaretLine(); focus = multi.GetCaretColumn(); }
        UpdateSetTextSelection(e.ID, e.SourceTextLines[anchorLine].ID, anchor, e.SourceTextLines[focusLine].ID, focus);
        e.SourceTextPrepared = true;
    }
}
