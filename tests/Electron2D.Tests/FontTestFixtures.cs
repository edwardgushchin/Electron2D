using System.Reflection;
using Electron2D;

internal static class FontTestFixtures
{
    internal static readonly byte[] MSDF = Read(typeof(FontTestFixtures).Assembly, "TestFonts.MSDFTest.otf");
    internal static readonly byte[] Metadata = Read(typeof(FontTestFixtures).Assembly, "TestFonts.MetadataTest.ttf");
    internal static readonly byte[] MetadataBudget = Read(typeof(FontTestFixtures).Assembly, "TestFonts.MetadataBudgetTest.ttf");
    internal static readonly byte[] MetadataDuplicate = Read(typeof(FontTestFixtures).Assembly, "TestFonts.MetadataDuplicateTest.ttf");
    internal static readonly byte[] ColorStrike = Read(typeof(FontTestFixtures).Assembly, "TestFonts.ColorStrikeTest.ttf");
    internal static readonly byte[] Strike = Read(typeof(FontTestFixtures).Assembly, "TestFonts.StrikeTest.ttf");
    internal static readonly byte[] OpenSans = Read(typeof(Font).Assembly, "Electron2D.Fonts.OpenSans_SemiBold.woff2");
    internal static readonly byte[] Arabic = Read(typeof(FontTestFixtures).Assembly, "TestFonts.Vazirmatn_Regular.woff2");
    internal static readonly byte[] Color = Read(typeof(FontTestFixtures).Assembly, "TestFonts.ColorTest.ttf");
    internal static readonly byte[] Hebrew = Read(typeof(FontTestFixtures).Assembly, "TestFonts.NotoSansHebrew_Regular.woff2");
    internal static readonly byte[] CJK = Read(typeof(FontTestFixtures).Assembly, "TestFonts.DroidSansFallback.woff2");
    internal static readonly byte[] Variable = Read(typeof(FontTestFixtures).Assembly, "TestFonts.VariationTest.ttf");
    internal static readonly byte[] Palette = Read(typeof(FontTestFixtures).Assembly, "TestFonts.PaletteTest.ttf");
    internal static readonly byte[] Collection = Read(typeof(FontTestFixtures).Assembly, "TestFonts.CollectionTest.ttc");
    private static byte[] Read(Assembly assembly, string name)
    {
        using var stream = assembly.GetManifestResourceStream(name) ?? throw new InvalidOperationException("Missing font fixture: " + name);
        var bytes = new byte[checked((int)stream.Length)]; stream.ReadExactly(bytes); return bytes;
    }
}
