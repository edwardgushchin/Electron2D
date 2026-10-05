using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text;
using Electron2D;

internal static class NativeFontPrecisionTests
{
    // Inputs are the pinned Open Sans SemiBold WOFF2 and a fixture with Arabic coverage.
    internal static void Run(byte[] openSansData, byte[] arabicData)
    {
        using (var resource = new FontFile { Data = openSansData })
            Check(resource.GetStringSize("ffi").X > 0, "The public font resource accepts the current packaged process architecture.");
        VerifyMetricsAndShaping(openSansData);
        VerifyMetadataAndRasterMetrics(openSansData);
        VerifyRasterPhases(openSansData);
        VerifyArabic(arabicData);
        VerifyLifetime(openSansData);
        VerifyWarmReuse(openSansData, arabicData);
        Console.WriteLine("Native font precision verifies fractional FT metrics, SFNT/WOFF2 shaping, ligatures, Arabic, clusters, lifetime and warmed zero managed allocations.");
    }

    private static void VerifyMetricsAndShaping(byte[] data)
    {
        using var font = new NativeFontPrecision(data);
        Check(font.FamilyName == "Open Sans" && font.StyleName == "SemiBold" && font.FaceCount == 1 && font.GlyphCount == 1150 && font.UnitsPerEm == 2048,
            "The pinned WOFF2 exposes the expected face metadata through public FreeType records.");
        Check(Marshal.SizeOf<CLong>() == (OperatingSystem.IsWindows() ? 4 : IntPtr.Size), "C long follows LLP64/LP64 ABI rather than assuming pointer size.");
        Check(font.Ascent == 18 && font.Descent == 5 && font.LineHeight == 22 && font.UnderlinePosition == 63 / 64f && font.UnderlineThickness == 25 / 64f,
            $"The face metrics retain independent ascent/descent, line height and fractional underline metrics: {font.Ascent}, {font.Descent}, {font.LineHeight}, {font.UnderlinePosition}, {font.UnderlineThickness}.");
        // Independent C FT_Get_Advance(FT_LOAD_NO_HINTING) oracle at 16 pixels:
        // glyph 36/57/76/3 => 16.16 advances 693248/653312/292352/272384.
        uint[] text = ['A', 'V', 'i', ' ']; int[] expected = [677, 638, 286, 266];
        for (var i = 0; i < text.Length; i++)
        {
            var glyph = font.GetGlyphIndex(text[i]);
            Check(font.GetGlyphAdvance(glyph) == expected[i], "Unhinted advances match the independent FreeType oracle in 26.6 units.");
            var shaped = font.Shape(text.AsSpan(i, 1), NativeTextDirection.LTR);
            Check(shaped.Length == 1 && shaped[0].GlyphIndex == glyph && shaped[0].XAdvance == expected[i], "Shaping consumes fractional FreeType advances without integer pixel truncation.");
        }
        var bounds = font.GetGlyphBounds(font.GetGlyphIndex('A'));
        Check(bounds == new Rect2(0, -734 / 64f, 677 / 64f, 734 / 64f), "Unhinted outline bounds match the independent glyph-slot metrics oracle.");
        var ligature = font.Shape(['f', 'f', 'i'], NativeTextDirection.LTR);
        Check(ligature.Length == 1 && ligature[0].GlyphIndex == 909 && ligature[0].XAdvance == 1026 && ligature[0].Cluster == 0, "Default shaping preserves the ffi ligature and its scalar cluster.");
        var plain = font.Shape(['f', 'f', 'i'], NativeTextDirection.LTR, features: [new(0x6C696761, 0)]);
        Check(plain.Length == 3 && plain[2].Cluster == 2, "Typed OpenType features control shaping without replacing text with per-character drawing.");
        var combining = font.Shape(['a', 0x301], NativeTextDirection.LTR);
        Check(combining.Length == 1 && combining[0].GlyphIndex == 163 && combining[0].Cluster == 0 && combining[0].XAdvance == 594,
            "A decomposed accented grapheme preserves its original scalar cluster.");
        var marks = font.Shape(['x', 0x301], NativeTextDirection.LTR);
        Check(marks.Length == 2 && marks[0].Cluster == 0 && marks[1].Cluster == 0 && marks[1].XAdvance == 0,
            "An uncomposed combining mark remains in the same cluster with no independent advance.");
        var vertical = font.Shape(['A', 'V'], NativeTextDirection.TTB);
        Check(vertical.Length == 2 && vertical[0].XAdvance == 0 && vertical[0].YAdvance < 0 && vertical[0].XOffset != 0,
            "Vertical shaping preserves signed vertical advances and the FreeType vertical origin.");
        font.SetSize(17.5f);
        Check(font.GetGlyphAdvance(font.GetGlyphIndex('A')) != expected[0], "Fractional point-size changes refresh both metric and shaping fonts.");
        font.SetSize(16);
        Check(font.Shape(['A'], NativeTextDirection.LTR)[0].XAdvance == 677, "Restoring a size does not retain stale HarfBuzz scale state.");
        Check(font.Shape([], NativeTextDirection.LTR).IsEmpty, "Empty text yields an empty reusable view.");
        Check(font.GetGlyphIndex(0x10FFFF) == 0, "Missing scalar lookup returns the missing-glyph index.");
    }

