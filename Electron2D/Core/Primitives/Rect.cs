namespace Electron2D;

/// <summary>
/// Прямоугольник в пикселях.
/// </summary>
/// <remarks>
/// <see cref="IsEmpty"/> трактует нулевую или отрицательную ширину/высоту как пустой прямоугольник.
/// </remarks>
public readonly record struct Rect(int X, int Y, int Width, int Height)
{
    /// <summary>
    /// Признак пустого прямоугольника (Width &lt;= 0 или Height &lt;= 0).
    /// </summary>
    public bool IsEmpty => Width <= 0 || Height <= 0;
}