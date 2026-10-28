using Electron2D;

internal static class GZIPCleanupTests
{
    internal static void Run(bool nativeProbe = false)
    {
        if (nativeProbe) NativeAllocationProbe.Prepare();
        foreach (var compress in new[] { true, false })
        {
            void Fail()
            {
                try { using var context = new GZIPContext(compress, 100); }
                catch (InvalidDataException error) when (error.Message == "Native compression initialization failed: -2") { return; }
                throw new InvalidOperationException("Invalid native window bits must preserve the initialization error.");
            }
            for (var i = 0; i < 128; i++) Fail();
            var before = NativeAllocation.Snapshot();
            if (nativeProbe) NativeAllocationProbe.Begin();
            NativeAllocationProbe.Counts counts = default;
            try { for (var i = 0; i < 128; i++) Fail(); }
            finally { if (nativeProbe) counts = NativeAllocationProbe.End(); }
            Check(NativeAllocation.Snapshot().Current == before.Current, "Failed initialization retires the engine-owned PAL record.");
            if (nativeProbe)
            {
                Console.WriteLine($"GZIP failed initialization compress={compress}: native allocations={counts.Allocations}, frees={counts.Frees}, reallocations={counts.Reallocations}, failures={counts.Failures}.");
                Check(counts.Allocations >= 256 && counts.Frees == counts.Allocations && counts.Reallocations == 0 && counts.Failures == 0,
                    "Every failed initialization must free both native PAL layers, including the runtime-owned inner record.");
            }
            foreach (var window in new[] { 15, 31 })
            {
                using (var context = new GZIPContext(compress, window))
                {
                    Check(NativeAllocation.Snapshot().Current > before.Current, "Valid initialization still owns its PAL record.");
                    context.Dispose(); context.Dispose();
                }
                Check(NativeAllocation.Snapshot().Current == before.Current, "Normal repeated disposal frees the PAL record once.");
            }
        }
        Console.WriteLine("GZIP failed initialization, preserved native error, normal initialization and repeated disposal passed.");
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
