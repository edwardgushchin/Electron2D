using Electron2D;

internal static class PhysicsSolverStorageTests
{
    internal static void Run() { VerifySolverArrayReuse(); VerifySolverVectorArithmetic(); VerifySleepingStorage(); VerifyBroadphasePairReuse(); }
    private static void VerifySolverArrayReuse()
    {
        var array = Box2D.NET.B2Arrays.b2Array_Create<Box2D.NET.B2ContactSim>(4);
        Box2D.NET.B2Arrays.b2Array_Resize(ref array, 3);
        var removed = array.data[0]; var last = array.data[2];
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        var moved = Box2D.NET.B2Arrays.b2Array_RemoveSwap(ref array, 0);
        Check(GC.GetAllocatedBytesForCurrentThread() == allocated && moved == 2 && array.count == 2 && ReferenceEquals(array.data[0], last) && ReferenceEquals(array.data[2], removed), "Solver compaction keeps active and spare objects distinct without constructing replacements.");
        for (var i = 0; i < 128; i++)
        {
            Box2D.NET.B2Arrays.b2Array_Add(ref array).contactId = i;
            Box2D.NET.B2Arrays.b2Array_RemoveSwap(ref array, 0);
        }
        allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 128; i++)
        {
            Box2D.NET.B2Arrays.b2Array_Add(ref array).contactId = i;
            Box2D.NET.B2Arrays.b2Array_RemoveSwap(ref array, 0);
        }
        Check(GC.GetAllocatedBytesForCurrentThread() == allocated && !ReferenceEquals(array.data[0], array.data[1]), "Repeated contact churn reuses spare solver storage and does not alias live slots.");
    }

    private static void VerifySolverVectorArithmetic()
    {
        var random = new System.Random(742);
        for (var i = 0; i < 256; i++)
        {
            var a = new Box2D.NET.B2FloatW(); var b = a; var c = a;
            for (var lane = 0; lane < 4; lane++)
            { a[lane] = (float)(random.NextDouble() * 200 - 100); b[lane] = (float)(random.NextDouble() * 200 - 100); c[lane] = (float)(random.NextDouble() * 200 - 100); }
            var add = Box2D.NET.B2ContactSolvers.b2AddW(a, b);
            var sub = Box2D.NET.B2ContactSolvers.b2SubW(a, b);
            var mul = Box2D.NET.B2ContactSolvers.b2MulW(a, b);
            var madd = Box2D.NET.B2ContactSolvers.b2MulAddW(a, b, c);
            var msub = Box2D.NET.B2ContactSolvers.b2MulSubW(a, b, c);
            for (var lane = 0; lane < 4; lane++)
                Check(add[lane] == a[lane] + b[lane] && sub[lane] == a[lane] - b[lane] && mul[lane] == a[lane] * b[lane] &&
                    madd[lane] == a[lane] + b[lane] * c[lane] && msub[lane] == a[lane] - b[lane] * c[lane], "Four-lane solver arithmetic preserves independent scalar results.");
        }
    }

    private static void VerifySleepingStorage()
    {
        using var root = new SubViewport();
        using var shape = new CircleShape();
        var body = new RigidBody { MaxContactsReported = 8, GravityScale = 0 };
        body.AddChild(new CollisionShape { Shape = shape });
        root.AddChild(body);
        using var tree = new SceneTree(root);
        for (var i = 0; i < 8; i++) { body.Sleeping = true; PhysicsTick(tree); body.Sleeping = false; PhysicsTick(tree); }
        var world = Box2D.NET.B2Worlds.b2GetWorldFromId(Box2D.NET.B2Bodies.b2Body_GetWorld(body.BackendID));
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) { body.Sleeping = true; PhysicsTick(tree); body.Sleeping = false; PhysicsTick(tree); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed sleep/wake fixed steps reuse sleeping-island buffers.");
        var retained = world.solverSets.data.Where(s => s.setIndex == -1 && s.bodySims.capacity > 0).ToArray();
        Check(retained.Length > 0, "Inactive solver sets retain prepared capacity until world teardown.");
        tree.Dispose();
        Check(retained.All(s => s.bodySims.data is null && s.contactSims.data is null), "World teardown releases live and inactive cached solver arrays.");
    }

    private static void VerifyBroadphasePairReuse()
    {
        using var root = new SubViewport();
        using var shape = new CircleShape { Radius = 20 };
        RigidBody? first = null;
        for (var i = 0; i < 96; i++)
        {
            var body = new RigidBody { Name = "Pair" + i, Position = new(100, 100), CanSleep = false };
            body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body); first ??= body;
        }
        using var tree = new SceneTree(root);
        PhysicsServer.AreaSetGravity(first!.GetWorld()!.Space, 0);
        PhysicsTick(tree);
        var world = Box2D.NET.B2Worlds.b2GetWorldFromId(first.Space!.WorldID);
        var needed = Box2D.NET.B2Atomics.b2AtomicLoadInt(ref world.broadPhase.movePairIndex);
        Check(needed > 16 * 96 && world.broadPhase.movePairCapacity >= needed,
            "Dense broad-phase queries retain overflow pair demand for subsequent arena reuse.");
    }

    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static void PhysicsTick(SceneTree tree) => tree.PhysicsFrame(1d / 60);
}
