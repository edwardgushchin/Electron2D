namespace Electron2D;

/// <summary>Controls coverage generation for native font glyphs.</summary>
public enum FontAntialiasing
{
    /// <summary>Generates monochrome coverage.</summary>
    None = 0,
    /// <summary>Generates grayscale coverage.</summary>
    Gray = 1,
    /// <summary>Generates independent LCD component coverage using the text owner's subpixel layout.</summary>
    LCD = 2
}

/// <summary>Describes display subpixel ordering used by native LCD glyph rasterization.</summary>
public enum FontLCDSubpixelLayout
{
    /// <summary>Uses grayscale rasterization and ordinary blending even for an LCD font.</summary>
    None = 0,
    /// <summary>Uses horizontal red, green and blue components.</summary>
    HorizontalRGB = 1,
    /// <summary>Uses horizontal blue, green and red components.</summary>
    HorizontalBGR = 2,
    /// <summary>Uses vertical red, green and blue components.</summary>
    VerticalRGB = 3,
    /// <summary>Uses vertical blue, green and red components.</summary>
    VerticalBGR = 4,
    /// <summary>Bounds valid layouts; this value cannot be selected.</summary>
    Max = 5
}
