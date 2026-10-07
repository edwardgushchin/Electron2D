using System.Reflection;
using Electron2D;

internal static class FontTestFixtures
{
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
