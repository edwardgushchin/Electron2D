using Electron2D;

internal static class TextServerHexCodeTests
{
    internal static void Run()
    {
        var server = TextServerManager.GetPrimaryInterface();
        (int Scalar, int Width)[] cases = [(0, 8), (0xFF, 8), (0x100, 12), (0xFFFF, 12), (0x10000, 16), (0x10FFFF, 16)];
        foreach (var (scalar, width) in cases)
        {
            Check(server.GetHexCodeBoxSize(15, scalar) == new Vector2(width, 15), "Digit-count boundary and trailing advance.");
            Check(server.GetHexCodeBoxSize(22, scalar) == new Vector2(width, 15) && server.GetHexCodeBoxSize(23, scalar) == new Vector2(width * 2, 30), "Pixel-scale rounding boundary.");
            Check(server.GetHexCodeBoxSize(int.MinValue, scalar) == new Vector2(width, 15) && server.GetHexCodeBoxSize(int.MaxValue, scalar).IsFinite(), "Signed size extremes remain finite.");
        }
        foreach (var scalar in new[] { -1, 0xD800, 0xDFFF, 0x110000, int.MaxValue })
            Reject<ArgumentOutOfRangeException>(() => server.GetHexCodeBoxSize(15, scalar));
        using var painter = new Painter();
        var vertices = new List<CanvasVertex>(); var batches = new List<CanvasBatch>();
        void Record()
        {
            painter.InvalidateCanvas(); painter.PrepareCanvas(); vertices.Clear(); batches.Clear(); painter.AppendCanvas(vertices, batches, Transform.Identity);
        }
        Record();
        Rect2[] expected =
        [
            new(0, 0, 1, 15), new(6, 0, 1, 15), new(0, 0, 7, 1), new(0, 14, 7, 1),
            new(4, 2, 1, 3), new(4, 4, 1, 3), new(2, 8, 3, 1), new(4, 8, 1, 3),
            new(2, 12, 3, 1), new(2, 10, 1, 3), new(2, 10, 3, 1)
        ];
        Check(vertices.Count == expected.Length * 6, "Two-digit replacement records the exact eleven frame/segment rectangles.");
        for (var i = 0; i < expected.Length; i++)
        {
            var min = vertices[i * 6].Position; var max = min;
            for (var j = 0; j < 6; j++) { var v = vertices[i * 6 + j]; min = min.Min(v.Position); max = max.Max(v.Position); Check(v.Color == Colors.Cyan, "Exact supplied color."); }
            Check(new Rect2(min, max - min) == new Rect2(expected[i].Position + new Vector2(10.25f, 7.5f), expected[i].Size), "Fractional origin and truncated baseline offset.");
        }
        foreach (var scalar in new[] { 0, 0x100, 0x10000, 0x10FFFF })
        {
            painter.Scalar = scalar; Record();
            Check((vertices.Count == 0) == (scalar == 0), "Only zero omits geometry.");
        }
        foreach (var scalar in new[] { 0, 0x12 })
        {
            Reject<ArgumentNullException>(() => server.DrawHexCodeBox(null!, 15, Vector2.Zero, scalar, Colors.White));
            Reject<InvalidOperationException>(() => server.DrawHexCodeBox(painter, 15, Vector2.Zero, scalar, Colors.White));
            Task.Run(() => Reject<InvalidOperationException>(() => server.DrawHexCodeBox(painter, 15, Vector2.Zero, scalar, Colors.White))).GetAwaiter().GetResult();
        }
        painter.ValidateErrors = true; Record(); painter.ValidateErrors = false;
        for (var i = 0; i < 64; i++) { painter.Scalar = (i & 1) == 0 ? 0x10FFFF : 0xABCD; Record(); }
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) { painter.Scalar = (i & 1) == 0 ? 0x10FFFF : 0xABCD; Record(); server.GetHexCodeBoxSize(15, painter.Scalar); }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated == 0, "64 warm public query/record/append cycles owner bytes=" + allocated);
        using var retired = new Entity(); retired.Dispose();
        Reject<ObjectDisposedException>(() => server.DrawHexCodeBox(retired, 15, Vector2.Zero, 0, Colors.White));
        Console.WriteLine("TextServer hexadecimal replacement: scalar/scale boundaries, exact baseline geometry/color, validation/owner/lifetime and 64 warm zero-byte cycles passed.");
    }
    private sealed class Painter : Entity
    {
        internal int Scalar = 0x12;
        internal bool ValidateErrors;
        protected override void OnDraw()
        {
            if (ValidateErrors) VerifyErrors();
            TextServerManager.GetPrimaryInterface().DrawHexCodeBox(this, 15, new(10.25f, 19.5f), Scalar, Colors.Cyan);
        }
        private void VerifyErrors()
        {
            var server = TextServerManager.GetPrimaryInterface();
            foreach (var scalar in new[] { 0, 0x12 })
            {
                Reject<ArgumentException>(() => server.DrawHexCodeBox(this, 15, new(float.NaN, 0), scalar, Colors.White));
                Reject<ArgumentException>(() => server.DrawHexCodeBox(this, 15, Vector2.Zero, scalar, new(float.PositiveInfinity, 0, 0)));
            }
            Reject<ArgumentOutOfRangeException>(() => server.DrawHexCodeBox(this, 15, Vector2.Zero, 0xD800, Colors.White));
        }
    }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException("TextServer hex code: " + message); }
}
