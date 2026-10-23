using System.Diagnostics;
using Electron2D;

internal static partial class Type1FontTests
{
    private static void Catalog(string directory)
    {
        if (!OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) return;
        var fonts = System.IO.Path.Combine(directory, "catalog"); Directory.CreateDirectory(fonts); Write(fonts, "pfb"); Write(fonts, "pfm");
        var config = System.IO.Path.Combine(directory, "fonts.conf"); System.IO.File.WriteAllText(config, "<?xml version=\"1.0\"?><fontconfig><dir>" + System.Security.SecurityElement.Escape(fonts) + "</dir><cachedir>" + System.Security.SecurityElement.Escape(System.IO.Path.Combine(directory, "cache")) + "</cachedir></fontconfig>");
        var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false }; if (System.IO.Path.GetFileNameWithoutExtension(start.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(typeof(Type1FontTests).Assembly.Location);
        start.Environment["FONTCONFIG_FILE"] = config; start.Environment["ELECTRON2D_TEST_TYPE1"] = "catalog";
        using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync();
        if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Type 1 catalog child timed out."); }
        Check(child.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Type 1 catalog passed."), "Installed source: " + error.GetAwaiter().GetResult());
    }
    internal static void RunCatalog()
    {
        const string family = "Electron2D Type 1 Test";
        Check(OS.GetSystemFonts().Contains(family, StringComparer.Ordinal), "Type 1 native catalog family.");
        using var font = new SystemFont { FontNames = [family], AllowSystemFallback = false }; using var bare = new FontFile { Data = Fixture("pfb"), AllowSystemFallback = false };
        Check(PairWidth(font) < PairWidth(bare) && Glyph(font).Texture != null, "System match retains PFM shaping/raster.");
        using var variation = new FontVariation { BaseFont = font }; Check(PairWidth(variation) == PairWidth(font), "System variation retains metrics.");
        font.MultichannelSignedDistanceField = true; Check(Glyph(font).MSDFPixelRange == 16, "System Type 1 outlines reach MSDF.");
        using var copy = (SystemFont)font.Duplicate(); Check(PairWidth(copy) == PairWidth(font), "System rematch/copy retains metrics.");
        using var fallback = new FontFile { AllowSystemFallback = true }; Check(fallback.PrimaryData!.FindSystemFallback('A', "") is { } source && source.GetKerning(new((int)source.GetGlyphIndex('A'), (int)source.GetGlyphIndex('V')), 24).X < 0, "Automatic owned source retains auxiliary metrics.");
        Console.WriteLine("Type 1 catalog passed.");
    }
}
