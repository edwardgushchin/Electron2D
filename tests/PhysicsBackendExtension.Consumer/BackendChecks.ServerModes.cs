using Electron2D;

public static partial class BackendChecks
{
    public static void RunServerModes(PhysicsServer.Backend? backend = null) => ServerModeChecks(backend);
    private static void ServerModeChecks(PhysicsServer.Backend? backend = null)
    {
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("ServerModes", () => solver = new());
        using var world = backend is { } selected ? new World(selected) : new World("ServerModes");
        using var circle = new CircleShape { Radius = 3 };
        using var root = new SubViewport { World = world };
        PhysicsBody[] scenes = [new StaticBody(), new AnimatableBody(), new CharacterBody(), new RigidBody(), new PhysicalBone()];
        var index = 0;
        foreach (var body in scenes)
        {
            body.Position = new(index++ * 100, 0);
            body.AddChild(new CollisionShape { Shape = circle }); root.AddChild(body);
            PhysicsServer.BodySetMode(body.GetRID(), PhysicsServer.BodyMode.RigidLinear);
            Check(PhysicsServer.BodyGetMode(body.GetRID()) == PhysicsServer.BodyMode.RigidLinear,
                "Detached scene RIDs accept every declared server mode");
        }
        using var tree = new SceneTree(root);
        foreach (var body in scenes)
        {
            Check((solver?.Bodies[body.GetRID()].Mode ?? PhysicsServer.BodyGetMode(body.GetRID())) == PhysicsServer.BodyMode.RigidLinear,
                "Initial scene attachment retains direct server mode authoring");
            var id = body.GetRID();
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodySetMode(id, (PhysicsServer.BodyMode)99));
            PhysicsServer.BodySetGravityScale(id, 0); PhysicsServer.BodySetCanSleep(id, false);
            PhysicsServer.BodySetMass(id, 2);
            PhysicsServer.BodySetLinearVelocity(id, new(6, 0)); PhysicsServer.BodyApplyCentralImpulse(id, new(2, 0));
            var before = body.Position; tree.PhysicsFrame(1d / 60);
            Check(body.Position.X > before.X && PhysicsServer.BodyGetLinearVelocity(id).X >= 6,
                "Every scene sibling has actual dynamic response and solver pose publication");
            Check(MathF.Abs(PhysicsServer.BodyGetDirectState(id)!.InverseMass - .5f) < .0001f,
                "Scene siblings retain configured kilograms through server mode changes");
            PhysicsServer.BodySetMode(id, PhysicsServer.BodyMode.Static);
            var fixedPose = body.Position;
            PhysicsServer.BodySetLinearVelocity(id, new(6, 0)); tree.PhysicsFrame(1d / 60);
            Check(body.Position == fixedPose && PhysicsServer.BodyGetLinearVelocity(id) == new Vector2(6, 0),
                "Direct static mode keeps pose while exposing surface velocity");
            PhysicsServer.BodySetMode(id, PhysicsServer.BodyMode.Rigid); PhysicsServer.BodySetAngularVelocity(id, 2);
            tree.PhysicsFrame(1d / 60);
            Check(body.Rotation != 0 && PhysicsServer.BodyGetMode(id) == PhysicsServer.BodyMode.Rigid,
                "Direct Rigid mode admits angular response on every scene sibling");
            PhysicsServer.BodySetMode(id, PhysicsServer.BodyMode.Kinematic);
            PhysicsServer.BodySetTransform(id, new(0, body.Position));
            tree.PhysicsFrame(1d / 60);
            var target = body.Position + new Vector2(1, 0);
            PhysicsServer.BodySetTransform(id, new(0, target)); tree.PhysicsFrame(1d / 60);
            Check(body.Position.IsEqualApprox(target), "Direct kinematic mode reaches its server-authored target");
            target += new Vector2(1, 0); body.GlobalPosition = target; tree.PhysicsFrame(1d / 60);
            Check(body.Position.IsEqualApprox(target), $"Scene transform authoring uses the direct kinematic target path for {body.GetType().Name}: {body.Position} versus {target}");
            PhysicsServer.BodySetMode(id, PhysicsServer.BodyMode.RigidLinear);
            PhysicsServer.BodySetSleeping(id, true);
            Check(PhysicsServer.BodyGetSleeping(id), "Every dynamic scene sibling supports direct sleep state");
            PhysicsServer.BodySetSleeping(id, false); PhysicsServer.BodySetLinearVelocity(id, new(6, 0));
            using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space);
            var saved = body.Position; tree.PhysicsFrame(1d / 60); checkpoint.Restore();
            Check(body.Position.IsEqualApprox(saved) && PhysicsServer.BodyGetMode(id) == PhysicsServer.BodyMode.RigidLinear,
                "Private history restores scene-owned direct mode and motion");
            root.RemoveChild(body); root.AddChild(body);
            Check(PhysicsServer.BodyGetMode(id) == PhysicsServer.BodyMode.RigidLinear && PhysicsServer.BodyGetLinearVelocity(id).X >= 6,
                "Reentry retains direct server mode and dynamic velocity");
        }
        using var map = new PhysicsSnapshotMap(world.Space);
        for (var i = 0; i < scenes.Length; i++) map.Bind((ulong)i + 1, 1, scenes[i].GetRID());
        var snapshot = new PhysicsSnapshot(65536); map.Capture(snapshot);
        var first = scenes[0].Position; tree.PhysicsFrame(1d / 60); map.Apply(snapshot);
        Check(scenes[0].Position.IsEqualApprox(first), "Portable state restores the direct scene mode profile");
        var configured = (RigidBody)scenes[3];
        configured.Freeze = true;
        Check(PhysicsServer.BodyGetMode(configured.GetRID()) == PhysicsServer.BodyMode.Static,
            "Scene Freeze authoring restores configured scene control");
        PhysicsServer.BodySetMode(configured.GetRID(), PhysicsServer.BodyMode.Rigid);
        Check(configured.Freeze && PhysicsServer.BodyGetMode(configured.GetRID()) == PhysicsServer.BodyMode.Rigid,
            "Direct mode authoring preserves scene descriptors while selecting the physical role");
        configured.LinearVelocity = new(5, 0); configured.AngularVelocity = 1;
        Check(PhysicsServer.BodyGetLinearVelocity(configured.GetRID()) == configured.LinearVelocity &&
            PhysicsServer.BodyGetAngularVelocity(configured.GetRID()) == configured.AngularVelocity,
            "Rigid scene velocity accessors share immediate direct server state even while configured Freeze is retained");
        configured.Sleeping = true; Check(PhysicsServer.BodyGetSleeping(configured.GetRID()), "Scene sleep accessor shares direct server state");
        configured.Sleeping = false;
        configured.FreezeMode = RigidFreezeMode.Kinematic;
        Check(PhysicsServer.BodyGetMode(configured.GetRID()) == PhysicsServer.BodyMode.Kinematic,
            "A later scene role edit replaces the direct mode profile");
        configured.Freeze = false;
        PhysicsServer.BodySetMode(configured.GetRID(), PhysicsServer.BodyMode.RigidLinear);
        var disabled = scenes[0]; disabled.DisableMode = CollisionDisableMode.MakeStatic; disabled.ProcessMode = ProcessMode.Disabled;
        Check(PhysicsServer.BodyGetMode(disabled.GetRID()) == PhysicsServer.BodyMode.Static,
            "MakeStatic takes precedence over direct server motion");
        disabled.ProcessMode = ProcessMode.Inherit;
        Check(PhysicsServer.BodyGetMode(disabled.GetRID()) == PhysicsServer.BodyMode.RigidLinear,
            "Re-enable restores the retained server mode");
        Collect(); _ = GC.GetAllocatedBytesForCurrentThread(); _ = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 64; i++) tree.PhysicsFrame(1d / 60);
        var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 64; i++) tree.PhysicsFrame(1d / 60);
        own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(own == 0 && all == 0, $"Server mode full frames allocated {own}/{all} owner/all-thread B");
        if (solver is not null)
        {
            solver.IdentityHook = id =>
            {
                var mode = PhysicsServer.BodyGetMode(id);
                Reject<InvalidOperationException>(() => PhysicsServer.BodySetMode(id, PhysicsServer.BodyMode.Static));
                Reject<InvalidOperationException>(() => configured.LinearVelocity = Vector2.Zero);
                Reject<InvalidOperationException>(() => configured.AngularVelocity = 0);
                Reject<InvalidOperationException>(() => configured.Sleeping = true);
                Check(PhysicsServer.BodyGetMode(id) == mode, "Numerical mode writes reject before changing retained configuration");
                if (id == scenes[4].GetRID())
                {
                    var bone = (PhysicalBone)scenes[4];
                    Reject<InvalidOperationException>(() => bone.SimulatePhysics = true);
                    Reject<InvalidOperationException>(() => bone.BoneIndex = 0);
                    Reject<InvalidOperationException>(() => bone.BoneNodePath = "missing");
                }
            };
            PhysicsServer.BodyAttachCanvasInstanceID(scenes[0].GetRID(), 17);
            PhysicsServer.BodyAttachCanvasInstanceID(scenes[4].GetRID(), 17); solver.IdentityHook = null;
        }
        Task.Run(() => Reject<InvalidOperationException>(() => PhysicsServer.BodySetMode(scenes[0].GetRID(), PhysicsServer.BodyMode.Static))).GetAwaiter().GetResult();
        Console.WriteLine("Registered server modes: all scene siblings, detached configuration, live dynamic/static response and state publication passed.");
    }
}
