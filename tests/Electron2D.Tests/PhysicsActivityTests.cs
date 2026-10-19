using Electron2D;

internal sealed class PhysicsActivityTests(string backendName)
{
    internal static void Run(string backendName = "CPU") => new PhysicsActivityTests(backendName).RunCore();

    private void RunCore()
    {
        _ = GC.GetAllocatedBytesForCurrentThread(); _ = GC.GetTotalAllocatedBytes(true);
        VerifyExplicitPause();
        VerifyGlobalAndLocalPolicy();
        VerifyScenePause();
        Console.WriteLine($"Physics activation {backendName}: global/local suspension, callbacks, retained spring, pending force and 64 full two-world cycles at 0/0 owner/all-thread B passed.");
    }

    private sealed class PhysicsTicker : Node
    {
        internal int Calls;
        internal PhysicsTicker() => PhysicsProcessEnabled = true;
        protected override void OnPhysicsProcess(double delta) { Calls++; base.OnPhysicsProcess(delta); }
    }

    private static RID MovingBody(RID space, float x = 0)
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

    private void VerifyExplicitPause()
    {
        var space = PhysicsServer.SpaceCreate(backendName); var body = MovingBody(space); var shape = PhysicsServer.CircleShapeCreate();
        using (var initialShapeData = new CircleShape()) PhysicsServer.ShapeSetData(shape, initialShapeData);
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
            using var point = new PhysicsPointQueryParameters { Position = Vector2.Zero };
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

    private static void SuspensionInterval(RID first, RID second)
    {
        PhysicsServer.SetActive(false); PhysicsServer.SpaceStep(first, 1d / 120);
        PhysicsServer.SpaceSetActive(second, false); PhysicsServer.SetActive(true);
        PhysicsServer.SpaceStep(first, 1d / 120); PhysicsServer.SpaceStep(second, 1d / 120);
        PhysicsServer.SpaceSetActive(second, true); PhysicsServer.SpaceStep(second, 1d / 120);
        _ = PhysicsServer.SpaceIsActive(first);
    }

    private void VerifyGlobalAndLocalPolicy()
    {
        var first = PhysicsServer.SpaceCreate(backendName); var second = PhysicsServer.SpaceCreate(backendName);
        var a = MovingBody(first); var b = MovingBody(second);
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
            // Prepare the same suspension/resume paths that the allocation interval exercises.
            for (var step = 0; step < 64; step++) SuspensionInterval(first, second);
            var active = PhysicsServer.GetProcessInfo(PhysicsServer.ProcessInfo.ActiveObjects);
            var firstTick = PhysicsServer.SpaceGetTick(first); var secondTick = PhysicsServer.SpaceGetTick(second);
            PhysicsServer.SpaceSetActive(second, false);
            Check(active == 2 && PhysicsServer.GetProcessInfo(PhysicsServer.ProcessInfo.ActiveObjects) == 1,
                "Local deactivation removes exactly that world's completed statistics.");
            PhysicsServer.SetActive(false); PhysicsServer.SpaceSetActive(second, true);
            Check(PhysicsServer.GetProcessInfo(PhysicsServer.ProcessInfo.ActiveObjects) == active &&
                PhysicsServer.SpaceGetTick(first) == firstTick && PhysicsServer.SpaceGetTick(second) == secondTick,
                "Local resume restores its completed statistics without stepping, including global suspension.");
            PhysicsServer.SetActive(true);
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
            for (var step = 0; step < 64; step++) SuspensionInterval(first, second);
            var firstMeasuredTick = PhysicsServer.SpaceGetTick(first); var secondMeasuredTick = PhysicsServer.SpaceGetTick(second);
            var firstPosition = PhysicsServer.BodyGetTransform(a).Origin.X; var secondPosition = PhysicsServer.BodyGetTransform(b).Origin.X;
            var before = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var step = 0; step < 64; step++) SuspensionInterval(first, second);
            var bytes = GC.GetAllocatedBytesForCurrentThread() - before; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(bytes == 0 && all == 0 && PhysicsServer.SpaceGetTick(first) == firstMeasuredTick + 64 &&
                PhysicsServer.SpaceGetTick(second) == secondMeasuredTick + 64 && PhysicsServer.BodyGetTransform(a).Origin.X > firstPosition &&
                PhysicsServer.BodyGetTransform(b).Origin.X > secondPosition,
                $"Warmed global/local switches retain two moving worlds and exact ticks, owner/all={bytes}/{all} B.");
        }
        finally
        {
            PhysicsServer.SetActive(true);
            PhysicsServer.FreeRID(a); PhysicsServer.FreeRID(b); PhysicsServer.FreeRID(first); PhysicsServer.FreeRID(second);
        }
    }

    private void VerifyScenePause()
    {
        using var geometry = new CircleShape { Radius = 5 };
        using var world = new World(backendName);
        using var root = new SubViewport { World = world }; var ticker = new PhysicsTicker(); root.AddChild(ticker); var anchor = new StaticBody { Name = "Anchor" };
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
        var space = body.GetWorld()!.Space;
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
