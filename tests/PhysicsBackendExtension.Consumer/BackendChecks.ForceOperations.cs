using Electron2D;

public static partial class BackendChecks
{
    private static Action[] ForceCalls(RID body, PhysicsDirectBodyState? state = null) =>
    [
        () => { if (state is null) PhysicsServer.BodyApplyCentralForce(body, Vector2.One); else state.ApplyCentralForce(Vector2.One); },
        () => { if (state is null) PhysicsServer.BodyApplyForce(body, Vector2.One, Vector2.One); else state.ApplyForce(Vector2.One, Vector2.One); },
        () => { if (state is null) PhysicsServer.BodyApplyTorque(body, 1); else state.ApplyTorque(1); },
        () => { if (state is null) PhysicsServer.BodyApplyCentralImpulse(body, Vector2.One); else state.ApplyCentralImpulse(Vector2.One); },
        () => { if (state is null) PhysicsServer.BodyApplyImpulse(body, Vector2.One, Vector2.One); else state.ApplyImpulse(Vector2.One, Vector2.One); },
        () => { if (state is null) PhysicsServer.BodyApplyTorqueImpulse(body, 1); else state.ApplyTorqueImpulse(1); },
        () => { if (state is null) PhysicsServer.BodyAddConstantCentralForce(body, Vector2.One); else state.AddConstantCentralForce(Vector2.One); },
        () => { if (state is null) PhysicsServer.BodyAddConstantForce(body, Vector2.One, Vector2.One); else state.AddConstantForce(Vector2.One, Vector2.One); },
        () => { if (state is null) PhysicsServer.BodyAddConstantTorque(body, 1); else state.AddConstantTorque(1); },
        () => { _ = state is null ? PhysicsServer.BodyGetConstantForce(body) : state.GetConstantForce(); },
        () => { _ = state is null ? PhysicsServer.BodyGetConstantTorque(body) : state.GetConstantTorque(); },
        () => { if (state is null) PhysicsServer.BodySetConstantForce(body, Vector2.Zero); else state.SetConstantForce(Vector2.Zero); },
        () => { if (state is null) PhysicsServer.BodySetConstantTorque(body, 0); else state.SetConstantTorque(0); }
    ];

