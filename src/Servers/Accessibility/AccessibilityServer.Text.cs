using System.Text;
namespace Electron2D;

public sealed partial class AccessibilityServer
{
    internal readonly record struct TextStyle(int Start, int End, string Family, int Size, int Weight, bool Italic, string Language, TextDirection Direction);
    internal static int TextLength(string text) { var count = 0; foreach (var _ in text.EnumerateRunes()) count++; return count; }
    internal static int TextIndex(string text, int scalar)
    {
        if (scalar < 0) throw new ArgumentOutOfRangeException(nameof(scalar));
        var index = 0; while (scalar-- > 0) { if (index == text.Length) throw new ArgumentOutOfRangeException(nameof(scalar)); Rune.DecodeFromUtf16(text.AsSpan(index), out _, out var size); index += size; }
        return index;
    }
    internal static string TextSlice(string text, int start, int end) => text[TextIndex(text, start)..TextIndex(text, end)];
    private void EnableText(Element e)
    {
        if (e.TextEnabled) return; _native!.EnableText(e); e.TextEnabled = true; _native.InterfacesChanged(e);
    }
    private void EnableTextParents(Element e)
    {
        for (var p = e; p is not null && !p.IsWindow; p = p.Parent)
        {
            if (p != e && p.Role is not (AccessibilityRole.TextField or AccessibilityRole.MultilineTextField or AccessibilityRole.StaticText or AccessibilityRole.Container)) break;
            EnableText(p);
        }
    }
    /// <summary>Creates an owned text-line container from a prepared shaped buffer, copying its text and character geometry.</summary>
    /// <param name="parentRID">Live semantic parent.</param><param name="shapedText">Borrowed shaped buffer, or an empty RID for an empty line.</param>
    /// <param name="minHeight">Finite nonnegative minimum cross-axis size.</param><param name="insertPosition">Child position, or -1 to append.</param><param name="isLastLine">Suppresses the trailing newline when true.</param>
    /// <returns>A parent-owned line RID accepted by text selection; its content survives retirement of the shaped buffer.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Height or insertion index is invalid.</exception>
    /// <exception cref="KeyNotFoundException">A semantic or shaped identity is absent or foreign.</exception>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="InvalidOperationException">Access is off the owning thread.</exception>
    /// <remarks>Creation snapshots the buffer. Recreate the line after its authored shape changes. Native query and cold snapshot work may allocate.</remarks>
    public static RID CreateSubTextEditElements(RID parentRID, RID shapedText, float minHeight, int insertPosition = -1, bool isLastLine = false)
    {
        if (!float.IsFinite(minHeight) || minHeight < 0) throw new ArgumentOutOfRangeException(nameof(minHeight));
        var r = Require(); var parent = r.Get(parentRID); var oldText = CaptureText(parent);
        if (insertPosition < -1 || insertPosition > parent.Children.Count) throw new ArgumentOutOfRangeException(nameof(insertPosition));
        var text = ""; var rectangles = Array.Empty<Rect2>(); var size = new Vector2(0, minHeight); var vertical = false; var styles = new List<TextStyle>();
        if (shapedText.IsValid())
        {
            var server = TextServerManager.GetPrimaryInterface(); var range = server.ShapedTextGetRange(shapedText);
            text = TextSlice(server.ShapedGetText(shapedText), range.X, range.Y);
            vertical = server.ShapedTextGetOrientation(shapedText) == TextOrientation.Vertical;
            size = server.ShapedTextGetSize(shapedText).Max(Vector2.Zero); if (vertical) size.X = Math.Max(size.X, minHeight); else size.Y = Math.Max(size.Y, minHeight);
            rectangles = new Rect2[TextLength(text) + (isLastLine ? 0 : 1)];
            var glyphs = server.ShapedTextGetGlyphs(shapedText); var pen = 0f;
            for (var at = 0; at < glyphs.Length;)
            {
                var glyph = glyphs[at]; var next = Math.Min(glyphs.Length, at + Math.Max(1, glyph.Count)); var advance = 0f;
                for (var j = at; j < next; j++) advance += glyphs[j].Advance * glyphs[j].Repeat;
                if (glyph.End > glyph.Start && (glyph.Flags & TextGraphemeFlags.Virtual) == 0)
                {
                    var rtl = (glyph.Flags & TextGraphemeFlags.RTL) != 0; var step = advance / (glyph.End - glyph.Start);
                    for (var c = Math.Max(range.X, glyph.Start); c < Math.Min(range.Y, glyph.End); c++)
                    {
                        var axis = pen + step * (rtl ? glyph.End - c - 1 : c - glyph.Start);
                        var origin = Math.Min(axis, axis + step); var extent = Math.Abs(step);
                        rectangles[c - range.X] = vertical ? new(0, origin, size.X, extent) : new(origin, 0, extent, size.Y);
                    }
                }
                pen += advance; at = next;
            }
            foreach (var style in server.CaptureStyleMetadata(shapedText)) styles.Add(new(style.Start, style.End, style.Family, style.Size, style.Weight, style.Italic, style.Language, style.Direction));
            styles.Sort(static (left, right) => left.Start.CompareTo(right.Start));
        }
        if (!isLastLine) { text += "\n"; if (rectangles.Length == 0) rectangles = new Rect2[1]; rectangles[^1] = new(vertical ? new Vector2(0, size.Y) : new Vector2(size.X, 0), vertical ? new Vector2(size.X, 0) : new Vector2(0, size.Y)); }
        var line = r.Add(parent.WindowID, AccessibilityRole.Container); line.OwnedSubelement = true; line.IsTextLine = true; line.Bounds = new(Vector2.Zero, size);
        try
        {
            r.Attach(parent, line, insertPosition); var run = r.Add(parent.WindowID, AccessibilityRole.TextRun); run.OwnedSubelement = true; r.Attach(line, run, -1);
            run.TextValue = text; run.TextScalarCount = TextLength(text); run.TextRectangles = rectangles; run.TextStyles.AddRange(styles); run.Bounds = new(Vector2.Zero, size); run.TextVertical = vertical; run.TextFormatMask |= 2;
            r.EnableText(run); r.EnableTextParents(line); r._native!.TextChanged(parent, oldText, CaptureText(parent)); return line.ID;
        }
        catch { r.Free(line); throw; }
    }
    /// <summary>Publishes a string value through the native Text interface without invoking an edit callback.</summary>
    /// <param name="id">Live producer element.</param><param name="value">Nonnull actual text value.</param>
    /// <exception cref="ArgumentNullException">Value is null.</exception><exception cref="KeyNotFoundException">Element is absent or retired.</exception>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception><exception cref="InvalidOperationException">Access is off the display owner.</exception>
    /// <remarks>Shaped sub-elements supply character geometry when available. This state setter does not invent a layout for unshaped values.</remarks>
    public static void UpdateSetValue(RID id, string value)
    {
        ArgumentNullException.ThrowIfNull(value); var r = Require(); var e = r.Get(id); r.EnableTextParents(e);
        var old = CaptureText(e); if (e.TextValue == value) return; e.TextValue = value; e.TextScalarCount = TextLength(value); r._native!.TextChanged(e, old, CaptureText(e)); r._native.PropertyChanged(e, "accessible-value", value);
    }
    private static bool TextDescendsFrom(Element line, Element owner)
    { for (var p = line; p is not null; p = p.Parent) if (p == owner) return true; return false; }
    internal static int ElementTextLength(Element e)
    {
        if (e.TextValue is not null && !HasTextChildren(e)) return e.TextScalarCount;
        var count = 0; foreach (var child in e.Children) count = checked(count + ElementTextLength(child)); return count;
    }
    /// <summary>Publishes directed anchor/focus endpoints in descendant text lines, including a collapsed caret.</summary>
    /// <param name="id">Text owner.</param><param name="textStartID">Anchor line RID.</param><param name="startChar">Anchor Unicode scalar offset.</param><param name="textEndID">Focus line RID.</param><param name="endChar">Focus Unicode scalar offset.</param>
    /// <exception cref="ArgumentException">An endpoint is outside the owner's descendant text tree.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An offset is outside its line.</exception><exception cref="KeyNotFoundException">An element is absent or retired.</exception>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception><exception cref="InvalidOperationException">Access is off the display owner.</exception>
    public static void UpdateSetTextSelection(RID id, RID textStartID, int startChar, RID textEndID, int endChar)
    {
        var r = Require(); var e = r.Get(id); var anchor = r.Get(textStartID); var focus = r.Get(textEndID);
        if (anchor.WindowID != e.WindowID || focus.WindowID != e.WindowID || !TextDescendsFrom(anchor, e) || !TextDescendsFrom(focus, e)) throw new ArgumentException("Text endpoints must descend from their semantic owner in the same window.");
        if ((uint)startChar > ElementTextLength(anchor) || (uint)endChar > ElementTextLength(focus)) throw new ArgumentOutOfRangeException(nameof(startChar));
        if (e.SelectionAnchor == anchor && e.SelectionFocus == focus && e.SelectionAnchorOffset == startChar && e.SelectionFocusOffset == endChar) return;
        e.SelectionAnchor = anchor; e.SelectionFocus = focus; e.SelectionAnchorOffset = startChar; e.SelectionFocusOffset = endChar; r._native!.TextSelectionChanged(e);
    }
    /// <summary>Registers a compiled text-selection request callback.</summary>
    /// <param name="id">Text owner.</param><param name="action">SetTextSelection only.</param><param name="callback">Receives anchor RID/offset and focus RID/offset on the display owner.</param>
    /// <exception cref="ArgumentNullException">Callback is null.</exception><exception cref="ArgumentOutOfRangeException">Action has a different signature.</exception>
    /// <exception cref="KeyNotFoundException">Element is absent or retired.</exception><exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    public static void UpdateAddAction(RID id, AccessibilityAction action, Action<RID, int, RID, int> callback)
    {
        ArgumentNullException.ThrowIfNull(callback); if (action != AccessibilityAction.SetTextSelection) throw new ArgumentOutOfRangeException(nameof(action));
        var r = Require(); var e = r.Get(id); e.SetTextSelectionAction = callback; r.EnableText(e); r.EnableEditableText(e);
    }
    /// <summary>Registers a compiled request to replace selected text.</summary>
    /// <param name="id">Text owner.</param><param name="action">ReplaceSelectedText only.</param><param name="callback">Receives replacement text; the producer owns editing, history and publication.</param>
    /// <exception cref="ArgumentNullException">Callback is null.</exception><exception cref="ArgumentOutOfRangeException">Action has a different signature.</exception>
    /// <exception cref="KeyNotFoundException">Element is absent or retired.</exception><exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    public static void UpdateAddAction(RID id, AccessibilityAction action, Action<string> callback)
    {
        ArgumentNullException.ThrowIfNull(callback); if (action != AccessibilityAction.ReplaceSelectedText) throw new ArgumentOutOfRangeException(nameof(action));
        var r = Require(); var e = r.Get(id); e.ReplaceTextAction = callback; r.EnableText(e); r.EnableEditableText(e);
    }
    private void EnableEditableText(Element e)
    {
        if (e.EditableTextRegistered || e.ReplaceTextAction is null || e.SetTextSelectionAction is null && e.Source is not (LineEdit or TextEdit)) return;
        _native!.EnableEditableText(e); e.EditableTextRegistered = true; _native.InterfacesChanged(e);
    }
    private void ValidateTextSelections()
    {
        List<Exception>? errors = null;
        foreach (var e in _elements.Values)
        {
            if (e.SelectionAnchor is not { } anchor || e.SelectionFocus is not { } focus) continue;
            if (!anchor.Retired && !focus.Retired && TextDescendsFrom(anchor, e) && TextDescendsFrom(focus, e) && e.SelectionAnchorOffset <= ElementTextLength(anchor) && e.SelectionFocusOffset <= ElementTextLength(focus)) continue;
            e.SelectionAnchor = e.SelectionFocus = null; e.SelectionAnchorOffset = e.SelectionFocusOffset = 0;
            try { _native!.TextSelectionChanged(e); } catch (Exception error) { (errors ??= []).Add(error); }
        }
        if (errors is not null) throw new AggregateException("Text selection retirement notifications failed.", errors);
    }

