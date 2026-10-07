using System.Text;

namespace Electron2D;

/// <summary>Shapes, measures and draws scalable or authored bitmap text with ordered fallback fonts.</summary>
/// <remarks>Text positions specify the baseline. Fonts borrow fallback resources and forward their changes and
/// disposal. Layout caches and resource state are synchronized; canvas drawing still requires its owning thread
/// and recording scope. Active reads retain native source snapshots; layouts retry changing state at most
/// sixty-four times. Already recorded glyph pixels survive data replacement independently of native faces.</remarks>
public abstract class Font : Resource
{
    internal static readonly object FallbackMutationGate = new();
    internal object FontGate { get; } = new();
    internal abstract FontData? PrimaryData { get; }
    internal virtual Font? BaseDependency => null;
    internal virtual NativeFontFeature[]? GetShapingFeatures(FontData source) => null;
    internal Font?[] LocalFallbacks => _fallbacks;
    internal virtual Font?[] EffectiveFallbacks => _fallbacks;
    private Font?[] _fallbacks = [];
    private readonly Action<Resource> _fallbackChanged;
    private readonly Action<ElectronObject> _fallbackDisposed;
    private readonly List<FontData> _sources = [];
    private readonly List<Font> _dependencies = [];
    private readonly List<long> _dependencyGenerations = [];
    private bool _sourcesDirty = true;
    private readonly List<TextLayout> _single = [], _multi = [];
    private readonly TextLayout _scratch = new();
    private int _singleCapacity = 64, _multiCapacity = 16;
    private long _access, _generation;
    internal const int MaximumReadAttempts = 64;
    private readonly List<ReadContext> _reads = [];
    private int _readDepth;
    private static readonly PropertyDescriptor FallbackProperty = new PropertyDescriptor<Font, Font?[]>(
        nameof(Fallbacks), font => font.Fallbacks, (font, value) => font.Fallbacks = value, _ => [], stored: true);

    /// <summary>Initializes empty fallback lists and caches for sixty-four lines and sixteen paragraphs.</summary>
    protected Font() { _fallbackChanged = FallbackChanged; _fallbackDisposed = FallbackDisposed; }

    /// <summary>Gets or sets the ordered borrowed fallback fonts, retaining null slots and repeated identities.</summary>
    /// <value>An independent array snapshot; initially empty. Every successful assignment emits Changed.</value>
    /// <exception cref="ArgumentNullException">The array is null.</exception>
    /// <exception cref="ArgumentException">The fallback graph would contain a cycle or exceed sixty-four levels.</exception>
    /// <exception cref="ObjectDisposedException">This font or a supplied nonnull fallback is disposed.</exception>
    public Font?[] Fallbacks
    {
        get { lock (FontGate) { ThrowIfDisposed(); return (Font?[])_fallbacks.Clone(); } }
        set
        {
            ArgumentNullException.ThrowIfNull(value); var copy = (Font?[])value.Clone();
            lock (FallbackMutationGate)
            {
                foreach (var font in copy) if (font is not null) ValidateFallback(font, this, 1);
                lock (FontGate) { ThrowIfDisposed(); UnsubscribeFallbacks(); _fallbacks = copy; SubscribeFallbacks(); ClearLayoutCaches(); }
            }
            EmitChanged();
        }
    }

    /// <summary>Returns the maximum ascent across this font and its fallbacks.</summary>
    /// <param name="fontSize">Positive font size in logical pixels.</param>
    /// <returns>Pixels above the baseline, including top spacing.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The font size is not positive.</exception>
    /// <exception cref="ObjectDisposedException">The font or a required fallback is disposed.</exception>
    public float GetAscent(int fontSize = 16) => GetMetric(fontSize, 0);
    /// <summary>Returns the maximum descent across this font and its fallbacks.</summary>
    /// <param name="fontSize">Positive font size in logical pixels.</param>
    /// <returns>Pixels below the baseline, including bottom spacing.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The font size is not positive.</exception>
    /// <exception cref="ObjectDisposedException">The font or a required fallback is disposed.</exception>
    public float GetDescent(int fontSize = 16) => GetMetric(fontSize, 1);
    /// <summary>Returns the maximum face ascent-plus-descent, including top and bottom spacing.</summary>
    /// <param name="fontSize">Positive font size in logical pixels.</param>
    /// <returns>The line height in logical pixels.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The font size is not positive.</exception>
    /// <exception cref="ObjectDisposedException">The font or a required fallback is disposed.</exception>
    public float GetHeight(int fontSize = 16) => GetMetric(fontSize, 2);
    /// <summary>Returns the maximum underline position below the baseline, including top spacing.</summary>
    /// <param name="fontSize">Positive font size in logical pixels.</param><returns>Underline offset in pixels.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The font size is not positive.</exception>
    /// <exception cref="ObjectDisposedException">The font or a required fallback is disposed.</exception>
    public float GetUnderlinePosition(int fontSize = 16) => GetMetric(fontSize, 3);
    /// <summary>Returns the maximum underline thickness across this font and its fallbacks.</summary>
    /// <param name="fontSize">Positive font size in logical pixels.</param><returns>Underline thickness in pixels.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The font size is not positive.</exception>
    /// <exception cref="ObjectDisposedException">The font or a required fallback is disposed.</exception>
    public float GetUnderlineThickness(int fontSize = 16) => GetMetric(fontSize, 4);

