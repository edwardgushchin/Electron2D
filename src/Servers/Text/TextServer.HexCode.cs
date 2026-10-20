using System.Text;

namespace Electron2D;

public abstract partial class TextServer
{
    /// <summary>Measures a hexadecimal replacement box without loading a font.</summary>
    /// <param name="size">Logical font size; rounded to a pixel scale of at least one.</param>
    /// <param name="index">Unicode scalar to display.</param>
    /// <returns>Advance and height, including one trailing scale unit.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is not a Unicode scalar.</exception>
    public Vector2 GetHexCodeBoxSize(int size, int index)
    {
        if (!Rune.IsValid(index)) throw new ArgumentOutOfRangeException(nameof(index));
        return TextMissingGlyph.Size(size, (uint)index);
    }

    /// <summary>Records a frame and hexadecimal digits at a text baseline without loading a font.</summary>
    /// <param name="canvas">Canvas item in its active recording scope.</param>
    /// <param name="size">Logical font size; rounded to a pixel scale of at least one.</param>
    /// <param name="position">Finite baseline origin.</param>
    /// <param name="index">Unicode scalar to display; zero records no geometry.</param>
    /// <param name="color">Finite frame and digit color.</param>
    /// <remarks>Uses two, four or six digits. Canvas transforms, modulation and clipping apply normally.
    /// Validation also applies to the zero-scalar no-op. Prepared recording reuses canvas storage.</remarks>
    /// <exception cref="ArgumentNullException">The canvas is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The index is not a Unicode scalar.</exception>
    /// <exception cref="ArgumentException">The position or color is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The canvas is off its owner thread or outside its recording scope.</exception>
    /// <exception cref="ObjectDisposedException">The canvas is disposed.</exception>
    public void DrawHexCodeBox(CanvasItem canvas, int size, Vector2 position, int index, Color color)
    {
        if (!Rune.IsValid(index)) throw new ArgumentOutOfRangeException(nameof(index));
        Font.ValidateDraw(canvas, position, color, 0);
        TextMissingGlyph.Draw(canvas, size, position, (uint)index, color);
    }
}
