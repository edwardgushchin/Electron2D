using Electron2D;

internal static partial class PhysicsBackendOwnershipTests
{
    private static void SelectedViews(PhysicsServer.Backend backend)
    {
        ViewBackend selected = null!;
        World? creatingWorld = null;
        using var world = new World(new WorldRuntime(false, backend, factory: space =>
        {
            Reject<InvalidOperationException>(creatingWorld!.Dispose);
            Reject<InvalidOperationException>(() => _ = creatingWorld.Space);
            var recursive = Task.Run(() => Reject<InvalidOperationException>(() => _ = creatingWorld.Space));
            Check(recursive.Wait(TimeSpan.FromSeconds(5)), "World factory runs outside its runtime lock and rejects concurrent recursive creation");
            selected = new(space, backend);
            selected.SpaceFactory = () =>
            {
                var read = Task.Run(() => PhysicsServer.SpaceGetBackend(space.RID));
                Check(read.Wait(TimeSpan.FromSeconds(5)) && read.Result == backend, "View factory runs outside the shared registry lock");
                Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(space.RID));
                Reject<InvalidOperationException>(() => space.Step(1d / 60));
                return new InitializationQueries(space.RID, new PhysicsDirectSpaceState(space.RID));
            };
            selected.BodyFactory = runtime =>
            {
                Check(runtime.View is null || runtime.View.IsDisposed, "Body factory sees unpublished or disposed cache");
                Reject<InvalidOperationException>(() => PhysicsServer.BodyGetDirectState(runtime.RID));
                if (runtime.Owners.Server is not null)
                    Reject<InvalidOperationException>(() => PhysicsServer.BodySetSpace(runtime.RID, default));
                else Reject<InvalidOperationException>(() => runtime.Owners.Scene!.Parent!.RemoveChild(runtime.Owners.Scene!));
                Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(runtime.RID));
                return new FactoryBodyState(runtime.RID);
            };
            return selected;
        }));
        creatingWorld = world;
        var space = PhysicsServer.Service.GetSceneSpace(world.Space);
        var query = world.DirectSpaceState;
        EmptyQueries(query);
        Check(query is InitializationQueries && ReferenceEquals(query, world.DirectSpaceState) && selected.SpaceCreations == 1,
            "World and server consumers retain the selected direct-space view");
        using var circle = new CircleShape { Radius = 3 };
        using var box = new RectangleShape { Size = new(200, 4) };
        using var sensorShape = new CircleShape { Radius = 100 };
        using var root = new SubViewport { World = world };
        var scene = new FactoryRigid { CanSleep = false, MaxContactsReported = 8, Position = new(-20, 0), Mass = 2 };
        var floor = new StaticBody { Position = new(0, 20) };
        var sensor = new Area { Monitoring = true };
        scene.AddChild(new CollisionShape { Shape = circle }); floor.AddChild(new CollisionShape { Shape = box });
        sensor.AddChild(new CollisionShape { Shape = sensorShape });
        root.AddChild(scene); root.AddChild(floor); root.AddChild(sensor);
        using var tree = new SceneTree(root);
        var raw = PhysicsServer.BodyCreate(); var foreign = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.BodyAddShape(raw, circle.GetRID()); PhysicsServer.BodySetMass(raw, 2);
            PhysicsServer.BodySetCanSleep(raw, false); PhysicsServer.BodySetMaxContactsReported(raw, 8);
            scene.ContinuousCD = CCDMode.CastRay;
            PhysicsServer.BodySetContinuousCollisionDetectionMode(raw, CCDMode.CastShape);
            PhysicsServer.BodySetTransform(raw, new(0, new(20, 0))); PhysicsServer.BodySetSpace(raw, world.Space);
            PhysicsServer.BodyAddShape(foreign, circle.GetRID()); PhysicsServer.BodySetSpace(foreign, world.Space);
            var callbacks = 0;
            scene.OnState = state => Observe(state, scene.GetRID());
            PhysicsServer.BodySetForceIntegrationCallback(raw, state => Observe(state, raw));
            PhysicsServer.BodySetStateSyncCallback(raw, state => Observe(state, raw));
            void Observe(PhysicsDirectBodyState state, RID body)
            {
                Check(state is FactoryBodyState && ReferenceEquals(state, PhysicsServer.BodyGetDirectState(body)) &&
                    ReferenceEquals(state.GetSpaceState(), query), "Scene integration and raw force/sync callbacks receive selected cached views");
                if (callbacks < 3) Reject<InvalidOperationException>(state.Dispose);
                _ = state.LinearVelocity; callbacks++;
            }
            tree.PhysicsFrame(1d / 60);
            var sceneView = PhysicsServer.BodyGetDirectState(scene.GetRID())!;
            var rawView = PhysicsServer.BodyGetDirectState(raw)!;
            Check(callbacks == 3 && selected.BodyCreations == 2, "Required views are created before solver borrowing exactly once");
            rawView.ApplyCentralImpulse(new(20, 0));
            Check(MathF.Abs(PhysicsServer.BodyGetLinearVelocity(raw).X - 20) < .001f, "Selected body hook changes a real mass-two impulse");
            Check(PhysicsServer.Service.BodyRuntime(raw).Backend.Implementation is WrappedCollider { ImpulseCalls: > 0, PolicyCalls: > 0 },
                "Real wrapped collider executes impulse/contact policy without shared concrete type assumptions");
            rawView.LinearVelocity = new(0, 0);
            for (var i = 0; i < 120; i++) tree.PhysicsFrame(1d / 60);
            Check(sceneView.GetContactCount() > 0 && rawView.GetContactCount() > 0 &&
                sceneView.GetContactCollider(0) == floor.GetRID() && rawView.GetContactCollider(0) == floor.GetRID(),
                "Selected extension views receive actual completed scene/raw contact snapshots");
            Check(sensor.GetOverlappingBodies().Contains(scene), "Wrapped Area geometry executes real overlap monitoring");
            using var associated = new Node();
            PhysicsServer.BodyAttachObject(raw, associated); PhysicsServer.BodyAttachCanvasInstanceID(raw, 777);
            PhysicsServer.BodySetCollisionPriority(raw, 2);
            Check(PhysicsServer.Service.BodyRuntime(raw).Backend.Implementation is WrappedCollider { IdentityCalls: > 1, PriorityCalls: > 0 },
                "Selected identity and priority hooks receive live edits");
            using (var identityPoint = new PhysicsPointQueryParameters { Position = rawView.Transform.Origin, CanvasInstanceID = 777 })
                Check(query.IntersectPoint(identityPoint).Any(hit => hit.ColliderRID == raw && ReferenceEquals(hit.ColliderObject, associated)),
                    "Wrapped collider publishes real canvas/object identities to selected queries");
            PhysicsServer.BodyAttachObject(raw, null); PhysicsServer.BodyAttachCanvasInstanceID(raw, 0);
            using var point = new PhysicsPointQueryParameters { Position = scene.GlobalPosition };
            Check(query.IntersectPoint(point).Any(hit => hit.ColliderRID == scene.GetRID()), "Selected space hooks query real scene geometry");
            Task.Run(() => Reject<InvalidOperationException>(() => PhysicsServer.SpaceGetDirectState(world.Space))).GetAwaiter().GetResult();

            var validSpaceFactory = selected.SpaceFactory;
            query.Dispose();
            selected.SpaceFactory = () => PhysicsServer.SpaceGetDirectState(world.Space);
            Reject<InvalidOperationException>(() => PhysicsServer.SpaceGetDirectState(world.Space));
            selected.SpaceFactory = () => null!;
            Reject<InvalidOperationException>(() => PhysicsServer.SpaceGetDirectState(world.Space));
            using var other = new World(backend);
            var otherView = other.DirectSpaceState;
            selected.SpaceFactory = () => otherView;
            Reject<InvalidOperationException>(() => PhysicsServer.SpaceGetDirectState(world.Space));
            Check(!otherView.IsDisposed, "Rejected foreign view remains caller-owned");
            selected.SpaceFactory = () => query;
            Reject<ObjectDisposedException>(() => PhysicsServer.SpaceGetDirectState(world.Space));
            selected.SpaceFactory = validSpaceFactory;
            query = world.DirectSpaceState;

            var validBodyFactory = selected.BodyFactory;
            rawView.Dispose();
            var foreignView = PhysicsServer.BodyGetDirectState(foreign)!;
            selected.BodyFactory = _ => foreignView;
            Reject<InvalidOperationException>(() => PhysicsServer.BodyGetDirectState(raw));
            Check(!foreignView.IsDisposed && foreignView.Transform.IsFinite(), "Rejected foreign-body view stays valid");
            selected.BodyFactory = _ => null!;
            Reject<InvalidOperationException>(() => PhysicsServer.BodyGetDirectState(raw));
            selected.BodyFactory = _ => rawView;
            Reject<ObjectDisposedException>(() => PhysicsServer.BodyGetDirectState(raw));
            selected.BodyFactory = validBodyFactory;
            rawView = PhysicsServer.BodyGetDirectState(raw)!;
            var facade = PhysicsServer.Service.BodyRuntime(raw).Backend;
            var retired = selected.NativeCollider(facade);
            PhysicsServer.BodySetSpace(raw, default);
            Reject<InvalidOperationException>(() => selected.NativeCollider(facade));
            PhysicsServer.BodySetSpace(raw, world.Space);
            var current = selected.NativeCollider(facade);
            Check(!ReferenceEquals(retired, current), "Reentry owns a fresh native collider independently of the wrapped implementation");
            retired.Detach();
            Check(ReferenceEquals(current, selected.NativeCollider(facade)), "Retiring an old attachment cannot remove the current native ownership entry");
            selected.BodyFactory = _ => rawView;
            Reject<InvalidOperationException>(() => PhysicsServer.BodyGetDirectState(raw));
            Reject<ObjectDisposedException>(() => _ = rawView.LinearVelocity);
            selected.BodyFactory = validBodyFactory;
            rawView = PhysicsServer.BodyGetDirectState(raw)!;
            rawView.Dispose();
            selected.BodyFactory = _ => throw new InvalidOperationException("Expected pre-solver view factory failure");
            var tick = PhysicsServer.SpaceGetTick(world.Space);
            Reject<AggregateException>(() => tree.PhysicsFrame(1d / 60));
            Check(PhysicsServer.SpaceGetTick(world.Space) == tick && !selected.ResultsReady && !space.HasBackendFailure,
                "A failed pre-solver factory publishes no solved tick and releases the interval guard");
            selected.BodyFactory = validBodyFactory;
            rawView = PhysicsServer.BodyGetDirectState(raw)!;
            var laterCalls = 0;
            PhysicsServer.BodySetCanSleep(foreign, false); PhysicsServer.BodySetSleeping(foreign, false);
            PhysicsServer.BodySetForceIntegrationCallback(foreign, _ => laterCalls++);
            scene.OnState = state =>
            {
                Observe(state, scene.GetRID()); rawView.Dispose();
                selected.BodyFactory = runtime => runtime.RID == raw
                    ? throw new InvalidOperationException("Expected late view factory failure") : validBodyFactory!(runtime);
            };
            Reject<AggregateException>(() => tree.PhysicsFrame(1d / 60));
            Check(PhysicsServer.SpaceGetTick(world.Space) == tick + 1 && selected.ResultsReady && laterCalls == 1,
                $"A post-solver view factory failure preserves the solved tick and attempts later body callbacks: tick={PhysicsServer.SpaceGetTick(world.Space)}/{tick + 1}, ready={selected.ResultsReady}, later={laterCalls}");
            selected.BodyFactory = validBodyFactory;
            scene.OnState = state => Observe(state, scene.GetRID());
            PhysicsServer.BodySetForceIntegrationCallback(foreign, null);
            rawView = PhysicsServer.BodyGetDirectState(raw)!;
            rawView.Sleeping = false;
            for (var i = 0; i < 64; i++) tree.PhysicsFrame(1d / 60);
            Collect();
            for (var i = 0; i < 64; i++) tree.PhysicsFrame(1d / 60);
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) tree.PhysicsFrame(1d / 60);
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0, $"Selected view callback/contact dispatch allocated {owner}/{all} bytes");
            using (var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space))
            {
                var before = rawView.Transform;
                checkpoint.Capture(); rawView.Transform = new(0, new(50, 0)); checkpoint.Restore();
                Check(rawView.Transform.Origin.DistanceTo(before.Origin) < .001f, "Wrapped native publication cache restores actual checkpoint state");
            }
            var joint = PhysicsServer.JointCreate();
            try
            {
                var anchor = rawView.Transform.Origin;
                PhysicsServer.JointMakePin(joint, anchor, raw, floor.GetRID());
                var runtime = space.SnapshotJoints.Single(value => value.RID == joint);
                Check(runtime.Backend.Implementation is WrappedJoint { Attachments: 1 }, "Selected joint creation receives immutable parameter values");
                PhysicsServer.JointSetBias(joint, .2f); PhysicsServer.JointSetMaxForce(joint, 1000);
                Check(runtime.Backend.Implementation is WrappedJoint { Policies: > 0, Last.Bias: .2f, Last.MaxForce: 1000 }, "Live selected joint policy receives a complete refreshed value");
                PhysicsServer.JointSetMaxForce(joint, float.MaxValue);
                rawView.ApplyCentralImpulse(new(2, 0));
                for (var i = 0; i < 8; i++) tree.PhysicsFrame(1d / 60);
                Check(rawView.Transform.Origin.DistanceTo(anchor) < .5f, $"Wrapped collider handles bind a real two-body pin constraint: error={rawView.Transform.Origin.DistanceTo(anchor)}, pose={rawView.Transform.Origin}, anchor={anchor}");
                if (backend == PhysicsServer.Backend.CPU)
                {
                    var retiredJoint = selected.NativeJoint(runtime.Backend);
                    PhysicsServer.JointClear(joint);
                    Reject<InvalidOperationException>(() => selected.NativeJoint(runtime.Backend));
                    PhysicsServer.JointMakePin(joint, anchor, raw, floor.GetRID());
                    var currentJoint = selected.NativeJoint(runtime.Backend); retiredJoint.Detach();
                    Check(ReferenceEquals(currentJoint, selected.NativeJoint(runtime.Backend)), "Retiring an old native joint preserves its replacement");
                }
                PhysicsServer.JointMakeGroove(joint, anchor - new Vector2(0, 5), anchor + new Vector2(0, 5), anchor, floor.GetRID(), raw);
                tree.PhysicsFrame(1d / 60);
                PhysicsServer.JointMakeDampedSpring(joint, anchor, anchor + new Vector2(0, 4), floor.GetRID(), raw);
                PhysicsServer.DampedSpringJointSetRestLength(joint, 2); PhysicsServer.DampedSpringJointSetStiffness(joint, 3);
                PhysicsServer.DampedSpringJointSetDamping(joint, .5f);
                PhysicsServer.BodySetGravityScale(raw, 0);
                rawView.LinearVelocity = default; rawView.AngularVelocity = 0;
                for (var i = 0; i < 8; i++) tree.PhysicsFrame(1d / 60);
                Check(rawView.LinearVelocity.Y < -.01f && rawView.Transform.IsFinite(), "Wrapped spring pulls the isolated stretched anchor without common CPU casts");
                Collect();
                for (var i = 0; i < 64; i++) tree.PhysicsFrame(1d / 60);
                owner = GC.GetAllocatedBytesForCurrentThread(); all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 64; i++) tree.PhysicsFrame(1d / 60);
                owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(owner == 0 && all == 0, $"Wrapped selected joint full intervals allocated {owner}/{all} bytes");
                Console.WriteLine($"{backend}: wrapped pin/groove/spring, matching native retirement and 64 warmed joint intervals: {owner}/{all} B.");
            }
            finally { PhysicsServer.FreeRID(joint); }
            using var map = new PhysicsSnapshotMap(world.Space);
            map.Bind(1, 1, scene.GetRID()); map.Bind(2, 1, floor.GetRID()); map.Bind(3, 1, sensor.GetRID());
            map.Bind(4, 1, raw); map.Bind(5, 1, foreign);
            var snapshot = new PhysicsSnapshot(); map.Capture(snapshot); var saved = rawView.Transform;
            rawView.Transform = new(0, new(50, 0)); map.Apply(snapshot);
            Check(rawView.Transform.Origin.DistanceTo(saved.Origin) < .001f, "Selected ownership restores real portable body state and sleep through wrapped selected attachments");
            Console.WriteLine($"{backend}: selected scene/raw state/query factories, real hooks/contacts, identity/reentry/failure/borrow guards and 64 warmed steps: {owner}/{all} B.");
        }
        finally { PhysicsServer.FreeRID(raw); PhysicsServer.FreeRID(foreign); }
    }

    private sealed class FactoryRigid : RigidBody
    {
        internal Action<PhysicsDirectBodyState>? OnState;
        protected override void IntegrateForces(PhysicsDirectBodyState state) => OnState?.Invoke(state);
    }

    private sealed class FactoryBodyState(RID body) : PhysicsDirectBodyStateExtension(body)
    {
        protected override float GetAngularVelocityCore() => Runtime.ViewAngularVelocity;
        protected override void SetAngularVelocityCore(float value) => Runtime.SetAngularVelocity(value);
        protected override Vector2 GetLinearVelocityCore() => Runtime.ViewLinearVelocity;
        protected override void SetLinearVelocityCore(Vector2 value) => Runtime.SetLinearVelocity(value);
        protected override Vector2 GetCenterOfMassCore() => Runtime.ViewCenterOfMass;
        protected override Vector2 GetCenterOfMassLocalCore() => Runtime.ViewCenterOfMassLocal;
        protected override float GetInverseMassCore() => Runtime.ViewInverseMass;
        protected override float GetInverseInertiaCore() => Runtime.ViewInverseInertia;
        protected override bool IsSleepingCore() => Runtime.ViewSleeping;
        protected override void SetSleepStateCore(bool value) => Runtime.SetSleeping(value);
        protected override float GetStepCore() => Space.LastStep;
        protected override Vector2 GetTotalGravityCore() => Runtime.Owners.Scene?.EffectiveGravity ?? Runtime.Gravity;
        protected override float GetTotalLinearDampCore() => Runtime.Owners.Scene is RigidBody rigid ? rigid.ResolvedLinearDamp : Runtime.LinearDamp;
        protected override float GetTotalAngularDampCore() => Runtime.Owners.Scene is RigidBody rigid ? rigid.ResolvedAngularDamp : Runtime.AngularDamp;
        protected override uint GetCollisionLayerCore() => PhysicsServer.BodyGetCollisionLayer(Runtime.RID);
        protected override void SetCollisionLayerCore(uint value) => PhysicsServer.BodySetCollisionLayer(Runtime.RID, value);
        protected override uint GetCollisionMaskCore() => PhysicsServer.BodyGetCollisionMask(Runtime.RID);
        protected override void SetCollisionMaskCore(uint value) => PhysicsServer.BodySetCollisionMask(Runtime.RID, value);
        protected override Transform GetTransformCore() => Runtime.ViewTransform;
        protected override void SetTransformCore(Transform value) => Runtime.SetTransform(value);
        protected override Vector2 GetConstantForceCore() => Runtime.GetConstantForce();
        protected override float GetConstantTorqueCore() => Runtime.GetConstantTorque();
        protected override int GetContactCountCore() => CapturedContacts.Length;
        protected override PhysicsDirectSpaceState GetSpaceStateCore() => PhysicsServer.SpaceGetDirectState(Space.RID);
        protected override Vector2 GetVelocityAtLocalPositionCore(Vector2 localPosition) => Runtime.GetViewPointVelocity(localPosition);
        protected override void SetConstantForceCore(Vector2 force) => Runtime.SetViewConstantForce(force);
        protected override void SetConstantTorqueCore(float torque) => Runtime.SetViewConstantTorque(torque);
        protected override void AddConstantCentralForceCore(Vector2 force) => PhysicsServer.BodyAddConstantCentralForce(Runtime.RID, force);
        protected override void AddConstantTorqueCore(float torque) => PhysicsServer.BodyAddConstantTorque(Runtime.RID, torque);
        protected override void AddConstantForceCore(Vector2 force, Vector2 position) => PhysicsServer.BodyAddConstantForce(Runtime.RID, force, position);
        protected override void ApplyCentralForceCore(Vector2 force) => PhysicsServer.BodyApplyCentralForce(Runtime.RID, force);
        protected override void ApplyCentralImpulseCore(Vector2 impulse) => PhysicsServer.BodyApplyCentralImpulse(Runtime.RID, impulse * 2);
        protected override void ApplyForceCore(Vector2 force, Vector2 position) => PhysicsServer.BodyApplyForce(Runtime.RID, force, position);
        protected override void ApplyImpulseCore(Vector2 impulse, Vector2 position) => PhysicsServer.BodyApplyImpulse(Runtime.RID, impulse, position);
        protected override void ApplyTorqueCore(float torque) => PhysicsServer.BodyApplyTorque(Runtime.RID, torque);
        protected override void ApplyTorqueImpulseCore(float impulse) => PhysicsServer.BodyApplyTorqueImpulse(Runtime.RID, impulse);
        protected override void IntegrateForcesCore()
        {
            LinearVelocity = (LinearVelocity + TotalGravity * Step) * MathF.Max(0, 1 - Step * TotalLinearDamp);
            AngularVelocity *= MathF.Max(0, 1 - Step * TotalAngularDamp);
        }
        protected override PhysicsBodyContact GetContactCore(int contactIndex)
        {
            var value = CapturedContacts[contactIndex];
            return new(Runtime.RID, value.LocalShape, value.Collider, value.ColliderShape, value.LocalPoint, value.ColliderPoint,
                value.Normal, value.LocalVelocity, value.ColliderVelocity, value.Impulse);
        }
    }
}
