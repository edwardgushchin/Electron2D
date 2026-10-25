using Electron2D;

internal static class BodyFrameChecks
{
    internal static void Run(PhysicsServer.Backend backend, bool threaded)
    {
        _ = GC.GetAllocatedBytesForCurrentThread(); _ = GC.GetTotalAllocatedBytes(true);
        using var world = new World(backend);
        using var root = new SubViewport { World = world };
        using var circle = new CircleShape { Radius = 5 };
        using var box = new RectangleShape { Size = new(200, 2) };
        var scene = new RigidBody { Mass = 2, GravityScale = 0, CanSleep = false, MaxContactsReported = 4 };
        scene.AddChild(new CollisionShape { Shape = circle }); root.AddChild(scene);
        var floor = new StaticBody { Position = new(0, 10) };
        floor.AddChild(new CollisionShape { Shape = box }); root.AddChild(floor);
        using var tree = new SceneTree(root);
        var raw = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.BodyAddShape(raw, circle.GetRID()); PhysicsServer.BodySetMass(raw, 3);
            PhysicsServer.BodySetGravityScale(raw, 0); PhysicsServer.BodySetCanSleep(raw, false);
            PhysicsServer.BodySetMaxContactsReported(raw, 4); PhysicsServer.BodySetSpace(raw, world.Space);
            RID[] ids = [scene.GetRID(), raw];
            using var sceneState = new ControlledState(ids[0]); using var rawState = new ControlledState(ids[1]);
            ControlledState[] states = [sceneState, rawState];
            var before = new Vector2[2]; var callbacks = 0; var frames = 0; var contactReads = 0;
            var owner = Thread.CurrentThread;
            foreach (var id in ids)
            {
                PhysicsServer.BodySetLinearDampMode(id, RigidBody.DampMode.Replace); PhysicsServer.BodySetLinearDamp(id, 0);
                PhysicsServer.BodySetAngularDampMode(id, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDamp(id, 0);
                PhysicsServer.BodySetForceIntegrationCallback(id, Observe); PhysicsServer.BodySetStateSyncCallback(id, Observe);
            }
            Check(world.PhysicsBackend == backend && world.PhysicsFallbackReason is null, "Full frames use the requested numerical owner without fallback.");
            for (var i = 0; i < 128; i++) Frame();
            AllocationWarmupCheck.Collect();
            var startCallbacks = callbacks; var startContacts = contactReads;
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0 && callbacks - startCallbacks == 512 && contactReads - startContacts == 256,
                $"Body extension full frames allocated {own}/{all} B or lost callbacks/contacts: {callbacks - startCallbacks}/{contactReads - startContacts}.");
            Check(sceneState.Seen == (1UL << 38) - 1 && rawState.Seen == (1UL << 38) - 1,
                "Each scene/raw extension executes every required hook in active full frames.");
            Console.WriteLine($"Body extension {backend}/{(threaded ? "separate" : "owner")}: 128 changed complete frames, 512 owner callbacks, 256 full contacts, all 38 hooks per body, {own}/{all} owner/all-thread B passed.");

            void Observe(PhysicsDirectBodyState state)
            {
                Check(Thread.CurrentThread == owner && state.GetContactCount() > 0 && ReferenceEquals(state.GetSpaceState(), world.DirectSpaceState),
                    "Full-frame callbacks run on the owner with completed contacts and the current world.");
                callbacks++;
            }
            void Frame()
            {
                var index = frames++;
                for (var i = 0; i < states.Length; i++)
                {
                    var state = states[i];
                    state.Transform = new(.02f * (index & 3), new(i * 40 - 20 + .25f * (index & 1), 5));
                    state.LinearVelocity = new(i == 0 ? 5 : -5, 8); state.AngularVelocity = i == 0 ? .2f : -.2f;
                    state.Sleeping = false; state.CollisionLayer = 1u | ((uint)(index & 3) << 29); state.CollisionMask = uint.MaxValue;
                    state.SetConstantForce(new(3, 2)); state.AddConstantCentralForce(new(1, -1));
                    state.SetConstantTorque(.3f); state.AddConstantTorque(.1f); state.AddConstantForce(new(0, 2), Vector2.Right);
                    state.ApplyCentralForce(new(1, 2)); state.ApplyForce(new(2, 1), new(0, .5f)); state.ApplyTorque(.2f);
                    state.ApplyCentralImpulse(new(.25f, 0)); state.ApplyImpulse(new(0, .3f), Vector2.Right); state.ApplyTorqueImpulse(.1f);
                    state.IntegrateForces(); before[i] = state.LinearVelocity;
                    Check(state.GetConstantForce() == new Vector2(4, 3) && MathF.Abs(state.GetConstantTorque() - 2.4f) < .0001f &&
                        state.CenterOfMass.Length() < .0001f && state.CenterOfMassLocal.Length() < .0001f && state.InverseInertia > 0 &&
                        state.TotalGravity == Vector2.Zero && state.TotalLinearDamp == 0 && state.TotalAngularDamp == 0 && !state.Sleeping &&
                        state.CollisionLayer == (1u | ((uint)(index & 3) << 29)) && state.CollisionMask == uint.MaxValue,
                        "Complete extension state retains changed authoring and physical mass/field values.");
                }
                var tick = PhysicsServer.SpaceGetTick(world.Space); tree.PhysicsFrame(.01);
                Check(PhysicsServer.SpaceGetTick(world.Space) == tick + 1, "Each changed frame advances exactly one tick.");
                for (var i = 0; i < states.Length; i++)
                {
                    var state = states[i];
                    Check(state.CaptureContact(), "Every active frame refreshes a real scene/raw contact.");
                    var contact = state.CapturedContact;
                    Check(state.GetContactCount() == 1 && state.GetContactCollider(0) == floor.GetRID() &&
                        state.GetContactColliderID(0) == floor.InstanceID && state.GetContactLocalShape(0) == 0 && state.GetContactColliderShape(0) == 0 &&
                        state.GetContactLocalPosition(0) == contact.LocalPosition && state.GetContactColliderPosition(0) == contact.ColliderPosition &&
                        state.GetContactLocalNormal(0) == contact.Normal && state.GetContactLocalVelocityAtPosition(0) == contact.LocalVelocity &&
                        state.GetContactColliderVelocityAtPosition(0) == contact.ColliderVelocity && state.GetContactImpulse(0) == contact.Impulse &&
                        ReferenceEquals(state.GetContactColliderObject(0), floor) && ReferenceEquals(state.GetContactColliderObject<Node>(0), floor),
                        "All contact getters preserve the refreshed complete immutable sample.");
                    var mass = 1 / state.InverseMass;
                    var residual = (state.LinearVelocity - before[i]) * mass - new Vector2(7, 6) * .01f - contact.Impulse;
                    Check(residual.Length() < .02f && contact.Impulse.Y < -1 && state.Transform.Origin.X > i * 40 - 21 &&
                        MathF.Abs(state.AngularVelocity) > .001f && MathF.Abs(state.Step - .01f) < .000001f &&
                        state.GetVelocityAtLocalPosition(Vector2.One).IsFinite() && ReferenceEquals(state.GetSpaceState(), world.DirectSpaceState),
                        "Complete frames preserve force/contact momentum within .02 scene-unit impulse and live rotating state.");
                    contactReads++;
                }
            }
        }
        finally { PhysicsServer.FreeRID(raw); }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
