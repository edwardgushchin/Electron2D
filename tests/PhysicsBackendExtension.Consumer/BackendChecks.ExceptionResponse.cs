using Electron2D;

public static partial class BackendChecks
{
    private static void ExceptionResponse(SceneTree tree, SubViewport root, World world, RigidBody scene, StaticBody peer, RID raw, CircleShape circle, Solver? solver)
    {
        var target = PhysicsServer.BodyCreate(); var sensor = PhysicsServer.AreaCreate();
        using var area = new Area { Position = peer.Position };
        area.AddChild(new CollisionShape { Shape = circle }); root.AddChild(area);
        var sceneEntered = 0; var rawEntered = 0;
        area.BodyEntered += body => { if (ReferenceEquals(body, scene)) sceneEntered++; };
        using var point = new PhysicsPointQueryParameters { Position = peer.Position, CollideWithAreas = true };
        var hits = new PhysicsPointResult[8];
        using var forward = new PhysicsTestMotionParameters { Motion = new(60, 0) };
        using var reverse = new PhysicsTestMotionParameters { From = new(0, peer.Position), Motion = new(-60, 0) };
        try
        {
            PhysicsServer.BodyAddShape(target, circle.GetRID()); PhysicsServer.BodySetMode(target, PhysicsServer.BodyMode.Static);
            PhysicsServer.BodySetTransform(target, new(0, new Vector2(30, 100))); PhysicsServer.BodySetSpace(target, world.Space);
            PhysicsServer.AreaAddShape(sensor, circle.GetRID()); PhysicsServer.AreaSetTransform(sensor, new(0, new Vector2(30, 100)));
            PhysicsServer.AreaSetSpace(sensor, world.Space);
            PhysicsServer.AreaSetMonitorCallback(sensor, (status, body, _, _, _) => { if (status == PhysicsServer.AreaBodyStatus.Added && body == raw) rawEntered++; });
            var movers = new[] { scene.GetRID(), raw }; var targets = new[] { peer.GetRID(), target };
            foreach (var body in movers)
            {
                PhysicsServer.BodySetLinearDampMode(body, RigidBody.DampMode.Replace); PhysicsServer.BodySetLinearDamp(body, 0);
                PhysicsServer.BodySetAngularDampMode(body, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDamp(body, 0);
                PhysicsServer.BodySetMaxContactsReported(body, 4);
            }
            Reset();
            for (var i = 0; i < movers.Length; i++)
            {
                PhysicsServer.BodyAddCollisionException(movers[i], targets[i]); PhysicsServer.BodyAddCollisionException(movers[i], targets[i]);
                forward.From = new(0, new Vector2(0, i * 100));
                Check(!PhysicsServer.BodyTestMotion(movers[i], forward), "An immediate one-sided exception suppresses body motion before the next step");
                reverse.From = new(0, new Vector2(30, i * 100));
                Check(!PhysicsServer.BodyTestMotion(targets[i], reverse), "The same exception suppresses reverse body motion");
            }
            Check(scene.GetCollisionExceptions() is [var listed] && ReferenceEquals(listed, peer), "Duplicate scene/server writes preserve one ordered entry");
            var count = world.DirectSpaceState.IntersectPoint(point, hits);
            var found = false; for (var i = 0; i < count; i++) found |= hits[i].ColliderRID == peer.GetRID();
            Check(found, "Ordinary direct-space queries still see an excepted solid body");
            Step(50);
            foreach (var body in movers)
                Check(PhysicsServer.BodyGetTransform(body).Origin.X > 40 && PhysicsServer.BodyGetDirectState(body)!.GetContactCount() == 0,
                    "Selected numerical response lets excepted scene/raw bodies cross their peers without solid contacts");
            Check(sceneEntered > 0 && rawEntered > 0, "Scene and raw Area sensing remain active while solid collision exceptions apply");
            for (var i = 0; i < movers.Length; i++)
            {
                PhysicsServer.BodyRemoveCollisionException(movers[i], targets[i]); PhysicsServer.BodyRemoveCollisionException(movers[i], targets[i]);
                forward.From = new(0, new Vector2(0, i * 100));
                Check(PhysicsServer.BodyTestMotion(movers[i], forward), "Removing the final exception immediately restores motion contacts");
            }
            Reset(); Step(40);
            for (var i = 0; i < movers.Length; i++)
            {
                var state = PhysicsServer.BodyGetDirectState(movers[i])!; var x = PhysicsServer.BodyGetTransform(movers[i]).Origin.X;
                Check(x is > 23 and < 25.1f && MathF.Abs(state.LinearVelocity.X) < .01f && state.GetContactCount() > 0 && state.GetContactCollider(0) == targets[i],
                    "Restored circle response stops both routes within solver penetration allowance and reports the actual peer");
                PhysicsServer.BodySetCanSleep(movers[i], true); PhysicsServer.BodySetSleeping(movers[i], true);
                Check(PhysicsServer.BodyGetSleeping(movers[i]), "The body is sleeping before editing its exception");
                PhysicsServer.BodyAddCollisionException(movers[i], targets[i]);
                Check(!PhysicsServer.BodyGetSleeping(movers[i]), "Exception edits immediately wake an eligible body");
                PhysicsServer.BodySetSleeping(movers[i], true); PhysicsServer.BodyAddCollisionException(movers[i], targets[i]);
                Check(!PhysicsServer.BodyGetSleeping(movers[i]), "Duplicate exception writes still request wakeup");
                PhysicsServer.BodySetSleeping(movers[i], true); PhysicsServer.BodyRemoveCollisionException(movers[i], default);
                Check(!PhysicsServer.BodyGetSleeping(movers[i]), "Absent exception removals still request wakeup");
                PhysicsServer.BodySetCanSleep(movers[i], false); PhysicsServer.BodySetLinearVelocity(movers[i], new(60, 0));
            }
            Step(20);
            foreach (var body in movers) Check(PhysicsServer.BodyGetTransform(body).Origin.X > 40, "Editing an already touching pair releases its next physical response");
            for (var i = 0; i < movers.Length; i++) PhysicsServer.BodyRemoveCollisionException(movers[i], targets[i]);
            foreach (var body in movers) PhysicsServer.BodySetLinearVelocity(body, Vector2.Zero);
        }
        finally { PhysicsServer.FreeRID(sensor); PhysicsServer.FreeRID(target); }

        void Reset()
        {
            PhysicsServer.BodySetTransform(scene.GetRID(), new(0, Vector2.Zero)); PhysicsServer.BodySetTransform(raw, new(0, new Vector2(0, 100)));
            PhysicsServer.BodySetLinearVelocity(scene.GetRID(), new(60, 0)); PhysicsServer.BodySetLinearVelocity(raw, new(60, 0));
        }
        void Step(int count) { for (var i = 0; i < count; i++) tree.PhysicsFrame(1d / 60); }
    }
}
