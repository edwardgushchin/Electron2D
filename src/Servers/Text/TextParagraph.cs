using System.Text;

namespace Electron2D;

/// <summary>Authors, wraps, measures and draws retained multi-font text with inline objects and an independent dropcap.</summary>
/// <remarks>The creating thread owns access. Fonts, object tokens and typed metadata remain borrowed.
/// Full/dropcap identities are stable; reflow retires borrowed line identities. Indexed queries include hidden lines.
/// Prepared queries/drawing reuse storage; authoring, reflow and returned snapshots may allocate.
/// This transient object is reconstructed from authored source rather than serialized.</remarks>
public sealed class TextParagraph : ElectronObject
{
    private TextLine _source = new(), _dropcap = new();
    private TextLayout _geometry = new(), _display = new();
    private readonly int _thread = Environment.CurrentManagedThreadId;
    private readonly List<Font> _fonts = [];
    private readonly List<Font.ReadScope> _reads = [];
    private readonly List<RID> _lines = [];
    private long _sourceVersion = -1, _dropcapVersion = -1;
    private bool _dirty = true, _busy;
    private Rect2 _margins;
    private float _width = -1, _spacing;
    private int _maxLines = -1;
    private HorizontalAlignment _alignment;
    private TextLineBreakFlags _breaks = TextLineBreakFlags.Mandatory | TextLineBreakFlags.WordBound;
    private TextJustificationFlags _justification = TextJustificationFlags.Kashida | TextJustificationFlags.WordBound | TextJustificationFlags.SkipLastLine | TextJustificationFlags.DoNotSkipSingleLine;
    private TextOverrunBehavior _overrun;
    private string _punctuation = string.Empty, _ellipsis = "…";
    private Vector2 _size, _capSize;
    private Vector2[] _origins = [];
    private int VisibleLines => _maxLines < 0 ? _display.LineCount : Math.Min(_maxLines, _display.LineCount);
    private bool Vertical => _source.Orientation == TextOrientation.Vertical;
    private float CapAdvance => Vertical ? _capSize.Y : _capSize.X;

