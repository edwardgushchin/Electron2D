using System.Runtime.CompilerServices;

internal static class AllocationWarmupCheck
{
    internal static void Collect()
    {
        // Finish background collection before its context retirement can change the allocation counters.
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers();
    }

    internal static void Verify(Action collect)
    {
        using var entered = new ManualResetEventSlim(); using var release = new ManualResetEventSlim();
        using var completed = new ManualResetEventSlim();
        var state = new ProbeState(entered, release); CreateProbe(state);
        Exception? failure = null;
        var collector = new Thread(() => { try { collect(); } catch (Exception error) { failure = error; } finally { completed.Set(); } });
        collector.Start();
        var returnedEarly = false;
        try
        {
            if (!entered.Wait(5000)) throw new InvalidOperationException("The warmup probe did not reach its second finalization pass.");
            returnedEarly = completed.Wait(100);
        }
        finally
        {
            release.Set();
            if (!collector.Join(5000)) throw new InvalidOperationException("The warmup collector did not finish after releasing the finalizer.");
            GC.WaitForPendingFinalizers();
        }
        if (failure is not null) throw new InvalidOperationException("The warmup collector failed.", failure);
        if (returnedEarly || state.Completed != 2) throw new InvalidOperationException("Allocation warmup returned before finalizers from its final collection completed.");
        Console.WriteLine("Allocation warmup drains the controlled second finalization pass before measurement.");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void CreateProbe(ProbeState state) => _ = new Probe(state);
    private sealed class ProbeState(ManualResetEventSlim entered, ManualResetEventSlim release)
    {
        internal int Completed;
        internal readonly ManualResetEventSlim Entered = entered, Release = release;
    }
    private sealed class Probe(ProbeState state)
    {
        private int _passes;
        ~Probe()
        {
            if (++_passes == 1) GC.ReRegisterForFinalize(this);
            else { state.Entered.Set(); state.Release.Wait(); }
            Interlocked.Increment(ref state.Completed);
        }
    }
}
