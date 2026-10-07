namespace Electron2D;

public partial class TextEdit
{
    private bool _codeOverlayDrawing;
    internal void SetCodeDrawingScope(bool enabled) => _codeOverlayDrawing = enabled;
    internal bool IsCodeLineHidden(int line) => AtLine(line).Hidden;
    internal void SetCodeLineHidden(int line, bool hidden)
    {
        EnsureTextMutable(); var value = AtLine(line); if (value.Hidden == hidden) return; value.Hidden = hidden; InvalidateTextLayout();
    }
    internal bool TryGetCodeGutterMetadata<T>(int line, int gutter, out T value)
    {
        if (AtGutterCell(line, gutter).Metadata is GutterMetadata<T> stored) { value = stored.Value; return true; }
        value = default!; return false;
    }
}
