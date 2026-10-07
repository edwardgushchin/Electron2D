using Electron2D;
using Electron2D.Examples.PhysicsSandbox;
using SDL = SDL3.SDL;

internal static partial class PhysicsSandboxTests
{
    internal static void Run()
    {
        PhysicsMassProfileTests.Run();
        PhysicsShapeQueryTests.Run();
        CharacterBodyTests.Run();
        VerifySolverArrayReuse();
        VerifySolverVectorArithmetic();
        VerifySleepingStorage();
        VerifyBroadphasePairReuse();
        using var font = new FontFile();
        font.LoadDynamicFont(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "IBMPlexSans-Regular.ttf"));
        var previousBudget = Engine.MaxPhysicsStepsPerFrame;
        using (var configured = new SandboxWindow(font, font))
            Check(configured.Size == new Vector2i(1152, 800) && configured.Unresizable && configured.MinSize == configured.MaxSize && Engine.MaxPhysicsStepsPerFrame == 1, "Splash client dimensions and responsive fixed-step frame budget.");
        Check(Engine.MaxPhysicsStepsPerFrame == previousBudget, "Window teardown restores the engine physics-frame budget.");
        using var root = new SubViewport { Size = SandboxWindow.ClientSize };
        using var tree = new SceneTree(root);
        PhysicsScene? current = null;
        void Switch(int index)
        {
            if (current is not null) { root.RemoveChild(current); current.Dispose(); }
            current = new PhysicsScene(index, font);
            root.AddChild(current);
            if (Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_GPU_SOLVER") == "1")
                current.Colliders.OfType<PhysicsBody>().First().Space!.EnableGPUSolver();
        }
        for (var index = 0; index < SandboxWindow.SceneNames.Length; index++)
        {
            Switch(index);
            var scene = current!;
            var initial = scene.BodyCount;
            if (index == 11) Console.WriteLine("Smash: standard wall attached; checking collision propagation.");
            if (index == 11) Check(initial == PhysicsScene.SmashDefaultCount + 1 && scene.WorldGravity == 0 && scene.Bodies[0].LinearVelocity.X == 600 * PhysicsScene.SmashScale, "Smash starts a real heavy block against 9,600 fragments in zero gravity.");
            Check(scene.SelectedBody is null, "Every new story starts without selection: " + index);
            Check(scene.GetChildren().All(n => n.Name != "PhysicsDebug"), "Stories have no diagnostic collision layer.");
            Check(PhysicsServer.AreaGetGravity(scene.GetWorld()!.Space) == scene.WorldGravity &&
                PhysicsServer.AreaGetLinearDamp(scene.GetWorld()!.Space) == scene.WorldLinearDamp &&
                PhysicsServer.AreaGetAngularDamp(scene.GetWorld()!.Space) == scene.WorldAngularDamp, "Story parameters configure actual world defaults.");
            for (var i = 0; i < 180; i++) { PhysicsTick(tree); tree.ProcessFrame(1d / 60); }
            Check(scene.PhysicsSteps == 180 && scene.Bodies.All(b => b.Position.IsFinite() && b.LinearVelocity.IsFinite()), "Every story advances finite real physics: " + index);
            if (index == 11) Check(scene.Score > 50 && scene.ContactEvents > 30, "The heavy block scatters fragments through real contact propagation.");
            if (index == 0) Check(initial == 72 && scene.ContactEvents > 30, "Warehouse starts with 72 crates and real collision callbacks.");
            for (var action = 0; action < 3; action++)
            {
                scene.Act(action);
                for (var i = 0; i < 20; i++) { PhysicsTick(tree); tree.ProcessFrame(1d / 60); }
            }
            Check(scene.SelectedBody is null, "Story actions do not select objects: " + index);
            if (index == 0) Check(scene.BodyCount == 74, "Crate and projectile actions create real bodies.");
            tree.ProcessFrame(1d / 60);
            scene.Running = false;
            var steps = scene.PhysicsSteps;
            var poses = scene.Bodies.Select(b => b.GlobalTransform).ToArray();
            PhysicsTick(tree); PhysicsTick(tree);
            Check(scene.PhysicsSteps == steps && scene.Bodies.Select(b => b.GlobalTransform).SequenceEqual(poses), "Pause retains body state: " + index);
            scene.StepOnce(); PhysicsTick(tree); PhysicsTick(tree);
            Check(scene.PhysicsSteps == steps + 1, "Single step advances exactly one interval: " + index);
            var rids = scene.Bodies.Select(b => b.GetRID()).Concat(scene.SmashFragments.Select(b => b.RID)).ToArray();
            Switch((index + 1) % SandboxWindow.SceneNames.Length);
            Check(scene.IsDisposed && scene.Bodies.All(b => b.IsDisposed), "Switch releases every scene node.");
            foreach (var rid in rids)
            {
                try { PhysicsServer.BodyGetDirectState(rid); throw new Exception("A departed scene RID remained usable."); }
                catch (ArgumentException) { }
            }
        }
        Switch(0);
        for (var i = 0; i < 180; i++) PhysicsTick(tree);
        var crate = current!.Bodies[17];
        var point = crate.Position;
        Pointer(root, point, true);
        Check(current.SelectedBody == crate, "Click selects the actual body.");
        Motion(root, point + new Vector2(80, -100));
        for (var i = 0; i < 30; i++) PhysicsTick(tree);
        tree.ProcessFrame(1d / 60);
        Check(crate.Position.DistanceTo(point) > 10, $"Grab spring moves a free top crate through the solver: {point} -> {crate.Position}; {current.Observation}");
        Pointer(root, point + new Vector2(80, -100), false);
        Key(root, Electron2D.Key.K);
        Check(crate.Freeze, "Selected body can freeze.");
        Key(root, Electron2D.Key.K);
        Check(!crate.Freeze, "A frozen selected body can resume.");
        Key(root, Electron2D.Key.H);
        Check(crate.Freeze && crate.FreezeMode == RigidFreezeMode.Kinematic, "Frozen kinematic mode is interactive.");
        Key(root, Electron2D.Key.K);
        Key(root, Electron2D.Key.C); Check(!crate.ConstantForce.IsZeroApprox(), "Persistent lift force toggles on.");
        Key(root, Electron2D.Key.C); Check(crate.ConstantForce.IsZeroApprox(), "Persistent force toggles off.");
        foreach (var policy in new[] { CollisionDisableMode.Remove, CollisionDisableMode.MakeStatic, CollisionDisableMode.KeepActive })
        {
            Key(root, Electron2D.Key.V); PhysicsTick(tree);
            Check(crate.ProcessMode == ProcessMode.Disabled && crate.DisableMode == policy, "Disabled participation cycle: " + policy);
        }
        Key(root, Electron2D.Key.V); PhysicsTick(tree); Check(crate.ProcessMode != ProcessMode.Disabled, "Disabled body restores.");
        var empty = new Vector2(1000, 230);
        Pointer(root, empty, true); Pointer(root, empty, false);
        Check(current.SelectedBody is null, "Empty field click clears selection.");
        Key(root, Electron2D.Key.K); Check(!crate.Freeze, "Cleared selection receives no body commands.");
        var count = current!.BodyCount;
        Pointer(root, new(60, 380), true);
        Pointer(root, new(60, 380), false);
        Key(root, Electron2D.Key.B);
        Check(current!.BodyCount == count + 1, "Unhandled keyboard actions reach the scene.");
        Switch(4);
        var courier = current!.Colliders.OfType<CharacterBody>().Single();
        var parcels = current.Areas.ToArray();
        foreach (var parcel in parcels)
        {
            courier.GlobalPosition = parcel.GlobalPosition; courier.Velocity = Vector2.Zero;
            PhysicsTick(tree); PhysicsTick(tree);
            Check(!parcel.Visible && !parcel.Monitoring, "Collected parcels disable sensing after callback delivery.");
        }
        Check(current.Score == 4, "All four parcels can be collected without monitoring reentrancy.");
        courier.GlobalPosition = parcels[0].GlobalPosition; PhysicsTick(tree);
        Check(current.Score == 4, "Re-entering a collected parcel does not score twice.");
        Switch(8);
        current!.SetStoryParameter(1024);
        Check(current.SelectedBody is null, "Population growth keeps selection empty.");
        Check(current.BodyCount == 1024 && current.Bodies.All(body => !body.CanSleep), "Stress capacity is 1,024 active physical bodies.");
        var removedParticle = current.Bodies[^1];
        Pointer(root, removedParticle.Position, true); Pointer(root, removedParticle.Position, false);
        Check(current.SelectedBody == removedParticle, "A stress particle can be selected.");
        current.SetStoryParameter(64);
        Check(current.SelectedBody is null && removedParticle.IsDisposed, "Removing the selected particle clears selection.");
        Check(current.BodyCount == 64, "Population slider removes real bodies and frees their RIDs.");
        Switch(9);
        var bike = current!.Bodies[0]; var start = bike.Position;
        current.Act(1);
        for (var i = 0; i < 360; i++) PhysicsTick(tree);
        Check(bike.Position.DistanceTo(start) > 40 && current.Joints.Count == 4, "Motorcycle torque drives real wheels with guides and suspension.");
        Switch(10);
        current!.Act(0);
        var bird = current.Bodies[^1];
        Check(!bird.Freeze && bird.LinearVelocity.X > 100 && bird.LinearVelocity.Y < 0, "Slingshot launches a real dynamic bird.");
        for (var i = 0; i < 240; i++) PhysicsTick(tree);
        Check(current.ContactEvents > 0 && bird.Position.DistanceTo(new(160, 526)) > 150, "The projectile travels and generates real tower/floor contacts.");
        Switch(11);
        var oldPiece = current!.SmashFragments[^1];
        current.SetSmashPopulation(64);
        Check(current.BodyCount == 65 && oldPiece.IsDisposed && !current.HasSelection, "Smash population removes actual bodies without selecting one.");
        current.SetSmashPopulation(128);
        Check(current.BodyCount == 129 && current.Bodies.All(b => b.Sleeping) && current.SmashFragments.All(b => b.State!.Sleeping), "Rebuilt Smash is ready with a sleeping wall and block.");
        var colorPiece = current.SmashFragments[0];
        Check(current.GetSmashColor(colorPiece) == PhysicsScene.SmashSleepingColor, "Sleeping fragments use the muted state color.");
        colorPiece.State!.Sleeping = false; colorPiece.State.LinearVelocity = Vector2.Zero;
        Check(current.GetSmashColor(colorPiece) == PhysicsScene.Pink, "Awake slow fragments use pink.");
        colorPiece.State!.LinearVelocity = new(200 * PhysicsScene.SmashScale, 0);
        Check(current.GetSmashColor(colorPiece) == PhysicsScene.Apricot, "Fast fragments use apricot based on step travel and size.");
        current.Act(2);
        current.InputBounds = new Rect2(Vector2.Zero, new Vector2(1152, 800) * PhysicsScene.SmashScale);
        var fragment = current.SmashFragments[0]; var fragmentPoint = fragment.Pose.Origin;
        Pointer(root, fragmentPoint, true);
        Check(current.SelectedRID == fragment.RID && current.SelectedRole == "Fragment", "Server fragments can be selected through scene queries.");
        Motion(root, fragmentPoint + new Vector2(120, -100) * PhysicsScene.SmashScale);
        for (var i = 0; i < 30; i++) PhysicsTick(tree);
        Check(fragment.Pose.Origin.DistanceTo(fragmentPoint) > 10, "Grab forces move a server fragment through the solver.");
        Pointer(root, fragment.Pose.Origin, false);
        Key(root, Electron2D.Key.K); Check(fragment.Frozen && PhysicsServer.BodyGetMode(fragment.RID) == PhysicsServer.BodyMode.Static, "Fragment freeze uses a real static solver body.");
        Key(root, Electron2D.Key.H); Check(fragment.Kinematic && PhysicsServer.BodyGetMode(fragment.RID) == PhysicsServer.BodyMode.Kinematic, "Fragment freeze mode uses a real kinematic solver body.");
        Pointer(root, fragment.Pose.Origin, true);
        var target = fragment.Pose.Origin + new Vector2(100, -80);
        Motion(root, target); PhysicsTick(tree); PhysicsTick(tree);
        Check(fragment.Pose.Origin.DistanceTo(target) < 1, $"Kinematic fragment dragging reaches the target pose: {fragment.Pose.Origin} vs {target}.");
        Pointer(root, target, false);
        Key(root, Electron2D.Key.K); Key(root, Electron2D.Key.L);
        Check(PhysicsServer.BodyGetMode(fragment.RID) == PhysicsServer.BodyMode.RigidLinear, "Fragment rotation locking uses the solver's linear rigid mode.");
        Key(root, Electron2D.Key.C); Check(!PhysicsServer.BodyGetConstantForce(fragment.RID).IsZeroApprox(), "Fragment persistent lift force is applied to its server body.");
        Key(root, Electron2D.Key.C);
        for (var policy = 1; policy <= 3; policy++)
        {
            Key(root, Electron2D.Key.V); PhysicsTick(tree);
            Check(fragment.Policy == policy && (policy != 1 || fragment.State is null) &&
                (policy != 2 || PhysicsServer.BodyGetMode(fragment.RID) == PhysicsServer.BodyMode.Static), "Server fragment participation policy: " + policy);
        }
        Key(root, Electron2D.Key.V); PhysicsTick(tree);
        Check(fragment.Policy == 0 && fragment.State is { IsDisposed: false }, "Fragment participation can resume with a new live view.");
        Pointer(root, new Vector2(300, 200) * PhysicsScene.SmashScale, true);
        Pointer(root, new Vector2(300, 200) * PhysicsScene.SmashScale, false);
        Check(!current.HasSelection, "An empty stage click clears the server-fragment selection.");
        current.SetStoryParameter(800 * PhysicsScene.SmashScale); current.Act(0);
        Check(current.Bodies[0].LinearVelocity == new Vector2(800 * PhysicsScene.SmashScale, 0), "Smash impact-speed control drives actual block velocity.");
        for (var i = 0; i < 120; i++) PhysicsTick(tree);
        Check(current.Score >= 16 && current.ContactEvents > 0, "Smash wakes and scatters the resized wall.");
        var detachedView = current.SmashFragments[0].State!;
        var retainedVelocity = detachedView.LinearVelocity; var retainedSleep = detachedView.Sleeping;
        root.RemoveChild(current);
        Check(detachedView.IsDisposed && current.SmashFragments.All(b => b.State is null), "Scene exit releases fragment attachments and live views.");
        root.AddChild(current);
        Check(current.SmashFragments.All(b => b.State is { IsDisposed: false }) &&
            current.SmashFragments[0].State!.LinearVelocity.IsEqualApprox(retainedVelocity) &&
            current.SmashFragments[0].State!.Sleeping == retainedSleep, "Reentry reattaches every server fragment and preserves motion/sleep state.");
        current.Act(2);
        Check(current.Score == 0 && current.Bodies.All(b => b.LinearVelocity.IsZeroApprox()) && current.SmashFragments.All(b => b.State!.LinearVelocity.IsZeroApprox()), "Rebuilding clears motion and score without replacing bodies.");
        Console.WriteLine("Smash: checking maximum wall construction and teardown.");
        current.SetSmashPopulation(PhysicsScene.SmashMaximumCount);
        Check(current.BodyCount == PhysicsScene.SmashMaximumCount + 1, "Smash maximum consists of 65,536 real fragments and one block.");
        var largeWorld = Box2D.NET.B2Worlds.b2GetWorldFromId(Box2D.NET.B2Bodies.b2Body_GetWorld(current.Bodies[0].BackendID));
        Check(Box2D.NET.B2Worlds.b2World_GetCounters(current.Bodies[0].Space!.WorldID).bodyCount == current.BodyCount + 4, "All fragments, the block and four boundaries belong to the same real solver world.");
        Check(largeWorld.bodyMoveEvents.capacity >= current.BodyCount, "Body movement events are prepared before a large sleeping wall wakes.");
        var dormantSlots = largeWorld.solverSets.data.AsSpan(3, largeWorld.solverSets.count - 3);
        long dormantBodies = 0, dormantContacts = 0;
        foreach (var set in dormantSlots) { dormantBodies += set.bodySims.capacity; dormantContacts += set.contactSims.capacity; }
        Check(dormantBodies <= dormantSlots.Length * 16L + current.BodyCount * 2L &&
            dormantContacts <= dormantSlots.Length * 32L + current.BodyCount * 8L,
            "Independent sleeping fragments prepare linear dormant storage, without full-world copies per island.");
        current.Act(0);
        for (var i = 0; i < 32; i++) PhysicsTick(tree);
        Check(current.Bodies.All(b => b.Position.IsFinite() && b.LinearVelocity.IsFinite()) && current.SmashFragments.All(b => b.Pose.IsFinite() && b.State!.LinearVelocity.IsFinite()), "Maximum Smash load preserves finite solver state.");
        var removedMaximumPiece = current.SmashFragments[^1]; current.SetSmashPopulation(64);
        Check(removedMaximumPiece.IsDisposed && current.BodyCount == 65, "Maximum population can shrink and release all removed bodies.");
        for (var i = 0; i < SandboxWindow.SceneNames.Length; i++) Switch(i % SandboxWindow.SceneNames.Length);
        Console.WriteLine("PhysicsSandbox passed: twelve stories, finite simulation, all 36 actions, pause/step, grabbing, freeze and repeated scene/RID cleanup.");
    }

