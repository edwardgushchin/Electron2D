using Electron2D;

internal static class CollisionDisableModeTests
{
    internal static void Run(PhysicsServer.Backend backend = PhysicsServer.Backend.CPU)
    {
        VerifyRemovalAndInheritance(backend);
        VerifyStaticAndFreeze(backend);
        VerifyKinematicAndSensors(backend);
        VerifyFailuresAndPacking(backend);
        VerifyWarmFrames(backend);
        Console.WriteLine($"Collision disable policies ({backend}), inheritance, restoration, failures and allocation checks passed.");
    }

    private static void VerifyRemovalAndInheritance(PhysicsServer.Backend backend)
    {
        using var world = new World(backend);
        using var circle = new CircleShape { Radius = 10 };
        var root = new SubViewport { World = world };
        var branch = new Node { ProcessMode = ProcessMode.Disabled };
        var body = new RigidBody { Name = "body", GravityScale = 0, CanSleep = false };
        var independent = new StaticBody { Name = "independent", Position = new(100, 0), ProcessMode = ProcessMode.Always };
        var removedArea = new Area { Name = "removedArea" };
        var observer = new Area { Name = "observer" };
        Add(body, circle); Add(independent, circle); Add(removedArea, circle); Add(observer, circle);
        branch.AddChild(body); branch.AddChild(independent); branch.AddChild(removedArea);
        root.AddChild(branch); root.AddChild(observer);
        var rid = body.GetRID(); var owner = body.GetShapeOwners()[0];
        Check(body.DisableMode == CollisionDisableMode.Remove && (int)CollisionDisableMode.MakeStatic == 1 &&
            (int)CollisionDisableMode.KeepActive == 2, "Stored defaults and enum values.");
        using var tree = new SceneTree(root);
        Check(PhysicsServer.SpaceGetBackend(world.Space) == backend, "The complete scene uses the explicitly selected physics backend.");
        Check(!Attached(body) && !Attached(removedArea) && Attached(independent),
            "Disabled inherited entry omits bodies/areas; an explicit process mode breaks inheritance.");
        branch.ProcessMode = ProcessMode.Pausable;
        using var point = new PhysicsPointQueryParameters { Position = Vector2.Zero };
        var direct = observer.GetWorld()!.DirectSpaceState;
        Check(direct.IntersectPoint(point).Any(hit => hit.ColliderRID == rid), "Enabling restores query membership immediately.");
        var view = PhysicsServer.BodyGetDirectState(rid)!;
        tree.Paused = true;
        Check(Attached(body), "Pausing alone does not invoke a disable policy.");
        tree.Paused = false;
        tree.PhysicsFrame(1d / 60);
        var exits = 0; var shapeExits = 0;
        observer.BodyExited += _ => exits++;
        observer.BodyShapeExited += (_, _, _, _) => shapeExits++;
        branch.ProcessMode = ProcessMode.Disabled;
        Check(exits == 1 && shapeExits == 1 && !observer.HasOverlappingBodies() && !Attached(body) &&
            !direct.IntersectPoint(point).Any(hit => hit.ColliderRID == rid), "Remove clears peer pairs and query membership synchronously.");
        Reject<InvalidOperationException>(() => _ = view.Transform);
        Check(body.GetRID() == rid && body.GetShapeOwners()[0] == owner && body.ShapeOwnerGetShapeCount(owner) == 1,
            "RID, owners and borrowed geometry survive removal.");
        body.DisableMode = CollisionDisableMode.KeepActive;
        Check(Attached(body) && !ReferenceEquals(view, PhysicsServer.BodyGetDirectState(rid)),
            "Changing a disabled policy reattaches with a new live view.");
        body.LinearVelocity = new(30, 0); tree.PhysicsFrame(1d / 60);
        Check(body.Position.X > 0.4f, "KeepActive continues dynamics with disabled node callbacks.");
        body.DisableMode = CollisionDisableMode.Remove;
        branch.RemoveChild(body); body.ProcessMode = ProcessMode.Inherit; root.AddChild(body);
        Check(Attached(body), "Reparenting out of a disabled ancestor restores inherited participation.");
        body.Notify(Node.NotificationDisabled);
        Check(Attached(body), "Manual notification cannot fake effective disabled processing.");
        Reject<ArgumentOutOfRangeException>(() => body.DisableMode = (CollisionDisableMode)99);
        Check(body.DisableMode == CollisionDisableMode.Remove && Attached(body), "Invalid policy rejects before mutation.");
        Check(Task.Run(() => Capture(() => body.DisableMode = CollisionDisableMode.KeepActive)).Result is InvalidOperationException,
            "Attached policies enforce scene owner-thread mutation.");
    }

