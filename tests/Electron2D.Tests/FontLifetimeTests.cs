using System.Reflection;
using Electron2D;

internal static class FontLifetimeTests
{
    private static readonly FieldInfo NativeFace = typeof(FontData).GetField("_precision", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly FieldInfo EncodedBytes = typeof(FontData).GetField("_bytes", BindingFlags.Instance | BindingFlags.NonPublic)!;

    internal static void Run(byte[] bytes)
    {
        VerifyReentrantReads(bytes);
        VerifyDisposalReads(bytes);
        VerifyParallelReplacement(bytes);
        VerifyWarmReads(bytes);
        Console.WriteLine("Font reads preserve in-flight native lifetime, retry reentrant/parallel replacement, isolate nested layouts and release retired faces explicitly; warm reads=0 B.");
    }

    private static void VerifyReentrantReads(byte[] bytes)
    {
        using var child = new FontFile { Data = bytes };
        using var parent = new SpacingFont { Fallbacks = [child] };
        var expected = child.GetStringSize("A"); var old = child.PrimaryData!;
        parent.Once = () =>
        {
            child.Data = bytes;
            Check(NativeFace.GetValue(old) is not null, "A reentrant replacement retains the face while its active reader still needs it.");
        };
        Check(parent.GetStringSize("A") == expected, "An active layout retries a replaced fallback rather than reading a disposed face.");
        Released(old);
        parent.SetCacheCapacity(0, 0);
        parent.Once = () => Check(parent.GetStringSize("B") == child.GetStringSize("B"), "Nested uncached layouts use independent reusable storage.");
        Check(parent.GetStringSize("AA") == child.GetStringSize("AA"), "A nested query cannot overwrite the outer scratch layout.");
        parent.SetCacheCapacity(1, 1); parent.OpenTypeFeatureOverrides = [];
        parent.Once = () => Check(parent.GetStringSize("A") == expected, "A nested query for the same key avoids the actively building cache entry.");
        Check(parent.GetStringSize("A") == expected, "Same-key reentry leaves the outer layout intact.");
        parent.Once = () => throw new ApplicationException("expected spacing failure"); parent.OpenTypeFeatureOverrides = [];
        Reject<ApplicationException>(() => parent.GetStringSize("A"));
        Check(parent.GetStringSize("A") == expected, "A failed hook releases its read scope and does not cache unfinished layout state.");
        var attempts = 0; parent.OpenTypeFeatureOverrides = [];
        parent.Repeat = () => { attempts++; parent.InvalidateFont(); };
        var unsettled = Reject<InvalidOperationException>(() => parent.GetStringSize("A"));
        Check(attempts >= Font.MaximumReadAttempts && unsettled.Message == "Font content did not settle after sixty-four read attempts.",
            "Continuously changing callbacks terminate through the bounded retry guard rather than an unrelated error.");
        parent.Repeat = null;
        Check(parent.GetStringSize("A") == expected, "Exhausted retries release all scopes and permit the next settled read.");
    }

    private static void VerifyDisposalReads(byte[] bytes)
    {
        using var child = new SpacingFont { Data = bytes }; using var parent = new FontFile { Fallbacks = [child] };
        var expected = parent.GetStringSize("A"); var called = false;
        child.BeforeDelete = () =>
        {
            called = true;
            Check(child.GetStringSize("A") == expected && parent.GetStringSize("A") == expected,
                "The disposing thread can read font state and borrowed dependencies during pre-delete callbacks.");
        };
        child.Dispose(); Check(called, "The pre-delete read regression executed.");
        Reject<ObjectDisposedException>(() => parent.GetStringSize("A"));
    }

    private static void VerifyParallelReplacement(byte[] bytes)
    {
        using var child = new FontFile { Data = bytes };
        child.Changed += _ => throw new ApplicationException("expected earlier fallback observer");
        using var parent = new SpacingFont { Fallbacks = [child] }; parent.SetCacheCapacity(0, 0);
        using var entered = new ManualResetEventSlim(); using var resume = new ManualResetEventSlim();
        var expected = parent.GetStringSize("A");
        for (var pass = 0; pass < 8; pass++)
        {
            entered.Reset(); resume.Reset(); var previous = child.PrimaryData!;
            parent.Once = () =>
            {
                entered.Set();
                if (!resume.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("The fallback reload did not release its waiting reader.");
            };
            var query = Task.Run(() => parent.GetStringSize("A"));
            try
            {
                Check(entered.Wait(TimeSpan.FromSeconds(5)), "The reader reached its deterministic in-flight checkpoint.");
                Reject<ApplicationException>(() => child.Data = bytes);
                Check(!parent.IsDisposed && !child.IsDisposed && NativeFace.GetValue(previous) is not null,
                    "A missed Changed observer cannot retire the native face beneath a concurrent live reader.");
            }
            finally { resume.Set(); }
            Check(query.GetAwaiter().GetResult() == expected, "The concurrent reader completes with a coherent replacement snapshot.");
            Released(previous);
        }
    }

    private static void VerifyWarmReads(byte[] bytes)
    {
        using var child = new FontFile { Data = bytes }; using var parent = new FontFile { Fallbacks = [child] };
        parent.SetCacheCapacity(0, 0); var observed = 0f;
        void Cycle()
        {
            observed += parent.GetStringSize("AV ffi").X + parent.GetCharSize('A', 16).X + parent.GetHeight(16);
            Check(parent.HasChar('A'), "The prepared fallback remains covered.");
        }
        for (var pass = 0; pass < 64; pass++) Cycle();
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var pass = 0; pass < 64; pass++) Cycle();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated == 0 && observed > 0, $"Prepared native read leases and uncached layout rebuilds allocated {allocated} bytes.");
    }

    private static void Released(FontData data)
    {
        Check(NativeFace.GetValue(data) is null && ((byte[])EncodedBytes.GetValue(data)!).Length == 0,
            "The last reader releases the retired native face and encoded bytes without waiting for GC or cache eviction.");
        Reject<ObjectDisposedException>(() => data.GetMetrics(16));
    }
    private sealed class SpacingFont : FontFile
    {
        internal Action? Once;
        internal Action? Repeat;
        internal Action? BeforeDelete;
        public override int GetSpacing(TextSpacingType spacing)
        {
            if (spacing == TextSpacingType.Glyph) { Interlocked.Exchange(ref Once, null)?.Invoke(); Repeat?.Invoke(); }
            return base.GetSpacing(spacing);
        }
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what == NotificationPreDelete) BeforeDelete?.Invoke();
        }
    }
    private static T Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T error) { return error; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
