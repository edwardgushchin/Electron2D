using Electron2D;

public static partial class BackendChecks
{
    private static void CompoundResponse(World world, SceneTree tree, SubViewport root, PhysicsServer.Backend? backend, Solver? solver)
    {
        using var disk = new CircleShape { Radius = 2 }; using var sensorDisk = new CircleShape { Radius = 1 };
        var mover = new RigidBody
        {
            Name = "CompoundMover",
            Mass = 2,
            Inertia = 4,
            LockRotation = true,
            GravityScale = 0,
            CanSleep = false,
            MaxContactsReported = 8,
            LinearDamp = 0,
            LinearDampMode = RigidBody.DampMode.Replace
        };
        mover.AddChild(new CollisionShape { Name = "Disabled", Shape = disk, Disabled = true });
        mover.AddChild(new CollisionShape { Name = "Top", Shape = disk, Position = new(0, -6) });
        mover.AddChild(new CollisionShape { Name = "Bottom", Shape = disk, Position = new(0, 6) }); root.AddChild(mover);
        var sensor = new Area { Name = "CompoundSensor" };
        sensor.AddChild(new CollisionShape { Name = "Disabled", Shape = sensorDisk, Disabled = true });
        sensor.AddChild(new CollisionShape { Name = "Top", Shape = sensorDisk, Position = new(0, -6) });
        sensor.AddChild(new CollisionShape { Name = "Bottom", Shape = sensorDisk, Position = new(0, 6) }); root.AddChild(sensor);
        var entered = 0; var exited = 0;
        sensor.BodyShapeEntered += (rid, _, remote, local) => { Pair(rid, remote, local); entered++; };
        sensor.BodyShapeExited += (rid, _, remote, local) => { Pair(rid, remote, local); exited++; };
        void Pair(RID rid, int remote, int local)
        { Check(rid == mover.GetRID() && remote == local && remote is 1 or 2, "Area events retain each real compound logical pair"); }
        var obstacle = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate();
        try
        {
            PhysicsServer.BodySetMode(obstacle, PhysicsServer.BodyMode.Static);
            foreach (var id in new[] { mover.GetRID(), obstacle }) { PhysicsServer.BodySetFriction(id, 0); PhysicsServer.BodySetBounce(id, 0); }
            PhysicsServer.BodyAddShape(obstacle, disk.GetRID(), disabled: true);
            PhysicsServer.BodyAddShape(obstacle, disk.GetRID(), new(0, new Vector2(0, -6)));
            PhysicsServer.BodyAddShape(obstacle, disk.GetRID(), new(0, new Vector2(0, 6)));
            PhysicsServer.BodySetTransform(obstacle, new(0, new Vector2(4, 0))); PhysicsServer.BodySetSpace(obstacle, world.Space);
            PhysicsServer.AreaAddShape(area, disk.GetRID(), disabled: true);
            PhysicsServer.AreaAddShape(area, disk.GetRID(), new(0, new Vector2(0, -6)));
            PhysicsServer.AreaAddShape(area, disk.GetRID(), new(0, new Vector2(0, 6)));
            PhysicsServer.AreaSetSpace(area, world.Space);
            mover.Inertia = 0; mover.LockRotation = false;
            using var direct = PhysicsServer.BodyGetDirectState(mover.GetRID())!;
            Check(MathF.Abs(direct.InverseInertia - 1f / 76) < .00001f && direct.CenterOfMassLocal.Length() < .00001f,
                "Compound automatic mass uses both circles and the parallel-axis contribution");
            mover.Inertia = 4; mover.LockRotation = true;
            using var point = new PhysicsPointQueryParameters { Position = new(0, -6), CollideWithAreas = true };
            var hits = new PhysicsPointResult[8];
            var pointCount = world.DirectSpaceState.IntersectPoint(point, hits); var sawRawArea = false; var sawBody = false;
            for (var i = 0; i < pointCount; i++)
            {
                Check(hits[i].ShapeIndex == 1, "Each overlapping scene/raw Area/body publishes its real nonzero slot");
                sawRawArea |= hits[i].ColliderRID == area; sawBody |= hits[i].ColliderRID == mover.GetRID();
            }
            Check(sawRawArea && sawBody, "Server Area and scene body expose their actual nonzero slots");
            using var motion = new PhysicsTestMotionParameters { From = new(0, new Vector2(-10, 0)), Motion = new(20, 0), Margin = 0 };
            using var result = new PhysicsTestMotionResult();
            Motion();
            Solve(); Verify(); Check(entered == 2, "Scene Area enters both nonzero compound pairs");
            using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space);
            var expected = mover.GlobalTransform; var expectedVelocity = mover.LinearVelocity;
            mover.Position = new(-50, 0); tree.PhysicsFrame(1d / 60);
            Check(direct.GetContactCount() == 0, "A discarded compound future retires its completed contact rows");
            Check(exited == 2, "The discarded future exits both compound logical pairs");
            var deliveredEntered = entered; var deliveredExited = exited;
            checkpoint.Restore(); Verify();
            Check(entered == deliveredEntered && exited == deliveredExited, "Private compound restore publishes no duplicate Area gameplay effects");
            Check(mover.GlobalTransform.Origin.DistanceTo(expected.Origin) < .001f && mover.LinearVelocity.DistanceTo(expectedVelocity) < .001f,
                "Compound private history restores physical motion and distinct slot observations");
            if (solver is not null)
            {
                using var owned = new OwnedState(solver, mover.GetRID());
                Check(owned.GetContactCount() == 2 && owned.GetContactLocalShape(0) != owned.GetContactLocalShape(1), "Independent private history retains both local features");
            }
            for (var i = 0; i < 128; i++) Cycle(); Collect();
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Cycle();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Compound full-frame/query/capture/restore cycles allocated {own}/{all} B");
            Check(entered == deliveredEntered && exited == deliveredExited, "Warm replay retains confirmed compound Area effects without duplicate callbacks");
            foreach (var mode in new[] { CCDMode.Disabled, CCDMode.CastRay, CCDMode.CastShape })
            {
                PhysicsServer.BodySetTransform(obstacle, new(0, new Vector2(40, 0)));
                mover.Position = Vector2.Zero; mover.LinearVelocity = new(1200, 0); mover.ContinuousCD = mode;
                tree.PhysicsFrame(.05);
                if (mode == CCDMode.Disabled) Check(mover.Position.X > 50, "Discrete compound control crosses the wall in the chosen interval");
                else Check(MathF.Abs(mover.Position.X - 36) < .1f && MathF.Abs(mover.LinearVelocity.X) < .1f,
                    $"Compound {mode} stops at the real transformed slot surface, x={mover.Position.X}, velocity={mover.LinearVelocity}");
            }
            sensor.CollisionMask = 0;
            mover.ContinuousCD = CCDMode.Disabled; mover.Position = Vector2.Zero; mover.LinearVelocity = Vector2.Zero;
            PhysicsServer.BodySetTransform(obstacle, Transform.Identity);
            PhysicsServer.BodySetShapeTransform(obstacle, 1, new(0, new Vector2(3, -6)));
            PhysicsServer.BodySetShapeTransform(obstacle, 2, new(0, new Vector2(2, 6)));
            PhysicsServer.BodySetMode(mover.GetRID(), PhysicsServer.BodyMode.Kinematic); mover.MaxContactsReported = 1;
            tree.PhysicsFrame(1d / 60);
            Check(direct.GetContactCount() == 1 && direct.GetContactLocalShape(0) == 2 && direct.GetContactColliderShape(0) == 2,
                "The deepest cap selects the deeper feature even when both rows share one peer");
            mover.MaxContactsReported = 8; PhysicsServer.BodySetContactsReportedDepthThreshold(mover.GetRID(), 1.5f);
            tree.PhysicsFrame(1d / 60);
            Check(direct.GetContactCount() == 1 && direct.GetContactLocalShape(0) == 2, "Compound depth policy filters individual same-peer feature rows");
            PhysicsServer.BodySetContactsReportedDepthThreshold(mover.GetRID(), 0); mover.MaxContactsReported = 0;
            tree.PhysicsFrame(1d / 60); Check(direct.GetContactCount() == 0, "Zero compound contact capacity clears publication");
            Console.WriteLine($"Compound response {backend?.ToString() ?? "registered"}: scene/raw/Area slots, momentum, private history and 128 cycles {own}/{all} B passed.");
            void Solve() { mover.Position = Vector2.Zero; mover.LinearVelocity = new(120, 0); tree.PhysicsFrame(1d / 60); }
            void Cycle() { checkpoint.Restore(); Solve(); Verify(); checkpoint.Capture(); _ = world.DirectSpaceState.IntersectPoint(point, hits); Motion(); }
            void Motion()
            {
                var before = mover.GlobalTransform;
                Check(PhysicsServer.BodyTestMotion(mover.GetRID(), motion, result) && result.GetColliderRID() == obstacle &&
                    result.GetCollisionLocalShape() is 1 or 2 && result.GetColliderShape() == result.GetCollisionLocalShape() &&
                    MathF.Abs(result.GetTravel().X - 10) < .1f && mover.GlobalTransform == before,
                    "Compound body-motion tests preserve real local/peer slots and leave simulation pose unchanged");
            }
            void Verify()
            {
                var count = direct.GetContactCount(); if (count != 2) throw new InvalidOperationException($"Both compound pairs remain in the completed frame: {count}");
                var top = false; var bottom = false; var sum = Vector2.Zero;
                for (var i = 0; i < count; i++)
                {
                    Check(direct.GetContactCollider(i) == obstacle, "Compound contact owns the actual peer");
                    var local = direct.GetContactLocalShape(i); var remote = direct.GetContactColliderShape(i);
                    if (local == 1 && remote == 1) top = true;
                    else if (local == 2 && remote == 2) bottom = true;
                    else throw new InvalidOperationException($"Compound contact lost feature identity {local}/{remote}");
                    sum += direct.GetContactImpulse(i);
                }
                Check(top && bottom && sum.DistanceTo((mover.LinearVelocity - new Vector2(120, 0)) * 2) < .05f,
                    "Distinct compound contact impulses sum to the physical momentum change");
                Check(MathF.Abs(mover.LinearVelocity.X) < .05f && mover.Position.X < 1, "The compound wall stops real scene motion");
            }
        }
        finally { PhysicsServer.FreeRID(area); PhysicsServer.FreeRID(obstacle); root.RemoveChild(sensor); sensor.Dispose(); root.RemoveChild(mover); mover.Dispose(); }
    }
}
