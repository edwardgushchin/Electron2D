using Electron2D;

internal static class PhysicsJointEnergyTests
{
    internal static void Run(PhysicsServer.Backend backend, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            (double Angular, float Gap) coarse = default;
            if (Environment.GetEnvironmentVariable("ELECTRON2D_JOINT_ENERGY_IMPACT_ONLY") == "1")
                coarse = VerifyLattice(backend, threaded, 8, true);
            else
            {
                foreach (var count in new[] { 65, 129, 257 })
                    foreach (var groove in new[] { false, true }) Verify(backend, threaded, count, groove);
                Verify(backend, threaded, 129, false, true);
                foreach (var side in new[] { 4, 8, 16 })
                {
                    VerifyLattice(backend, threaded, side, false);
                    var response = VerifyLattice(backend, threaded, side, true);
                    if (side == 8) coarse = response;
                }
                VerifyLattice(backend, threaded, 8, false, mixed: true);
                VerifyLattice(backend, threaded, 8, true, mixed: true);
            }
            Check(coarse.Angular < .03, "Default-iteration pin impact retains the original angular-momentum bound");
            var refined = VerifyLattice(backend, threaded, 8, true, iterations: 64);
            Check(refined.Angular < .03 && refined.Angular < coarse.Angular * .75 + .0001 && refined.Gap < coarse.Gap * .75 + .00001,
                "Additional requested iterations reduce angular and anchor error in the same coupled impact");
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
    private static (double Angular, float Gap) VerifyLattice(PhysicsServer.Backend backend, bool threaded, int side, bool collision, bool mixed = false, int iterations = 16)
    {
        const double step = 1d / 60;
        var space = PhysicsServer.SpaceCreate(backend);
        using var shape = new CircleShape { Radius = 1 };
        var gridCount = side * side; var count = gridCount + (collision ? 1 : 0);
        var bodies = new RID[count]; var masses = new float[count]; var poses = new Transform[count];
        var joints = new List<RID>(); var anchors = new List<(int A, int B, Vector2 LocalA, Vector2 LocalB)>();
        var angle = mixed ? .37f : 0; var direction = Vector2.Right.Rotated(angle);
        try
        {
            PhysicsServer.AreaSetGravity(space, 0); PhysicsServer.AreaSetLinearDamp(space, 0); PhysicsServer.AreaSetAngularDamp(space, 0);
            PhysicsServer.SpaceSetSolverIterations(space, iterations);
            for (var i = 0; i < count; i++)
            {
                var body = bodies[i] = PhysicsServer.BodyCreate(); masses[i] = mixed && i % 2 != 0 ? 8 : 1;
                PhysicsServer.BodySetMass(body, masses[i]); PhysicsServer.BodySetInertia(body, 1); PhysicsServer.BodySetCanSleep(body, false);
                PhysicsServer.BodySetCollisionLayer(body, collision ? 1u : 0u); PhysicsServer.BodySetCollisionMask(body, collision ? 1u : 0u);
                PhysicsServer.BodyAddShape(body, shape.GetRID());
                var position = i == gridCount ? new Vector2(-8, 0) : new Vector2(i % side * 4, i / side * 4);
                poses[i] = new(angle, position.Rotated(angle)); PhysicsServer.BodySetTransform(body, poses[i]); PhysicsServer.BodySetSpace(body, space);
            }
            for (var y = 0; y < side; y++) for (var x = 0; x < side; x++)
                {
                    var a = y * side + x;
                    if (x + 1 < side) Join(a, a + 1);
                    if (y + 1 < side) Join(a, a + side);
                }
            Check(joints.Count == 2 * side * (side - 1), "The connected lattice retains every horizontal and vertical constraint");
            var kicked = collision ? count - 1 : 0; var impulse = direction * (collision ? 8 : 1);
            using var state = collision ? PhysicsServer.BodyGetDirectState(bodies[kicked]) : null;
            if (collision) PhysicsServer.BodySetMaxContactsReported(bodies[kicked], 4);
            PhysicsServer.SpaceSetActive(space, true);
            for (var i = 0; i < 128; i++) PhysicsServer.SpaceStep(space, step);
            if (collision)
            {
                var savedPoses = new Transform[count]; var savedVelocities = new Vector2[count]; var savedAngular = new float[count];
                for (var i = 0; i < count; i++)
                {
                    savedPoses[i] = PhysicsServer.BodyGetTransform(bodies[i]); savedVelocities[i] = PhysicsServer.BodyGetLinearVelocity(bodies[i]);
                    savedAngular[i] = PhysicsServer.BodyGetAngularVelocity(bodies[i]);
                }
                using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(space);
                PhysicsServer.BodyApplyCentralImpulse(bodies[kicked], impulse);
                for (var i = 0; i < 256; i++) PhysicsServer.SpaceStep(space, step);
                checkpoint.Restore();
                for (var i = 0; i < count; i++)
                    Check(PhysicsServer.BodyGetTransform(bodies[i]).Origin.DistanceTo(savedPoses[i].Origin) < .0001f &&
                        MathF.Abs(Mathf.AngleDifference(PhysicsServer.BodyGetTransform(bodies[i]).Rotation, savedPoses[i].Rotation)) < .0001f &&
                        PhysicsServer.BodyGetLinearVelocity(bodies[i]).DistanceTo(savedVelocities[i]) < .0001f &&
                        MathF.Abs(PhysicsServer.BodyGetAngularVelocity(bodies[i]) - savedAngular[i]) < .0001f,
                        "Impact preparation restores every body's captured state through its checkpoint");
            }
            double suppliedEnergy = 0, expectedAngular = 0; var expectedMomentum = impulse;
            for (var i = 0; i < count; i++)
            {
                var position = PhysicsServer.BodyGetTransform(bodies[i]).Origin; var velocity = PhysicsServer.BodyGetLinearVelocity(bodies[i]);
                var angular = PhysicsServer.BodyGetAngularVelocity(bodies[i]);
                suppliedEnergy += .5 * (masses[i] * velocity.LengthSquared() + angular * angular); expectedMomentum += velocity * masses[i];
                expectedAngular += masses[i] * (position.X * velocity.Y - position.Y * velocity.X) + angular;
            }
            var kickPosition = PhysicsServer.BodyGetTransform(bodies[kicked]).Origin;
            suppliedEnergy += PhysicsServer.BodyGetLinearVelocity(bodies[kicked]).Dot(impulse) + .5 * impulse.LengthSquared();
            expectedAngular += kickPosition.X * impulse.Y - kickPosition.Y * impulse.X;
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
            var tick = PhysicsServer.SpaceGetTick(space);
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            PhysicsServer.BodyApplyCentralImpulse(bodies[kicked], impulse);
            double peakEnergy = 0, maxMomentum = 0, maxAngular = 0; float maxGap = 0; var contactFrames = 0;
            for (var frame = 0; frame < 512; frame++)
            {
                var before = PhysicsServer.BodyGetLinearVelocity(bodies[kicked]); PhysicsServer.SpaceStep(space, step);
                if (collision)
                {
                    var reported = Vector2.Zero;
                    for (var contact = 0; contact < state!.GetContactCount(); contact++)
                    {
                        reported += state.GetContactImpulse(contact);
                        Check(bodies.AsSpan(0, gridCount).Contains(state.GetContactCollider(contact)) && state.GetContactLocalShape(contact) == 0 && state.GetContactColliderShape(contact) == 0,
                            "The free witness reports real lattice identities and logical shape slots");
                    }
                    if (reported.LengthSquared() > 0) contactFrames++;
                    Check((PhysicsServer.BodyGetLinearVelocity(bodies[kicked]) - before).DistanceTo(reported) < .002f,
                        "The unit-mass witness publishes its complete contact impulse without hidden joint forces");
                }
                double energy = 0, angularMomentum = 0; var momentum = Vector2.Zero;
                for (var i = 0; i < count; i++)
                {
                    var pose = PhysicsServer.BodyGetTransform(bodies[i]); var velocity = PhysicsServer.BodyGetLinearVelocity(bodies[i]); var angular = PhysicsServer.BodyGetAngularVelocity(bodies[i]);
                    Check(pose.IsFinite() && velocity.IsFinite() && float.IsFinite(angular) && !PhysicsServer.BodyGetSleeping(bodies[i]), "Every lattice body stays finite and awake");
                    energy += .5 * (masses[i] * velocity.LengthSquared() + angular * angular); momentum += velocity * masses[i];
                    angularMomentum += masses[i] * (pose.Origin.X * velocity.Y - pose.Origin.Y * velocity.X) + angular;
                }
                peakEnergy = Math.Max(peakEnergy, energy); maxMomentum = Math.Max(maxMomentum, momentum.DistanceTo(expectedMomentum)); maxAngular = Math.Max(maxAngular, Math.Abs(angularMomentum - expectedAngular));
                foreach (var a in anchors) maxGap = Math.Max(maxGap, (PhysicsServer.BodyGetTransform(bodies[a.A]) * a.LocalA).DistanceTo(PhysicsServer.BodyGetTransform(bodies[a.B]) * a.LocalB));
                Check(double.IsFinite(energy) && double.IsFinite(angularMomentum) && energy <= suppliedEnergy * 1.01 + .0001 && maxMomentum < .002,
                    "Passive coupled impact cannot create energy or lose complete linear momentum");
            }
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0, "Warmed coupled lattice response allocates managed memory");
            Check(PhysicsServer.SpaceGetTick(space) == tick + 512 && PhysicsServer.SpaceGetBackend(space) == backend && PhysicsServer.SpaceGetSolverIterations(space) == iterations,
                "Every measured lattice step retains the requested numerical backend, iteration count and fixed tick");
            Check(maxGap < .05f && (!collision || contactFrames > 0), "The lattice keeps joined anchors and the impact has actual nonzero contact impulses");
            Console.WriteLine($"Joint lattice {backend}, threaded={threaded}, side={side}, mixed={mixed}, collision={collision}, iterations={iterations}: {count} bodies/{joints.Count} joints, energy={peakEnergy:R}, momentum={maxMomentum:R}, angular={maxAngular:R}, gap={maxGap:R}, contactFrames={contactFrames}; 512 full steps {owner}/{all} B.");
            return (maxAngular, maxGap);

            void Join(int a, int b)
            {
                var point = (poses[a].Origin + poses[b].Origin) * .5f; var joint = PhysicsServer.JointCreate(); joints.Add(joint);
                PhysicsServer.JointMakePin(joint, point, bodies[a], bodies[b]);
                anchors.Add((a, b, poses[a].AffineInverse() * point, poses[b].AffineInverse() * point));
            }
        }
        finally
        {
            foreach (var joint in joints) PhysicsServer.FreeRID(joint);
            foreach (var body in bodies) if (body.IsValid()) PhysicsServer.FreeRID(body);
            PhysicsServer.FreeRID(space);
        }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
