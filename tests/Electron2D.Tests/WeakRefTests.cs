using System.Runtime.CompilerServices;
using Electron2D;

internal static class WeakRefTests
{
    internal static void Run()
    {
        using var target = new Resource();
        using (var reference = new WeakRef<Resource>(target))
        {
            Check(ReferenceEquals(reference.GetRef(), target),
                "A live weak target must retain identity without copying its resource.");
            target.Dispose();
            Check(reference.GetRef() is null,
                "Deterministic target disposal must invalidate a weak reference even while the target is strongly held.");
        }

        using (var empty = new WeakRef<Resource>(null))
            Check(empty.GetRef() is null, "A null target creates an empty weak reference.");
        using (var disposedTarget = new WeakRef<Resource>(target))
            Check(disposedTarget.GetRef() is null, "A disposed target creates an empty weak reference.");

        using var owned = new Resource();
        var wrapper = new WeakRef<ElectronObject>(owned);
        wrapper.Dispose();
        Check(!owned.IsDisposed, "Disposing the weak wrapper must not dispose its target.");
        Reject<ObjectDisposedException>(() => wrapper.GetRef());

        using var collected = CreateUnrooted();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Check(collected.GetRef() is null, "The weak wrapper must not keep an otherwise unrooted target alive.");
        Console.WriteLine("Typed weak reference identity, disposal and collection checks passed.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakRef<Resource> CreateUnrooted()
    {
        var target = new Resource();
        return new WeakRef<Resource>(target);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
