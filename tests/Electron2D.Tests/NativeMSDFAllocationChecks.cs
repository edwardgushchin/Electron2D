using Electron2D;

internal static class NativeMSDFAllocationChecks
{
    internal static void Run(Func<(long Current, long Peak)> read, byte[] source)
    {
        using (var warm = Create(source, 48))
            warm.RenderGlyph(0, new(48, 0), warm.GetGlyphIndex(48, 'A', 0));
        var baseline = read();
        long peak, temporary;
        using (var font = Create(source, 1536))
        {
            var index = font.GetGlyphIndex(1536, 'A', 0);
            font.RenderGlyph(0, new(1536, 0), index);
            var size = font.GetGlyphSize(0, new(1536, 0), index);
            Check(size.X > 2 && size.Y > 2 && font.GetTextureCount(0, new(1536, 0)) > 0, "Public MSDF generation did not publish its image.");
            temporary = checked((long)(size.X - 2) * (long)(size.Y - 2) * 16);
            var generated = read(); peak = generated.Peak;
            Check(peak > baseline.Peak && peak >= temporary, "Temporary MSDF pixels bypassed the native high-water mark.");
            for (var i = 0; i < 16; i++) font.RenderGlyph(0, new(1536, 0), index);
            Check(read().Current == generated.Current, "Cached MSDF generation retained new native storage.");
            void Values()
            {
                Check(font.MultichannelSignedDistanceField && font.MSDFSize == 1536 && font.MSDFPixelRange == 16, "Prepared MSDF preferences changed.");
                _ = read();
            }
            for (var i = 0; i < 256; i++) Values();
            AllocationWarmupCheck.Collect();
            for (var i = 0; i < 256; i++) Values();
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 20000; i++) Values();
            var ownBytes = GC.GetAllocatedBytesForCurrentThread() - own; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
            Check(ownBytes == 0 && allBytes == 0, $"Prepared MSDF metadata/read cycles allocated {ownBytes}/{allBytes} managed bytes.");
        }
        Check(read().Current == baseline.Current, "Disposed MSDF font retained native storage.");
        Console.WriteLine($"MSDF allocation: floatPixels={temporary}, nativePeak={peak}, currentDelta={read().Current - baseline.Current}, prepared=20000, managed=0/0; public image/cache/disposal passed.");
    }
    private static FontFile Create(byte[] source, int size) => new()
    {
        Data = source,
        AllowSystemFallback = false,
        MultichannelSignedDistanceField = true,
        MSDFSize = size
    };
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
