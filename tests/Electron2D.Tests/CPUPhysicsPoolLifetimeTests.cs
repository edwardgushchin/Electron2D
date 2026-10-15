using System.Runtime.CompilerServices;
using Box2D.NET;
using Electron2D;
using static Box2D.NET.B2Arrays;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;
using static PhysicsDebugTests;

internal static class CPUPhysicsPoolLifetimeTests
{
    private sealed class Payload
    {
        private readonly byte[] _data = new byte[65536];
        internal int Counter;
        internal B2Manifold Manifold(B2Shape a, in B2Transform first, B2Shape b, in B2Transform second, ref B2SimplexCache cache)
        { GC.KeepAlive(_data); return default; }
    }
    internal static void Run()
    {
        List<string> retained = [];
        foreach (var parent in new[] { false, true })
            foreach (var history in new[] { false, true })
            {
                var world = b2CreateWorld(b2DefaultWorldDef());
                try
                {
                    var (point, targets) = RetireRecords(world, parent, history);
                    using (point)
                    {
                        Collect();
                        for (var i = 0; i < targets.Length; i++)
                            if (Alive(targets[i])) retained.Add($"records parent={parent}, history={history}, payload={i}");
                        GC.KeepAlive(point);
                    }
                }
                finally { b2DestroyWorld(world); }
            }
        foreach (var history in new[] { false, true })
        {
            var world = b2CreateWorld(b2DefaultWorldDef());
            try
            {
                var (point, target) = RetireEvents(world, history);
                using (point) { Collect(); if (Alive(target)) retained.Add($"move events history={history}"); GC.KeepAlive(point); }
            }
            finally { b2DestroyWorld(world); }
        }
        for (var operation = 0; operation < 5; operation++)
        {
            var (array, kept, removed) = RetireArray(operation);
            Collect(); Check(kept is null || Alive(kept), "The active event prefix keeps its borrowed payload");
            if (Alive(removed)) retained.Add($"event array operation={operation}");
            GC.KeepAlive(array.data);
        }
        var space = PhysicsServer.SpaceCreate(PhysicsServer.Backend.CPU);
        try { var target = RetireCollider(space); Collect(); if (target.TryGetTarget(out _)) retained.Add("engine shaped collider"); }
        finally { PhysicsServer.FreeRID(space); }
        WarmSpares();
        BorrowedHistory();
        Console.WriteLine("CPU pool retained targets: " + string.Join(", ", retained));
        Check(retained.Count == 0, "CPU pool/history kept released payloads");
        if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_CPU_POOL_LIFETIME") == "1")
            Check(!DisplayServer.IsAvailable && !RenderingServer.IsAvailable, "CPU pool lifetime checks require no graphics services");
        Console.WriteLine("CPU pool lifetime: retired body/shape/joint payloads, move events, recapture, engine attachments and reusable class spares passed.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (CPUPhysicsCheckpoint?, WeakReference<object>[]) RetireRecords(B2WorldId world, bool parent, bool history)
    {
        var bodyData = new Payload(); var shapeData = new Payload(); var jointData = new Payload();
        var definition = b2DefaultBodyDef(); definition.type = B2BodyType.b2_dynamicBody; definition.userData = new(bodyData); definition.name = new string('n', 64);
        var a = b2CreateBody(world, definition); var b = b2CreateBody(world, b2DefaultBodyDef());
        var shape = b2DefaultShapeDef(); shape.userData = new(shapeData); shape.manifoldOverride = shapeData.Manifold;
        var id = b2CreatePolygonShape(a, shape, b2MakeBox(1, 1));
        var joint = b2DefaultRevoluteJointDef(); joint.@base.bodyIdA = a; joint.@base.bodyIdB = b; joint.@base.userData = new(jointData);
        var link = b2CreateRevoluteJoint(world, joint);
        WeakReference<object>[] targets = [new(bodyData), new(shapeData), new(jointData), new(b2Body_GetName(a))];
        var point = history ? new CPUPhysicsCheckpoint(world) : null;
        if (!parent) { b2DestroyJoint(link, false); b2DestroyShape(id, false); }
        b2DestroyBody(a);
        point?.Capture();
        return (point, targets);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (CPUPhysicsCheckpoint?, WeakReference<object>) RetireEvents(B2WorldId world, bool history)
    {
        var payload = new Payload(); var body = b2DefaultBodyDef(); body.type = B2BodyType.b2_dynamicBody; body.enableSleep = false; body.userData = new(payload);
        var id = b2CreateBody(world, body); b2CreatePolygonShape(id, b2DefaultShapeDef(), b2MakeBox(1, 1));
        b2World_Step(world, 1f / 60, 4);
        Check(b2World_GetBodyEvents(world).moveCount == 1, "The source publishes one actual body move event");
        b2Body_SetUserData(id, default);
        var point = history ? new CPUPhysicsCheckpoint(world) : null;
        b2DestroyBody(id); b2World_Step(world, 1f / 60, 4);
        Check(b2World_GetBodyEvents(world).moveCount == 0, "A new interval replaces the retired event history");
        point?.Capture(); return (point, new(payload));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (B2Array<B2BodyMoveEvent>, WeakReference<object>?, WeakReference<object>) RetireArray(int operation)
    {
        var a = new Payload(); var b = new Payload(); B2Array<B2BodyMoveEvent> array = default;
        b2Array_Push(ref array, new() { userData = new(a) }); b2Array_Push(ref array, new() { userData = new(b) });
        switch (operation)
        {
            case 0: b2Array_Clear(ref array); return (array, null, new(b));
            case 1: b2Array_Resize(ref array, 1); return (array, new(a), new(b));
            case 2: _ = b2Array_Pop(ref array); return (array, new(a), new(b));
            case 3: b2Array_RemoveSwap(ref array, 0); return (array, new(b), new(a));
            default: b2Array_RemoveSwap(ref array, 1); return (array, new(a), new(b));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<PhysicsColliderBackend> RetireCollider(RID space)
    {
        using var circle = new CircleShape { Radius = 3 }; var body = PhysicsServer.BodyCreate();
        PhysicsServer.BodyAddShape(body, circle.GetRID()); PhysicsServer.BodySetSpace(body, space);
        var target = new WeakReference<PhysicsColliderBackend>(PhysicsServer.Service.GetSceneSpace(space).FindSnapshotObject(body)!.Backend);
        PhysicsServer.FreeRID(body); return target;
    }

    private static void WarmSpares()
    {
        B2Array<B2BodySim> array = default; b2Array_Resize(ref array, 2);
        var a = array.data[0]; var b = array.data[1];
        Cycle(); Check(ReferenceEquals(a, array.data[1]) && ReferenceEquals(b, array.data[0]), "Removal preserves two distinct reusable record shells");
        Collect(); for (var i = 0; i < 64; i++) Cycle();
        var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 64; i++) Cycle();
        own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(own == 0 && all == 0, $"Class spare reuse allocated {own}/{all} B");
        Console.WriteLine($"CPU class spare resize/remove/clear: 64 cycles {own}/{all} B.");
        void Cycle() { b2Array_Clear(ref array); b2Array_Resize(ref array, 2); b2Array_RemoveSwap(ref array, 0); b2Array_Resize(ref array, 2); }
    }
    private static void BorrowedHistory()
    {
        var world = b2CreateWorld(b2DefaultWorldDef());
        try
        {
            var (point, target, body) = CaptureBorrowed(world);
            using (point)
            {
                Collect(); Check(Alive(target), "A current checkpoint retains its borrowed payload");
                RestoreBorrowed(point, body); point.Dispose(); Collect();
                Check(!Alive(target), "Disposal releases the payload after the live body clears its reference");
                GC.KeepAlive(point);
            }
        }
        finally { b2DestroyWorld(world); }
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (CPUPhysicsCheckpoint, WeakReference<object>, B2BodyId) CaptureBorrowed(B2WorldId world)
    {
        var payload = new Payload(); var definition = b2DefaultBodyDef(); definition.userData = new(payload);
        var body = b2CreateBody(world, definition); var point = new CPUPhysicsCheckpoint(world);
        payload.Counter = 7; b2Body_SetUserData(body, default); return (point, new(payload), body);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void RestoreBorrowed(CPUPhysicsCheckpoint point, B2BodyId body)
    {
        point.Restore(); Check(b2Body_GetUserData(body).GetRef<Payload>()?.Counter == 7, "Restore borrows the same object without cloning its mutable contents");
        b2Body_SetUserData(body, default);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Alive(WeakReference<object> target) => target.TryGetTarget(out _);
    private static void Collect()
    {
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
    }
}
