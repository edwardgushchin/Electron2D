using System.Diagnostics;
using Electron2D;

internal static partial class FontAntialiasingTests
{
    private static void Storage(FontFile font, SystemFont system)
    {
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-font-aa-" + Guid.NewGuid()); Directory.CreateDirectory(directory);
        try
        {
            var path = System.IO.Path.Combine(directory, "font.e2dres"); ResourceSaver.Save(font, path);
            using var restored = ResourceLoader.Load<FontFile>(path, ResourceLoader.CacheMode.Ignore);
            Check(restored.Antialiasing == FontAntialiasing.LCD && Glyph(restored).LCD, "Font archive restores executable glyph policy.");
            var systemPath = System.IO.Path.Combine(directory, "system.e2dres"); ResourceSaver.Save(system, systemPath);
            using var loadedSystem = ResourceLoader.Load<SystemFont>(systemPath, ResourceLoader.CacheMode.Ignore);
            Check(loadedSystem.Antialiasing == FontAntialiasing.LCD && Glyph(loadedSystem).LCD, "System archive stores preferences and realizes fresh sources.");
            var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
            start.ArgumentList.Add(typeof(FontAntialiasingTests).Assembly.Location); start.ArgumentList.Add("--font-aa-archive"); start.ArgumentList.Add(path);
            using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(30000)) { process.Kill(true); throw new TimeoutException("Font archive child timed out."); }
            Check(process.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh AA archive passed."), "Fresh AA resource load: " + error.GetAwaiter().GetResult());
            using var reset = (FontFile)font.Duplicate(); reset.LoadBitmapFont(FontCacheTests.Fixture(directory)); Check(reset.Antialiasing == FontAntialiasing.Gray, "Bitmap import restores gray defaults.");
            var source = System.IO.Path.Combine(directory, "source.woff2"); File.WriteAllBytes(source, FontTestFixtures.OpenSans);
            reset.Antialiasing = FontAntialiasing.None; reset.LoadDynamicFont(source); Check(reset.Antialiasing == FontAntialiasing.Gray, "Dynamic import restores gray defaults.");
        }
        finally { Directory.Delete(directory, true); }
    }
    internal static void RunArchive(string path)
    {
        using var font = ResourceLoader.Load<FontFile>(path, ResourceLoader.CacheMode.Ignore);
        Check(font.Antialiasing == FontAntialiasing.LCD && Glyph(font).LCD, "Fresh process reconstructs coverage without native identities.");
        Console.WriteLine("Fresh AA archive passed.");
    }
}
