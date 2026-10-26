using System.Text;
using static Electron2D.GIONative;
using Element = Electron2D.AccessibilityServer.Element;
using Flag = Electron2D.AccessibilityServer.AccessibilityFlags;
namespace Electron2D;

internal sealed unsafe partial class ATSPIServer
{
    private static nint AccessibleAttributes(Element e)
    {
        var builder = Builder("a{ss}");
        try { g_variant_builder_add_value(builder, g_variant_new_dict_entry(S("toolkit"), S("Electron2D"))); if (e.TextValue is { } text) g_variant_builder_add_value(builder, g_variant_new_dict_entry(S("valuetext"), S(text))); AddScrollAttributes(builder, e); AddCollectionAttributes(builder, e); AddLiveAttributes(builder, e); return g_variant_builder_end(builder); }
        finally { g_variant_builder_unref(builder); }
    }
    internal void InterfacesChanged(Element e) { if (e.Parent is not null || e.IsApplication) Emit(CachePath, Cache, "AddAccessible", Tuple([CacheItem(e)])); }
    internal void EnableText(Element e) => EnableInterface(e, TextInterface);
    internal void EnableEditableText(Element e) => EnableInterface(e, EditableTextInterface);
    internal void TextChanged(Element e, string oldText, string newText)
    {
        if (oldText == newText) return; QueueAnnouncement(e, newText);
        if (oldText.Length != 0) Emit(e.Path, "org.a11y.atspi.Event.Object", "TextChanged", Event("delete", 0, AccessibilityServer.TextLength(oldText), S(oldText), e));
        if (newText.Length != 0) Emit(e.Path, "org.a11y.atspi.Event.Object", "TextChanged", Event("insert", 0, AccessibilityServer.TextLength(newText), S(newText), e));
    }
    internal void TextSelectionChanged(Element e)
    {
        var focus = AccessibilityServer.TextSelectionOffset(e, e.SelectionFocus, e.SelectionFocusOffset);
        Emit(e.Path, "org.a11y.atspi.Event.Object", "TextSelectionChanged", Event("", 0, 0, I(0), e));
        Emit(e.Path, "org.a11y.atspi.Event.Object", "TextCaretMoved", Event("", focus, 0, I(0), e));
    }
    private sealed class TextDocument
    {
        internal readonly Element Owner;
        internal readonly List<(Element Leaf, Element Line, int Start, int Count)> Spans = [];
        internal readonly string Value;
        internal readonly int[] UTF16;
        internal int Count => UTF16.Length - 1;
        internal readonly int Anchor, Focus;
        internal readonly List<(int Start, int End, int Caret)> Selections = [];
        internal TextDocument(Element owner)
        {
            Owner = owner; var builder = new StringBuilder(); var length = 0;
            void Append(Element e, Element? line)
            {
                if (e.Retired) return;
                if (e.IsTextLine) line = e;
                if (e.TextValue is { } value && !AccessibilityServer.HasTextChildren(e))
                {
                    var count = e.TextScalarCount; Spans.Add((e, line ?? e, length, count)); length += count; builder.Append(value.Replace('\0', '\ufffd')); return;
                }
                foreach (var child in e.Children) Append(child, line);
            }
            if (owner.SourceTextLines.Count != 0) { foreach (var line in owner.SourceTextLines) Append(line, null); } else Append(owner, null); Value = builder.ToString(); var offsets = new List<int> { 0 }; var index = 0;
            foreach (var rune in Value.EnumerateRunes()) { index += rune.Utf16SequenceLength; offsets.Add(index); }
            UTF16 = offsets.ToArray();
            Anchor = Endpoint(owner.SelectionAnchor, owner.SelectionAnchorOffset); Focus = Endpoint(owner.SelectionFocus, owner.SelectionFocusOffset);
            if (owner.Source is TextEdit edit && owner.SetTextSelectionAction == owner.SourceTextSelectionAction) edit.AccessibilitySelections(Selections);
            else if (Anchor != Focus) Selections.Add((Math.Min(Anchor, Focus), Math.Max(Anchor, Focus), 0));
        }
        private int Endpoint(Element? line, int at)
        {
            if (line is null || line.Retired) return 0;
            foreach (var span in Spans) if (span.Line == line || span.Leaf == line || IsUnder(span.Leaf, line)) return Math.Clamp(span.Start + at, span.Start, Math.Min(Count, span.Start + AccessibilityServer.ElementTextLength(line)));
            return 0;
        }
        private static bool IsUnder(Element leaf, Element parent) { for (var p = leaf.Parent; p is not null; p = p.Parent) if (p == parent) return true; return false; }
        internal (RID Line, int At) Position(int offset)
        {
            for (var i = 0; i < Spans.Count; i++)
            {
                var s = Spans[i]; if (offset < s.Start + s.Count || i == Spans.Count - 1) return (s.Line.ID, Math.Clamp(offset - s.Start, 0, s.Count));
            }
            return (Owner.ID, 0);
        }
        internal string Slice(int start, int end) => Value[UTF16[start]..UTF16[end]];
        internal void Range(ref int start, ref int end)
        {
            if (end == -1) end = Count;
            if (start < 0 || end < start || end > Count) throw new InvalidRequestException("The Unicode scalar interval is outside the text.");
        }
        internal (Element Leaf, Element Line, int Start, int Count) SpanAt(int at)
        {
            if (at == Count) return Spans[^1];
            var low = 0; var high = Spans.Count - 1;
            while (low < high) { var mid = low + (high - low) / 2; var span = Spans[mid]; if (span.Start + span.Count <= at) low = mid + 1; else high = mid; }
            return Spans[low];
        }
        internal Rect2 Rectangle(int at, uint coordinates)
        {
            if (at < 0 || at > Count) throw new InvalidRequestException("The character offset is outside the text.");
            if (Spans.Count != 0)
            {
                var span = SpanAt(at); var leaf = span.Leaf; var column = at - span.Start; Rect2 rect;
                if (leaf.TextSource is { IsDisposed: false, IsInsideTree: true } source)
                {
                    var local = source switch { LineEdit line => line.AccessibilityCharacterRect(column), TextEdit edit => edit.AccessibilityCharacterRect(leaf.TextLine, column), _ => default };
                    rect = (source.GetViewport()!.GetScreenTransform() * source.GetGlobalTransformWithCanvas()) * local;
                }
                else
                {
                    var local = column < leaf.TextRectangles.Length ? leaf.TextRectangles[column] : new Rect2(leaf.Bounds.Size.X, 0, 0, leaf.Bounds.Size.Y);
                    rect = AccessibilityServer.TransformBounds(new(local.Position + leaf.Bounds.Position, local.Size), leaf.Transform);
                    if (span.Line != leaf) rect = AccessibilityServer.TransformBounds(new(rect.Position + span.Line.Bounds.Position, rect.Size), span.Line.Transform);
                }
                var reference = Coordinates(Owner, coordinates); return new(rect.Position + reference.Position - Bounds(Owner).Position, rect.Size);
            }
            return new(Coordinates(Owner, coordinates).Position, Vector2.Zero);
        }
        internal Rect2 RangeRectangle(int start, int end, uint coordinates)
        {
            if (start == end) return Rectangle(start, coordinates);
            var rect = Rectangle(start, coordinates); for (var i = start + 1; i < end; i++) rect = rect.Merge(Rectangle(i, coordinates)); return rect;
        }
        private bool SameLineAt(int offset)
        {
            var left = SpanAt(offset - 1); var right = SpanAt(offset); if (left.Leaf == right.Leaf) return false;
            return AccessibilityServer.RelatedTarget(left.Line, AccessibilityServer.RelationKind.TextNext) == right.Line || AccessibilityServer.RelatedTarget(right.Line, AccessibilityServer.RelationKind.TextPrevious) == left.Line ||
                AccessibilityServer.RelatedTarget(left.Leaf, AccessibilityServer.RelationKind.TextNext) == right.Leaf || AccessibilityServer.RelatedTarget(right.Leaf, AccessibilityServer.RelationKind.TextPrevious) == left.Leaf;
        }
        internal int[] Breaks(uint granularity, bool ends = false)
        {
            if (granularity == 0) return Enumerable.Range(0, Count + 1).ToArray();
            if (granularity is 1 or 2) return NativeTextBreak.AccessibilityBoundaries(Value, UTF16, granularity == 1 ? 1 : 3, ends, TextAttribute(Owner, "language") ?? "");
            if (granularity > 4) throw new InvalidRequestException("Unknown text granularity.");
            var result = new List<int> { 0 };
            for (var i = 1; i <= Count; i++)
            {
                var newline = Value[UTF16[i - 1]] == '\n';
                var wrapped = granularity == 3 && i < Count && Rectangle(i, 1).Position.Y != Rectangle(i - 1, 1).Position.Y && !SameLineAt(i);
                if (newline || wrapped) result.Add(ends && newline ? i - 1 : i);
            }
            if (result[^1] != Count) result.Add(Count); return result.Distinct().Order().ToArray();
        }
        internal (int Start, int End) Segment(int at, uint granularity, int direction = 0, bool ends = false)
        {
            if (at < 0 || at > Count) throw new InvalidRequestException("The text offset is outside the document.");
            var breaks = Breaks(granularity, ends); var slot = System.Array.BinarySearch(breaks, at); if (slot < 0) slot = ~slot - 1;
            slot += direction; if (slot < 0) return (0, 0); if (slot >= breaks.Length - 1) return (Count, Count); return (breaks[slot], breaks[slot + 1]);
        }
    }
    private static bool BoolArg(nint args, int index)
    { var child = g_variant_get_child_value(args, (nuint)index); try { return g_variant_get_boolean(child) != 0; } finally { g_variant_unref(child); } }
    private static int CharacterAt(TextDocument text, int at) { Rune.DecodeFromUtf16(text.Value.AsSpan(text.UTF16[at]), out var rune, out _); return rune.Value; }
    private nint TextProperty(Element e, string name)
    {
        var text = new TextDocument(e); return name switch { "CharacterCount" => I(text.Count), "CaretOffset" => I(text.Focus), _ => throw new InvalidRequestException("Unknown text property.") };
    }
    private static bool TextRequestAllowed(Element e, bool edit)
    {
        if (!Visible(e)) return false;
        for (var p = e; p is not null; p = p.Parent) if (p.Flag(Flag.Disabled) || edit && p.Flag(Flag.ReadOnly)) return false;
        return !edit || e.Source switch { LineEdit line => line.Editable, TextEdit text => text.Editable, _ => true };
    }
    private bool RequestSelection(Element e, TextDocument text, int start, int end)
    {
        text.Range(ref start, ref end); if (!TextRequestAllowed(e, false) || e.SetTextSelectionAction is not { } action) return false;
        var anchor = text.Position(start); var focus = text.Position(end);
        try { action(anchor.Line, anchor.At, focus.Line, focus.At); if (!e.Retired) AccessibilityServer.UpdateSourceText(e); return true; }
        catch (Exception error) { throw new CallbackException(error); }
    }
    private bool RequestReplacement(Element e, TextDocument text, int start, int end, string value)
    {
        text.Range(ref start, ref end);
        if (!TextRequestAllowed(e, true) || e.ReplaceTextAction is not { } action) return false;
        if (action == e.SourceTextReplaceAction && (e.SetTextSelectionAction is null || e.SetTextSelectionAction == e.SourceTextSelectionAction) && e.Source is Control source)
        {
            try { if (source is LineEdit field) field.AccessibilityReplaceRange(start, end, value); else ((TextEdit)source).AccessibilityReplaceRange(start, end, value); if (!e.Retired) AccessibilityServer.UpdateSourceText(e); return true; }
            catch (Exception error) { throw new CallbackException(error); }
        }
        if (e.SetTextSelectionAction is null) return false;
        if (!RequestSelection(e, text, start, end) || e.Retired) return false;
        try { action(value); if (!e.Retired) AccessibilityServer.UpdateSourceText(e); return true; }
        catch (Exception error) { throw new CallbackException(error); }
    }
    private nint TextCall(Element e, string method, nint args)
    {
        var text = new TextDocument(e);
        int Start() => IntArg(args, 0); int End() => IntArg(args, 1);
        switch (method)
        {
            case "GetText": { var start = Start(); var end = End(); text.Range(ref start, ref end); return Tuple([S(text.Slice(start, end))]); }
            case "GetCharacterAtOffset": { var at = Start(); if ((uint)at >= text.Count) throw new InvalidRequestException("The character is outside the document."); return Tuple([I(CharacterAt(text, at))]); }
            case "GetStringAtOffset":
            case "GetTextBeforeOffset":
            case "GetTextAtOffset":
            case "GetTextAfterOffset":
                {
                    var boundary = UIntArg(args, 1); var modern = method == "GetStringAtOffset";
                    if (!modern && boundary > 6) throw new InvalidRequestException("Unknown text boundary.");
                    var granularity = modern ? boundary : boundary == 0 ? 0u : boundary <= 2 ? 1u : boundary <= 4 ? 2u : 3u;
                    var (start, end) = text.Segment(Start(), granularity, method == "GetTextBeforeOffset" ? -1 : method == "GetTextAfterOffset" ? 1 : 0, !modern && boundary is 2 or 4 or 6);
                    return Tuple([S(text.Slice(start, end)), I(start), I(end)]);
                }
            case "SetCaretOffset": return Tuple([B(RequestSelection(e, text, Start(), Start()))]);
            case "GetNSelections": return Tuple([I(text.Selections.Count)]);
            case "GetSelection": { if ((uint)Start() >= text.Selections.Count) throw new InvalidRequestException("The selection does not exist."); var selection = text.Selections[Start()]; return Tuple([I(selection.Start), I(selection.End)]); }
            case "AddSelection": return Tuple([B(ChangeSelection(e, text, -1, Start(), End()))]);
            case "SetSelection": return Tuple([B(ChangeSelection(e, text, Start(), IntArg(args, 1), IntArg(args, 2)))]);
            case "RemoveSelection": return Tuple([B(ChangeSelection(e, text, Start(), 0, 0, true))]);
            case "GetCharacterExtents": return RectangleResult(text.Rectangle(Start(), UIntArg(args, 1)));
            case "GetRangeExtents": { var start = Start(); var end = End(); text.Range(ref start, ref end); return RectangleResult(text.RangeRectangle(start, end, UIntArg(args, 2))); }
            case "GetOffsetAtPoint": { var point = new Vector2(Start(), End()); var kind = UIntArg(args, 2); _ = Coordinates(e, kind); if (kind == 0 && WindowRoot(e) is not { ScreenOriginKnown: true }) return Tuple([I(-1)]); for (var i = 0; i < text.Count; i++) if (text.Rectangle(i, kind).HasPoint(point)) return Tuple([I(i)]); return Tuple([I(-1)]); }
            case "GetDefaultAttributes": case "GetDefaultAttributeSet": return Tuple([TextAttributes(e)]);
            case "GetAttributes": case "GetAttributeRun": { var at = Start(); if ((uint)at > text.Count) throw new InvalidRequestException("The offset is outside the document."); if (text.Spans.Count == 0) return Tuple([TextAttributes(e), I(0), I(0)]); var span = text.SpanAt(at); var style = TextStyleAt(span.Leaf, at - span.Start); var from = style is { } st ? Math.Clamp(st.Start, 0, span.Count) : 0; var to = style is { } en ? Math.Clamp(en.End, from, span.Count) : span.Count; return Tuple([TextAttributes(span.Leaf, method != "GetAttributeRun" || BoolArg(args, 1), at - span.Start), I(span.Start + from), I(span.Start + to)]); }
            case "GetAttributeValue": { var at = Start(); if ((uint)at > text.Count) throw new InvalidRequestException("The offset is outside the document."); return Tuple([S(TextAttribute(text.Spans.Count == 0 ? e : text.SpanAt(at).Leaf, StringArg(args, 1), text.Spans.Count == 0 ? 0 : at - text.SpanAt(at).Start) ?? "")]); }
            case "GetBoundedRanges": return BoundedText(text, args);
            case "ScrollSubstringTo": { var start = Start(); var end = End(); text.Range(ref start, ref end); return Tuple([B(ScrollText(e, text, start, end, UIntArg(args, 2), null))]); }
            case "ScrollSubstringToPoint": { var start = Start(); var end = End(); text.Range(ref start, ref end); var kind = UIntArg(args, 2); _ = Coordinates(e, kind); var point = new Vector2(IntArg(args, 3), IntArg(args, 4)) - Coordinates(e, kind).Position + Bounds(e).Position; return Tuple([B(ScrollText(e, text, start, end, 6, point))]); }
            default: throw new InvalidRequestException("Unknown text method.");
        }
    }
    private bool ChangeSelection(Element e, TextDocument text, int selection, int start, int end, bool remove = false)
    {
        if (selection < -1 || selection >= text.Selections.Count || remove && selection < 0) return false;
        text.Range(ref start, ref end); if (!TextRequestAllowed(e, false) || e.SetTextSelectionAction is null) return false;
        if (e.Source is TextEdit edit && e.SetTextSelectionAction == e.SourceTextSelectionAction)
        {
            var caret = selection < 0 ? text.Selections.Count == 0 ? 0 : -1 : text.Selections[selection].Caret;
            try { var result = edit.AccessibilityNativeSelection(caret, start, end, remove); if (!e.Retired) AccessibilityServer.UpdateSourceText(e); return result; }
            catch (Exception error) { throw new CallbackException(error); }
        }
        if (selection < 0 && text.Selections.Count != 0 || selection > 0) return false;
        return RequestSelection(e, text, remove ? text.Focus : start, remove ? text.Focus : end);
    }
    private static nint RectangleResult(Rect2 r)
    { var x = Math.Floor(r.Position.X); var y = Math.Floor(r.Position.Y); return Tuple([I(Pixel(x)), I(Pixel(y)), I(Pixel(Math.Ceiling((double)r.Size.X + r.Position.X - x))), I(Pixel(Math.Ceiling((double)r.Size.Y + r.Position.Y - y)))]); }
    private nint EditableTextCall(Element e, string method, nint args)
    {
        var text = new TextDocument(e);
        switch (method)
        {
            case "SetTextContents": return Tuple([B(RequestReplacement(e, text, 0, text.Count, StringArg(args, 0)))]);
            case "InsertText": { var value = StringArg(args, 1); var length = IntArg(args, 2); if (length < 0) throw new InvalidRequestException("Text length must be nonnegative."); var bytes = Encoding.UTF8.GetBytes(value); if (length < bytes.Length) { try { value = new UTF8Encoding(false, true).GetString(bytes, 0, length); } catch (DecoderFallbackException) { throw new InvalidRequestException("Text byte length splits a UTF-8 scalar."); } } return Tuple([B(RequestReplacement(e, text, IntArg(args, 0), IntArg(args, 0), value))]); }
            case "CopyText": case "CutText": { var start = IntArg(args, 0); var end = IntArg(args, 1); text.Range(ref start, ref end); if (!e.TextProtected && TextRequestAllowed(e, method == "CutText")) { var value = text.Slice(start, end); if (method == "CopyText") { DisplayServer.ClipboardSet(value); return Tuple([]); } if (RequestReplacement(e, text, start, end, "")) { DisplayServer.ClipboardSet(value); return Tuple([B(true)]); } } if (method == "CopyText") throw new InvalidRequestException("Copying this text is unavailable."); return Tuple([B(false)]); }
            case "DeleteText": return Tuple([B(RequestReplacement(e, text, IntArg(args, 0), IntArg(args, 1), ""))]);
            case "PasteText": if (!TextRequestAllowed(e, true)) return Tuple([B(false)]); return Tuple([B(RequestReplacement(e, text, IntArg(args, 0), IntArg(args, 0), DisplayServer.ClipboardGet()))]);
            default: throw new InvalidRequestException("Unknown editable text method.");
        }
    }
    private static bool ScrollText(Element e, TextDocument text, int start, int end, uint hint, Vector2? point)
    {
        if (hint > 6) throw new InvalidRequestException("Unknown scroll hint."); if (!TextRequestAllowed(e, false)) return false;
        if (e.Source is TextEdit edit) { var at = text.Position(start); var line = e.SourceTextLines.FindIndex(item => item.ID == at.Line); if (line < 0) return false; return edit.AccessibilityScrollText(line, at.At, hint, point); }
        if (e.Source is LineEdit field) return field.AccessibilityScrollText(start, hint, point);
        return false;
    }
    private static nint BoundedText(TextDocument text, nint args)
    {
        var rect = new Rect2(IntArg(args, 0), IntArg(args, 1), IntArg(args, 2), IntArg(args, 3)); var kind = UIntArg(args, 4); var xClip = UIntArg(args, 5); var yClip = UIntArg(args, 6);
        if (rect.Size.X < 0 || rect.Size.Y < 0 || xClip > 3 || yClip > 3) throw new InvalidRequestException("Invalid bounded-text rectangle or clip selector.");
        _ = Coordinates(text.Owner, kind);
        if (kind == 0 && WindowRoot(text.Owner) is not { ScreenOriginKnown: true }) return Tuple([Array("(iisv)", [])]);
        var builder = Builder("a(iisv)");
        bool Fits(Rect2 c) => c.Intersects(rect, true) && ((xClip & 1) == 0 || c.Position.X >= rect.Position.X) && ((xClip & 2) == 0 || c.End.X <= rect.End.X) && ((yClip & 1) == 0 || c.Position.Y >= rect.Position.Y) && ((yClip & 2) == 0 || c.End.Y <= rect.End.Y);
        try
        {
            var start = -1; for (var i = 0; i <= text.Count; i++) { var fits = i < text.Count && Fits(text.Rectangle(i, kind)); if (fits && start < 0) start = i; else if (!fits && start >= 0) { g_variant_builder_add_value(builder, Tuple([I(start), I(i), S(text.Slice(start, i)), g_variant_new_variant(S(""))])); start = -1; } }
            return Tuple([g_variant_builder_end(builder)]);
        }
        finally { g_variant_builder_unref(builder); }
    }
    internal void TextAttributesChanged(Element e) => Emit(e.Path, "org.a11y.atspi.Event.Object", "TextAttributesChanged", Event("", 0, 0, I(0), e));
    private static AccessibilityServer.TextStyle? TextStyleAt(Element e, int column)
    {
        if (e.TextSource is LineEdit field) return field.AccessibilityTextStyle(column);
        if (e.TextSource is TextEdit editor) return editor.AccessibilityTextStyle(e.TextLine, column);
        foreach (var style in e.TextStyles) if (column >= style.Start && column < style.End) return style;
        if (e.SourceTextLines.Count != 0) return TextStyleAt(e.SourceTextLines[0], column);
        if (e.TextValue is null) foreach (var child in e.Children) if (TextStyleAt(child, column) is { } value) return value;
        return null;
    }
    private static string? TextAttribute(Element e, string name, int column = 0)
    {
        var font = TextStyleAt(e, column);
        if (font is { } style)
        {
            var value = name switch
            {
                "family-name" when style.Family.Length != 0 => style.Family,
                "size" => (style.Size * .75).ToString("G", System.Globalization.CultureInfo.InvariantCulture),
                "weight" => style.Weight.ToString(System.Globalization.CultureInfo.InvariantCulture),
                "style" => style.Italic ? "italic" : "normal",
                "language" when style.Language.Length != 0 && (e.TextFormatMask & 8) == 0 => style.Language,
                _ => null
            };
            if (value is not null) return value;
        }
        var bit = name switch { "justification" => 1, "direction" => 2, "underline" or "strikethrough" or "overline" or "underline-color" or "strikethrough-color" or "overline-color" => 4, "language" => 8, "placeholder-text" => 16, _ => 0 };
        var source = e.Source as Control ?? e.TextSource;
        var current = e;
        while (current.Parent is { } parent && (bit != 0 ? (current.TextFormatMask & bit) == 0 : name == "fg-color" ? current.TextForeground is null : current.TextBackground is null)) current = parent;
        e = current;
        if (source is not null && (bit == 0 || (e.TextFormatMask & bit) == 0))
        {
            var automatic = name switch
            {
                "justification" => source is LineEdit field ? field.Alignment switch { HorizontalAlignment.Center => "center", HorizontalAlignment.Right => "right", HorizontalAlignment.Fill => "fill", _ => "left" } : "left",
                "direction" => font?.Direction == TextDirection.RTL || source.IsLayoutRTL() ? "rtl" : "ltr",
                "language" => source is LineEdit field ? field.Language : source is TextEdit editor ? editor.Language : "",
                "placeholder-text" => source is LineEdit field ? field.PlaceholderText : source is TextEdit editor ? editor.PlaceholderText : "",
                "fg-color" when e.TextForeground is null => ColorText(source.GetThemeColor("font_color")),
                _ => null
            };
            if (automatic is not null) return automatic;
        }
        return name switch
        {
            "justification" => e.TextAlignment switch { HorizontalAlignment.Center => "center", HorizontalAlignment.Right => "right", HorizontalAlignment.Fill => "fill", _ => "left" },
            "direction" => e.TextVertical ? "ttb" : font?.Direction == TextDirection.RTL ? "rtl" : "ltr",
            "underline" => e.TextUnderline ? "single" : "none",
            "strikethrough" => e.TextStrikethrough ? "true" : "false",
            "overline" => e.TextOverline ? "single" : "none",
            "fg-color" => e.TextForeground is { } fg ? ColorText(fg) : null,
            "bg-color" => e.TextBackground is { } bg ? ColorText(bg) : null,
            "underline-color" or "strikethrough-color" or "overline-color" => ColorText(e.TextDecorationColor),
            "language" => e.TextLanguage,
            "placeholder-text" => e.TextPlaceholder,
            _ => null
        };
    }
    private static string ColorText(Color c) => string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{(int)(Math.Clamp(c.R, 0, 1) * 65535)},{(int)(Math.Clamp(c.G, 0, 1) * 65535)},{(int)(Math.Clamp(c.B, 0, 1) * 65535)}");
    private static bool ExplicitTextAttribute(Element e, string key) => key switch
    {
        "family-name" or "size" or "weight" or "style" => e.TextSource is not null || e.TextStyles.Count != 0,
        "fg-color" => e.TextForeground.HasValue,
        "bg-color" => e.TextBackground.HasValue,
        "justification" => (e.TextFormatMask & 1) != 0,
        "direction" => (e.TextFormatMask & 2) != 0,
        "language" => (e.TextFormatMask & 8) != 0,
        "placeholder-text" => (e.TextFormatMask & 16) != 0,
        _ => (e.TextFormatMask & 4) != 0
    };
    private static nint TextAttributes(Element e, bool defaults = true, int column = 0)
    {
        var builder = Builder("a{ss}");
        try { foreach (var key in new[] { "family-name", "size", "weight", "style", "justification", "direction", "underline", "strikethrough", "overline", "fg-color", "bg-color", "underline-color", "strikethrough-color", "overline-color", "language", "placeholder-text" }) if ((defaults || ExplicitTextAttribute(e, key)) && TextAttribute(e, key, column) is { } value) g_variant_builder_add_value(builder, g_variant_new_dict_entry(S(key), S(value))); return g_variant_builder_end(builder); }
        finally { g_variant_builder_unref(builder); }
    }
}
