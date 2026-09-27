namespace Electron2D;

/// <summary>Aligns content along a horizontal or text-advance axis.</summary>
public enum HorizontalAlignment
{
    /// <summary>Aligns to the start of the available axis.</summary>
    Left = 0,
    /// <summary>Centers in the available axis.</summary>
    Center = 1,
    /// <summary>Aligns to the end of the available axis.</summary>
    Right = 2,
    /// <summary>Expands eligible spacing to fill the available axis.</summary>
    Fill = 3
}

/// <summary>Selects a paragraph's base writing direction.</summary>
public enum TextDirection
{
    /// <summary>Uses the first strong directional character, falling back to the text consumer's language policy when none is present.</summary>
    Auto = 0,
    /// <summary>Uses left-to-right paragraph order.</summary>
    LTR = 1,
    /// <summary>Uses right-to-left paragraph order.</summary>
    RTL = 2,
    /// <summary>Uses the consumer's inherited direction, or automatic direction without a consumer.</summary>
    Inherited = 3
}

/// <summary>Selects the axis along which glyphs advance.</summary>
public enum TextOrientation
{
    /// <summary>Glyphs advance horizontally.</summary>
    Horizontal = 0,
    /// <summary>Glyphs advance vertically using vertical font metrics and substitutions.</summary>
    Vertical = 1
}

/// <summary>Controls how a line expands to its requested width.</summary>
[Flags]
public enum TextJustificationFlags
{
    /// <summary>Does not expand glyph spacing.</summary>
    None = 0,
    /// <summary>Allows joining-script elongation.</summary>
    Kashida = 1,
    /// <summary>Allows expansion at word boundaries.</summary>
    WordBound = 2,
    /// <summary>Excludes spaces at line edges.</summary>
    TrimEdgeSpaces = 4,
    /// <summary>Expands only content following the last tab.</summary>
    AfterLastTab = 8,
    /// <summary>Preserves space occupied by an ellipsis.</summary>
    ConstrainEllipsis = 16,
    /// <summary>Does not justify the paragraph's final line.</summary>
    SkipLastLine = 32,
    /// <summary>Does not justify its last line containing visible content.</summary>
    SkipLastLineWithVisibleChars = 64,
    /// <summary>Justifies a one-line paragraph even when final lines are otherwise skipped.</summary>
    DoNotSkipSingleLine = 128
}

/// <summary>Controls mandatory and width-dependent text line breaks.</summary>
[Flags]
public enum TextLineBreakFlags
{
    /// <summary>Does not break the text.</summary>
    None = 0,
    /// <summary>Breaks at mandatory Unicode separators.</summary>
    Mandatory = 1,
    /// <summary>Allows Unicode word-boundary line breaks.</summary>
    WordBound = 2,
    /// <summary>Allows breaks at grapheme boundaries.</summary>
    GraphemeBound = 4,
    /// <summary>Falls back to grapheme boundaries when a word cannot fit.</summary>
    Adaptive = 8,
    /// <summary>Trims both start and end spaces.</summary>
    TrimEdgeSpaces = 16,
    /// <summary>Trims indentation after the initial line.</summary>
    TrimIndent = 32,
    /// <summary>Trims spaces at line starts.</summary>
    TrimStartEdgeSpaces = 64,
    /// <summary>Trims spaces at line ends.</summary>
    TrimEndEdgeSpaces = 128
}

/// <summary>Identifies additional spacing applied by a font.</summary>
public enum TextSpacingType
{
    /// <summary>Spacing after ordinary glyphs.</summary>
    Glyph = 0,
    /// <summary>Spacing after word spaces.</summary>
    Space = 1,
    /// <summary>Spacing above the baseline's ascent.</summary>
    Top = 2,
    /// <summary>Spacing below the baseline's descent.</summary>
    Bottom = 3,
    /// <summary>Number of spacing categories.</summary>
    Max = 4
}