    /// <summary>Returns the primary face's family name, or an empty name without data.</summary>
    /// <returns>The family name.</returns><exception cref="ObjectDisposedException">The font is disposed.</exception>
    public string GetFontName() { lock (FontGate) { ThrowIfDisposed(); return PrimaryData?.FamilyName ?? string.Empty; } }
    /// <summary>Returns the primary face's style name.</summary>
    /// <returns>The style name, or an empty name without data.</returns><exception cref="ObjectDisposedException">The font is disposed.</exception>
    public string GetFontStyleName() { lock (FontGate) { ThrowIfDisposed(); return PrimaryData?.StyleName ?? string.Empty; } }
    /// <summary>Returns the primary face's intrinsic style flags.</summary>
    /// <returns>The style flags.</returns><exception cref="ObjectDisposedException">The font is disposed.</exception>
    public FontStyle GetFontStyle() { lock (FontGate) { ThrowIfDisposed(); return PrimaryData?.FontStyle ?? FontStyle.None; } }
    /// <summary>Returns the primary face's weight class.</summary>
    /// <returns>The weight, with four hundred representing normal weight.</returns><exception cref="ObjectDisposedException">The font is disposed.</exception>
    public int GetFontWeight() { lock (FontGate) { ThrowIfDisposed(); return PrimaryData?.FontWeight ?? 400; } }
    /// <summary>Returns the primary face's width as a percentage of normal width.</summary>
    /// <returns>The stretch percentage.</returns><exception cref="ObjectDisposedException">The font is disposed.</exception>
    public int GetFontStretch() { lock (FontGate) { ThrowIfDisposed(); return PrimaryData?.FontStretch ?? 100; } }
    /// <summary>Returns the number of faces in the primary font collection.</summary>
    /// <returns>The collection face count, or zero without data.</returns><exception cref="ObjectDisposedException">The font is disposed.</exception>
    public int GetFaceCount() { lock (FontGate) { ThrowIfDisposed(); return PrimaryData?.FaceCount ?? 0; } }
    /// <summary>Returns additional spacing supplied by this font.</summary>
    /// <param name="spacing">The spacing category.</param><returns>Zero for an unmodified font.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The spacing category is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public virtual int GetSpacing(TextSpacingType spacing)
    {
        ThrowIfDisposed(); if (spacing is < TextSpacingType.Glyph or >= TextSpacingType.Max) throw new ArgumentOutOfRangeException(nameof(spacing)); return 0;
    }

    /// <summary>Returns this font's span-level OpenType feature overrides.</summary>
    /// <returns>An independent empty dictionary for a scalable font; file-level feature defaults are stored separately.</returns>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public virtual Dictionary<string, int> GetOpenTypeFeatures()
    {
        ThrowIfDisposed(); return new(StringComparer.Ordinal);
    }

    /// <summary>Creates a caller-owned font variation borrowing this font and using independent native caches.</summary>
    /// <param name="variationCoordinates">Optional copied numeric OpenType design coordinates.</param>
    /// <param name="faceIndex">Nonnegative collection face index.</param><param name="strength">Finite synthetic embolden strength.</param>
    /// <param name="transform">Optional finite outline basis; translation is ignored.</param>
    /// <param name="spacingTop">Extra top pixels.</param><param name="spacingBottom">Extra bottom pixels.</param>
    /// <param name="spacingSpace">Extra space width.</param><param name="spacingGlyph">Extra glyph width.</param>
    /// <param name="baselineOffset">Finite fraction of native ascent plus descent.</param><param name="paletteIndex">Predefined palette index, clamped during realization.</param>
    /// <param name="customColors">Optional copied palette colors; transparent black preserves entries.</param>
    /// <returns>A new resource that the caller must dispose.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A configuration value is invalid.</exception>
    /// <exception cref="ObjectDisposedException">This font is disposed.</exception>
    public FontVariation FindVariation(Dictionary<uint, float>? variationCoordinates = null, int faceIndex = 0, float strength = 0,
        Transform? transform = null, int spacingTop = 0, int spacingBottom = 0, int spacingSpace = 0, int spacingGlyph = 0,
        float baselineOffset = 0, int paletteIndex = 0, Color[]? customColors = null)
    {
        ThrowIfDisposed(); var result = new FontVariation();
        try
        {
            result.BaseFont = this; result.VariationOpenType = variationCoordinates ?? []; result.VariationFaceIndex = faceIndex;
            result.VariationEmbolden = strength; result.VariationTransform = transform ?? Transform.Identity;
            result.SpacingTop = spacingTop; result.SpacingBottom = spacingBottom; result.SpacingSpace = spacingSpace; result.SpacingGlyph = spacingGlyph;
            result.BaselineOffset = baselineOffset; result.PaletteIndex = paletteIndex; result.PaletteCustomColors = customColors ?? []; return result;
        }
        catch { result.Dispose(); throw; }
    }