    private static void VerifyStaticAndFreeze(PhysicsServer.Backend backend)
    {
        using var world = new World(backend);
        using var box = new RectangleShape { Size = new(200, 20) };
        using var circle = new CircleShape { Radius = 10 };
        var root = new SubViewport { World = world };
        var support = new RigidBody
        {
            Name = "support",
            Position = new(0, 100),
            Mass = 7,
            LockRotation = true,
            DisableMode = CollisionDisableMode.MakeStatic,
            LinearVelocity = new(50, 0),
            AngularVelocity = 3
        };
        var falling = new RigidBody { Name = "falling", CanSleep = false };
        Add(support, box); Add(falling, circle); root.AddChild(support); root.AddChild(falling);
        using var tree = new SceneTree(root);
        Check(PhysicsServer.SpaceGetBackend(world.Space) == backend, "The complete scene uses the explicitly selected physics backend.");
        var view = PhysicsServer.BodyGetDirectState(support.GetRID())!;
        support.ProcessMode = ProcessMode.Disabled;
        Check(!support.Freeze && support.LinearVelocity == Vector2.Zero && support.AngularVelocity == 0 && view.InverseMass == 0,
            "MakeStatic clears prior dynamic velocities without setting Freeze or invalidating the view.");
        for (var frame = 0; frame < 120; frame++) tree.PhysicsFrame(1d / 60);
        Check(support.Position.IsEqualApprox(new(0, 100)) && falling.Position.Y is > 75 and < 82,
            "A disabled static rigid body retains real solver contact response.");
        support.LinearVelocity = new(30, 0); support.AngularVelocity = 2;
        support.Freeze = true; support.Freeze = false;
        Check(PhysicsServer.BodyGetMode(support.GetRID()) == PhysicsServer.BodyMode.Static && support.LinearVelocity == new Vector2(30, 0),
            "Freeze changes cannot override temporary static participation; newly assigned velocity is retained.");
        support.DisableMode = CollisionDisableMode.KeepActive;
        Check(MathF.Abs(view.InverseMass - 1f / 7) < 0.001f && view.InverseInertia == 0 && view.LinearVelocity.IsEqualApprox(new(30, 0)),
            "Restoring dynamics reapplies explicit mass, rotation lock and latest stored velocity.");
        support.Freeze = true; support.DisableMode = CollisionDisableMode.MakeStatic;
        support.ProcessMode = ProcessMode.Always;
        Check(support.Freeze && PhysicsServer.BodyGetMode(support.GetRID()) == PhysicsServer.BodyMode.Static,
            "Enabling never unfreezes a user-frozen body.");
        support.Freeze = false;
        support.ProcessMode = ProcessMode.Disabled; support.DisableMode = CollisionDisableMode.Remove;
        Check(!Attached(support), "Changing MakeStatic to Remove detaches.");
        support.LinearVelocity = new(25, 0); support.GravityScale = 0;
        support.DisableMode = CollisionDisableMode.KeepActive;
        tree.PhysicsFrame(1d / 60);
        Check(support.Position.X > 0.3f, "Removal from temporary static mode cannot turn retained dynamic sleep into static inactivity.");
        support.DisableMode = CollisionDisableMode.MakeStatic;
        Check(view != PhysicsServer.BodyGetDirectState(support.GetRID()) &&
            PhysicsServer.BodyGetMode(support.GetRID()) == PhysicsServer.BodyMode.Static, "Disabled reattachment creates a static native body.");
    }

