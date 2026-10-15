using System.Text;

namespace Electron2D;

/// <summary>A caller-owned reference-identity key for one inline text object.</summary>
/// <remarks>The same token identifies the object in a duplicated line. Tokens own no scene or native state.</remarks>
public sealed class TextObjectKey
{
    /// <summary>Creates a distinct inline-object token.</summary>
    public TextObjectKey() { }
}

/// <summary>Authors, shapes, measures and draws a retained single line with borrowed fonts and inline objects.</summary>
/// <remarks>The creating thread owns access. Typed span metadata, object keys and font resources remain borrowed;
/// duplicates copy authored state and own separate buffers. Prepared queries and drawing reuse layout storage.
/// Size/width describe the trimmed display; carets and inline geometry retain the complete shaped buffer.
/// Authoring is bounded to 1,048,576 Unicode scalars and 64 MiB UTF-16 text. This transient object is not serialized.</remarks>
public sealed class TextLine : ElectronObject
{
    private sealed class NeutralFont : Font { internal override FontData? PrimaryData => null; }
    private sealed record Span(int At, int Length, Font Font, int Size, string Language, RichTextMetadata? Metadata);
    private sealed record Inline(TextObjectKey Key, int At, int Length, Vector2 Size, InlineAlignment Alignment, float Baseline);
    private readonly int _thread = Environment.CurrentManagedThreadId;
    private readonly NeutralFont _neutral = new();
    private TextLayout _full = new(), _display = new();
    private readonly StringBuilder _text = new();
    private readonly List<Span> _spans = [];
    private readonly List<Inline> _objects = [];
    private readonly List<Font> _fonts = [];
    private readonly List<Font.ReadScope> _reads = [];
    private readonly List<long> _generations = [];
    private TextLayoutStyleSpan[] _styles = [];
    private TextLayoutInlineObject[] _inline = [];
    private TextBIDIRange[] _bidi = [];
    private float[] _tabs = [];
    private string _source = string.Empty, _ellipsis = "…";
    private int _count;
    private bool _dirty = true, _contentDirty = true, _busy, _preserveControl, _preserveInvalid = true;
    private float _width = -1;
    private HorizontalAlignment _alignment;
    private TextDirection _direction;
    private TextOrientation _orientation;
    private TextJustificationFlags _flags = TextJustificationFlags.Kashida | TextJustificationFlags.WordBound;
    private TextOverrunBehavior _overrun = TextOverrunBehavior.TrimEllipsis;
    private readonly RID _rid;

