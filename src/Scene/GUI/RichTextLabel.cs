using System.Text;
namespace Electron2D;

/// <summary>Displays shaped styled paragraphs, inline objects, tables, links and glyph effects.</summary>
/// <remarks>Text retains untranslated source. Manual stack edits are runtime content; assigning Text replaces them.
/// Effect resources, fonts, textures, argument contexts and metadata remain borrowed. Owned children and callback
/// glyph state cannot be disposed independently. Layout and drawing reject document mutation.</remarks>
public partial class RichTextLabel : Control
{
    /// <summary>ImageUnit domain for rich text.</summary>
    public enum ImageUnit
    {
        /// <summary>image unit em.</summary>
        EM = 2,
        /// <summary>image unit percent.</summary>
        Percent = 1,
        /// <summary>image unit pixel.</summary>
        Pixel = 0,
    }
    /// <summary>ImageUpdateMask domain for rich text.</summary>
    [Flags]
    public enum ImageUpdateMask
    {
        /// <summary>update alignment.</summary>
        Alignment = 8,
        /// <summary>update color.</summary>
        Color = 4,
        /// <summary>update pad.</summary>
        Pad = 32,
        /// <summary>update region.</summary>
        Region = 16,
        /// <summary>update size.</summary>
        Size = 2,
        /// <summary>update texture.</summary>
        Texture = 1,
        /// <summary>update tooltip.</summary>
        Tooltip = 64,
        /// <summary>update width unit.</summary>
        WidthUnit = 128,
    }
    /// <summary>ListType domain for rich text.</summary>
    public enum ListType
    {
        /// <summary>list dots.</summary>
        Dots = 3,
        /// <summary>list letters.</summary>
        Letters = 1,
        /// <summary>list numbers.</summary>
        Numbers = 0,
        /// <summary>list roman.</summary>
        Roman = 2,
    }
    /// <summary>MenuItems domain for rich text.</summary>
    public enum MenuItems
    {
        /// <summary>menu copy.</summary>
        Copy = 0,
        /// <summary>menu max.</summary>
        Max = 2,
        /// <summary>menu select all.</summary>
        SelectAll = 1,
    }
    /// <summary>MetaUnderline domain for rich text.</summary>
    public enum MetaUnderline
    {
        /// <summary>meta underline always.</summary>
        Always = 1,
        /// <summary>meta underline never.</summary>
        Never = 0,
        /// <summary>meta underline on hover.</summary>
        OnHover = 2,
    }
    private enum FontRole { Normal, Bold, Italics, BoldItalics, Mono }
    private enum PartKind { Text, Image, Rule, Table, Dropcap, Marker }
    private sealed record ParagraphStyle(HorizontalAlignment Alignment, TextDirection Direction, string Language, StructuredTextParser Parser, TextJustificationFlags Justification, float[] Tabs);
    private sealed record Format(FontRole Role = FontRole.Normal, Font? Font = null, int Size = 0, Color? Color = null,
        Color? BG = null, Color? FG = null, Color? OutlineColor = null, int OutlineSize = -1, bool Underline = false,
        Color? UnderlineColor = null, bool Strike = false, Color? StrikeColor = null, string Language = "", string Hint = "",
        RichTextMetadata? Meta = null, MetaUnderline MetaUnderline = MetaUnderline.Never, string MetaTooltip = "",
        int Indent = 0, ListType? List = null, bool Capitalize = false, string Bullet = "•", FX? Effects = null, ParagraphStyle? Paragraph = null);
    private sealed class FX
    { internal string Kind = ""; internal RichTextEffect? Effect; internal RichTextEffectEnvironment Env = new(); internal FX? Parent; internal double Started; internal int Start; internal readonly OwnedGlyphState State = new(); internal Vector2 PreviousOffset; internal int PreviousScalar = -1; internal uint Seed = (uint)Random.Shared.Next(); }
    private sealed class OwnedGlyphState : CharFXTransform { internal bool Released; protected override void ValidateDisposal() { if (!Released) throw new InvalidOperationException("Glyph state belongs to its effect block."); base.ValidateDisposal(); } }
    private readonly List<Font> _ownedFonts = [];
    private readonly HashSet<FX> _ownedFX = [];
    private void ReleaseFX() { foreach (var font in _ownedFonts) font.Dispose(); _ownedFonts.Clear(); foreach (var fx in _ownedFX) { fx.State.Released = true; fx.State.Dispose(); } _ownedFX.Clear(); }
    private void PruneOwnedState()
    {
        var effects = new HashSet<FX>(); var fonts = new HashSet<Font>(); void Keep(Format format) { if (format.Font != null) fonts.Add(format.Font); for (var fx = format.Effects; fx != null; fx = fx.Parent) effects.Add(fx); }
        Keep(_format); foreach (var entry in _stack) Keep(entry.Format); foreach (var part in AllParts(_main)) { Keep(part.Format); if (part.DropFont != null) fonts.Add(part.DropFont); }
        _ownedFX.RemoveWhere(fx => { if (effects.Contains(fx)) return false; fx.State.Released = true; fx.State.Dispose(); return true; }); _ownedFonts.RemoveAll(font => { if (fonts.Contains(font)) return false; font.Dispose(); return true; });
    }
    private sealed class Paragraph
    { internal readonly List<Part> Parts = []; internal HorizontalAlignment? Alignment; internal TextDirection? Direction; internal string Language = ""; internal StructuredTextParser Parser; internal TextJustificationFlags Justification = (TextJustificationFlags)163; internal float[] Tabs = []; internal readonly float[] DefaultTabs = new float[1]; internal Format Format = new(); internal string Marker = ""; internal readonly TextLayout MarkerLayout = new(); internal int Start, Count, GlyphStart; internal float Y, Width, Height; internal Vector2 Origin; internal readonly TextLayout Layout = new(); internal string Text = ""; internal Format[] Styles = []; internal int[] Offsets = []; internal readonly List<TextLayoutStyleSpan> Spans = []; internal readonly List<TextLayoutInlineObject> Objects = []; internal readonly Dictionary<int, Part> Inline = []; internal readonly List<TextBIDIRange> Contexts = []; }
    private sealed class Frame
    { internal readonly List<Paragraph> Paragraphs = [new()]; internal Vector2 Position, Size; internal Cell? Cell; }
    private sealed class Part(PartKind kind, Format format)
    { internal readonly PartKind Kind = kind; internal readonly Format Format = format; internal string Text = ""; internal Texture? Texture; internal float Width, Height; internal Color Color = Colors.White; internal InlineAlignment Alignment = InlineAlignment.Center; internal Rect2 Region; internal ImageKey? Key; internal bool Pad, WidthPercent = true, HeightPercent; internal string Tooltip = "", Alt = ""; internal ImageUnit WidthUnit, HeightUnit; internal Table? Table; internal Font? DropFont; internal int DropSize, DropOutline; internal Color DropOutlineColor; internal Rect2 DropMargins; internal readonly TextLayout DropLayout = new(); internal Rect2 Rect; internal int Start, Count, Local; }
    private abstract class ImageKey { internal abstract bool Matches<T>(T key); }
    private sealed class ImageKey<T>(T value) : ImageKey { internal override bool Matches<TValue>(TValue key) => typeof(TValue) == typeof(T) && EqualityComparer<T>.Default.Equals(value, (T)(object?)key!); }
    private sealed class Column { internal bool Expand; internal bool Shrink = true; internal int Ratio = 1; internal string Name = ""; internal float Width, Minimum; }
    private sealed class Cell
    { internal readonly Frame Frame = new(); internal Vector2 Minimum, Maximum; internal Rect2 Padding; internal Color? Odd, Even, Border; internal Rect2 Rect; internal Cell() { Frame.Cell = this; } }
    private sealed class Table(int columns)
    { internal readonly Column[] Columns = Enumerable.Range(0, columns).Select(_ => new Column()).ToArray(); internal readonly List<Cell> Cells = []; internal int AlignRow = -1; internal string Name = ""; internal Vector2 Size; internal float Baseline; }
    private sealed record StackEntry(Format Format, Frame Frame, Table? Table, string Tag, bool Context = false);
    private readonly Frame _main = new(); private Frame _frame; private Table? _table; private Format _format = new(); private readonly List<StackEntry> _stack = [];
    private readonly OwnedScroll _scroll; private readonly OwnedMenu _menu; private readonly Action<TextGlyphDrawing> _glyphRenderer, _glyphForeground;
    private readonly List<Paragraph> _drawParagraphs = []; private readonly List<(Paragraph Paragraph, int Line, float Y)> _lines = [];
    private string _text = "", _language = ""; private bool _bbcode, _selectionEnabled, _contextMenu, _fitContent, _threaded, _scrollActive = true, _scrollFollowing, _scrollFollowingVisible, _shortcutKeys = true, _deselectFocus = true, _dragSelection = true, _metaUnderlined = true, _hintUnderlined = true;
    private bool _hasFX; private bool _dirty = true, _building, _drawing, _disposingRich, _externalStack, _finished = true; private int _tabSize = 4, _progressDelay = 1000, _visibleCharacters = -1; private float _visibleRatio = 1; private double _elapsed, _pendingSince;
    private TextAutowrapMode _autowrap = TextAutowrapMode.WordSmart; private TextLineBreakFlags _trim = TextLineBreakFlags.TrimStartEdgeSpaces | TextLineBreakFlags.TrimEndEdgeSpaces;
    private HorizontalAlignment _horizontal; private VerticalAlignment _vertical; private TextDirection _direction; private TextJustificationFlags _justification = (TextJustificationFlags)163; private TextVisibleCharactersBehavior _visibleBehavior;
    private StructuredTextParser _parser; private string[] _parserOptions = []; private float[] _tabs = []; private RichTextEffect[] _effects = [];
    private int _totalGlyphs, _totalCharacters, _selectionFrom = -1, _selectionTo = -1, _selectionAnchor = -1; private bool _selecting; private RichTextMetadata? _hoverMeta; private string _hoverHint = ""; private Rect2 _textRect; private Vector2 _content; private Paragraph? _currentDraw; private bool _drawOutline, _drawShadow; private Vector2 _drawOrigin;
    private Task? _worker; private Exception? _workerError;
    /// <summary>Creates an empty clipped rich-text control with owned scrolling and context-menu children.</summary>
    public RichTextLabel() : this("") { }
    /// <summary>Creates plain initial text with the default rich-text policies.</summary><param name="text">Nonnull source text.</param>
    public RichTextLabel(string text)
    {
        ArgumentNullException.ThrowIfNull(text); _frame = _main; FocusMode = FocusMode.Accessibility; ClipContents = true;
        _glyphRenderer = DrawRichGlyph; _glyphForeground = DrawRichForeground; _scroll = new(this) { Name = "_rich_scroll" }; _menu = new(this) { Name = "_rich_menu" }; AddChild(_scroll, InternalMode.Front); AddChild(_menu, InternalMode.Front);
        _menu.Hide(); _scroll.ValueChanged += _ => QueueRedraw(); _menu.IDPressed += id => MenuOption((MenuItems)id); _menu.AddItem("Copy", (int)MenuItems.Copy); _menu.AddItem("Select All", (int)MenuItems.SelectAll);
        Text = text;
    }
    private void ApplyParagraph(Paragraph paragraph) { var spec = _format.Paragraph; paragraph.Alignment = spec?.Alignment; paragraph.Direction = spec?.Direction; paragraph.Language = spec?.Language ?? ""; paragraph.Parser = spec?.Parser ?? StructuredTextParser.Default; paragraph.Justification = spec?.Justification ?? (TextJustificationFlags)163; paragraph.Tabs = spec?.Tabs ?? []; }
    private Paragraph Current => _frame.Paragraphs[^1];
    private void CheckRich() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    private void MutableRich() { EnsureMutable(); if (_drawing || _building || _publishing) throw new InvalidOperationException("Active rich-text layout cannot mutate its document."); StopWorker(); }
    private void Changed() { _dirty = true; _finished = false; _pendingSince = _elapsed; QueueRedraw(); if (_fitContent) UpdateMinimumSize(); }
    private void Push(Format format, string tag = "", bool context = false) { MutableRich(); _stack.Add(new(_format, _frame, _table, tag, context)); _format = format; _externalStack = true; Changed(); }
    private void Add(Part part) { MutableRich(); Current.Parts.Add(part); _externalStack = true; Changed(); }
    /// <summary>Clears the runtime stack and document without replacing the source Text property.</summary>
    public void Clear() { MutableRich(); ReleaseFX(); _main.Paragraphs.Clear(); _main.Paragraphs.Add(new()); _frame = _main; _table = null; _stack.Clear(); _format = new(); _elapsed = 0; _externalStack = false; Deselect(); Changed(); }
    /// <summary>Adds raw text, splitting LF/CRLF into independently shaped paragraphs.</summary><param name="text">Nonnull raw text.</param>
    public void AddText(string text)
    { ArgumentNullException.ThrowIfNull(text); MutableRich(); var parts = text.Replace("\r\n", "\n").Split('\n'); for (var i = 0; i < parts.Length; i++) { if (parts[i].Length > 0) Add(new(PartKind.Text, _format) { Text = parts[i] }); if (i + 1 < parts.Length) Newline(); } }
    /// <summary>Starts another independently shaped paragraph.</summary>
    public void Newline() { MutableRich(); var paragraph = new Paragraph { Format = _format }; ApplyParagraph(paragraph); _frame.Paragraphs.Add(paragraph); _externalStack = true; Changed(); }
    /// <summary>Restores the last pushed style/frame context.</summary>
    public void Pop() { MutableRich(); if (_stack.Count == 0) return; var entry = _stack[^1]; if (ReferenceEquals(_frame, entry.Frame) && (_format.Paragraph != entry.Format.Paragraph || _format.Indent != entry.Format.Indent || _format.List != entry.Format.List) && Current.Parts.Count > 0) Newline(); _stack.RemoveAt(_stack.Count - 1); _format = entry.Format; _frame = entry.Frame; _table = entry.Table; Current.Format = _format; ApplyParagraph(Current); Changed(); }
    /// <summary>Restores all pushed contexts.</summary>
    public void PopAll() { MutableRich(); while (_stack.Count > 0) Pop(); }
    /// <summary>Marks an independently restorable push context.</summary>
    public void PushContext() => Push(_format, context: true);
    /// <summary>Restores through the most recent explicit context.</summary>
    public void PopContext() { MutableRich(); while (_stack.Count > 0) { var context = _stack[^1].Context; Pop(); if (context) break; } }
    private sealed class OwnedScroll(RichTextLabel owner) : VScrollBar { protected override void ValidateDisposal() { if (!owner.IsDisposed && !owner._disposingRich) throw new InvalidOperationException("Scrollbar belongs to RichTextLabel."); base.ValidateDisposal(); } }
    private sealed class OwnedMenu(RichTextLabel owner) : PopupMenu { protected override void ValidateDisposal() { if (!owner.IsDisposed && !owner._disposingRich) throw new InvalidOperationException("Menu belongs to RichTextLabel."); base.ValidateDisposal(); } }
    /// <summary>Reports that the current document has completed layout.</summary>
    public event Action? Finished;
    /// <summary>Reports the borrowed typed metadata of a clicked span.</summary>
    public event Action<RichTextMetadata>? MetaClicked;
    /// <summary>Reports entry into a metadata span.</summary>
    public event Action<RichTextMetadata>? MetaHoverStarted;
    /// <summary>Reports departure from a metadata span.</summary>
    public event Action<RichTextMetadata>? MetaHoverEnded;
}
