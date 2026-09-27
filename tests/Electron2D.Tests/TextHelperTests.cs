using Electron2D;

internal static class TextHelperTests
{
    internal static void Run()
    {
        VerifyKashida();
        VerifyMissingGlyph();
        Console.WriteLine("Text justification opportunities and hexadecimal missing-glyph geometry passed; warm recording=0 B.");
    }

    private static void VerifyKashida()
    {
        // Expected indices were produced by the pinned C++ opportunity function using ICU Unicode 17.
        (uint[] Text, int Index)[] cases =
        [
            ([], -1), ([0x640], 0), ([0x633], -1), ([0x633, 0x628], 0), ([0x633, 0x200C, 0x628], -1),
            ([0x628, 0x629], 0), ([0x628, 0x62F], 0), ([0x628, 0x647], 0), ([0x628, 0x647, 0x628], -1),
            ([0x628, 0x627], 0), ([0x644, 0x627], -1), ([0x628, 0x644], 0), ([0x628, 0x637], 0),
            ([0x628, 0x643], 0), ([0x628, 0x6AF], 0), ([0x628, 0x628, 0x631], 0), ([0x628, 0x6CC, 0x6D2], 0),
            ([0x628, 0x648], 0), ([0x628, 0x639], 0), ([0x628, 0x642], 0), ([0x628, 0x641], 0), ([0x628, 0x631], 0),
            ([0x627, 0x631], -1), ([0x628, 0x64E, 0x62F], 1), ([0x633, 0x64E], -1),
            ([0x633, 0x628, 0x633, 0x628], 2), ([0x640, 0x633, 0x628, 0x640], 3),
            ([0x628, 0x8BB], 0), ([0x628, 0x8B4], 0), ([0x628, 0x8B5], 0)
        ];
        foreach (var item in cases)
            Check(TextJustification.FindKashida(item.Text, 0, item.Text.Length) == item.Index, "Kashida priorities, joins and transparent marks match the independent source oracle.");
        uint[] slice = [0x640, 0x628, 0x62F, 0x640];
        Check(TextJustification.FindKashida(slice, 1, 3) == 1 && TextJustification.FindKashida(slice, 3, 3) == -1,
            "Logical subranges retain absolute scalar indices and ignore adjoining text.");
        Reject<ArgumentOutOfRangeException>(() => TextJustification.FindKashida(slice, -1, 2));
        Reject<ArgumentOutOfRangeException>(() => TextJustification.FindKashida(slice, 1, 5));
        Reject<ArgumentOutOfRangeException>(() => TextJustification.FindKashida(slice, 3, 2));
    }

    private static void VerifyMissingGlyph()
    {
        Check(TextMissingGlyph.Size(15, 0xFF) == new Vector2(8, 15) && TextMissingGlyph.Size(15, 0x100) == new Vector2(12, 15) &&
            TextMissingGlyph.Size(15, 0x10000) == new Vector2(16, 15), "Missing glyphs choose two, four or six hexadecimal digits with one trailing advance unit.");
        Check(TextMissingGlyph.Size(22, 0xFF) == new Vector2(8, 15) && TextMissingGlyph.Size(23, 0xFF) == new Vector2(16, 30) &&
            TextMissingGlyph.Size(-1, 0xFF) == new Vector2(8, 15), "Missing glyph scale rounds to the nearest positive pixel scale.");
        using var painter = new Painter { Scalar = 0x12, Baseline = new(10.25f, 19.5f) };
        var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>();
        painter.PrepareCanvas(); painter.AppendCanvas(vertices, batches, Transform.Identity);
        Rect2[] relative =
        [
            new(0, 0, 1, 15), new(6, 0, 1, 15), new(0, 0, 7, 1), new(0, 14, 7, 1),
            new(4, 2, 1, 3), new(4, 4, 1, 3),
            new(2, 8, 3, 1), new(4, 8, 1, 3), new(2, 12, 3, 1), new(2, 10, 1, 3), new(2, 10, 3, 1)
        ];
        Check(vertices.Count == relative.Length * 6, "The source two-digit seven-segment pattern records eleven filled rectangles.");
        for (var i = 0; i < relative.Length; i++)
        {
            var minimum = vertices[i * 6].Position; var maximum = minimum;
            for (var j = 0; j < 6; j++)
            {
                var vertex = vertices[i * 6 + j]; minimum = minimum.Min(vertex.Position); maximum = maximum.Max(vertex.Position);
                Check(vertex.Color == Colors.Cyan, "Missing glyphs preserve supplied color.");
            }
            Check(new Rect2(minimum, maximum - minimum) == new Rect2(relative[i].Position + new Vector2(10.25f, 7.5f), relative[i].Size),
                "Hexadecimal digits use source segment order and a truncated integer baseline offset.");
        }
        painter.Scalar = 0; painter.InvalidateCanvas(); painter.PrepareCanvas(); vertices.Clear(); batches.Clear(); painter.AppendCanvas(vertices, batches, Transform.Identity);
        Check(vertices.Count == 0 && TextMissingGlyph.Size(15, 0) == new Vector2(8, 15), "Zero scalar retains placeholder metrics but records no rectangle.");
        Reject<InvalidOperationException>(() => TextMissingGlyph.Draw(painter, 15, Vector2.Zero, 1, Colors.White));
        uint[] word = [0x628, 0x64E, 0x62F]; var observed = 0;
        void Cycle(int pass)
        {
            observed += TextJustification.FindKashida(word, 0, word.Length);
            painter.Scalar = (pass & 1) == 0 ? 0x10FFFFu : 0xABCDu; painter.FontSize = (pass & 1) == 0 ? 30 : 15;
            painter.InvalidateCanvas(); painter.PrepareCanvas(); vertices.Clear(); batches.Clear(); painter.AppendCanvas(vertices, batches, Transform.Identity);
        }
        for (var pass = 0; pass < 64; pass++) Cycle(pass);
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var pass = 0; pass < 64; pass++) Cycle(pass);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated == 0 && observed == 128, $"Warmed active missing-glyph recording and kashida lookup allocated {allocated} bytes.");
    }

    private sealed class Painter : Entity
    {
        internal uint Scalar;
        internal int FontSize = 15;
        internal Vector2 Baseline;
        protected override void OnDraw() => TextMissingGlyph.Draw(this, FontSize, Baseline, Scalar, Colors.Cyan);
    }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