    internal static int TextSelectionOffset(Element e, Element? target, int offset)
    {
        var total = 0;
        bool Find(Element current)
        {
            if (current == target) { total += Math.Clamp(offset, 0, ElementTextLength(current)); return true; }
            if (current.TextValue is not null && !HasTextChildren(current)) { total += current.TextScalarCount; return false; }
            foreach (var child in current.Children) if (Find(child)) return true;
            return false;
        }
        if (e.SourceTextLines.Count != 0) { foreach (var line in e.SourceTextLines) if (Find(line)) return total; }
        else if (Find(e)) return total;
        return 0;
    }

    private static string CaptureText(Element e)
    {
        if (e.TextValue is { } value && !HasTextChildren(e)) return value;
        var builder = new StringBuilder(); void Append(Element item) { if (item.TextValue is { } text && !HasTextChildren(item)) builder.Append(text); else foreach (var child in item.Children) Append(child); }
        Append(e); return builder.ToString();
    }
    private static Element? ManualTextOwner(Element? e)
    {
        for (; e is not null && !e.IsWindow; e = e.Parent) if (e.TextEnabled && !e.IsTextLine) return e.Source is null ? e : null;
        return null;
    }

    /// <summary>Sets a non-owning next text fragment on the same visual line.</summary>
    /// <param name="id">Text fragment or line container.</param><param name="otherID">Same-window neighbor, or empty to clear.</param>
    /// <remarks>Native line-boundary queries join adjacent fragments linked on the same line. A literal newline remains a boundary. Target retirement clears the link; cycles never change hierarchy or recurse.</remarks>
    /// <exception cref="ArgumentException">Windows differ.</exception><exception cref="KeyNotFoundException">An identity is absent or retired.</exception>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception><exception cref="InvalidOperationException">Access is off the display owner.</exception>
    /// <exception cref="AggregateException">Native notifications fail after the graph commits.</exception>
    public static void UpdateSetNextOnLine(RID id, RID otherID)
    {
        var r = Require(); var e = r.Get(id); r.SetRelation(e, RelationKind.TextNext, r.RelationTarget(e, otherID, allowEmpty: true));
    }

    /// <summary>Sets a non-owning previous text fragment on the same visual line.</summary>
    /// <param name="id">Text fragment or line container.</param><param name="otherID">Same-window neighbor, or empty to clear.</param>
    /// <remarks>Native line-boundary queries join adjacent fragments linked on the same line. A literal newline remains a boundary. Target retirement clears the link; cycles never change hierarchy or recurse.</remarks>
    /// <exception cref="ArgumentException">Windows differ.</exception><exception cref="KeyNotFoundException">An identity is absent or retired.</exception>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception><exception cref="InvalidOperationException">Access is off the display owner.</exception>
    /// <exception cref="AggregateException">Native notifications fail after the graph commits.</exception>
    public static void UpdateSetPreviousOnLine(RID id, RID otherID)
    {
        var r = Require(); var e = r.Get(id); r.SetRelation(e, RelationKind.TextPrevious, r.RelationTarget(e, otherID, allowEmpty: true));
    }

    internal static bool HasTextChildren(Element e)
    {
        foreach (var child in e.Children) if (child.TextValue is not null || child.TextEnabled || HasTextChildren(child)) return true;
        return false;
    }

}
