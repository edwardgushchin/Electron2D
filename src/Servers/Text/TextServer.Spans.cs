namespace Electron2D;

public abstract partial class TextServer
{
    /// <summary>Replaces a span's ordered fonts, size and features without changing its text, language or metadata.</summary>
    /// <param name="shaped">Live owned or borrowed buffer.</param><param name="index">Slice-local text-or-object span index.</param>
    /// <param name="fonts">One to sixty-four live borrowed fonts, copied in source order.</param><param name="size">Positive logical font size.</param><param name="openTypeFeatures">Copied feature overrides; null removes the previous overrides.</param>
    /// <remarks>A successful update detaches a substring and invalidates its prepared geometry. Inline-object geometry remains explicitly authored.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The index or size is invalid.</exception>
    /// <exception cref="ArgumentException">The font list or feature count exceeds its authoring limits.</exception>
    /// <exception cref="ArgumentNullException">A supplied font is null.</exception>
    /// <exception cref="ObjectDisposedException">A supplied font has retired.</exception>
    /// <exception cref="InvalidOperationException">The buffer is retired, off-owner or active.</exception>
    public void ShapedSetSpanUpdateFont(RID shaped, int index, ReadOnlySpan<Font> fonts, int size, IReadOnlyDictionary<string, int>? openTypeFeatures = null)
    {
        _ = Span(shaped, index);
        using var mutation = Mutation(shaped);
        mutation.Buffer.ServerUpdateFont(index, fonts, size, openTypeFeatures); mutation.Commit();
    }
    /// <summary>Returns word ranges delimited by selected shaped-cluster properties.</summary>
    /// <param name="shaped">Live shaped buffer.</param><param name="graphemeFlags">Delimiter properties; initially Space and Punctuation.</param><param name="skipGraphemeFlags">Properties suppressing a delimiter; initially Virtual.</param>
    /// <returns>Independent alternating global Unicode scalar starts and exclusive ends in logical order.</returns>
    /// <remarks>Delimiter clusters are excluded. Virtual opportunities are boundary points and never remove source characters.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Either mask contains undefined bits.</exception>
    public int[] ShapedTextGetWordBreaks(RID shaped, TextGraphemeFlags graphemeFlags = TextGraphemeFlags.Space | TextGraphemeFlags.Punctuation, TextGraphemeFlags skipGraphemeFlags = TextGraphemeFlags.Virtual)
    {
        if (((int)(graphemeFlags | skipGraphemeFlags) & ~16383) != 0) throw new ArgumentOutOfRangeException(nameof(graphemeFlags));
        var view = View(shaped); return view.Layout.ServerWordBreaks(view.Start - view.Offset, view.End - view.Offset, view.Offset, graphemeFlags, skipGraphemeFlags);
    }
}
