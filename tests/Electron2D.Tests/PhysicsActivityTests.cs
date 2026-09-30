using Electron2D;

internal static class PhysicsActivityTests
{
    internal static void Run()
    {
        VerifyExplicitPause();
        VerifyGlobalAndLocalPolicy();
        VerifyScenePause();
        Console.WriteLine("Physics world activation, global/local suspension, callbacks, pending force and allocation checks passed.");
    }

    private sealed class PhysicsTicker : Node
    {
        internal int Calls;
        internal PhysicsTicker() => PhysicsProcessEnabled = true;
        protected override void OnPhysicsProcess(double delta) { Calls++; base.OnPhysicsProcess(delta); }
    }

    private static RID MovingBody(PhysicsServer server, RID space, float x = 0)
    {
        var body = server.BodyCreate();
        server.BodySetTransform(body, new(0, Vector2.One, 0, new(x, 0)));
        server.BodySetGravityScale(body, 0);
        server.BodySetLinearDampMode(body, RigidBody.DampMode.Replace);
        server.BodySetAngularDampMode(body, RigidBody.DampMode.Replace);
        server.BodySetLinearVelocity(body, new(60, 0));
        server.BodySetSpace(body, space);
        return body;
    }

    private static void VerifyExplicitPause()
    {
        var server = PhysicsServer.Instance;
        var space = server.SpaceCreate(); var body = MovingBody(server, space); var shape = server.CircleShapeCreate();
        var calls = 0;
        try
        {
            Check(!server.SpaceIsActive(space), "An explicit space starts inactive.");
            server.BodyAddShape(body, shape);
            server.BodySetForceIntegrationCallback(body, _ => calls++);
            Reject<ArgumentOutOfRangeException>(() => server.SpaceStep(space, double.NaN));
            Reject<InvalidOperationException>(() => Task.Run(() => server.SpaceStep(space, 2)).GetAwaiter().GetResult());
            server.SpaceStep(space, 2);
            Check(server.BodyGetTransform(body).Origin == Vector2.Zero && calls == 0,
                "An inactive explicit space does not move bodies or deliver integration callbacks.");
            using var point = new PhysicsPointQueryParameters2D { Position = Vector2.Zero };
            Check(server.SpaceGetDirectState(space).IntersectPoint(point).Any(hit => hit.ColliderRID == body),
                "Direct queries prepare and expose geometry while simulation is inactive.");
            server.SpaceSetActive(space, true); server.SpaceStep(space, 1d / 60);
            Check(MathF.Abs(server.BodyGetTransform(body).Origin.X - 1) < 0.001f && calls == 1,
                "Explicit activation advances the existing world, without skipped-time catchup.");
            var state = server.BodyGetDirectState(body)!; var lastStep = state.Step; var pausedVelocity = state.LinearVelocity;
            server.SpaceSetActive(space, false);
            server.BodyApplyCentralForce(body, new(120, 0));
            for (var step = 0; step < 100; step++) server.SpaceStep(space, 1);
            Check(calls == 1 && state.LinearVelocity == pausedVelocity && state.Step == lastStep,
                "Suspension retains velocity, view lifetime, previous interval and pending forces.");
            server.BodySetTransform(body, new(0, Vector2.One, 0, new(20, 0))); point.Position = new(20, 0);
            Check(server.SpaceGetDirectState(space).IntersectPoint(point).Any(hit => hit.ColliderRID == body),
                "Explicit configuration and queries remain live during suspension.");
            server.SpaceSetActive(space, true); server.SpaceStep(space, 1d / 60);
            Check(MathF.Abs(state.LinearVelocity.X - 62) < 0.001f && calls == 2 &&
                  server.BodyGetTransform(body).Origin.X is > 21 and < 21.1f,
                "Reactivation integrates a queued force once using only its current delta.");
            server.SpaceStep(space, 1d / 60);
            Check(MathF.Abs(state.LinearVelocity.X - 62) < 0.001f, "The resumed pending force is not replayed.");
            Reject<InvalidOperationException>(() => Task.Run(() => server.SpaceSetActive(space, false)).GetAwaiter().GetResult());
            Reject<InvalidOperationException>(() => Task.Run(() => server.SpaceIsActive(space)).GetAwaiter().GetResult());
            Reject<ArgumentException>(() => server.SpaceSetActive(body, true));
            Check(server.SpaceIsActive(space), "Rejected world access preserves the active policy.");
            server.SpaceSetActive(space, false); server.FreeRID(body); body = default;
            Check(server.SpaceIsActive(space) == false, "Inactive worlds still permit body cleanup.");
        }
        finally
        {
            if (body.IsValid()) server.FreeRID(body);
            server.FreeRID(shape); server.FreeRID(space);
        }
        Reject<ArgumentException>(() => server.SpaceIsActive(space));
    }

