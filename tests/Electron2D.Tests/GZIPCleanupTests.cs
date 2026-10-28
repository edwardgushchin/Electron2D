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
            Check(NativeAllocation.Snapshot().Current == before.Current, "Failed initialization retires the complete codec storage.");
            if (nativeProbe)
            {
                Console.WriteLine($"GZIP failed initialization compress={compress}: native allocations={counts.Allocations}, frees={counts.Frees}, reallocations={counts.Reallocations}, failures={counts.Failures}.");
                Check(counts.Allocations >= 128 && counts.Frees == counts.Allocations && counts.Reallocations == 0 && counts.Failures == 0,
                    "Every failed initialization must free the context and any partial codec storage.");
            }
            foreach (var window in new[] { 15, 31 })
            {
                using (var context = new GZIPContext(compress, window))
                {
                    var prepared = NativeAllocation.Snapshot();
                    Check(prepared.Current - before.Current >= 4096 && prepared.Peak >= prepared.Current, "Prepared native codec payload participates in coherent current/peak accounting.");
                    context.Dispose(); context.Dispose();
                }
                Check(NativeAllocation.Snapshot().Current == before.Current, "Normal repeated disposal frees the complete codec once.");
            }
        }
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        var baseline = NativeAllocation.Snapshot(); var forgotten = Forget(baseline.Current);
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        Check(!forgotten.IsAlive && NativeAllocation.Snapshot().Current == baseline.Current, "SafeHandle finalization retires unowned codec storage.");
        using (var stream = new StreamPeerGZIP())
        {
            var current = NativeAllocation.Snapshot().Current; stream.StartCompression();
            var prepared = NativeAllocation.Snapshot(); Check(prepared.Current - current >= 4096, "The public stream tracks prepared codec storage.");
            stream.Clear(); stream.Clear();
            var cleared = NativeAllocation.Snapshot(); Check(cleared.Current == current && cleared.Peak >= prepared.Current, "Clear retires codec storage once and preserves its transient peak.");
        }
        Console.WriteLine("GZIP native codec accounting, failed initialization, preserved error, repeated disposal and finalization passed.");
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static WeakReference Forget(long baseline)
    {
        var context = new GZIPContext(true, 31);
        Check(NativeAllocation.Snapshot().Current - baseline >= 4096, "An undisposed live handle retains tracked native codec storage.");
        var reference = new WeakReference(context); GC.KeepAlive(context); return reference;
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