    /// <summary>Creates an empty thread-owned shaped-text buffer with the default line policies.</summary>
    public TextLine() => _rid = TextLineBuffers.Register(this);
    /// <summary>Gets or sets advance-axis alignment; initially Left.</summary>
    /// <value>Left, Center, Right or Fill.</value>
    public HorizontalAlignment Alignment { get { Check(); return _alignment; } set { CheckMutation(); ValidateEnum(value); if (_alignment == value) return; _alignment = value; _dirty = true; } }
    /// <summary>Gets or sets writing direction; initially Auto.</summary><value>A defined text direction.</value>
    public TextDirection Direction { get { Check(); return _direction; } set { CheckMutation(); ValidateEnum(value); if (_direction == value) return; _direction = value; _dirty = true; } }
    /// <summary>Gets or sets the drawing advance axis; initially Horizontal.</summary><value>A defined text orientation.</value>
    public TextOrientation Orientation { get { Check(); return _orientation; } set { CheckMutation(); ValidateEnum(value); if (_orientation == value) return; _orientation = value; _dirty = true; } }
    /// <summary>Gets or sets the custom ellipsis text; its first Unicode scalar is used, or the default ellipsis for empty text.</summary>
    /// <value>The ellipsis character initially.</value>
    public string EllipsisChar { get { Check(); return _ellipsis; } set { CheckMutation(); ArgumentNullException.ThrowIfNull(value); if (value.Length > 33554432) throw new ArgumentException("Ellipsis exceeds text limits."); if (_ellipsis == value) return; _ellipsis = value; _dirty = true; } }
    /// <summary>Gets or sets Fill justification flags; initially Kashida and WordBound.</summary><value>Defined flag bits.</value>
    public TextJustificationFlags Flags { get { Check(); return _flags; } set { CheckMutation(); if (((int)value & ~255) != 0) throw new ArgumentOutOfRangeException(nameof(value)); if (_flags == value) return; _flags = value; _dirty = true; } }
    /// <summary>Gets or sets whether otherwise nonprinting controls are displayed; initially false.</summary><value>The real control-glyph policy.</value>
    public bool PreserveControl { get { Check(); return _preserveControl; } set { CheckMutation(); if (_preserveControl == value) return; _preserveControl = value; _dirty = true; } }
    /// <summary>Gets or sets whether missing characters display hexadecimal glyph boxes; initially true.</summary><value>The real missing-glyph policy.</value>
    public bool PreserveInvalid { get { Check(); return _preserveInvalid; } set { CheckMutation(); if (_preserveInvalid == value) return; _preserveInvalid = value; _dirty = true; } }
    /// <summary>Gets or sets the display trimming policy; initially TrimEllipsis.</summary><value>A defined overrun policy.</value>
    public TextOverrunBehavior TextOverrunBehavior { get { Check(); return _overrun; } set { CheckMutation(); ValidateEnum(value); if (_overrun == value) return; _overrun = value; _dirty = true; } }
    /// <summary>Gets or sets available advance-axis width; initially minus one.</summary><value>A finite value; nonpositive values do not trim or align.</value>
    public float Width { get { Check(); return _width; } set { CheckMutation(); if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_width == value) return; _width = value; _dirty = true; } }
    /// <summary>Gets the number of authored text segments, excluding inline objects and empty additions.</summary><value>Zero initially.</value>
    public int SpanCount { get { Check(); return _spans.Count; } }

    /// <summary>Appends one copied text segment with its borrowed font and language.</summary>
    /// <param name="text">Text; empty is a successful no-op. Unpaired UTF-16 units become replacement scalars.</param>
    /// <param name="font">Live borrowed font.</param><param name="fontSize">Positive supported logical size.</param><param name="language">Language tag, or empty for automatic script inference.</param>
    /// <returns>True after successful authoring.</returns>
    /// <exception cref="ArgumentNullException">Text, font or language is null.</exception>
    /// <exception cref="ArgumentException">Text exceeds the preparation limits.</exception>
    /// <exception cref="InvalidOperationException">Explicit BIDI contexts are active, access is off-owner or native work is active.</exception>
    public bool AddString(string text, Font font, int fontSize, string language = "") => AddText(text, font, fontSize, language, null);
    /// <summary>Appends a segment retaining metadata under its exact original type.</summary>
    /// <typeparam name="T">Borrowed metadata type.</typeparam><param name="text">Copied source text.</param><param name="font">Live borrowed font.</param>
    /// <param name="fontSize">Logical font size.</param><param name="language">Language tag.</param><param name="metadata">Value retained by ordinary C# assignment.</param>
    /// <returns>True after successful authoring.</returns>
    public bool AddString<T>(string text, Font font, int fontSize, string language, T metadata) => AddText(text, font, fontSize, language, RichTextMetadata.Create(metadata));
    /// <summary>Reads a segment's exact typed metadata without conversion.</summary><typeparam name="T">Requested original type.</typeparam>
    /// <param name="spanIndex">Zero-based successful nonempty AddString index.</param><param name="metadata">Borrowed value, or default.</param><returns>Whether the exact type matches.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The text span does not exist.</exception>
    public bool TryGetSpanMetadata<T>(int spanIndex, out T metadata)
    { Check(); if ((uint)spanIndex >= (uint)_spans.Count) throw new ArgumentOutOfRangeException(nameof(spanIndex)); if (_spans[spanIndex].Metadata is { } value) return value.TryGet(out metadata); metadata = default!; return false; }
    private bool AddText(string text, Font font, int size, string language, RichTextMetadata? metadata)
    {
        CheckMutation(); ArgumentNullException.ThrowIfNull(text); ArgumentNullException.ThrowIfNull(font); ArgumentNullException.ThrowIfNull(language); Font.ValidateSize(size);
        if (font.IsDisposed) throw new ObjectDisposedException(nameof(font)); if (text.Length == 0) return true;
        RequireAppend(); if (text.Length > 33554432 - _text.Length || text.Length > (1048576 - _count) * 2) throw new ArgumentException("Text buffer exceeds preparation limits."); var length = Count(text); ValidateExpansion(text.Length, length);
        _spans.EnsureCapacity(_spans.Count + 1); _fonts.EnsureCapacity(_fonts.Count + 1); _generations.EnsureCapacity(_fonts.Count + 1); _text.EnsureCapacity(_text.Length + text.Length);
        _spans.Add(new(_count, length, font, size, language, metadata)); _text.Append(text); _count += length;
        if (!_fonts.Contains(font)) { _fonts.Add(font); _fonts.Sort(static (a, b) => a.InstanceID.CompareTo(b.InstanceID)); }
        _dirty = _contentDirty = true; return true;
    }
    /// <summary>Appends one inline object represented by the requested number of replacement scalars.</summary>
    /// <param name="key">Unique borrowed reference token.</param><param name="size">Finite nonnegative dimensions.</param><param name="inlineAlign">Two-axis alignment mask.</param>
    /// <param name="length">Positive scalar length; one initially.</param><param name="baseline">Finite explicit baseline offset, including negative offsets.</param>
    /// <returns>False for an already present token, otherwise true.</returns>
    public bool AddObject(TextObjectKey key, Vector2 size, InlineAlignment inlineAlign = InlineAlignment.Center, int length = 1, float baseline = 0)
    {
        CheckMutation(); ValidateObject(key, size, inlineAlign, baseline); if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length));
        if (_objects.Any(item => ReferenceEquals(item.Key, key))) return false; RequireAppend(); ValidateExpansion(length, length);
        var content = new string('\uFFFC', length); _objects.EnsureCapacity(_objects.Count + 1); _text.EnsureCapacity(_text.Length + length);
        _objects.Add(new(key, _count, length, size, inlineAlign, baseline)); _text.Append(content); _count += length; _dirty = _contentDirty = true; return true;
    }
    /// <summary>Changes an existing inline object's size/alignment/baseline without changing its source range.</summary>
    /// <param name="key">Existing borrowed token.</param><param name="size">Finite nonnegative dimensions.</param><param name="inlineAlign">Two-axis mask.</param><param name="baseline">Finite explicit offset.</param>
    /// <returns>False for an absent token.</returns>
    public bool ResizeObject(TextObjectKey key, Vector2 size, InlineAlignment inlineAlign = InlineAlignment.Center, float baseline = 0)
    {
        CheckMutation(); ValidateObject(key, size, inlineAlign, baseline);
        for (var i = 0; i < _objects.Count; i++) if (ReferenceEquals(_objects[i].Key, key)) { _objects[i] = _objects[i] with { Size = size, Alignment = inlineAlign, Baseline = baseline }; _dirty = _contentDirty = true; return true; }
        return false;
    }
    /// <summary>Reports whether the exact inline token is authored in this line.</summary><param name="key">Borrowed token.</param><returns>Presence in the complete buffer.</returns>
    public bool HasObject(TextObjectKey key) { Check(); ArgumentNullException.ThrowIfNull(key); foreach (var item in _objects) if (ReferenceEquals(item.Key, key)) return true; return false; }
    /// <summary>Returns inline tokens in authored order as a caller-owned snapshot.</summary><returns>Borrowed tokens, including trimmed objects.</returns>
    public TextObjectKey[] GetObjects() { Check(); var keys = new TextObjectKey[_objects.Count]; for (var i = 0; i < keys.Length; i++) keys[i] = _objects[i].Key; return keys; }
    /// <summary>Returns an inline rectangle in line-top-left coordinates, including display alignment.</summary><param name="key">Borrowed token.</param><returns>Zero for an absent token; full geometry for a trimmed object.</returns>
    public Rect2 GetObjectRect(TextObjectKey key)
    {
        Check(); ArgumentNullException.ThrowIfNull(key); Ensure();
        for (var i = 0; i < _objects.Count; i++) if (ReferenceEquals(_objects[i].Key, key)) { var rect = _full.InlineRect(i, _objects[i].Size); rect.Position += AlignmentOffset(); return rect; }
        return default;
    }
    /// <summary>Replaces BIDI contexts with copied contiguous ranges covering the current scalar buffer.</summary><param name="ranges">Defined directions and full nonoverlapping scalar ranges; empty clears overrides.</param>
    /// <exception cref="ArgumentException">Ranges do not cover the buffer contiguously.</exception>
    public void SetBIDIOverride(ReadOnlySpan<TextBIDIRange> ranges)
    {
        CheckMutation(); var end = 0;
        foreach (var range in ranges) { ValidateEnum(range.Direction); if (range.Start != end || range.End <= range.Start || range.End > _count) throw new ArgumentException("BIDI ranges must cover current text contiguously."); end = range.End; }
        if (ranges.Length > 0 && end != _count) throw new ArgumentException("BIDI ranges must cover current text."); _bidi = ranges.ToArray(); _dirty = true;
    }
    /// <summary>Replaces repeating tab interval lengths.</summary><param name="tabStops">Copied finite positive intervals; empty restores the backend default.</param>
    public void TabAlign(ReadOnlySpan<float> tabStops)
    { CheckMutation(); if (tabStops.Length > 1048576) throw new ArgumentException("Tab count exceeds preparation limits."); foreach (var stop in tabStops) if (!float.IsFinite(stop) || stop <= 0) throw new ArgumentOutOfRangeException(nameof(tabStops)); _tabs = tabStops.ToArray(); _dirty = true; }
    /// <summary>Returns the stable borrowed identity of this line's live shaped buffer.</summary><returns>A distinct nonempty identity; Clear preserves it and duplicates have their own identity.</returns>
    public RID GetRID() { Check(); return _rid; }
    /// <summary>Returns the inferred first-paragraph writing direction.</summary><returns>LTR for an empty automatic buffer.</returns>
    public TextDirection GetInferredDirection() { Check(); Ensure(); return _full.LineCount > 0 ? (_full.Lines[0].ParagraphLevel == 1 ? TextDirection.RTL : TextDirection.LTR) : _direction == TextDirection.RTL ? TextDirection.RTL : TextDirection.LTR; }
    /// <summary>Returns the complete shaped line ascent.</summary><returns>Fractional pixels, or zero for empty text.</returns>
    public float GetLineAscent() { Check(); Ensure(); return _full.FirstAscent; }
    /// <summary>Returns the complete shaped line descent.</summary><returns>Fractional pixels, or zero for empty text.</returns>
    public float GetLineDescent() { Check(); Ensure(); return _full.LineCount == 0 ? 0 : _full.Lines[0].Descent; }
    /// <summary>Returns the ceiled advance-axis width of the trimmed display.</summary><returns>Zero for empty text.</returns>
    public float GetLineWidth() { Check(); Ensure(); return DisplayWidth(); }
    /// <summary>Returns the ceiled trimmed bounding size.</summary><returns>Width/height follow Orientation.</returns>
    public Vector2 GetSize() { Check(); Ensure(); return _display.Size; }
    /// <summary>Returns maximum authored-font underline offset below the baseline.</summary><returns>Zero for no font spans.</returns>
    public float GetLineUnderlinePosition() { Check(); Ensure(); float result = 0; foreach (var span in _spans) result = Math.Max(result, span.Font.GetUnderlinePosition(span.Size)); return result; }
    /// <summary>Returns maximum authored-font underline thickness.</summary><returns>Zero for no font spans.</returns>
    public float GetLineUnderlineThickness() { Check(); Ensure(); float result = 0; foreach (var span in _spans) result = Math.Max(result, span.Font.GetUnderlineThickness(span.Size)); return result; }
    /// <summary>Returns a valid complete-buffer scalar caret position near the baseline offset.</summary><param name="coords">Finite advance-axis pixels, including positions outside the line.</param><returns>Unicode scalar index with grapheme-boundary preference.</returns>
    public int HitTest(float coords) { Check(); if (!float.IsFinite(coords)) throw new ArgumentOutOfRangeException(nameof(coords)); Ensure(); var limit = _full.LineCount > 0 ? MathF.Ceiling(_full.Lines[0].Width) + 1 : 1; return _full.HitColumn(Math.Clamp(coords, -1, limit), false); }
    /// <summary>Draws the trimmed line from its top-left origin into a recording canvas.</summary><param name="canvas">Live recording canvas item.</param><param name="position">Finite top-left position.</param><param name="color">Finite modulation, or white.</param><param name="oversampling">Finite positive raster scale, or nonpositive for automatic scale.</param>
    public void Draw(CanvasItem canvas, Vector2 position, Color? color = null, float oversampling = 0) => DrawCore(canvas, position, 0, color ?? Colors.White, oversampling, false);
    /// <summary>Draws the trimmed glyph outline from its top-left origin.</summary><param name="canvas">Live recording canvas.</param><param name="position">Finite top-left.</param><param name="outlineSize">Nonpositive draws the unexpanded glyph.</param><param name="color">Finite modulation, or white.</param><param name="oversampling">Raster scale, or nonpositive automatic scale.</param>
    public void DrawOutline(CanvasItem canvas, Vector2 position, int outlineSize = 1, Color? color = null, float oversampling = 0) => DrawCore(canvas, position, Math.Max(0, outlineSize), color ?? Colors.White, oversampling, true);
    private void DrawCore(CanvasItem canvas, Vector2 position, int outline, Color color, float oversampling, bool outlinePass)
    {
        Check(); Font.ValidateDraw(canvas, position, color, oversampling); Ensure(); _busy = true;
        try
        {
            BeginReads(); if (FontsChanged()) Prepare();
            var owner = TextLineBuffers.Resolve(_rid); if (!ReferenceEquals(owner, this)) throw new InvalidOperationException("Wrong text buffer owner.");
            var origin = position + AlignmentOffset() + (_orientation == TextOrientation.Horizontal ? new Vector2(0, _full.FirstAscent) : new Vector2(_full.FirstAscent, 0));
            if (!origin.IsFinite()) throw new OverflowException("Text draw origin exceeds finite geometry.");
            var offset = AlignmentOffset(); var clipStart = Math.Max(0, -(_orientation == TextOrientation.Horizontal ? offset.X : offset.Y));
            _display.Draw(canvas, origin, color, outline, oversampling, outlinePass: outlinePass, clipToWidth: _width > 0, clipStart: clipStart);
        }
        finally { try { EndReads(); } finally { _busy = false; } }
    }
    /// <summary>Copies authored state and every line policy into an independent thread-owned buffer.</summary><returns>A caller-owned duplicate sharing borrowed fonts/keys/payload values.</returns>
    public TextLine Duplicate()
    {
        Check(); var copy = new TextLine { _source = _source, _count = _count, _alignment = _alignment, _direction = _direction, _orientation = _orientation, _ellipsis = _ellipsis, _flags = _flags, _overrun = _overrun, _width = _width, _preserveControl = _preserveControl, _preserveInvalid = _preserveInvalid, _tabs = (float[])_tabs.Clone(), _bidi = (TextBIDIRange[])_bidi.Clone() };
        copy._text.Append(_text); copy._spans.AddRange(_spans); copy._objects.AddRange(_objects); copy._fonts.AddRange(_fonts); return copy;
    }
    /// <summary>Removes content, objects and BIDI contexts while preserving RID, tabs and line policies.</summary>
    public void Clear() { CheckMutation(); _text.Clear(); _spans.Clear(); _objects.Clear(); _fonts.Clear(); _bidi = []; _count = 0; _source = string.Empty; _styles = []; _inline = []; _full.ReleaseContent(); _display.ReleaseContent(); _dirty = _contentDirty = true; }
    private void Ensure()
    {
        Check(); if (_busy) throw new InvalidOperationException("Text buffer cannot reenter active layout work."); _busy = true;
        try { BeginReads(); if (_dirty || FontsChanged()) Prepare(); }
        finally { try { EndReads(); } finally { _busy = false; } }
    }
    private void BeginReads() { foreach (var font in _fonts) _reads.Add(font.BeginRead()); }
    private void EndReads() { for (var i = _reads.Count - 1; i >= 0; i--) _reads[i].Dispose(); _reads.Clear(); }
    private bool FontsChanged() { if (_generations.Count != _fonts.Count) return true; for (var i = 0; i < _fonts.Count; i++) if (_fonts[i].GetContentGeneration() != _generations[i]) return true; return false; }
    private void Prepare()
    {
        if (_contentDirty)
        {
            _source = _text.ToString(); _styles = new TextLayoutStyleSpan[_spans.Count]; _inline = new TextLayoutInlineObject[_objects.Count];
            for (var i = 0; i < _spans.Count; i++) { var span = _spans[i]; _styles[i] = new(span.At, span.At + span.Length, span.Font, span.Size, span.Language, i); }
            for (var i = 0; i < _objects.Count; i++) { var item = _objects[i]; _inline[i] = new(item.At, item.Size, item.Alignment, i, item.Baseline, item.Length, true); }
        }
        var ellipsis = _ellipsis.EnumerateRunes(); var symbol = ellipsis.MoveNext() ? ellipsis.Current.ToString() : "…";
        var key = new TextLayoutKey(_source, 16, _width, _alignment, 1, TextLineBreakFlags.None, _flags, _direction, _orientation);
        var options = new TextLayoutOptions(TabStops: _tabs, Overrun: 0, Ellipsis: symbol, BIDIOverride: _bidi, ApplyAlignment: false, PreserveControl: _preserveControl, Styles: _styles, Objects: _inline, PreserveInvalid: _preserveInvalid, GlobalObjectMetrics: true);
        _full.Build(_neutral, key, options); _display.Build(_neutral, key, options with { Overrun = (int)_overrun });
        _generations.Clear(); foreach (var font in _fonts) _generations.Add(font.GetContentGeneration()); _dirty = _contentDirty = false;
    }
    private float DisplayWidth() => _display.LineCount == 0 ? 0 : MathF.Ceiling(_display.Lines[0].Width);
    private Vector2 AlignmentOffset()
    {
        var width = DisplayWidth(); var offset = _width <= 0 ? 0 : _alignment switch { HorizontalAlignment.Center when width <= _width => MathF.Floor((_width - width) * .5f), HorizontalAlignment.Center when _full.LineCount > 0 && _full.Lines[0].ParagraphLevel == 1 => _width - width, HorizontalAlignment.Right => _width - width, _ => 0 };
        return _orientation == TextOrientation.Horizontal ? new(offset, 0) : new(0, offset);
    }
    private void Check() { if (Environment.CurrentManagedThreadId != _thread) throw new InvalidOperationException("TextLine belongs to its creating thread."); ThrowIfDisposed(); }
    private void CheckMutation() { Check(); if (_busy) throw new InvalidOperationException("An active text buffer cannot mutate or dispose."); }
    private static void ValidateEnum<T>(T value) where T : struct, Enum { if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); }
    private static void ValidateObject(TextObjectKey key, Vector2 size, InlineAlignment alignment, float baseline)
    { ArgumentNullException.ThrowIfNull(key); if (!size.IsFinite() || size.X < 0 || size.Y < 0 || !float.IsFinite(baseline)) throw new ArgumentException("Inline geometry must be finite and nonnegative."); if ((uint)alignment > 15) throw new ArgumentOutOfRangeException(nameof(alignment)); }
    private void RequireAppend() { if (_bidi.Length > 0) throw new InvalidOperationException("Clear explicit BIDI ranges before appending source text."); }
    private void ValidateExpansion(int utf16, int scalars) { if (utf16 > 33554432 - _text.Length || scalars > 1048576 - _count) throw new ArgumentException("Text buffer exceeds preparation limits."); }
    private static int Count(string text) { var count = 0; foreach (var rune in text.EnumerateRunes()) count++; return count; }
    private static readonly PropertyDescriptor[] LineProperties =
    [
        new PropertyDescriptor<TextLine, HorizontalAlignment>(nameof(Alignment), n => n.Alignment, (n, v) => n.Alignment = v, _ => HorizontalAlignment.Left),
        new PropertyDescriptor<TextLine, TextDirection>(nameof(Direction), n => n.Direction, (n, v) => n.Direction = v, _ => TextDirection.Auto),
        new PropertyDescriptor<TextLine, TextOrientation>(nameof(Orientation), n => n.Orientation, (n, v) => n.Orientation = v, _ => TextOrientation.Horizontal),
        new PropertyDescriptor<TextLine, string>(nameof(EllipsisChar), n => n.EllipsisChar, (n, v) => n.EllipsisChar = v, _ => "…"),
        new PropertyDescriptor<TextLine, TextJustificationFlags>(nameof(Flags), n => n.Flags, (n, v) => n.Flags = v, _ => TextJustificationFlags.Kashida | TextJustificationFlags.WordBound),
        new PropertyDescriptor<TextLine, bool>(nameof(PreserveControl), n => n.PreserveControl, (n, v) => n.PreserveControl = v, _ => false),
        new PropertyDescriptor<TextLine, bool>(nameof(PreserveInvalid), n => n.PreserveInvalid, (n, v) => n.PreserveInvalid = v, _ => true),
        new PropertyDescriptor<TextLine, TextOverrunBehavior>(nameof(TextOverrunBehavior), n => n.TextOverrunBehavior, (n, v) => n.TextOverrunBehavior = v, _ => Electron2D.TextOverrunBehavior.TrimEllipsis),
        new PropertyDescriptor<TextLine, float>(nameof(Width), n => n.Width, (n, v) => n.Width = v, _ => -1f),
        new PropertyDescriptor<TextLine, int>(nameof(SpanCount), n => n.SpanCount)
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(LineProperties);
    /// <inheritdoc />
    protected override void ValidateMutation() { CheckMutation(); base.ValidateMutation(); }
    /// <inheritdoc />
    protected override void ValidateDisposal() { CheckMutation(); base.ValidateDisposal(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { TextLineBuffers.Remove(_rid); _neutral.Dispose(); _full = null!; _display = null!; _source = string.Empty; _text.Clear(); _text.Capacity = 0; _spans.Clear(); _objects.Clear(); _fonts.Clear(); _reads.Clear(); _styles = []; _inline = []; _tabs = []; _bidi = []; base.Dispose(disposing); }
}

internal static class TextLineBuffers
{
    private static readonly object Gate = new();
    private static readonly Dictionary<RID, WeakReference<TextLine>> Buffers = [];
    private static readonly List<RID> Dead = [];
    private static int _registrations;
    internal static RID Register(TextLine owner)
    {
        lock (Gate)
        {
            if (++_registrations % 64 == 0) { Dead.Clear(); foreach (var entry in Buffers) if (!entry.Value.TryGetTarget(out var target) || target.IsDisposed) Dead.Add(entry.Key); foreach (var key in Dead) Buffers.Remove(key); }
            var id = RID.Allocate(); Buffers.Add(id, new(owner)); return id;
        }
    }
    internal static TextLine Resolve(RID id) { lock (Gate) { if (Buffers.TryGetValue(id, out var weak) && weak.TryGetTarget(out var owner) && !owner.IsDisposed) return owner; throw new InvalidOperationException("Text buffer is absent or disposed."); } }
    internal static void Remove(RID id) { lock (Gate) Buffers.Remove(id); }
}
