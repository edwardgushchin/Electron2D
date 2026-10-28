using System.Diagnostics;
using System.Globalization;
using Electron2D;

internal static partial class Type1FontTests
{
    internal static void RunRaster()
    {
        var aaOracle = Environment.GetEnvironmentVariable("ELECTRON2D_FONT_AA_ORACLE") ?? throw new InvalidOperationException("Specify the FreeType oracle.");
        var msdfOracle = Environment.GetEnvironmentVariable("ELECTRON2D_FONT_MSDF_ORACLE") ?? throw new InvalidOperationException("Specify the MSDF oracle.");
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-type1-oracle-" + Guid.NewGuid() + ".pfb"); var ordinary = 0; var distance = 0;
        try
        {
            foreach (var extension in new[] { "pfb", "pfa" })
            {
                var bytes = Fixture(extension); System.IO.File.WriteAllBytes(path, bytes); using var face = new NativeFontPrecision(bytes, auxiliaryMetrics: Fixture("pfm"));
                foreach (var hint in new[] { FontHinting.None, FontHinting.Light, FontHinting.Normal })
                {
                    face.SetSize(24);
                    foreach (var mode in new[] { FontAntialiasing.None, FontAntialiasing.Gray, FontAntialiasing.LCD })
                        foreach (var layout in mode == FontAntialiasing.LCD ? new[] { FontLCDSubpixelLayout.None, FontLCDSubpixelLayout.HorizontalRGB, FontLCDSubpixelLayout.HorizontalBGR, FontLCDSubpixelLayout.VerticalRGB, FontLCDSubpixelLayout.VerticalBGR } : new[] { FontLCDSubpixelLayout.None })
                            foreach (var phase in new[] { 0, 16 }) foreach (var stroke in new[] { 0, 64 })
                                {
                                    var actual = face.Rasterize(face.GetGlyphIndex('A'), hint, phase, stroke, antialiasing: mode, layout: layout);
                                    var result = Oracle(aaOracle, [path, "24", ((int)hint).ToString(), phase.ToString(), stroke.ToString(), ((int)mode).ToString(), ((int)layout).ToString()]);
                                    Match(actual, result, false); ordinary++;
                                }
                    foreach (var size in new[] { 16, 48 }) foreach (var range in new[] { 4, 16 })
                        {
                            face.SetSize(size); var actual = face.RasterizeMSDF(face.GetGlyphIndex('A'), hint, range, false);
                            var result = Oracle(msdfOracle, [path, size.ToString(), range.ToString(), ((int)hint).ToString(), "65", "0", "0"]); Match(actual, result, true); distance++;
                        }
                }
            }
        }
        finally { System.IO.File.Delete(path); }
        Check(ordinary == 168 && distance == 24, "Complete Type 1 native image matrix."); Console.WriteLine("168 Type 1 raster and 24 MSDF independent packaged-native images/bounds passed.");
    }
    private static string[] Oracle(string executable, string[] arguments)
    {
        var start = new ProcessStartInfo(executable) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (var arg in arguments) start.ArgumentList.Add(arg);
        start.Environment["LD_PRELOAD"] = System.IO.Path.Combine(AppContext.BaseDirectory, "runtimes", "linux-x64", "native", "libElectron2DFreeType.so");
        using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(10000)) { process.Kill(true); throw new TimeoutException("Type 1 native oracle."); }
        Check(process.ExitCode == 0 && string.IsNullOrWhiteSpace(error.GetAwaiter().GetResult()), "Native oracle: " + error.GetAwaiter().GetResult()); return output.GetAwaiter().GetResult().Trim().Split('\n');
    }
    private static void Match(NativeRasterGlyph actual, string[] result, bool msdf)
    {
        var header = result[0].Split(' '); Check(header[0] == "2.14.3" && actual.Width == int.Parse(header[1]) && actual.Height == int.Parse(header[2]) && Math.Abs(actual.Left - double.Parse(header[3], CultureInfo.InvariantCulture)) < .0001 && Math.Abs(actual.Top - double.Parse(header[4], CultureInfo.InvariantCulture)) < .0001 && (msdf || actual.LCD == (header[5] == "1")) && actual.Pixels.AsSpan().SequenceEqual(Convert.FromHexString(result[1])), "Exact independent Type 1 glyph pixels/bounds.");
    }
}
