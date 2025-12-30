namespace Electron2D;

[Flags]
public enum FlipMode
{
    None = 0,
    Horizontal = 1,
    Vertical = 2,
    HorizontalAndVertical = Horizontal |  Vertical
}