using System.Buffers;
using System.Text;

namespace Electron2D;

public partial class CanvasItem
{
    /// <summary>Draws one unshaped Unicode character during this item's recording.</summary>
    /// <param name="font">The live borrowed font.</param>
    /// <param name="position">The finite local baseline position.</param>
    /// <param name="character">Exactly one Unicode scalar, encoded as one or two UTF-16 code units.</param>
    /// <param name="fontSize">Positive logical font size.</param>
    /// <param name="modulate">Finite color multiplier, or null for white.</param>
    /// <param name="oversampling">Positive raster scale, or a nonpositive value for automatic scale.</param>
    /// <exception cref="ArgumentNullException">The font or text is null.</exception>
    /// <exception cref="ArgumentException">Text, geometry, color or an option is invalid.</exception>
    /// <exception cref="InvalidOperationException">Called outside this item's recording scope or off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node or required resource is disposed.</exception>
    public void DrawChar(Font font, Vector2 position, string character,
        int fontSize = 16, Color? modulate = null, float oversampling = 0)
    {
        EnsureDrawing(); ArgumentNullException.ThrowIfNull(font);
        font.DrawChar(this, position, SingleTextScalar(character), fontSize, modulate, oversampling);
    }

    /// <summary>Draws the outline of one unshaped Unicode character during this item's recording.</summary>
    /// <param name="font">The live borrowed font.</param>
    /// <param name="position">The finite local baseline position.</param>
    /// <param name="character">Exactly one Unicode scalar, encoded as one or two UTF-16 code units.</param>
    /// <param name="fontSize">Positive logical font size.</param>
    /// <param name="size">Outline radius; nonpositive values draw an unexpanded glyph.</param>
    /// <param name="modulate">Finite color multiplier, or null for white.</param>
    /// <param name="oversampling">Positive raster scale, or a nonpositive value for automatic scale.</param>
    /// <exception cref="ArgumentNullException">The font or text is null.</exception>
    /// <exception cref="ArgumentException">Text, geometry, color or an option is invalid.</exception>
    /// <exception cref="InvalidOperationException">Called outside this item's recording scope or off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node or required resource is disposed.</exception>
    public void DrawCharOutline(Font font, Vector2 position, string character,
        int fontSize = 16, int size = -1, Color? modulate = null,
        float oversampling = 0)
    {
        EnsureDrawing(); ArgumentNullException.ThrowIfNull(font);
        font.DrawCharOutline(this, position, SingleTextScalar(character), fontSize, size, modulate, oversampling);
    }

    /// <summary>Draws one shaped line during this item's recording.</summary>
    /// <param name="font">The live borrowed font.</param>
    /// <param name="position">The finite local baseline position.</param>
    /// <param name="text">The text to shape and draw.</param>
    /// <param name="alignment">Advance-axis alignment.</param>
    /// <param name="width">Available width; a negative value leaves width unconstrained.</param>
    /// <param name="fontSize">Positive logical font size.</param>
    /// <param name="modulate">Finite color multiplier, or null for white.</param>
    /// <param name="justificationFlags">Fill-spacing rules.</param>
    /// <param name="direction">Paragraph direction.</param>
    /// <param name="orientation">The glyph advance axis.</param>
    /// <param name="oversampling">Positive raster scale, or a nonpositive value for automatic scale.</param>
    /// <exception cref="ArgumentNullException">The font or text is null.</exception>
    /// <exception cref="ArgumentException">Text, geometry, color or an option is invalid.</exception>
    /// <exception cref="InvalidOperationException">Called outside this item's recording scope or off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node or required resource is disposed.</exception>
    public void DrawString(Font font, Vector2 position, string text,
        HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1, int fontSize = 16,
        Color? modulate = null, TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida | TextJustificationFlags.WordBound, TextDirection direction = TextDirection.Auto,
        TextOrientation orientation = TextOrientation.Horizontal, float oversampling = 0)
    {
        EnsureDrawing(); ArgumentNullException.ThrowIfNull(font);
        font.DrawString(this, position, text, alignment, width, fontSize, modulate, justificationFlags, direction, orientation, oversampling);
    }

    /// <summary>Draws the outline of one shaped line during this item's recording.</summary>
    /// <param name="font">The live borrowed font.</param>
    /// <param name="position">The finite local baseline position.</param>
    /// <param name="text">The text to shape and draw.</param>
    /// <param name="alignment">Advance-axis alignment.</param>
    /// <param name="width">Available width; a negative value leaves width unconstrained.</param>
    /// <param name="fontSize">Positive logical font size.</param>
    /// <param name="size">Outline radius; nonpositive values draw an unexpanded glyph.</param>
    /// <param name="modulate">Finite color multiplier, or null for white.</param>
    /// <param name="justificationFlags">Fill-spacing rules.</param>
    /// <param name="direction">Paragraph direction.</param>
    /// <param name="orientation">The glyph advance axis.</param>
    /// <param name="oversampling">Positive raster scale, or a nonpositive value for automatic scale.</param>
    /// <exception cref="ArgumentNullException">The font or text is null.</exception>
    /// <exception cref="ArgumentException">Text, geometry, color or an option is invalid.</exception>
    /// <exception cref="InvalidOperationException">Called outside this item's recording scope or off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node or required resource is disposed.</exception>
    public void DrawStringOutline(Font font, Vector2 position, string text,
        HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1, int fontSize = 16,
        int size = 1, Color? modulate = null, TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida | TextJustificationFlags.WordBound,
        TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal, float oversampling = 0)
    {
        EnsureDrawing(); ArgumentNullException.ThrowIfNull(font);
        font.DrawStringOutline(this, position, text, alignment, width, fontSize, size, modulate, justificationFlags, direction, orientation, oversampling);
    }