    private static void VerifyMetadataAndRasterMetrics(byte[] data)
    {
        using var font = new NativeFontPrecision(data);
        Check(font.FaceFlags == 2585 && font.FaceStyleFlags == 0, "The pinned face exposes SFNT/scalable/horizontal flags without inferring bold from its family name.");
        var os2 = font.GetSFNTTable(0x4F532F32);
        Check(os2.Length == 96 && BinaryPrimitives.ReadUInt16BigEndian(os2.AsSpan(4)) == 600 && BinaryPrimitives.ReadUInt16BigEndian(os2.AsSpan(6)) == 5,
            "The decompressed OS/2 table exposes the source weight and width classes.");
        var first = os2[0]; os2[0] ^= 255;
        Check(font.GetSFNTTable(0x4F532F32)[0] == first, "Metadata table queries return independent caller-owned bytes.");
        Check(font.GetSFNTTable(0x58585858).Length == 0, "An absent optional SFNT table returns an empty buffer.");
        var supported = font.GetSupportedChars();
        Check(supported.Contains('A') && supported.Contains(' ') && supported.Contains('é'), "The FreeType character-map iterator supplies available Unicode characters.");
        var previous = -1;
        foreach (var rune in supported.EnumerateRunes())
        {
            Check(rune.Value > previous && font.GetGlyphIndex((uint)rune.Value) != 0, "Supported characters are ordered unique scalars with actual glyphs.");
            previous = rune.Value;
        }
        var glyph = font.GetGlyphIndex('A');
        // Independent FT_Load_Glyph(FT_LOAD_TARGET_LIGHT) oracle: bearings0/768, extent704/768 in26.6.
        Check(font.GetRasterBounds(glyph) == new Rect2(0, -12, 11, 12), "Raster bearings use LIGHT hinting rather than fractional unhinted outline bounds.");
        Check(font.GetRasterBounds(glyph, true) == font.GetGlyphBounds(glyph), "The explicit no-hint raster query retains unhinted metrics.");
        Check(font.Shape(['A'], NativeTextDirection.LTR)[0].XAdvance == 677, "Raster metric queries cannot contaminate subsequent unhinted shaping advances.");
        Reject<ArgumentOutOfRangeException>(() => font.GetRasterBounds(uint.MaxValue));
        font.Dispose();
        Reject<ObjectDisposedException>(() => font.GetSFNTTable(0x4F532F32));
        Reject<ObjectDisposedException>(() => font.GetSupportedChars());
        Reject<ObjectDisposedException>(() => font.GetRasterBounds(glyph));
    }

