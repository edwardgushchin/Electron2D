using Electron2D;
using static PhysicsDebugTests;

internal static class PhysicsGPUPublicationTests
{
    internal static void Run(bool includeGPU = true)
    {
        Verify(PhysicsServer.Backend.CPU); Scene(PhysicsServer.Backend.CPU); ConnectedWake(PhysicsServer.Backend.CPU); ContactReceivers(PhysicsServer.Backend.CPU); LateReceiver(PhysicsServer.Backend.CPU);
        if (includeGPU) { Verify(PhysicsServer.Backend.GPU); Scene(PhysicsServer.Backend.GPU); ConnectedWake(PhysicsServer.Backend.GPU); ContactReceivers(PhysicsServer.Backend.GPU); LateReceiver(PhysicsServer.Backend.GPU); }
    }
    private static RID CreateBody(RID space)
    {
        var body = PhysicsServer.BodyCreate(); PhysicsServer.BodySetMass(body, 1); PhysicsServer.BodySetInertia(body, 1);
        PhysicsServer.BodySetSpace(body, space); return body;
    }
    private static RID CreateSpace(PhysicsServer.Backend backend)
    {
        var space = PhysicsServer.SpaceCreate(backend); PhysicsServer.SpaceSetActive(space, true);
        PhysicsServer.AreaSetGravity(space, 0); PhysicsServer.AreaSetLinearDamp(space, 0); PhysicsServer.AreaSetAngularDamp(space, 0); return space;
    }
    private static void Verify(PhysicsServer.Backend backend)
    {
        var space = CreateSpace(backend); var a = CreateBody(space); var b = CreateBody(space);
        try
        {
            Step();
            var original = PhysicsServer.BodyGetTransform(a);
            PhysicsServer.BodySetTransform(a, new(0, new Vector2(500, 100)));
            Check(PhysicsServer.BodyGetTransform(a).Origin.X == 500, "Explicit getters observe an intermediate authored pose");
            PhysicsServer.BodySetTransform(a, original); Step();
            Check(PhysicsServer.BodyGetTransform(a).Origin.DistanceTo(original.Origin) < .001f, "A step returning to the last published state must discard an intermediate getter cache");
            PhysicsServer.BodySetTransform(a, new(0, new Vector2(3, 7))); PhysicsServer.BodySetLinearVelocity(a, new(2, 0));
            Step(); Check(PhysicsServer.BodyGetTransform(a).Origin.X > 3, "Completion publishes solved pose after an authored edit");
            var first = 0; var second = 0;
            PhysicsServer.BodySetForceIntegrationCallback(a, _ => { first++; PhysicsServer.BodySetStateSyncCallback(b, _ => second++); });
            Step(); Check(first == 1 && second == 1, "An earlier callback can enable a later receiver in the same step");
            PhysicsServer.BodySetForceIntegrationCallback(a, null); PhysicsServer.BodySetStateSyncCallback(b, null); Step();
            PhysicsServer.BodySetLinearVelocity(a, Vector2.Zero); PhysicsServer.BodyApplyCentralForce(a, new(60, 0)); PhysicsServer.BodySetSleeping(a, true);
            Step(); Near(PhysicsServer.BodyGetLinearVelocity(a).X, 0);
            PhysicsServer.BodySetSleeping(a, false); Step(); Near(PhysicsServer.BodyGetLinearVelocity(a).X, 1);
            Step(); Near(PhysicsServer.BodyGetLinearVelocity(a).X, 1);
            PhysicsServer.BodySetMode(b, PhysicsServer.BodyMode.Static); PhysicsServer.BodyApplyCentralForce(b, new(60, 0)); Step();
            PhysicsServer.BodySetMode(b, PhysicsServer.BodyMode.Rigid); Step(); Near(PhysicsServer.BodyGetLinearVelocity(b).X, 1);
            PhysicsServer.BodySetOmitForceIntegration(b, true); PhysicsServer.BodyApplyCentralForce(b, new(60, 0)); Step(); Near(PhysicsServer.BodyGetLinearVelocity(b).X, 1);
            PhysicsServer.BodySetOmitForceIntegration(b, false); Step(); Near(PhysicsServer.BodyGetLinearVelocity(b).X, 1);
            PhysicsServer.BodySetLinearVelocity(a, Vector2.Zero); PhysicsServer.BodyApplyTorque(a, 60); Step(); Near(PhysicsServer.BodyGetAngularVelocity(a), 1);
            Step(); Near(PhysicsServer.BodyGetAngularVelocity(a), 1);
            Console.WriteLine($"Pre-step publication {backend}: no unused snapshot, fresh completion, later callback registration, sleeping/static retained force, omission and single-tick torque passed.");
        }
        finally { PhysicsServer.FreeRID(b); PhysicsServer.FreeRID(a); PhysicsServer.FreeRID(space); }
        void Step() => PhysicsServer.SpaceStep(space, 1d / 60);
    }
    private static void Scene(PhysicsServer.Backend backend)
    {
        using var world = new World(backend); using var root = new SubViewport { World = world };
        var body = new RigidBody { Name = "Plain", GravityScale = 0, CanSleep = false, LinearVelocity = new(60, 0) }; root.AddChild(body);
        using var tree = new SceneTree(root);
        Step(); Check(body.Position.X > .9f && body.LinearVelocity.X > 59, "Plain scene body receives current solved pose/velocity");
        using var probe = new Probe { Name = "Probe", GravityScale = 0, CanSleep = false, CustomIntegrator = true }; root.AddChild(probe);
        Step(); Check(probe.Calls == 1, "A scene integration override retains its active-before-step observation");
        Step(); Check(probe.Calls == 2, "A callback velocity write requires fresh activity publication");
        root.RemoveChild(probe); Step();
        using var observer = new Observer { Name = "Observer", GravityScale = 0, CustomIntegrator = true }; root.AddChild(observer);
        Step(); Step(); Check(observer.Calls == 2, "Read-only integration callbacks reuse a current complete publication");
        observer.Sleeping = true; Step(); Check(observer.Calls == 2, "A separately invalidated sleep cache requires fresh publication");
        root.RemoveChild(observer); Step();
        using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!; Step(); Check(state.LinearVelocity.X > 59, "A live view without captured contacts needs no pre-step publication");
        Console.WriteLine($"Pre-step scene publication {backend}: plain bodies, custom integrator attach/detach and live direct view passed.");
        void Step() => tree.PhysicsFrame(1d / 60);
    }
    private static void LateReceiver(PhysicsServer.Backend backend)
    {
        var space = CreateSpace(backend); var a = CreateBody(space); var b = CreateBody(space);
        try
        {
            PhysicsServer.SpaceSetBodyTimeToSleep(space, 0);
            PhysicsServer.BodySetCanSleep(a, false); PhysicsServer.BodySetGravityScale(b, 0);
            PhysicsServer.AreaSetGravity(space, 60);
            var first = 0; var second = 0;
            PhysicsServer.BodySetForceIntegrationCallback(a, _ =>
            {
                first++;
                PhysicsServer.BodySetStateSyncCallback(b, state =>
                {
                    second++; Near(state.TotalGravity.Y, 0);
                    Check(state.Sleeping, "A newly sleeping receiver still receives its active-before-step callback");
                });
            });
            PhysicsServer.SpaceStep(space, 1d / 60);
            Check(first == 1 && second == 1, "Late receiver eligibility retains initial activity without a full state mirror");
            PhysicsServer.SpaceStep(space, 1d / 60);
            Check(first == 2 && second == 1, "An already sleeping receiver stays ineligible");
            PhysicsServer.BodySetCanSleep(b, false); PhysicsServer.BodySetGravityScale(b, 1);
            PhysicsServer.BodySetStateSyncCallback(b, null);
            PhysicsServer.BodySetForceIntegrationCallback(a, _ => PhysicsServer.BodySetStateSyncCallback(b, state =>
            {
                second++; Near(state.TotalGravity.Y, 60); Near(state.LinearVelocity.Y, 1);
            }));
            PhysicsServer.SpaceStep(space, 1d / 60);
            Check(second == 2, "Late receiver resolves fresh fields and solved velocity");
            PhysicsServer.BodySetStateSyncCallback(b, null);
            PhysicsServer.BodySetForceIntegrationCallback(a, _ =>
            {
                PhysicsServer.BodySetLinearVelocity(b, new(7, 8));
                PhysicsServer.BodySetStateSyncCallback(b, state =>
                {
                    second++; Near(state.LinearVelocity.X, 7); Near(state.LinearVelocity.Y, 8);
                    using var other = PhysicsServer.BodyGetDirectState(a)!; Near(other.TotalGravity.Y, 60);
                });
                throw new InvalidOperationException("Expected preceding callback failure");
            });
            Reject<AggregateException>(() => PhysicsServer.SpaceStep(space, 1d / 60));
            Check(second == 3, "Committed edits and later receivers survive an earlier callback failure");
            PhysicsServer.BodySetStateSyncCallback(b, null);
            PhysicsServer.BodySetForceIntegrationCallback(a, _ =>
            {
                PhysicsServer.BodySetStateSyncCallback(b, _ => second++);
                PhysicsServer.BodySetSpace(b, default);
            });
            PhysicsServer.SpaceStep(space, 1d / 60);
            Check(second == 3, "A detached late receiver cannot observe a retired attachment");
            PhysicsServer.BodySetForceIntegrationCallback(a, null);
            PhysicsServer.BodySetSpace(b, space); PhysicsServer.SpaceStep(space, 1d / 60);
            Check(second == 4, "Reattached receivers use their new active attachment");
            Console.WriteLine($"Late receiver {backend}: new sleep, fresh fields, edits/errors, detach/reattach and cross-body reads passed.");
        }
        finally { PhysicsServer.FreeRID(b); PhysicsServer.FreeRID(a); PhysicsServer.FreeRID(space); }
    }
    private static void ConnectedWake(PhysicsServer.Backend backend)
    {
        var space = CreateSpace(backend); var a = CreateBody(space); var b = CreateBody(space); var joint = PhysicsServer.JointCreate();
        try
        {
            PhysicsServer.BodySetTransform(b, new(0, new Vector2(20, 0))); PhysicsServer.JointMakePin(joint, new(10, 0), a, b);
            PhysicsServer.SpaceStep(space, 1d / 60);
            PhysicsServer.BodySetSleeping(a, true); PhysicsServer.BodySetSleeping(b, true);
            PhysicsServer.BodySetLinearVelocity(a, new(10, 0)); PhysicsServer.SpaceStep(space, 1d / 60);
            Check(!PhysicsServer.BodyGetSleeping(b) && PhysicsServer.BodyGetLinearVelocity(b).X > .1f, "Skipping unused snapshots preserves connected wake and joint response");
        }
        finally { PhysicsServer.FreeRID(joint); PhysicsServer.FreeRID(b); PhysicsServer.FreeRID(a); PhysicsServer.FreeRID(space); }
    }
    private static void ContactReceivers(PhysicsServer.Backend backend)
    {
        using var world = new World(backend); using var root = new SubViewport { World = world };
        using var shape = new RectangleShape { Size = new(20, 20) };
        using var floorShape = new RectangleShape { Size = new(200, 20) };
        var floor = new StaticBody { Name = "Floor", Position = new(0, 100) };
        floor.AddChild(new CollisionShape { Shape = floorShape }); root.AddChild(floor);
        using var scene = new RigidBody { Name = "Scene", GravityScale = 0, CanSleep = false };
        scene.AddChild(new CollisionShape { Shape = shape }); root.AddChild(scene);
        var area = new Area { Name = "Sensor", Position = new(0, 80) };
        area.AddChild(new CollisionShape { Shape = floorShape }); root.AddChild(area);
        using var tree = new SceneTree(root);
        var raw = PhysicsServer.BodyCreate(); var sensor = PhysicsServer.AreaCreate();
        try
        {
            PhysicsServer.BodyAddShape(raw, shape.GetRID()); PhysicsServer.BodySetGravityScale(raw, 0); PhysicsServer.BodySetCanSleep(raw, false);
            PhysicsServer.BodySetSpace(raw, world.Space);
            PhysicsServer.AreaAddShape(sensor, floorShape.GetRID()); PhysicsServer.AreaSetTransform(sensor, new(0, new(0, 80))); PhysicsServer.AreaSetSpace(sensor, world.Space);
            using var sceneView = PhysicsServer.BodyGetDirectState(scene.GetRID())!;
            using var rawView = PhysicsServer.BodyGetDirectState(raw)!;
            using var floorView = PhysicsServer.BodyGetDirectState(floor.GetRID())!;
            foreach (var limit in new[] { 0, 1, 4, 0, 1 })
            {
                scene.MaxContactsReported = limit;
                PhysicsServer.BodySetMaxContactsReported(raw, limit);
                PhysicsServer.BodySetMaxContactsReported(floor.GetRID(), limit);
                Check(sceneView.GetContactCount() == 0 && rawView.GetContactCount() == 0 && floorView.GetContactCount() == 0, "Live limit edits clear prior scene, raw and static contact snapshots");
                Step(); CheckReports(limit, sceneView, rawView);
            }
            root.RemoveChild(scene); PhysicsServer.BodySetSpace(raw, default);
            tree.PhysicsFrame(1d / 60);
            Check(floorView.GetContactCount() == 0, "Detached receivers cannot retain reports in their former world");
            root.AddChild(scene); PhysicsServer.BodySetSpace(raw, world.Space);
            Reject<ObjectDisposedException>(() => sceneView.GetContactCount());
            Reject<ObjectDisposedException>(() => rawView.GetContactCount());
            using var reattachedScene = PhysicsServer.BodyGetDirectState(scene.GetRID())!;
            using var reattachedRaw = PhysicsServer.BodyGetDirectState(raw)!;
            Step(); CheckReports(1, reattachedScene, reattachedRaw);
            Console.WriteLine($"Contact receivers {backend}: mixed scene/raw/static bodies, sensor exclusion, live zero/grow/shrink caps and detach/reattach passed.");

            void Step()
            {
                scene.Position = new(-30, 80); scene.LinearVelocity = new(0, 100); scene.Rotation = 0; scene.AngularVelocity = 0;
                PhysicsServer.BodySetTransform(raw, new(0, new(30, 80))); PhysicsServer.BodySetLinearVelocity(raw, new(0, 100)); PhysicsServer.BodySetAngularVelocity(raw, 0);
                tree.PhysicsFrame(1d / 60);
            }
            void CheckReports(int limit, PhysicsDirectBodyState sceneState, PhysicsDirectBodyState rawState)
            {
                VerifyView(sceneState, floor.GetRID()); VerifyView(rawState, floor.GetRID());
                Check(floorView.GetContactCount() <= limit && (floorView.GetContactCount() > 0) == (limit > 0), "Static scene receivers retain their independent cap");
                for (var i = 0; i < floorView.GetContactCount(); i++)
                    Check(floorView.GetContactCollider(i) == raw || floorView.GetContactCollider(i) == scene.GetRID(), "Sensors never enter physical contact reports");
                void VerifyView(PhysicsDirectBodyState view, RID collider)
                {
                    Check(view.GetContactCount() <= limit && (view.GetContactCount() > 0) == (limit > 0), "Live cap applies on the next step");
                    for (var i = 0; i < view.GetContactCount(); i++) Check(view.GetContactCollider(i) == collider, "Report belongs to the actual receiver and collider");
                }
            }
        }
        finally { PhysicsServer.FreeRID(sensor); PhysicsServer.FreeRID(raw); }
    }
    private static void Near(float value, float expected) => Check(Math.Abs(value - expected) < .001f, "A one-second-unit force integrated for 1/60 s agrees within .001 scene units/s (or rad/s)");
    private sealed class Observer : RigidBody
    { internal int Calls; protected override void IntegrateForces(PhysicsDirectBodyState state) { Calls++; } }
    private sealed class Probe : RigidBody
    { internal int Calls; protected override void IntegrateForces(PhysicsDirectBodyState state) { Calls++; state.LinearVelocity = new(5, 0); } }
}
