namespace Electron2D;

public abstract partial class TextServer
{
    /// <summary>Prepares shaped break opportunities for the default line-breaking operations.</summary>
    /// <param name="shaped">A live shaped buffer under operation admission.</param><returns>Whether its break state is ready.</returns>
    /// <remarks>The default breaking operation preserves a false optional result and continues with current glyph state.</remarks>
    protected virtual bool OnShapedTextUpdateBreaks(RID shaped)
    { using var nativeTextScope = EnterNativeBackend(shaped); _ = View(shaped); return true; }
    /// <summary>Prepares shaped justification opportunities for default fitting and word-boundary operations.</summary>
    /// <param name="shaped">A live shaped buffer under operation admission.</param><returns>Whether opportunity state is ready.</returns>
    /// <remarks>The default operation continues with current state when this optional callback returns false.</remarks>
    protected virtual bool OnShapedTextUpdateJustificationOps(RID shaped)
    { using var nativeTextScope = EnterNativeBackend(shaped); _ = View(shaped); return true; }
}
