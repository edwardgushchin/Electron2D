namespace Electron2D;

public abstract partial class TextServer
{
    /// <summary>Returns the two baseline-relative caret shapes at a Unicode scalar position.</summary><param name="shaped">Live buffer identity.</param><param name="position">Global scalar caret position.</param><returns>Signed leading/trailing rectangles and their directions; absent sides are zero.</returns>
    public TextCaretInfo ShapedTextGetCarets(RID shaped, int position)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextGetCarets(shaped, position);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Returns the two baseline-relative caret shapes at a Unicode scalar position.</summary><param name="shaped">Live buffer identity.</param><param name="position">Global scalar caret position.</param><returns>Signed leading/trailing rectangles and their directions; absent sides are zero.</returns>
    protected virtual TextCaretInfo OnShapedTextGetCarets(RID shaped, int position)
    { using var nativeTextScope = EnterNativeBackend(shaped); var v = View(shaped); return v.Layout.ServerCarets(v.Line, (long)position - v.Offset, v.Start - v.Offset, v.End - v.Offset); }
    /// <summary>Returns ordered visual advance intervals for a logical scalar selection.</summary><param name="shaped">Live buffer.</param><param name="start">One global scalar endpoint.</param><param name="end">The other endpoint; reversed ranges are accepted.</param><returns>An independent interval array; inline objects retain gaps.</returns><remarks>Visual interval adjacency uses a fixed .001 logical-pixel tolerance.</remarks>
    public Vector2[] ShapedTextGetSelection(RID shaped, int start, int end)
    { using var read = ReadSelection(shaped, start, end); return read.Ranges.ToArray(); }

