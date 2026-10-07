namespace Electron2D;

public sealed partial class TreeItem
{
    /// <summary>Sets one cell's AutoTranslateMode configuration and refreshes its presentation.</summary><param name="column">Existing column.</param><param name="mode">Typed configuration.</param>
    public void SetAutoTranslateMode(int column, NodeAutoTranslateMode mode) { MutableItem(); if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode)); At(column).AutoTranslate = mode; Changed(); }
    /// <summary>Returns one cell's AutoTranslateMode configuration.</summary><param name="column">Existing column.</param><returns>Configured typed value.</returns>
    public NodeAutoTranslateMode GetAutoTranslateMode(int column) => At(column).AutoTranslate;
    /// <summary>Sets one cell's AutowrapMode configuration and refreshes its presentation.</summary><param name="column">Existing column.</param><param name="autowrapMode">Typed configuration.</param>
    public void SetAutowrapMode(int column, TextAutowrapMode autowrapMode) { MutableItem(); if (!Enum.IsDefined(autowrapMode)) throw new ArgumentOutOfRangeException(nameof(autowrapMode)); At(column).Autowrap = autowrapMode; Changed(); }
    /// <summary>Returns one cell's AutowrapMode configuration.</summary><param name="column">Existing column.</param><returns>Configured typed value.</returns>
    public TextAutowrapMode GetAutowrapMode(int column) => At(column).Autowrap;
    /// <summary>Sets one cell's AutowrapTrimFlags configuration and refreshes its presentation.</summary><param name="column">Existing column.</param><param name="flags">Typed configuration.</param>
    public void SetAutowrapTrimFlags(int column, TextLineBreakFlags flags) { MutableItem(); At(column).Trim = flags; Changed(); }
    /// <summary>Returns one cell's AutowrapTrimFlags configuration.</summary><param name="column">Existing column.</param><returns>Configured typed value.</returns>
    public TextLineBreakFlags GetAutowrapTrimFlags(int column) => At(column).Trim;
    /// <summary>Changes the cell mode and resets its numeric, checked, text/icon and icon-width fields.</summary><param name="column">Existing column.</param><param name="mode">Typed configuration.</param>
    public void SetCellMode(int column, TreeCellMode mode) { MutableItem(); if (!Enum.IsDefined(mode)) throw new ArgumentOutOfRangeException(nameof(mode)); var cell = At(column); if (cell.Mode == mode) return; cell.Mode = mode; cell.Min = 0; cell.Max = 100; cell.Step = 1; cell.Value = 0; cell.Checked = false; cell.Icon = null; cell.Text = ""; cell.IconMaxWidth = 0; Changed(); }
    /// <summary>Returns one cell's CellMode configuration.</summary><param name="column">Existing column.</param><returns>Configured typed value.</returns>
    public TreeCellMode GetCellMode(int column) => At(column).Mode;
    /// <summary>Borrows a live custom font, or null to use the owning theme.</summary><param name="column">Existing column.</param><param name="font">Typed configuration.</param>
    public void SetCustomFont(int column, Font? font) { MutableItem(); if (font?.IsDisposed == true) throw new ObjectDisposedException(nameof(font)); At(column).Font = font; Changed(); }
    /// <summary>Returns one cell's CustomFont configuration.</summary><param name="column">Existing column.</param><returns>Configured typed value.</returns>
    public Font? GetCustomFont(int column) => At(column).Font is { IsDisposed: false } resource ? resource : null;
    /// <summary>Sets custom font size in pixels; nonpositive values use the owning theme.</summary><param name="column">Existing column.</param><param name="fontSize">Typed configuration.</param>
    public void SetCustomFontSize(int column, int fontSize) { MutableItem(); At(column).FontSize = fontSize; Changed(); }
    /// <summary>Returns one cell's CustomFontSize configuration.</summary><param name="column">Existing column.</param><returns>Configured typed value.</returns>
    public int GetCustomFontSize(int column) => At(column).FontSize;
    /// <summary>Borrows a live cell style, or null to omit it.</summary><param name="column">Existing column.</param><param name="styleBox">Typed configuration.</param>
    public void SetCustomStyleBox(int column, StyleBox? styleBox) { MutableItem(); if (styleBox?.IsDisposed == true) throw new ObjectDisposedException(nameof(styleBox)); At(column).Style = styleBox; Changed(); }
    /// <summary>Returns one cell's CustomStyleBox configuration.</summary><param name="column">Existing column.</param><returns>Configured typed value.</returns>
    public StyleBox? GetCustomStyleBox(int column) => At(column).Style is { IsDisposed: false } resource ? resource : null;
    /// <summary>Retains a runtime semantic description; native publication requires the semantic service.</summary><param name="column">Existing column.</param><param name="description">Typed configuration.</param>
    public void SetDescription(int column, string description) { MutableItem(); ArgumentNullException.ThrowIfNull(description); At(column).Description = description; Changed(); }
    /// <summary>Returns one cell's Description configuration.</summary><param name="column">Existing column.</param><returns>Configured typed value.</returns>
    public string GetDescription(int column) => At(column).Description;
    /// <summary>Allows text and cell geometry to span consecutive empty noneditable String cells to its right.</summary><param name="column">Existing column.</param><param name="enable">Typed configuration.</param>
    public void SetExpandRight(int column, bool enable) { MutableItem(); At(column).ExpandRight = enable; Changed(); }
    /// <summary>Returns one cell's ExpandRight configuration.</summary><param name="column">Existing column.</param><returns>Configured typed value.</returns>
    public bool GetExpandRight(int column) => At(column).ExpandRight;
    /// <summary>Borrows a live icon, or null to omit it.</summary><param name="column">Existing column.</param><param name="texture">Typed configuration.</param>
    public void SetIcon(int column, Texture? texture) { MutableItem(); if (texture?.IsDisposed == true) throw new ObjectDisposedException(nameof(texture)); At(column).Icon = texture; Changed(); }
    /// <summary>Returns one cell's Icon configuration.</summary><param name="column">Existing column.</param><returns>Configured typed value.</returns>
    public Texture? GetIcon(int column) => At(column).Icon is { IsDisposed: false } resource ? resource : null;
    /// <summary>Sets one cell's IconMaxWidth configuration and refreshes its presentation.</summary><param name="column">Existing column.</param><param name="width">Typed configuration.</param>
    public void SetIconMaxWidth(int column, int width) { MutableItem(); At(column).IconMaxWidth = width; Changed(); }
    /// <summary>Returns one cell's IconMaxWidth configuration.</summary><param name="column">Existing column.</param><returns>Configured typed value.</returns>
    public int GetIconMaxWidth(int column) => At(column).IconMaxWidth;
    /// <summary>Sets one cell's IconModulate configuration and refreshes its presentation.</summary><param name="column">Existing column.</param><param name="modulate">Typed configuration.</param>
    public void SetIconModulate(int column, Color modulate) { MutableItem(); if (!modulate.IsFinite()) throw new ArgumentException("Cell colors must be finite.", nameof(modulate)); At(column).IconModulate = modulate; Changed(); }
    /// <summary>Returns one cell's IconModulate configuration.</summary><param name="column">Existing column.</param><returns>Configured typed value.</returns>
    public Color GetIconModulate(int column) => At(column).IconModulate;
    /// <summary>Borrows a live lower-right icon overlay, or null to omit it.</summary><param name="column">Existing column.</param><param name="texture">Typed configuration.</param>
    public void SetIconOverlay(int column, Texture? texture) { MutableItem(); if (texture?.IsDisposed == true) throw new ObjectDisposedException(nameof(texture)); At(column).Overlay = texture; Changed(); }
    /// <summary>Returns one cell's IconOverlay configuration.</summary><param name="column">Existing column.</param><returns>Configured typed value.</returns>
    public Texture? GetIconOverlay(int column) => At(column).Overlay is { IsDisposed: false } resource ? resource : null;
    /// <summary>Sets one cell's IconRegion configuration and refreshes its presentation.</summary><param name="column">Existing column.</param><param name="region">Typed configuration.</param>
    public void SetIconRegion(int column, Rect2 region) { MutableItem(); if (!region.IsFinite()) throw new ArgumentException("Cell geometry must be finite.", nameof(region)); At(column).IconRegion = region; Changed(); }
    /// <summary>Returns one cell's IconRegion configuration.</summary><param name="column">Existing column.</param><returns>Configured typed value.</returns>
    public Rect2 GetIconRegion(int column) => At(column).IconRegion;
    /// <summary>Sets one cell's Language configuration and refreshes its presentation.</summary><param name="column">Existing column.</param><param name="language">Typed configuration.</param>
    public void SetLanguage(int column, string language) { MutableItem(); ArgumentNullException.ThrowIfNull(language); At(column).Language = language; Changed(); }
    /// <summary>Returns one cell's Language configuration.</summary><param name="column">Existing column.</param><returns>Configured typed value.</returns>
    public string GetLanguage(int column) => At(column).Language;
    /// <summary>Sets one cell's StructuredTextBIDIOverride configuration and refreshes its presentation.</summary><param name="column">Existing column.</param><param name="parser">Typed configuration.</param>
    public void SetStructuredTextBIDIOverride(int column, StructuredTextParser parser) { MutableItem(); if (!Enum.IsDefined(parser)) throw new ArgumentOutOfRangeException(nameof(parser)); At(column).Parser = parser; Changed(); }
    /// <summary>Returns one cell's StructuredTextBIDIOverride configuration.</summary><param name="column">Existing column.</param><returns>Configured typed value.</returns>
    public StructuredTextParser GetStructuredTextBIDIOverride(int column) => At(column).Parser;
    /// <summary>Sets one cell's StructuredTextBIDIOverrideOptions configuration and refreshes its presentation.</summary><param name="column">Existing column.</param><param name="args">Typed configuration.</param>
    public void SetStructuredTextBIDIOverrideOptions(int column, string[] args) { MutableItem(); ArgumentNullException.ThrowIfNull(args); if (args.Any(a => a == null)) throw new ArgumentException("Null parser option.", nameof(args)); At(column).ParserOptions = (string[])args.Clone(); Changed(); }
    /// <summary>Returns one cell's StructuredTextBIDIOverrideOptions configuration.</summary><param name="column">Existing column.</param><returns>Configured typed value; copied array.</returns>
    public string[] GetStructuredTextBIDIOverrideOptions(int column) => (string[])At(column).ParserOptions.Clone();
    /// <summary>Sets one cell's Suffix configuration and refreshes its presentation.</summary><param name="column">Existing column.</param><param name="text">Typed configuration.</param>
    public void SetSuffix(int column, string text) { MutableItem(); ArgumentNullException.ThrowIfNull(text); At(column).Suffix = text; Changed(); }
    /// <summary>Returns one cell's Suffix configuration.</summary><param name="column">Existing column.</param><returns>Configured typed value.</returns>
    public string GetSuffix(int column) => At(column).Suffix;
    /// <summary>Sets source text; in Range mode comma-separated label:id choices define bounds and disable snapping.</summary><param name="column">Existing column.</param><param name="text">Typed configuration.</param>
    public void SetText(int column, string text) { MutableItem(); ArgumentNullException.ThrowIfNull(text); var cell = At(column); cell.Text = text; if (cell.Mode == TreeCellMode.Range) { var choices = text.Split(','); cell.Min = int.MaxValue; cell.Max = int.MinValue; for (var i = 0; i < choices.Length; i++) { var split = choices[i].LastIndexOf(':'); var id = split >= 0 && int.TryParse(choices[i].AsSpan(split + 1), out var parsed) ? parsed : i; cell.Min = Math.Min(cell.Min, id); cell.Max = Math.Max(cell.Max, id); } cell.Step = 0; } Changed(); }
    /// <summary>Returns one cell's Text configuration.</summary><param name="column">Existing column.</param><returns>Configured typed value.</returns>
    public string GetText(int column) => At(column).Text;
    /// <summary>Sets one cell's TextAlignment configuration and refreshes its presentation.</summary><param name="column">Existing column.</param><param name="alignment">Typed configuration.</param>
    public void SetTextAlignment(int column, HorizontalAlignment alignment) { MutableItem(); if (!Enum.IsDefined(alignment)) throw new ArgumentOutOfRangeException(nameof(alignment)); At(column).Alignment = alignment; Changed(); }
    /// <summary>Returns one cell's TextAlignment configuration.</summary><param name="column">Existing column.</param><returns>Configured typed value.</returns>
    public HorizontalAlignment GetTextAlignment(int column) => At(column).Alignment;
    /// <summary>Sets one cell's TextDirection configuration and refreshes its presentation.</summary><param name="column">Existing column.</param><param name="direction">Typed configuration.</param>
    public void SetTextDirection(int column, TextDirection direction) { MutableItem(); if (!Enum.IsDefined(direction)) throw new ArgumentOutOfRangeException(nameof(direction)); At(column).Direction = direction; Changed(); }
    /// <summary>Returns one cell's TextDirection configuration.</summary><param name="column">Existing column.</param><returns>Configured typed value.</returns>
    public TextDirection GetTextDirection(int column) => At(column).Direction;
    /// <summary>Sets one cell's TextOverrunBehavior configuration and refreshes its presentation.</summary><param name="column">Existing column.</param><param name="behavior">Typed configuration.</param>
    public void SetTextOverrunBehavior(int column, TextOverrunBehavior behavior) { MutableItem(); if (!Enum.IsDefined(behavior)) throw new ArgumentOutOfRangeException(nameof(behavior)); At(column).Overrun = behavior; Changed(); }
    /// <summary>Returns one cell's TextOverrunBehavior configuration.</summary><param name="column">Existing column.</param><returns>Configured typed value.</returns>
    public TextOverrunBehavior GetTextOverrunBehavior(int column) => At(column).Overrun;
    /// <summary>Sets one cell's TooltipText configuration and refreshes its presentation.</summary><param name="column">Existing column.</param><param name="tooltip">Typed configuration.</param>
    public void SetTooltipText(int column, string tooltip) { MutableItem(); ArgumentNullException.ThrowIfNull(tooltip); At(column).Tooltip = tooltip; Changed(); }
    /// <summary>Returns one cell's TooltipText configuration.</summary><param name="column">Existing column.</param><returns>Configured typed value.</returns>
    public string GetTooltipText(int column) => At(column).Tooltip;
    /// <summary>Sets the cell's CustomAsButton policy.</summary><param name="column">Existing column.</param><param name="enable">Desired policy.</param>
    public void SetCustomAsButton(int column, bool enable) { MutableItem(); At(column).CustomButton = enable; Changed(); }
    /// <summary>Reports the cell's CustomSetAsButton policy.</summary><param name="column">Existing column.</param><returns>Configured state.</returns>
    public bool IsCustomSetAsButton(int column) => At(column).CustomButton;
    /// <summary>Sets the cell's EditMultiline policy.</summary><param name="column">Existing column.</param><param name="enable">Desired policy.</param>
    public void SetEditMultiline(int column, bool enable) { MutableItem(); At(column).Multiline = enable; Changed(); }
    /// <summary>Reports the cell's EditMultiline policy.</summary><param name="column">Existing column.</param><returns>Configured state.</returns>
    public bool IsEditMultiline(int column) => At(column).Multiline;
    /// <summary>Sets the cell's Editable policy.</summary><param name="column">Existing column.</param><param name="enable">Desired policy.</param>
    public void SetEditable(int column, bool enable) { MutableItem(); At(column).Editable = enable; Changed(); }
    /// <summary>Reports the cell's Editable policy.</summary><param name="column">Existing column.</param><returns>Configured state.</returns>
    public bool IsEditable(int column) => At(column).Editable;
    /// <summary>Sets the cell's Selectable policy.</summary><param name="column">Existing column.</param><param name="enable">Desired policy.</param>
    public void SetSelectable(int column, bool enable) { MutableItem(); At(column).Selectable = enable; Changed(); }
    /// <summary>Reports the cell's Selectable policy.</summary><param name="column">Existing column.</param><returns>Configured state.</returns>
    public bool IsSelectable(int column) => At(column).Selectable;
}
