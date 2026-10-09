using Electron2D;
using static PhysicsDebugTests;

internal static class PhysicsGPUPublicationTests
{
    internal static void Run(bool includeGPU = true)
    {
        Verify(PhysicsServer.Backend.CPU); Scene(PhysicsServer.Backend.CPU); ConnectedWake(PhysicsServer.Backend.CPU);
        if (includeGPU) { Verify(PhysicsServer.Backend.GPU); Scene(PhysicsServer.Backend.GPU); ConnectedWake(PhysicsServer.Backend.GPU); }
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
        var store = PhysicsServer.Service.GetSceneSpace(space).GPUStore;
        try
        {
            Step(1);
            var original = PhysicsServer.BodyGetTransform(a);
            PhysicsServer.BodySetTransform(a, new(0, new Vector2(500, 100)));
            Check(PhysicsServer.BodyGetTransform(a).Origin.X == 500, "Explicit getters observe an intermediate authored pose");
            PhysicsServer.BodySetTransform(a, original); Step(1);
            Check(PhysicsServer.BodyGetTransform(a).Origin.DistanceTo(original.Origin) < .001f, "A step returning to the last published state must discard an intermediate getter cache");
            PhysicsServer.BodySetTransform(a, new(0, new Vector2(3, 7))); PhysicsServer.BodySetLinearVelocity(a, new(2, 0));
            Step(1); Check(PhysicsServer.BodyGetTransform(a).Origin.X > 3, "Completion publishes solved pose after an authored edit");
            var first = 0; var second = 0;
            PhysicsServer.BodySetForceIntegrationCallback(a, _ => { first++; PhysicsServer.BodySetStateSyncCallback(b, _ => second++); });
            Step(2); Check(first == 1 && second == 1, "An earlier callback can enable a later receiver in the same step");
            PhysicsServer.BodySetForceIntegrationCallback(a, null); PhysicsServer.BodySetStateSyncCallback(b, null); Step(1);
            PhysicsServer.BodySetLinearVelocity(a, Vector2.Zero); PhysicsServer.BodyApplyCentralForce(a, new(60, 0)); PhysicsServer.BodySetSleeping(a, true);
            Step(2); Near(PhysicsServer.BodyGetLinearVelocity(a).X, 0);
            PhysicsServer.BodySetSleeping(a, false); Step(2); Near(PhysicsServer.BodyGetLinearVelocity(a).X, 1);
            Step(1); Near(PhysicsServer.BodyGetLinearVelocity(a).X, 1);
            PhysicsServer.BodySetMode(b, PhysicsServer.BodyMode.Static); PhysicsServer.BodyApplyCentralForce(b, new(60, 0)); Step(2);
            PhysicsServer.BodySetMode(b, PhysicsServer.BodyMode.Rigid); Step(2); Near(PhysicsServer.BodyGetLinearVelocity(b).X, 1);
            PhysicsServer.BodySetOmitForceIntegration(b, true); PhysicsServer.BodyApplyCentralForce(b, new(60, 0)); Step(2); Near(PhysicsServer.BodyGetLinearVelocity(b).X, 1);
            PhysicsServer.BodySetOmitForceIntegration(b, false); Step(1); Near(PhysicsServer.BodyGetLinearVelocity(b).X, 1);
            PhysicsServer.BodySetLinearVelocity(a, Vector2.Zero); PhysicsServer.BodyApplyTorque(a, 60); Step(2); Near(PhysicsServer.BodyGetAngularVelocity(a), 1);
            Step(1); Near(PhysicsServer.BodyGetAngularVelocity(a), 1);
            Console.WriteLine($"Pre-step publication {backend}: no unused snapshot, fresh completion, later callback registration, sleeping/static retained force, omission and single-tick torque passed.");
        }
        finally { PhysicsServer.FreeRID(b); PhysicsServer.FreeRID(a); PhysicsServer.FreeRID(space); }
        void Step(int publications)
        {
            var before = store?.ChangePublicationCount ?? 0; PhysicsServer.SpaceStep(space, 1d / 60);
            Check(store is null || store.ChangePublicationCount - before == publications, $"Expected {publications} necessary body publications per step");
        }
    }
    private static void Scene(PhysicsServer.Backend backend)
    {
        using var world = new World(backend); using var root = new SubViewport { World = world };
        var body = new RigidBody { Name = "Plain", GravityScale = 0, CanSleep = false, LinearVelocity = new(60, 0) }; root.AddChild(body);
        using var tree = new SceneTree(root); var store = PhysicsServer.Service.GetSceneSpace(world.Space).GPUStore;
        Step(1); Check(body.Position.X > .9f && body.LinearVelocity.X > 59, "Plain scene body receives current solved pose/velocity");
        using var probe = new Probe { Name = "Probe", GravityScale = 0, CanSleep = false, CustomIntegrator = true }; root.AddChild(probe);
        Step(2); Check(probe.Calls == 1, "A scene integration override retains its active-before-step observation");
        root.RemoveChild(probe); Step(1);
        using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!; Step(1); Check(state.LinearVelocity.X > 59, "A live view without captured contacts needs no pre-step publication");
        Console.WriteLine($"Pre-step scene publication {backend}: plain bodies, custom integrator attach/detach and live direct view passed.");
        void Step(int publications)
        {
            var before = store?.ChangePublicationCount ?? 0; tree.PhysicsFrame(1d / 60);
            Check(store is null || store.ChangePublicationCount - before == publications, "Scene publication follows actual pre-step consumers");
        }
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
    private static void Near(float value, float expected) => Check(Math.Abs(value - expected) < .001f, "A one-second-unit force integrated for 1/60 s agrees within .001 scene units/s (or rad/s)");
    private sealed class Probe : RigidBody
    { internal int Calls; protected override void IntegrateForces(PhysicsDirectBodyState state) { Calls++; state.LinearVelocity = new(5, 0); } }
}
