namespace Electron2D;

public sealed partial class AccessibilityServer
{
    private static void TextAttributesChanged(Element e) => Runtime._native!.TextAttributesChanged(e);
    /// <summary>Publishes horizontal text alignment through native text attributes.</summary>
    /// <param name="id">Live text element.</param><param name="align">The horizontal text alignment.</param>
    /// <exception cref="ArgumentException">A supplied selector, color or text value is invalid.</exception>
    /// <exception cref="KeyNotFoundException">Element is absent or retired.</exception><exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    public static void UpdateSetTextAlign(RID id, HorizontalAlignment align)
    {
        if (!Enum.IsDefined(align)) throw new ArgumentOutOfRangeException(nameof(align));
        var r = Require(); var e = r.Get(id); e.TextAlignment = align; e.TextFormatMask |= 1; r.EnableText(e); TextAttributesChanged(e);
    }
    /// <summary>Publishes the text advance orientation through native text attributes.</summary>
    /// <param name="id">Live text element.</param><param name="vertical">Whether the text advances vertically.</param>
    /// <exception cref="ArgumentException">A supplied selector, color or text value is invalid.</exception>
    /// <exception cref="KeyNotFoundException">Element is absent or retired.</exception><exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    public static void UpdateSetTextOrientation(RID id, bool vertical)
    {

        var r = Require(); var e = r.Get(id); e.TextVertical = vertical; e.TextFormatMask |= 2; r.EnableText(e); TextAttributesChanged(e);
    }
    /// <summary>Publishes finite foreground color through native text attributes.</summary>
    /// <param name="id">Live text element.</param><param name="color">Finite foreground color.</param>
    /// <exception cref="ArgumentException">A supplied selector, color or text value is invalid.</exception>
    /// <exception cref="KeyNotFoundException">Element is absent or retired.</exception><exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    public static void UpdateSetForegroundColor(RID id, Color color)
    {
        if (!color.IsFinite()) throw new ArgumentException("Text color must be finite.", nameof(color));
        var r = Require(); var e = r.Get(id); e.TextForeground = color; r.EnableText(e); TextAttributesChanged(e);
    }
    /// <summary>Publishes finite background color through native text attributes.</summary>
    /// <param name="id">Live text element.</param><param name="color">Finite background color.</param>
    /// <exception cref="ArgumentException">A supplied selector, color or text value is invalid.</exception>
    /// <exception cref="KeyNotFoundException">Element is absent or retired.</exception><exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    public static void UpdateSetBackgroundColor(RID id, Color color)
    {
        if (!color.IsFinite()) throw new ArgumentException("Text color must be finite.", nameof(color));
        var r = Require(); var e = r.Get(id); e.TextBackground = color; r.EnableText(e); TextAttributesChanged(e);
    }
    /// <summary>Publishes a text language tag through native text attributes.</summary>
    /// <param name="id">Live text element.</param><param name="language">Nonnull text language tag.</param>
    /// <exception cref="ArgumentException">A supplied selector, color or text value is invalid.</exception>
    /// <exception cref="KeyNotFoundException">Element is absent or retired.</exception><exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    public static void UpdateSetLanguage(RID id, string language)
    {
        ArgumentNullException.ThrowIfNull(language);
        var r = Require(); var e = r.Get(id); e.TextLanguage = language; e.TextFormatMask |= 8; r.EnableText(e); TextAttributesChanged(e);
    }
    /// <summary>Publishes placeholder text separately from the actual value through native text attributes.</summary>
    /// <param name="id">Live text element.</param><param name="placeholder">Nonnull placeholder exposed independently of actual text.</param>
    /// <exception cref="ArgumentException">A supplied selector, color or text value is invalid.</exception>
    /// <exception cref="KeyNotFoundException">Element is absent or retired.</exception><exception cref="NotSupportedException">No native bridge is active.</exception>
    /// <exception cref="InvalidOperationException">Access is off the display owner.</exception>
    public static void UpdateSetPlaceholder(RID id, string placeholder)
    {
        ArgumentNullException.ThrowIfNull(placeholder);
        var r = Require(); var e = r.Get(id); e.TextPlaceholder = placeholder; e.TextFormatMask |= 16; r.EnableText(e); TextAttributesChanged(e);
    }
    /// <summary>Publishes underline, strikethrough and overline styling through native text attributes.</summary>
    /// <param name="id">Live text element.</param><param name="underline">Underline enabled.</param><param name="strikethrough">Strikethrough enabled.</param><param name="overline">Overline enabled.</param><param name="color">Finite decoration color; null selects opaque black.</param>
    /// <exception cref="ArgumentException">Color is nonfinite.</exception><exception cref="KeyNotFoundException">Element is absent or retired.</exception>
    /// <exception cref="NotSupportedException">No native bridge is active.</exception><exception cref="InvalidOperationException">Access is off the display owner.</exception>
    public static void UpdateSetTextDecorations(RID id, bool underline, bool strikethrough, bool overline, Color? color = null)
    {
        var c = color ?? Colors.Black; if (!c.IsFinite()) throw new ArgumentException("Decoration color must be finite.", nameof(color));
        var r = Require(); var e = r.Get(id); e.TextUnderline = underline; e.TextStrikethrough = strikethrough; e.TextOverline = overline; e.TextDecorationColor = c; e.TextFormatMask |= 4;
        r.EnableText(e); TextAttributesChanged(e);
    }
}