    private static void VerifyGlobalAndLocalPolicy()
    {
        var server = PhysicsServer.Instance;
        var first = server.SpaceCreate(); var second = server.SpaceCreate();
        var a = MovingBody(server, first); var b = MovingBody(server, second);
        try
        {
            server.SpaceSetActive(first, true); server.SpaceSetActive(second, false);
            Task.Run(() => server.SetActive(false)).GetAwaiter().GetResult();
            server.SpaceStep(first, 100); server.SpaceStep(second, 100);
            Check(server.SpaceIsActive(first) && !server.SpaceIsActive(second) &&
                  server.BodyGetTransform(a).Origin == Vector2.Zero && server.BodyGetTransform(b).Origin == Vector2.Zero,
                "The atomic global gate suspends both worlds without overwriting local policy.");
            server.SetActive(true); server.SpaceStep(first, 1d / 60); server.SpaceStep(second, 1d / 60);
            Check(server.BodyGetTransform(a).Origin.X > 0.9f && server.BodyGetTransform(b).Origin == Vector2.Zero,
                "Global resume preserves a locally inactive world.");
            server.SpaceSetActive(second, true);
            var beforeA = server.BodyGetTransform(a); var beforeB = server.BodyGetTransform(b);
            server.BodySetForceIntegrationCallback(a, _ => server.SetActive(false));
            server.SpaceStep(first, 1d / 60); server.SpaceStep(second, 1d / 60);
            Check(server.BodyGetTransform(a).Origin.X > beforeA.Origin.X && server.BodyGetTransform(b) == beforeB,
                "A callback global toggle affects the next world boundary after the current interval completes.");
            server.BodySetForceIntegrationCallback(a, _ => server.SpaceSetActive(first, false));
            server.SetActive(true); server.SpaceStep(first, 1d / 60);
            Check(!server.SpaceIsActive(first), "A post-solver callback can change local policy for later intervals.");
            server.BodySetForceIntegrationCallback(a, _ =>
            {
                server.SpaceSetActive(first, false);
                throw new InvalidOperationException("Activity callback probe.");
            });
            server.SpaceSetActive(first, true); var committed = server.BodyGetTransform(a);
            Reject<AggregateException>(() => server.SpaceStep(first, 1d / 60));
            Check(!server.SpaceIsActive(first) && server.BodyGetTransform(a).Origin.X > committed.Origin.X,
                "A failing post-solver callback preserves the completed solve and committed activation policy.");
            server.BodySetForceIntegrationCallback(a, null);
            server.SpaceSetActive(first, true);
            for (var step = 0; step < 64; step++) { server.SpaceStep(first, 1d / 120); server.SpaceStep(second, 1d / 120); }
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var step = 0; step < 64; step++)
            {
                server.SetActive(false); server.SpaceStep(first, 1d / 120);
                server.SpaceSetActive(second, false); server.SetActive(true);
                server.SpaceStep(first, 1d / 120); server.SpaceStep(second, 1d / 120);
                server.SpaceSetActive(second, true); server.SpaceStep(second, 1d / 120);
                _ = server.SpaceIsActive(first);
            }
            Check(GC.GetAllocatedBytesForCurrentThread() - before == 0,
                "Warmed global/local switches, skipped intervals and active native frames allocate no managed bytes.");
        }
        finally
        {
            server.SetActive(true);
            server.FreeRID(a); server.FreeRID(b); server.FreeRID(first); server.FreeRID(second);
        }
    }

    private static void VerifyScenePause()
    {
        var server = PhysicsServer.Instance;
        using var geometry = new CircleShape { Radius = 5 };
        var root = new Node(); var ticker = new PhysicsTicker(); root.AddChild(ticker); var anchor = new StaticBody { Name = "Anchor" };
        var body = new RigidBody
        {
            Name = "Body",
            Position = new(0, 50),
            GravityScale = 0,
            CanSleep = false,
            LinearDampMode = RigidBody.DampMode.Replace,
            AngularDampMode = RigidBody.DampMode.Replace
        };
        body.AddChild(new CollisionShape { Shape = geometry });
        var spring = new DampedSpringJoint { NodeA = "../Anchor", NodeB = "../Body", Length = 50, RestLength = 30, Damping = 0 };
        root.AddChild(anchor); root.AddChild(body); root.AddChild(spring);
        using var tree = new SceneTree(root);
        var space = body.GetWorld2D()!.Space;
        var frames = 0; var timedOut = false;
        tree.PhysicsFrameStarted += _ => frames++;
        using var timer = tree.CreateTimer(0.05, processInPhysics: true);
        timer.Timeout += _ => timedOut = true;
        try
        {
            Check(server.SpaceIsActive(space), "The SceneTree activates its own shared physics world.");
            server.SpaceSetActive(space, false);
            for (var step = 0; step < 10; step++) tree.PhysicsFrame(1d / 60);
            Check(body.Position == new Vector2(0, 50) && body.LinearVelocity == Vector2.Zero && frames == 10 && ticker.Calls == 10 && timedOut &&
                  server.JointGetType(spring.GetRID()) == PhysicsServer.JointType.DampedSpring,
                "Inactive scene simulation retains joint/body state while the scene frame/timer lanes continue.");
            server.SpaceSetActive(space, true); tree.PhysicsFrame(1d / 60);
            Check(body.LinearVelocity.Y < -6 && body.Position.Y < 50,
                "Scene reactivation executes the retained spring using only the resumed interval.");
            Action<CanvasItem> guard = _ => Reject<InvalidOperationException>(() => server.SpaceSetActive(space, false));
            body.NotifyLocalTransformChanges = true; body.LocalTransformChanged += guard;
            tree.PhysicsFrame(1d / 60); body.LocalTransformChanged -= guard;
            Check(server.SpaceIsActive(space), "In-solver local policy writes reject before changing the flag.");
            server.SetActive(false); var pose = body.GlobalTransform; var velocity = body.LinearVelocity;
            for (var step = 0; step < 10; step++) tree.PhysicsFrame(1d / 60);
            Check(body.GlobalTransform == pose && body.LinearVelocity == velocity && server.SpaceIsActive(space),
                "The global gate also suspends the scene world while retaining its local activation.");
            server.SetActive(true); tree.PhysicsFrame(1d / 60);
            Check(body.GlobalTransform != pose, "Global scene resume continues the same native connection.");
        }
        finally { server.SetActive(true); }
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
