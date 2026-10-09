using Electron2D;
using static PhysicsDebugTests;

internal static class PhysicsVelocityEditTests
{
    internal static void Run(bool includeGPU = true)
    {
        Verify(PhysicsServer.Backend.CPU, false); Verify(PhysicsServer.Backend.CPU, true);
        if (includeGPU) { Verify(PhysicsServer.Backend.GPU, false); Verify(PhysicsServer.Backend.GPU, true); VerifyResident(); }
    }
    private static void Verify(PhysicsServer.Backend backend, bool scene)
    {
        using var shape = new CircleShape { Radius = 5 };
        using var world = new World(backend); using var view = new SubViewport { World = world };
        using var rigid = new ProbeBody { GravityScale = 0, Mass = 2, Inertia = 4 };
        var spaceRID = scene ? world.Space : PhysicsServer.SpaceCreate(backend);
        RID body;
        if (scene) { rigid.AddChild(new CollisionShape { Shape = shape }); view.AddChild(rigid); body = rigid.GetRID(); }
        else
        {
            body = PhysicsServer.BodyCreate(); PhysicsServer.BodySetGravityScale(body, 0);
            PhysicsServer.BodySetMass(body, 2); PhysicsServer.BodySetInertia(body, 4);
            PhysicsServer.BodyAddShape(body, shape.GetRID()); PhysicsServer.BodySetSpace(body, spaceRID);
        }
        using var tree = scene ? new SceneTree(view) : null;
        var space = PhysicsServer.Service.GetSceneSpace(spaceRID);
        try
        {
            PhysicsServer.BodySetCanSleep(body, false);
            if (scene) tree!.PhysicsFrame(1d / 60); else { PhysicsServer.SpaceSetActive(spaceRID, true); PhysicsServer.SpaceStep(spaceRID, 1d / 60); }
            _ = PhysicsServer.BodyGetAngularVelocity(body);
            var gpu = space.GPUStore; var submits = gpu?.SubmissionCount ?? 0; var bytes = gpu?.ReadbackBytes ?? 0;
            PhysicsServer.BodySetTransform(body, new(0, new Vector2(20, 30)));
            PhysicsServer.BodySetLinearVelocity(body, new(12, 8)); PhysicsServer.BodySetAngularVelocity(body, 3);
            PhysicsServer.BodySetLinearVelocity(body, new(5, 6));
            Check((gpu?.SubmissionCount ?? 0) == submits && (gpu?.ReadbackBytes ?? 0) == bytes, "Pose and component velocity writes must queue without GPU submission/readback");
            Motion(new(5, 6), 3); Near(PhysicsServer.BodyGetTransform(body).Origin, new(20, 30));
            using var state = PhysicsServer.BodyGetDirectState(body)!;
            state.AngularVelocity = 4; state.LinearVelocity = new(7, 9); Motion(new(7, 9), 4);
            PhysicsServer.BodyApplyCentralImpulse(body, new(2, 4)); PhysicsServer.BodyApplyTorqueImpulse(body, 8);
            PhysicsServer.BodySetLinearVelocity(body, new(5, 6)); Motion(new(5, 6), 6);
            PhysicsServer.BodyApplyCentralImpulse(body, new(2, 4)); PhysicsServer.BodyApplyTorqueImpulse(body, 8);
            PhysicsServer.BodySetAngularVelocity(body, 3); Motion(new(6, 8), 3);
            PhysicsServer.BodySetSleeping(body, true); state.AngularVelocity = 0;
            Check(!state.Sleeping, "Explicit zero component wakes after sleep"); Motion(Vector2.Zero, 0);
            state.LinearVelocity = new(7, 9); state.AngularVelocity = 2;
            PhysicsServer.BodySetAxisVelocity(body, new(0, 20)); Motion(new(7, 20), 2);
            if (scene)
            {
                rigid.LockRotation = true; rigid.AngularVelocity = 8; Motion(new(7, 20), 0);
                rigid.LockRotation = false; rigid.AngularVelocity = 3; Motion(new(7, 20), 3);
            }
            else
            {
                PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.RigidLinear); PhysicsServer.BodySetAngularVelocity(body, 8); Motion(new(7, 20), 0);
                PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Rigid); PhysicsServer.BodySetAngularVelocity(body, 3); Motion(new(7, 20), 3);
            }
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodySetLinearVelocity(body, new(float.NaN, 0)));
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodySetAngularVelocity(body, float.PositiveInfinity));
            Motion(new(7, 20), 3);
            Reject<InvalidOperationException>(() => Task.Run(() => PhysicsServer.BodySetAngularVelocity(body, 0)).GetAwaiter().GetResult());
            if (scene)
            {
                rigid.CustomIntegrator = true; rigid.Write = true; tree!.PhysicsFrame(1d / 60); rigid.Write = false;
                Motion(new(8, 10), 2);
            }
            Console.WriteLine($"Component velocity edits {backend}/{(scene ? "scene" : "server")}: queued pose/velocities, direct state, impulse ordering, sleep, locks, axis projection and guards passed.");
        }
        finally { if (!scene) { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(spaceRID); } }
        void Motion(Vector2 linear, float angular) { Near(PhysicsServer.BodyGetLinearVelocity(body), linear); Check(Math.Abs(PhysicsServer.BodyGetAngularVelocity(body) - angular) < .0001f, "Angular velocity preserves the other component, within .0001 rad/s"); }
    }
    private sealed class ProbeBody : RigidBody
    {
        internal bool Write;
        protected override void IntegrateForces(PhysicsDirectBodyState state)
        { if (Write) { state.LinearVelocity = new(13, 21); state.AngularVelocity = 2; state.LinearVelocity = new(8, 10); } }
    }
    private static void VerifyResident()
    {
        using var store = new GPUPhysicsBodyStore();
        var body = store.Add(new(PhysicsServer.BodyMode.Rigid, Vector2.Zero, 0, new(2, 4), 3, Mass: 2, Inertia: 4));
        var handles = new[] { body }; var state = new GPUPhysicsBodyStore.Snapshot[1];
        store.SetSolverLinearVelocity(body, new(5, 6)); Motion(new(5, 6), 3);
        var submits = store.SubmissionCount; var reads = store.ReadbackBytes;
        store.ApplyImpulse(body, new(4, 6), 8); store.SetSolverLinearVelocity(body, new(10, 20));
        Check(store.SubmissionCount == submits && store.ReadbackBytes == reads, "Partial edits and prepared impulses stay queued");
        Motion(new(10, 20), 5);
        store.SetSolverVelocity(body, new(5, 6), 3); store.ApplyImpulse(body, new(4, 6), 8);
        store.SetSolverAngularVelocity(body, 9); Motion(new(7, 9), 9);
        store.SetSolverLinearVelocity(body, new(10, 20)); store.SetSolverAngularVelocity(body, 7);
        store.ApplyImpulse(body, new(4, 6), 8); Motion(new(12, 23), 9);
        store.SetIntegrationPolicy(body, new(LockRotation: true)); store.SetIntegrationPolicy(body, new());
        store.SetSolverLinearVelocity(body, new(3, 4)); Motion(new(3, 4), 0);
        store.SetIntegrationPolicy(body, new(LockRotation: true)); store.SetIntegrationPolicy(body, new());
        store.SetSolverAngularVelocity(body, 2); Motion(new(3, 4), 2);
        store.SetSleeping(body, true); store.SetSolverLinearVelocity(body, new(5, 6)); Motion(new(5, 6), 0);
        Check(!state[0].Sleeping, "Partial edit supersedes earlier sleep but keeps cleared motion");
        store.SetSleeping(body, true); store.SetSolverAngularVelocity(body, 2); Motion(Vector2.Zero, 2);
        store.SetSolverLinearVelocity(body, new(9, 9)); store.SetSolverVelocity(body, new(1, 2), 3); Motion(new(1, 2), 3);
        var fixedBody = store.Add(new(PhysicsServer.BodyMode.Static, new(30, 40), 0, new(3, 4), 5));
        store.SetSolverAngularVelocity(fixedBody, 0); store.SetSolverLinearVelocity(fixedBody, Vector2.Zero);
        handles[0] = fixedBody; Motion(new(3, 4), 5); Near(state[0].Position, new(30, 40)); handles[0] = body;
        store.SetMode(body, PhysicsServer.BodyMode.RigidLinear); store.SetSolverAngularVelocity(body, 20); Motion(new(1, 2), 0);
        store.SetMode(body, PhysicsServer.BodyMode.Rigid); store.SetSolverAngularVelocity(body, 4); Motion(new(1, 2), 4);
        Reject<ArgumentOutOfRangeException>(() => store.SetSolverLinearVelocity(body, new(float.NaN, 0)));
        Reject<ArgumentOutOfRangeException>(() => store.SetSolverAngularVelocity(body, float.PositiveInfinity));
        Motion(new(1, 2), 4);
        Reject<InvalidOperationException>(() => Task.Run(() => store.SetSolverAngularVelocity(body, 1)).GetAwaiter().GetResult());
        using var other = new GPUPhysicsBodyStore();
        var foreign = other.Add(new(PhysicsServer.BodyMode.Rigid, Vector2.Zero, 0, Vector2.Zero, 0));
        Reject<ArgumentException>(() => store.SetSolverLinearVelocity(foreign, Vector2.Zero));
        store.Remove(body); Reject<ArgumentException>(() => store.SetSolverAngularVelocity(body, 1));
        Console.WriteLine("Resident component commands preserve creation, impulses on the other lane, sleep, locks, roles and virtual surfaces without reads.");
        void Motion(Vector2 linear, float angular)
        { store.Read(handles, state); Near(new(state[0].Velocity.X, state[0].Velocity.Y), linear); Check(Math.Abs(state[0].Velocity.Z - angular) < .0001f, "Resident angular command order within .0001 rad/s"); }
    }
    private static void Near(Vector2 a, Vector2 b) => Check(a.DistanceTo(b) < .0001f, "Linear state is within .0001 scene units (or units/s)");
}
