namespace Electron2D;

public abstract partial class TextServer
{
    /// <summary>Splits a shaped buffer into half-open Unicode scalar line ranges.</summary>
    /// <param name="shaped">Live buffer identity.</param><param name="width">Finite advance limit; nonpositive disables width wrapping.</param><param name="start">Global scalar start, clamped to the buffer.</param><param name="breakFlags">Mandatory, word, cluster, adaptive and edge policies.</param>
    /// <returns>Independent alternating start/end offsets, including separators unless trimmed.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The width is not finite or flags contain undefined bits.</exception>
    public int[] ShapedTextGetLineBreaks(RID shaped, float width, int start = 0, TextLineBreakFlags breakFlags = TextLineBreakFlags.Mandatory | TextLineBreakFlags.WordBound)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextGetLineBreaks(shaped, width, start, breakFlags);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Splits a shaped buffer into half-open Unicode scalar line ranges.</summary>
    /// <param name="shaped">Live buffer identity.</param><param name="width">Finite advance limit; nonpositive disables width wrapping.</param><param name="start">Global scalar start, clamped to the buffer.</param><param name="breakFlags">Mandatory, word, cluster, adaptive and edge policies.</param>
    /// <returns>Independent alternating start/end offsets, including separators unless trimmed.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The width is not finite or flags contain undefined bits.</exception>
    protected virtual int[] OnShapedTextGetLineBreaks(RID shaped, float width, int start = 0, TextLineBreakFlags breakFlags = TextLineBreakFlags.Mandatory | TextLineBreakFlags.WordBound)
    { using var nativeTextScope = EnterNativeBackend(shaped); return LineBreaks(shaped, [width], start, false, breakFlags); }
    /// <summary>Splits a shaped buffer using successive column widths.</summary>
    /// <param name="shaped">Live buffer.</param><param name="width">One to 4096 finite advance limits; nonpositive entries disable width wrapping.</param><param name="start">Global scalar start, clamped to the buffer.</param><param name="once">Stop after one width cycle or mandatory separator; otherwise repeat the widths.</param><param name="breakFlags">The same break policies as the single-width operation.</param>
    /// <returns>Independent alternating global start/end offsets.</returns>
    /// <exception cref="ArgumentException">The width list is empty or exceeds the column limit.</exception><exception cref="ArgumentOutOfRangeException">A width is not finite or flags are invalid.</exception>
    public int[] ShapedTextGetLineBreaksAdv(RID shaped, ReadOnlySpan<float> width, int start = 0, bool once = true, TextLineBreakFlags breakFlags = TextLineBreakFlags.Mandatory | TextLineBreakFlags.WordBound)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextGetLineBreaksAdv(shaped, width, start, once, breakFlags);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Splits a shaped buffer using successive column widths.</summary>
    /// <param name="shaped">Live buffer.</param><param name="width">One to 4096 finite advance limits; nonpositive entries disable width wrapping.</param><param name="start">Global scalar start, clamped to the buffer.</param><param name="once">Stop after one width cycle or mandatory separator; otherwise repeat the widths.</param><param name="breakFlags">The same break policies as the single-width operation.</param>
    /// <returns>Independent alternating global start/end offsets.</returns>
    /// <exception cref="ArgumentException">The width list is empty or exceeds the column limit.</exception><exception cref="ArgumentOutOfRangeException">A width is not finite or flags are invalid.</exception>
    protected virtual int[] OnShapedTextGetLineBreaksAdv(RID shaped, ReadOnlySpan<float> width, int start = 0, bool once = true, TextLineBreakFlags breakFlags = TextLineBreakFlags.Mandatory | TextLineBreakFlags.WordBound)
    { using var nativeTextScope = EnterNativeBackend(shaped); return LineBreaks(shaped, width, start, once, breakFlags); }
    private int[] LineBreaks(RID shaped, ReadOnlySpan<float> widths, int start, bool once, TextLineBreakFlags flags)
    {
        if (widths.Length is 0 or > 4096) throw new ArgumentException("One to 4096 column widths are required.", nameof(widths));
        foreach (var width in widths) if (!float.IsFinite(width)) throw new ArgumentOutOfRangeException(nameof(widths));
        if (((int)flags & ~239) != 0) throw new ArgumentOutOfRangeException(nameof(flags));
        _ = OnShapedTextUpdateBreaks(shaped);
        var v = View(shaped);
        return v.Layout.ServerLineBreaks(widths, Math.Clamp(start, v.Start, v.End) - v.Offset, v.Start - v.Offset, v.End - v.Offset, v.Offset, once, flags);
    }
    /// <summary>Creates an owned substring retaining complete original shaped clusters and source context.</summary>
    /// <param name="shaped">Live source or substring; nested slices address its original parent range.</param><param name="start">Global scalar start.</param><param name="length">Nonnegative scalar length within the source.</param><returns>An independent caller-owned buffer; release with FreeRID.</returns>
    /// <remarks>Fonts, keys and metadata remain borrowed. Parent identity is informational; retained source leases survive its retirement. First successful authoring mutation detaches the substring; fit/trim/ellipsis preserve its context. Partial ligatures and inline objects have no copied glyph.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The range is outside the source or its length is negative.</exception>
    public RID ShapedTextSubstr(RID shaped, int start, int length)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            var dispatcher = implementationCall.Dispatcher; return CreateOwnedRID(false, () => AdoptBuffer(dispatcher, dispatcher.OnShapedTextSubstr(shaped, start, length)));
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Creates an owned substring retaining complete original shaped clusters and source context.</summary>
    /// <param name="shaped">Live source or substring; nested slices address its original parent range.</param><param name="start">Global scalar start.</param><param name="length">Nonnegative scalar length within the source.</param><returns>An independent caller-owned buffer; release with FreeRID.</returns>
    /// <remarks>Fonts, keys and metadata remain borrowed. Parent identity is informational; retained source leases survive its retirement. First successful authoring mutation detaches the substring; fit/trim/ellipsis preserve its context. Partial ligatures and inline objects have no copied glyph.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The range is outside the source or its length is negative.</exception>
    protected virtual RID OnShapedTextSubstr(RID shaped, int start, int length)
    {
        using var nativeTextScope = EnterNativeBackend(shaped);
        var v = View(shaped); var parent = shaped;
        var owner = TextBuffers.Resolve(shaped);
        if (owner.Owner is TextParagraph paragraph)
        { var full = paragraph.ServerLineParent(owner.Line, shaped); if (full != default) { parent = full; v = View(full); } }
        if (v.Source.ServerShapingSource is { } original)
        { parent = v.Source.ServerParent; v = TextBuffers.TryResolve(parent, out _) ? View(parent) : new(original, original.ServerLayout, 0, original.ServerOffset, original.ServerOffset + original.ScalarCount, original.ServerOffset); }
        if (length < 0 || start < v.Start || start > v.End || length > v.End - start) throw new ArgumentOutOfRangeException(nameof(length));
        var copy = v.Source.ServerSlice(start - v.Offset, start - v.Offset + length, completeObjectsOnly: true);
        try { copy.ServerAttachSubstring(v.Source, parent); return Own(copy); }
        catch { if (!copy.IsDisposed) copy.Dispose(); throw; }
    }
    /// <summary>Returns the original substring parent identity.</summary><param name="shaped">Live buffer.</param><returns>The parent RID, possibly retired, or an empty RID for independent/detached buffers.</returns>
    public RID ShapedTextGetParent(RID shaped)
    {
        var implementationCall = EnterImplementation(shaped, false);
        Exception? callbackFailure = null;
        try
        {
            return implementationCall.Dispatcher.OnShapedTextGetParent(shaped);
        }
        catch (Exception failure) { callbackFailure = failure; throw; }
        finally { implementationCall.Dispose(callbackFailure); }
    }

    /// <summary>Returns the original substring parent identity.</summary><param name="shaped">Live buffer.</param><returns>The parent RID, possibly retired, or an empty RID for independent/detached buffers.</returns>
    protected virtual RID OnShapedTextGetParent(RID shaped)
    {
        using var nativeTextScope = EnterNativeBackend(shaped);
        var owner = TextBuffers.Resolve(shaped);
        return owner.Owner is TextParagraph paragraph ? paragraph.ServerLineParent(owner.Line, shaped) : ReadSource(shaped).ServerParent;
    }
}