    private static void ForceOperationChecks(PhysicsServer.Backend? backend, bool threaded)
    {
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("ForceOperations", () => solver = new());
        using var world = backend is { } selected ? new World(selected) : new World("ForceOperations");
        using var root = new SubViewport { World = world }; using var circle = new CircleShape { Radius = 2 };
        PhysicsBody[] nodes = [new StaticBody(), new AnimatableBody(), new CharacterBody(), new RigidBody(), new PhysicalBone()];
        for (var i = 0; i < nodes.Length; i++)
        {
            nodes[i].Position = new(i * 80, 0); nodes[i].AddChild(new CollisionShape { Shape = circle, Disabled = true });
            nodes[i].AddChild(new CollisionShape { Shape = circle }); root.AddChild(nodes[i]);
        }
        using var tree = new SceneTree(root); var raw = PhysicsServer.BodyCreate();
        RID[] ids = [.. nodes.Select(n => n.GetRID()), raw]; var states = new PhysicsDirectBodyState[ids.Length];
        var owner = Thread.CurrentThread; var mode = PhysicsServer.BodyMode.Static; var frames = 0; var callbacks = 0; var queries = 0;
        using var point = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1];
        try
        {
            PhysicsServer.BodyAddShape(raw, circle.GetRID(), disabled: true); PhysicsServer.BodyAddShape(raw, circle.GetRID()); PhysicsServer.BodySetSpace(raw, world.Space);
            for (var i = 0; i < ids.Length; i++)
            {
                var id = ids[i]; PhysicsServer.BodySetMode(id, PhysicsServer.BodyMode.Rigid); PhysicsServer.BodySetMass(id, 2); PhysicsServer.BodySetInertia(id, 8);
                PhysicsServer.BodySetCenterOfMass(id, new(2, -1)); PhysicsServer.BodySetGravityScale(id, 0); PhysicsServer.BodySetCanSleep(id, false); PhysicsServer.BodySetCollisionMask(id, 0);
                PhysicsServer.BodySetLinearDampMode(id, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDampMode(id, RigidBody.DampMode.Replace);
                PhysicsServer.BodySetLinearDamp(id, 0); PhysicsServer.BodySetAngularDamp(id, 0); states[i] = PhysicsServer.BodyGetDirectState(id)!;
            }
            using var area = new Area(); var stale = PhysicsServer.BodyCreate(); PhysicsServer.BodySetSpace(stale, world.Space);
            var staleView = PhysicsServer.BodyGetDirectState(stale)!; PhysicsServer.FreeRID(stale);
            foreach (var action in ForceCalls(stale, staleView)) Reject<InvalidOperationException>(action);
            foreach (var bad in new[] { area.GetRID(), circle.GetRID(), stale, default })
                foreach (var action in ForceCalls(bad)) Reject<ArgumentException>(action);
            var calls = ids.SelectMany((id, i) => ForceCalls(id).Concat(ForceCalls(id, states[i]))).ToArray();
            Task.Run(() => { foreach (var action in calls) Reject<InvalidOperationException>(action); }).GetAwaiter().GetResult();
            if (solver is not null)
            {
                solver.StepHook = () =>
                {
                    Check((Thread.CurrentThread != owner) == threaded, "Force operations use the actual captured numerical thread");
                    foreach (var id in ids)
                    {
                        var family = ForceCalls(id);
                        for (var i = 0; i < family.Length; i++) { if (i is 9 or 10) family[i](); else Reject<InvalidOperationException>(family[i]); }
                        foreach (var action in ForceCalls(id, states[Array.IndexOf(ids, id)])) Reject<InvalidOperationException>(action);
                    }
                    foreach (var rigid in nodes.OfType<RigidBody>())
                    {
                        Reject<InvalidOperationException>(() => rigid.ConstantForce = rigid.ConstantForce);
                        Reject<InvalidOperationException>(() => rigid.ConstantTorque = rigid.ConstantTorque);
                    }
                };
                tree.PhysicsFrame(.01);
                solver.StepHook = () => Check((Thread.CurrentThread != owner) == threaded, "Warm force operations retain the captured numerical thread");
            }
            for (var i = 0; i < ids.Length; i++)
            {
                var index = i; PhysicsServer.BodySetStateSyncCallback(ids[i], state =>
                {
                    Check(Thread.CurrentThread == owner && PhysicsServer.SpaceIsFlushingQueries(world.Space), "Force operation observers retain owner and notification phase");
                    var dynamic = mode is PhysicsServer.BodyMode.Rigid or PhysicsServer.BodyMode.RigidLinear;
                    var expected = dynamic ? new Vector2(7.14f, 2.01f) : new Vector2(3, 1);
                    if (mode == PhysicsServer.BodyMode.Kinematic) expected += (new Vector2(2, -1).Rotated(MathF.PI / 2 + .002f) - new Vector2(2, -1).Rotated(MathF.PI / 2)) / .01f;
                    var angular = mode == PhysicsServer.BodyMode.Rigid ? 3.695f : mode == PhysicsServer.BodyMode.RigidLinear ? 0 : .2f;
                    var tolerance = mode == PhysicsServer.BodyMode.Kinematic ? .0002f + 4 * (MathF.BitIncrement(index * 80f) - index * 80f) / .01f : .0002f;
                    if (!(state.LinearVelocity.DistanceTo(expected) < tolerance && MathF.Abs(state.AngularVelocity - angular) < .0002f))
                        throw new InvalidOperationException($"Force frame {frames}/{mode}, owner {index}: motion {state.LinearVelocity}/{state.AngularVelocity}, expected {expected}/{angular}, tolerance {tolerance}.");
                    point.Position = state.Transform.Origin;
                    Check(world.DirectSpaceState.IntersectPoint(point, hits) == 1 && hits[0].ColliderRID == ids[index] && hits[0].ShapeIndex == 1, "Force frames retain exact collider and logical shape identity"); callbacks++; queries++;
                });
            }
            for (var i = 0; i < 64; i++) Frame(); Collect();
            var beforeCallbacks = callbacks; var beforeQueries = queries; var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0 && callbacks - beforeCallbacks == 576 && queries - beforeQueries == 576, "Complete force operation frames retain every observer/query and zero owner/all-thread managed bytes");
            Console.WriteLine($"Force operations {backend?.ToString() ?? "registered"}/{threaded}: 13 operations, 6 owners, 4 roles, 128 changed full frames, 576 callbacks, 576 queries 0/0 B passed.");

            void Frame()
            {
                var index = frames++; mode = (PhysicsServer.BodyMode)(index & 3); var route = index / 4 % 3;
                for (var i = 0; i < ids.Length; i++)
                {
                    var id = ids[i]; var state = states[i]; PhysicsServer.BodySetMode(id, mode);
                    PhysicsServer.BodySetTransform(id, new(MathF.PI / 2, new Vector2(i * 80, 0)));
                    PhysicsServer.BodySetLinearVelocity(id, mode == PhysicsServer.BodyMode.Kinematic ? Vector2.Zero : new(3, 1));
                    PhysicsServer.BodySetAngularVelocity(id, mode is PhysicsServer.BodyMode.Kinematic or PhysicsServer.BodyMode.RigidLinear ? 0 : .2f);
                    Act(id, state, i < nodes.Length ? nodes[i] as RigidBody : null, route);
                    Check(PhysicsServer.BodyGetConstantForce(id) == new Vector2(12, 6) && MathF.Abs(PhysicsServer.BodyGetConstantTorque(id) - 4) < .00001f, "Every force route retains the complete configured totals");
                    if (mode == PhysicsServer.BodyMode.Kinematic) PhysicsServer.BodySetTransform(id, new(MathF.PI / 2 + .002f, new Vector2(i * 80 + .03f, .01f)));
                }
                var tick = PhysicsServer.SpaceGetTick(world.Space); tree.PhysicsFrame(.01); Check(PhysicsServer.SpaceGetTick(world.Space) == tick + 1, "Force operation frames advance exactly one tick");
            }
        }
        finally { if (solver is not null) solver.StepHook = null; PhysicsServer.FreeRID(raw); }