    private static void VerifyRasterPhases(byte[] data)
    {
        using var font = new NativeFontPrecision(data);
        // Independent FT2.13.3 C oracle loads a glyph, translates its outline AFTER hinting,
        // then renders or applies a butt-cap/round-join stroke. The bridge uses FT_Set_Transform.
        (int Hint, int Phase, int Radius, int Width, int Height, int Left, int Top, ulong Hash)[] cases =
        [
            (1,0,0,11,12,0,12,0x036B6E204FA2E56CUL),
            (1,16,0,11,12,0,12,0xA485F1A952210A00UL),
            (1,32,0,12,12,0,12,0x5DE754988D0FCB46UL),
            (1,48,0,12,12,0,12,0xC2AD733E0E77978FUL),
            (1,0,16,12,14,-1,13,0x786886C30B299E11UL),
            (1,16,16,13,14,-1,13,0x5DFF5630F0FDC320UL),
            (1,32,16,12,14,0,13,0xE26285AFE5E880B5UL),
            (1,48,16,12,14,0,13,0xF2E467CFDC344EF2UL),
            (0,16,0,11,12,0,12,0x7BD7A09237BE1D5EUL),
            (2,32,0,12,12,0,12,0x56D292E6E3299A30UL),
            (0,48,64,14,14,-1,13,0xB652AC57B09059F6UL),
            (2,16,32,13,14,-1,13,0xAD6C8FB43A54B1EFUL)
        ];
        var glyph = font.GetGlyphIndex('A');
        foreach (var expected in cases)
        {
            var raster = font.Rasterize(glyph, (FontHinting)expected.Hint, expected.Phase, expected.Radius);
            ulong hash = 14695981039346656037;
            foreach (var value in raster.Pixels) { hash ^= value; hash = unchecked(hash * 1099511628211); }
            Check(raster.Width == expected.Width && raster.Height == expected.Height && raster.Left == expected.Left && raster.Top == expected.Top && !raster.Colored && hash == expected.Hash,
                $"Raster hint={expected.Hint},phase={expected.Phase},radius={expected.Radius} must match the independent C pixel and bearing oracle.");
        }
        Check(font.GetGlyphBounds(glyph) == new Rect2(0, -734 / 64f, 677 / 64f, 734 / 64f) && font.Shape(['A'], NativeTextDirection.LTR)[0].XAdvance == 677,
            "Raster phases and strokes cannot leave transformed outlines or hinted advances in subsequent precision queries.");
        Check(font.Rasterize(font.GetGlyphIndex(' '), FontHinting.Light, 48).Pixels.Length == 0, "A whitespace glyph yields a valid empty raster.");
        Reject<ArgumentOutOfRangeException>(() => font.Rasterize(glyph, FontHinting.Light, -1));
        Reject<ArgumentOutOfRangeException>(() => font.Rasterize(glyph, FontHinting.Light, 64));
        Reject<ArgumentOutOfRangeException>(() => font.Rasterize(glyph, FontHinting.Light, 0, -1));
        Reject<ArgumentOutOfRangeException>(() => font.Rasterize(uint.MaxValue, FontHinting.Light));
    }

    private static void VerifyArabic(byte[] data)
    {
        using var font = new NativeFontPrecision(data);
        uint[] arabic = [0x633, 0x644, 0x627, 0x645];
        var shaped = font.Shape(arabic, NativeTextDirection.RTL, 0x41726162, "ar");
        Check(shaped.Length > 0 && shaped.Length < arabic.Length, "Arabic joining includes contextual substitution and the lam-alef ligature.");
        var hasFraction = false;
        for (var i = 0; i < shaped.Length; i++)
        {
            Check(shaped[i].GlyphIndex != 0 && shaped[i].Cluster < arabic.Length && shaped[i].XAdvance > 0, "Arabic output uses covered glyphs and original scalar clusters.");
            if (i > 0) Check(shaped[i].Cluster <= shaped[i - 1].Cluster, "Right-to-left output retains decreasing logical clusters.");
            hasFraction |= (shaped[i].XAdvance & 63) != 0;
        }
        Check(hasFraction, "Arabic advances retain subpixel precision.");
        uint[] joining = [0x628, 0x628];
        var whole = font.Shape(joining, NativeTextDirection.RTL, 0x41726162, "ar");
        var finalGlyph = whole[0].GlyphIndex;
        var initialGlyph = whole[1].GlyphIndex;
        Check((whole[0].Flags & 4) != 0 && (whole[1].Flags & 4) == 0,
            "HarfBuzz marks the join before the second logical beh as safe for tatweel insertion, not the word boundary.");
        var firstRun = font.Shape(joining, 0, 1, NativeTextDirection.RTL, 0x41726162, "ar");
        Check(firstRun.Length == 1 && firstRun[0].GlyphIndex == initialGlyph && firstRun[0].Cluster == 0,
            "A font run retains paragraph post-context for Arabic initial joining.");
        var lastRun = font.Shape(joining, 1, 1, NativeTextDirection.RTL, 0x41726162, "ar");
        Check(lastRun.Length == 1 && lastRun[0].GlyphIndex == finalGlyph && lastRun[0].Cluster == 1,
            "A font run retains paragraph pre-context and absolute scalar clusters for Arabic final joining.");
        var isolatedGlyph = font.Shape([0x628], NativeTextDirection.RTL, 0x41726162, "ar")[0].GlyphIndex;
        Check(initialGlyph != isolatedGlyph && finalGlyph != isolatedGlyph, "The context check distinguishes joined from isolated glyphs.");
    }

