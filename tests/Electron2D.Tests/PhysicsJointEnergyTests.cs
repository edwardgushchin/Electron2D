using Electron2D;

internal static class PhysicsJointEnergyTests
{
    internal static void Run(PhysicsServer.Backend backend, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            foreach (var count in new[] { 65, 129, 257 })
                foreach (var groove in new[] { false, true }) Verify(backend, threaded, count, groove);
            Verify(backend, threaded, 129, false, true);
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private static void Verify(PhysicsServer.Backend backend, bool threaded, int count, bool groove, bool mixed = false)
    {
        const double step = 1d / 60;
        var space = PhysicsServer.SpaceCreate(backend);
        using var shape = new CircleShape { Radius = 1 };
        var bodies = new RID[count]; var joints = new RID[count - 1]; var masses = new float[count];
        var angle = mixed ? .37f : 0; var direction = Vector2.Right.Rotated(angle);
        try
        {
            PhysicsServer.AreaSetGravity(space, 0); PhysicsServer.AreaSetLinearDamp(space, 0); PhysicsServer.AreaSetAngularDamp(space, 0);
            for (var i = 0; i < count; i++)
            {
                var body = bodies[i] = PhysicsServer.BodyCreate(); masses[i] = mixed && i % 2 != 0 ? 8 : 1;
                PhysicsServer.BodySetMass(body, masses[i]); PhysicsServer.BodySetInertia(body, 1); PhysicsServer.BodySetCanSleep(body, false);
                PhysicsServer.BodySetCollisionLayer(body, 0); PhysicsServer.BodySetCollisionMask(body, 0);
                PhysicsServer.BodyAddShape(body, shape.GetRID()); PhysicsServer.BodySetTransform(body, new(angle, direction * (i * 4)));
                PhysicsServer.BodySetSpace(body, space);
                if (i == 0) continue;
                var joint = joints[i - 1] = PhysicsServer.JointCreate(); var anchor = direction * (i * 4 - 2);
                if (groove) PhysicsServer.JointMakeGroove(joint, anchor + Vector2.Up, anchor + Vector2.Down, anchor, bodies[i - 1], body);
                else PhysicsServer.JointMakePin(joint, anchor, bodies[i - 1], body);
            }
            PhysicsServer.SpaceSetActive(space, true);
            for (var i = 0; i < 128; i++) PhysicsServer.SpaceStep(space, step);
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            PhysicsServer.BodyApplyCentralImpulse(bodies[0], direction);
            double peak = 0;
            for (var frame = 0; frame < 512; frame++)
            {
                PhysicsServer.SpaceStep(space, step);
                double energy = 0; var momentum = Vector2.Zero;
                for (var i = 0; i < count; i++)
                {
                    var velocity = PhysicsServer.BodyGetLinearVelocity(bodies[i]); var angular = PhysicsServer.BodyGetAngularVelocity(bodies[i]);
                    energy += .5 * (masses[i] * velocity.LengthSquared() + angular * angular); momentum += velocity * masses[i];
                }
                peak = Math.Max(peak, energy);
                // A unit impulse supplies 0.5 scene energy units; allow 1% plus float anchor noise.
                if (!double.IsFinite(energy) || energy > .5051)
                    throw new InvalidOperationException($"Passive {count}-body {(groove ? "groove" : "pin")} chain creates energy at frame {frame + 1}: {energy:R} scene energy units.");
                Check(momentum.DistanceTo(direction) < .002f, "Passive joint chains preserve complete linear momentum");
            }
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Console.WriteLine($"Joint energy {backend}, threaded={threaded}, bodies={count}, groove={groove}, mixed={mixed}: peak={peak:R} scene energy units, 512 response frames {owner}/{all} B.");
            Check(owner == 0 && all == 0, "Warmed passive chain response allocates managed memory");
            Check(PhysicsServer.SpaceGetBackend(space) == backend, "Passive chain retains selected backend identity");
            for (var i = 1; i < count; i++)
            {
                var a = PhysicsServer.BodyGetTransform(bodies[i - 1]) * new Vector2(2, 0);
                var b = PhysicsServer.BodyGetTransform(bodies[i]) * new Vector2(-2, 0);
                Check(a.DistanceTo(b) < .05f, "Passive chain keeps its physical anchors joined");
            }
        }
        finally
        {
            foreach (var joint in joints) if (joint.IsValid()) PhysicsServer.FreeRID(joint);
            foreach (var body in bodies) if (body.IsValid()) PhysicsServer.FreeRID(body);
            PhysicsServer.FreeRID(space);
        }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
