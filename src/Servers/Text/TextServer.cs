namespace Electron2D;

/// <summary>Provides low-level operations over retained shaped-text buffer identities.</summary>
/// <remarks>Obtain the retained interface from TextServerManager. Each buffer belongs to its creating thread.
/// Caller fonts, inline keys and metadata remain borrowed; only buffers returned by this interface may be freed through FreeRID.</remarks>
public abstract partial class TextServer : ElectronObject
{
    private readonly object _buffersGate = new();
    private readonly Dictionary<RID, TextLine> _owned = [];
    private protected TextServer() { }
    internal sealed class Builtin : TextServer { public override string GetName() => "FreeType/HarfBuzz"; }

    /// <summary>Returns the stable name of this text implementation.</summary><returns>The implementation name.</returns>
    public abstract string GetName();
    /// <summary>Creates an empty owned text buffer.</summary><param name="direction">Base writing direction.</param><param name="orientation">Advance axis.</param><returns>A caller-owned identity; release with FreeRID.</returns>
    public RID CreateShapedText(TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal)
    {
        if (!Enum.IsDefined(direction) || direction == TextDirection.Inherited) throw new ArgumentOutOfRangeException(nameof(direction));
        if (!Enum.IsDefined(orientation)) throw new ArgumentOutOfRangeException(nameof(orientation));
        var line = new TextLine { Direction = direction, Orientation = orientation, TextOverrunBehavior = TextOverrunBehavior.NoTrimming };
        return Own(line);
    }
    private RID Own(TextLine line)
    {
        try { var rid = line.GetRID(); lock (_buffersGate) _owned.Add(rid, line); return rid; }
        catch { line.Dispose(); throw; }
    }
    /// <summary>Reports whether an identity resolves to a live shaped buffer on this interface.</summary><param name="rid">Buffer identity.</param><returns>False for empty, retired or foreign identities.</returns>
    public bool Has(RID rid) => TextBuffers.TryResolve(rid, out _);
    /// <summary>Releases an owned buffer, preserving borrowed text-object lifetimes.</summary><param name="rid">Identity returned by buffer creation, duplication or substring operations.</param>
    /// <exception cref="InvalidOperationException">The identity is borrowed, unknown, off-owner or active.</exception>
    public void FreeRID(RID rid)
    {
        lock (_buffersGate)
        {
            if (!_owned.TryGetValue(rid, out var line)) throw new InvalidOperationException("Only buffers owned by this interface can be freed here.");
            line.Dispose(); _owned.Remove(rid);
        }
    }
    /// <summary>Copies a complete buffer into an independent owned identity.</summary><param name="shaped">Live source identity.</param><returns>A new identity sharing borrowed fonts, keys and metadata values.</returns>
    public RID ShapedTextDuplicate(RID shaped)
    {
        var v = View(shaped);
        return Own(v.Start == v.Offset && v.End == v.Offset + v.Source.ScalarCount ? v.Source.Duplicate() : v.Source.ServerSlice(v.Start - v.Offset, v.End - v.Offset));
    }
    /// <summary>Appends copied text with ordered borrowed fonts and copied feature overrides.</summary>
    /// <param name="shaped">Live buffer.</param><param name="text">Copied source; empty succeeds without a new span.</param><param name="fonts">One to sixty-four live ordered font resources.</param><param name="size">Positive logical font size.</param><param name="openTypeFeatures">Copied named or four-character OpenType selectors.</param><param name="language">Language tag.</param><returns>True after successful authoring.</returns>
    public bool ShapedTextAddString(RID shaped, string text, ReadOnlySpan<Font> fonts, int size, IReadOnlyDictionary<string, int>? openTypeFeatures = null, string language = "")
    { using var mutation = Mutation(shaped); var result = mutation.Buffer.ServerAdd(text, fonts, size, openTypeFeatures, language, null); if (text.Length != 0) mutation.Commit(); return result; }
    /// <summary>Appends text retaining metadata under its exact supplied type.</summary><typeparam name="T">Metadata type.</typeparam>
    /// <param name="shaped">Buffer identity.</param><param name="text">Copied source.</param><param name="fonts">Ordered borrowed fonts.</param><param name="size">Logical font size.</param><param name="openTypeFeatures">Copied feature overrides.</param><param name="language">Language tag.</param><param name="metadata">Borrowed typed value.</param><returns>True after successful authoring.</returns>
    public bool ShapedTextAddString<T>(RID shaped, string text, ReadOnlySpan<Font> fonts, int size, IReadOnlyDictionary<string, int>? openTypeFeatures, string language, T metadata)
    { using var mutation = Mutation(shaped); var result = mutation.Buffer.ServerAdd(text, fonts, size, openTypeFeatures, language, RichTextMetadata.Create(metadata)); if (text.Length != 0) mutation.Commit(); return result; }
    /// <summary>Appends one identity-keyed inline object.</summary><param name="shaped">Buffer identity.</param><param name="key">Unique borrowed token.</param><param name="size">Nonnegative finite dimensions.</param><param name="inlineAlign">Inline alignment mask.</param><param name="length">Positive replacement scalar count.</param><param name="baseline">Finite signed baseline offset.</param><returns>False for a duplicate key.</returns>
    public bool ShapedTextAddObject(RID shaped, TextObjectKey key, Vector2 size, InlineAlignment inlineAlign = InlineAlignment.Center, int length = 1, float baseline = 0)
    { using var mutation = Mutation(shaped); var b = mutation.Buffer; var result = b.AddObject(key, size, inlineAlign, length, baseline); if (result) { b.ServerChanged?.Invoke(); mutation.Commit(); } return result; }
    /// <summary>Updates an existing inline object.</summary><param name="shaped">Buffer identity.</param><param name="key">Borrowed token.</param><param name="size">Finite nonnegative dimensions.</param><param name="inlineAlign">Alignment mask.</param><param name="baseline">Finite signed baseline.</param><returns>False for a missing token.</returns>
    public bool ShapedTextResizeObject(RID shaped, TextObjectKey key, Vector2 size, InlineAlignment inlineAlign = InlineAlignment.Center, float baseline = 0)
    { using var mutation = Mutation(shaped); var b = mutation.Buffer; var result = b.ResizeObject(key, size, inlineAlign, baseline); if (result) { b.ServerChanged?.Invoke(); mutation.Commit(); } return result; }
    /// <summary>Clears authored text, objects and BIDI overrides while retaining policies and identity.</summary><param name="shaped">Live buffer.</param>
    public void ShapedTextClear(RID shaped) { using var mutation = Mutation(shaped); var b = mutation.Buffer; b.Clear(); b.ServerOffset = 0; b.ServerPrefix = b.ServerSuffix = default; b.ServerChanged?.Invoke(); mutation.Commit(); }
    /// <summary>Prepares the real native layout and metrics.</summary><param name="shaped">Buffer identity.</param><returns>True on successful preparation; errors throw.</returns>
    public bool ShapedTextShape(RID shaped) { _ = View(shaped); return true; }
    /// <summary>Reports whether a prepared buffer remains current without forcing a rebuild.</summary><param name="shaped">Buffer identity.</param><returns>Whether the authored content and borrowed font generations match.</returns>
    public bool ShapedTextIsReady(RID shaped)
    {
        var owner = TextBuffers.Resolve(shaped);
        if (owner.Owner is TextParagraph paragraph) return paragraph.ServerIsReady(owner.Line, shaped);
        var source = (TextLine)owner.Owner; source.ServerAccess(); return source.ServerReady;
    }
    /// <summary>Returns complete underlying source text, including replacement scalars and the surrounding source of a line view.</summary><param name="shaped">Buffer identity.</param><returns>An immutable source snapshot.</returns>
    public string ShapedGetText(RID shaped) { var v = AuthoringView(shaped); return v.Source.ServerFullText; }
    /// <summary>Returns the number of source text and inline-object spans intersecting this buffer.</summary><param name="shaped">Buffer identity.</param><returns>Authored span count.</returns>
    public int ShapedGetSpanCount(RID shaped)
    { var v = AuthoringView(shaped); return v.Source.ServerSpanCountInRange(v.Start - v.Offset, v.End - v.Offset); }
    private static (BufferView View, int At, int End, TextObjectKey? Key, RichTextMetadata? Metadata) Span(RID shaped, int index)
    {
        var v = AuthoringView(shaped); var span = v.Source.ServerSpan(index, v.Start - v.Offset, v.End - v.Offset);
        return (v, span.At, span.At + span.Length, span.Key, span.Metadata);
    }
    /// <summary>Returns the source text of an authored span.</summary><param name="shaped">Buffer identity.</param><param name="index">Text-or-object span index.</param><returns>The source substring, including object replacement scalars.</returns>
    public string ShapedGetSpanText(RID shaped, int index) { var s = Span(shaped, index); return s.View.Source.ServerSliceText(s.At, s.End); }
    /// <summary>Returns a span's inline-object key.</summary><param name="shaped">Buffer identity.</param><param name="index">Text-or-object span index.</param><returns>The borrowed token, or null for a text span.</returns>
    public TextObjectKey? ShapedGetSpanObject(RID shaped, int index) => Span(shaped, index).Key;
    /// <summary>Returns a span's embedded inline-object key.</summary><param name="shaped">Buffer identity.</param><param name="index">Text-or-object span index.</param><returns>The borrowed token, or null for a text span.</returns>
    public TextObjectKey? ShapedGetSpanEmbeddedObject(RID shaped, int index) => Span(shaped, index).Key;
    /// <summary>Reads span metadata under its exact original type.</summary><typeparam name="T">Original metadata type.</typeparam><param name="shaped">Buffer identity.</param><param name="index">Text-or-object span index.</param><param name="metadata">Borrowed value, or default.</param><returns>Whether the exact type matches.</returns>
    public bool TryGetSpanMetadata<T>(RID shaped, int index, out T metadata)
    { var span = Span(shaped, index); if (span.Metadata is { } value) return value.TryGet(out metadata); metadata = default!; return false; }
    /// <summary>Returns the buffer's half-open Unicode scalar range.</summary><param name="shaped">Buffer identity.</param><returns>Start and exclusive end.</returns>
    public Vector2i ShapedTextGetRange(RID shaped) { var v = AuthoringView(shaped); return new(v.Start, v.End); }
    /// <summary>Returns the number of glyph records in visual order.</summary><param name="shaped">Buffer identity.</param><returns>The prepared glyph count.</returns>
    public int ShapedTextGetGlyphCount(RID shaped) => View(shaped).Metrics.GlyphCount;
    /// <summary>Returns the ceiled advance-axis width.</summary><param name="shaped">Buffer identity.</param><returns>Logical pixels.</returns>
    public float ShapedTextGetWidth(RID shaped) => MathF.Ceiling(View(shaped).Advance);
    /// <summary>Returns fractional ascent above the baseline.</summary><param name="shaped">Buffer identity.</param><returns>Logical pixels.</returns>
    public float ShapedTextGetAscent(RID shaped) => View(shaped).Metrics.Ascent;
    /// <summary>Returns fractional descent below the baseline.</summary><param name="shaped">Buffer identity.</param><returns>Logical pixels.</returns>
    public float ShapedTextGetDescent(RID shaped) => View(shaped).Metrics.Descent;
    /// <summary>Returns ceiled advance and cross-axis dimensions.</summary><param name="shaped">Buffer identity.</param><returns>Dimensions in the buffer's orientation.</returns>
    public Vector2 ShapedTextGetSize(RID shaped) { var v = View(shaped); var m = v.Metrics; return (v.Source.Orientation == TextOrientation.Horizontal ? new Vector2(v.Advance, m.Height) : new Vector2(m.Height, v.Advance)).Ceil(); }
    /// <summary>Returns underline offset for the buffer's authored fonts.</summary><param name="shaped">Buffer identity.</param><returns>Logical pixels below the baseline.</returns>
    public float ShapedTextGetUnderlinePosition(RID shaped) { var v = View(shaped); return v.Layout.ServerUnderline(v.Line, false); }
    /// <summary>Returns maximum authored underline thickness.</summary><param name="shaped">Buffer identity.</param><returns>Logical pixels.</returns>
    public float ShapedTextGetUnderlineThickness(RID shaped) { var v = View(shaped); return v.Layout.ServerUnderline(v.Line, true); }
    /// <summary>Returns the first paragraph's inferred direction.</summary><param name="shaped">Buffer identity.</param><returns>LTR or RTL.</returns>
    public TextDirection ShapedTextGetInferredDirection(RID shaped) => View(shaped).Metrics.ParagraphLevel == 1 ? TextDirection.RTL : TextDirection.LTR;
    /// <summary>Returns the authored base direction.</summary><param name="shaped">Buffer identity.</param><returns>The direction policy.</returns>
    public TextDirection ShapedTextGetDirection(RID shaped) => ReadSource(shaped).Direction;
    /// <summary>Replaces the base direction.</summary><param name="shaped">Buffer identity.</param><param name="direction">Defined direction.</param>
    public void ShapedTextSetDirection(RID shaped, TextDirection direction = TextDirection.Auto) { if (direction == TextDirection.Inherited) throw new ArgumentOutOfRangeException(nameof(direction)); if (ReadSource(shaped).Direction == direction) return; using var mutation = Mutation(shaped); var b = mutation.Buffer; b.Direction = direction; b.ServerChanged?.Invoke(); mutation.Commit(); }
    /// <summary>Returns the advance orientation.</summary><param name="shaped">Buffer identity.</param><returns>Horizontal or Vertical.</returns>
    public TextOrientation ShapedTextGetOrientation(RID shaped) => ReadSource(shaped).Orientation;
    /// <summary>Replaces the advance orientation.</summary><param name="shaped">Buffer identity.</param><param name="orientation">Defined orientation.</param>
    public void ShapedTextSetOrientation(RID shaped, TextOrientation orientation = TextOrientation.Horizontal) { if (ReadSource(shaped).Orientation == orientation) return; using var mutation = Mutation(shaped); var b = mutation.Buffer; b.Orientation = orientation; b.ServerChanged?.Invoke(); mutation.Commit(); }
    /// <summary>Returns the missing-character display policy.</summary><param name="shaped">Buffer identity.</param><returns>Whether invalid characters display hexadecimal boxes.</returns>
    public bool ShapedTextGetPreserveInvalid(RID shaped) => ReadSource(shaped).PreserveInvalid;
    /// <summary>Replaces the missing-character display policy.</summary><param name="shaped">Buffer identity.</param><param name="enabled">Whether to display missing characters.</param>
    public void ShapedTextSetPreserveInvalid(RID shaped, bool enabled) { if (ReadSource(shaped).PreserveInvalid == enabled) return; using var mutation = Mutation(shaped); var b = mutation.Buffer; b.PreserveInvalid = enabled; b.ServerChanged?.Invoke(); mutation.Commit(); }
    /// <summary>Returns the nonprinting-control display policy.</summary><param name="shaped">Buffer identity.</param><returns>The control display preference.</returns>
    public bool ShapedTextGetPreserveControl(RID shaped) => ReadSource(shaped).PreserveControl;
    /// <summary>Replaces the nonprinting-control display policy.</summary><param name="shaped">Buffer identity.</param><param name="enabled">Whether controls display glyphs.</param>
    public void ShapedTextSetPreserveControl(RID shaped, bool enabled) { if (ReadSource(shaped).PreserveControl == enabled) return; using var mutation = Mutation(shaped); var b = mutation.Buffer; b.PreserveControl = enabled; b.ServerChanged?.Invoke(); mutation.Commit(); }
    /// <summary>Replaces contiguous scalar BIDI override ranges.</summary><param name="shaped">Buffer identity.</param><param name="ranges">Copied full-buffer contexts, or empty to clear.</param>
    public void ShapedTextSetBIDIOverride(RID shaped, ReadOnlySpan<TextBIDIRange> ranges)
    {
        using var mutation = Mutation(shaped); var b = mutation.Buffer;
        if (b.ServerOffset == 0 || ranges.IsEmpty) b.SetBIDIOverride(ranges);
        else { var copy = new TextBIDIRange[ranges.Length]; for (var i = 0; i < copy.Length; i++) copy[i] = new(checked(ranges[i].Start - b.ServerOffset), checked(ranges[i].End - b.ServerOffset), ranges[i].Direction); b.SetBIDIOverride(copy); }
        b.ServerChanged?.Invoke(); mutation.Commit();
    }
    /// <summary>Aligns tabs to repeating positive interval lengths.</summary><param name="shaped">Buffer identity.</param><param name="tabStops">Copied finite intervals.</param><returns>The resulting ceiled width.</returns>
    public float ShapedTextTabAlign(RID shaped, ReadOnlySpan<float> tabStops) { using var mutation = Mutation(shaped); var b = mutation.Buffer; b.TabAlign(tabStops); b.ServerChanged?.Invoke(); mutation.Commit(); return ShapedTextGetWidth(shaped); }
    /// <summary>Tests for an inline token in the buffer.</summary><param name="shaped">Buffer identity.</param><param name="key">Borrowed token.</param><returns>Whether present.</returns>
    public bool ShapedTextHasObject(RID shaped, TextObjectKey key)
    { var v = AuthoringView(shaped); var range = v.Source.ServerObjectRange(key); return range.X >= 0 && range.X + v.Offset >= v.Start && range.X + v.Offset < v.End; }
    /// <summary>Returns inline tokens in source order.</summary><param name="shaped">Buffer identity.</param><returns>An independent array of borrowed tokens.</returns>
    public TextObjectKey[] ShapedTextGetObjects(RID shaped)
    {
        var v = AuthoringView(shaped); var output = new List<TextObjectKey>();
        for (var i = 0; i < v.Source.ObjectCount; i++) { var item = v.Source.ObjectAt(i); if (item.At + v.Offset >= v.Start && item.At + v.Offset < v.End) output.Add(item.Key); }
        return output.ToArray();
    }
    /// <summary>Returns an object's half-open scalar range.</summary><param name="shaped">Buffer identity.</param><param name="key">Borrowed token.</param><returns>Start and exclusive end.</returns><exception cref="KeyNotFoundException">The token is absent.</exception>
    public Vector2i ShapedTextGetObjectRange(RID shaped, TextObjectKey key)
    {
        var v = AuthoringView(shaped); var range = v.Source.ServerObjectRange(key);
        if (range.X < 0 || range.X + v.Offset < v.Start || range.X + v.Offset >= v.End) throw new KeyNotFoundException("The inline object is absent from this buffer.");
        return range + new Vector2i(v.Offset, v.Offset);
    }
    /// <summary>Returns an inline rectangle relative to the buffer baseline.</summary><param name="shaped">Buffer identity.</param><param name="key">Borrowed token.</param><returns>Full shaped geometry.</returns><exception cref="KeyNotFoundException">The token is absent.</exception>
    public Rect2 ShapedTextGetObjectRect(RID shaped, TextObjectKey key)
    {
        var v = View(shaped); ArgumentNullException.ThrowIfNull(key);
        for (var i = 0; i < v.Source.ObjectCount; i++) { var o = v.Source.ObjectAt(i); if (ReferenceEquals(o.Key, key) && o.At + v.Offset >= v.Start && o.At + v.Offset < v.End) return v.Layout.ServerObjectRect(i, o.Size, v.Line); }
        throw new KeyNotFoundException("The inline object is absent from this buffer.");
    }
    /// <summary>Finds the closest scalar caret, interpolating inside shaped ligatures.</summary><param name="shaped">Buffer identity.</param><param name="coords">Finite baseline advance offset.</param><returns>A Unicode scalar position.</returns>
    public int ShapedTextHitTestPosition(RID shaped, float coords)
    { if (!float.IsFinite(coords)) throw new ArgumentOutOfRangeException(nameof(coords)); var v = View(shaped); return v.Metrics.GlyphCount == 0 ? 0 : v.Layout.ServerHitPosition(v.Line, coords, MathF.Ceiling(v.Advance)) + v.Offset; }
    /// <summary>Records the current shaped display at a baseline origin.</summary><param name="shaped">Buffer identity.</param><param name="canvas">Recording canvas.</param><param name="position">Finite baseline origin.</param><param name="clipL">Negative disables the leading advance bound.</param><param name="clipR">Negative disables the trailing advance bound.</param><param name="color">Modulation, or white.</param><param name="oversampling">Positive raster scale, or automatic.</param>
    public void ShapedTextDraw(RID shaped, CanvasItem canvas, Vector2 position, float clipL = -1, float clipR = -1, Color? color = null, float oversampling = 0)
        => DrawBuffer(shaped, canvas, position, clipL, clipR, 0, color ?? Colors.White, oversampling, false);
    /// <summary>Records a shaped glyph outline at a baseline origin.</summary><param name="shaped">Buffer identity.</param><param name="canvas">Recording canvas.</param><param name="position">Baseline origin.</param><param name="clipL">Leading advance bound, or negative.</param><param name="clipR">Trailing advance bound, or negative.</param><param name="outlineSize">Outline radius; nonpositive uses unexpanded glyphs.</param><param name="color">Modulation, or white.</param><param name="oversampling">Positive raster scale, or automatic.</param>
    public void ShapedTextDrawOutline(RID shaped, CanvasItem canvas, Vector2 position, float clipL = -1, float clipR = -1, int outlineSize = 1, Color? color = null, float oversampling = 0)
        => DrawBuffer(shaped, canvas, position, clipL, clipR, Math.Max(0, outlineSize), color ?? Colors.White, oversampling, true);
    private static void DrawBuffer(RID rid, CanvasItem canvas, Vector2 baseline, float left, float right, int outline, Color color, float oversampling, bool outlinePass)
    {
        var owner = TextBuffers.Resolve(rid);
        if (owner.Owner is TextParagraph paragraph) paragraph.ServerDraw(owner.Line, rid, canvas, baseline, left, right, outline, color, oversampling, outlinePass);
        else { var source = (TextLine)owner.Owner; source.ServerAccess(); source.ServerDraw(canvas, baseline, left, right, outline, color, oversampling, outlinePass); }
    }
    private sealed class BufferMutation(TextLine buffer, TextParagraph? parent = null, int line = -1) : IDisposable
    {
        internal TextLine Buffer { get; } = buffer;
        private bool _pending = parent is not null;
        internal void Commit() { Buffer.ServerDetachSubstring(); if (!_pending) return; parent!.ServerPublishLine(line, Buffer); _pending = false; }
        public void Dispose() { if (_pending) Buffer.Dispose(); }
    }
    private static BufferMutation Mutation(RID rid)
    {
        var owner = TextBuffers.Resolve(rid);
        if (owner.Owner is TextLine line) { line.ServerAccess(); return new(line); }
        var parent = (TextParagraph)owner.Owner; var buffer = parent.ServerMutableLine(owner.Line, rid, out var pending);
        return new(buffer, pending ? parent : null, owner.Line);
    }
    private static TextLine ReadSource(RID rid)
    {
        var owner = TextBuffers.Resolve(rid);
        if (owner.Owner is TextLine line) { line.ServerAccess(); return line; }
        return ((TextParagraph)owner.Owner).ServerAuthoringView(owner.Line, rid).Source;
    }
    private static BufferView AuthoringView(RID rid)
    {
        var owner = TextBuffers.Resolve(rid);
        if (owner.Owner is TextParagraph paragraph) return paragraph.ServerAuthoringView(owner.Line, rid);
        var source = (TextLine)owner.Owner; source.ServerAccess();
        return new(source, source.ServerUnpreparedLayout, 0, source.ServerOffset, source.ServerOffset + source.ScalarCount, source.ServerOffset);
    }
    private static BufferView View(RID rid)
    {
        var owner = TextBuffers.Resolve(rid);
        if (owner.Owner is TextParagraph paragraph) return paragraph.ServerView(owner.Line, rid);
        var line = (TextLine)owner.Owner; line.ServerAccess(); var layout = line.ServerLayout;
        return new(line, layout, 0, line.ServerOffset, line.ServerOffset + line.ScalarCount, line.ServerOffset, line.ServerDisplayWidth);
    }
    internal readonly record struct BufferView(TextLine Source, TextLayout Layout, int Line, int Start, int End, int Offset = 0, float? DisplayWidth = null)
    {
        internal float Advance => DisplayWidth ?? Metrics.Width;
        internal TextLayoutLine Metrics => Layout.LineCount == 0 ? default : Layout.Lines[Line];
    }
    /// <inheritdoc />
    protected override void ValidateDisposal() => throw new InvalidOperationException("A retained text interface cannot be disposed.");
}
