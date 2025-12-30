namespace Electron2D;

/// <summary>
/// Прямоугольник в пикселях.
/// </summary>
public readonly struct Rect(int x, int y, int width, int height)
{
    public int X { get; } = x;
    public int Y { get; } = y;
    public int Width { get; } = width;
    public int Height { get; } = height;

    public bool IsEmpty => Width <= 0 || Height <= 0;
}