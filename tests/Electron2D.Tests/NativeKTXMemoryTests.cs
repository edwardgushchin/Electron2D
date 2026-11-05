using System.Runtime.InteropServices;
using System.Text.Json;
using Electron2D;

internal static unsafe class NativeKTXMemoryTests
{
    internal static void Run()
    {
        if (!OperatingSystem.IsLinux()) throw new PlatformNotSupportedException("The KTX allocation-refusal fixture requires Linux.");
        var file = System.IO.Path.Combine(AppContext.BaseDirectory, "runtimes", NativeLibraries.RuntimeRID, "native", "libElectron2D.so");
        var library = NativeLibrary.Load(file);
        try
        {
            var arm = (delegate* unmanaged[Cdecl]<nuint, void>)NativeLibrary.GetExport(library, "e2d_test_arm");
            var attempts = (delegate* unmanaged[Cdecl]<nuint>)NativeLibrary.GetExport(library, "e2d_test_attempts");
            Run(arm, attempts);
        }
        finally { NativeLibrary.Free(library); }
    }

    private static void Run(delegate* unmanaged[Cdecl]<nuint, void> arm, delegate* unmanaged[Cdecl]<nuint> attempts)
    {
        using var stream = typeof(NativeKTXMemoryTests).Assembly.GetManifestResourceStream("TestKTX.oracle.json")!;
        using var oracle = JsonDocument.Parse(stream);
        nuint refusals = 0;
        foreach (var entry in oracle.RootElement.GetProperty("fixtures").EnumerateArray())
        {
            var name = entry.GetProperty("name").GetString()!;
            var source = entry.GetProperty("encoded").GetBytesFromBase64();
            var pristine = source.ToArray();
            var expected = entry.GetProperty("pixels").GetBytesFromBase64();
            using (var warm = new Image()) warm.LoadKTXFromBuffer(source);
            var baseline = NativeAllocation.Snapshot().Current;
            nuint requests;
            using (var ready = new Image())
            {
                arm(0); ready.LoadKTXFromBuffer(source); requests = attempts();
                Check(ready.GetData().AsSpan().SequenceEqual(expected), "Independent pixels before refusal: " + name);
                Check(NativeAllocation.Snapshot().Current == baseline, "Native decoder storage retires before Image publication: " + name);
            }
            using var previous = Image.CreateFromData(2, 2, false, Image.Format.Rgba8, new byte[16]);
            var old = previous.GetData(); var changes = 0;
            previous.Changed += _ => changes++;
            for (nuint failure = 1; failure <= requests; failure++)
            {
                arm(failure); var rejected = false;
                try { previous.LoadKTXFromBuffer(source); }
                catch (IOException) { rejected = true; }
                finally { arm(0); }
                Check(rejected, "Native allocation refusal projects IOException: " + name);
                Check(previous.Width == 2 && previous.Height == 2 && previous.MIPMapCount == 0 && previous.PixelFormat == Image.Format.Rgba8 &&
                    previous.GetData().AsSpan().SequenceEqual(old) && changes == 0 && source.AsSpan().SequenceEqual(pristine),
                    "Refusal preserves the public Image, source and Changed boundary: " + name);
                Check(NativeAllocation.Snapshot().Current == baseline, "Refusal retires every temporary native allocation: " + name);
                using var retry = new Image(); retry.LoadKTXFromBuffer(source);
                Check(retry.GetData().AsSpan().SequenceEqual(expected) && NativeAllocation.Snapshot().Current == baseline,
                    "A healthy retry retains complete independent mip pixels: " + name);
                refusals++;
            }
            previous.LoadKTXFromBuffer(source);
            Check(changes == 1 && previous.GetData().AsSpan().SequenceEqual(expected), "A healthy load publishes once after refusal: " + name);
        }
        Console.WriteLine($"KTX public Image: {refusals} native refusals preserve pixels/metadata/source/Changed, drain storage and retry all mip oracles.");
    }

    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
