namespace Electron2D;

public sealed class TextureRegion(Rect sourceRectangle)
{
    public TextureRegion(int x, int y, int width, int height)
        : this(new Rect(x, y, width, height)) { }

    public Rect SourceRectangle { get; } = sourceRectangle;
}