    /// <summary>Creates an empty thread-owned paragraph with mandatory word wrapping and unlimited visible lines.</summary>
    public TextParagraph() { }
    /// <summary>Gets or sets Advance-axis paragraph alignment; initially Left.</summary><value>The current policy.</value>
    public HorizontalAlignment Alignment { get { Check(); return _alignment; } set { CheckMutation(); ValidateEnum(value); if (_alignment == value) return; _alignment = value; _dirty = true; } }
    /// <summary>Gets or sets Mandatory and width-dependent breaks; initially Mandatory and WordBound.</summary><value>The current policy.</value>
    public TextLineBreakFlags BreakFlags { get { Check(); return _breaks; } set { CheckMutation(); if (((int)value & ~239) != 0) throw new ArgumentOutOfRangeException(nameof(value)); if (_breaks == value) return; _breaks = value; _dirty = true; } }
    /// <summary>Gets or sets Replacement punctuation classification for justification; empty uses Unicode punctuation.</summary><value>The current policy.</value>
    public string CustomPunctuation { get { Check(); return _punctuation; } set { CheckMutation(); ArgumentNullException.ThrowIfNull(value); if (_punctuation == value) return; _punctuation = value; _dirty = true; } }
    /// <summary>Gets or sets First Unicode scalar used for ellipsis; empty uses the standard ellipsis.</summary><value>The current policy.</value>
    public string EllipsisChar { get { Check(); return _ellipsis; } set { CheckMutation(); ArgumentNullException.ThrowIfNull(value); value = FirstScalar(value); if (_ellipsis == value) return; _ellipsis = value; _dirty = true; } }
    /// <summary>Gets or sets Line justification policies; initially Kashida, WordBound, SkipLastLine and DoNotSkipSingleLine.</summary><value>The current policy.</value>
    public TextJustificationFlags JustificationFlags { get { Check(); return _justification; } set { CheckMutation(); if (((int)value & ~255) != 0) throw new ArgumentOutOfRangeException(nameof(value)); if (_justification == value) return; _justification = value; _dirty = true; } }
    /// <summary>Gets or sets Finite signed pixels between lines; initially zero.</summary><value>The current policy.</value>
    public float LineSpacing { get { Check(); return _spacing; } set { CheckMutation(); if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_spacing == value) return; _spacing = value; _dirty = true; } }
    /// <summary>Gets or sets Maximum visible lines; any negative value means all lines.</summary><value>The current policy.</value>
    public int MaxLinesVisible { get { Check(); return _maxLines; } set { CheckMutation(); if (_maxLines == value) return; _maxLines = value; _dirty = true; } }
    /// <summary>Gets or sets Display trimming policy; initially NoTrimming.</summary><value>The current policy.</value>
    public TextOverrunBehavior TextOverrunBehavior { get { Check(); return _overrun; } set { CheckMutation(); ValidateEnum(value); if (_overrun == value) return; _overrun = value; _dirty = true; } }
    /// <summary>Gets or sets Finite advance-axis width; nonpositive disables width-dependent wrapping and trimming.</summary><value>The current policy.</value>
    public float Width { get { Check(); return _width; } set { CheckMutation(); if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_width == value) return; _width = value; _dirty = true; } }
    /// <summary>Gets or sets text direction for text and dropcap.</summary><value>Initially TextDirection.Auto.</value>
    public TextDirection Direction { get { Check(); return _source.Direction; } set { CheckMutation(); if (_source.Direction == value) return; _source.Direction = value; _dropcap.Direction = value; _dirty = true; } }
    /// <summary>Gets or sets text orientation for text and dropcap.</summary><value>Initially TextOrientation.Horizontal.</value>
    public TextOrientation Orientation { get { Check(); return _source.Orientation; } set { CheckMutation(); if (_source.Orientation == value) return; _source.Orientation = value; _dropcap.Orientation = value; _dirty = true; } }
    /// <summary>Gets or sets control-box display for text and dropcap.</summary><value>Initially false.</value>
    public bool PreserveControl { get { Check(); return _source.PreserveControl; } set { CheckMutation(); if (_source.PreserveControl == value) return; _source.PreserveControl = value; _dropcap.PreserveControl = value; _dirty = true; } }
    /// <summary>Gets or sets invalid-glyph display for text and dropcap.</summary><value>Initially true.</value>
    public bool PreserveInvalid { get { Check(); return _source.PreserveInvalid; } set { CheckMutation(); if (_source.PreserveInvalid == value) return; _source.PreserveInvalid = value; _dropcap.PreserveInvalid = value; _dirty = true; } }

    /// <summary>Appends a copied text segment with a borrowed font.</summary>
    /// <param name="text">Copied text; empty succeeds without a segment.</param><param name="font">Live borrowed font.</param>
    /// <param name="fontSize">Positive logical size.</param><param name="language">Language tag, or empty.</param><returns>True on successful authoring.</returns>
    public bool AddString(string text, Font font, int fontSize, string language = "")
    { CheckMutation(); var result = _source.AddString(text, font, fontSize, language); ContentChanged(); return result; }
    /// <summary>Appends a segment retaining exact typed metadata.</summary><typeparam name="T">Borrowed metadata type.</typeparam>
    /// <param name="text">Copied text.</param><param name="font">Borrowed font.</param><param name="fontSize">Positive logical size.</param>
    /// <param name="language">Language tag.</param><param name="metadata">Borrowed value.</param><returns>True on success.</returns>
    public bool AddString<T>(string text, Font font, int fontSize, string language, T metadata)
    { CheckMutation(); var result = _source.AddString(text, font, fontSize, language, metadata); ContentChanged(); return result; }
    /// <summary>Gets authored text-span count, excluding inline objects and empty additions.</summary><value>Nonnegative count.</value>
    public int SpanCount { get { Check(); return _source.SpanCount; } }
    /// <summary>Reads metadata under its exact original type.</summary><typeparam name="T">Original type.</typeparam>
    /// <param name="spanIndex">Text span index.</param><param name="metadata">Borrowed result or default.</param><returns>Whether the type matches.</returns>
    public bool TryGetSpanMetadata<T>(int spanIndex, out T metadata) { Check(); return _source.TryGetSpanMetadata(spanIndex, out metadata); }
    /// <summary>Appends one inline object with reference identity and one advance.</summary>
    /// <param name="key">Unique borrowed token.</param><param name="size">Finite nonnegative dimensions.</param><param name="inlineAlign">Two-axis alignment.</param>
    /// <param name="length">Positive replacement-scalar count.</param><param name="baseline">Finite signed baseline.</param><returns>False for a duplicate token.</returns>
    public bool AddObject(TextObjectKey key, Vector2 size, InlineAlignment inlineAlign = InlineAlignment.Center, int length = 1, float baseline = 0)
    { CheckMutation(); var result = _source.AddObject(key, size, inlineAlign, length, baseline); _dirty |= result; return result; }
    /// <summary>Changes existing object geometry without changing its scalar range.</summary><param name="key">Borrowed token.</param>
    /// <param name="size">Finite nonnegative size.</param><param name="inlineAlign">Two-axis alignment.</param><param name="baseline">Finite signed baseline.</param><returns>False for a missing token.</returns>
    public bool ResizeObject(TextObjectKey key, Vector2 size, InlineAlignment inlineAlign = InlineAlignment.Center, float baseline = 0)
    { CheckMutation(); var result = _source.ResizeObject(key, size, inlineAlign, baseline); _dirty |= result; return result; }
    /// <summary>Tests authored inline identity, including hidden lines.</summary><param name="key">Borrowed token.</param><returns>Whether present.</returns>
    public bool HasObject(TextObjectKey key) { Check(); return _source.HasObject(key); }
    /// <summary>Copies explicit contiguous BIDI ranges over the current scalar buffer; empty clears them.</summary><param name="ranges">Full nonoverlapping ranges.</param>
    public void SetBIDIOverride(ReadOnlySpan<TextBIDIRange> ranges) { CheckMutation(); _source.SetBIDIOverride(ranges); _dirty = true; }
    /// <summary>Copies positive repeating tab intervals.</summary><param name="tabStops">Finite positive intervals; empty restores default tabs.</param>
    public void TabAlign(ReadOnlySpan<float> tabStops) { CheckMutation(); _source.TabAlign(tabStops); _dirty = true; }
    /// <summary>Replaces the independent dropcap text without changing its RID.</summary>
    /// <param name="text">Copied text, bounded like ordinary spans.</param><param name="font">Live borrowed font.</param><param name="fontSize">Positive logical size.</param>
    /// <param name="dropcapMargins">Finite signed leading and trailing margins.</param><param name="language">Language tag.</param><returns>True after successful authoring.</returns>
    public bool SetDropcap(string text, Font font, int fontSize, Rect2 dropcapMargins = default, string language = "")
    {
        CheckMutation(); ArgumentNullException.ThrowIfNull(text); ArgumentNullException.ThrowIfNull(font); ArgumentNullException.ThrowIfNull(language);
        Font.ValidateSize(fontSize); if (font.IsDisposed) throw new ObjectDisposedException(nameof(font));
        if (!dropcapMargins.Position.IsFinite() || !dropcapMargins.Size.IsFinite()) throw new ArgumentException("Dropcap margins must be finite.", nameof(dropcapMargins));
        if (text.Length > 2097152) throw new ArgumentException("Dropcap exceeds preparation limits.", nameof(text));
        var count = 0; foreach (var rune in text.EnumerateRunes()) if (++count > 1048576) throw new ArgumentException("Dropcap exceeds preparation limits.", nameof(text));
        _dirty = true; _dropcap.Clear(); _dropcap.AddString(text, font, fontSize, language); _margins = dropcapMargins; ContentChanged(); return true;
    }
    /// <summary>Clears dropcap content and margins, preserving its RID and paragraph settings.</summary>
    public void ClearDropcap() { CheckMutation(); _dropcap.Clear(); _margins = default; ContentChanged(); }
    /// <summary>Clears authored text, objects, BIDI and dropcap content; policies, tabs, margins and full/dropcap RIDs remain.</summary>
    public void Clear()
    { CheckMutation(); _source.Clear(); _dropcap.Clear(); _geometry.ReleaseContent(); _display.ReleaseContent(); RetireLines(); ContentChanged(); }
    /// <summary>Copies source and every paragraph policy into independent owned buffers.</summary><returns>A caller-owned duplicate sharing borrowed fonts/tokens/metadata.</returns>
    public TextParagraph Duplicate()
    {
        Check(); var copy = new TextParagraph(); copy._source.Dispose(); copy._dropcap.Dispose();
        copy._source = _source.Duplicate(); copy._dropcap = _dropcap.Duplicate(); copy._margins = _margins;
        copy._width = _width; copy._spacing = _spacing; copy._maxLines = _maxLines; copy._alignment = _alignment;
        copy._breaks = _breaks; copy._justification = _justification; copy._overrun = _overrun; copy._punctuation = _punctuation; copy._ellipsis = _ellipsis;
        copy.ContentChanged(); return copy;
    }
    /// <summary>Returns the stable borrowed full-string buffer identity.</summary><returns>Valid until disposal.</returns>
    public RID GetRID() { Check(); return _source.GetRID(); }
    /// <summary>Returns the stable borrowed independent dropcap identity.</summary><returns>Valid even when empty, until disposal.</returns>
    public RID GetDropcapRID() { Check(); return _dropcap.GetRID(); }
    /// <summary>Returns a borrowed shaped-line sub-buffer identity.</summary><param name="line">Index including hidden lines.</param><returns>Valid until reflow or disposal.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The line does not exist.</exception>
    public RID GetLineRID(int line) { EnsureLine(line); return _lines[line]; }
    /// <summary>Returns the complete half-open Unicode scalar range.</summary><returns>Zero to authored scalar count.</returns>
    public Vector2i GetRange() { Check(); return new(0, _source.ScalarCount); }
    /// <summary>Returns a line's half-open scalar range, including mandatory separators unless trimmed.</summary><param name="line">Index including hidden lines.</param><returns>Scalar start and end.</returns>
    public Vector2i GetLineRange(int line) { EnsureLine(line); var value = _display.Lines[line]; return new(value.Start, value.RangeEnd < 0 ? value.End : value.RangeEnd); }
    /// <summary>Returns total shaped line count independently of visibility.</summary><returns>One zero-sized line for empty text.</returns>
    public int GetLineCount() { Ensure(); return _display.LineCount; }
    /// <summary>Returns inferred full-string writing direction.</summary><returns>LTR or RTL.</returns>
    public TextDirection GetInferredDirection() { Check(); return _source.GetInferredDirection(); }
    /// <summary>Returns the full buffer size without line breaks, visibility or dropcap.</summary><returns>Ceiled dimensions.</returns>
    public Vector2 GetNonWrappedSize() { Check(); return _source.GetSize(); }
    /// <summary>Returns the visible paragraph bounding size, including spacing and dropcap reservation.</summary><returns>Finite dimensions; signed spacing/margins may reduce them.</returns>
    public Vector2 GetSize() { Ensure(); return _size; }
    /// <summary>Returns one display line's ceiled bounding size.</summary><param name="line">Index including hidden lines.</param><returns>Ceiled dimensions with the selected orientation.</returns>
    public Vector2 GetLineSize(int line) { EnsureLine(line); return LineSize(_display.Lines[line]); }
    /// <summary>Returns ceiled display advance width, or height in vertical orientation.</summary><param name="line">Index including hidden lines.</param><returns>Advance pixels.</returns>
    public float GetLineWidth(int line) { EnsureLine(line); return MathF.Ceiling(_display.Lines[line].Width); }
    /// <summary>Returns fractional line ascent.</summary><param name="line">Index including hidden lines.</param><returns>Pixels before the baseline.</returns>
    public float GetLineAscent(int line) { EnsureLine(line); return _geometry.Lines[line].Ascent; }
    /// <summary>Returns fractional line descent.</summary><param name="line">Index including hidden lines.</param><returns>Pixels after the baseline.</returns>
    public float GetLineDescent(int line) { EnsureLine(line); return _geometry.Lines[line].Descent; }
    /// <summary>Returns maximum underline offset across a line's authored fonts.</summary><param name="line">Index including hidden lines.</param><returns>Fractional pixels.</returns>
    public float GetLineUnderlinePosition(int line) { EnsureLine(line); var value = _geometry.Lines[line]; return _source.Underline(value.Start, value.End, false); }
    /// <summary>Returns maximum underline thickness across a line's authored fonts.</summary><param name="line">Index including hidden lines.</param><returns>Fractional pixels.</returns>
    public float GetLineUnderlineThickness(int line) { EnsureLine(line); var value = _geometry.Lines[line]; return _source.Underline(value.Start, value.End, true); }
    /// <summary>Returns the dropcap bounding size plus leading and trailing margins.</summary><returns>Finite signed dimensions.</returns>
    public Vector2 GetDropcapSize() { Ensure(); return _capSize; }
    /// <summary>Returns the number of complete line extents covered by dropcap reservation.</summary><returns>Zero when no line is completely covered.</returns>
    public int GetDropcapLines() { Ensure(); return _display.DropcapLines; }
    /// <summary>Returns borrowed inline tokens in one complete line as a caller-owned snapshot.</summary><param name="line">Index including hidden lines.</param><returns>Tokens in authored order, including trimmed objects.</returns>
    public TextObjectKey[] GetLineObjects(int line)
    {
        EnsureLine(line); var value = _geometry.Lines[line]; var count = 0;
        for (var i = 0; i < _source.ObjectCount; i++) { var item = _source.ObjectAt(i); if (item.At >= value.Start && item.At < value.End) count++; }
        var keys = new TextObjectKey[count]; count = 0;
        for (var i = 0; i < _source.ObjectCount; i++) { var item = _source.ObjectAt(i); if (item.At >= value.Start && item.At < value.End) keys[count++] = item.Key; }
        return keys;
    }
    /// <summary>Returns full inline geometry in paragraph-top-left coordinates, including alignment and preceding lines.</summary>
    /// <param name="line">Index including hidden lines.</param><param name="key">Borrowed token.</param><returns>Zero for an object absent from this line.</returns>
    public Rect2 GetLineObjectRect(int line, TextObjectKey key)
    {
        ArgumentNullException.ThrowIfNull(key); EnsureLine(line); var value = _geometry.Lines[line];
        for (var i = 0; i < _source.ObjectCount; i++)
        {
            var item = _source.ObjectAt(i); if (!ReferenceEquals(item.Key, key) || item.At < value.Start || item.At >= value.End) continue;
            var rect = _geometry.InlineRect(i, item.Size); rect.Position += LineOrigin(line) - Axis(0, value.CrossOffset); return rect;
        }
        return default;
    }
    /// <summary>Returns a full-buffer scalar caret near line-local advance and paragraph cross coordinates.</summary>
    /// <param name="coords">Finite coordinates; advance ignores paragraph alignment/dropcap offset.</param><returns>Scalar position; cross gaps/beyond the paragraph return the range end.</returns>
    public int HitTest(Vector2 coords)
    {
        if (!coords.IsFinite()) throw new ArgumentOutOfRangeException(nameof(coords)); Ensure();
        var cross = Vertical ? coords.X : coords.Y; if (cross < 0) return 0; float offset = 0;
        for (var i = 0; i < _geometry.LineCount; i++)
        {
            var size = LineSize(_geometry.Lines[i]); var extent = Vertical ? size.X : size.Y;
            if (cross >= offset && cross <= offset + extent) return _geometry.HitLine(i, Vertical ? coords.Y : coords.X);
            offset += extent + _spacing;
        }
        return _source.ScalarCount;
    }
    /// <summary>Draws visible lines and dropcap from the paragraph top-left.</summary><param name="canvas">Recording canvas.</param><param name="position">Finite origin.</param>
    /// <param name="color">Text modulation, or white.</param><param name="dropcapColor">Dropcap modulation, or white.</param><param name="oversampling">Raster scale, nonpositive automatic.</param>
    public void Draw(CanvasItem canvas, Vector2 position, Color? color = null, Color? dropcapColor = null, float oversampling = 0) => DrawCore(canvas, position, -1, 0, color ?? Colors.White, dropcapColor ?? Colors.White, oversampling, false, false);
    /// <summary>Draws visible line and dropcap outlines.</summary><param name="canvas">Recording canvas.</param><param name="position">Finite origin.</param><param name="outlineSize">Nonpositive uses the unexpanded outline pass.</param>
    /// <param name="color">Text modulation.</param><param name="dropcapColor">Dropcap modulation.</param><param name="oversampling">Raster scale, nonpositive automatic.</param>
    public void DrawOutline(CanvasItem canvas, Vector2 position, int outlineSize = 1, Color? color = null, Color? dropcapColor = null, float oversampling = 0) => DrawCore(canvas, position, -1, Math.Max(0, outlineSize), color ?? Colors.White, dropcapColor ?? Colors.White, oversampling, true, false);
    /// <summary>Draws an indexed line from its own top-left, without paragraph offsets or visibility restriction.</summary><param name="canvas">Recording canvas.</param><param name="position">Line-local origin.</param><param name="line">Any valid index.</param><param name="color">Modulation.</param><param name="oversampling">Raster scale.</param>
    public void DrawLine(CanvasItem canvas, Vector2 position, int line, Color? color = null, float oversampling = 0) { if (line < 0) throw new ArgumentOutOfRangeException(nameof(line)); DrawCore(canvas, position, line, 0, color ?? Colors.White, Colors.White, oversampling, false, false); }
    /// <summary>Draws one line outline from its own top-left.</summary><param name="canvas">Recording canvas.</param><param name="position">Line-local origin.</param><param name="line">Any valid index.</param><param name="outlineSize">Outline pixels.</param><param name="color">Modulation.</param><param name="oversampling">Raster scale.</param>
    public void DrawLineOutline(CanvasItem canvas, Vector2 position, int line, int outlineSize = 1, Color? color = null, float oversampling = 0) { if (line < 0) throw new ArgumentOutOfRangeException(nameof(line)); DrawCore(canvas, position, line, Math.Max(0, outlineSize), color ?? Colors.White, Colors.White, oversampling, true, false); }
    /// <summary>Draws the independent dropcap, including its leading margins.</summary><param name="canvas">Recording canvas.</param><param name="position">Paragraph top-left.</param><param name="color">Modulation.</param><param name="oversampling">Raster scale.</param>
    public void DrawDropcap(CanvasItem canvas, Vector2 position, Color? color = null, float oversampling = 0) => DrawCore(canvas, position, -1, 0, Colors.White, color ?? Colors.White, oversampling, false, true);
    /// <summary>Draws only the dropcap outline.</summary><param name="canvas">Recording canvas.</param><param name="position">Paragraph top-left.</param><param name="outlineSize">Outline pixels.</param><param name="color">Modulation.</param><param name="oversampling">Raster scale.</param>
    public void DrawDropcapOutline(CanvasItem canvas, Vector2 position, int outlineSize = 1, Color? color = null, float oversampling = 0) => DrawCore(canvas, position, -1, Math.Max(0, outlineSize), Colors.White, color ?? Colors.White, oversampling, true, true);

    private void DrawCore(CanvasItem canvas, Vector2 position, int line, int outline, Color color, Color capColor, float oversampling, bool outlinePass, bool capOnly)
    {
        Check(); Font.ValidateDraw(canvas, position, color, oversampling); Font.ValidateDraw(canvas, position, capColor, oversampling);
        if (line < -1) throw new ArgumentOutOfRangeException(nameof(line));
        _busy = true;
        try
        {
            BeginPreparedReads(); if (line >= 0 && line >= _display.LineCount) throw new ArgumentOutOfRangeException(nameof(line));
            if (line < 0 && CapAdvance > 0)
            {
                var offset = _dropcap.GetInferredDirection() == TextDirection.RTL ? (_width > 0 ? _width : (Vertical ? _size.Y : _size.X)) - CapAdvance : 0;
                var origin = position + Axis(offset, 0) + _margins.Position;
                if (outlinePass) _dropcap.DrawOutline(canvas, origin, outline, capColor, oversampling); else _dropcap.Draw(canvas, origin, capColor, oversampling);
            }
            if (capOnly) return;
            var first = line < 0 ? 0 : line; var end = line < 0 ? VisibleLines : line + 1;
            for (var i = first; i < end; i++)
            {
                var owner = TextBuffers.Resolve(_lines[i]); if (!ReferenceEquals(owner.Owner, this) || owner.Line != i) throw new InvalidOperationException("Invalid paragraph line owner.");
                var origin = position + (line < 0 ? LineOrigin(i) : Vector2.Zero) + Axis(0, _display.Lines[i].Ascent);
                if (!origin.IsFinite()) throw new OverflowException("Paragraph draw origin exceeds finite geometry.");
                var clip = line < 0 && _width > 0; var clipStart = clip ? Math.Max(0, -(Vertical ? LineOrigin(i).Y : LineOrigin(i).X)) : 0;
                _display.Draw(canvas, origin, color, outline, oversampling, firstLine: i, maxLines: 1, outlinePass: outlinePass, clipToWidth: clip, clipStart: clipStart, ignoreLineLimit: true, advanceClipWidth: clip ? _width - (CapAdvance > 0 && i <= _display.DropcapLines ? CapAdvance : 0) : null);
            }
        }
        finally { try { EndReads(); } finally { _busy = false; } }
    }
    private void Ensure()
    {
        Check(); _busy = true;
        try { BeginPreparedReads(); }
        finally { try { EndReads(); } finally { _busy = false; } }
    }
    private void BeginPreparedReads()
    {
        for (var attempt = 0; attempt < Font.MaximumReadAttempts; attempt++)
        {
            BeginReads(); Prepare(); if (ReadsCurrent()) return;
            _dirty = true; EndReads();
        }
        throw Font.UnsettledRead();
    }
    private bool ReadsCurrent() { foreach (var read in _reads) if (!read.IsCurrent) return false; return true; }
    private void Prepare()
    {
        var source = _source.PreparedRevision; var cap = _dropcap.PreparedRevision;
        if (!_dirty && source == _sourceVersion && cap == _dropcapVersion) return;
        _capSize = _dropcap.GetSize() + _margins.Position + _margins.Size;
        if (!_capSize.IsFinite()) throw new OverflowException("Dropcap reservation exceeds finite geometry.");
        var capCross = Vertical ? _capSize.X : _capSize.Y;
        var key = new TextLayoutKey(string.Empty, 16, _width, _alignment, _maxLines, _breaks, _justification, _source.Direction, _source.Orientation, true);
        var options = new TextLayoutOptions(LineSpacing: _spacing, Overrun: (int)_overrun, Ellipsis: _ellipsis.Length == 0 ? "…" : _ellipsis,
            ApplyAlignment: false, DropcapWidth: CapAdvance, DropcapHeight: capCross, ParagraphBuffers: true, CustomPunctuation: _punctuation);
        _source.BuildBuffer(_geometry, key, options with { Overrun = 0 }); _source.BuildBuffer(_display, key, options);
        RetireLines(); _lines.EnsureCapacity(_display.LineCount);
        for (var i = 0; i < _display.LineCount; i++) _lines.Add(TextBuffers.Register(this, i));
        var advance = 0f; var cross = 0f;
        for (var i = 0; i < VisibleLines; i++)
        {
            var size = LineSize(_display.Lines[i]); advance = Math.Max(advance, (Vertical ? size.Y : size.X) + (CapAdvance > 0 && i <= _display.DropcapLines ? CapAdvance : 0));
            cross += Vertical ? size.X : size.Y; if (i + 1 < VisibleLines) cross += _spacing;
        }
        if (CapAdvance > 0) cross = Math.Max(cross, capCross);
        _size = Axis(advance, cross); if (!_size.IsFinite()) throw new OverflowException("Paragraph exceeds finite geometry.");
        if (_origins.Length < _display.LineCount) Array.Resize(ref _origins, _display.LineCount);
        float drawCross = 0; var capLTR = _dropcap.GetInferredDirection() == TextDirection.LTR;
        for (var i = 0; i < _display.LineCount; i++) { _origins[i] = CalculateLineOrigin(i, drawCross, capLTR); if (!_origins[i].IsFinite()) throw new OverflowException("Paragraph line origin exceeds finite geometry."); drawCross += _display.Lines[i].Height + _spacing; }
        _sourceVersion = source; _dropcapVersion = cap; _dirty = false;
    }
    private Vector2 LineSize(TextLayoutLine line) => Axis(MathF.Ceiling(line.Width), MathF.Ceiling(line.Height));
    private Vector2 Axis(float advance, float cross) => Vertical ? new(cross, advance) : new(advance, cross);
    private Vector2 LineOrigin(int index) => _origins[index];
    private Vector2 CalculateLineOrigin(int index, float cross, bool capLTR)
    {
        var cap = CapAdvance > 0 && index <= _display.DropcapLines ? CapAdvance : 0;
        var available = (_width > 0 ? _width : (Vertical ? _size.Y : _size.X)) - cap; var width = MathF.Ceiling(_display.Lines[index].Width);
        var rtl = _display.Lines[index].ParagraphLevel == 1;
        var align = available <= 0 ? 0 : _alignment switch
        { HorizontalAlignment.Center when width <= available => MathF.Floor((available - width) * .5f), HorizontalAlignment.Center when rtl => available - width, HorizontalAlignment.Right => available - width, HorizontalAlignment.Fill when rtl => available - width, _ => 0 };
        return Axis(align + (capLTR ? cap : 0), cross);
    }
    private void ContentChanged() { _dirty = true; _fonts.Clear(); _source.CollectFonts(_fonts); _dropcap.CollectFonts(_fonts); _fonts.Sort(static (a, b) => a.InstanceID.CompareTo(b.InstanceID)); }
    private void BeginReads() { foreach (var font in _fonts) _reads.Add(font.BeginRead()); }
    private void EndReads() { for (var i = _reads.Count - 1; i >= 0; i--) _reads[i].Dispose(); _reads.Clear(); }
    private void RetireLines() { foreach (var rid in _lines) TextBuffers.Remove(rid); _lines.Clear(); }
    private void EnsureLine(int line) { Ensure(); if ((uint)line >= (uint)_display.LineCount) throw new ArgumentOutOfRangeException(nameof(line)); }
    private void Check() { if (Environment.CurrentManagedThreadId != _thread) throw new InvalidOperationException("TextParagraph belongs to its creating thread."); ThrowIfDisposed(); if (_busy) throw new InvalidOperationException("An active paragraph cannot reenter, mutate or dispose."); }
    private void CheckMutation() => Check();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(ParagraphProperties);
    /// <inheritdoc />
    protected override void ValidateMutation() { CheckMutation(); base.ValidateMutation(); }
    /// <inheritdoc />
    protected override void ValidateDisposal() { CheckMutation(); base.ValidateDisposal(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { RetireLines(); _source.Dispose(); _dropcap.Dispose(); _geometry = null!; _display = null!; _fonts.Clear(); _reads.Clear(); base.Dispose(disposing); }
    private static readonly PropertyDescriptor[] ParagraphProperties =
    [
        new PropertyDescriptor<TextParagraph, HorizontalAlignment>(nameof(Alignment), p => p.Alignment, (p, v) => p.Alignment = v, _ => HorizontalAlignment.Left),
        new PropertyDescriptor<TextParagraph, TextLineBreakFlags>(nameof(BreakFlags), p => p.BreakFlags, (p, v) => p.BreakFlags = v, _ => TextLineBreakFlags.Mandatory | TextLineBreakFlags.WordBound),
        new PropertyDescriptor<TextParagraph, string>(nameof(CustomPunctuation), p => p.CustomPunctuation, (p, v) => p.CustomPunctuation = v, _ => string.Empty),
        new PropertyDescriptor<TextParagraph, string>(nameof(EllipsisChar), p => p.EllipsisChar, (p, v) => p.EllipsisChar = v, _ => "…"),
        new PropertyDescriptor<TextParagraph, TextJustificationFlags>(nameof(JustificationFlags), p => p.JustificationFlags, (p, v) => p.JustificationFlags = v, _ => TextJustificationFlags.Kashida | TextJustificationFlags.WordBound | TextJustificationFlags.SkipLastLine | TextJustificationFlags.DoNotSkipSingleLine),
        new PropertyDescriptor<TextParagraph, float>(nameof(LineSpacing), p => p.LineSpacing, (p, v) => p.LineSpacing = v, _ => 0f),
        new PropertyDescriptor<TextParagraph, int>(nameof(MaxLinesVisible), p => p.MaxLinesVisible, (p, v) => p.MaxLinesVisible = v, _ => -1),
        new PropertyDescriptor<TextParagraph, TextOverrunBehavior>(nameof(TextOverrunBehavior), p => p.TextOverrunBehavior, (p, v) => p.TextOverrunBehavior = v, _ => Electron2D.TextOverrunBehavior.NoTrimming),
        new PropertyDescriptor<TextParagraph, float>(nameof(Width), p => p.Width, (p, v) => p.Width = v, _ => -1f),
        new PropertyDescriptor<TextParagraph, TextDirection>(nameof(Direction), p => p.Direction, (p, v) => p.Direction = v, _ => TextDirection.Auto),
        new PropertyDescriptor<TextParagraph, TextOrientation>(nameof(Orientation), p => p.Orientation, (p, v) => p.Orientation = v, _ => TextOrientation.Horizontal),
        new PropertyDescriptor<TextParagraph, bool>(nameof(PreserveControl), p => p.PreserveControl, (p, v) => p.PreserveControl = v, _ => false),
        new PropertyDescriptor<TextParagraph, bool>(nameof(PreserveInvalid), p => p.PreserveInvalid, (p, v) => p.PreserveInvalid = v, _ => true),
        new PropertyDescriptor<TextParagraph, int>(nameof(SpanCount), p => p.SpanCount)
    ];
    private static void ValidateEnum<T>(T value) where T : struct, Enum { if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); }
    private static string FirstScalar(string value) { var runes = value.EnumerateRunes(); return runes.MoveNext() ? runes.Current.ToString() : string.Empty; }
}
