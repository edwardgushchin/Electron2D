using Electron2D;

public static partial class BackendChecks
{
    public static void RunAnchorCapacity(bool gpu, bool threaded)
    {
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        foreach (var count in new[] { 8, 16, 64, 256 })
        {
            var backend = gpu ? PhysicsServer.Backend.GPU : PhysicsServer.Backend.CPU;
            var space = PhysicsServer.SpaceCreate(backend);
            Check(PhysicsServer.SpaceGetBackend(space) == backend && PhysicsServer.SpaceGetBackendFallbackReason(space) is null, "World anchor uses the requested backend");
            using var circle = new CircleShape { Radius = 2 };
            var bodies = new RID[count]; var joint = PhysicsServer.JointCreate();
            try
            {
                PhysicsServer.SpaceSetActive(space, true);
                for (var i = 0; i < count; i++)
                {
                    var body = bodies[i] = PhysicsServer.BodyCreate();
                    PhysicsServer.BodyAddShape(body, circle.GetRID()); PhysicsServer.BodySetGravityScale(body, 0); PhysicsServer.BodySetCanSleep(body, false);
                    PhysicsServer.BodySetCollisionLayer(body, 0); PhysicsServer.BodySetCollisionMask(body, 0);
                    PhysicsServer.BodySetSpace(body, space);
                }
                // Exhaust the collider capacity before the single-body joint adds its fixed anchor.
                PhysicsServer.SpaceStep(space, 1d / 120);
                PhysicsServer.JointMakePin(joint, Vector2.Zero, bodies[0]);
                PhysicsServer.BodySetLinearVelocity(bodies[0], new(10, 0));
                PhysicsServer.SpaceStep(space, 1d / 120);
                Check(PhysicsServer.BodyGetLinearVelocity(bodies[0]).Length() < .01f,
                    "A hard origin pin stops a 10 unit/s body within .01 unit/s after 1/120 s");
                for (var i = 0; i < 64; i++) PhysicsServer.SpaceStep(space, 1d / 120);
                Collect();
                var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 64; i++) PhysicsServer.SpaceStep(space, 1d / 120);
                own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(own == 0 && all == 0, $"World-anchor steps allocated {own}/{all} B");
                Console.WriteLine($"World anchor {backend}, separate={threaded}, {count} bodies: physical response, 64 steps {own}/{all} B.");
            }
            finally
            {
                PhysicsServer.FreeRID(joint);
                foreach (var body in bodies) if (body.IsValid()) PhysicsServer.FreeRID(body);
                PhysicsServer.FreeRID(space);
            }
        }
    }
}
