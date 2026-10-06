namespace Electron2D;

public partial class TextEdit
{
    private sealed class Gutter
    { internal string Name = ""; internal GutterType Type; internal int Width; internal bool Draw = true, Clickable, Overwritable; internal Action<CanvasItem, int, int, Rect2>? CustomDraw; }
    private sealed class GutterCell
    { internal string Text = ""; internal Texture? Icon; internal Color Color = Colors.White; internal object? Metadata; internal bool Clickable; internal readonly TextLayout Layout = new(); internal GutterCell Copy() => new() { Text = Text, Icon = Icon, Color = Color, Metadata = Metadata, Clickable = Clickable }; }
    private Gutter AtGutter(int gutter) { CheckTextEdit(); if ((uint)gutter >= _gutters.Count) throw new ArgumentOutOfRangeException(nameof(gutter)); return _gutters[gutter]; }
    private GutterCell AtGutterCell(int line, int gutter) { AtGutter(gutter); return AtLine(line).Gutters[gutter]; }
    /// <summary>Inserts a gutter and an empty cell on every line.</summary><param name="at">Insertion index or -1 for the end.</param>
    public void AddGutter(int at = -1) { EnsureTextMutable(); if (at == -1) at = _gutters.Count; if ((uint)at > _gutters.Count) throw new ArgumentOutOfRangeException(nameof(at)); _gutters.Insert(at, new()); foreach (var line in _lines) line.Gutters.Insert(at, new()); InvalidateTextLayout(); GutterAdded?.Invoke(); }
    /// <summary>Removes a gutter and its line cells.</summary><param name="gutter">Existing index.</param>
    public void RemoveGutter(int gutter) { EnsureTextMutable(); AtGutter(gutter); _gutters.RemoveAt(gutter); foreach (var line in _lines) line.Gutters.RemoveAt(gutter); InvalidateTextLayout(); GutterRemoved?.Invoke(); }
    /// <summary>Returns the gutter count.</summary><returns>Current count.</returns>
    public int GetGutterCount() { CheckTextEdit(); return _gutters.Count; }
    /// <summary>Sets a gutter's source name.</summary><param name="gutter">Existing index.</param><param name="name">Nonnull name.</param>
    public void SetGutterName(int gutter, string name) { EnsureTextMutable(); ArgumentNullException.ThrowIfNull(name); AtGutter(gutter).Name = name; }
    /// <summary>Returns a gutter's source name.</summary><param name="gutter">Existing index.</param><returns>Configured name.</returns>
    public string GetGutterName(int gutter) => AtGutter(gutter).Name;
    /// <summary>Sets a gutter's rendering kind.</summary><param name="gutter">Existing index.</param><param name="type">Defined rendering kind.</param>
    public void SetGutterType(int gutter, GutterType type) { EnsureTextMutable(); if (!Enum.IsDefined(type)) throw new ArgumentOutOfRangeException(nameof(type)); AtGutter(gutter).Type = type; InvalidateTextLayout(); }
    /// <summary>Returns the gutter's rendering kind.</summary><param name="gutter">Existing index.</param><returns>Configured type.</returns>
    public GutterType GetGutterType(int gutter) => AtGutter(gutter).Type;
    /// <summary>Sets the nonnegative pixel width.</summary><param name="gutter">Existing index.</param><param name="width">Nonnegative width.</param>
    public void SetGutterWidth(int gutter, int width) { EnsureTextMutable(); if (width < 0) throw new ArgumentOutOfRangeException(nameof(width)); AtGutter(gutter).Width = width; InvalidateTextLayout(); }
    /// <summary>Returns the configured pixel width.</summary><param name="gutter">Existing index.</param><returns>Width in pixels.</returns>
    public int GetGutterWidth(int gutter) => AtGutter(gutter).Width;
    /// <summary>Gets the sum of currently drawn gutter widths.</summary><returns>Pixel width.</returns>
    public int GetTotalGutterWidth() { CheckTextEdit(); var width = 0; foreach (var gutter in _gutters) if (gutter.Draw) width = checked(width + gutter.Width); return width; }
    /// <summary>Sets visibility of a gutter.</summary><param name="gutter">Existing index.</param><param name="draw">Whether drawn.</param>
    public void SetGutterDraw(int gutter, bool draw) { EnsureTextMutable(); AtGutter(gutter).Draw = draw; InvalidateTextLayout(); }
    /// <summary>Reports gutter visibility.</summary><param name="gutter">Existing index.</param><returns>Whether drawn.</returns>
    public bool IsGutterDrawn(int gutter) => AtGutter(gutter).Draw;
    /// <summary>Sets whether every gutter cell can publish a click.</summary><param name="gutter">Existing index.</param><param name="clickable">Click policy.</param>
    public void SetGutterClickable(int gutter, bool clickable) { EnsureTextMutable(); AtGutter(gutter).Clickable = clickable; }
    /// <summary>Reports the gutter's shared click policy.</summary><param name="gutter">Existing index.</param><returns>Configured policy.</returns>
    public bool IsGutterClickable(int gutter) => AtGutter(gutter).Clickable;
    /// <summary>Sets whether line-merging may transfer this gutter's content.</summary><param name="gutter">Existing index.</param><param name="overwritable">Transfer policy.</param>
    public void SetGutterOverwritable(int gutter, bool overwritable) { EnsureTextMutable(); AtGutter(gutter).Overwritable = overwritable; }
    /// <summary>Reports the line-merge transfer policy.</summary><param name="gutter">Existing index.</param><returns>Configured policy.</returns>
    public bool IsGutterOverwritable(int gutter) => AtGutter(gutter).Overwritable;
    /// <summary>Sets a typed custom gutter draw callback invoked during this editor's canvas recording.</summary><param name="column">Existing gutter index.</param><param name="drawCallback">Callback borrowing canvas, logical line, gutter and local rectangle; null clears it.</param>
    public void SetGutterCustomDraw(int column, Action<CanvasItem, int, int, Rect2>? drawCallback) { EnsureTextMutable(); AtGutter(column).CustomDraw = drawCallback; QueueRedraw(); }
    /// <summary>Sets a gutter cell's source text.</summary><param name="line">Existing line.</param><param name="gutter">Existing gutter.</param><param name="text">Nonnull text.</param>
    public void SetLineGutterText(int line, int gutter, string text) { EnsureTextMutable(); ArgumentNullException.ThrowIfNull(text); AtGutterCell(line, gutter).Text = text; InvalidateTextLayout(); }
    /// <summary>Gets a cell's source text.</summary><param name="line">Existing line.</param><param name="gutter">Existing gutter.</param><returns>Configured text.</returns>
    public string GetLineGutterText(int line, int gutter) => AtGutterCell(line, gutter).Text;
    /// <summary>Sets a borrowed gutter cell texture.</summary><param name="line">Existing line.</param><param name="gutter">Existing gutter.</param><param name="icon">Live texture or null.</param>
    public void SetLineGutterIcon(int line, int gutter, Texture? icon) { EnsureTextMutable(); if (icon?.IsDisposed == true) throw new ObjectDisposedException(nameof(icon)); AtGutterCell(line, gutter).Icon = icon; InvalidateTextLayout(); }
    /// <summary>Gets a live borrowed gutter icon.</summary><param name="line">Existing line.</param><param name="gutter">Existing gutter.</param><returns>Live icon or null.</returns>
    public Texture? GetLineGutterIcon(int line, int gutter) => AtGutterCell(line, gutter).Icon is { IsDisposed: false } icon ? icon : null;
    /// <summary>Sets the gutter text/icon color.</summary><param name="line">Existing line.</param><param name="gutter">Existing gutter.</param><param name="color">Finite color.</param>
    public void SetLineGutterItemColor(int line, int gutter, Color color) { EnsureTextMutable(); if (!color.IsFinite()) throw new ArgumentException("Gutter color must be finite.", nameof(color)); AtGutterCell(line, gutter).Color = color; QueueRedraw(); }
    /// <summary>Gets a cell's text/icon color.</summary><param name="line">Existing line.</param><param name="gutter">Existing gutter.</param><returns>Finite color.</returns>
    public Color GetLineGutterItemColor(int line, int gutter) => AtGutterCell(line, gutter).Color;
    private sealed record GutterMetadata<T>(T Value);
    /// <summary>Stores runtime gutter metadata with an exact generic type.</summary><typeparam name="T">The payload type.</typeparam><param name="line">Existing line.</param><param name="gutter">Existing gutter.</param><param name="metadata">Borrowed typed value.</param>
    public void SetLineGutterMetadata<T>(int line, int gutter, T metadata) { EnsureTextMutable(); AtGutterCell(line, gutter).Metadata = new GutterMetadata<T>(metadata); }
    /// <summary>Gets runtime gutter metadata of the exact generic type.</summary><typeparam name="T">The stored type.</typeparam><param name="line">Existing line.</param><param name="gutter">Existing gutter.</param><returns>Borrowed typed value.</returns>
    public T GetLineGutterMetadata<T>(int line, int gutter) => AtGutterCell(line, gutter).Metadata is GutterMetadata<T> metadata ? metadata.Value : throw new KeyNotFoundException("No matching typed gutter metadata.");
    /// <summary>Sets a cell-specific clickable policy.</summary><param name="line">Existing line.</param><param name="gutter">Existing gutter.</param><param name="clickable">Click policy.</param>
    public void SetLineGutterClickable(int line, int gutter, bool clickable) { EnsureTextMutable(); AtGutterCell(line, gutter).Clickable = clickable; }
    /// <summary>Gets whether a cell-specific or gutter-wide policy permits clicks.</summary><param name="line">Existing line.</param><param name="gutter">Existing gutter.</param><returns>Effective click policy.</returns>
    public bool IsLineGutterClickable(int line, int gutter) => AtGutter(gutter).Clickable || AtGutterCell(line, gutter).Clickable;
    /// <summary>Merges transferable nonempty gutter items between two logical lines.</summary><param name="fromLine">Source line.</param><param name="toLine">Destination line.</param>
    public void MergeGutters(int fromLine, int toLine)
    { EnsureTextMutable(); var from = AtLine(fromLine); var to = AtLine(toLine); for (var i = 0; i < _gutters.Count; i++) if (_gutters[i].Overwritable && (from.Gutters[i].Text.Length > 0 || from.Gutters[i].Icon != null || from.Gutters[i].Metadata != null)) { var src = from.Gutters[i]; var dst = to.Gutters[i]; dst.Text = src.Text; dst.Icon = src.Icon; dst.Color = src.Color; dst.Metadata = src.Metadata; dst.Clickable = src.Clickable; } InvalidateTextLayout(); }
    /// <summary>Sets the background color of a logical line.</summary><param name="line">Existing index.</param><param name="color">Finite RGBA color.</param>
    public void SetLineBackgroundColor(int line, Color color) { EnsureTextMutable(); if (!color.IsFinite()) throw new ArgumentException("Line color must be finite.", nameof(color)); AtLine(line).Background = color; QueueRedraw(); }
    /// <summary>Returns a line's background color.</summary><param name="line">Existing index.</param><returns>Transparent initially.</returns>
    public Color GetLineBackgroundColor(int line) => AtLine(line).Background;
}