    /// <summary>Copies the primary face's OpenType axis design bounds.</summary><returns>Numeric tag keys with minimum, maximum and default coordinates. Keys retain the four-byte OpenType tags.</returns>
    /// <exception cref="ObjectDisposedException">The font or required source is disposed.</exception>
    public Dictionary<uint, FontVariationAxis> GetSupportedVariationList() { lock (FontGate) { ThrowIfDisposed(); return PrimaryData is { } data ? new(data.VariationAxes) : []; } }
    /// <summary>Returns the primary face's predefined palette count.</summary><returns>Zero without color palette data.</returns>
    /// <exception cref="ObjectDisposedException">The font or required source is disposed.</exception>
    public int GetPaletteCount() { lock (FontGate) { ThrowIfDisposed(); return PrimaryData?.Palettes.Length ?? 0; } }
    /// <summary>Copies a predefined palette, independently of instance overrides.</summary><param name="index">Valid palette index.</param><returns>Caller-owned colors.</returns>
    /// <exception cref="ObjectDisposedException">The font or required source is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The palette index is outside the available range.</exception>
    public Color[] GetPaletteColors(int index) { lock (FontGate) { ThrowIfDisposed(); var palettes = PrimaryData?.Palettes ?? []; if ((uint)index >= (uint)palettes.Length) throw new ArgumentOutOfRangeException(nameof(index)); return (Color[])palettes[index].Clone(); } }
    /// <summary>Reads a predefined palette's optional SFNT name.</summary><param name="index">Valid palette index.</param><returns>Palette name, or an empty string when unnamed.</returns>
    /// <exception cref="ObjectDisposedException">The font or required source is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The palette index is outside the available range.</exception>
    public string GetPaletteName(int index) { lock (FontGate) { ThrowIfDisposed(); var names = PrimaryData?.PaletteNames ?? []; if ((uint)index >= (uint)names.Length) throw new ArgumentOutOfRangeException(nameof(index)); return names[index]; } }

    /// <summary>Tests whether this font or a fallback contains a Unicode scalar.</summary>
    /// <param name="character">A Unicode scalar value.</param><returns>Whether a glyph is available.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The character is not a Unicode scalar.</exception>
    /// <exception cref="ObjectDisposedException">The font or a required fallback is disposed.</exception>
    public bool HasChar(int character)
    {
        ValidateCharacter(character);
        for (var attempt = 0; attempt < MaximumReadAttempts; attempt++)
        {
            using var read = BeginRead(); var result = FindSource((uint)character) is not null;
            if (read.IsCurrent) return result;
        }
        throw UnsettledRead();
    }
    /// <summary>Returns all supported Unicode characters, preserving face order and removing duplicates.</summary>
    /// <returns>A string containing the distinct supported scalars.</returns>
    /// <exception cref="ObjectDisposedException">The font or a required fallback is disposed.</exception>
    public string GetSupportedChars()
    {
        for (var attempt = 0; attempt < MaximumReadAttempts; attempt++)
        {
            using var read = BeginRead(); var result = new StringBuilder(); var seen = new HashSet<int>();
            foreach (var source in GetSources()) foreach (var rune in source.GetSupportedChars().EnumerateRunes())
                    if (seen.Add(rune.Value)) result.Append(rune.ToString());
            if (read.IsCurrent) return result.ToString();
        }
        throw UnsettledRead();
    }
    /// <summary>Measures one character without kerning or contextual shaping.</summary>
    /// <param name="character">A Unicode scalar value.</param><param name="fontSize">Positive font size.</param>
    /// <returns>Its advance and the font height, or zero when no face contains it.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The character or size is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The font or a required fallback is disposed.</exception>
    public Vector2 GetCharSize(int character, int fontSize)
    {
        ValidateCharacter(character); ValidateSize(fontSize);
        for (var attempt = 0; attempt < MaximumReadAttempts; attempt++)
        {
            using var read = BeginRead(); var source = FindSource((uint)character);
            var result = source is null ? Vector2.Zero : new Vector2(source.GetGlyphAdvance(source.GetGlyphIndex((uint)character), fontSize), GetHeight(fontSize));
            if (read.IsCurrent) return result;
        }
        throw UnsettledRead();
    }

