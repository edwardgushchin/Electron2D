namespace Electron2D;

public abstract partial class TextServer
{
    /// <summary>Queries or authors the shared clear glyphs cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontClearGlyphs(RID fontRID, Vector2i size) => SourceFont(fontRID).ClearGlyphs(0, size);
    /// <summary>Queries or authors the shared clear kerning map cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontClearKerningMap(RID fontRID, int size) => SourceFont(fontRID).ClearKerningMap(0, size);
    /// <summary>Queries or authors the shared clear size cache cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontClearSizeCache(RID fontRID) => SourceFont(fontRID).ClearSizeCache(0);
    /// <summary>Clears realized automatic fallback caches and invalidates all live font consumers.</summary>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontClearSystemFallbackCache() => Font.ClearSystemFontCaches();
    /// <summary>Queries or authors the shared clear textures cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontClearTextures(RID fontRID, Vector2i size) => SourceFont(fontRID).ClearTextures(0, size);
    /// <summary>Records a primary glyph at a baseline using the actual shared raster cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="canvas">Canvas item in its active recording scope.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="position">Finite baseline origin.</param>
    /// <param name="index">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="color">Finite drawing color, or null for white.</param>
    /// <param name="oversampling">Finite raster scale; nonpositive values select font or viewport policy.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontDrawGlyph(RID fontRID, CanvasItem canvas, int size, Vector2 position, int index, Color? color = null, float oversampling = 0) => SourceFont(fontRID).ServerDrawGlyph(canvas, size, 0, position, index, color ?? Colors.White, oversampling);
    /// <summary>Records a primary glyph stroke at a baseline using the actual shared raster cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="canvas">Canvas item in its active recording scope.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="outlineSize">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="position">Finite baseline origin.</param>
    /// <param name="index">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="color">Finite drawing color, or null for white.</param>
    /// <param name="oversampling">Finite raster scale; nonpositive values select font or viewport policy.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontDrawGlyphOutline(RID fontRID, CanvasItem canvas, int size, int outlineSize, Vector2 position, int index, Color? color = null, float oversampling = 0) => SourceFont(fontRID).ServerDrawGlyph(canvas, size, outlineSize, position, index, color ?? Colors.White, oversampling, true);
    /// <summary>Reads the shared font antialiasing setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public FontAntialiasing FontGetAntialiasing(RID fontRID) => SourceFont(fontRID).Antialiasing;
    /// <summary>Reads the size-cache ascent.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public float FontGetAscent(RID fontRID, int size) => SourceFont(fontRID).GetCacheAscent(0, size);
    /// <summary>Reads this font's independent baseline-height fraction.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public float FontGetBaselineOffset(RID fontRID) => FontEntry(fontRID).Baseline;
    /// <summary>Returns the first primary character mapped to a glyph, or zero.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyphIndex">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public int FontGetCharFromGlyphIndex(RID fontRID, int size, int glyphIndex) => SourceFont(fontRID).GetCharFromGlyphIndex(size, glyphIndex);
    /// <summary>Reads the size-cache descent.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public float FontGetDescent(RID fontRID, int size) => SourceFont(fontRID).GetCacheDescent(0, size);
    /// <summary>Reads the shared font disable embedded bitmaps setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public bool FontGetDisableEmbeddedBitmaps(RID fontRID) => SourceFont(fontRID).DisableEmbeddedBitmaps;
    /// <summary>Reads the shared font embolden.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public float FontGetEmbolden(RID fontRID) => SourceFont(fontRID).GetEmbolden(0);
    /// <summary>Copies or reads the primary font face count.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public int FontGetFaceCount(RID fontRID) => SourceFont(fontRID).GetFaceCount();
    /// <summary>Reads the shared font face index.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public int FontGetFaceIndex(RID fontRID) => SourceFont(fontRID).GetFaceIndex(0);
    /// <summary>Reads the shared font fixed size setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public int FontGetFixedSize(RID fontRID) => SourceFont(fontRID).FixedSize;
    /// <summary>Reads the shared font fixed size scale mode setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public FixedSizeScaleMode FontGetFixedSizeScaleMode(RID fontRID) => SourceFont(fontRID).FixedSizeScaleMode;
    /// <summary>Reads the shared font generate mipmaps setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public bool FontGetGenerateMIPMaps(RID fontRID) => SourceFont(fontRID).GenerateMIPMaps;
    /// <summary>Queries or authors the shared glyph advance cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public Vector2 FontGetGlyphAdvance(RID fontRID, int size, int glyph) => SourceFont(fontRID).ServerGlyphAdvance(size, glyph);
    /// <summary>Copies baseline-relative native Bézier points, contour ends and fill orientation.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="index">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public FontGlyphContours FontGetGlyphContours(RID fontRID, int size, int index) => SourceFont(fontRID).ServerContours(size, index);
    /// <summary>Resolves a Unicode scalar and optional variation selector to the primary glyph index.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="character">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="variationSelector">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public int FontGetGlyphIndex(RID fontRID, int size, int character, int variationSelector) => SourceFont(fontRID).GetGlyphIndex(size, character, variationSelector);
    /// <summary>Queries or authors the shared glyph list cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public int[] FontGetGlyphList(RID fontRID, Vector2i size) => SourceFont(fontRID).GetGlyphList(0, size);
    /// <summary>Reads the shared glyph offset.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public Vector2 FontGetGlyphOffset(RID fontRID, Vector2i size, int glyph) => SourceFont(fontRID).ServerGlyphOffset(size, glyph);
    /// <summary>Reads the shared glyph size.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public Vector2 FontGetGlyphSize(RID fontRID, Vector2i size, int glyph) => SourceFont(fontRID).ServerGlyphSize(size, glyph);
    /// <summary>Reads the shared glyph texture idx.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public int FontGetGlyphTextureIndex(RID fontRID, Vector2i size, int glyph) => SourceFont(fontRID).ServerGlyphTextureIndex(size, glyph);
    /// <summary>Returns the real borrowed renderer atlas texture identity, or an empty identity.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public RID FontGetGlyphTextureRID(RID fontRID, Vector2i size, int glyph) => SourceFont(fontRID).ServerGlyphTexture(size, glyph);
    /// <summary>Returns actual renderer atlas dimensions, or zero.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public Vector2 FontGetGlyphTextureSize(RID fontRID, Vector2i size, int glyph) => SourceFont(fontRID).ServerGlyphTextureSize(size, glyph);
    /// <summary>Reads the shared glyph uv rect.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public Rect2 FontGetGlyphUVRect(RID fontRID, Vector2i size, int glyph) => SourceFont(fontRID).ServerGlyphUVRect(size, glyph);
    /// <summary>Reads the shared font hinting setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public FontHinting FontGetHinting(RID fontRID) => SourceFont(fontRID).Hinting;
    /// <summary>Reads the shared font keep rounding remainders setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public bool FontGetKeepRoundingRemainders(RID fontRID) => SourceFont(fontRID).KeepRoundingRemainders;
    /// <summary>Queries or authors the shared kerning cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyphPair">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public Vector2 FontGetKerning(RID fontRID, int size, Vector2i glyphPair) => SourceFont(fontRID).GetKerning(0, size, glyphPair);
    /// <summary>Queries or authors the shared kerning list cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public Vector2i[] FontGetKerningList(RID fontRID, int size) => SourceFont(fontRID).GetKerningList(0, size);
    /// <summary>Queries or authors the shared language support policy.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="language">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public bool FontGetLanguageSupportOverride(RID fontRID, string language) => SourceFont(fontRID).GetLanguageSupportOverride(language);
    /// <summary>Queries or authors the shared language support policy.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public string[] FontGetLanguageSupportOverrides(RID fontRID) => SourceFont(fontRID).GetLanguageSupportOverrides();
    /// <summary>Reads the shared font msdf pixel range setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public int FontGetMSDFPixelRange(RID fontRID) => SourceFont(fontRID).MSDFPixelRange;
    /// <summary>Reads the shared font msdf size setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public int FontGetMSDFSize(RID fontRID) => SourceFont(fontRID).MSDFSize;
    /// <summary>Reads the shared font name setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public string FontGetName(RID fontRID) => SourceFont(fontRID).FontName;
    /// <summary>Reads the shared font opentype feature overrides setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public Dictionary<string, int> FontGetOpenTypeFeatureOverrides(RID fontRID) => SourceFont(fontRID).OpenTypeFeatureOverrides;
    /// <summary>Copies or reads the primary font ot name strings.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public Dictionary<string, Dictionary<string, string>> FontGetOTNameStrings(RID fontRID) => SourceFont(fontRID).GetOTNameStrings();
    /// <summary>Reads the shared font oversampling setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public float FontGetOversampling(RID fontRID) => SourceFont(fontRID).Oversampling;
    /// <summary>Copies or reads the primary font palette colors.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="index">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public Color[] FontGetPaletteColors(RID fontRID, int index) => SourceFont(fontRID).GetPaletteColors(index);
    /// <summary>Copies or reads the primary font palette count.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public int FontGetPaletteCount(RID fontRID) => SourceFont(fontRID).GetPaletteCount();
    /// <summary>Reads the shared font palette custom colors setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public Color[] FontGetPaletteCustomColors(RID fontRID) => SourceFont(fontRID).ServerCustomColors;
    /// <summary>Copies or reads the primary font palette name.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="index">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public string FontGetPaletteName(RID fontRID, int index) => SourceFont(fontRID).GetPaletteName(index);
    /// <summary>Reads the size-cache scale.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public float FontGetScale(RID fontRID, int size) => SourceFont(fontRID).GetCacheScale(0, size);
    /// <summary>Queries or authors the shared script support policy.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="script">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public bool FontGetScriptSupportOverride(RID fontRID, string script) => SourceFont(fontRID).GetScriptSupportOverride(script);
    /// <summary>Queries or authors the shared script support policy.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public string[] FontGetScriptSupportOverrides(RID fontRID) => SourceFont(fontRID).GetScriptSupportOverrides();
    /// <summary>Copies actual nominal size-cache counts, atlas storage estimates and realized raster scales.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public FontSizeCacheInfo[] FontGetSizeCacheInfo(RID fontRID) => SourceFont(fontRID).ServerCacheInfo();
    /// <summary>Queries or authors the shared size cache list cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public Vector2i[] FontGetSizeCacheList(RID fontRID) => SourceFont(fontRID).GetSizeCacheList(0);
    /// <summary>Reads this root or linked font's independent signed spacing.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="spacing">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public int FontGetSpacing(RID fontRID, TextSpacingType spacing) => FontEntry(fontRID).GetSpacing(spacing);
    /// <summary>Reads the shared font stretch setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public int FontGetStretch(RID fontRID) => SourceFont(fontRID).FontStretch;
    /// <summary>Reads the shared font style setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public FontStyle FontGetStyle(RID fontRID) => SourceFont(fontRID).FontStyle;
    /// <summary>Reads the shared font style name setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public string FontGetStyleName(RID fontRID) => SourceFont(fontRID).StyleName;
    /// <summary>Reads the shared font subpixel positioning setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public FontSubpixelPositioning FontGetSubpixelPositioning(RID fontRID) => SourceFont(fontRID).SubpixelPositioning;
    /// <summary>Copies or reads the primary font supported chars.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public string FontGetSupportedChars(RID fontRID) => SourceFont(fontRID).GetSupportedChars();
    /// <summary>Copies or reads the primary font supported glyphs.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public int[] FontGetSupportedGlyphs(RID fontRID) => SourceFont(fontRID).ServerSupportedGlyphs();
    /// <summary>Queries or authors the shared texture count cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public int FontGetTextureCount(RID fontRID, Vector2i size) => SourceFont(fontRID).GetTextureCount(0, size);
    /// <summary>Queries or authors the shared texture image cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="textureIndex">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public Image? FontGetTextureImage(RID fontRID, Vector2i size, int textureIndex) => SourceFont(fontRID).GetTextureImage(0, size, textureIndex);
    /// <summary>Queries or authors the shared texture offsets cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="textureIndex">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public int[] FontGetTextureOffsets(RID fontRID, Vector2i size, int textureIndex) => SourceFont(fontRID).GetTextureOffsets(0, size, textureIndex);
    /// <summary>Reads the shared font transform.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public Transform FontGetTransform(RID fontRID) => SourceFont(fontRID).GetTransform(0);
    /// <summary>Reads the size-cache underline position.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public float FontGetUnderlinePosition(RID fontRID, int size) => SourceFont(fontRID).GetCacheUnderlinePosition(0, size);
    /// <summary>Reads the size-cache underline thickness.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public float FontGetUnderlineThickness(RID fontRID, int size) => SourceFont(fontRID).GetCacheUnderlineThickness(0, size);
    /// <summary>Reads the shared font used palette setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public int FontGetUsedPalette(RID fontRID) => SourceFont(fontRID).ServerPalette;
    /// <summary>Reads the shared font variation coordinates.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public Dictionary<uint, float> FontGetVariationCoordinates(RID fontRID) => SourceFont(fontRID).GetVariationCoordinates(0);
    /// <summary>Reads the shared font weight setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public int FontGetWeight(RID fontRID) => SourceFont(fontRID).FontWeight;
    /// <summary>Tests the primary source character map without automatic fallback.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="character">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public bool FontHasChar(RID fontRID, int character) => SourceFont(fontRID).HasChar(character);
    /// <summary>Reads the shared font allow system fallback setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public bool FontIsAllowSystemFallback(RID fontRID) => SourceFont(fontRID).AllowSystemFallback;
    /// <summary>Reads the shared font force autohinter setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public bool FontIsForceAutohinter(RID fontRID) => SourceFont(fontRID).ForceAutohinter;
    /// <summary>Queries or authors the shared language support policy.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="language">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public bool FontIsLanguageSupported(RID fontRID, string language) => SourceFont(fontRID).IsLanguageSupported(language);
    /// <summary>Reads the shared font modulate color glyphs setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public bool FontIsModulateColorGlyphs(RID fontRID) => SourceFont(fontRID).ModulateColorGlyphs;
    /// <summary>Reads the shared font multichannel signed distance field setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public bool FontIsMultichannelSignedDistanceField(RID fontRID) => SourceFont(fontRID).MultichannelSignedDistanceField;
    /// <summary>Queries or authors the shared script support policy.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="script">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public bool FontIsScriptSupported(RID fontRID, string script) => SourceFont(fontRID).IsScriptSupported(script);
    /// <summary>Queries or authors the shared remove glyph cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontRemoveGlyph(RID fontRID, Vector2i size, int glyph) => SourceFont(fontRID).RemoveGlyph(0, size, glyph);
    /// <summary>Queries or authors the shared remove kerning cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyphPair">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontRemoveKerning(RID fontRID, int size, Vector2i glyphPair) => SourceFont(fontRID).RemoveKerning(0, size, glyphPair);
    /// <summary>Queries or authors the shared language support policy.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="language">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontRemoveLanguageSupportOverride(RID fontRID, string language) => SourceFont(fontRID).RemoveLanguageSupportOverride(language);
    /// <summary>Queries or authors the shared script support policy.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="script">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontRemoveScriptSupportOverride(RID fontRID, string script) => SourceFont(fontRID).RemoveScriptSupportOverride(script);
    /// <summary>Queries or authors the shared remove size cache cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontRemoveSizeCache(RID fontRID, Vector2i size) => SourceFont(fontRID).RemoveSizeCache(0, size);
    /// <summary>Queries or authors the shared remove texture cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="textureIndex">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontRemoveTexture(RID fontRID, Vector2i size, int textureIndex) => SourceFont(fontRID).RemoveTexture(0, size, textureIndex);
    /// <summary>Queries or authors the shared render glyph cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="index">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontRenderGlyph(RID fontRID, Vector2i size, int index) => SourceFont(fontRID).RenderGlyph(0, size, index);
    /// <summary>Queries or authors the shared render range cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="start">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="end">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontRenderRange(RID fontRID, Vector2i size, int start, int end) => SourceFont(fontRID).RenderRange(0, size, start, end);
    /// <summary>Sets the shared font allow system fallback setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetAllowSystemFallback(RID fontRID, bool value) => SourceFont(fontRID).AllowSystemFallback = value;
    /// <summary>Sets the shared font antialiasing setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetAntialiasing(RID fontRID, FontAntialiasing value) => SourceFont(fontRID).Antialiasing = value;
    /// <summary>Authors the size-cache ascent.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetAscent(RID fontRID, int size, float value) => SourceFont(fontRID).SetCacheAscent(0, size, value);
    /// <summary>Sets this font's independent finite baseline-height fraction.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetBaselineOffset(RID fontRID, float value) => FontEntry(fontRID).Baseline = value;
    /// <summary>Copies and validates source bytes before replacing the shared font data.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="data">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetData(RID fontRID, byte[] data) => SourceFont(fontRID).ServerSetData(data);
    /// <summary>Authors the size-cache descent.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetDescent(RID fontRID, int size, float value) => SourceFont(fontRID).SetCacheDescent(0, size, value);
    /// <summary>Sets the shared font disable embedded bitmaps setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetDisableEmbeddedBitmaps(RID fontRID, bool value) => SourceFont(fontRID).DisableEmbeddedBitmaps = value;
    /// <summary>Authors the shared font embolden.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetEmbolden(RID fontRID, float value) => SourceFont(fontRID).SetEmbolden(0, value);
    /// <summary>Authors the shared font face index.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetFaceIndex(RID fontRID, int value) => SourceFont(fontRID).SetFaceIndex(0, value);
    /// <summary>Sets the shared font fixed size setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetFixedSize(RID fontRID, int value) => SourceFont(fontRID).FixedSize = value;
    /// <summary>Sets the shared font fixed size scale mode setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetFixedSizeScaleMode(RID fontRID, FixedSizeScaleMode value) => SourceFont(fontRID).FixedSizeScaleMode = value;
    /// <summary>Sets the shared font force autohinter setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetForceAutohinter(RID fontRID, bool value) => SourceFont(fontRID).ForceAutohinter = value;
    /// <summary>Sets the shared font generate mipmaps setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetGenerateMIPMaps(RID fontRID, bool value) => SourceFont(fontRID).GenerateMIPMaps = value;
    /// <summary>Queries or authors the shared glyph advance cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetGlyphAdvance(RID fontRID, int size, int glyph, Vector2 value) => SourceFont(fontRID).SetGlyphAdvance(0, size, glyph, value);
    /// <summary>Authors the shared glyph offset.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetGlyphOffset(RID fontRID, Vector2i size, int glyph, Vector2 value) => SourceFont(fontRID).SetGlyphOffset(0, size, glyph, value);
    /// <summary>Authors the shared glyph size.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetGlyphSize(RID fontRID, Vector2i size, int glyph, Vector2 value) => SourceFont(fontRID).SetGlyphSize(0, size, glyph, value);
    /// <summary>Authors the shared glyph texture idx.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="textureIndex">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetGlyphTextureIndex(RID fontRID, Vector2i size, int glyph, int textureIndex) => SourceFont(fontRID).SetGlyphTextureIndex(0, size, glyph, textureIndex);
    /// <summary>Authors the shared glyph uv rect.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetGlyphUVRect(RID fontRID, Vector2i size, int glyph, Rect2 value) => SourceFont(fontRID).SetGlyphUVRect(0, size, glyph, value);
    /// <summary>Sets the shared font hinting setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetHinting(RID fontRID, FontHinting value) => SourceFont(fontRID).Hinting = value;
    /// <summary>Sets the shared font keep rounding remainders setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetKeepRoundingRemainders(RID fontRID, bool value) => SourceFont(fontRID).KeepRoundingRemainders = value;
    /// <summary>Queries or authors the shared kerning cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyphPair">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetKerning(RID fontRID, int size, Vector2i glyphPair, Vector2 value) => SourceFont(fontRID).SetKerning(0, size, glyphPair, value);
    /// <summary>Queries or authors the shared language support policy.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="language">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="supported">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetLanguageSupportOverride(RID fontRID, string language, bool supported) => SourceFont(fontRID).SetLanguageSupportOverride(language, supported);
    /// <summary>Sets the shared font modulate color glyphs setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetModulateColorGlyphs(RID fontRID, bool value) => SourceFont(fontRID).ModulateColorGlyphs = value;
    /// <summary>Sets the shared font msdf pixel range setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetMSDFPixelRange(RID fontRID, int value) => SourceFont(fontRID).MSDFPixelRange = value;
    /// <summary>Sets the shared font msdf size setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetMSDFSize(RID fontRID, int value) => SourceFont(fontRID).MSDFSize = value;
    /// <summary>Sets the shared font multichannel signed distance field setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetMultichannelSignedDistanceField(RID fontRID, bool value) => SourceFont(fontRID).MultichannelSignedDistanceField = value;
    /// <summary>Sets the shared font name setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetName(RID fontRID, string value) => SourceFont(fontRID).FontName = value;
    /// <summary>Sets the shared font opentype feature overrides setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetOpenTypeFeatureOverrides(RID fontRID, Dictionary<string, int> value) => SourceFont(fontRID).OpenTypeFeatureOverrides = value;
    /// <summary>Sets the shared font oversampling setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetOversampling(RID fontRID, float value) => SourceFont(fontRID).Oversampling = value;
    /// <summary>Sets the shared font palette custom colors setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetPaletteCustomColors(RID fontRID, Color[] value) => SourceFont(fontRID).ServerCustomColors = value;
    /// <summary>Authors the size-cache scale.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetScale(RID fontRID, int size, float value) => SourceFont(fontRID).SetCacheScale(0, size, value);
    /// <summary>Queries or authors the shared script support policy.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="script">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="supported">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetScriptSupportOverride(RID fontRID, string script, bool supported) => SourceFont(fontRID).SetScriptSupportOverride(script, supported);
    /// <summary>Sets this root or linked font's independent signed spacing.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="spacing">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetSpacing(RID fontRID, TextSpacingType spacing, int value) => FontEntry(fontRID).SetSpacing(spacing, value);
    /// <summary>Sets the shared font stretch setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetStretch(RID fontRID, int value) => SourceFont(fontRID).FontStretch = value;
    /// <summary>Sets the shared font style setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetStyle(RID fontRID, FontStyle value) => SourceFont(fontRID).FontStyle = value;
    /// <summary>Sets the shared font style name setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetStyleName(RID fontRID, string value) => SourceFont(fontRID).StyleName = value;
    /// <summary>Sets the shared font subpixel positioning setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetSubpixelPositioning(RID fontRID, FontSubpixelPositioning value) => SourceFont(fontRID).SubpixelPositioning = value;
    /// <summary>Queries or authors the shared texture image cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="textureIndex">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="image">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetTextureImage(RID fontRID, Vector2i size, int textureIndex, Image image) => SourceFont(fontRID).SetTextureImage(0, size, textureIndex, image);
    /// <summary>Queries or authors the shared texture offsets cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="textureIndex">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="offsets">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetTextureOffsets(RID fontRID, Vector2i size, int textureIndex, int[] offsets) => SourceFont(fontRID).SetTextureOffsets(0, size, textureIndex, offsets);
    /// <summary>Authors the shared font transform.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetTransform(RID fontRID, Transform value) => SourceFont(fontRID).SetTransform(0, value);
    /// <summary>Authors the size-cache underline position.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetUnderlinePosition(RID fontRID, int size, float value) => SourceFont(fontRID).SetCacheUnderlinePosition(0, size, value);
    /// <summary>Authors the size-cache underline thickness.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetUnderlineThickness(RID fontRID, int size, float value) => SourceFont(fontRID).SetCacheUnderlineThickness(0, size, value);
    /// <summary>Sets the shared font used palette setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetUsedPalette(RID fontRID, int value) => SourceFont(fontRID).ServerPalette = value;
    /// <summary>Authors the shared font variation coordinates.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetVariationCoordinates(RID fontRID, Dictionary<uint, float> value) => SourceFont(fontRID).SetVariationCoordinates(0, value);
    /// <summary>Sets the shared font weight setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetWeight(RID fontRID, int value) => SourceFont(fontRID).FontWeight = value;
    /// <summary>Copies supported OpenType feature metadata.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public Dictionary<uint, FontFeatureInfo> FontSupportedFeatureList(RID fontRID) => SourceFont(fontRID).GetSupportedFeatureList();
    /// <summary>Copies supported OpenType variation-axis metadata.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public Dictionary<uint, FontVariationAxis> FontSupportedVariationList(RID fontRID) => SourceFont(fontRID).GetSupportedVariationList();
}