    /// <summary>Draws a shaped Unicode paragraph during this item's recording.</summary>
    /// <param name="font">The live borrowed font.</param>
    /// <param name="position">The finite local baseline position.</param>
    /// <param name="text">The text to shape and draw.</param>
    /// <param name="alignment">Advance-axis alignment.</param>
    /// <param name="width">Available width; a negative value leaves width unconstrained.</param>
    /// <param name="fontSize">Positive logical font size.</param>
    /// <param name="maxLines">Maximum visible lines; a negative value draws every line.</param>
    /// <param name="modulate">Finite color multiplier, or null for white.</param>
    /// <param name="breakFlags">Unicode line-break rules.</param>
    /// <param name="justificationFlags">Fill-spacing rules.</param>
    /// <param name="direction">Paragraph direction.</param>
    /// <param name="orientation">The glyph advance axis.</param>
    /// <param name="oversampling">Positive raster scale, or a nonpositive value for automatic scale.</param>
    /// <exception cref="ArgumentNullException">The font or text is null.</exception>
    /// <exception cref="ArgumentException">Text, geometry, color or an option is invalid.</exception>
    /// <exception cref="InvalidOperationException">Called outside this item's recording scope or off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node or required resource is disposed.</exception>
    public void DrawMultilineString(Font font, Vector2 position, string text,
        HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1, int fontSize = 16,
        int maxLines = -1, Color? modulate = null, TextLineBreakFlags breakFlags = TextLineBreakFlags.Mandatory | TextLineBreakFlags.WordBound,
        TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida | TextJustificationFlags.WordBound, TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal,
        float oversampling = 0)
    {
        EnsureDrawing(); ArgumentNullException.ThrowIfNull(font);
        font.DrawMultilineString(this, position, text, alignment, width, fontSize, maxLines, modulate, breakFlags, justificationFlags, direction, orientation, oversampling);
    }

    /// <summary>Draws the outline of a shaped Unicode paragraph during this item's recording.</summary>
    /// <param name="font">The live borrowed font.</param>
    /// <param name="position">The finite local baseline position.</param>
    /// <param name="text">The text to shape and draw.</param>
    /// <param name="alignment">Advance-axis alignment.</param>
    /// <param name="width">Available width; a negative value leaves width unconstrained.</param>
    /// <param name="fontSize">Positive logical font size.</param>
    /// <param name="maxLines">Maximum visible lines; a negative value draws every line.</param>
    /// <param name="size">Outline radius; nonpositive values draw an unexpanded glyph.</param>
    /// <param name="modulate">Finite color multiplier, or null for white.</param>
    /// <param name="breakFlags">Unicode line-break rules.</param>
    /// <param name="justificationFlags">Fill-spacing rules.</param>
    /// <param name="direction">Paragraph direction.</param>
    /// <param name="orientation">The glyph advance axis.</param>
    /// <param name="oversampling">Positive raster scale, or a nonpositive value for automatic scale.</param>
    /// <exception cref="ArgumentNullException">The font or text is null.</exception>
    /// <exception cref="ArgumentException">Text, geometry, color or an option is invalid.</exception>
    /// <exception cref="InvalidOperationException">Called outside this item's recording scope or off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The node or required resource is disposed.</exception>
    public void DrawMultilineStringOutline(Font font, Vector2 position, string text,
        HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1, int fontSize = 16,
        int maxLines = -1, int size = 1, Color? modulate = null,
        TextLineBreakFlags breakFlags = TextLineBreakFlags.Mandatory | TextLineBreakFlags.WordBound, TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida | TextJustificationFlags.WordBound, TextDirection direction = TextDirection.Auto,
        TextOrientation orientation = TextOrientation.Horizontal, float oversampling = 0)
    {
        EnsureDrawing(); ArgumentNullException.ThrowIfNull(font);
        font.DrawMultilineStringOutline(this, position, text, alignment, width, fontSize, maxLines, size, modulate, breakFlags, justificationFlags, direction, orientation, oversampling);
    }

    private static int SingleTextScalar(string character)
    {
        ArgumentNullException.ThrowIfNull(character);
        if (Rune.DecodeFromUtf16(character, out var scalar, out var consumed) != OperationStatus.Done || consumed != character.Length)
            throw new ArgumentException("Exactly one Unicode scalar is required.", nameof(character));
        return scalar.Value;
    }
}
