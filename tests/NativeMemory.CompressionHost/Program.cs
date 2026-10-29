using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Electron2D;

internal static unsafe class Program
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Counts
    {
        public ulong ManagedCurrent, ManagedPeak, NativeCurrent, NativePeak, Current, Peak,
            Allocated, Retired, Allocations, Resizes, Frees, Failures;
    }
    private static delegate* unmanaged[Cdecl]<Counts*, void> _snapshot;
    private static Counts Read() { Counts value; _snapshot(&value); Check(value.Allocated - value.Retired == value.NativeCurrent, "Native conservation failed."); return value; }
    private static void Main(string[] args)
    {
        if (args.Length != 1) throw new ArgumentException("Expected deflate, gzip, zlib, brotli or webp.");
        var library = NativeLibrary.Load(System.IO.Path.Combine(AppContext.BaseDirectory, "runtimes", "linux-x64", "native", "libElectron2DENet.so"));
        _snapshot = (delegate* unmanaged[Cdecl]<Counts*, void>)NativeLibrary.GetExport(library, "e2d_memory_snapshot");
        var mode = args[0]; var source = new byte[65536];
        for (var i = 0; i < source.Length; i++) source[i] = (byte)(i * 113 + i / 7);
        var baseline = Read();
        if (mode == "webp") { WebP(source, baseline); Finish(mode, baseline); return; }
        using var encoded = new MemoryStream();
        using (var stream = Codec(encoded, mode, true))
        {
            stream.Write(source); stream.Flush();
            Check(Read().NativeCurrent > baseline.NativeCurrent + 4096, "BCL compression storage bypassed the native ledger.");
        }
        Check(Read().NativeCurrent == baseline.NativeCurrent, "Compression storage did not drain.");
        encoded.Position = 0;
        using (var stream = Codec(encoded, mode, false))
        {
            using var decoded = new MemoryStream(); stream.CopyTo(decoded);
            Check(decoded.ToArray().AsSpan().SequenceEqual(source), "BCL round trip changed bytes.");
        }
        Check(Read().NativeCurrent == baseline.NativeCurrent, "Decompression storage did not drain.");
        var codec = mode switch { "gzip" => FileCompressionMode.Gzip, "brotli" => FileCompressionMode.Brotli, _ => FileCompressionMode.Deflate };
        var stored = ByteBuffer.Compress(source, codec);
        Check(ByteBuffer.Decompress(stored, source.Length, codec).AsSpan().SequenceEqual(source), "Public file codec changed bytes.");
        Check(Read().NativeCurrent == baseline.NativeCurrent && Read().NativePeak > baseline.NativePeak, "Public file codec leaked or lost its transient peak.");
        var abandoned = Abandon(mode, source);
        for (var i = 0; i < 10 && (abandoned.IsAlive || Read().NativeCurrent != baseline.NativeCurrent); i++) { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); }
        Check(!abandoned.IsAlive && Read().NativeCurrent == baseline.NativeCurrent, "Finalized codec storage did not drain.");
        if (mode == "deflate") ZIP(source, baseline.NativeCurrent);
        Finish(mode, baseline);
    }
    private static void Finish(string mode, Counts baseline)
    {
        for (var i = 0; i < 1024; i++) _ = Read();
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 20000; i++) Check(Read().NativeCurrent == baseline.NativeCurrent, "Prepared read changed native storage.");
        var allBytes = GC.GetTotalAllocatedBytes(true) - all; var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner;
        Check(ownerBytes == 0 && allBytes == 0, "Prepared native reads allocated managed storage.");
        var paths = System.IO.File.ReadLines("/proc/self/maps").Select(line => line[(line.LastIndexOf(' ') + 1)..]).Distinct().ToArray();
        var codecLibrary = mode == "webp" ? "libElectron2DWebP.so" : "libSystem.IO.Compression.Native.so";
        Check(paths.Count(path => path.EndsWith(codecLibrary, StringComparison.Ordinal)) == 1 && paths.Count(path => path.EndsWith("libElectron2DENet.so", StringComparison.Ordinal)) == 1, "Expected one codec and one common ledger.");
        Check(paths.Any(path => path.EndsWith("libcoreclr.so", StringComparison.Ordinal)) == RuntimeFeature.IsDynamicCodeSupported, "Runtime identity mismatch.");
        var exercised = mode == "webp" ? "public WebP 14 encoding/decoding profiles" : "streaming/public file/finalization; ZIP=" + (mode == "deflate" ? "passed" : "separate deflate role");
        Console.WriteLine($"Codec {mode}: nativePeak={Read().NativePeak}, currentDelta={Read().NativeCurrent - baseline.NativeCurrent}; {exercised} passed; prepared20000={ownerBytes}/{allBytes}; dynamicCode={RuntimeFeature.IsDynamicCodeSupported}.");
        foreach (var path in paths.Where(path => path.EndsWith(codecLibrary, StringComparison.Ordinal) || path.EndsWith("libElectron2DENet.so", StringComparison.Ordinal))) Console.WriteLine(path);
    }
    private static void WebP(byte[] source, Counts baseline)
    {
        using var image = Image.CreateFromData(128, 128, false, Image.Format.Rgba8, source);
        using var restored = new Image();
        for (var method = 0; method <= 6; method++)
        {
            ProjectSettings.Set(ProjectSettings.WebPCompressionMethod, method);
            foreach (var lossy in new[] { false, true })
            {
                var encoded = image.SaveWebPToBuffer(lossy); restored.LoadWebPFromBuffer(encoded);
                Check(restored.Width == 128 && restored.Height == 128 && (lossy || restored.GetData().AsSpan().SequenceEqual(source)), "Public WebP pixels changed.");
                Check(Read().NativeCurrent == baseline.NativeCurrent && Read().NativePeak > baseline.NativePeak, "Public WebP lost its transient peak or leaked native storage.");
            }
        }
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference Abandon(string mode, byte[] source)
    {
        var stream = Codec(new MemoryStream(), mode, true); stream.Write(source); stream.Flush();
        return new WeakReference(stream);
    }
    private static void ZIP(byte[] source, ulong baseline)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-compression-" + Guid.NewGuid().ToString("N") + ".zip");
        try
        {
            using (var packer = new ZIPPacker())
            {
                packer.Open(path); packer.StartFile("data.bin"); packer.WriteFile(source);
                Check(Read().NativeCurrent > baseline + 4096, "ZIP compression bypassed the native ledger.");
                packer.CloseFile();
            }
            using (var reader = new ZIPReader()) { reader.Open(path); Check(reader.ReadFile("data.bin").AsSpan().SequenceEqual(source), "Public ZIP changed bytes."); }
            Check(Read().NativeCurrent == baseline, "Public ZIP leaked native storage.");
        }
        finally { System.IO.File.Delete(path); }
    }
    private static Stream Codec(Stream stream, string mode, bool compress)
    {
        var direction = compress ? CompressionMode.Compress : CompressionMode.Decompress;
        return mode switch
        {
            "deflate" => new DeflateStream(stream, direction, true),
            "gzip" => new GZipStream(stream, direction, true),
            "zlib" => new ZLibStream(stream, direction, true),
            "brotli" => new BrotliStream(stream, direction, true),
            _ => throw new ArgumentException("Invalid mode.")
        };
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
