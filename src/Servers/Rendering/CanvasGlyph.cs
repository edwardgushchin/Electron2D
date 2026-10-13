namespace Electron2D;

internal enum CanvasGlyphMode { None, MSDF = 2, LCD = 3 }
internal readonly record struct CanvasGlyph(CanvasGlyphMode Mode, float Outline = 0, float PixelRange = 4, float Scale = 1)
{
    internal Vector4 Parameters => new((float)Mode, PixelRange, Outline / Scale / 4, 0);
    internal void Validate()
    {
        if (!float.IsFinite(Outline)) throw new ArgumentException("Glyph outlines must be finite.");
        if (!float.IsFinite(PixelRange) || PixelRange <= 0) throw new ArgumentOutOfRangeException(nameof(PixelRange));
        if (!float.IsFinite(Scale) || Scale <= 0) throw new ArgumentOutOfRangeException(nameof(Scale));
        if (!Parameters.IsFinite()) throw new ArgumentException("Glyph parameters overflowed finite values.");
    }
}
