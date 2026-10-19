using System.Text;

namespace Electron2D;

public abstract partial class TextServer
{
    /// <summary>Adjusts current shaped advances using available justification opportunities.</summary><param name="shaped">Live buffer.</param><param name="width">Finite target advance width.</param><param name="justificationFlags">Defined justification flags.</param><returns>The resulting ceiled advance, which may exceed the target when no further shrink is possible.</returns>
    /// <remarks>Raw clusters and hits follow fitted advances; trimming keeps its independent display view. Formatting preserves a substring's parent relation.</remarks>
    public float ShapedTextFitToWidth(RID shaped, float width, TextJustificationFlags justificationFlags = TextJustificationFlags.Kashida | TextJustificationFlags.WordBound)
    {
        if (!float.IsFinite(width)) throw new ArgumentOutOfRangeException(nameof(width)); if (((int)justificationFlags & ~255) != 0) throw new ArgumentOutOfRangeException(nameof(justificationFlags));
        using var mutation = LayoutBuffer(shaped, out var b); var result = b.ServerFit(width, justificationFlags); mutation?.Commit(false); return result;
    }
    /// <summary>Chooses an independent visible glyph range and optional ellipsis without removing original clusters.</summary><param name="shaped">Live buffer.</param><param name="width">Finite available advance width; nonpositive disables trimming.</param><param name="overrunTrimFlags">Defined trimming flags; NoTrim resets the display.</param>
    public void ShapedTextOverrunTrimToWidth(RID shaped, float width = 0, TextOverrunFlags overrunTrimFlags = TextOverrunFlags.NoTrim)
    {
        if (!float.IsFinite(width)) throw new ArgumentOutOfRangeException(nameof(width)); if (((int)overrunTrimFlags & ~63) != 0) throw new ArgumentOutOfRangeException(nameof(overrunTrimFlags));
        using var mutation = LayoutBuffer(shaped, out var b); b.ServerTrim(width, overrunTrimFlags); mutation?.Commit(false);
    }
    /// <summary>Returns the current visual glyph-record trim boundary.</summary><param name="shaped">Live buffer.</param><returns>A buffer-local glyph boundary, or minus one when untrimmed/unprepared.</returns>
    public int ShapedTextGetTrimPos(RID shaped) => TrimPosition(shaped, false);
    /// <summary>Returns the current visual glyph-record ellipsis boundary.</summary><param name="shaped">Live buffer.</param><returns>A buffer-local glyph boundary, or minus one when no ellipsis is active.</returns>
    public int ShapedTextGetEllipsisPos(RID shaped) => TrimPosition(shaped, true);
    private static int TrimPosition(RID shaped, bool ellipsis)
    {
        var owner = TextBuffers.Resolve(shaped);
        if (owner.Owner is TextParagraph paragraph) return paragraph.ServerTrimPosition(owner.Line, shaped, ellipsis);
        var line = (TextLine)owner.Owner; line.ServerAccess(); return line.ServerTrimPosition(ellipsis);
    }
    /// <summary>Sets the Unicode scalar used by subsequent explicit trimming.</summary><param name="shaped">Live buffer.</param><param name="character">Valid Unicode scalar; unsupported glyphs fall back to three periods when available.</param><exception cref="ArgumentOutOfRangeException">The value is not a Unicode scalar.</exception>
    public void ShapedTextSetCustomEllipsis(RID shaped, int character)
    {
        if (!Rune.IsValid(character)) throw new ArgumentOutOfRangeException(nameof(character)); if (ShapedTextGetCustomEllipsis(shaped) == character) return;
        using var mutation = LayoutBuffer(shaped, out var b); b.ServerSetEllipsis(character); mutation?.Commit(false);
    }
    /// <summary>Returns the configured ellipsis Unicode scalar.</summary><param name="shaped">Live buffer.</param><returns>The scalar; initially U+2026. Borrowed paragraph lines inherit their paragraph symbol.</returns>
    public int ShapedTextGetCustomEllipsis(RID shaped)
    {
        var owner = TextBuffers.Resolve(shaped);
        if (owner.Owner is TextParagraph paragraph) return paragraph.ServerLineEllipsis(owner.Line, shaped);
        var line = (TextLine)owner.Owner; line.ServerAccess(); return line.ServerEllipsis;
    }
    /// <summary>Replaces the punctuation scalar list used when classifying justification opportunities.</summary><param name="shaped">Live buffer.</param><param name="punctuation">Copied immutable list; empty selects Unicode defaults.</param>
    public void ShapedTextSetCustomPunctuation(RID shaped, string punctuation)
    {
        ArgumentNullException.ThrowIfNull(punctuation); if (punctuation.Length > 33554432) throw new ArgumentException("Punctuation exceeds text limits.", nameof(punctuation));
        if (ReadSource(shaped).ServerPunctuation == punctuation) return; using var mutation = Mutation(shaped); mutation.Buffer.ServerSetPunctuation(punctuation); mutation.Commit();
    }
    /// <summary>Returns the custom punctuation list.</summary><param name="shaped">Live buffer.</param><returns>The immutable authored value, or empty for defaults.</returns>
    public string ShapedTextGetCustomPunctuation(RID shaped) => ReadSource(shaped).ServerPunctuation;
    /// <summary>Sets extra signed buffer spacing independently of font-source spacing.</summary><param name="shaped">Live buffer.</param><param name="spacing">Glyph, Space, Top or Bottom; Max is not selectable.</param><param name="value">Signed logical pixels.</param>
    public void ShapedTextSetSpacing(RID shaped, TextSpacingType spacing, int value)
    {
        ValidateSpacing(spacing); if (ReadSource(shaped).ServerSpacing(spacing) == value) return;
        using var mutation = Mutation(shaped); mutation.Buffer.ServerSetSpacing(spacing, value); mutation.Commit();
    }
    /// <summary>Returns extra buffer spacing.</summary><param name="shaped">Live buffer.</param><param name="spacing">Selectable spacing category.</param><returns>Signed logical pixels, initially zero.</returns>
    public int ShapedTextGetSpacing(RID shaped, TextSpacingType spacing) { ValidateSpacing(spacing); return ReadSource(shaped).ServerSpacing(spacing); }
    private static void ValidateSpacing(TextSpacingType spacing) { if ((uint)spacing >= 4) throw new ArgumentOutOfRangeException(nameof(spacing)); }
    private static BufferMutation? LayoutBuffer(RID rid, out TextLine buffer)
    {
        var owner = TextBuffers.Resolve(rid);
        if (owner.Owner is TextLine line) { line.ServerAccess(); buffer = line; return null; }
        var parent = (TextParagraph)owner.Owner; buffer = parent.ServerMutableLine(owner.Line, rid, out var pending);
        if (!pending) return null;
        try { parent.ServerPrepareLayoutLine(owner.Line, rid, buffer); return new(buffer, parent, owner.Line); }
        catch { buffer.Dispose(); throw; }
    }
}