    private static void VerifySolverArrayReuse()
    {
        var array = Box2D.NET.B2Arrays.b2Array_Create<Box2D.NET.B2ContactSim>(4);
        Box2D.NET.B2Arrays.b2Array_Resize(ref array, 3);
        var removed = array.data[0]; var last = array.data[2];
        var allocated = GC.GetAllocatedBytesForCurrentThread();
        var moved = Box2D.NET.B2Arrays.b2Array_RemoveSwap(ref array, 0);
        Check(GC.GetAllocatedBytesForCurrentThread() == allocated && moved == 2 && array.count == 2 && ReferenceEquals(array.data[0], last) && ReferenceEquals(array.data[2], removed), "Solver compaction keeps active and spare objects distinct without constructing replacements.");
        for (var i = 0; i < 128; i++)
        {
            Box2D.NET.B2Arrays.b2Array_Add(ref array).contactId = i;
            Box2D.NET.B2Arrays.b2Array_RemoveSwap(ref array, 0);
        }
        allocated = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 128; i++)
        {
            Box2D.NET.B2Arrays.b2Array_Add(ref array).contactId = i;
            Box2D.NET.B2Arrays.b2Array_RemoveSwap(ref array, 0);
        }
        Check(GC.GetAllocatedBytesForCurrentThread() == allocated && !ReferenceEquals(array.data[0], array.data[1]), "Repeated contact churn reuses spare solver storage and does not alias live slots.");
    }

    private static void VerifySolverVectorArithmetic()
    {
        var random = new System.Random(742);
        for (var i = 0; i < 256; i++)
        {
            var a = new Box2D.NET.B2FloatW(); var b = a; var c = a;
            for (var lane = 0; lane < 4; lane++)
            { a[lane] = (float)(random.NextDouble() * 200 - 100); b[lane] = (float)(random.NextDouble() * 200 - 100); c[lane] = (float)(random.NextDouble() * 200 - 100); }
            var add = Box2D.NET.B2ContactSolvers.b2AddW(a, b);
            var sub = Box2D.NET.B2ContactSolvers.b2SubW(a, b);
            var mul = Box2D.NET.B2ContactSolvers.b2MulW(a, b);
            var madd = Box2D.NET.B2ContactSolvers.b2MulAddW(a, b, c);
            var msub = Box2D.NET.B2ContactSolvers.b2MulSubW(a, b, c);
            for (var lane = 0; lane < 4; lane++)
                Check(add[lane] == a[lane] + b[lane] && sub[lane] == a[lane] - b[lane] && mul[lane] == a[lane] * b[lane] &&
                    madd[lane] == a[lane] + b[lane] * c[lane] && msub[lane] == a[lane] - b[lane] * c[lane], "Four-lane solver arithmetic preserves independent scalar results.");
        }
    }

    private static void VerifySleepingStorage()
    {
        using var root = new SubViewport();
        using var shape = new CircleShape();
        var body = new RigidBody { MaxContactsReported = 8, GravityScale = 0 };
        body.AddChild(new CollisionShape { Shape = shape });
        root.AddChild(body);
        using var tree = new SceneTree(root);
        for (var i = 0; i < 8; i++) { body.Sleeping = true; PhysicsTick(tree); body.Sleeping = false; PhysicsTick(tree); }
        var world = Box2D.NET.B2Worlds.b2GetWorldFromId(Box2D.NET.B2Bodies.b2Body_GetWorld(body.BackendID));
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) { body.Sleeping = true; PhysicsTick(tree); body.Sleeping = false; PhysicsTick(tree); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warmed sleep/wake fixed steps reuse sleeping-island buffers.");
        var retained = world.solverSets.data.Where(s => s.setIndex == -1 && s.bodySims.capacity > 0).ToArray();
        Check(retained.Length > 0, "Inactive solver sets retain prepared capacity until world teardown.");
        tree.Dispose();
        Check(retained.All(s => s.bodySims.data is null && s.contactSims.data is null), "World teardown releases live and inactive cached solver arrays.");
    }

    private static void VerifyBroadphasePairReuse()
    {
        using var root = new SubViewport();
        using var shape = new CircleShape { Radius = 20 };
        RigidBody? first = null;
        for (var i = 0; i < 96; i++)
        {
            var body = new RigidBody { Name = "Pair" + i, Position = new(100, 100), CanSleep = false };
            body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body); first ??= body;
        }
        using var tree = new SceneTree(root);
        PhysicsServer.AreaSetGravity(first!.GetWorld()!.Space, 0);
        PhysicsTick(tree);
        var world = Box2D.NET.B2Worlds.b2GetWorldFromId(first.Space!.WorldID);
        var needed = Box2D.NET.B2Atomics.b2AtomicLoadInt(ref world.broadPhase.movePairIndex);
        Check(needed > 16 * 96 && world.broadPhase.movePairCapacity >= needed,
            "Dense broad-phase queries retain overflow pair demand for subsequent arena reuse.");
    }

    internal static void RunNative()
    {
        const int framesPerScene = 180;
        using var regular = new FontFile();
        regular.LoadDynamicFont(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "IBMPlexSans-Regular.ttf"));
        using var semibold = new FontFile();
        semibold.LoadDynamicFont(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "IBMPlexSans-SemiBold.ttf"));
        var backend = Environment.GetEnvironmentVariable("ELECTRON2D_SANDBOX_BACKEND") ?? "compatibility";
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        Engine.MaxFPS = 60;
        ProjectSettings.Set(ProjectSettings.PhysicsInterpolation, true);
        var directory = System.IO.Path.GetFullPath("bin/physics-sandbox/" + backend);
        Directory.CreateDirectory(directory);
        using var window = new SandboxWindow(regular, semibold);
        var frame = 0;
        var pausedSteps = 0;
        RigidBody? grabbedBody = null;
        var grabbedStart = Vector2.Zero; var grabSteps = 0;
        Vector2 UI(string name, float ratio = .5f)
        {
            var control = window.GetNode<CanvasLayer>("Interface").GetNode<Control>(name);
            return control.Position + new Vector2(control.Size.X * ratio, control.Size.Y / 2);
        }
        Action? post = null;
        post = () =>
        {
            frame++;
            var scene = (frame - 1) / framesPerScene;
            var phase = (frame - 1) % framesPerScene;
            if (scene >= SandboxWindow.SceneNames.Length) { window.Tree!.Quit(); return; }
            if (phase == 0) { window.SwitchScene(scene); Check(window.Scene.SelectedBody is null, "New native story has no selection."); }
            if (scene == 11 && phase == 0) { NativeKey(SDL.Scancode.P); window.Scene.Act(2); }
            if (scene == 11 && phase == 2)
            {
                using var wall = RenderingServer.Service!.Readback();
                wall.SavePNG(System.IO.Path.Combine(directory, "12-solid.png"));
                var transform = window.Scene.GetGlobalTransformWithCanvas();
                var first = transform * window.Scene.SmashFragments[0].Pose.Origin;
                var last = transform * window.Scene.SmashFragments[^1].Pose.Origin;
                var fill = wall.GetPixel((int)first.X, (int)first.Y);
                var seams = 0;
                for (var y = (int)MathF.Ceiling(first.Y); y < (int)last.Y; y++)
                    for (var x = (int)MathF.Ceiling(first.X); x < (int)last.X; x++)
                        if (!wall.GetPixel(x, y).IsEqualApprox(fill)) seams++;
                Check(!fill.IsEqualApprox(PhysicsScene.Paper) && seams == 0, $"Sleeping Smash wall reads as one solid object without raster seams: {seams} mismatched pixels.");
                window.Scene.Act(0); NativeKey(SDL.Scancode.P);
            }
            if (scene == 0 && phase == 2) NativeClick(UI("SceneSelector"));
            if (scene == 0 && phase == 3)
            {
                var selector = window.GetNode<CanvasLayer>("Interface").GetChildren().OfType<OptionButton>().Single();
                Check(selector.GetPopup().Visible, "The real scene dropdown opens.");
                NativeKey(SDL.Scancode.Down); NativeKey(SDL.Scancode.Down); NativeKey(SDL.Scancode.Return);
            }
            if (scene == 0 && phase == 4)
            {
                var selector = window.GetNode<CanvasLayer>("Interface").GetChildren().OfType<OptionButton>().Single();
                Check(window.SceneIndex == 1 && selector.Selected == 1 && !selector.GetPopup().Visible, $"Keyboard selection switches the actual scene and caption: scene={window.SceneIndex}, selected={selector.Selected}, focus={selector.GetPopup().GetFocusedItem()}, visible={selector.GetPopup().Visible}.");
                window.SwitchScene(0);
            }
            if (scene == 0 && phase == 6) NativeClick(UI("Pause"));
            if (scene == 0 && phase == 7)
            {
                Check(!window.Scene.Running, "Pause button suspends the solver.");
                pausedSteps = window.Scene.PhysicsSteps;
            }
            if (scene == 0 && phase == 8)
            {
                Check(window.Scene.PhysicsSteps == pausedSteps, "Paused frames do not advance physics.");
                NativeClick(UI("Step"));
            }
            if (scene == 0 && phase == 10)
            {
                Check(window.Scene.PhysicsSteps == pausedSteps + 1, "Step toolbar executes exactly one interval.");
                NativeClick(UI("Pause"));
            }
            if (scene == 0 && phase == 11) Check(window.Scene.Running, "Play resumes physics.");
            if (phase == 12) NativeKey(SDL.Scancode.F3);
            if (phase == 14)
            {
                Check(window.GetNode<CanvasLayer>("Interface").GetChildren().All(n => n.Name != "Debug"), "The sandbox has no debug control.");
                Check(window.Scene.GetChildren().All(n => n.Name != "PhysicsDebug"), "F3 does not create a collision overlay.");
            }
            if (scene == 0 && phase == 15) NativeClick(window.Scene.GetGlobalTransformWithCanvas() * window.Scene.Bodies[0].GlobalPosition);
            if (scene == 0 && phase == 16)
            {
                Check(window.Scene.SelectedBody == window.Scene.Bodies[0] && window.GetNode<CanvasLayer>("Interface").GetNode<HSlider>("Parameter4").Visible, "Native body click selects it and opens Object.");
                NativeClick(new(820, 210));
            }
            if (scene == 0 && phase == 17)
            {
                Check(window.Scene.SelectedBody is null && !window.GetNode<CanvasLayer>("Interface").GetNode<HSlider>("Parameter4").Visible, "Native empty click clears selection and hides object controls.");
                using var emptyInspector = RenderingServer.Service!.Readback();
                emptyInspector.SavePNG(System.IO.Path.Combine(directory, "01-unselected.png"));
                NativeClick(window.Scene.GetGlobalTransformWithCanvas() * window.Scene.Bodies[0].GlobalPosition);
            }
            if (scene == 0 && phase == 18)
            {
                Check(window.Scene.SelectedBody == window.Scene.Bodies[0], "An object can be selected again after clearing.");
                NativeClick(window.Scene.GetGlobalTransformWithCanvas() * window.Scene.Bodies[0].GlobalPosition);
            }
            if (scene == 0 && phase == 19)
            {
                Check(window.GetNode<CanvasLayer>("Interface").GetNode<HSlider>("Parameter4").Visible, "Repeated selection keeps Object available.");
                NativeClick(new(820, 210)); NativeClick(UI("Inspector0"));
            }
            if (phase == 20)
            {
                using var pixels = RenderingServer.Service!.Readback();
                Check(pixels.Size == new Vector2i(1152, 800), "Real renderer uses splash client dimensions.");
                for (var x = 28; x < 860; x += 16)
                    Check(pixels.GetPixel(x, 699).IsEqualApprox(PhysicsScene.Paper), "Scene drawing stays clipped above the action dock.");
                Check(window.GetNode<CanvasLayer>("SimulationCanvas").GetNode<Control>("StageClip").ClipContents, "The world uses the actual public clipping path.");
                Check(pixels.GetPixel(10, 100).IsEqualApprox(PhysicsScene.Paper), "The actual paper background is drawn.");
                pixels.SavePNG(System.IO.Path.Combine(directory, $"{scene + 1:00}.png"));
            }
            if (scene == 0 && phase == 23)
            {
                grabbedBody = window.Scene.Bodies[17]; grabSteps = window.Scene.PhysicsSteps; grabbedStart = grabbedBody.Position;
                var screen = window.Scene.GetGlobalTransformWithCanvas() * grabbedStart;
                NativeMotion(screen); NativeButton(screen, true);
            }
            if (scene == 0 && phase is >= 24 and <= 34) NativeMotion(window.Scene.GetGlobalTransformWithCanvas() * (grabbedStart + new Vector2(80, -50)));
            if (scene == 0 && phase == 35)
            {
                Check(grabbedBody!.Position.DistanceTo(grabbedStart) > 3, $"Native drag moves real geometry without diagnostic geometry: start={grabbedStart}, now={grabbedBody.Position}, selected={window.Scene.SelectedBody?.Name}, target={grabbedBody.Name}, ticks={window.Scene.PhysicsSteps - grabSteps}, screen={window.Scene.GetGlobalTransformWithCanvas() * grabbedStart}.");
                NativeMotion(UI("Pause")); NativeButton(UI("Pause"), false);
            }
            if (phase == 36)
            {
                using var pixels = RenderingServer.Service!.Readback();
                for (var x = 28; x < 860; x += 16) Check(pixels.GetPixel(x, 699).IsEqualApprox(PhysicsScene.Paper), "Dragged geometry stays inside the field.");
                pixels.SavePNG(System.IO.Path.Combine(directory, $"{scene + 1:00}-actions.png"));
                window.Scene.Act(0); window.Scene.Act(1); window.Scene.Act(2);
            }
            if (phase == 40) NativeClick(UI("Inspector0"));
            if (phase == 42)
            {
                Check(window.GetNode<CanvasLayer>("Interface").GetChildren().OfType<HSlider>().Count() == 12, "Every story has native world/object/story sliders.");
                NativeClick(UI("Parameter0", .75f));
            }
            if (phase == 44)
            {
                Check(window.Scene.WorldGravity > 1200 && PhysicsServer.AreaGetGravity(window.Scene.GetWorld()!.Space) == window.Scene.WorldGravity, "Native world slider changes actual world defaults.");
                NativeClick(UI("Inspector1"));
            }
            if (phase == 45) NativeClick(UI("Parameter4", .35f));
            if (phase == 47)
            {
                using var inspector = RenderingServer.Service!.Readback();
                inspector.SavePNG(System.IO.Path.Combine(directory, $"{scene + 1:00}-object.png"));
                Check(window.Scene.SelectedBody is not RigidBody selected || selected.Mass > 2, "Native selected-object slider changes solver mass.");
            }
            if (scene == 8 && phase == 48) NativeClick(UI("Inspector2"));
            if (scene == 8 && phase == 49) NativeClick(UI("Parameter10", .99f));
            if (scene == 8 && phase == 51)
            {
                Check(window.Scene.BodyCount == 1024, "Native population slider reaches 1,024 active bodies.");
                using var population = RenderingServer.Service!.Readback();
                population.SavePNG(System.IO.Path.Combine(directory, "09-population.png"));
            }
            if (scene == 9 && phase == 48) NativeClick(window.Scene.GetGlobalTransformWithCanvas() * window.Scene.Bodies[1].GlobalPosition);
            if (scene == 9 && phase == 50)
            {
                var friction = window.GetNode<CanvasLayer>("Interface").GetChildren().OfType<HSlider>().ElementAt(5);
                Check(window.Scene.SelectedRID == window.Scene.Bodies[1].GetRID() && Math.Abs(friction.Value - 1.2) < .001, "Rough tire friction displays its actual magnitude.");
                NativeClick(UI("Parameter5", .35f));
            }
            if (scene == 9 && phase == 52) Check(PhysicsServer.BodyGetFriction(window.Scene.Bodies[1].GetRID()) < 0, "Editing tire friction preserves rough material mixing.");
            if (scene == 10 && phase == 48) window.Scene.Act(1);
            if (scene == 10 && phase == 49)
            {
                var screen = window.Scene.GetGlobalTransformWithCanvas() * new Vector2(160, 526);
                NativeMotion(screen); NativeButton(screen, true);
            }
            if (scene == 10 && phase == 50) NativeMotion(window.Scene.GetGlobalTransformWithCanvas() * new Vector2(80, 580));
            if (scene == 10 && phase == 52) NativeButton(window.Scene.GetGlobalTransformWithCanvas() * new Vector2(80, 580), false);
            if (scene == 10 && phase == 54) Check(!window.Scene.Bodies[^1].Freeze && window.Scene.Bodies[^1].LinearVelocity.X > 100, "Native pull/release launches the bird through the solver.");
            if (scene == 11 && phase == 48)
            {
                window.Scene.WorldGravity = 0; window.Scene.Act(2);
                NativeClick(UI("Inspector2"));
            }
            if (scene == 11 && phase == 49) NativeClick(UI("Parameter10", .8f));
            if (scene == 11 && phase == 50)
            {
                Check(window.Scene.StoryParameter > 750 && window.GetNode<CanvasLayer>("Interface").GetNode<HSlider>("Parameter11").Visible, "Smash has live impact speed and fragment-count controls.");
                NativeClick(UI("Parameter11", .05f));
            }
            if (scene == 11 && phase == 52)
            {
                Check(window.Scene.SmashFragmentCount is >= 64 and <= 4096 && window.Scene.BodyCount == window.Scene.SmashFragmentCount + 1, "Native Smash population control changes real body count.");
                using var controls = RenderingServer.Service!.Readback();
                controls.SavePNG(System.IO.Path.Combine(directory, "12-controls.png"));
                NativeClick(window.Scene.GetGlobalTransformWithCanvas() * window.Scene.SmashFragments[0].Pose.Origin);
            }
            if (scene == 11 && phase == 54)
            {
                Check(window.Scene.SelectedRID == window.Scene.SmashFragments[0].RID && Math.Abs(window.GetNode<CanvasLayer>("Interface").GetNode<HSlider>("Parameter4").Value - .0045) < .00001, "Fragment mass is shown without clamping.");
                NativeClick(UI("Parameter4", .1f));
            }
            if (scene == 11 && phase == 56) Check(PhysicsServer.BodyGetMass(window.Scene.SmashFragments[0].RID) > 1, "Native object slider edits a Smash fragment's physical mass.");
            if (scene == 11 && phase == 58) NativeClick(new(200, 210));
            if (scene == 11 && phase == 60) Check(!window.Scene.HasSelection, "Empty Smash field click clears selection.");
            if (scene == 11 && phase == 62) { window.Scene.SetSmashPopulation(PhysicsScene.SmashDefaultCount); window.Scene.Act(0); }
            if (scene == 11 && phase == 170)
            {
                Check(window.Scene.Score > 50 && window.Scene.SmashFragments.All(b => b.Pose.IsFinite()), "The rendered full-load impact scatters real fragments.");
                using var impact = RenderingServer.Service!.Readback();
                impact.SavePNG(System.IO.Path.Combine(directory, "12-impact.png"));
            }
            if (phase == 60)
                Check(window.Scene.Bodies.All(b => b.Position.IsFinite() && b.LinearVelocity.IsFinite()), "Rendered actions preserve finite state.");
            if (frame == SandboxWindow.SceneNames.Length * framesPerScene - 2) RenderingServer.FramePostDraw -= post;
            if (frame == SandboxWindow.SceneNames.Length * framesPerScene - 2) window.Tree!.Quit();
        };
        window.Ready += _ => RenderingServer.FramePostDraw += post;
        try { var code = Engine.Run(window); Check(code == 0 && frame >= SandboxWindow.SceneNames.Length * framesPerScene - 2, $"All native scenes finish: code={code}, frame={frame}."); }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= post; }
        Console.WriteLine($"PhysicsSandbox native {backend} passed: 1152x800, twelve scenes, scene captures and rendered actions. {directory}");
    }

    private static void PhysicsTick(SceneTree tree)
    {
        try
        {
            tree.PhysicsFrame(1d / 60);
            for (var i = 0; i < tree.Root.GetChildCount(); i++)
                if (tree.Root.GetChild(i) is PhysicsScene scene) scene.CaptureSmashState();
        }
        catch (Exception error) { Console.WriteLine("Original physics failure before teardown: " + error); throw; }
    }
    private static void NativeClick(Vector2 point)
    {
        NativeMotion(point); NativeButton(point, true); NativeButton(point, false);
    }
    private static void NativeMotion(Vector2 point)
    {
        var input = new SDL.Event { Motion = new SDL.MouseMotionEvent { Type = SDL.EventType.MouseMotion, WindowID = SDL.GetWindowID(SDL.GetWindows(out _)![0]), X = point.X, Y = point.Y } };
        Check(SDL.PushEvent(ref input), "Native mouse motion accepted.");
    }
    private static void NativeButton(Vector2 point, bool down)
    {
        var input = new SDL.Event { Button = new SDL.MouseButtonEvent { Type = down ? SDL.EventType.MouseButtonDown : SDL.EventType.MouseButtonUp, WindowID = SDL.GetWindowID(SDL.GetWindows(out _)![0]), Button = 1, Down = down, X = point.X, Y = point.Y } };
        Check(SDL.PushEvent(ref input), "Native mouse button accepted.");
    }
    private static void NativeKey(SDL.Scancode code)
    {
        foreach (var down in new[] { true, false })
        {
            var input = new SDL.Event { Key = new SDL.KeyboardEvent { Type = down ? SDL.EventType.KeyDown : SDL.EventType.KeyUp, WindowID = SDL.GetWindowID(SDL.GetWindows(out _)![0]), Scancode = code, Key = SDL.GetKeyFromScancode(code, SDL.Keymod.None, true), Down = down } };
            Check(SDL.PushEvent(ref input), "Native keyboard input accepted.");
        }
    }
    private static void Pointer(Viewport root, Vector2 point, bool down)
    {
        using var input = new InputEventMouseButton { Position = point, ButtonIndex = MouseButton.Left, Pressed = down };
        root.PushInput(input, true);
    }
    private static void Motion(Viewport root, Vector2 point)
    {
        using var input = new InputEventMouseMotion { Position = point };
        root.PushInput(input, true);
    }
    private static void Key(Viewport root, Electron2D.Key key)
    {
        using var input = new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = true };
        root.PushInput(input, true);
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