        static void Act(RID id, PhysicsDirectBodyState state, RigidBody? scene, int route)
        {
            var position = PhysicsServer.BodyGetTransform(id).BasisXform(new Vector2(2, -1)) + new Vector2(1, 2);
            if (route == 2 && scene is not null)
            {
                scene.ConstantForce = new(6, 8); scene.ConstantTorque = 12; scene.AddConstantCentralForce(new(2, -4)); scene.AddConstantForce(new(4, 2), position); scene.AddConstantTorque(-2);
                scene.ApplyCentralImpulse(new(2, 4)); scene.ApplyImpulse(new(6, -2), new(3, -4)); scene.ApplyTorqueImpulse(-4);
                scene.ApplyCentralForce(new(4, -8)); scene.ApplyForce(new(12, 4), new(-2, 3)); scene.ApplyTorque(16);
            }
            else if (route == 1)
            {
                state.SetConstantForce(new(6, 8)); state.SetConstantTorque(12); state.AddConstantCentralForce(new(2, -4)); state.AddConstantForce(new(4, 2), position); state.AddConstantTorque(-2);
                state.ApplyCentralImpulse(new(2, 4)); state.ApplyImpulse(new(6, -2), new(3, -4)); state.ApplyTorqueImpulse(-4);
                state.ApplyCentralForce(new(4, -8)); state.ApplyForce(new(12, 4), new(-2, 3)); state.ApplyTorque(16);
            }
            else
            {
                PhysicsServer.BodySetConstantForce(id, new(6, 8)); PhysicsServer.BodySetConstantTorque(id, 12); PhysicsServer.BodyAddConstantCentralForce(id, new(2, -4)); PhysicsServer.BodyAddConstantForce(id, new(4, 2), position); PhysicsServer.BodyAddConstantTorque(id, -2);
                PhysicsServer.BodyApplyCentralImpulse(id, new(2, 4)); PhysicsServer.BodyApplyImpulse(id, new(6, -2), new(3, -4)); PhysicsServer.BodyApplyTorqueImpulse(id, -4);
                PhysicsServer.BodyApplyCentralForce(id, new(4, -8)); PhysicsServer.BodyApplyForce(id, new(12, 4), new(-2, 3)); PhysicsServer.BodyApplyTorque(id, 16);
            }
        }
    }
}
