using Electron2D;
using static PhysicsDebugTests;

internal static class RenderHandleReplacementTests
{
    internal static void Run()
    {
        var released = new List<nint>(); var ownerReleased = 0;
        using var owner = new RenderHandle(1, _ => ownerReleased++);
        var resource = new RenderHandle(2, released.Add, owner);
        owner.Dispose();
        resource.ReplaceNative(3); resource.ReplaceNative(4);
        Check(resource.DangerousGetHandle() == 4 && released.SequenceEqual(new nint[] { 2, 3 }) && ownerReleased == 0,
            "Replacement retires each old native allocation while retaining the original device owner");
        resource.Dispose(); resource.Dispose();
        Check(released.SequenceEqual(new nint[] { 2, 3, 4 }) && ownerReleased == 1, "Last replacement and retained owner release exactly once");
        Reject<ObjectDisposedException>(() => resource.ReplaceNative(5));
        Check(released.SequenceEqual(new nint[] { 2, 3, 4, 5 }), "Closed replacement rejects and cleans its incoming allocation");

        var cleanup = new List<nint>();
        using var failed = new RenderHandle(6, value => { cleanup.Add(value); if (value == 6) throw new InvalidOperationException("Expected old-resource cleanup error"); });
        Reject<InvalidOperationException>(() => failed.ReplaceNative(7));
        Check(failed.DangerousGetHandle() == 7, "Replacement remains owned when old cleanup throws");
        failed.Dispose();
        Check(cleanup.SequenceEqual(new nint[] { 6, 7 }), "Cleanup failure does not leak or double release the newly adopted resource");
        ZeroAllocations();
        Console.WriteLine("Private native buffer replacement preserves ownership, cleanup/failure policy and zero warmed managed bytes.");
    }

    private static void ZeroAllocations()
    {
        var retired = 0;
        using var resource = new RenderHandle(100, _ => retired++);
        for (var value = 101; value < 229; value++) resource.ReplaceNative(value);
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var value = 229; value < 293; value++) resource.ReplaceNative(value);
        owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(owner == 0 && all == 0 && retired == 192 && resource.DangerousGetHandle() == 292,
            $"64 real replacements retain one wrapper: {owner}/{all} owner/all-thread bytes");
    }
}
