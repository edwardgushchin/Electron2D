using Electron2D;

public static partial class BackendChecks
{
    private static void StateChecks()
    {
        Solver? solver = null;
        PhysicsServerManager.RegisterServer("StateChecks", () => solver = new());
        using var world = new World("StateChecks");
        using var root = new SubViewport { World = world };
        using var circle = new CircleShape { Radius = 3 };
        var scene = new RigidBody { GravityScale = 0 };
        scene.AddChild(new CollisionShape { Shape = circle }); root.AddChild(scene);
        _ = world.Space;
        solver!.IdentityHook = RejectSceneWrites;
        using var tree = new SceneTree(root);
        solver.IdentityHook = null;
        Check(scene.CanSleep, "Initial numerical hook cannot change configured sleep eligibility");
        solver.IdentityHook = RejectSceneWrites;
        PhysicsServer.BodyAttachCanvasInstanceID(scene.GetRID(), 17); solver.IdentityHook = null;
        Check(scene.CanSleep, "Live numerical hook cannot change configured sleep eligibility");
        PhysicsBody[] siblings = [scene, new StaticBody(), new AnimatableBody(), new CharacterBody(), new PhysicalBone()];
        foreach (var body in siblings)
        {
            if (body == scene) root.RemoveChild(body);
            else body.AddChild(new CollisionShape { Shape = circle });
            var rid = body.GetRID();
            PhysicsServer.BodySetMode(rid, PhysicsServer.BodyMode.Rigid);
            PhysicsServer.BodySetSleeping(rid, true);
            Check(PhysicsServer.BodyGetSleeping(rid), "Detached direct dynamic profile accepts sleep");
            PhysicsServer.BodySetCanSleep(rid, false);
            Check(!PhysicsServer.BodyGetSleeping(rid) && !PhysicsServer.BodyGetCanSleep(rid),
                "Disabling sleep wakes every detached direct scene profile");
            root.AddChild(body);
            Check(!PhysicsServer.BodyGetSleeping(rid), "Reentry preserves the authored wake request");
            solver.IdentityHook = _ => Reject<InvalidOperationException>(() => body.GlobalTransform = new(0, Vector2.One));
            PhysicsServer.BodyAttachCanvasInstanceID(rid, 19); solver.IdentityHook = null;
            PhysicsServer.BodySetCanSleep(rid, true);
            PhysicsServer.BodySetSleeping(rid, true);
            PhysicsServer.BodySetCanSleep(rid, false);
            Check(!PhysicsServer.BodyGetSleeping(rid), "Disabling sleep wakes every attached direct scene profile");
            CheckState(rid);
            PhysicsServer.BodySetMode(rid, PhysicsServer.BodyMode.Static);
            PhysicsServer.BodySetCanSleep(rid, true);
            PhysicsServer.BodySetCanSleep(rid, false);
            Check(PhysicsServer.BodyGetMode(rid) == PhysicsServer.BodyMode.Static, "Sleep eligibility preserves static mode");
            PhysicsServer.BodySetMode(rid, PhysicsServer.BodyMode.Rigid);
        }
        var raw = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.BodyAddShape(raw, circle.GetRID());
            solver.IdentityHook = ReadBorrowedState;
            PhysicsServer.BodySetSpace(raw, world.Space);
            solver.IdentityHook = null;
            CheckState(raw);
        }
        finally { PhysicsServer.FreeRID(raw); }
        var callbacks = 0;
        PhysicsServer.BodySetStateSyncCallback(scene.GetRID(), state =>
        {
            state.Transform = new(0, new Vector2(30, 40));
            scene.GlobalPosition = new(50, 60);
            scene.CanSleep = false;
            callbacks++;
        });
        tree.PhysicsFrame(1d / 60);
        PhysicsServer.BodySetStateSyncCallback(scene.GetRID(), null);
        Check(callbacks == 1 && scene.GlobalPosition == new Vector2(50, 60) && !scene.CanSleep,
            "Ordinary state callback retains direct-view and scene pose/sleep writes");
        Console.WriteLine("Registered state checks: all five scene/raw state pairs, non-preparing reads, numerical pose/sleep guards and detached/live wake passed.");

        void CheckState(RID rid)
        {
            PhysicsServer.BodySetTransform(rid, new(0.25f, new Vector2(10, 20)));
            PhysicsServer.BodySetLinearVelocity(rid, new(2, 3));
            PhysicsServer.BodySetAngularVelocity(rid, 4);
            PhysicsServer.BodySetSleeping(rid, false);
            PhysicsServer.BodySetCanSleep(rid, false);
            var prepared = solver.StatePreparations;
            Check(PhysicsServer.BodyGetTransform(rid).Origin == new Vector2(10, 20) &&
                PhysicsServer.BodyGetLinearVelocity(rid) == new Vector2(2, 3) && PhysicsServer.BodyGetAngularVelocity(rid) == 4 &&
                !PhysicsServer.BodyGetSleeping(rid) && !PhysicsServer.BodyGetCanSleep(rid), "Typed state pairs expose actual selected state");
            Check(prepared == solver.StatePreparations, "All five state reads avoid numerical preparation");
        }
        void RejectSceneWrites(RID bodyRID)
        {
            Reject<InvalidOperationException>(() => scene.CanSleep = false);
            Reject<InvalidOperationException>(() => scene.Sleeping = true);
            Reject<InvalidOperationException>(() => scene.LinearVelocity = Vector2.One);
            Reject<InvalidOperationException>(() => scene.AngularVelocity = 1);
            Reject<InvalidOperationException>(() => scene.GlobalPosition = Vector2.One);
            Reject<InvalidOperationException>(() => scene.Transform = new(0, Vector2.One));
            Reject<InvalidOperationException>(() => scene.Rotation = 1);
            Reject<InvalidOperationException>(() => scene.Scale = Vector2.One * 2);
            Reject<InvalidOperationException>(() => scene.Skew = 1);
            Reject<InvalidOperationException>(() => scene.MoveLocalX(1));
            ReadBorrowedState(bodyRID);
        }
        void ReadBorrowedState(RID bodyRID)
        {
            var prepared = solver.StatePreparations;
            _ = PhysicsServer.BodyGetTransform(bodyRID);
            _ = PhysicsServer.BodyGetLinearVelocity(bodyRID);
            _ = PhysicsServer.BodyGetAngularVelocity(bodyRID);
            _ = PhysicsServer.BodyGetSleeping(bodyRID);
            _ = PhysicsServer.BodyGetCanSleep(bodyRID);
            Check(prepared == solver.StatePreparations, "Borrowed state reads avoid recursive numerical preparation");
        }
    }
}
