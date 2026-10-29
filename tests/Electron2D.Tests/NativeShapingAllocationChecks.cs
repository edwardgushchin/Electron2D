using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Globalization;
using System.Text.Json;
using Electron2D;

internal static unsafe class NativeShapingAllocationChecks
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Functions { public nint Allocate, AllocateZeroed, Resize, Release; }
    internal static void Run(Func<(long Current, long Peak)> read, byte[] data, bool fault, bool oracle = false)
    {
        var path = System.IO.Path.Combine(AppContext.BaseDirectory, "runtimes", "linux-x64", "native", "libHarfBuzzSharp.so");
        var library = NativeLibrary.Load(path);
        if (!oracle)
        {
            var core = NativeLibrary.Load(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path)!, "libElectron2DENet.so"));
            var selected = (delegate* unmanaged[Cdecl]<Functions*, void>)NativeLibrary.GetExport(library, "e2d_shaping_memory");
            var expected = (delegate* unmanaged[Cdecl]<Functions*, void>)NativeLibrary.GetExport(core, "e2d_memory_functions");
            Functions actual, normal; selected(&actual); expected(&normal);
            Check(actual.Allocate != 0 && actual.Allocate == normal.Allocate &&
                actual.AllocateZeroed == normal.AllocateZeroed && actual.Resize == normal.Resize && actual.Release == normal.Release,
                "The selected shaping library does not share the actual ledger callbacks.");
        }
        using var font = new FontFile { Data = data };
        var texts = new[] { "Affine office e\u0301", "بب السلام", "नमस्ते क्ष" };
        var sizes = texts.Select(text => font.GetStringSize(text, fontSize: 24)).ToArray();
        Console.WriteLine($"{{\"FontName\":\"{JsonEncodedText.Encode(font.FontName)}\",\"StyleName\":\"{JsonEncodedText.Encode(font.StyleName)}\",\"Sizes\":[{string.Join(",", sizes.Select(value => $"[{value.X.ToString("R", CultureInfo.InvariantCulture)},{value.Y.ToString("R", CultureInfo.InvariantCulture)}]"))}]}}");
        if (fault)
        {
            var arm = (delegate* unmanaged[Cdecl]<nuint, void>)NativeLibrary.GetExport(library, "e2d_shaping_test_arm");
            var attempts = (delegate* unmanaged[Cdecl]<nuint>)NativeLibrary.GetExport(library, "e2d_shaping_test_attempts");
            var failures = (delegate* unmanaged[Cdecl]<nuint>)NativeLibrary.GetExport(library, "e2d_shaping_test_failures");
            arm(0); using (var calibration = new FontFile { Data = data }) Check(calibration.FontName == font.FontName, "Calibration changed font metadata.");
            var maximum = attempts(); Check(maximum > 0, "No shaping allocations were calibrated.");
            var changed = 0; font.Changed += _ => changed++;
            for (nuint position = 1; position <= maximum; position++)
            {
                changed = 0; var before = read(); var refused = false; nuint observed = 0;
                arm(position);
                try { font.Data = data; }
                catch (OutOfMemoryException) { refused = true; }
                finally { observed = failures(); arm(0); }
                Check(refused && observed == 1 && changed == 0, "Failed native replacement was published or its error was lost.");
                Check(read().Current == before.Current && font.Data.AsSpan().SequenceEqual(data), "Failed font replacement leaked or changed stored bytes.");
                for (var i = 0; i < texts.Length; i++) Check(font.GetStringSize(texts[i], fontSize: 24) == sizes[i], "Failed replacement poisoned prior shaping.");
                font.Data = data; Check(changed == 1, "Healthy font replacement did not publish once.");
                for (var i = 0; i < texts.Length; i++) Check(font.GetStringSize(texts[i], fontSize: 24) == sizes[i], "Healthy replacement changed shaping after a refusal.");
            }
            Console.WriteLine($"Public shaping: {maximum} refused constructors, old bytes/metrics/events retained and healthy replacement passed.");
            var longText = string.Concat(Enumerable.Repeat("Affine office e\u0301 بب السلام नमस्ते क्ष ", 32));
            var expected = font.GetStringSize(longText, fontSize: 24);
            using (var calibration = new FontFile { Data = data })
            {
                arm(0); Check(calibration.GetStringSize(longText, fontSize: 24) == expected, "Shaping calibration changed metrics.");
                maximum = attempts(); arm(0);
            }
            Check(maximum > 0, "No actual shaping allocations were calibrated.");
            for (nuint position = 1; position <= maximum; position++)
            {
                using var candidate = new FontFile { Data = data };
                var refused = false; nuint observed = 0;
                arm(position);
                try { Check(candidate.GetStringSize(longText, fontSize: 24) == expected, "Native refusal published incorrect layout metrics."); }
                catch (OutOfMemoryException) { refused = true; }
                finally { observed = failures(); arm(0); }
                Check(refused && observed == 1, "Native shaping refusal was not propagated.");
                var retry = candidate.GetStringSize(longText, fontSize: 24);
                Check(retry == expected, $"Refused shaping {position}/{maximum} poisoned the same font's retry: {retry}, expected {expected}.");
            }
            Console.WriteLine($"Public shaping: {maximum} refused actual layouts, same-font exact retry passed.");
        }
        for (var i = 0; i < 256; i++) _ = font.GetStringSize(texts[0], fontSize: 24);
        AllocationWarmupCheck.Collect();
        for (var i = 0; i < 256; i++) _ = font.GetStringSize(texts[0], fontSize: 24);
        var baseline = read(); var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 20000; i++) { _ = font.GetStringSize(texts[0], fontSize: 24); _ = read(); }
        var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - own; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
        var after = read();
        Check(ownerBytes == 0 && allBytes == 0 && after.Current == baseline.Current,
            $"Prepared public layout/cache reads allocated {ownerBytes}/{allBytes} managed bytes or changed native ownership {baseline.Current}->{after.Current}.");
        var paths = System.IO.File.ReadLines("/proc/self/maps").Select(line => line[(line.LastIndexOf(' ') + 1)..]).Distinct().ToArray();
        Check(paths.Count(value => value.EndsWith("libHarfBuzzSharp.so", StringComparison.Ordinal)) == 1 &&
            paths.Count(value => value.EndsWith("libElectron2DENet.so", StringComparison.Ordinal)) == 1, "Multiple shaping libraries or ledgers were loaded.");
        Check(paths.Any(value => value.EndsWith("libcoreclr.so", StringComparison.Ordinal)) == RuntimeFeature.IsDynamicCodeSupported,
            "The exercised runtime does not match its dynamic-code identity.");
        Console.WriteLine(RuntimeFeature.IsDynamicCodeSupported ? "Runtime: CLR" : "Runtime: NativeAOT, no CoreCLR");
        Console.WriteLine($"Public shaping cache/read: 20000, managed={ownerBytes}/{allBytes}, nativePeak={read().Peak}; exact selected library and ledger.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
