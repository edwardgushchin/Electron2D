using System.Diagnostics;
using Electron2D;

internal static partial class FontMSDFTests
{
    private static void Storage(FontFile font, SystemFont system)
    {
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-font-msdf-" + Guid.NewGuid()); Directory.CreateDirectory(directory);
        try
        {
            var path = System.IO.Path.Combine(directory, "font.e2dres"); ResourceSaver.Save(font, path);
            using var restored = ResourceLoader.Load<FontFile>(path, ResourceLoader.CacheMode.Ignore);
            Check(restored.MultichannelSignedDistanceField && restored.MSDFSize == 48 && restored.MSDFPixelRange == 8 && Glyph(restored).MSDFPixelRange == 8, "File archive restores executable settings.");
            var systemPath = System.IO.Path.Combine(directory, "system.e2dres"); ResourceSaver.Save(system, systemPath);
            using var loadedSystem = ResourceLoader.Load<SystemFont>(systemPath, ResourceLoader.CacheMode.Ignore);
            Check(loadedSystem.MultichannelSignedDistanceField && loadedSystem.MSDFSize == 32 && loadedSystem.MSDFPixelRange == 8 && Glyph(loadedSystem).MSDFPixelRange == 8, "System archive stores preferences and rematches.");
            var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false }; if (System.IO.Path.GetFileNameWithoutExtension(start.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(typeof(FontMSDFTests).Assembly.Location); start.ArgumentList.Add("--font-msdf-archive"); start.ArgumentList.Add(path);
            using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("MSDF archive child timed out."); }
            Check(process.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh MSDF archive passed."), "Fresh MSDF load: " + error.GetAwaiter().GetResult());
            var catalog = System.IO.Path.Combine(directory, "catalog"); Directory.CreateDirectory(catalog);
            File.WriteAllBytes(System.IO.Path.Combine(catalog, "color.ttf"), FontTestFixtures.Color);
            var config = System.IO.Path.Combine(directory, "fonts.conf");
            File.WriteAllText(config, "<?xml version=\"1.0\"?><fontconfig><dir>" + System.Security.SecurityElement.Escape(catalog) + "</dir><cachedir>" + System.Security.SecurityElement.Escape(System.IO.Path.Combine(directory, "cache")) + "</cachedir></fontconfig>");
            var colorStart = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false }; if (System.IO.Path.GetFileNameWithoutExtension(colorStart.FileName).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) colorStart.ArgumentList.Add(typeof(FontMSDFTests).Assembly.Location); colorStart.Environment["FONTCONFIG_FILE"] = config; colorStart.Environment["ELECTRON2D_TEST_FONT_MSDF"] = "color-fallback";
            using var colorProcess = Process.Start(colorStart)!; var colorOutput = colorProcess.StandardOutput.ReadToEndAsync(); var colorError = colorProcess.StandardError.ReadToEndAsync();
            if (!colorProcess.WaitForExit(30000)) { colorProcess.Kill(true); throw new TimeoutException("Color fallback child timed out."); }
            Check(colorProcess.ExitCode == 0 && colorOutput.GetAwaiter().GetResult().Contains("Color fallback passed."), "Isolated real color fallback: " + colorError.GetAwaiter().GetResult());
            var nativePath = System.IO.Path.Combine(directory, "font.woff2"); File.WriteAllBytes(nativePath, FontTestFixtures.OpenSans); restored.LoadDynamicFont(nativePath);
            Check(!restored.MultichannelSignedDistanceField && restored.MSDFSize == 48 && restored.MSDFPixelRange == 16, "Dynamic file import restores defaults.");
        }
        finally { Directory.Delete(directory, true); }
    }
    internal static void RunArchive(string path)
    {
        using var font = ResourceLoader.Load<FontFile>(path, ResourceLoader.CacheMode.Ignore);
        Check(font.MultichannelSignedDistanceField && font.MSDFSize == 48 && font.MSDFPixelRange == 8 && Glyph(font).MSDFPixelRange == 8, "Fresh child executes archived distance generation.");
        Console.WriteLine("Fresh MSDF archive passed.");
    }
    internal static void RunColorFallback()
    {
        using var font = new FontFile { Data = FontTestFixtures.OpenSans, MultichannelSignedDistanceField = true };
        var data = font.PrimaryData!.FindSystemFallback('A', "") ?? throw new InvalidOperationException("Isolated fixture fallback is absent.");
        var glyph = data.GetGlyph(data.GetGlyphIndex('A'), 24, 0, 1);
        Check(!data.MultichannelSignedDistanceField && glyph.Colored && glyph.MSDFPixelRange == 0, "Color fallback retains native colored pixels under an MSDF parent.");
        Console.WriteLine("Color fallback passed.");
    }
}
