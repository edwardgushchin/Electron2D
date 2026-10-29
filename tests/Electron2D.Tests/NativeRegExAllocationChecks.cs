using System.Runtime.CompilerServices;
using Electron2D;

internal static class NativeRegExAllocationChecks
{
    internal static void Run(Func<(long Current, long Peak)> read, bool fresh = false)
    {
        AllocationWarmupCheck.Collect();
        var baseline = read();
        using var expression = new RegEx("(?<item>a+)");
        var compiled = read();
        Check(compiled.Current > baseline.Current, "Compiled PCRE2 storage bypassed the common native ledger.");
        var contribution = compiled.Current - baseline.Current;
        void Exercise()
        {
            using var match = expression.Search("!aaa!");
            Check(match?.GetString("item") == "aaa", "Accounted search changed captures.");
            Check(expression.Search("!") is null, "No-match result changed.");
            Check(expression.Sub("aaa aaa", "${item}$$", all: true) == "aaa$ aaa$", "Accounted substitution changed output.");
            Check(read().Current == compiled.Current, "Temporary PCRE2 search/substitution storage leaked.");
        }
        Exercise(); var retiredPeak = read().Peak;
        if (fresh) Check(retiredPeak > compiled.Peak, "Unobserved temporary matching storage did not enter the native high-water mark.");
        for (var i = 0; i < 64; i++) Exercise();
        void Values()
        {
            Check(expression.IsValid && expression.GroupCount == 1 && expression.Pattern == "(?<item>a+)", "Prepared metadata changed.");
            _ = read();
        }
        for (var i = 0; i < 256; i++) Values();
        AllocationWarmupCheck.Collect();
        for (var i = 0; i < 256; i++) Values();
        var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 20000; i++) Values();
        var ownBytes = GC.GetAllocatedBytesForCurrentThread() - own; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
        Check(ownBytes == 0 && allBytes == 0, $"Prepared metadata/read cycles allocated {ownBytes}/{allBytes} managed bytes.");
        using (var retained = expression.Search("aaa")!)
        {
            expression.Clear();
            Check(read().Current == baseline.Current && retained.GetString() == "aaa", "Clear did not retire the native program or changed retained captures.");
        }
        expression.Compile("a+");
        try { expression.Compile("("); throw new InvalidOperationException("Invalid pattern compiled."); }
        catch (ArgumentException) { }
        Check(!expression.IsValid && read().Current == baseline.Current, "Failed recompilation retained a native program.");
        var forgotten = Forget();
        Check(read().Current > baseline.Current, "Forgotten owner did not hold tracked native storage.");
        AllocationWarmupCheck.Collect();
        Check(!forgotten.IsAlive && read().Current == baseline.Current, "SafeHandle finalization failed to retire native program storage.");
        Parallel.For(0, 4, _ =>
        {
            for (var i = 0; i < 32; i++)
            {
                using var independent = new RegEx("a+");
                using var result = independent.Search("aaa");
                Check(result?.GetString() == "aaa" && independent.Sub("aaa", "b") == "b", "Concurrent matching failed.");
            }
        });
        Check(read().Current == baseline.Current && read().Peak >= retiredPeak, "Concurrent owners leaked native storage or lost the peak.");
        Console.WriteLine($"PCRE2 allocation: compiled={contribution}, retainedPeak={retiredPeak}, currentDelta={read().Current - baseline.Current}, prepared=20000, managed={ownBytes}/{allBytes}; search/substitution/clear/failure/finalizer/4 workers passed.");
    }
    internal static void Faults(Func<(long Current, long Peak)> read, Action<uint> fail)
    {
        using var expression = new RegEx("a+");
        expression.Clear();
        var baseline = read().Current;
        void Reject(uint position, Action action)
        {
            fail(position);
            try
            {
                action();
                throw new InvalidOperationException($"Native allocation {position} unexpectedly succeeded.");
            }
            catch (OutOfMemoryException) { }
            finally { fail(0); }
        }
        for (uint position = 1; position <= 3; position++)
        {
            expression.Compile("a+");
            Reject(position, () => expression.Compile("a+"));
            Check(!expression.IsValid && read().Current == baseline, "Failed compilation retained native storage.");
        }
        expression.Compile("a+");
        var compiled = read().Current;
        for (uint position = 1; position <= 4; position++)
        {
            Reject(position, () => { using var match = expression.Search("aaa"); });
            Check(read().Current == compiled, "Failed search changed compiled ownership.");
            using var retry = expression.Search("aaa");
            Check(retry?.GetString() == "aaa", "Search retry failed.");
            Reject(position, () => { _ = expression.Sub("aaa", "b"); });
            Check(read().Current == compiled, "Failed substitution changed compiled ownership.");
            Check(expression.Sub("aaa", "b") == "b", "Substitution retry failed.");
        }
        expression.Clear();
        Check(read().Current == baseline, "Faulted native owners did not drain.");
        Console.WriteLine("PCRE2 public faults: 11 allocation positions raised OutOfMemoryException; retries and native ownership passed.");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference Forget() => new(new RegEx("(?<word>[a-z]+)"));
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