    private static void VerifyLifetime(byte[] data)
    {
        Reject<ArgumentException>(() => new NativeFontPrecision([]));
        Reject<InvalidOperationException>(() => new NativeFontPrecision([0, 1, 2, 3]));
        Reject<ArgumentOutOfRangeException>(() => new NativeFontPrecision(data, -1));
        Reject<InvalidOperationException>(() => new NativeFontPrecision(data, int.MaxValue));
        var mutable = (byte[])data.Clone();
        using var font = new NativeFontPrecision(mutable);
        Array.Clear(mutable);
        Check(font.Shape(['A'], NativeTextDirection.LTR)[0].XAdvance == 677, "The native face owns its immutable source bytes independently of the caller.");
        Reject<ArgumentOutOfRangeException>(() => font.SetSize(float.NaN));
        Reject<ArgumentOutOfRangeException>(() => font.SetSize(0));
        Reject<ArgumentOutOfRangeException>(() => font.Shape([65], 2, 0, NativeTextDirection.LTR));
        Reject<ArgumentOutOfRangeException>(() => font.Shape([65], 0, -1, NativeTextDirection.LTR));
        Reject<ArgumentException>(() => font.Shape([0xD800], NativeTextDirection.LTR));
        Reject<ArgumentException>(() => font.Shape([0x110000], NativeTextDirection.LTR));
        Reject<ArgumentOutOfRangeException>(() => font.GetGlyphAdvance(uint.MaxValue));
        Reject<ArgumentOutOfRangeException>(() => font.GetGlyphBounds(uint.MaxValue));
        Reject<ArgumentException>(() => font.Shape(['A'], NativeTextDirection.LTR, language: "en\0x"));
        if (!OperatingSystem.IsBrowser())
        {
            Task.Run(() => Reject<InvalidOperationException>(() => font.SetSize(18))).GetAwaiter().GetResult();
            Task.Run(() => Reject<InvalidOperationException>(font.Dispose)).GetAwaiter().GetResult();
        }
        else Console.WriteLine("The nonthreaded browser profile excludes foreign-thread font rejection checks.");
        font.Dispose(); font.Dispose();
        Reject<ObjectDisposedException>(() => font.Shape(['A'], NativeTextDirection.LTR));
    }

    private static void VerifyWarmReuse(byte[] latinData, byte[] arabicData)
    {
        using var latin = new NativeFontPrecision(latinData);
        using var arabic = new NativeFontPrecision(arabicData);
        uint[] latinText = ['A', 'V', ' ', 'f', 'f', 'i', ' ', 'x', 0x301];
        uint[] arabicText = [0x633, 0x644, 0x627, 0x645];
        void Cycle(int i)
        {
            latin.SetSize((i & 1) == 0 ? 16 : 17);
            _ = latin.Shape(latinText, NativeTextDirection.LTR, 0x4C61746E, "en");
            _ = latin.GetGlyphBounds(36); _ = latin.GetRasterBounds(36); _ = latin.GetGlyphAdvance(36);
            _ = arabic.Shape(arabicText, NativeTextDirection.RTL, 0x41726162, "ar");
        }
        for (var i = 0; i < 64; i++) Cycle(i);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) Cycle(i);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated == 0, $"Warmed shaping, native metrics and size changes allocate {allocated} managed bytes.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
