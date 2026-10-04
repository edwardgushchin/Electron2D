namespace Electron2D;

/// <summary>Measures and records the hexadecimal placeholder used for unsupported text scalars.</summary>
internal static class TextMissingGlyph
{
    /// <summary>Gets the advance and height, including one trailing scale unit.</summary>
    internal static Vector2 Size(int fontSize, uint scalar) => new Vector2(4 + 4 * Columns(scalar), 15) * Scale(fontSize);

    /// <summary>Records a frame and hexadecimal digits relative to a text baseline.</summary>
    internal static void Draw(CanvasItem canvas, int fontSize, Vector2 baseline, uint scalar, Color color, Rect2? clip = null, bool drawZero = false)
    {
        if (scalar == 0 && !drawZero) return;
        var columns = Columns(scalar); var scale = Scale(fontSize);
        var width = (3 + 4 * columns) * scale; var height = 15 * scale;
        var origin = baseline - new Vector2(0, (float)Math.Truncate(height * .85));
        Rect(canvas, new(origin, new(scale, height)), color, clip);
        Rect(canvas, new(origin + new Vector2(width - scale, 0), new(scale, height)), color, clip);
        Rect(canvas, new(origin, new(width, scale)), color, clip);
        Rect(canvas, new(origin + new Vector2(0, height - scale), new(width, scale)), color, clip);
        for (var row = 0; row < 2; row++)
            for (var column = 0; column < columns; column++)
            {
                var shift = (2 * columns - row * columns - column - 1) * 4;
                Digit(canvas, origin + new Vector2(2 + column * 4, 2 + row * 6) * scale, scale, (int)((scalar >> shift) & 15), color, clip);
            }
    }

    private static void Rect(CanvasItem canvas, Rect2 rect, Color color, Rect2? clip)
    {
        if (clip is { } value) rect = rect.Intersection(value);
        if (rect.HasArea()) canvas.DrawRect(rect, color);
    }
    private static int Columns(uint scalar) => scalar <= 0xFF ? 1 : scalar <= 0xFFFF ? 2 : 3;
    private static float Scale(int fontSize) => MathF.Max(1, MathF.Round(fontSize / 15f, MidpointRounding.AwayFromZero));

    private static void Digit(CanvasItem canvas, Vector2 origin, float scale, int digit, Color color, Rect2? clip)
    {
        ReadOnlySpan<byte> masks = [0x7E, 0x30, 0x6D, 0x79, 0x33, 0x5B, 0x5F, 0x70, 0x7F, 0x7B, 0x77, 0x1F, 0x4E, 0x3D, 0x4F, 0x47];
        ReadOnlySpan<byte> segments = [0, 0, 3, 1, 2, 0, 1, 3, 2, 2, 1, 3, 0, 4, 3, 1, 0, 2, 1, 3, 0, 0, 1, 3, 0, 2, 3, 1];
        var mask = masks[digit];
        for (var segment = 0; segment < 7; segment++)
        {
            if ((mask & (1 << (6 - segment))) == 0) continue;
            var offset = segment * 4;
            Rect(canvas, new(origin + new Vector2(segments[offset], segments[offset + 1]) * scale,
                new Vector2(segments[offset + 2], segments[offset + 3]) * scale), color, clip);
        }
    }
}
