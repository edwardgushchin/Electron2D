using Electron2D;
using static PhysicsDebugTests;

internal static class GPUPhysicsMemoryTests
{
    internal static void Run()
    {
        Check(GPUPhysicsBodyStore.BufferCreatedForTesting is null, "Memory checks own the diagnostic allocation hook");
        Growth();
        FailedPreparation();
    }

    private static void Growth()
    {
        using var store = new GPUPhysicsBodyStore();
        Check(store.BufferMemory == default, "Empty resident storage requests no buffers");
        var sizes = new uint[9]; var observed = 0;
        GPUPhysicsBodyStore.BufferCreatedForTesting = (_, _, size) => sizes[observed++] = size;
        var bodies = new GPUPhysicsBodyStore.BodyHandle[65];
        try { bodies[0] = store.Add(new(PhysicsServer.BodyMode.Rigid, default, 0, default, 0)); }
        finally { GPUPhysicsBodyStore.BufferCreatedForTesting = null; }
        var first = store.BufferMemory;
        var device = sizes.Sum(size => (long)size);
        var transfer = 24L + sizes[5] + sizes[6] + sizes[7];
        Check(observed == 9 && first.DeviceBytes == device && first.TransferBytes == transfer &&
            first.DeviceBuffers == 9 && first.TransferBuffers == 2 && first.Allocations == 11 && first.Releases == 0 &&
            first.PeakBytes == device + transfer, "Initial totals match independently observed buffer sizes and transfer layout");
        for (var i = 1; i < 64; i++) bodies[i] = store.Add(new(PhysicsServer.BodyMode.Rigid, default, 0, default, 0));
        Check(store.BufferMemory == first, "Authored bodies within retained capacity allocate no native buffers");

        var attempts = 0;
        GPUPhysicsBodyStore.BufferCreatedForTesting = (_, _, _) => { if (++attempts == 5) throw new IOException("memory growth probe"); };
        try { Reject(() => store.Add(new(PhysicsServer.BodyMode.Rigid, default, 0, default, 0))); }
        finally { GPUPhysicsBodyStore.BufferCreatedForTesting = null; }
        var failed = store.BufferMemory;
        Check(failed.DeviceBytes == first.DeviceBytes && failed.TransferBytes == first.TransferBytes &&
            failed.DeviceBuffers == first.DeviceBuffers && failed.TransferBuffers == first.TransferBuffers &&
            failed.Allocations - first.Allocations == 5 && failed.Releases - first.Releases == 5 && !store.HasFailed,
            "Rejected growth frees every temporary buffer and preserves live capacity");
        bodies[64] = store.Add(new(PhysicsServer.BodyMode.Rigid, default, 0, default, 0));
        var grown = store.BufferMemory;
        Check(grown.DeviceBytes == (device - 16) * 2 + 16 && grown.TransferBytes == (transfer - 24) * 2 + 24 &&
            grown.DeviceBuffers == 9 && grown.TransferBuffers == 2 &&
            grown.PeakBytes == first.DeviceBytes + first.TransferBytes + grown.DeviceBytes + grown.TransferBytes,
            "Successful growth accounts for both old and replacement buffers in its peak");
        foreach (var body in bodies) store.Remove(body);
        Check(store.BufferMemory == grown, "Removing bodies retains reusable native capacity");
        Console.WriteLine($"GPU buffer capacity, 64->128 slots: device {first.DeviceBytes}->{grown.DeviceBytes}, transfer {first.TransferBytes}->{grown.TransferBytes}, transient peak {grown.PeakBytes} B.");
        store.Dispose(); Empty(store); store.Dispose(); Empty(store);
    }

    private static void FailedPreparation()
    {
        for (var failure = 1; failure <= 9; failure++)
        {
            using var store = new GPUPhysicsBodyStore();
            var attempts = 0;
            GPUPhysicsBodyStore.BufferCreatedForTesting = (_, _, _) => { if (++attempts == failure) throw new IOException("memory preparation probe"); };
            try { Reject(() => store.Add(new(PhysicsServer.BodyMode.Rigid, default, 0, default, 0))); }
            finally { GPUPhysicsBodyStore.BufferCreatedForTesting = null; }
            Empty(store);
            Check(store.BufferMemory.Allocations == failure && !store.HasFailed, "Each preparation failure releases its exact requested prefix");
            store.Add(new(PhysicsServer.BodyMode.Rigid, default, 0, default, 0));
            store.Step(1f / 60, default); store.Dispose(); Empty(store);
        }
        Console.WriteLine("GPU buffer accounting: growth, retained capacity, nine preparation failures, retry and repeated disposal passed.");
    }

    internal static void Empty(GPUPhysicsBodyStore store)
    {
        var memory = store.BufferMemory;
        Check(memory.DeviceBytes == 0 && memory.TransferBytes == 0 && memory.DeviceBuffers == 0 &&
            memory.TransferBuffers == 0 && memory.Allocations == memory.Releases, "All requested native buffers have matching releases");
    }

    private static void Reject(Action action)
    {
        try { action(); }
        catch (IOException error) when (error.Message.StartsWith("memory ", StringComparison.Ordinal)) { return; }
        throw new InvalidOperationException("Expected injected preparation failure.");
    }
}