    private static void VerifyKinematicAndSensors(PhysicsServer.Backend backend)
    {
        using var world = new World(backend);
        using var circle = new CircleShape { Radius = 10 };
        var root = new SubViewport { World = world, ProcessMode = ProcessMode.Disabled };
        var platform = new AnimatableBody { Name = "platform", DisableMode = CollisionDisableMode.MakeStatic };
        var character = new CharacterBody { Name = "character", DisableMode = CollisionDisableMode.MakeStatic };
        var rigid = new RigidBody { Name = "rigid", DisableMode = CollisionDisableMode.MakeStatic, GravityScale = 0.5f };
        var frozen = new RigidBody { Name = "frozen", Freeze = true, DisableMode = CollisionDisableMode.KeepActive };
        var scenery = new StaticBody { Name = "scenery", DisableMode = CollisionDisableMode.KeepActive };
        var area = new Area
        {
            Name = "sensor",
            DisableMode = CollisionDisableMode.MakeStatic,
            GravitySpaceOverride = Area.SpaceOverride.Replace,
            Gravity = 100,
            GravityDirection = Vector2.Up
        };
        Add(platform, circle); Add(character, circle); Add(rigid, circle); Add(frozen, circle); Add(scenery, circle); Add(area, circle);
        root.AddChild(platform); root.AddChild(character); root.AddChild(rigid); root.AddChild(frozen); root.AddChild(scenery); root.AddChild(area);
        using var tree = new SceneTree(root);
        Check(PhysicsServer.SpaceGetBackend(world.Space) == backend, "The complete scene uses the explicitly selected physics backend.");
        tree.PhysicsFrame(1d / 60);
        Check(area.GetOverlappingBodies().Contains(platform) && PhysicsServer.BodyGetMode(platform.GetRID()) == PhysicsServer.BodyMode.Static &&
            PhysicsServer.BodyGetMode(character.GetRID()) == PhysicsServer.BodyMode.Static, "MakeStatic keeps Area sensing and converts both kinematic siblings.");
        Check(character.GetGravity().IsEqualApprox(new(0, -100)), "MakeStatic Area fields continue resolving for attached bodies.");
        Check(rigid.GetGravity().IsEqualApprox(new(0, -50)) && frozen.GetGravity().IsEqualApprox(new(0, -100)) &&
            PhysicsServer.BodyGetDirectState(rigid.GetRID())!.TotalGravity.IsEqualApprox(new(0, -50)) &&
            PhysicsServer.BodyGetDirectState(frozen.GetRID())!.TotalGravity.IsEqualApprox(new(0, -100)) &&
            scenery.GetGravity() == Vector2.Zero && rigid.Position == Vector2.Zero && frozen.Position == Vector2.Zero,
            "Temporary static and frozen rigid roles retain scaled scene/direct fields without force integration or static scenery fields.");
        area.DisableMode = CollisionDisableMode.Remove; tree.PhysicsFrame(1d / 60);
        Check(character.GetGravity().IsEqualApprox(new(0, 980)) && rigid.GetGravity().IsEqualApprox(new(0, 490)) &&
            frozen.GetGravity().IsEqualApprox(new(0, 980)), "Remove omits a disabled Area from field resolution, retaining world fallback.");
        area.DisableMode = CollisionDisableMode.KeepActive; tree.PhysicsFrame(1d / 60);
        Check(character.GetGravity().IsEqualApprox(new(0, -100)) && rigid.GetGravity().IsEqualApprox(new(0, -50)) &&
            frozen.GetGravity().IsEqualApprox(new(0, -100)), "KeepActive restores disabled Area fields.");
        platform.Position = new(20, 0); character.Position = new(120, 0);
        tree.PhysicsFrame(1d / 60);
        Check(platform.Position.IsEqualApprox(new(20, 0)) && character.Position.IsEqualApprox(new(120, 0)) &&
            PhysicsServer.BodyGetDirectState(platform.GetRID())!.LinearVelocity == Vector2.Zero, "Static manual movement teleports without kinematic contact velocity.");
        root.ProcessMode = ProcessMode.Pausable;
        Check(PhysicsServer.BodyGetMode(platform.GetRID()) == PhysicsServer.BodyMode.Kinematic &&
            PhysicsServer.BodyGetMode(character.GetRID()) == PhysicsServer.BodyMode.Kinematic, "Enable restores both configured kinematic types.");
        root.RemoveChild(platform); root.ProcessMode = ProcessMode.Disabled; root.AddChild(platform);
        Check(PhysicsServer.BodyGetMode(platform.GetRID()) == PhysicsServer.BodyMode.Static, "Exit/reentry reapplies the disabled policy.");
    }

