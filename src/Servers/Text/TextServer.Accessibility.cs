namespace Electron2D;

public abstract partial class TextServer
{
    internal (int Start, int End, string Family, int Size, int Weight, bool Italic, string Language, TextDirection Direction)[] CaptureStyleMetadata(RID shaped)
    {
        var view = View(shaped); var runs = view.Layout.ServerRuns(view.Line, view.Offset, view.Source.ServerGlyphSpans()); var result = new (int Start, int End, string Family, int Size, int Weight, bool Italic, string Language, TextDirection Direction)[runs.Count];
        for (var i = 0; i < runs.Count; i++)
        {
            var run = runs[i]; var font = run.Font; var family = ""; var weight = 400; var italic = false;
            if (font.IsValid()) { family = FontGetName(font); weight = FontGetWeight(font); italic = (FontGetStyle(font) & FontStyle.Italic) != 0; }
            result[i] = (run.Range.X - view.Start, run.Range.Y - view.Start, family, run.Size, weight, italic, run.Span.Language, run.Direction);
        }
        return result;
    }
}
