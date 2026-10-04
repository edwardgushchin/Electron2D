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
        var body = PhysicsServer.BodyCreate();
        PhysicsServer.BodySetTransform(body, new(0, Vector2.One, 0, new(x, 0)));
        PhysicsServer.BodySetGravityScale(body, 0);
        PhysicsServer.BodySetLinearDampMode(body, RigidBody.DampMode.Replace);
        PhysicsServer.BodySetAngularDampMode(body, RigidBody.DampMode.Replace);
        PhysicsServer.BodySetLinearVelocity(body, new(60, 0));
        PhysicsServer.BodySetSpace(body, space);
        return body;
    }

    private static void VerifyExplicitPause()
    {
        var server = PhysicsServer.Service;
        var space = PhysicsServer.SpaceCreate(); var body = MovingBody(server, space); var shape = PhysicsServer.CircleShapeCreate();
        var calls = 0;
        try
        {
            Check(!PhysicsServer.SpaceIsActive(space), "An explicit space starts inactive.");
            PhysicsServer.BodyAddShape(body, shape);
            PhysicsServer.BodySetForceIntegrationCallback(body, _ => calls++);
            Reject<ArgumentOutOfRangeException>(() => PhysicsServer.SpaceStep(space, double.NaN));
            Reject<InvalidOperationException>(() => Task.Run(() => PhysicsServer.SpaceStep(space, 2)).GetAwaiter().GetResult());
            PhysicsServer.SpaceStep(space, 2);
            Check(PhysicsServer.BodyGetTransform(body).Origin == Vector2.Zero && calls == 0,
                "An inactive explicit space does not move bodies or deliver integration callbacks.");
            using var point = new PhysicsPointQueryParameters2D { Position = Vector2.Zero };
            Check(PhysicsServer.SpaceGetDirectState(space).IntersectPoint(point).Any(hit => hit.ColliderRID == body),
                "Direct queries prepare and expose geometry while simulation is inactive.");
            PhysicsServer.SpaceSetActive(space, true); PhysicsServer.SpaceStep(space, 1d / 60);
            Check(MathF.Abs(PhysicsServer.BodyGetTransform(body).Origin.X - 1) < 0.001f && calls == 1,
                "Explicit activation advances the existing world, without skipped-time catchup.");
            var state = PhysicsServer.BodyGetDirectState(body)!; var lastStep = state.Step; var pausedVelocity = state.LinearVelocity;
            PhysicsServer.SpaceSetActive(space, false);
            PhysicsServer.BodyApplyCentralForce(body, new(120, 0));
            for (var step = 0; step < 100; step++) PhysicsServer.SpaceStep(space, 1);
            Check(calls == 1 && state.LinearVelocity == pausedVelocity && state.Step == lastStep,
                "Suspension retains velocity, view lifetime, previous interval and pending forces.");
            PhysicsServer.BodySetTransform(body, new(0, Vector2.One, 0, new(20, 0))); point.Position = new(20, 0);
            Check(PhysicsServer.SpaceGetDirectState(space).IntersectPoint(point).Any(hit => hit.ColliderRID == body),
                "Explicit configuration and queries remain live during suspension.");
            PhysicsServer.SpaceSetActive(space, true); PhysicsServer.SpaceStep(space, 1d / 60);
            Check(MathF.Abs(state.LinearVelocity.X - 62) < 0.001f && calls == 2 &&
                  PhysicsServer.BodyGetTransform(body).Origin.X is > 21 and < 21.1f,
                "Reactivation integrates a queued force once using only its current delta.");
            PhysicsServer.SpaceStep(space, 1d / 60);
            Check(MathF.Abs(state.LinearVelocity.X - 62) < 0.001f, "The resumed pending force is not replayed.");
            Reject<InvalidOperationException>(() => Task.Run(() => PhysicsServer.SpaceSetActive(space, false)).GetAwaiter().GetResult());
            Reject<InvalidOperationException>(() => Task.Run(() => PhysicsServer.SpaceIsActive(space)).GetAwaiter().GetResult());
            Reject<ArgumentException>(() => PhysicsServer.SpaceSetActive(body, true));
            Check(PhysicsServer.SpaceIsActive(space), "Rejected world access preserves the active policy.");
            PhysicsServer.SpaceSetActive(space, false); PhysicsServer.FreeRID(body); body = default;
            Check(PhysicsServer.SpaceIsActive(space) == false, "Inactive worlds still permit body cleanup.");
        }
        finally
        {
            if (body.IsValid()) PhysicsServer.FreeRID(body);
            PhysicsServer.FreeRID(shape); PhysicsServer.FreeRID(space);
        }
        Reject<ArgumentException>(() => PhysicsServer.SpaceIsActive(space));
    }

    private static void VerifyGlobalAndLocalPolicy()
    {
        var server = PhysicsServer.Service;
        var first = PhysicsServer.SpaceCreate(); var second = PhysicsServer.SpaceCreate();
        var a = MovingBody(server, first); var b = MovingBody(server, second);
        try
        {
            PhysicsServer.SpaceSetActive(first, true); PhysicsServer.SpaceSetActive(second, false);
            Task.Run(() => PhysicsServer.SetActive(false)).GetAwaiter().GetResult();
            PhysicsServer.SpaceStep(first, 100); PhysicsServer.SpaceStep(second, 100);
            Check(PhysicsServer.SpaceIsActive(first) && !PhysicsServer.SpaceIsActive(second) &&
                  PhysicsServer.BodyGetTransform(a).Origin == Vector2.Zero && PhysicsServer.BodyGetTransform(b).Origin == Vector2.Zero,
                "The atomic global gate suspends both worlds without overwriting local policy.");
            PhysicsServer.SetActive(true); PhysicsServer.SpaceStep(first, 1d / 60); PhysicsServer.SpaceStep(second, 1d / 60);
            Check(PhysicsServer.BodyGetTransform(a).Origin.X > 0.9f && PhysicsServer.BodyGetTransform(b).Origin == Vector2.Zero,
                "Global resume preserves a locally inactive world.");
            PhysicsServer.SpaceSetActive(second, true);
            var beforeA = PhysicsServer.BodyGetTransform(a); var beforeB = PhysicsServer.BodyGetTransform(b);
            PhysicsServer.BodySetForceIntegrationCallback(a, _ => PhysicsServer.SetActive(false));
            PhysicsServer.SpaceStep(first, 1d / 60); PhysicsServer.SpaceStep(second, 1d / 60);
            Check(PhysicsServer.BodyGetTransform(a).Origin.X > beforeA.Origin.X && PhysicsServer.BodyGetTransform(b) == beforeB,
                "A callback global toggle affects the next world boundary after the current interval completes.");
            PhysicsServer.BodySetForceIntegrationCallback(a, _ => PhysicsServer.SpaceSetActive(first, false));
            PhysicsServer.SetActive(true); PhysicsServer.SpaceStep(first, 1d / 60);
            Check(!PhysicsServer.SpaceIsActive(first), "A post-solver callback can change local policy for later intervals.");
            PhysicsServer.BodySetForceIntegrationCallback(a, _ =>
            {
                PhysicsServer.SpaceSetActive(first, false);
                throw new InvalidOperationException("Activity callback probe.");
            });
            PhysicsServer.SpaceSetActive(first, true); var committed = PhysicsServer.BodyGetTransform(a);
            Reject<AggregateException>(() => PhysicsServer.SpaceStep(first, 1d / 60));
            Check(!PhysicsServer.SpaceIsActive(first) && PhysicsServer.BodyGetTransform(a).Origin.X > committed.Origin.X,
                "A failing post-solver callback preserves the completed solve and committed activation policy.");
            PhysicsServer.BodySetForceIntegrationCallback(a, null);
            PhysicsServer.SpaceSetActive(first, true);
            for (var step = 0; step < 64; step++) { PhysicsServer.SpaceStep(first, 1d / 120); PhysicsServer.SpaceStep(second, 1d / 120); }
            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var step = 0; step < 64; step++)
            {
                PhysicsServer.SetActive(false); PhysicsServer.SpaceStep(first, 1d / 120);
                PhysicsServer.SpaceSetActive(second, false); PhysicsServer.SetActive(true);
                PhysicsServer.SpaceStep(first, 1d / 120); PhysicsServer.SpaceStep(second, 1d / 120);
                PhysicsServer.SpaceSetActive(second, true); PhysicsServer.SpaceStep(second, 1d / 120);
                _ = PhysicsServer.SpaceIsActive(first);
            }
            Check(GC.GetAllocatedBytesForCurrentThread() - before == 0,
                "Warmed global/local switches, skipped intervals and active native frames allocate no managed bytes.");
        }
        finally
        {
            PhysicsServer.SetActive(true);
            PhysicsServer.FreeRID(a); PhysicsServer.FreeRID(b); PhysicsServer.FreeRID(first); PhysicsServer.FreeRID(second);
        }
    }

    private static void VerifyScenePause()
    {
        var server = PhysicsServer.Service;
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
            Check(PhysicsServer.SpaceIsActive(space), "The SceneTree activates its own shared physics world.");
            PhysicsServer.SpaceSetActive(space, false);
            for (var step = 0; step < 10; step++) tree.PhysicsFrame(1d / 60);
            Check(body.Position == new Vector2(0, 50) && body.LinearVelocity == Vector2.Zero && frames == 10 && ticker.Calls == 10 && timedOut &&
                  PhysicsServer.JointGetType(spring.GetRID()) == PhysicsServer.JointType.DampedSpring,
                "Inactive scene simulation retains joint/body state while the scene frame/timer lanes continue.");
            PhysicsServer.SpaceSetActive(space, true); tree.PhysicsFrame(1d / 60);
            Check(body.LinearVelocity.Y < -6 && body.Position.Y < 50,
                "Scene reactivation executes the retained spring using only the resumed interval.");
            Action<CanvasItem> guard = _ => Reject<InvalidOperationException>(() => PhysicsServer.SpaceSetActive(space, false));
            body.NotifyLocalTransformChanges = true; body.LocalTransformChanged += guard;
            tree.PhysicsFrame(1d / 60); body.LocalTransformChanged -= guard;
            Check(PhysicsServer.SpaceIsActive(space), "In-solver local policy writes reject before changing the flag.");
            PhysicsServer.SetActive(false); var pose = body.GlobalTransform; var velocity = body.LinearVelocity;
            for (var step = 0; step < 10; step++) tree.PhysicsFrame(1d / 60);
            Check(body.GlobalTransform == pose && body.LinearVelocity == velocity && PhysicsServer.SpaceIsActive(space),
                "The global gate also suspends the scene world while retaining its local activation.");
            PhysicsServer.SetActive(true); tree.PhysicsFrame(1d / 60);
            Check(body.GlobalTransform != pose, "Global scene resume continues the same native connection.");
        }
        finally { PhysicsServer.SetActive(true); }
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