    private static void VerifyFailuresAndPacking(PhysicsServer.Backend backend)
    {
        using var world = new World(backend);
        using var circle = new CircleShape();
        var root = new SubViewport { World = world }; var branch = new Node();
        var first = new StaticBody { Name = "first" }; var second = new StaticBody { Name = "second" };
        var sensor = new Area { Name = "sensor" };
        Add(first, circle); Add(second, circle); Add(sensor, circle);
        branch.AddChild(first); branch.AddChild(second); root.AddChild(branch); root.AddChild(sensor);
        using var tree = new SceneTree(root);
        Check(PhysicsServer.SpaceGetBackend(world.Space) == backend, "The complete scene uses the explicitly selected physics backend."); tree.PhysicsFrame(1d / 60);
        Action<Entity> fail = _ => throw new ApplicationException("departure");
        sensor.BodyExited += fail;
        Check(Capture(() => branch.ProcessMode = ProcessMode.Disabled) is AggregateException &&
            !Attached(first) && !Attached(second) && !sensor.HasOverlappingBodies(),
            "A throwing departure subscriber cannot skip remaining inherited disable transitions.");
        sensor.BodyExited -= fail;
        branch.ProcessMode = ProcessMode.Pausable;
        var hook = new DisableInCallback { Name = "hook", CanSleep = false, GravityScale = 0 };
        Add(hook, circle); root.AddChild(hook); tree.PhysicsFrame(1d / 60);
        var poseMutation = new MutateDuringPose
        {
            Name = "poseMutation",
            GravityScale = 0,
            CanSleep = false,
            LinearVelocity = new(20, 0),
            NotifyLocalTransformChanges = true
        };
        Add(poseMutation, circle); root.AddChild(poseMutation); tree.PhysicsFrame(1d / 60);
        Check(poseMutation.Rejected && poseMutation.ProcessMode == ProcessMode.Inherit &&
            poseMutation.DisableMode == CollisionDisableMode.Remove && !poseMutation.Freeze && Attached(poseMutation),
            "Solver pose callbacks reject policy/process/freeze changes before replacing stored state.");
        Check(hook.Called && !Attached(hook), "Post-solver integration may disable its own object after solver ownership ends.");
        var packedRoot = new SubViewport { World = world };
        var packedBody = new RigidBody { DisableMode = CollisionDisableMode.MakeStatic, ProcessMode = ProcessMode.Disabled };
        packedRoot.AddChild(packedBody); packedBody.Owner = packedRoot;
        using var scene = new PackedScene(); scene.Pack(packedRoot); packedRoot.Dispose();
        using var copyWorld = new World(backend);
        using var copy = (SubViewport)scene.Instantiate(); copy.World = copyWorld;
        using var copyTree = new SceneTree(copy);
        var stored = (RigidBody)copy.GetChild(0);
        Check(PhysicsServer.SpaceGetBackend(stored.GetWorld()!.Space) == backend, "Instantiated scene runs in the explicitly assigned world.");
        Check(stored.DisableMode == CollisionDisableMode.MakeStatic && stored.ProcessMode == ProcessMode.Disabled &&
            PhysicsServer.BodyGetMode(stored.GetRID()) == PhysicsServer.BodyMode.Static, "PackedScene restores policy and effective disabled entry.");
        using var disposed = new StaticBody(); disposed.Dispose();
        Reject<ObjectDisposedException>(() => _ = disposed.DisableMode);
        Reject<ObjectDisposedException>(() => disposed.DisableMode = CollisionDisableMode.KeepActive);
    }

    private static void VerifyWarmFrames(PhysicsServer.Backend backend)
    {
        using var world = new World(backend);
        using var circle = new CircleShape();
        var root = new SubViewport { World = world, ProcessMode = ProcessMode.Disabled };
        var active = new RigidBody { Name = "active", DisableMode = CollisionDisableMode.KeepActive, GravityScale = 0, CanSleep = false };
        var stationary = new AnimatableBody { Name = "stationary", Position = new(100, 0), DisableMode = CollisionDisableMode.MakeStatic };
        var area = new Area { Name = "area", DisableMode = CollisionDisableMode.MakeStatic };
        Add(active, circle); Add(stationary, circle); Add(area, circle);
        root.AddChild(active); root.AddChild(stationary); root.AddChild(area); using var tree = new SceneTree(root);
        Check(PhysicsServer.SpaceGetBackend(world.Space) == backend, "The complete scene uses the explicitly selected physics backend.");
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        _ = GC.GetTotalAllocatedBytes(true);
        var before = GC.GetAllocatedBytesForCurrentThread();
        var allBefore = GC.GetTotalAllocatedBytes(true);
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        var allBytes = GC.GetTotalAllocatedBytes(true) - allBefore;
        Check(ownerBytes == 0 && allBytes == 0, $"Warmed mixed disable-policy solver/sensor frames allocate zero managed bytes: owner={ownerBytes}, all={allBytes}.");
        Console.WriteLine($"Disable-policy warm frames ({backend}): owner={ownerBytes}, all={allBytes} bytes.");
    }

    private sealed class MutateDuringPose : RigidBody
    {
        internal bool Rejected;
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what != NotificationLocalTransformChanged || !IsInsideTree) return;
            Rejected = Capture(() => ProcessMode = ProcessMode.Disabled) is InvalidOperationException &&
                Capture(() => DisableMode = CollisionDisableMode.MakeStatic) is InvalidOperationException &&
                Capture(() => Freeze = true) is InvalidOperationException;
        }
    }

    private sealed class DisableInCallback : RigidBody
    {
        internal bool Called;
        protected override void IntegrateForces(PhysicsDirectBodyState state)
        {
            Called = true;
            ProcessMode = ProcessMode.Disabled;
        }
    }

    private static bool Attached(CollisionObject collider) =>
        (collider is Area ? PhysicsServer.AreaGetSpace(collider.GetRID()) : PhysicsServer.BodyGetSpace(collider.GetRID())) != default;

    private static void Add(CollisionObject collider, Shape shape) => collider.ShapeOwnerAddShape(collider.CreateShapeOwner(null), shape);
    private static Exception? Capture(Action action) { try { action(); return null; } catch (Exception error) { return error; } }
    private static void Reject<T>(Action action) where T : Exception => Check(Capture(action) is T, $"Expected {typeof(T).Name}.");
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
