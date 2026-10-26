using Electron2D;

public static partial class BackendChecks
{
    public static void RunCollisionObjects(PhysicsServer.Backend backend, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            ShapeOwnerTests.Run(backend);
            CollisionDisableModeTests.Run(backend);
            RunShapeOwners(backend);
            CollisionObjectComposition(backend);
            RunPicking(backend, threaded);
            RunPriorityGeometry(backend, threaded);
            Console.WriteLine($"CollisionObject complete declared contract {backend}, threaded={threaded}: passed.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private static void CollisionObjectComposition(PhysicsServer.Backend backend)
    {
        using var world = new World(backend);
        using var root = new SubViewport { World = world };
        using var circle = new CircleShape { Radius = 3 };
        using var sensorShape = new CircleShape { Radius = 140 };
        using var identity = new Resource();
        CollisionObject[] colliders = [new StaticBody(), new AnimatableBody(), new CharacterBody(),
            new RigidBody { GravityScale = 0, CanSleep = false, LinearDampMode = RigidBody.DampMode.Replace },
            new PhysicalBone { GravityScale = 0, CanSleep = false, BoneNodePath = "../Binding", SimulatePhysics = true }, new Area()];
        var owners = new uint[colliders.Length];
        var sensors = new Area[colliders.Length];
        var entered = new HashSet<int>[colliders.Length];
        for (var i = 0; i < colliders.Length; i++)
        {
            var collider = colliders[i]; collider.Position = new(i * 400, 0);
            Check(collider.CollisionLayer == 1 && collider.CollisionMask == 1 && collider.CollisionPriority == 1 &&
                collider.DisableMode == CollisionDisableMode.Remove && collider.InputPickable == (collider is Area),
                "Every sibling preserves the declared filter, priority, disable and picking defaults");
            collider.SetCollisionLayerValue(32, true); collider.SetCollisionMaskValue(32, true);
            collider.SetCollisionLayerValue(1, false); collider.SetCollisionMaskValue(1, false);
            Check(collider.CollisionLayer == 0x80000000 && collider.CollisionMask == 0x80000000 &&
                collider.GetCollisionLayerValue(32) && collider.GetCollisionMaskValue(32), "Bit access retains the unsigned high bit");
            Reject<ArgumentOutOfRangeException>(() => collider.SetCollisionLayerValue(0, true));
            Reject<ArgumentOutOfRangeException>(() => collider.GetCollisionMaskValue(33));
            var a = owners[i] = collider.CreateShapeOwner(identity);
            var b = collider.CreateShapeOwner(null);
            collider.ShapeOwnerAddShape(a, circle); collider.ShapeOwnerAddShape(b, circle); collider.ShapeOwnerAddShape(a, circle);
            collider.ShapeOwnerSetTransform(b, new(0, Vector2.One, 0, new(40, 0)));
            if (collider is Area) PhysicsServer.AreaSetShapeTransform(collider.GetRID(), 2, new(0, Vector2.One, 0, new(80, 0)));
            else PhysicsServer.BodySetShapeTransform(collider.GetRID(), 2, new(0, Vector2.One, 0, new(80, 0)));
            var seen = entered[i] = [];
            var sensor = sensors[i] = new Area { Position = collider.Position + new Vector2(40, 0), CollisionLayer = 0, CollisionMask = 0x80000000 };
            sensor.AddChild(new CollisionShape { Shape = sensorShape });
            sensor.BodyShapeEntered += (rid, _, shape, _) => { if (rid == collider.GetRID()) seen.Add(shape); };
            sensor.AreaShapeEntered += (rid, _, shape, _) => { if (rid == collider.GetRID()) seen.Add(shape); };
            if (collider is PhysicalBone)
            {
                var rig = new Skeleton(); rig.AddChild(new Bone { Name = "Binding", Position = collider.Position });
                rig.AddChild(collider); root.AddChild(rig);
            }
            else root.AddChild(collider);
            root.AddChild(sensor);
        }
        using var tree = new SceneTree(root);
        Check(PhysicsServer.SpaceGetBackend(world.Space) == backend, "The composed collider family uses the explicit backend");
        tree.PhysicsFrame(1d / 120);
        using var point = new PhysicsPointQueryParameters { CollideWithAreas = true };
        var results = new PhysicsPointResult[2];
        for (var i = 0; i < colliders.Length; i++)
        {
            var collider = colliders[i]; var a = owners[i]; var b = collider.ShapeFindOwner(1); var rid = collider.GetRID();
            Check(entered[i].SetEquals([0, 1, 2]), $"{collider.GetType().Name} Area shape events carry append-order indices across interleaved groups: [{string.Join(',', entered[i])}]");
            Hit(collider, 80, 2, true);
            collider.ShapeOwnerRemoveShape(a, 0);
            Check(collider.ShapeOwnerGetShapeIndex(b, 0) == 0 && collider.ShapeOwnerGetShapeIndex(a, 0) == 1 &&
                collider.ShapeFindOwner(1) == a, "Structural edits reindex both owner lists and selected geometry");
            Hit(collider, 80, 1, true);
            collider.ShapeOwnerSetDisabled(a, true);
            Hit(collider, 80, 1, false); Hit(collider, 40, 0, true);
            collider.ProcessMode = ProcessMode.Disabled;
            Hit(collider, 40, 0, false);
            Check(Space(collider) == default && collider.GetRID() == rid && collider.ShapeOwnerGetShapeCount(a) == 1 &&
                ReferenceEquals(collider.ShapeOwnerGetOwner(a), identity), "Remove retains logical geometry, weak identity and RID");
            collider.ShapeOwnerSetTransform(a, new(0, Vector2.One, 0, new(100, 0)));
            collider.DisableMode = CollisionDisableMode.KeepActive;
            Hit(collider, 40, 0, true); Hit(collider, 100, 1, false);
            collider.ShapeOwnerSetDisabled(a, false); Hit(collider, 100, 1, true);
            var mode = collider is Area ? default : PhysicsServer.BodyGetMode(rid);
            collider.DisableMode = CollisionDisableMode.MakeStatic;
            Check(Space(collider) == world.Space && (collider is Area || PhysicsServer.BodyGetMode(rid) == PhysicsServer.BodyMode.Static),
                "MakeStatic preserves both owner groups and selects the static body role");
            Hit(collider, 40, 0, true); Hit(collider, 100, 1, true);
            collider.ProcessMode = ProcessMode.Always;
            Check(collider is Area || PhysicsServer.BodyGetMode(rid) == mode, "Enabling restores each sibling's requested role");
            tree.PhysicsFrame(1d / 120);
            Check(collider is Area area ? sensors[i].OverlapsArea(area) : sensors[i].OverlapsBody((PhysicsBody)collider),
                "Sensor membership resumes after owner edits and disabled-policy transitions");
        }
        var moving = (RigidBody)colliders[3]; moving.LinearVelocity = new(1, 0);
        for (var i = 0; i < 128; i++) Frame();
        Collect(); GC.WaitForPendingFinalizers(); _ = GC.GetTotalAllocatedBytes(true);
        var start = moving.Position;
        var ownerBytes = GC.GetAllocatedBytesForCurrentThread(); var allBytes = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) Frame();
        ownerBytes = GC.GetAllocatedBytesForCurrentThread() - ownerBytes; allBytes = GC.GetTotalAllocatedBytes(true) - allBytes;
        Check(ownerBytes == 0 && allBytes == 0, $"Composed moving collider frames allocated {ownerBytes}/{allBytes} B");
        Check(moving.Position.X > start.X + 1, "The allocation interval retains actual selected rigid-body motion");
        Console.WriteLine($"CollisionObject composition {backend}: six siblings, interleaved slots, disable/role/sensor recovery, 128 moving frames {ownerBytes}/{allBytes} B.");

        void Frame()
        {
            tree.PhysicsFrame(1d / 120);
            foreach (var collider in colliders) { Hit(collider, 40, 0, true); Hit(collider, 100, 1, true); }
        }
        void Hit(CollisionObject collider, float x, int slot, bool expected)
        {
            point.Position = collider.GlobalPosition + new Vector2(x, 0);
            var count = world.DirectSpaceState.IntersectPoint(point, results);
            Check(count == (expected ? 1 : 0) && (!expected || results[0].ColliderRID == collider.GetRID() &&
                ReferenceEquals(results[0].ColliderObject, collider) && results[0].ShapeIndex == slot),
                "Live query identity and logical slot follow the committed owner/disable configuration");
        }
        static RID Space(CollisionObject collider) => collider is Area ? PhysicsServer.AreaGetSpace(collider.GetRID()) : PhysicsServer.BodyGetSpace(collider.GetRID());
    }
}
