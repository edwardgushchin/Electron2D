namespace Electron2D;

public sealed partial class TextServerDummy
{
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override TextCaretInfo OnShapedTextGetCarets(RID shaped, int position) => default;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override ReadOnlySpan<Vector2> OnShapedTextGetSelection(RID shaped, int start, int end) => [];
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override int[] OnShapedTextGetCharacterBreaks(RID shaped) => [];
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override int OnShapedTextClosestCharacterPos(RID shaped, int position) => 0;
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override int OnShapedTextNextCharacterPos(RID shaped, int position) => 0;
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override int OnShapedTextPrevCharacterPos(RID shaped, int position) => 0;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override Vector2 OnShapedTextGetGraphemeBounds(RID shaped, int position) => default;
    /// <summary>Returns the fixed disabled-profile value position.</summary>
    /// <inheritdoc />
    protected override int OnShapedTextNextGraphemePos(RID shaped, int position) => position;
    /// <summary>Returns the fixed disabled-profile value position.</summary>
    /// <inheritdoc />
    protected override int OnShapedTextPrevGraphemePos(RID shaped, int position) => position;
    /// <summary>Returns the fixed disabled-profile value -1.</summary>
    /// <inheritdoc />
    protected override int OnShapedTextHitTestGrapheme(RID shaped, float coords) => -1;
    /// <summary>Returns the fixed disabled-profile value false.</summary>
    /// <inheritdoc />
    protected override bool OnShapedTextHasVisibleChars(RID shaped) => false;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override TextDirection OnShapedTextGetDominantDirectionInRange(RID shaped, int start, int end) => default;
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override float OnShapedTextFitToWidth(RID shaped, float width, TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida | TextJustificationFlags.WordBound) => 0;
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnShapedTextOverrunTrimToWidth(RID shaped, float width = 0, TextOverrunFlags overrunTrimFlags = TextOverrunFlags.NoTrim) { }
    /// <summary>Returns the fixed disabled-profile value -1.</summary>
    /// <inheritdoc />
    protected override int OnShapedTextGetTrimPos(RID shaped) => -1;
    /// <summary>Returns the fixed disabled-profile value -1.</summary>
    /// <inheritdoc />
    protected override int OnShapedTextGetEllipsisPos(RID shaped) => -1;
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnShapedTextSetCustomEllipsis(RID shaped, int character) { }
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override int OnShapedTextGetCustomEllipsis(RID shaped) => 0;
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnShapedTextSetCustomPunctuation(RID shaped, string punctuation) { }
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override string OnShapedTextGetCustomPunctuation(RID shaped) => string.Empty;
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnShapedTextSetSpacing(RID shaped, TextSpacingType spacing, int value) { }
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override int OnShapedTextGetSpacing(RID shaped, TextSpacingType spacing) => 0;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override ReadOnlySpan<TextGlyph> OnShapedTextGetGlyphs(RID shaped) => [];
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override ReadOnlySpan<TextGlyph> OnShapedTextSortLogical(RID shaped) => [];
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override ReadOnlySpan<TextGlyph> OnShapedTextGetEllipsisGlyphs(RID shaped) => [];
    /// <summary>Returns the fixed disabled-profile value -1.</summary>
    /// <inheritdoc />
    protected override int OnShapedTextGetEllipsisGlyphCount(RID shaped) => -1;
    /// <summary>Returns the fixed disabled-profile value -1.</summary>
    /// <inheritdoc />
    protected override int OnShapedTextGetObjectGlyph(RID shaped, TextObjectKey key) => -1;
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override int OnShapedGetRunCount(RID shaped) => 0;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override string OnShapedGetRunText(RID shaped, int index) => string.Empty;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override Vector2i OnShapedGetRunRange(RID shaped, int index) => default;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override Vector2i OnShapedGetRunGlyphRange(RID shaped, int index) => default;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override RID OnShapedGetRunFontRID(RID shaped, int index) => default;
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override int OnShapedGetRunFontSize(RID shaped, int index) => 0;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override string OnShapedGetRunLanguage(RID shaped, int index) => string.Empty;
    /// <summary>Returns the fixed disabled-profile value TextDirection.LTR.</summary>
    /// <inheritdoc />
    protected override TextDirection OnShapedGetRunDirection(RID shaped, int index) => TextDirection.LTR;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override TextObjectKey? OnShapedGetRunObject(RID shaped, int index) => null;
    /// <summary>Returns the fixed disabled-profile value false.</summary>
    /// <inheritdoc />
    protected override bool OnShapedTextUpdateBreaks(RID shaped) => false;
    /// <summary>Returns the fixed disabled-profile value false.</summary>
    /// <inheritdoc />
    protected override bool OnShapedTextUpdateJustificationOps(RID shaped) => false;
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnShapedSetSpanUpdateFont(RID shaped, int index, ReadOnlySpan<Font> fonts, int size, IReadOnlyDictionary<string, int>? openTypeFeatures = null) { }
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override int[] OnShapedTextGetWordBreaks(RID shaped, TextGraphemeFlags graphemeFlags = TextGraphemeFlags.Space | TextGraphemeFlags.Punctuation, TextGraphemeFlags skipGraphemeFlags = TextGraphemeFlags.Virtual) => [];
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override int[] OnShapedTextGetLineBreaks(RID shaped, float width, int start = 0, TextLineBreakFlags breakFlags = TextLineBreakFlags.Mandatory | TextLineBreakFlags.WordBound) => [];
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override int[] OnShapedTextGetLineBreaksAdv(RID shaped, ReadOnlySpan<float> width, int start = 0, bool once = true, TextLineBreakFlags breakFlags = TextLineBreakFlags.Mandatory | TextLineBreakFlags.WordBound) => [];
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override RID OnShapedTextSubstr(RID shaped, int start, int length) => default;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override RID OnShapedTextGetParent(RID shaped) => default;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override RID OnCreateShapedText(TextDirection direction = TextDirection.Auto, TextOrientation orientation = TextOrientation.Horizontal) => default;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override RID OnShapedTextDuplicate(RID shaped) => default;
    /// <summary>Returns the fixed disabled-profile value false.</summary>
    /// <inheritdoc />
    protected override bool OnShapedTextAddString(RID shaped, string text, ReadOnlySpan<Font> fonts, int size, IReadOnlyDictionary<string, int>? openTypeFeatures = null, string language = "") => false;
    /// <summary>Returns the fixed disabled-profile value false.</summary>
    /// <inheritdoc />
    protected override bool OnShapedTextAddString<T>(RID shaped, string text, ReadOnlySpan<Font> fonts, int size, IReadOnlyDictionary<string, int>? openTypeFeatures, string language, T metadata) => false;
    /// <summary>Returns the fixed disabled-profile value false.</summary>
    /// <inheritdoc />
    protected override bool OnShapedTextAddObject(RID shaped, TextObjectKey key, Vector2 size, InlineAlignment inlineAlign = InlineAlignment.Center, int length = 1, float baseline = 0) => false;
    /// <summary>Returns the fixed disabled-profile value false.</summary>
    /// <inheritdoc />
    protected override bool OnShapedTextResizeObject(RID shaped, TextObjectKey key, Vector2 size, InlineAlignment inlineAlign = InlineAlignment.Center, float baseline = 0) => false;
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnShapedTextClear(RID shaped) { }
    /// <summary>Returns the fixed disabled-profile value false.</summary>
    /// <inheritdoc />
    protected override bool OnShapedTextShape(RID shaped) => false;
    /// <summary>Returns the fixed disabled-profile value false.</summary>
    /// <inheritdoc />
    protected override bool OnShapedTextIsReady(RID shaped) => false;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override string OnShapedGetText(RID shaped) => string.Empty;
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override int OnShapedGetSpanCount(RID shaped) => 0;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override string OnShapedGetSpanText(RID shaped, int index) => string.Empty;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override TextObjectKey? OnShapedGetSpanObject(RID shaped, int index) => null;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override TextObjectKey? OnShapedGetSpanEmbeddedObject(RID shaped, int index) => null;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override Vector2i OnShapedTextGetRange(RID shaped) => default;
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override int OnShapedTextGetGlyphCount(RID shaped) => 0;
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override float OnShapedTextGetWidth(RID shaped) => 0;
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override float OnShapedTextGetAscent(RID shaped) => 0;
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override float OnShapedTextGetDescent(RID shaped) => 0;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override Vector2 OnShapedTextGetSize(RID shaped) => default;
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override float OnShapedTextGetUnderlinePosition(RID shaped) => 0;
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override float OnShapedTextGetUnderlineThickness(RID shaped) => 0;
    /// <summary>Returns the fixed disabled-profile value TextDirection.LTR.</summary>
    /// <inheritdoc />
    protected override TextDirection OnShapedTextGetInferredDirection(RID shaped) => TextDirection.LTR;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override TextDirection OnShapedTextGetDirection(RID shaped) => default;
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnShapedTextSetDirection(RID shaped, TextDirection direction = TextDirection.Auto) { }
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override TextOrientation OnShapedTextGetOrientation(RID shaped) => default;
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnShapedTextSetOrientation(RID shaped, TextOrientation orientation = TextOrientation.Horizontal) { }
    /// <summary>Returns the fixed disabled-profile value false.</summary>
    /// <inheritdoc />
    protected override bool OnShapedTextGetPreserveInvalid(RID shaped) => false;
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnShapedTextSetPreserveInvalid(RID shaped, bool enabled) { }
    /// <summary>Returns the fixed disabled-profile value false.</summary>
    /// <inheritdoc />
    protected override bool OnShapedTextGetPreserveControl(RID shaped) => false;
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnShapedTextSetPreserveControl(RID shaped, bool enabled) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnShapedTextSetBIDIOverride(RID shaped, ReadOnlySpan<TextBIDIRange> ranges) { }
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override float OnShapedTextTabAlign(RID shaped, ReadOnlySpan<float> tabStops) => 0;
    /// <summary>Returns the fixed disabled-profile value false.</summary>
    /// <inheritdoc />
    protected override bool OnShapedTextHasObject(RID shaped, TextObjectKey key) => false;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override TextObjectKey[] OnShapedTextGetObjects(RID shaped) => [];
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override Vector2i OnShapedTextGetObjectRange(RID shaped, TextObjectKey key) => default;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override Rect2 OnShapedTextGetObjectRect(RID shaped, TextObjectKey key) => default;
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override int OnShapedTextHitTestPosition(RID shaped, float coords) => 0;
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnShapedTextDraw(RID shaped, CanvasItem canvas, Vector2 position, float clipL = -1, float clipR = -1, Color? color = null, float oversampling = 0) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnShapedTextDrawOutline(RID shaped, CanvasItem canvas, Vector2 position, float clipL = -1, float clipR = -1, int outlineSize = 1, Color? color = null, float oversampling = 0) { }
}
