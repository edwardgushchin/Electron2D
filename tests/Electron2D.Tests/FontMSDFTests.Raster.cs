using System.Diagnostics;
using System.Globalization;
using Electron2D;

internal static partial class FontMSDFTests
{
    internal static void RunRaster()
    {
        var oracle = Environment.GetEnvironmentVariable("ELECTRON2D_FONT_MSDF_ORACLE") ?? throw new InvalidOperationException("Specify the independent native MSDF oracle.");
        var path = System.IO.Path.GetTempFileName(); var count = 0;
        try
        {
            foreach (var bytes in new[] { FontTestFixtures.OpenSans, FontTestFixtures.Arabic, FontTestFixtures.MSDF })
            {
                File.WriteAllBytes(path, bytes);
                foreach (var size in new[] { 16, 48 })
                    foreach (var range in new[] { 4, 16 })
                        foreach (var hinting in new[] { FontHinting.None, FontHinting.Light, FontHinting.Normal })
                            foreach (var style in new[] { (0f, 0f), (1f, .25f) })
                                foreach (var character in new[] { 'A', 'O', 'g' })
                                {
                                    using var face = new NativeFontPrecision(bytes);
                                    face.ConfigureInstance(new FontInstance(0, [], style.Item1, new Transform(new(1, 0), new(style.Item2, 1), Vector2.Zero), 0, 0, []));
                                    face.SetSize(size); var raster = face.RasterizeMSDF(face.GetGlyphIndex(character), hinting, range, false);
                                    var start = new ProcessStartInfo(oracle) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
                                    foreach (var arg in new[] { path, size.ToString(), range.ToString(), ((int)hinting).ToString(), ((int)character).ToString(), style.Item1.ToString(CultureInfo.InvariantCulture), style.Item2.ToString(CultureInfo.InvariantCulture) }) start.ArgumentList.Add(arg);
                                    start.Environment["LD_PRELOAD"] = System.IO.Path.Combine(AppContext.BaseDirectory, "runtimes", "linux-x64", "native", "libElectron2DFreeType.so");
                                    using var process = Process.Start(start)!; var output = process.StandardOutput.ReadToEnd(); var error = process.StandardError.ReadToEnd(); process.WaitForExit();
                                    Check(process.ExitCode == 0 && string.IsNullOrWhiteSpace(error), "MSDF oracle executes: " + error);
                                    var lines = output.Trim().Split('\n'); var header = lines[0].Split(' ');
                                    Check(header[0] == "2.14.3" && raster.Width == int.Parse(header[1]) && raster.Height == int.Parse(header[2]) && Math.Abs(raster.Left - double.Parse(header[3], CultureInfo.InvariantCulture)) < 0.0001 && Math.Abs(raster.Top - double.Parse(header[4], CultureInfo.InvariantCulture)) < 0.0001 && raster.Pixels.AsSpan().SequenceEqual(Convert.FromHexString(lines[1])), $"Exact source MSDF size={size}, range={range}, hint={hinting}, style={style}, char={character}."); count++;
                                }
            }
        }
        finally { File.Delete(path); }
        Check(count == 216, "Complete outline/range/size/hinting/style matrix."); Console.WriteLine("216 independent FreeType/MSDF glyph image and bound cases passed.");
    }
}
