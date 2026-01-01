namespace Electron2D;

/// <summary>
/// Прямоугольник в пикселях.
/// </summary>
public readonly record struct Rect(int X, int Y, int Width, int Height)
{
    public int X { get; } = X;
    public int Y { get; } = Y;
    public int Width { get; } = Width;
    public int Height { get; } = Height;

    public bool IsEmpty => Width <= 0 || Height <= 0;
}