    /// <summary>Sets the bounded least-recently-used layout cache capacities.</summary>
    /// <param name="singleLine">Number of single-line layouts retained; zero disables retention.</param>
    /// <param name="multiLine">Number of paragraph layouts retained; zero disables retention.</param>
    /// <exception cref="ArgumentOutOfRangeException">A capacity is negative.</exception>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void SetCacheCapacity(int singleLine, int multiLine)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(singleLine); ArgumentOutOfRangeException.ThrowIfNegative(multiLine);
        lock (FontGate)
        {
            ThrowIfDisposed(); _singleCapacity = singleLine; _multiCapacity = multiLine;
            if (_single.Count > singleLine) _single.RemoveRange(singleLine, _single.Count - singleLine);
            if (_multi.Count > multiLine) _multi.RemoveRange(multiLine, _multi.Count - multiLine);
        }
    }

    /// <summary>Measures a shaped single line, including kerning, bidi order and optional width trimming.</summary>
    /// <param name="text">The text to shape.</param><param name="alignment">Alignment along the advance axis.</param>
    /// <param name="width">Finite available width, or a negative value for unconstrained text.</param>
    /// <param name="fontSize">Positive logical font size.</param><param name="justificationFlags">Fill-spacing rules.</param>
    /// <param name="direction">Paragraph direction.</param><param name="orientation">Glyph advance axis.</param>
    /// <returns>The shaped line size in logical pixels.</returns>
    /// <exception cref="ArgumentNullException">The text is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Size, width or an enum value is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The font or a required fallback is disposed.</exception>
    public Vector2 GetStringSize(string text, HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1, int fontSize = 16,
        TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida | TextJustificationFlags.WordBound,
        TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal)
    {
        lock (FontGate) return GetLayout(text, alignment, width, fontSize, -1, TextLineBreakFlags.None, justificationFlags, direction, orientation, false).Size;
    }
    /// <summary>Measures shaped text wrapped into lines according to Unicode boundaries.</summary>
    /// <param name="text">The text to shape.</param><param name="alignment">Alignment along the advance axis.</param>
    /// <param name="width">Finite wrap width, or a negative value for unconstrained text.</param><param name="fontSize">Positive logical font size.</param>
    /// <param name="maxLines">Maximum visible lines, or a negative value for all lines.</param><param name="breakFlags">Line-break rules.</param>
    /// <param name="justificationFlags">Fill-spacing rules.</param><param name="direction">Paragraph direction.</param><param name="orientation">Glyph advance axis.</param>
    /// <returns>The visible paragraph size.</returns><exception cref="ArgumentNullException">The text is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Size, width or an enum value is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The font or a required fallback is disposed.</exception>
    public Vector2 GetMultilineStringSize(string text, HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1, int fontSize = 16,
        int maxLines = -1, TextLineBreakFlags breakFlags = TextLineBreakFlags.Mandatory | TextLineBreakFlags.WordBound,
        TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida | TextJustificationFlags.WordBound,
        TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal)
    {
        lock (FontGate) return GetLayout(text, alignment, width, fontSize, maxLines, breakFlags, justificationFlags, direction, orientation, true).Size;
    }

    /// <summary>Draws one unshaped Unicode character at a baseline position.</summary>
    /// <param name="canvasItem">The currently recording canvas item.</param><param name="position">The finite baseline position.</param>
    /// <param name="character">A Unicode scalar.</param><param name="fontSize">Positive logical font size.</param>
    /// <param name="modulate">Finite modulation, or null for white.</param><param name="oversampling">Positive raster scale, or a nonpositive value for automatic scale.</param>
    /// <returns>The character advance, or zero when missing.</returns>
    /// <exception cref="ArgumentNullException">The canvas is null.</exception><exception cref="ArgumentException">Geometry or color is nonfinite.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Character, font size or raster scale is invalid.</exception>
    /// <exception cref="InvalidOperationException">The canvas is not recording on its owner thread.</exception>
    /// <exception cref="ObjectDisposedException">A required resource or canvas item is disposed.</exception>
    public float DrawChar(CanvasItem canvasItem, Vector2 position, int character, int fontSize, Color? modulate = null, float oversampling = 0) =>
        DrawCharacter(canvasItem, position, character, fontSize, 0, modulate ?? Colors.White, oversampling);
    /// <summary>Draws the outline of one unshaped character at a baseline position.</summary>
    /// <param name="canvasItem">The currently recording canvas item.</param><param name="position">The finite baseline position.</param>
    /// <param name="character">A Unicode scalar.</param><param name="fontSize">Positive logical font size.</param>
    /// <param name="size">Outline radius; nonpositive values draw the unexpanded glyph.</param><param name="modulate">Finite modulation, or null for white.</param>
    /// <param name="oversampling">Positive raster scale, or a nonpositive value for automatic scale.</param><returns>The character advance.</returns>
    /// <exception cref="ArgumentNullException">The canvas is null.</exception><exception cref="ArgumentException">Geometry or color is nonfinite.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Character, font size or raster scale is invalid.</exception>
    /// <exception cref="InvalidOperationException">The canvas is not recording on its owner thread.</exception>
    /// <exception cref="ObjectDisposedException">A required resource or canvas item is disposed.</exception>
    public float DrawCharOutline(CanvasItem canvasItem, Vector2 position, int character, int fontSize, int size = -1, Color? modulate = null, float oversampling = 0) =>
        DrawCharacter(canvasItem, position, character, fontSize, Math.Max(0, size), modulate ?? Colors.White, oversampling, true);

    /// <summary>Draws a shaped single line at a baseline position.</summary>
    /// <param name="canvasItem">The currently recording canvas item.</param><param name="position">The finite baseline position.</param><param name="text">Text to shape.</param>
    /// <param name="alignment">Advance-axis alignment.</param><param name="width">Finite available width, or a negative value for no limit.</param>
    /// <param name="fontSize">Positive logical font size.</param><param name="modulate">Finite modulation, or null for white.</param>
    /// <param name="justificationFlags">Fill-spacing rules.</param><param name="direction">Paragraph direction.</param><param name="orientation">Advance axis.</param>
    /// <param name="oversampling">Positive raster scale, or a nonpositive value for automatic scale.</param>
    /// <exception cref="ArgumentNullException">Text or canvas is null.</exception><exception cref="ArgumentException">Geometry or color is nonfinite.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Size, width, raster scale or an enum value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The canvas is not recording on its owner thread.</exception>
    /// <exception cref="ObjectDisposedException">A required resource or canvas item is disposed.</exception>
    public void DrawString(CanvasItem canvasItem, Vector2 position, string text, HorizontalAlignment alignment = HorizontalAlignment.Left,
        float width = -1, int fontSize = 16, Color? modulate = null,
        TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida | TextJustificationFlags.WordBound,
        TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal, float oversampling = 0) =>
        DrawText(canvasItem, position, text, alignment, width, fontSize, -1, 0, modulate ?? Colors.White, TextLineBreakFlags.None, justificationFlags, direction, orientation, oversampling, false);
    /// <summary>Draws the outline of a shaped single line at a baseline position.</summary>
    /// <param name="canvasItem">The currently recording canvas item.</param><param name="position">The finite baseline position.</param><param name="text">Text to shape.</param>
    /// <param name="alignment">Advance-axis alignment.</param><param name="width">Finite available width, or a negative value for no limit.</param>
    /// <param name="fontSize">Positive logical font size.</param><param name="modulate">Finite modulation, or null for white.</param>
    /// <param name="justificationFlags">Fill-spacing rules.</param><param name="direction">Paragraph direction.</param><param name="orientation">Advance axis.</param>
    /// <param name="oversampling">Positive raster scale, or a nonpositive value for automatic scale.</param>
    /// <exception cref="ArgumentNullException">Text or canvas is null.</exception><exception cref="ArgumentException">Geometry or color is nonfinite.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Size, width, raster scale or an enum value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The canvas is not recording on its owner thread.</exception>
    /// <exception cref="ObjectDisposedException">A required resource or canvas item is disposed.</exception>
    /// <param name="size">Outline radius; nonpositive values draw the unexpanded glyph.</param>
    public void DrawStringOutline(CanvasItem canvasItem, Vector2 position, string text, HorizontalAlignment alignment = HorizontalAlignment.Left,
        float width = -1, int fontSize = 16, int size = 1, Color? modulate = null,
        TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida | TextJustificationFlags.WordBound,
        TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal, float oversampling = 0) =>
        DrawText(canvasItem, position, text, alignment, width, fontSize, -1, Math.Max(0, size), modulate ?? Colors.White, TextLineBreakFlags.None, justificationFlags, direction, orientation, oversampling, false, true);
    /// <summary>Draws a Unicode-wrapped paragraph with the first line at the supplied baseline.</summary>
    /// <param name="canvasItem">The currently recording canvas item.</param><param name="position">The finite baseline position.</param><param name="text">Text to shape.</param>
    /// <param name="alignment">Advance-axis alignment.</param><param name="width">Finite available width, or a negative value for no limit.</param>
    /// <param name="fontSize">Positive logical font size.</param><param name="modulate">Finite modulation, or null for white.</param>
    /// <param name="justificationFlags">Fill-spacing rules.</param><param name="direction">Paragraph direction.</param><param name="orientation">Advance axis.</param>
    /// <param name="oversampling">Positive raster scale, or a nonpositive value for automatic scale.</param>
    /// <exception cref="ArgumentNullException">Text or canvas is null.</exception><exception cref="ArgumentException">Geometry or color is nonfinite.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Size, width, raster scale or an enum value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The canvas is not recording on its owner thread.</exception>
    /// <exception cref="ObjectDisposedException">A required resource or canvas item is disposed.</exception>
    /// <param name="maxLines">Maximum visible lines, or a negative value for all lines.</param><param name="breakFlags">Line-break rules.</param>
    public void DrawMultilineString(CanvasItem canvasItem, Vector2 position, string text, HorizontalAlignment alignment = HorizontalAlignment.Left,
        float width = -1, int fontSize = 16, int maxLines = -1, Color? modulate = null,
        TextLineBreakFlags breakFlags = TextLineBreakFlags.Mandatory | TextLineBreakFlags.WordBound,
        TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida | TextJustificationFlags.WordBound,
        TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal, float oversampling = 0) =>
        DrawText(canvasItem, position, text, alignment, width, fontSize, maxLines, 0, modulate ?? Colors.White, breakFlags, justificationFlags, direction, orientation, oversampling, true);
    /// <summary>Draws the outline of a Unicode-wrapped paragraph with its first line at the supplied baseline.</summary>
    /// <param name="canvasItem">The currently recording canvas item.</param><param name="position">The finite baseline position.</param><param name="text">Text to shape.</param>
    /// <param name="alignment">Advance-axis alignment.</param><param name="width">Finite available width, or a negative value for no limit.</param>
    /// <param name="fontSize">Positive logical font size.</param><param name="modulate">Finite modulation, or null for white.</param>
    /// <param name="justificationFlags">Fill-spacing rules.</param><param name="direction">Paragraph direction.</param><param name="orientation">Advance axis.</param>
    /// <param name="oversampling">Positive raster scale, or a nonpositive value for automatic scale.</param>
    /// <exception cref="ArgumentNullException">Text or canvas is null.</exception><exception cref="ArgumentException">Geometry or color is nonfinite.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Size, width, raster scale or an enum value is invalid.</exception>
    /// <exception cref="InvalidOperationException">The canvas is not recording on its owner thread.</exception>
    /// <exception cref="ObjectDisposedException">A required resource or canvas item is disposed.</exception>
    /// <param name="maxLines">Maximum visible lines, or a negative value for all lines.</param><param name="breakFlags">Line-break rules.</param>
    /// <param name="size">Outline radius; nonpositive values draw the unexpanded glyph.</param>
    public void DrawMultilineStringOutline(CanvasItem canvasItem, Vector2 position, string text, HorizontalAlignment alignment = HorizontalAlignment.Left,
        float width = -1, int fontSize = 16, int maxLines = -1, int size = 1, Color? modulate = null,
        TextLineBreakFlags breakFlags = TextLineBreakFlags.Mandatory | TextLineBreakFlags.WordBound,
        TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida | TextJustificationFlags.WordBound,
        TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal, float oversampling = 0) =>
        DrawText(canvasItem, position, text, alignment, width, fontSize, maxLines, Math.Max(0, size), modulate ?? Colors.White, breakFlags, justificationFlags, direction, orientation, oversampling, true, true);

    private float DrawCharacter(CanvasItem canvas, Vector2 position, int character, int size, int outline, Color color, float oversampling, bool outlinePass = false)
    {
        ValidateCharacter(character); ValidateSize(size); ValidateDraw(canvas, position, color, oversampling);
        for (var attempt = 0; attempt < MaximumReadAttempts; attempt++)
        {
            using var read = BeginRead(); var source = FindSource((uint)character);
            if (source is null) { if (read.IsCurrent) return 0; continue; }
            var glyph = source.GetGlyphIndex((uint)character);
            var image = source.GetGlyph(glyph, size, outline, Math.Max(0, oversampling), position, out var rasterPosition);
            var advance = source.GetGlyphAdvance(glyph, size); var modulate = !outlinePass && source.ModulateColorGlyphs;
            if (!read.IsCurrent) continue;
            TextLayout.DrawGlyph(canvas, image, rasterPosition, color, modulate);
            return advance;
        }
        throw UnsettledRead();
    }
    private void DrawText(CanvasItem canvas, Vector2 position, string text, HorizontalAlignment alignment, float width, int size, int maxLines,
        int outline, Color color, TextLineBreakFlags breaks, TextJustificationFlags justification, TextDirection direction, TextOrientation orientation, float oversampling, bool multiline, bool outlinePass = false)
    {
        ValidateDraw(canvas, position, color, oversampling);
        lock (FontGate)
        {
            var layout = GetLayout(text, alignment, width, size, maxLines, breaks, justification, direction, orientation, multiline);
            if (outline >= 0) layout.Draw(canvas, position, color, outline, Math.Max(0, oversampling), outlinePass: outlinePass);
        }
    }
    internal static void ValidateDraw(CanvasItem canvas, Vector2 position, Color color, float oversampling)
    {
        ArgumentNullException.ThrowIfNull(canvas); canvas.ValidateStyleDraw(new(position, Vector2.Zero));
        if (!color.IsFinite()) throw new ArgumentException("Text color must be finite.", nameof(color));
        if (!float.IsFinite(oversampling)) throw new ArgumentOutOfRangeException(nameof(oversampling));
    }
    internal TextLayout GetLayout(string text, HorizontalAlignment alignment, float width, int size, int maxLines, TextLineBreakFlags breaks,
        TextJustificationFlags justification, TextDirection direction, TextOrientation orientation, bool multiline)
    {
        ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(text); ValidateSize(size);
        if (!float.IsFinite(width)) throw new ArgumentOutOfRangeException(nameof(width));
        if (alignment is < HorizontalAlignment.Left or > HorizontalAlignment.Fill) throw new ArgumentOutOfRangeException(nameof(alignment));
        if (direction is < TextDirection.Auto or > TextDirection.Inherited) throw new ArgumentOutOfRangeException(nameof(direction));
        if (orientation is < TextOrientation.Horizontal or > TextOrientation.Vertical) throw new ArgumentOutOfRangeException(nameof(orientation));
        EnsureSourcesCurrent();
        var key = new TextLayoutKey(text, size, width, alignment, maxLines, breaks, justification, direction, orientation, multiline);
        var cache = multiline ? _multi : _single; var capacity = multiline ? _multiCapacity : _singleCapacity;
        foreach (var entry in cache)
        {
            if (entry.IsBusy || entry.Key != key) continue;
            if (entry.Generation != _generation) { entry.Generation = -1; entry.Build(this, key); entry.Generation = _generation; }
            entry.LastAccess = ++_access; return entry;
        }
        TextLayout result;
        if (capacity == 0) result = _scratch.IsBusy ? (ReadContextAt(_readDepth).Scratch ??= new()) : _scratch;
        else if (cache.Count < capacity) { result = new(); cache.Add(result); }
        else
        {
            TextLayout? available = null;
            foreach (var entry in cache) if (!entry.IsBusy && (available is null || entry.LastAccess < available.LastAccess)) available = entry;
            result = available ?? (ReadContextAt(_readDepth).Scratch ??= new());
        }
        result.Generation = -1; result.Build(this, key); result.Generation = _generation; result.LastAccess = ++_access; return result;
    }
    internal long GetContentGeneration()
    {
        lock (FontGate) { ThrowIfDisposed(); EnsureSourcesCurrent(); return _generation; }
    }
    internal List<FontData> GetSources()
    {
        if (_readDepth != 0) return _reads[_readDepth - 1].Sources;
        EnsureSourcesCurrent(); return _sources;
    }

    internal sealed class ReadContext
    {
        internal readonly List<FontData> Sources = [];
        internal readonly List<Font> Dependencies = [];
        internal readonly List<long> Generations = [];
        internal long Generation;
        internal TextLayout? Scratch;
    }
    internal readonly struct ReadScope(Font owner, ReadContext context) : IDisposable
    {
        internal long Generation => context.Generation;
        internal bool IsCurrent
        {
            get
            {
                owner.ThrowIfDisposed();
                for (var i = 0; i < context.Dependencies.Count; i++)
                {
                    var dependency = context.Dependencies[i]; dependency.ThrowIfDisposed();
                    if (Volatile.Read(ref dependency._generation) != context.Generations[i]) return false;
                }
                return true;
            }
        }
        public void Dispose()
        {
            try { ReleaseContext(context, context.Sources.Count); }
            finally { owner._readDepth--; Monitor.Exit(owner.FontGate); }
        }
    }
    private ReadContext ReadContextAt(int depth)
    {
        if (depth >= MaximumReadAttempts) throw new InvalidOperationException("Font read reentry exceeds sixty-four levels.");
        while (_reads.Count <= depth) _reads.Add(new());
        return _reads[depth];
    }
    internal ReadScope BeginRead()
    {
        Monitor.Enter(FontGate);
        ReadContext? context = null; var acquired = 0;
        try
        {
            ThrowIfDisposed(); context = ReadContextAt(_readDepth);
            for (var attempt = 0; attempt < MaximumReadAttempts; attempt++)
            {
                EnsureSourcesCurrent();
                context.Generation = _generation; context.Sources.AddRange(_sources);
                context.Dependencies.AddRange(_dependencies); context.Generations.AddRange(_dependencyGenerations);
                while (acquired < context.Sources.Count && context.Sources[acquired].TryAcquireRead()) acquired++;
                if (acquired == context.Sources.Count) { _readDepth++; return new(this, context); }
                var releaseCount = acquired; acquired = 0; ReleaseContext(context, releaseCount); ClearLayoutCaches();
            }
            throw UnsettledRead();
        }
        catch
        {
            try { if (context is not null) ReleaseContext(context, acquired); }
            finally { Monitor.Exit(FontGate); }
            throw;
        }
    }
    private static void ReleaseContext(ReadContext context, int acquired)
    {
        List<Exception>? errors = null;
        for (var i = 0; i < acquired; i++)
            try { context.Sources[i].ReleaseRead(); } catch (Exception error) { (errors ??= []).Add(error); }
        context.Sources.Clear(); context.Dependencies.Clear(); context.Generations.Clear();
        if (errors is not null) throw new AggregateException("Font read cleanup failed.", errors);
    }
    internal static InvalidOperationException UnsettledRead() => new("Font content did not settle after sixty-four read attempts.");
    private void EnsureSourcesCurrent()
    {
        if (!_sourcesDirty)
        {
            for (var i = 0; i < _dependencies.Count; i++)
            {
                var dependency = _dependencies[i];
                dependency.ThrowIfDisposed();
                if (Volatile.Read(ref dependency._generation) != _dependencyGenerations[i])
                {
                    ClearLayoutCaches(); break;
                }
            }
        }
        if (!_sourcesDirty) return;
        _sources.Clear(); _dependencies.Clear(); _dependencyGenerations.Clear();
        CollectSources(this, 0); _sourcesDirty = false;
    }
    private void CollectSources(Font font, int depth)
    {
        if (depth > 64) throw new InvalidOperationException("Font fallback depth exceeds sixty-four levels.");
        if (_dependencies.Contains(font)) return;
        lock (font.FontGate)
        {
            font.ThrowIfDisposed(); _dependencies.Add(font); _dependencyGenerations.Add(font._generation);
            var data = font.PrimaryData;
            if (data is { HasData: true } && !_sources.Contains(data)) _sources.Add(data);
            if (font.BaseDependency is { } dependency) CollectDependency(dependency, depth + 1);
            foreach (var fallback in font.EffectiveFallbacks) if (fallback is not null) CollectSources(fallback, depth + 1);
        }
    }
    private void CollectDependency(Font font, int depth)
    {
        if (depth > 64) throw new InvalidOperationException("Font base depth exceeds sixty-four levels.");
        if (_dependencies.Contains(font)) return;
        lock (font.FontGate) { font.ThrowIfDisposed(); _dependencies.Add(font); _dependencyGenerations.Add(font._generation); if (font.BaseDependency is { } parent) CollectDependency(parent, depth + 1); }
    }
    private FontData? FindSource(uint character)
    {
        foreach (var source in GetSources()) if (source.GetGlyphIndex(character) != 0) return source; return null;
    }
    private float GetMetric(int size, int kind)
    {
        ValidateSize(size);
        for (var attempt = 0; attempt < MaximumReadAttempts; attempt++)
        {
            using var read = BeginRead(); var result = 0f;
            foreach (var source in GetSources())
            {
                var m = source.GetMetrics(size);
                result = Math.Max(result, kind switch { 0 => m.Ascent, 1 => m.Descent, 2 => m.Height, 3 => m.UnderlinePosition, _ => m.UnderlineThickness });
            }
            if (kind is 0 or 2 or 3) result += GetSpacing(TextSpacingType.Top);
            if (kind is 1 or 2) result += GetSpacing(TextSpacingType.Bottom);
            if (read.IsCurrent) return result;
        }
        throw UnsettledRead();
    }
    private static void ValidateCharacter(int character)
    {
        if (!Rune.IsValid(character)) throw new ArgumentOutOfRangeException(nameof(character));
    }
    internal static void ValidateSize(int size) { ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size); }
    internal static void ValidateFallback(Font font, Font target, int depth)
    {
        if (ReferenceEquals(font, target) || depth > 64) throw new ArgumentException("Font fallback graphs must be acyclic and at most sixty-four levels deep.", "value");
        lock (font.FontGate) { font.ThrowIfDisposed(); if (font.BaseDependency is { } parent) ValidateFallback(parent, target, depth + 1); foreach (var child in font._fallbacks) if (child is not null) ValidateFallback(child, target, depth + 1); }
    }
    private void SubscribeFallbacks()
    {
        for (var i = 0; i < _fallbacks.Length; i++)
        {
            var font = _fallbacks[i]; if (font is null || Array.IndexOf(_fallbacks, font) != i) continue;
            font.Changed += _fallbackChanged; font.Disposed += _fallbackDisposed;
        }
    }
    private void UnsubscribeFallbacks()
    {
        for (var i = 0; i < _fallbacks.Length; i++)
        {
            var font = _fallbacks[i]; if (font is null || Array.IndexOf(_fallbacks, font) != i) continue;
            font.Changed -= _fallbackChanged; font.Disposed -= _fallbackDisposed;
        }
    }
    private void FallbackChanged(Resource _) { if (!IsDisposed) InvalidateFont(); }
    private void FallbackDisposed(ElectronObject _) { if (!IsDisposed) InvalidateFont(); }
    internal void InvalidateFont()
    {
        lock (FontGate) { ThrowIfDisposed(); ClearLayoutCaches(); }
        EmitChanged();
    }
    internal void InvalidateFontStateLocked()
    {
        if (!Monitor.IsEntered(FontGate)) throw new InvalidOperationException("Font state invalidation requires its state lock.");
        ThrowIfDisposed(); ClearLayoutCaches();
    }
    private void ClearLayoutCaches() { _sourcesDirty = true; Interlocked.Increment(ref _generation); }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property; yield return FallbackProperty;
    }
    /// <inheritdoc />
    protected override void OnResetState() { lock (FontGate) ClearLayoutCaches(); base.OnResetState(); }
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var fallbacks = Fallbacks;
        if (deep) for (var i = 0; i < fallbacks.Length; i++) fallbacks[i] = (Font?)duplicateSubresource(fallbacks[i]);
        ((Font)target).Fallbacks = fallbacks;
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) lock (FontGate) { UnsubscribeFallbacks(); _fallbacks = []; _sources.Clear(); _dependencies.Clear(); _dependencyGenerations.Clear(); _single.Clear(); _multi.Clear(); }
        base.Dispose(disposing);
    }
}