    /// <summary>Returns ordered visual advance intervals for a logical scalar selection.</summary><param name="shaped">Live buffer.</param><param name="start">One global scalar endpoint.</param><param name="end">The other endpoint; reversed ranges are accepted.</param><returns>An independent interval array; inline objects retain gaps.</returns><remarks>Visual interval adjacency uses a fixed .001 logical-pixel tolerance. Borrowed intervals remain valid until the next operation on the buffer. Public callers receive copies.</remarks>
    protected virtual ReadOnlySpan<Vector2> OnShapedTextGetSelection(RID shaped, int start, int end)
    { using var nativeTextScope = EnterNativeBackend(shaped); var v = View(shaped); return v.Layout.ServerSelection(v.Line, (long)start - v.Offset, (long)end - v.Offset); }
    /// <summary>Returns composite-character end boundaries, including the buffer end.</summary><param name="shaped">Live buffer.</param><returns>Independent global Unicode scalar offsets; empty for empty text.</returns>
    public int[] ShapedTextGetCharacterBreaks(RID shaped)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextGetCharacterBreaks(shaped);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Returns composite-character end boundaries, including the buffer end.</summary><param name="shaped">Live buffer.</param><returns>Independent global Unicode scalar offsets; empty for empty text.</returns>
    protected virtual int[] OnShapedTextGetCharacterBreaks(RID shaped)
    { using var nativeTextScope = EnterNativeBackend(shaped); var v = View(shaped); return v.Layout.ServerCharacterBreaks(v.Start - v.Offset, v.End - v.Offset, v.Offset); }
    /// <summary>Finds the closest composite-character boundary, choosing the preceding one on a tie.</summary><param name="shaped">Live buffer.</param><param name="position">Global scalar position.</param><returns>A global boundary clamped to the buffer range.</returns>
    public int ShapedTextClosestCharacterPos(RID shaped, int position)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextClosestCharacterPos(shaped, position);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Finds the closest composite-character boundary, choosing the preceding one on a tie.</summary><param name="shaped">Live buffer.</param><param name="position">Global scalar position.</param><returns>A global boundary clamped to the buffer range.</returns>
    protected virtual int OnShapedTextClosestCharacterPos(RID shaped, int position) { using var nativeTextScope = EnterNativeBackend(shaped); return CharacterPosition(shaped, position, 0); }
    /// <summary>Finds the first composite-character end strictly after a position.</summary><param name="shaped">Live buffer.</param><param name="position">Global scalar position.</param><returns>The next boundary, or the last boundary when exhausted.</returns>
    public int ShapedTextNextCharacterPos(RID shaped, int position)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextNextCharacterPos(shaped, position);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Finds the first composite-character end strictly after a position.</summary><param name="shaped">Live buffer.</param><param name="position">Global scalar position.</param><returns>The next boundary, or the last boundary when exhausted.</returns>
    protected virtual int OnShapedTextNextCharacterPos(RID shaped, int position) { using var nativeTextScope = EnterNativeBackend(shaped); return CharacterPosition(shaped, position, 1); }
    /// <summary>Finds the composite-character boundary strictly before a position.</summary><param name="shaped">Live buffer.</param><param name="position">Global scalar position.</param><returns>The preceding boundary, clamped to the buffer range.</returns>
    public int ShapedTextPrevCharacterPos(RID shaped, int position)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextPrevCharacterPos(shaped, position);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Finds the composite-character boundary strictly before a position.</summary><param name="shaped">Live buffer.</param><param name="position">Global scalar position.</param><returns>The preceding boundary, clamped to the buffer range.</returns>
    protected virtual int OnShapedTextPrevCharacterPos(RID shaped, int position) { using var nativeTextScope = EnterNativeBackend(shaped); return CharacterPosition(shaped, position, -1); }
    private static int CharacterPosition(RID shaped, int position, int mode)
    { var v = View(shaped); return v.Layout.ServerCharacterPosition(v.Start - v.Offset, v.End - v.Offset, v.Offset, position, mode); }
    /// <summary>Returns the first visual shaped-cluster interval containing a scalar position, including its end.</summary><param name="shaped">Live buffer.</param><param name="position">Global scalar position.</param><returns>Advance offsets, or zero when no eligible glyph or space contains it.</returns>
    public Vector2 ShapedTextGetGraphemeBounds(RID shaped, int position)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextGetGraphemeBounds(shaped, position);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Returns the first visual shaped-cluster interval containing a scalar position, including its end.</summary><param name="shaped">Live buffer.</param><param name="position">Global scalar position.</param><returns>Advance offsets, or zero when no eligible glyph or space contains it.</returns>
    protected virtual Vector2 OnShapedTextGetGraphemeBounds(RID shaped, int position)
    { using var nativeTextScope = EnterNativeBackend(shaped); var v = View(shaped); return v.Layout.ServerGraphemeBounds(v.Line, (long)position - v.Offset); }
    /// <summary>Returns the end of the shaped cluster containing a scalar position.</summary><param name="shaped">Live buffer.</param><param name="position">Global scalar position.</param><returns>The cluster end, or the unchanged position outside clusters.</returns>
    public int ShapedTextNextGraphemePos(RID shaped, int position)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextNextGraphemePos(shaped, position);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Returns the end of the shaped cluster containing a scalar position.</summary><param name="shaped">Live buffer.</param><param name="position">Global scalar position.</param><returns>The cluster end, or the unchanged position outside clusters.</returns>
    protected virtual int OnShapedTextNextGraphemePos(RID shaped, int position) { using var nativeTextScope = EnterNativeBackend(shaped); return GraphemePosition(shaped, position, true); }
    /// <summary>Returns the start of the shaped cluster immediately before a scalar position.</summary><param name="shaped">Live buffer.</param><param name="position">Global scalar position.</param><returns>The cluster start, or the unchanged position outside clusters.</returns>
    public int ShapedTextPrevGraphemePos(RID shaped, int position)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextPrevGraphemePos(shaped, position);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Returns the start of the shaped cluster immediately before a scalar position.</summary><param name="shaped">Live buffer.</param><param name="position">Global scalar position.</param><returns>The cluster start, or the unchanged position outside clusters.</returns>
    protected virtual int OnShapedTextPrevGraphemePos(RID shaped, int position) { using var nativeTextScope = EnterNativeBackend(shaped); return GraphemePosition(shaped, position, false); }
    private static int GraphemePosition(RID shaped, int position, bool next)
    { var v = View(shaped); return v.Layout.ServerGraphemePosition(v.Line, (long)position - v.Offset, next) is { } result ? result + v.Offset : position; }
    /// <summary>Finds the visual glyph record intersected by an advance coordinate.</summary><param name="shaped">Live buffer.</param><param name="coords">Finite baseline advance coordinate.</param><returns>A buffer-local glyph index, or minus one outside all positive advances.</returns><exception cref="ArgumentOutOfRangeException">The coordinate is not finite.</exception>
    public int ShapedTextHitTestGrapheme(RID shaped, float coords)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextHitTestGrapheme(shaped, coords);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Finds the visual glyph record intersected by an advance coordinate.</summary><param name="shaped">Live buffer.</param><param name="coords">Finite baseline advance coordinate.</param><returns>A buffer-local glyph index, or minus one outside all positive advances.</returns><exception cref="ArgumentOutOfRangeException">The coordinate is not finite.</exception>
    protected virtual int OnShapedTextHitTestGrapheme(RID shaped, float coords)
    { using var nativeTextScope = EnterNativeBackend(shaped); if (!float.IsFinite(coords)) throw new ArgumentOutOfRangeException(nameof(coords)); var v = View(shaped); return v.Layout.ServerHitGrapheme(v.Line, coords); }
    /// <summary>Reports whether the buffer has a nonzero, nonvirtual glyph index.</summary><param name="shaped">Live buffer.</param><returns>True for nonzero ordinary or hexadecimal-placeholder indices, independently of display trimming; inline objects do not count.</returns>
    public bool ShapedTextHasVisibleChars(RID shaped)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextHasVisibleChars(shaped);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Reports whether the buffer has a nonzero, nonvirtual glyph index.</summary><param name="shaped">Live buffer.</param><returns>True for nonzero ordinary or hexadecimal-placeholder indices, independently of display trimming; inline objects do not count.</returns>
    protected virtual bool OnShapedTextHasVisibleChars(RID shaped) { using var nativeTextScope = EnterNativeBackend(shaped); var v = View(shaped); return v.Layout.ServerVisible(v.Line); }
    /// <summary>Returns the direction represented by more overlapping shaped clusters.</summary><param name="shaped">Live buffer.</param><param name="start">Global scalar endpoint.</param><param name="end">Other endpoint; reversed ranges are accepted.</param><returns>LTR, RTL, or Auto for a tie or empty range.</returns>
    public TextDirection ShapedTextGetDominantDirectionInRange(RID shaped, int start, int end)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextGetDominantDirectionInRange(shaped, start, end);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Returns the direction represented by more overlapping shaped clusters.</summary><param name="shaped">Live buffer.</param><param name="start">Global scalar endpoint.</param><param name="end">Other endpoint; reversed ranges are accepted.</param><returns>LTR, RTL, or Auto for a tie or empty range.</returns>
    protected virtual TextDirection OnShapedTextGetDominantDirectionInRange(RID shaped, int start, int end)
    { using var nativeTextScope = EnterNativeBackend(shaped); var v = View(shaped); return v.Layout.ServerDominantDirection(v.Line, (long)start - v.Offset, (long)end - v.Offset); }
}