/// <summary>Describes a font face's intrinsic style.</summary>
[Flags]
public enum FontStyle
{
    /// <summary>A regular proportional face.</summary>
    None = 0,
    /// <summary>A bold face.</summary>
    Bold = 1,
    /// <summary>An italic or oblique face.</summary>
    Italic = 2,
    /// <summary>A monospaced face.</summary>
    FixedWidth = 4
}

/// <summary>Aligns text vertically inside a control.</summary>
public enum VerticalAlignment
{
    /// <summary>Aligns the first line to the top.</summary>
    Top = 0,
    /// <summary>Centers the visible lines.</summary>
    Center = 1,
    /// <summary>Aligns the last visible line to the bottom.</summary>
    Bottom = 2,
    /// <summary>Distributes remaining height between visible lines.</summary>
    Fill = 3
}

/// <summary>Selects width-dependent label wrapping.</summary>
public enum TextAutowrapMode
{
    /// <summary>Uses only explicit line separators.</summary>
    Off = 0,
    /// <summary>Allows breaks at grapheme boundaries.</summary>
    Arbitrary = 1,
    /// <summary>Allows Unicode word-boundary line breaks.</summary>
    Word = 2,
    /// <summary>Uses word boundaries and falls back to grapheme boundaries for oversized words.</summary>
    WordSmart = 3
}

/// <summary>Controls trimming of text that exceeds its available width.</summary>
public enum TextOverrunBehavior
{
    /// <summary>Preserves the complete text.</summary>
    NoTrimming = 0,
    /// <summary>Removes complete character clusters.</summary>
    TrimChar = 1,
    /// <summary>Removes complete words.</summary>
    TrimWord = 2,
    /// <summary>Trims clusters and adds an ellipsis when at least six glyphs remain.</summary>
    TrimEllipsis = 3,
    /// <summary>Trims words and adds an ellipsis when at least six glyphs remain.</summary>
    TrimWordEllipsis = 4,
    /// <summary>Always includes the ellipsis while trimming clusters.</summary>
    TrimEllipsisForce = 5,
    /// <summary>Always includes the ellipsis while trimming words.</summary>
    TrimWordEllipsisForce = 6
}

/// <summary>Selects whether partial text is limited before shaping or during glyph recording.</summary>
public enum TextVisibleCharactersBehavior
{
    /// <summary>Shapes only the visible logical characters.</summary>
    CharsBeforeShaping = 0,
    /// <summary>Shapes complete text, then hides clusters beyond the logical character limit.</summary>
    CharsAfterShaping = 1,
    /// <summary>Shows a proportion of glyphs in the control's layout direction.</summary>
    GlyphsAuto = 2,
    /// <summary>Shows glyphs from left to right.</summary>
    GlyphsLTR = 3,
    /// <summary>Shows glyphs from right to left.</summary>
    GlyphsRTL = 4
}

/// <summary>Selects independent bidirectional contexts for structured strings.</summary>
public enum StructuredTextParser
{
    /// <summary>Uses one inherited paragraph context.</summary>
    Default = 0,
    /// <summary>Separates URI components and gives separators a left-to-right context.</summary>
    URI = 1,
    /// <summary>Separates file-path components.</summary>
    File = 2,
    /// <summary>Separates the address local part and dotted domain components.</summary>
    Email = 3,
    /// <summary>Separates list fields using one supplied string delimiter.</summary>
    List = 4,
    /// <summary>Invokes the control's typed custom parser.</summary>
    Custom = 6
}

/// <summary>Identifies one independently resolved bidirectional context in logical Unicode-scalar coordinates.</summary>
/// <param name="Start">Inclusive scalar offset.</param><param name="End">Exclusive scalar offset.</param>
/// <param name="Direction">The context's direction; Inherited uses the surrounding paragraph direction.</param>
public readonly record struct TextBIDIRange(int Start, int End, TextDirection Direction);
