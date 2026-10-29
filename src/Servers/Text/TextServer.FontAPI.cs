namespace Electron2D;

public abstract partial class TextServer
{
    /// <summary>Queries or authors the shared clear glyphs cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontClearGlyphs(RID fontRID, Vector2i size)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontClearGlyphs(fontRID, size);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Queries or authors the shared clear glyphs cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontClearGlyphs(RID fontRID, Vector2i size) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).ClearGlyphs(0, size); }
    /// <summary>Queries or authors the shared clear kerning map cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontClearKerningMap(RID fontRID, int size)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontClearKerningMap(fontRID, size);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Queries or authors the shared clear kerning map cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontClearKerningMap(RID fontRID, int size) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).ClearKerningMap(0, size); }
    /// <summary>Queries or authors the shared clear size cache cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontClearSizeCache(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontClearSizeCache(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Queries or authors the shared clear size cache cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontClearSizeCache(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).ClearSizeCache(0); }
    /// <summary>Clears realized automatic fallback caches and invalidates all live font consumers.</summary>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontClearSystemFallbackCache()
    {
        var implementationCall = EnterImplementation();
        Exception? callbackFailure = null;
        try
        {
            OnFontClearSystemFallbackCache();
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Clears realized automatic fallback caches and invalidates all live font consumers.</summary>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontClearSystemFallbackCache() { using var nativeTextScope = EnterNativeBackend(); Font.ClearSystemFontCaches(); }
    /// <summary>Queries or authors the shared clear textures cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontClearTextures(RID fontRID, Vector2i size)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontClearTextures(fontRID, size);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Queries or authors the shared clear textures cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontClearTextures(RID fontRID, Vector2i size) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).ClearTextures(0, size); }
    /// <summary>Records a primary glyph at a baseline using the actual shared raster cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="canvas">Canvas item in its active recording scope.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="position">Finite baseline origin.</param>
    /// <param name="index">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="color">Finite drawing color, or null for white.</param>
    /// <param name="oversampling">Finite raster scale; nonpositive values select font or viewport policy.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontDrawGlyph(RID fontRID, CanvasItem canvas, int size, Vector2 position, int index, Color? color = null, float oversampling = 0)
    {
        Font.ValidateDraw(canvas, position, color ?? Colors.White, oversampling);
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            CanvasRasterScale.TrackProvider(this); OnFontDrawGlyph(fontRID, canvas, size, position, index, color, oversampling);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Records a primary glyph at a baseline using the actual shared raster cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="canvas">Canvas item in its active recording scope.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="position">Finite baseline origin.</param>
    /// <param name="index">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="color">Finite drawing color, or null for white.</param>
    /// <param name="oversampling">Finite raster scale; nonpositive values select font or viewport policy.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontDrawGlyph(RID fontRID, CanvasItem canvas, int size, Vector2 position, int index, Color? color = null, float oversampling = 0) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).ServerDrawGlyph(canvas, size, 0, position, index, color ?? Colors.White, oversampling); }
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
    public void FontDrawGlyphOutline(RID fontRID, CanvasItem canvas, int size, int outlineSize, Vector2 position, int index, Color? color = null, float oversampling = 0)
    {
        Font.ValidateDraw(canvas, position, color ?? Colors.White, oversampling);
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            CanvasRasterScale.TrackProvider(this); OnFontDrawGlyphOutline(fontRID, canvas, size, outlineSize, position, index, color, oversampling);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

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
    protected virtual void OnFontDrawGlyphOutline(RID fontRID, CanvasItem canvas, int size, int outlineSize, Vector2 position, int index, Color? color = null, float oversampling = 0) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).ServerDrawGlyph(canvas, size, outlineSize, position, index, color ?? Colors.White, oversampling, true); }
    /// <summary>Reads the shared font antialiasing setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public FontAntialiasing FontGetAntialiasing(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetAntialiasing(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the shared font antialiasing setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual FontAntialiasing OnFontGetAntialiasing(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).Antialiasing; }
    /// <summary>Reads the size-cache ascent.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public float FontGetAscent(RID fontRID, int size)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetAscent(fontRID, size);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the size-cache ascent.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual float OnFontGetAscent(RID fontRID, int size) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).GetCacheAscent(0, size); }
    /// <summary>Reads this font's independent baseline-height fraction.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public float FontGetBaselineOffset(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetBaselineOffset(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads this font's independent baseline-height fraction.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual float OnFontGetBaselineOffset(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceBaseline(fontRID); }
    /// <summary>Returns the first primary character mapped to a glyph, or zero.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyphIndex">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public int FontGetCharFromGlyphIndex(RID fontRID, int size, int glyphIndex)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetCharFromGlyphIndex(fontRID, size, glyphIndex);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Returns the first primary character mapped to a glyph, or zero.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyphIndex">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual int OnFontGetCharFromGlyphIndex(RID fontRID, int size, int glyphIndex) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).GetCharFromGlyphIndex(size, glyphIndex); }
    /// <summary>Reads the size-cache descent.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public float FontGetDescent(RID fontRID, int size)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetDescent(fontRID, size);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the size-cache descent.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual float OnFontGetDescent(RID fontRID, int size) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).GetCacheDescent(0, size); }
    /// <summary>Reads the shared font disable embedded bitmaps setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public bool FontGetDisableEmbeddedBitmaps(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetDisableEmbeddedBitmaps(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the shared font disable embedded bitmaps setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual bool OnFontGetDisableEmbeddedBitmaps(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).DisableEmbeddedBitmaps; }
    /// <summary>Reads the shared font embolden.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public float FontGetEmbolden(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetEmbolden(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the shared font embolden.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual float OnFontGetEmbolden(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).GetEmbolden(0); }
    /// <summary>Copies or reads the primary font face count.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public int FontGetFaceCount(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetFaceCount(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Copies or reads the primary font face count.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual int OnFontGetFaceCount(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).SourceFaceCount; }
    /// <summary>Reads the shared font face index.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public int FontGetFaceIndex(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetFaceIndex(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the shared font face index.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual int OnFontGetFaceIndex(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).GetFaceIndex(0); }
    /// <summary>Reads the shared font fixed size setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public int FontGetFixedSize(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetFixedSize(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the shared font fixed size setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual int OnFontGetFixedSize(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).FixedSize; }
    /// <summary>Reads the shared font fixed size scale mode setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public FixedSizeScaleMode FontGetFixedSizeScaleMode(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetFixedSizeScaleMode(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the shared font fixed size scale mode setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual FixedSizeScaleMode OnFontGetFixedSizeScaleMode(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).FixedSizeScaleMode; }
    /// <summary>Reads the shared font generate mipmaps setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public bool FontGetGenerateMIPMaps(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetGenerateMIPMaps(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the shared font generate mipmaps setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual bool OnFontGetGenerateMIPMaps(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).GenerateMIPMaps; }
    /// <summary>Queries or authors the shared glyph advance cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public Vector2 FontGetGlyphAdvance(RID fontRID, int size, int glyph)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetGlyphAdvance(fontRID, size, glyph);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Queries or authors the shared glyph advance cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual Vector2 OnFontGetGlyphAdvance(RID fontRID, int size, int glyph) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).ServerGlyphAdvance(size, glyph); }
    /// <summary>Copies baseline-relative native Bézier points, contour ends and fill orientation.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="index">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public FontGlyphContours FontGetGlyphContours(RID fontRID, int size, int index)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetGlyphContours(fontRID, size, index);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Copies baseline-relative native Bézier points, contour ends and fill orientation.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="index">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual FontGlyphContours OnFontGetGlyphContours(RID fontRID, int size, int index) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).ServerContours(size, index); }
    /// <summary>Resolves a Unicode scalar and optional variation selector to the primary glyph index.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="character">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="variationSelector">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public int FontGetGlyphIndex(RID fontRID, int size, int character, int variationSelector)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetGlyphIndex(fontRID, size, character, variationSelector);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Resolves a Unicode scalar and optional variation selector to the primary glyph index.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="character">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="variationSelector">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual int OnFontGetGlyphIndex(RID fontRID, int size, int character, int variationSelector) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).GetGlyphIndex(size, character, variationSelector); }
    /// <summary>Queries or authors the shared glyph list cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public int[] FontGetGlyphList(RID fontRID, Vector2i size)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetGlyphList(fontRID, size);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Queries or authors the shared glyph list cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual int[] OnFontGetGlyphList(RID fontRID, Vector2i size) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).GetGlyphList(0, size); }
    /// <summary>Reads the shared glyph offset.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public Vector2 FontGetGlyphOffset(RID fontRID, Vector2i size, int glyph)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetGlyphOffset(fontRID, size, glyph);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the shared glyph offset.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual Vector2 OnFontGetGlyphOffset(RID fontRID, Vector2i size, int glyph) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).ServerGlyphOffset(size, glyph); }
    /// <summary>Reads the shared glyph size.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public Vector2 FontGetGlyphSize(RID fontRID, Vector2i size, int glyph)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetGlyphSize(fontRID, size, glyph);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the shared glyph size.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual Vector2 OnFontGetGlyphSize(RID fontRID, Vector2i size, int glyph) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).ServerGlyphSize(size, glyph); }
    /// <summary>Reads the shared glyph texture idx.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public int FontGetGlyphTextureIndex(RID fontRID, Vector2i size, int glyph)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetGlyphTextureIndex(fontRID, size, glyph);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the shared glyph texture idx.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual int OnFontGetGlyphTextureIndex(RID fontRID, Vector2i size, int glyph) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).ServerGlyphTextureIndex(size, glyph); }
    /// <summary>Returns the real borrowed renderer atlas texture identity, or an empty identity.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public RID FontGetGlyphTextureRID(RID fontRID, Vector2i size, int glyph)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetGlyphTextureRID(fontRID, size, glyph);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Returns the real borrowed renderer atlas texture identity, or an empty identity.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual RID OnFontGetGlyphTextureRID(RID fontRID, Vector2i size, int glyph) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).ServerGlyphTexture(size, glyph); }
    /// <summary>Returns actual renderer atlas dimensions, or zero.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public Vector2 FontGetGlyphTextureSize(RID fontRID, Vector2i size, int glyph)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetGlyphTextureSize(fontRID, size, glyph);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Returns actual renderer atlas dimensions, or zero.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual Vector2 OnFontGetGlyphTextureSize(RID fontRID, Vector2i size, int glyph) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).ServerGlyphTextureSize(size, glyph); }
    /// <summary>Reads the shared glyph uv rect.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public Rect2 FontGetGlyphUVRect(RID fontRID, Vector2i size, int glyph)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetGlyphUVRect(fontRID, size, glyph);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the shared glyph uv rect.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual Rect2 OnFontGetGlyphUVRect(RID fontRID, Vector2i size, int glyph) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).ServerGlyphUVRect(size, glyph); }
    /// <summary>Reads the shared font hinting setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public FontHinting FontGetHinting(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetHinting(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the shared font hinting setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual FontHinting OnFontGetHinting(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).Hinting; }
    /// <summary>Reads the shared font keep rounding remainders setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public bool FontGetKeepRoundingRemainders(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetKeepRoundingRemainders(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the shared font keep rounding remainders setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual bool OnFontGetKeepRoundingRemainders(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).KeepRoundingRemainders; }
    /// <summary>Queries or authors the shared kerning cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyphPair">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public Vector2 FontGetKerning(RID fontRID, int size, Vector2i glyphPair)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetKerning(fontRID, size, glyphPair);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Queries or authors the shared kerning cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyphPair">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual Vector2 OnFontGetKerning(RID fontRID, int size, Vector2i glyphPair) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).GetKerning(0, size, glyphPair); }
    /// <summary>Queries or authors the shared kerning list cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public Vector2i[] FontGetKerningList(RID fontRID, int size)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetKerningList(fontRID, size);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Queries or authors the shared kerning list cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual Vector2i[] OnFontGetKerningList(RID fontRID, int size) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).GetKerningList(0, size); }
    /// <summary>Queries or authors the shared language support policy.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="language">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public bool FontGetLanguageSupportOverride(RID fontRID, string language)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetLanguageSupportOverride(fontRID, language);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Queries or authors the shared language support policy.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="language">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual bool OnFontGetLanguageSupportOverride(RID fontRID, string language) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).GetLanguageSupportOverride(language); }
    /// <summary>Queries or authors the shared language support policy.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public string[] FontGetLanguageSupportOverrides(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetLanguageSupportOverrides(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Queries or authors the shared language support policy.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual string[] OnFontGetLanguageSupportOverrides(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).GetLanguageSupportOverrides(); }
    /// <summary>Reads the shared font msdf pixel range setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public int FontGetMSDFPixelRange(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetMSDFPixelRange(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the shared font msdf pixel range setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual int OnFontGetMSDFPixelRange(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).MSDFPixelRange; }
    /// <summary>Reads the shared font msdf size setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public int FontGetMSDFSize(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetMSDFSize(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the shared font msdf size setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual int OnFontGetMSDFSize(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).MSDFSize; }
    /// <summary>Reads the shared font name setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public string FontGetName(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetName(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the shared font name setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual string OnFontGetName(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).FontName; }
    /// <summary>Reads the shared font opentype feature overrides setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public Dictionary<string, int> FontGetOpenTypeFeatureOverrides(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetOpenTypeFeatureOverrides(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the shared font opentype feature overrides setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual Dictionary<string, int> OnFontGetOpenTypeFeatureOverrides(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).OpenTypeFeatureOverrides; }
    /// <summary>Copies or reads the primary font ot name strings.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public Dictionary<string, Dictionary<string, string>> FontGetOTNameStrings(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetOTNameStrings(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Copies or reads the primary font ot name strings.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual Dictionary<string, Dictionary<string, string>> OnFontGetOTNameStrings(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).GetOTNameStrings(); }
    /// <summary>Reads the shared font oversampling setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public float FontGetOversampling(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetOversampling(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the shared font oversampling setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual float OnFontGetOversampling(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).Oversampling; }
    /// <summary>Copies or reads the primary font palette colors.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="index">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public Color[] FontGetPaletteColors(RID fontRID, int index)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetPaletteColors(fontRID, index);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Copies or reads the primary font palette colors.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="index">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual Color[] OnFontGetPaletteColors(RID fontRID, int index) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).GetPaletteColors(index); }
    /// <summary>Copies or reads the primary font palette count.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public int FontGetPaletteCount(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetPaletteCount(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Copies or reads the primary font palette count.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual int OnFontGetPaletteCount(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).GetPaletteCount(); }
    /// <summary>Reads the shared font palette custom colors setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public Color[] FontGetPaletteCustomColors(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetPaletteCustomColors(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the shared font palette custom colors setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual Color[] OnFontGetPaletteCustomColors(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).ServerCustomColors; }
    /// <summary>Copies or reads the primary font palette name.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="index">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public string FontGetPaletteName(RID fontRID, int index)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetPaletteName(fontRID, index);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Copies or reads the primary font palette name.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="index">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual string OnFontGetPaletteName(RID fontRID, int index) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).GetPaletteName(index); }
    /// <summary>Reads the size-cache scale.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public float FontGetScale(RID fontRID, int size)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetScale(fontRID, size);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the size-cache scale.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual float OnFontGetScale(RID fontRID, int size) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).GetCacheScale(0, size); }
    /// <summary>Queries or authors the shared script support policy.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="script">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public bool FontGetScriptSupportOverride(RID fontRID, string script)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetScriptSupportOverride(fontRID, script);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Queries or authors the shared script support policy.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="script">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual bool OnFontGetScriptSupportOverride(RID fontRID, string script) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).GetScriptSupportOverride(script); }
    /// <summary>Queries or authors the shared script support policy.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public string[] FontGetScriptSupportOverrides(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetScriptSupportOverrides(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Queries or authors the shared script support policy.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual string[] OnFontGetScriptSupportOverrides(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).GetScriptSupportOverrides(); }
    /// <summary>Copies actual nominal size-cache counts, atlas storage estimates and realized raster scales.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public FontSizeCacheInfo[] FontGetSizeCacheInfo(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetSizeCacheInfo(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Copies actual nominal size-cache counts, atlas storage estimates and realized raster scales.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual FontSizeCacheInfo[] OnFontGetSizeCacheInfo(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).ServerCacheInfo(); }
    /// <summary>Queries or authors the shared size cache list cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public Vector2i[] FontGetSizeCacheList(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetSizeCacheList(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Queries or authors the shared size cache list cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual Vector2i[] OnFontGetSizeCacheList(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).GetSizeCacheList(0); }
    /// <summary>Reads this root or linked font's independent signed spacing.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="spacing">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public int FontGetSpacing(RID fontRID, TextSpacingType spacing)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetSpacing(fontRID, spacing);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads this root or linked font's independent signed spacing.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="spacing">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual int OnFontGetSpacing(RID fontRID, TextSpacingType spacing) { using var nativeTextScope = EnterNativeBackend(); return SourceSpacing(fontRID, spacing); }
    /// <summary>Reads the shared font stretch setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public int FontGetStretch(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetStretch(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the shared font stretch setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual int OnFontGetStretch(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).FontStretch; }
    /// <summary>Reads the shared font style setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public FontStyle FontGetStyle(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetStyle(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the shared font style setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual FontStyle OnFontGetStyle(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).FontStyle; }
    /// <summary>Reads the shared font style name setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public string FontGetStyleName(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetStyleName(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the shared font style name setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual string OnFontGetStyleName(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).StyleName; }
    /// <summary>Reads the shared font subpixel positioning setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public FontSubpixelPositioning FontGetSubpixelPositioning(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetSubpixelPositioning(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the shared font subpixel positioning setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual FontSubpixelPositioning OnFontGetSubpixelPositioning(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).SubpixelPositioning; }
    /// <summary>Copies or reads the primary font supported chars.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public string FontGetSupportedChars(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetSupportedChars(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Copies or reads the primary font supported chars.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual string OnFontGetSupportedChars(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).GetSupportedChars(); }
    /// <summary>Copies or reads the primary font supported glyphs.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public int[] FontGetSupportedGlyphs(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetSupportedGlyphs(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Copies or reads the primary font supported glyphs.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual int[] OnFontGetSupportedGlyphs(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).ServerSupportedGlyphs(); }
    /// <summary>Queries or authors the shared texture count cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public int FontGetTextureCount(RID fontRID, Vector2i size)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetTextureCount(fontRID, size);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Queries or authors the shared texture count cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual int OnFontGetTextureCount(RID fontRID, Vector2i size) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).GetTextureCount(0, size); }
    /// <summary>Queries or authors the shared texture image cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="textureIndex">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public Image? FontGetTextureImage(RID fontRID, Vector2i size, int textureIndex)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetTextureImage(fontRID, size, textureIndex);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Queries or authors the shared texture image cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="textureIndex">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual Image? OnFontGetTextureImage(RID fontRID, Vector2i size, int textureIndex) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).GetTextureImage(0, size, textureIndex); }
    /// <summary>Queries or authors the shared texture offsets cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="textureIndex">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public int[] FontGetTextureOffsets(RID fontRID, Vector2i size, int textureIndex)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetTextureOffsets(fontRID, size, textureIndex);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Queries or authors the shared texture offsets cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="textureIndex">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual int[] OnFontGetTextureOffsets(RID fontRID, Vector2i size, int textureIndex) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).GetTextureOffsets(0, size, textureIndex); }
    /// <summary>Reads the shared font transform.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public Transform FontGetTransform(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetTransform(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the shared font transform.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual Transform OnFontGetTransform(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).GetTransform(0); }
    /// <summary>Reads the size-cache underline position.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public float FontGetUnderlinePosition(RID fontRID, int size)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetUnderlinePosition(fontRID, size);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the size-cache underline position.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual float OnFontGetUnderlinePosition(RID fontRID, int size) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).GetCacheUnderlinePosition(0, size); }
    /// <summary>Reads the size-cache underline thickness.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public float FontGetUnderlineThickness(RID fontRID, int size)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetUnderlineThickness(fontRID, size);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the size-cache underline thickness.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual float OnFontGetUnderlineThickness(RID fontRID, int size) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).GetCacheUnderlineThickness(0, size); }
    /// <summary>Reads the shared font used palette setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public int FontGetUsedPalette(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetUsedPalette(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the shared font used palette setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual int OnFontGetUsedPalette(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).ServerPalette; }
    /// <summary>Reads the shared font variation coordinates.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public Dictionary<uint, float> FontGetVariationCoordinates(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetVariationCoordinates(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the shared font variation coordinates.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual Dictionary<uint, float> OnFontGetVariationCoordinates(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).GetVariationCoordinates(0); }
    /// <summary>Reads the shared font weight setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public int FontGetWeight(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontGetWeight(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the shared font weight setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual int OnFontGetWeight(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).FontWeight; }
    /// <summary>Tests the primary source character map without automatic fallback.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="character">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public bool FontHasChar(RID fontRID, int character)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontHasChar(fontRID, character);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Tests the primary source character map without automatic fallback.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="character">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual bool OnFontHasChar(RID fontRID, int character) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).HasChar(character); }
    /// <summary>Reads the shared font allow system fallback setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public bool FontIsAllowSystemFallback(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontIsAllowSystemFallback(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the shared font allow system fallback setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual bool OnFontIsAllowSystemFallback(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).AllowSystemFallback; }
    /// <summary>Reads the shared font force autohinter setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public bool FontIsForceAutohinter(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontIsForceAutohinter(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the shared font force autohinter setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual bool OnFontIsForceAutohinter(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).ForceAutohinter; }
    /// <summary>Queries or authors the shared language support policy.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="language">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public bool FontIsLanguageSupported(RID fontRID, string language)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontIsLanguageSupported(fontRID, language);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Queries or authors the shared language support policy.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="language">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual bool OnFontIsLanguageSupported(RID fontRID, string language) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).IsLanguageSupported(language); }
    /// <summary>Reads the shared font modulate color glyphs setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public bool FontIsModulateColorGlyphs(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontIsModulateColorGlyphs(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the shared font modulate color glyphs setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual bool OnFontIsModulateColorGlyphs(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).ModulateColorGlyphs; }
    /// <summary>Reads the shared font multichannel signed distance field setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public bool FontIsMultichannelSignedDistanceField(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontIsMultichannelSignedDistanceField(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reads the shared font multichannel signed distance field setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual bool OnFontIsMultichannelSignedDistanceField(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).MultichannelSignedDistanceField; }
    /// <summary>Queries or authors the shared script support policy.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="script">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public bool FontIsScriptSupported(RID fontRID, string script)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontIsScriptSupported(fontRID, script);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Queries or authors the shared script support policy.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="script">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual bool OnFontIsScriptSupported(RID fontRID, string script) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).IsScriptSupported(script); }
    /// <summary>Queries or authors the shared remove glyph cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontRemoveGlyph(RID fontRID, Vector2i size, int glyph)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontRemoveGlyph(fontRID, size, glyph);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Queries or authors the shared remove glyph cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontRemoveGlyph(RID fontRID, Vector2i size, int glyph) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).RemoveGlyph(0, size, glyph); }
    /// <summary>Queries or authors the shared remove kerning cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyphPair">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontRemoveKerning(RID fontRID, int size, Vector2i glyphPair)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontRemoveKerning(fontRID, size, glyphPair);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Queries or authors the shared remove kerning cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyphPair">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontRemoveKerning(RID fontRID, int size, Vector2i glyphPair) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).RemoveKerning(0, size, glyphPair); }
    /// <summary>Queries or authors the shared language support policy.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="language">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontRemoveLanguageSupportOverride(RID fontRID, string language)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontRemoveLanguageSupportOverride(fontRID, language);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Queries or authors the shared language support policy.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="language">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontRemoveLanguageSupportOverride(RID fontRID, string language) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).RemoveLanguageSupportOverride(language); }
    /// <summary>Queries or authors the shared script support policy.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="script">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontRemoveScriptSupportOverride(RID fontRID, string script)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontRemoveScriptSupportOverride(fontRID, script);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Queries or authors the shared script support policy.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="script">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontRemoveScriptSupportOverride(RID fontRID, string script) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).RemoveScriptSupportOverride(script); }
    /// <summary>Queries or authors the shared remove size cache cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontRemoveSizeCache(RID fontRID, Vector2i size)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontRemoveSizeCache(fontRID, size);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Queries or authors the shared remove size cache cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontRemoveSizeCache(RID fontRID, Vector2i size) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).RemoveSizeCache(0, size); }
    /// <summary>Queries or authors the shared remove texture cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="textureIndex">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontRemoveTexture(RID fontRID, Vector2i size, int textureIndex)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontRemoveTexture(fontRID, size, textureIndex);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Queries or authors the shared remove texture cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="textureIndex">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontRemoveTexture(RID fontRID, Vector2i size, int textureIndex) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).RemoveTexture(0, size, textureIndex); }
    /// <summary>Queries or authors the shared render glyph cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="index">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontRenderGlyph(RID fontRID, Vector2i size, int index)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            OnFontRenderGlyph(fontRID, size, index);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Queries or authors the shared render glyph cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="index">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontRenderGlyph(RID fontRID, Vector2i size, int index) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).RenderGlyph(0, size, index); }
    /// <summary>Queries or authors the shared render range cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="start">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="end">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontRenderRange(RID fontRID, Vector2i size, int start, int end)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            OnFontRenderRange(fontRID, size, start, end);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Queries or authors the shared render range cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="start">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="end">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontRenderRange(RID fontRID, Vector2i size, int start, int end) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).RenderRange(0, size, start, end); }
    /// <summary>Sets the shared font allow system fallback setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetAllowSystemFallback(RID fontRID, bool value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetAllowSystemFallback(fontRID, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Sets the shared font allow system fallback setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetAllowSystemFallback(RID fontRID, bool value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).AllowSystemFallback = value; }
    /// <summary>Sets the shared font antialiasing setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetAntialiasing(RID fontRID, FontAntialiasing value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetAntialiasing(fontRID, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Sets the shared font antialiasing setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetAntialiasing(RID fontRID, FontAntialiasing value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).Antialiasing = value; }
    /// <summary>Authors the size-cache ascent.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetAscent(RID fontRID, int size, float value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetAscent(fontRID, size, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Authors the size-cache ascent.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetAscent(RID fontRID, int size, float value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).SetCacheAscent(0, size, value); }
    /// <summary>Sets this font's independent finite baseline-height fraction.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetBaselineOffset(RID fontRID, float value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetBaselineOffset(fontRID, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Sets this font's independent finite baseline-height fraction.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetBaselineOffset(RID fontRID, float value) { using var nativeTextScope = EnterNativeBackend(); SourceBaseline(fontRID, value); }
    /// <summary>Copies and validates source bytes before replacing the shared font data.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="data">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetData(RID fontRID, byte[] data)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetData(fontRID, data);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Copies and validates source bytes before replacing the shared font data.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="data">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetData(RID fontRID, byte[] data) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).ServerSetData(data); }
    /// <summary>Authors the size-cache descent.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetDescent(RID fontRID, int size, float value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetDescent(fontRID, size, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Authors the size-cache descent.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetDescent(RID fontRID, int size, float value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).SetCacheDescent(0, size, value); }
    /// <summary>Sets the shared font disable embedded bitmaps setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetDisableEmbeddedBitmaps(RID fontRID, bool value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetDisableEmbeddedBitmaps(fontRID, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Sets the shared font disable embedded bitmaps setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetDisableEmbeddedBitmaps(RID fontRID, bool value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).DisableEmbeddedBitmaps = value; }
    /// <summary>Authors the shared font embolden.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetEmbolden(RID fontRID, float value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetEmbolden(fontRID, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Authors the shared font embolden.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetEmbolden(RID fontRID, float value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).SetEmbolden(0, value); }
    /// <summary>Authors the shared font face index.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetFaceIndex(RID fontRID, int value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetFaceIndex(fontRID, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Authors the shared font face index.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetFaceIndex(RID fontRID, int value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).SetFaceIndex(0, value); }
    /// <summary>Sets the shared font fixed size setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetFixedSize(RID fontRID, int value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetFixedSize(fontRID, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Sets the shared font fixed size setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetFixedSize(RID fontRID, int value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).FixedSize = value; }
    /// <summary>Sets the shared font fixed size scale mode setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetFixedSizeScaleMode(RID fontRID, FixedSizeScaleMode value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetFixedSizeScaleMode(fontRID, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Sets the shared font fixed size scale mode setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetFixedSizeScaleMode(RID fontRID, FixedSizeScaleMode value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).FixedSizeScaleMode = value; }
    /// <summary>Sets the shared font force autohinter setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetForceAutohinter(RID fontRID, bool value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetForceAutohinter(fontRID, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Sets the shared font force autohinter setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetForceAutohinter(RID fontRID, bool value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).ForceAutohinter = value; }
    /// <summary>Sets the shared font generate mipmaps setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetGenerateMIPMaps(RID fontRID, bool value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetGenerateMIPMaps(fontRID, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Sets the shared font generate mipmaps setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetGenerateMIPMaps(RID fontRID, bool value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).GenerateMIPMaps = value; }
    /// <summary>Queries or authors the shared glyph advance cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetGlyphAdvance(RID fontRID, int size, int glyph, Vector2 value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetGlyphAdvance(fontRID, size, glyph, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Queries or authors the shared glyph advance cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetGlyphAdvance(RID fontRID, int size, int glyph, Vector2 value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).SetGlyphAdvance(0, size, glyph, value); }
    /// <summary>Authors the shared glyph offset.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetGlyphOffset(RID fontRID, Vector2i size, int glyph, Vector2 value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetGlyphOffset(fontRID, size, glyph, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Authors the shared glyph offset.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetGlyphOffset(RID fontRID, Vector2i size, int glyph, Vector2 value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).SetGlyphOffset(0, size, glyph, value); }
    /// <summary>Authors the shared glyph size.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetGlyphSize(RID fontRID, Vector2i size, int glyph, Vector2 value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetGlyphSize(fontRID, size, glyph, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Authors the shared glyph size.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetGlyphSize(RID fontRID, Vector2i size, int glyph, Vector2 value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).SetGlyphSize(0, size, glyph, value); }
    /// <summary>Authors the shared glyph texture idx.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="textureIndex">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetGlyphTextureIndex(RID fontRID, Vector2i size, int glyph, int textureIndex)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetGlyphTextureIndex(fontRID, size, glyph, textureIndex);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Authors the shared glyph texture idx.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="textureIndex">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetGlyphTextureIndex(RID fontRID, Vector2i size, int glyph, int textureIndex) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).SetGlyphTextureIndex(0, size, glyph, textureIndex); }
    /// <summary>Authors the shared glyph uv rect.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetGlyphUVRect(RID fontRID, Vector2i size, int glyph, Rect2 value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetGlyphUVRect(fontRID, size, glyph, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Authors the shared glyph uv rect.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyph">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetGlyphUVRect(RID fontRID, Vector2i size, int glyph, Rect2 value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).SetGlyphUVRect(0, size, glyph, value); }
    /// <summary>Sets the shared font hinting setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetHinting(RID fontRID, FontHinting value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetHinting(fontRID, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Sets the shared font hinting setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetHinting(RID fontRID, FontHinting value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).Hinting = value; }
    /// <summary>Sets the shared font keep rounding remainders setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetKeepRoundingRemainders(RID fontRID, bool value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetKeepRoundingRemainders(fontRID, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Sets the shared font keep rounding remainders setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetKeepRoundingRemainders(RID fontRID, bool value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).KeepRoundingRemainders = value; }
    /// <summary>Queries or authors the shared kerning cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyphPair">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetKerning(RID fontRID, int size, Vector2i glyphPair, Vector2 value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetKerning(fontRID, size, glyphPair, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Queries or authors the shared kerning cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="glyphPair">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetKerning(RID fontRID, int size, Vector2i glyphPair, Vector2 value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).SetKerning(0, size, glyphPair, value); }
    /// <summary>Queries or authors the shared language support policy.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="language">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="supported">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetLanguageSupportOverride(RID fontRID, string language, bool supported)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetLanguageSupportOverride(fontRID, language, supported);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Queries or authors the shared language support policy.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="language">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="supported">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetLanguageSupportOverride(RID fontRID, string language, bool supported) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).SetLanguageSupportOverride(language, supported); }
    /// <summary>Sets the shared font modulate color glyphs setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetModulateColorGlyphs(RID fontRID, bool value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetModulateColorGlyphs(fontRID, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Sets the shared font modulate color glyphs setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetModulateColorGlyphs(RID fontRID, bool value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).ModulateColorGlyphs = value; }
    /// <summary>Sets the shared font msdf pixel range setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetMSDFPixelRange(RID fontRID, int value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetMSDFPixelRange(fontRID, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Sets the shared font msdf pixel range setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetMSDFPixelRange(RID fontRID, int value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).MSDFPixelRange = value; }
    /// <summary>Sets the shared font msdf size setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetMSDFSize(RID fontRID, int value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetMSDFSize(fontRID, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Sets the shared font msdf size setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetMSDFSize(RID fontRID, int value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).MSDFSize = value; }
    /// <summary>Sets the shared font multichannel signed distance field setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetMultichannelSignedDistanceField(RID fontRID, bool value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetMultichannelSignedDistanceField(fontRID, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Sets the shared font multichannel signed distance field setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetMultichannelSignedDistanceField(RID fontRID, bool value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).MultichannelSignedDistanceField = value; }
    /// <summary>Sets the shared font name setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetName(RID fontRID, string value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetName(fontRID, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Sets the shared font name setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetName(RID fontRID, string value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).FontName = value; }
    /// <summary>Sets the shared font opentype feature overrides setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetOpenTypeFeatureOverrides(RID fontRID, Dictionary<string, int> value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetOpenTypeFeatureOverrides(fontRID, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Sets the shared font opentype feature overrides setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetOpenTypeFeatureOverrides(RID fontRID, Dictionary<string, int> value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).OpenTypeFeatureOverrides = value; }
    /// <summary>Sets the shared font oversampling setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetOversampling(RID fontRID, float value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetOversampling(fontRID, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Sets the shared font oversampling setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetOversampling(RID fontRID, float value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).Oversampling = value; }
    /// <summary>Sets the shared font palette custom colors setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetPaletteCustomColors(RID fontRID, Color[] value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetPaletteCustomColors(fontRID, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Sets the shared font palette custom colors setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetPaletteCustomColors(RID fontRID, Color[] value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).ServerCustomColors = value; }
    /// <summary>Authors the size-cache scale.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetScale(RID fontRID, int size, float value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetScale(fontRID, size, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Authors the size-cache scale.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetScale(RID fontRID, int size, float value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).SetCacheScale(0, size, value); }
    /// <summary>Queries or authors the shared script support policy.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="script">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="supported">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetScriptSupportOverride(RID fontRID, string script, bool supported)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetScriptSupportOverride(fontRID, script, supported);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Queries or authors the shared script support policy.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="script">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="supported">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetScriptSupportOverride(RID fontRID, string script, bool supported) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).SetScriptSupportOverride(script, supported); }
    /// <summary>Sets this root or linked font's independent signed spacing.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="spacing">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetSpacing(RID fontRID, TextSpacingType spacing, int value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetSpacing(fontRID, spacing, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Sets this root or linked font's independent signed spacing.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="spacing">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetSpacing(RID fontRID, TextSpacingType spacing, int value) { using var nativeTextScope = EnterNativeBackend(); SourceSpacing(fontRID, spacing, value); }
    /// <summary>Sets the shared font stretch setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetStretch(RID fontRID, int value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetStretch(fontRID, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Sets the shared font stretch setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetStretch(RID fontRID, int value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).FontStretch = value; }
    /// <summary>Sets the shared font style setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetStyle(RID fontRID, FontStyle value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetStyle(fontRID, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Sets the shared font style setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetStyle(RID fontRID, FontStyle value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).FontStyle = value; }
    /// <summary>Sets the shared font style name setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetStyleName(RID fontRID, string value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetStyleName(fontRID, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Sets the shared font style name setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetStyleName(RID fontRID, string value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).StyleName = value; }
    /// <summary>Sets the shared font subpixel positioning setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetSubpixelPositioning(RID fontRID, FontSubpixelPositioning value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetSubpixelPositioning(fontRID, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Sets the shared font subpixel positioning setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetSubpixelPositioning(RID fontRID, FontSubpixelPositioning value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).SubpixelPositioning = value; }
    /// <summary>Queries or authors the shared texture image cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="textureIndex">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="image">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetTextureImage(RID fontRID, Vector2i size, int textureIndex, Image image)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetTextureImage(fontRID, size, textureIndex, image);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Queries or authors the shared texture image cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="textureIndex">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="image">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetTextureImage(RID fontRID, Vector2i size, int textureIndex, Image image) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).SetTextureImage(0, size, textureIndex, image); }
    /// <summary>Queries or authors the shared texture offsets cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="textureIndex">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="offsets">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetTextureOffsets(RID fontRID, Vector2i size, int textureIndex, int[] offsets)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetTextureOffsets(fontRID, size, textureIndex, offsets);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Queries or authors the shared texture offsets cache.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="textureIndex">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <param name="offsets">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetTextureOffsets(RID fontRID, Vector2i size, int textureIndex, int[] offsets) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).SetTextureOffsets(0, size, textureIndex, offsets); }
    /// <summary>Authors the shared font transform.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetTransform(RID fontRID, Transform value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetTransform(fontRID, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Authors the shared font transform.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetTransform(RID fontRID, Transform value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).SetTransform(0, value); }
    /// <summary>Authors the size-cache underline position.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetUnderlinePosition(RID fontRID, int size, float value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetUnderlinePosition(fontRID, size, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Authors the size-cache underline position.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetUnderlinePosition(RID fontRID, int size, float value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).SetCacheUnderlinePosition(0, size, value); }
    /// <summary>Authors the size-cache underline thickness.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetUnderlineThickness(RID fontRID, int size, float value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetUnderlineThickness(fontRID, size, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Authors the size-cache underline thickness.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="size">Positive bounded font size, or size and nonnegative outline radius.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetUnderlineThickness(RID fontRID, int size, float value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).SetCacheUnderlineThickness(0, size, value); }
    /// <summary>Sets the shared font used palette setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetUsedPalette(RID fontRID, int value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetUsedPalette(fontRID, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Sets the shared font used palette setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetUsedPalette(RID fontRID, int value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).ServerPalette = value; }
    /// <summary>Authors the shared font variation coordinates.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetVariationCoordinates(RID fontRID, Dictionary<uint, float> value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetVariationCoordinates(fontRID, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Authors the shared font variation coordinates.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetVariationCoordinates(RID fontRID, Dictionary<uint, float> value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).SetVariationCoordinates(0, value); }
    /// <summary>Sets the shared font weight setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public void FontSetWeight(RID fontRID, int value)
    {
        var implementationCall = EnterImplementation(fontRID, true, mutation: true);
        Exception? callbackFailure = null;
        try
        {
            OnFontSetWeight(fontRID, value);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Sets the shared font weight setting.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <param name="value">Typed value or source selector; input collections are copied and output snapshots are independent.</param>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual void OnFontSetWeight(RID fontRID, int value) { using var nativeTextScope = EnterNativeBackend(); SourceFont(fontRID).FontWeight = value; }
    /// <summary>Copies supported OpenType feature metadata.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public Dictionary<uint, FontFeatureInfo> FontSupportedFeatureList(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontSupportedFeatureList(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Copies supported OpenType feature metadata.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual Dictionary<uint, FontFeatureInfo> OnFontSupportedFeatureList(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).GetSupportedFeatureList(); }
    /// <summary>Copies supported OpenType variation-axis metadata.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    public Dictionary<uint, FontVariationAxis> FontSupportedVariationList(RID fontRID)
    {
        var implementationCall = EnterImplementation(fontRID, true);
        Exception? callbackFailure = null;
        try
        {
            return OnFontSupportedVariationList(fontRID);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Copies supported OpenType variation-axis metadata.</summary>
    /// <param name="fontRID">Live root or linked font identity.</param>
    /// <returns>A typed current value or caller-owned snapshot; renderer texture identities remain borrowed.</returns>
    /// <remarks>Source/config/cache edits affect every link. Spacing and baseline belong to the individual identity. Invalid inputs fail before publication; source retirement is explicit.</remarks>
    protected virtual Dictionary<uint, FontVariationAxis> OnFontSupportedVariationList(RID fontRID) { using var nativeTextScope = EnterNativeBackend(); return SourceFont(fontRID).GetSupportedVariationList(); }
}
