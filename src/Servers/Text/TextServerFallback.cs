namespace Electron2D;

/// <summary>Provides scalar-by-scalar left-to-right text layout using the shared font raster and bitmap caches.</summary>
/// <remarks>Contextual substitutions and bidirectional reordering are unavailable. Each instance owns its
/// service-created fonts and buffers, independently of other registered text implementations.</remarks>
public sealed partial class TextServerFallback : TextServerExtension
{
    /// <summary>Creates an unregistered simple text implementation.</summary>
    public TextServerFallback() { }
    /// <inheritdoc />
    protected override string OnGetName() => "Fallback (Built-in)";
    /// <inheritdoc />
    protected override Feature OnGetFeatures() => Feature.SimpleLayout | Feature.FontBitmap | Feature.FontDynamic | Feature.FontMSDF;
    /// <inheritdoc />
    protected override RID OnCreateShapedText(TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal)
    {
        ValidateDirection(direction);
        return base.OnCreateShapedText(TextDirection.LTR, orientation);
    }
    internal static void ValidateDirection(TextDirection direction)
    {
        if (!Enum.IsDefined(direction) || direction == TextDirection.Inherited) throw new ArgumentOutOfRangeException(nameof(direction));
        if (direction == TextDirection.RTL) throw new NotSupportedException("The fallback text implementation supports left-to-right layout only.");
    }
    /// <inheritdoc />
    protected override TextDirection OnShapedTextGetDirection(RID shaped) => TextDirection.LTR;
    /// <inheritdoc />
    protected override void OnShapedTextSetDirection(RID shaped, TextDirection direction = TextDirection.Auto)
    { ValidateDirection(direction); base.OnShapedTextSetDirection(shaped, TextDirection.LTR); }
    /// <inheritdoc />
    protected override void OnShapedTextSetBIDIOverride(RID shaped, ReadOnlySpan<TextBIDIRange> ranges)
    {
        foreach (var range in ranges)
            if (range.Direction == TextDirection.RTL) throw new NotSupportedException("The fallback text implementation cannot reorder right-to-left contexts.");
        base.OnShapedTextSetBIDIOverride(shaped, ranges);
    }
}
