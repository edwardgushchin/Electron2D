namespace Electron2D;

internal sealed partial class TextLayout
{
    internal Rect2 AccessibilityScalarBounds(int character)
    {
        var rect = GetCharacterBounds(character);
        foreach (var glyph in _glyphs)
        {
            if (glyph.Virtual || character < glyph.Start || character >= glyph.End) continue;
            var width = Key.Orientation == TextOrientation.Vertical ? rect.Size.Y : rect.Size.X; var step = width / (glyph.End - glyph.Start);
            var shift = step * (glyph.RTL ? glyph.End - character - 1 : character - glyph.Start);
            return Key.Orientation == TextOrientation.Vertical ? new(rect.Position + new Vector2(0, shift), new(rect.Size.X, step)) : new(rect.Position + new Vector2(shift, 0), new(step, rect.Size.Y));
        }
        return rect;
    }
    internal (int Start, int End, string Family, int Size, int Weight, bool Italic, string Language, TextDirection Direction) ScalarStyle(int character)
    {
        foreach (var glyph in _glyphs) if (!glyph.Virtual && character >= glyph.Start && character < glyph.End)
                return (glyph.Start, glyph.End, glyph.Face?.FamilyName ?? "", GlyphSize(glyph), glyph.Face?.FontWeight ?? 400, glyph.Face is { } face && (face.FontStyle & FontStyle.Italic) != 0, StyleAt(character).Language, glyph.RTL ? TextDirection.RTL : TextDirection.LTR);
        return (0, _count, "", Key.FontSize, 400, false, "", TextDirection.LTR);
    }
}
