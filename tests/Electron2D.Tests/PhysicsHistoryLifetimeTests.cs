using System.Runtime.CompilerServices;
using Electron2D;
using static PhysicsDebugTests;

internal static class PhysicsHistoryLifetimeTests
{
    internal static void Run(bool gpu = false)
    {
        var backend = gpu ? PhysicsServer.Backend.GPU : PhysicsServer.Backend.CPU;
        List<string> retained = [];
        foreach (var explicitRelease in new[] { false, true })
        {
            var (checkpoint, space) = ClosedWorld(backend, explicitRelease);
            Collect();
            var alive = space.TryGetTarget(out _);
            var name = $"{backend} closed world, explicit checkpoint release={explicitRelease}";
            Console.WriteLine($"{name}: retained={alive}");
            if (alive) retained.Add(name);
            GC.KeepAlive(checkpoint);
        }
        for (var operation = 0; operation < 4; operation++)
        {
            var space = PhysicsServer.SpaceCreate(backend); var body = PhysicsServer.BodyCreate();
            try
            {
                PhysicsServer.BodySetSpace(body, space);
                using var map = new PhysicsSnapshotMap(space);
                var entry = RemovedBinding(map, body, operation);
                Collect(); var alive = entry.TryGetTarget(out _);
                var name = $"{backend} removed binding after operation {operation}";
                Console.WriteLine($"{name}: retained={alive}");
                if (alive) retained.Add(name);
                GC.KeepAlive(map);
            }
            finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(space); }
        }
        PartialPreparation(backend, retained);
        ContactTail(backend, retained);
        Check(retained.Count == 0, "History scratch retained released owners: " + string.Join(", ", retained));
        BackendChecks.RunHistoryStorage(backend);
        Console.WriteLine($"{backend} history lifetime: disposed checkpoints and successful/rejected portable operations release borrowed object graphs.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (PhysicsCheckpoint, WeakReference<PhysicsSpace>) ClosedWorld(PhysicsServer.Backend backend, bool explicitRelease)
    {
        var rid = PhysicsServer.SpaceCreate(backend);
        var space = new WeakReference<PhysicsSpace>(PhysicsServer.Service.GetSceneSpace(rid));
        var checkpoint = PhysicsServer.SpaceCreateCheckpoint(rid);
        if (explicitRelease) checkpoint.Dispose();
        PhysicsServer.FreeRID(rid);
        Check(checkpoint.IsDisposed, "The surviving public wrapper is disposed");
        return (checkpoint, space);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<PhysicsReplayEntry> RemovedBinding(PhysicsSnapshotMap map, RID body, int operation)
    {
        map.Bind(1, 1, body);
        var entry = new WeakReference<PhysicsReplayEntry>(map.Collider(1));
        var snapshot = new PhysicsSnapshot(operation == 3 ? 48 : 1024 * 1024);
        if (operation == 3) Reject<InvalidOperationException>(() => map.Capture(snapshot));
        else
        {
            map.Capture(snapshot);
            if (operation == 1) map.Apply(snapshot);
            else if (operation == 2)
            {
                PhysicsServer.BodySetCollisionMask(body, 2);
                Reject<InvalidDataException>(() => map.Apply(snapshot));
            }
        }
        Check(map.Unbind(1), "The binding was removed without disposing the map or body");
        return entry;
    }

    private static void PartialPreparation(PhysicsServer.Backend backend, List<string> retained)
    {
        var space = PhysicsServer.SpaceCreate(backend); var first = PhysicsServer.BodyCreate(); var second = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.BodySetSpace(first, space); PhysicsServer.BodySetSpace(second, space);
            using var map = new PhysicsSnapshotMap(space);
            var body = FailedPreparation(map, space, first, second); first = default;
            Collect(); var alive = body.TryGetTarget(out _);
            Console.WriteLine($"{backend} rejected partial preparation: retained={alive}");
            if (alive) retained.Add($"{backend} partial preparation");
            GC.KeepAlive(map);
        }
        finally { if (first.IsValid()) PhysicsServer.FreeRID(first); PhysicsServer.FreeRID(second); PhysicsServer.FreeRID(space); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<PhysicsColliderBackend> FailedPreparation(PhysicsSnapshotMap map, RID space, RID first, RID second)
    {
        map.Bind(1, 1, first); map.Bind(2, 1, second);
        var body = new WeakReference<PhysicsColliderBackend>(map.Collider(1).Backend);
        PhysicsServer.BodySetSpace(second, default); PhysicsServer.BodySetSpace(second, space);
        var snapshot = new PhysicsSnapshot();
        Reject<InvalidOperationException>(() => map.Capture(snapshot));
        Check(map.Unbind(1), "The successfully prepared prefix binding was removed");
        PhysicsServer.FreeRID(first);
        return body;
    }

    private static void Collect()
    {
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true);
    }

    private static void ContactTail(PhysicsServer.Backend backend, List<string> retained)
    {
        var space = PhysicsServer.SpaceCreate(backend); var body = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.BodySetSpace(body, space);
            var runtime = PhysicsServer.Service.BodyRuntime(body);
            var (state, current, retired) = ReplacedContacts(runtime, body);
            Collect();
            Check(Alive(current), "The current captured contact retains its sampled identity");
            if (Alive(retired)) retained.Add($"{backend} replaced contact tail");
            runtime.CaptureReplay(state); Collect();
            Check(state.ContactCount == 0, "Replacing contacts with an empty view captures an empty history");
            if (Alive(current) || Alive(retired)) retained.Add($"{backend} empty contact tail");
            GC.KeepAlive(state);
        }
        finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(space); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (PhysicsBodyRuntime.ReplayState, WeakReference<object>, WeakReference<object>) ReplacedContacts(PhysicsBodyRuntime runtime, RID body)
    {
        var current = new object(); var retired = new object();
        var view = PhysicsServer.BodyGetDirectState(body)!;
        view.RestoreContacts([default(PhysicsDirectBodyState.Contact) with { LocalSlotIdentity = current },
            default(PhysicsDirectBodyState.Contact) with { OtherSlotIdentity = retired }]);
        var state = new PhysicsBodyRuntime.ReplayState(); runtime.CaptureReplay(state);
        view.RestoreContacts([default(PhysicsDirectBodyState.Contact) with { LocalSlotIdentity = current }]);
        runtime.CaptureReplay(state); runtime.InvalidateView(); view.Dispose();
        return (state, new(current), new(retired));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Alive(WeakReference<object> value) => value.TryGetTarget(out _);
}
