namespace Electron2D;

public sealed partial class TextServerDummy
{
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontClearGlyphs(RID fontRID, Vector2i size) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontClearKerningMap(RID fontRID, int size) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontClearSizeCache(RID fontRID) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontClearSystemFallbackCache() { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontClearTextures(RID fontRID, Vector2i size) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontDrawGlyph(RID fontRID, CanvasItem canvas, int size, Vector2 position, int index, Color? color = null, float oversampling = 0) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontDrawGlyphOutline(RID fontRID, CanvasItem canvas, int size, int outlineSize, Vector2 position, int index, Color? color = null, float oversampling = 0) { }
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override FontAntialiasing OnFontGetAntialiasing(RID fontRID) => default;
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override float OnFontGetAscent(RID fontRID, int size) => 0;
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override float OnFontGetBaselineOffset(RID fontRID) => 0;
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override int OnFontGetCharFromGlyphIndex(RID fontRID, int size, int glyphIndex) => 0;
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override float OnFontGetDescent(RID fontRID, int size) => 0;
    /// <summary>Returns the fixed disabled-profile value false.</summary>
    /// <inheritdoc />
    protected override bool OnFontGetDisableEmbeddedBitmaps(RID fontRID) => false;
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override float OnFontGetEmbolden(RID fontRID) => 0;
    /// <summary>Returns the fixed disabled-profile value 1.</summary>
    /// <inheritdoc />
    protected override int OnFontGetFaceCount(RID fontRID) => 1;
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override int OnFontGetFaceIndex(RID fontRID) => 0;
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override int OnFontGetFixedSize(RID fontRID) => 0;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override FixedSizeScaleMode OnFontGetFixedSizeScaleMode(RID fontRID) => default;
    /// <summary>Returns the fixed disabled-profile value false.</summary>
    /// <inheritdoc />
    protected override bool OnFontGetGenerateMIPMaps(RID fontRID) => false;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override Vector2 OnFontGetGlyphAdvance(RID fontRID, int size, int glyph) => default;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override FontGlyphContours OnFontGetGlyphContours(RID fontRID, int size, int index) => new([], [], false);
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override int OnFontGetGlyphIndex(RID fontRID, int size, int character, int variationSelector) => 0;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override int[] OnFontGetGlyphList(RID fontRID, Vector2i size) => [];
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override Vector2 OnFontGetGlyphOffset(RID fontRID, Vector2i size, int glyph) => default;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override Vector2 OnFontGetGlyphSize(RID fontRID, Vector2i size, int glyph) => default;
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override int OnFontGetGlyphTextureIndex(RID fontRID, Vector2i size, int glyph) => 0;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override RID OnFontGetGlyphTextureRID(RID fontRID, Vector2i size, int glyph) => default;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override Vector2 OnFontGetGlyphTextureSize(RID fontRID, Vector2i size, int glyph) => default;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override Rect2 OnFontGetGlyphUVRect(RID fontRID, Vector2i size, int glyph) => default;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override FontHinting OnFontGetHinting(RID fontRID) => default;
    /// <summary>Returns the fixed disabled-profile value true.</summary>
    /// <inheritdoc />
    protected override bool OnFontGetKeepRoundingRemainders(RID fontRID) => true;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override Vector2 OnFontGetKerning(RID fontRID, int size, Vector2i glyphPair) => default;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override Vector2i[] OnFontGetKerningList(RID fontRID, int size) => [];
    /// <summary>Returns the fixed disabled-profile value false.</summary>
    /// <inheritdoc />
    protected override bool OnFontGetLanguageSupportOverride(RID fontRID, string language) => false;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override string[] OnFontGetLanguageSupportOverrides(RID fontRID) => [];
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override int OnFontGetMSDFPixelRange(RID fontRID) => 0;
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override int OnFontGetMSDFSize(RID fontRID) => 0;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override string OnFontGetName(RID fontRID) => string.Empty;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override Dictionary<string, int> OnFontGetOpenTypeFeatureOverrides(RID fontRID) => [];
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override Dictionary<string, Dictionary<string, string>> OnFontGetOTNameStrings(RID fontRID) => [];
    /// <summary>Returns the fixed disabled-profile value -1.</summary>
    /// <inheritdoc />
    protected override float OnFontGetOversampling(RID fontRID) => -1;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override Color[] OnFontGetPaletteColors(RID fontRID, int index) => [];
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override int OnFontGetPaletteCount(RID fontRID) => 0;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override Color[] OnFontGetPaletteCustomColors(RID fontRID) => [];
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override string OnFontGetPaletteName(RID fontRID, int index) => string.Empty;
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override float OnFontGetScale(RID fontRID, int size) => 0;
    /// <summary>Returns the fixed disabled-profile value false.</summary>
    /// <inheritdoc />
    protected override bool OnFontGetScriptSupportOverride(RID fontRID, string script) => false;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override string[] OnFontGetScriptSupportOverrides(RID fontRID) => [];
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override FontSizeCacheInfo[] OnFontGetSizeCacheInfo(RID fontRID) => [];
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override Vector2i[] OnFontGetSizeCacheList(RID fontRID) => [];
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override int OnFontGetSpacing(RID fontRID, TextSpacingType spacing) => 0;
    /// <summary>Returns the fixed disabled-profile value 100.</summary>
    /// <inheritdoc />
    protected override int OnFontGetStretch(RID fontRID) => 100;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override FontStyle OnFontGetStyle(RID fontRID) => default;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override string OnFontGetStyleName(RID fontRID) => string.Empty;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override FontSubpixelPositioning OnFontGetSubpixelPositioning(RID fontRID) => default;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override string OnFontGetSupportedChars(RID fontRID) => string.Empty;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override int[] OnFontGetSupportedGlyphs(RID fontRID) => [];
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override int OnFontGetTextureCount(RID fontRID, Vector2i size) => 0;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override Image? OnFontGetTextureImage(RID fontRID, Vector2i size, int textureIndex) => null;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override int[] OnFontGetTextureOffsets(RID fontRID, Vector2i size, int textureIndex) => [];
    /// <summary>Returns the fixed disabled-profile value Transform.Identity.</summary>
    /// <inheritdoc />
    protected override Transform OnFontGetTransform(RID fontRID) => Transform.Identity;
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override float OnFontGetUnderlinePosition(RID fontRID, int size) => 0;
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override float OnFontGetUnderlineThickness(RID fontRID, int size) => 0;
    /// <summary>Returns the fixed disabled-profile value 0.</summary>
    /// <inheritdoc />
    protected override int OnFontGetUsedPalette(RID fontRID) => 0;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override Dictionary<uint, float> OnFontGetVariationCoordinates(RID fontRID) => [];
    /// <summary>Returns the fixed disabled-profile value 400.</summary>
    /// <inheritdoc />
    protected override int OnFontGetWeight(RID fontRID) => 400;
    /// <summary>Returns the fixed disabled-profile value false.</summary>
    /// <inheritdoc />
    protected override bool OnFontHasChar(RID fontRID, int character) => false;
    /// <summary>Returns the fixed disabled-profile value false.</summary>
    /// <inheritdoc />
    protected override bool OnFontIsAllowSystemFallback(RID fontRID) => false;
    /// <summary>Returns the fixed disabled-profile value false.</summary>
    /// <inheritdoc />
    protected override bool OnFontIsForceAutohinter(RID fontRID) => false;
    /// <summary>Returns the fixed disabled-profile value false.</summary>
    /// <inheritdoc />
    protected override bool OnFontIsLanguageSupported(RID fontRID, string language) => false;
    /// <summary>Returns the fixed disabled-profile value false.</summary>
    /// <inheritdoc />
    protected override bool OnFontIsModulateColorGlyphs(RID fontRID) => false;
    /// <summary>Returns the fixed disabled-profile value false.</summary>
    /// <inheritdoc />
    protected override bool OnFontIsMultichannelSignedDistanceField(RID fontRID) => false;
    /// <summary>Returns the fixed disabled-profile value false.</summary>
    /// <inheritdoc />
    protected override bool OnFontIsScriptSupported(RID fontRID, string script) => false;
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontRemoveGlyph(RID fontRID, Vector2i size, int glyph) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontRemoveKerning(RID fontRID, int size, Vector2i glyphPair) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontRemoveLanguageSupportOverride(RID fontRID, string language) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontRemoveScriptSupportOverride(RID fontRID, string script) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontRemoveSizeCache(RID fontRID, Vector2i size) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontRemoveTexture(RID fontRID, Vector2i size, int textureIndex) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontRenderGlyph(RID fontRID, Vector2i size, int index) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontRenderRange(RID fontRID, Vector2i size, int start, int end) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetAllowSystemFallback(RID fontRID, bool value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetAntialiasing(RID fontRID, FontAntialiasing value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetAscent(RID fontRID, int size, float value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetBaselineOffset(RID fontRID, float value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetData(RID fontRID, byte[] data) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetDescent(RID fontRID, int size, float value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetDisableEmbeddedBitmaps(RID fontRID, bool value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetEmbolden(RID fontRID, float value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetFaceIndex(RID fontRID, int value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetFixedSize(RID fontRID, int value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetFixedSizeScaleMode(RID fontRID, FixedSizeScaleMode value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetForceAutohinter(RID fontRID, bool value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetGenerateMIPMaps(RID fontRID, bool value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetGlyphAdvance(RID fontRID, int size, int glyph, Vector2 value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetGlyphOffset(RID fontRID, Vector2i size, int glyph, Vector2 value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetGlyphSize(RID fontRID, Vector2i size, int glyph, Vector2 value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetGlyphTextureIndex(RID fontRID, Vector2i size, int glyph, int textureIndex) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetGlyphUVRect(RID fontRID, Vector2i size, int glyph, Rect2 value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetHinting(RID fontRID, FontHinting value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetKeepRoundingRemainders(RID fontRID, bool value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetKerning(RID fontRID, int size, Vector2i glyphPair, Vector2 value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetLanguageSupportOverride(RID fontRID, string language, bool supported) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetModulateColorGlyphs(RID fontRID, bool value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetMSDFPixelRange(RID fontRID, int value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetMSDFSize(RID fontRID, int value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetMultichannelSignedDistanceField(RID fontRID, bool value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetName(RID fontRID, string value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetOpenTypeFeatureOverrides(RID fontRID, Dictionary<string, int> value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetOversampling(RID fontRID, float value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetPaletteCustomColors(RID fontRID, Color[] value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetScale(RID fontRID, int size, float value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetScriptSupportOverride(RID fontRID, string script, bool supported) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetSpacing(RID fontRID, TextSpacingType spacing, int value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetStretch(RID fontRID, int value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetStyle(RID fontRID, FontStyle value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetStyleName(RID fontRID, string value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetSubpixelPositioning(RID fontRID, FontSubpixelPositioning value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetTextureImage(RID fontRID, Vector2i size, int textureIndex, Image image) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetTextureOffsets(RID fontRID, Vector2i size, int textureIndex, int[] offsets) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetTransform(RID fontRID, Transform value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetUnderlinePosition(RID fontRID, int size, float value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetUnderlineThickness(RID fontRID, int size, float value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetUsedPalette(RID fontRID, int value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetVariationCoordinates(RID fontRID, Dictionary<uint, float> value) { }
    /// <summary>Accepts a disabled text request without retaining resources or changing text state.</summary>
    /// <inheritdoc />
    protected override void OnFontSetWeight(RID fontRID, int value) { }
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override Dictionary<uint, FontFeatureInfo> OnFontSupportedFeatureList(RID fontRID) => [];
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override Dictionary<uint, FontVariationAxis> OnFontSupportedVariationList(RID fontRID) => [];
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override RID OnCreateFont() => default;
    /// <summary>Returns an empty typed result.</summary>
    /// <inheritdoc />
    protected override RID OnCreateFontLinkedVariation(RID fontRID) => default;
}
