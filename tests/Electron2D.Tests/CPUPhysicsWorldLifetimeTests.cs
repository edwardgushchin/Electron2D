using System.Runtime.CompilerServices;
using Box2D.NET;
using Electron2D;

internal static class CPUPhysicsWorldLifetimeTests
{
    internal static void Run()
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        var retained = 0;
        try
        {
            foreach (var threaded in new[] { false, true })
                foreach (var history in new[] { false, true })
                    foreach (var fault in new[] { false, true })
                    {
                        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
                        var (points, targets) = RetireWorlds(history, fault);
                        Collect();
                        foreach (var target in targets) if (target.TryGetTarget(out _)) retained++;
                        foreach (var point in points) point.Dispose();
                        GC.KeepAlive(points);
                        Console.WriteLine($"CPU retired world scratch: separate={threaded}, history={history}, fault={fault}, cumulative retained={retained}.");
                    }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
        Check(retained == 0, $"Static CPU world slots retained {retained} retired scratch arrays");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (CPUPhysicsCheckpoint[], WeakReference<object>[]) RetireWorlds(bool history, bool fault)
    {
        var spaces = new RID[4]; var bodies = new RID[4 * 32];
        var points = new List<CPUPhysicsCheckpoint>(); var targets = new List<WeakReference<object>>();
        using var shape = new CircleShape { Radius = 2 };
        try
        {
            for (var w = 0; w < spaces.Length; w++)
            {
                var space = spaces[w] = PhysicsServer.SpaceCreate(PhysicsServer.Backend.CPU);
                PhysicsServer.SpaceSetActive(space, true);
                for (var i = 0; i < 32; i++)
                {
                    var body = bodies[w * 32 + i] = PhysicsServer.BodyCreate();
                    PhysicsServer.BodySetGravityScale(body, 0); PhysicsServer.BodySetCanSleep(body, false);
                    PhysicsServer.BodyAddShape(body, shape.GetRID());
                    PhysicsServer.BodySetTransform(body, new(0, new(i / 2 * 12, i % 2 * 3.5f)));
                    PhysicsServer.BodySetSpace(body, space);
                }
                PhysicsServer.SpaceStep(space, 1d / 120);
                var world = B2Worlds.b2GetWorldFromId(PhysicsServer.Service.GetSceneSpace(space).WorldID);
                var blocks = world.reusableGraphColorBlocks.First(segment => segment.Array is not null).Array!;
                Check(world.constraintGraph.colors.Any(color => color.contactSims.count > 0), "The world has a real solved contact graph");
                targets.Add(new(blocks)); targets.Add(new(world.constraintGraph.colors));
                if (history) points.Add(new(PhysicsServer.Service.GetSceneSpace(space).WorldID));
                if (fault)
                {
                    world.finalizeBodyStates = _ => throw new InvalidOperationException("Injected retirement failure.");
                    var rejected = false;
                    try { PhysicsServer.SpaceStep(space, 1d / 120); }
                    catch (AggregateException) { rejected = true; }
                    Check(rejected, "The injected numerical failure is observable");
                }
                else
                {
                    for (var n = 0; n < 64; n++) PhysicsServer.SpaceStep(space, 1d / 120);
                    Check(world.reusableGraphColorBlocks.Any(segment => ReferenceEquals(segment.Array, blocks)), "Live steps reuse the graph-block array");
                }
            }
        }
        finally
        {
            foreach (var space in spaces) if (space.IsValid()) PhysicsServer.FreeRID(space);
            foreach (var body in bodies) if (body.IsValid()) PhysicsServer.FreeRID(body);
        }
        return (points.ToArray(), targets.ToArray());
    }

    private static void Collect()
    {
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
