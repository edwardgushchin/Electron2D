using Electron2D;

public static partial class BackendChecks
{
    private static void AxisVelocityInputs(Solver? solver, World world, SceneTree tree, PhysicsBody[] nodes, CircleShape circle, bool threaded)
    {
        var raw = new RID[4]; var owner = Thread.CurrentThread; var callbacks = 0;
        try
        {
            for (var i = 0; i < raw.Length; i++)
            {
                raw[i] = PhysicsServer.BodyCreate(); PhysicsServer.BodyAddShape(raw[i], circle.GetRID(), disabled: true);
                PhysicsServer.BodyAddShape(raw[i], circle.GetRID()); PhysicsServer.BodySetSpace(raw[i], world.Space);
            }
            RID[] ids = [.. nodes.Select(n => n.GetRID()), .. raw];
            Vector2[] axes = [new(5, 0), new(0, -7), new(3, 4), new(-3, 4), Vector2.Zero, new(float.Epsilon, 0)];
            Vector2[] expected = [new(5, -2), new(10, -7), new(10.36f, -1.52f), new(2.44f, 8.08f), new(10, -2), new(float.Epsilon, -2)];
            foreach (var id in ids)
                foreach (var mode in new[] { PhysicsServer.BodyMode.Static, PhysicsServer.BodyMode.Kinematic, PhysicsServer.BodyMode.Rigid, PhysicsServer.BodyMode.RigidLinear })
                {
                    PhysicsServer.BodySetMode(id, mode);
                    for (var n = 0; n < axes.Length; n++)
                    {
                        PhysicsServer.BodySetLinearVelocity(id, new(10, -2)); PhysicsServer.BodySetAngularVelocity(id, .2f);
                        var angular = PhysicsServer.BodyGetAngularVelocity(id);
                        PhysicsServer.BodySetAxisVelocity(id, axes[n]);
                        Check(PhysicsServer.BodyGetLinearVelocity(id).DistanceTo(expected[n]) < .0001f, "All scene/raw roles retain signed oblique axis projection");
                        Check(Math.Abs(PhysicsServer.BodyGetAngularVelocity(id) - angular) < .0001f,
                            "Every axis operation preserves the unrelated angular channel");
                    }
                }
            var area = PhysicsServer.AreaCreate(); var joint = PhysicsServer.JointCreate(); var stale = PhysicsServer.BodyCreate(); PhysicsServer.FreeRID(stale);
            try
            {
                foreach (var id in new[] { default, area, joint, circle.GetRID(), world.Space, stale })
                    Reject<ArgumentException>(() => PhysicsServer.BodySetAxisVelocity(id, Vector2.Zero));
            }
            finally { PhysicsServer.FreeRID(area); PhysicsServer.FreeRID(joint); }
            var status = 0;
            if (solver is not null) solver.StatusHook = () => status++;
            foreach (var id in ids)
                foreach (var invalid in new[] { new Vector2(float.NaN, 0), new Vector2(0, float.PositiveInfinity), new Vector2(float.NegativeInfinity, 0) })
                    Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodySetAxisVelocity(id, invalid));
            if (solver is not null) { solver.StatusHook = null; Check(status == 0, "Nonfinite axes reject before selected status or motion preparation"); }
            Task.Run(() =>
            {
                foreach (var id in ids) Reject<InvalidOperationException>(() => PhysicsServer.BodySetAxisVelocity(id, Vector2.Zero));
                Reject<InvalidOperationException>(() => ((RigidBody)nodes[3]).SetAxisVelocity(Vector2.Zero));
            }).GetAwaiter().GetResult();
            for (var i = 0; i < ids.Length; i++)
            {
                var id = ids[i]; PhysicsServer.BodySetMode(id, PhysicsServer.BodyMode.Rigid);
                PhysicsServer.BodySetMass(id, 1); PhysicsServer.BodySetInertia(id, 4); PhysicsServer.BodySetGravityScale(id, 0);
                PhysicsServer.BodySetCollisionMask(id, 0); PhysicsServer.BodySetCanSleep(id, false);
                PhysicsServer.BodySetLinearDamp(id, 0); PhysicsServer.BodySetAngularDamp(id, 0);
                PhysicsServer.BodySetLinearDampMode(id, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDampMode(id, RigidBody.DampMode.Replace);
                PhysicsServer.BodySetStateSyncCallback(id, _ => { Check(Thread.CurrentThread == owner, "Axis frames publish callbacks on the owner"); callbacks++; });
            }
            var inspect = true;
            if (solver is not null) solver.StepHook = () =>
            {
                Check((Thread.CurrentThread != owner) == threaded, "Axis checks exercise the actual numerical thread");
                if (!inspect) return;
                foreach (var id in ids) Reject<InvalidOperationException>(() => PhysicsServer.BodySetAxisVelocity(id, Vector2.Zero));
                Reject<InvalidOperationException>(() => ((RigidBody)nodes[3]).SetAxisVelocity(Vector2.Zero));
            };
            Frame(); inspect = false;
            for (var i = 0; i < 64; i++) Frame(); Collect();
            var start = callbacks; var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0 && callbacks - start == 1152, $"Axis frames allocated {own}/{all} B or lost callbacks: {callbacks - start}");
            Console.WriteLine($"Axis full frames/{threaded}: nine scene/raw owners, 128 edited impulse/projection frames, 1152 owner callbacks, 0/0 B passed.");
            if (solver is not null)
            {
                var attempts = 0;
                solver.StepHook = () => { Check((Thread.CurrentThread != owner) == threaded, "Terminal axis guard uses the actual numerical lane"); attempts++; };
                solver.FailSolve = true; Reject<AggregateException>(() => tree.PhysicsFrame(.01));
                foreach (var id in ids)
                {
                    Reject<InvalidOperationException>(() => PhysicsServer.BodySetAxisVelocity(id, Vector2.Zero));
                    Reject<InvalidOperationException>(() => PhysicsServer.BodySetAxisVelocity(id, new(5, 0)));
                }
                foreach (var node in nodes.OfType<RigidBody>()) Reject<InvalidOperationException>(() => node.SetAxisVelocity(Vector2.Zero));
                Reject<AggregateException>(() => tree.PhysicsFrame(.01));
                Check(attempts == 1, "Terminal axis writes and a later frame never replay the failed Solve");
                Console.WriteLine("Axis terminal guard: 20 scene/raw zero/changed writes rejected without Solve replay.");
            }
            void Frame()
            {
                for (var i = 0; i < ids.Length; i++)
                {
                    var id = ids[i]; PhysicsServer.BodySetTransform(id, new(0, new Vector2(i * 80, 0)));
                    PhysicsServer.BodySetLinearVelocity(id, new(3, 4)); PhysicsServer.BodySetAngularVelocity(id, .2f);
                    PhysicsServer.BodyApplyCentralImpulse(id, new(0, 6));
                    if (i is 3 or 4) ((RigidBody)nodes[i]).SetAxisVelocity(new(5, 0)); else PhysicsServer.BodySetAxisVelocity(id, new(5, 0));
                }
                tree.PhysicsFrame(.01);
                foreach (var id in ids)
                    Check(PhysicsServer.BodyGetLinearVelocity(id).DistanceTo(new(5, 10)) < .0001f &&
                        Math.Abs(PhysicsServer.BodyGetAngularVelocity(id) - .2f) < .0001f, "Complete axis frames retain both current linear and angular response");
            }
        }
        finally
        {
            if (solver is not null) solver.StatusHook = solver.StepHook = null;
            foreach (var id in raw) if (id.IsValid()) PhysicsServer.FreeRID(id);
        }
    }
}
