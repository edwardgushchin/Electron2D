namespace Electron2D;

public abstract partial class CanvasItem
{
    private int _textClipDepth;
    internal TextRecordingClip BeginTextRecordingClip(Rect2? clip)
    {
        if (clip is null) return default;
        EnsureDrawing();
        if (_textClipDepth >= 64) throw new InvalidOperationException("Text clip nesting exceeds sixty-four scopes.");
        RecordTextClip(clip); _textClipDepth++; return new(this);
    }
    private void RecordTextClip(Rect2? clip) => (_canvasCommands ??= []).Add(new(false, default, default, Colors.White, 0, false,
        Transform.Identity, TextClip: new CanvasTextClip(clip)));
    internal readonly struct TextRecordingClip(CanvasItem? owner) : IDisposable
    {
        public void Dispose()
        { if (owner is null) return; owner.RecordTextClip(null); owner._textClipDepth--; }
    }
    private static Rect2i TextClipPixels(Rect2 rect)
    {
        var left = Math.Clamp(Math.Floor(rect.Position.X), int.MinValue / 2d, int.MaxValue / 2d);
        var top = Math.Clamp(Math.Floor(rect.Position.Y), int.MinValue / 2d, int.MaxValue / 2d);
        var right = Math.Clamp(Math.Ceiling(rect.End.X), int.MinValue / 2d, int.MaxValue / 2d);
        var bottom = Math.Clamp(Math.Ceiling(rect.End.Y), int.MinValue / 2d, int.MaxValue / 2d);
        return new((int)left, (int)top, Math.Max(0, (int)(right - left)), Math.Max(0, (int)(bottom - top)));
    }
}
