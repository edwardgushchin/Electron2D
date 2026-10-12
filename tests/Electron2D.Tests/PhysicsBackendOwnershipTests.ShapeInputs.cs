using Electron2D;

internal static partial class PhysicsBackendOwnershipTests
{
    private delegate void ShapeInputHook(PhysicsColliderBackend owner, ReadOnlySpan<PhysicsColliderShape> slots, bool sensor);
    private sealed class InputShape : Shape
    {
        internal Action? BuildHook;
        internal void Invalidate() { EnsureGeometryChangeAccess(); EmitGeometryChanged(); }
        protected override ShapeGeometry GetGeometryCore() { BuildHook?.Invoke(); return ShapeGeometry.Circle(3); }
    }
    private static void ShapeInputs(PhysicsServer.Backend backend)
    {
        using var shape = new InputShape();
        var observed = new HashSet<RID>(); var validate = false;
        using var world = new World(new WorldRuntime(false, backend, factory: space => new ViewBackend(space, backend) { ShapeHook = Inspect }));
        using var root = new SubViewport { World = world };
        var scene = new StaticBody(); var sensor = new Area { Position = new(0, 60) };
        Configure(scene); Configure(sensor);
        root.AddChild(scene); root.AddChild(sensor);
        using var tree = new SceneTree(root);
        var raw = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate();
        try
        {
            PhysicsServer.BodySetMode(raw, PhysicsServer.BodyMode.Static);
            for (var i = 0; i < 3; i++)
            {
                var transform = new Transform(i == 0 ? Mathf.Pi / 2 : 0, new((i - 1) * 20, 0));
                PhysicsServer.BodyAddShape(raw, shape.GetRID(), transform, i == 1);
                PhysicsServer.AreaAddShape(area, shape.GetRID(), transform, i == 1);
            }
            PhysicsServer.BodySetShapeAsOneWayCollision(raw, 0, true, 7, Vector2.Right);
            PhysicsServer.BodySetTransform(raw, new(0, new(0, 30))); PhysicsServer.AreaSetTransform(area, new(0, new(0, 90)));
            PhysicsServer.BodySetSpace(raw, world.Space); PhysicsServer.AreaSetSpace(area, world.Space);
            validate = true;
            scene.MarkShapesDirty(); sensor.MarkShapesDirty(); tree.PhysicsFrame(1d / 60);
            PhysicsServer.BodySetCollisionLayer(raw, 2); PhysicsServer.AreaSetCollisionLayer(area, 2);
            PhysicsServer.Service.BodyRuntime(raw).Owners.Server!.RebuildShapes();
            PhysicsServer.Service.ResolveAreaOwners(area).Server!.RebuildShapes();
            Check(observed.SetEquals([scene.GetRID(), sensor.GetRID(), raw, area]), "The same neutral shape input family reaches selected scene/raw bodies and Areas");
            var direct = world.DirectSpaceState;
            foreach (var (rid, y, isArea) in new[] { (scene.GetRID(), 0f, false), (raw, 30f, false), (sensor.GetRID(), 60f, true), (area, 90f, true) })
            {
                using var point = new PhysicsPointQueryParameters { Position = new(20, y), CollideWithBodies = !isArea, CollideWithAreas = isArea };
                Check(direct.IntersectPoint(point).Any(hit => hit.ColliderRID == rid && hit.ShapeIndex == 2), "Disabled slots preserve later logical query indices");
                point.Position = new(0, y);
                Check(!direct.IntersectPoint(point).Any(hit => hit.ColliderRID == rid), "Disabled input slots produce no native collision geometry");
            }
            shape.Invalidate();
            shape.BuildHook = () => scene.Backend.RebuildShapes(scene.ShapeSlots, scene.CollisionLayer, scene.CollisionMask, false, 1);
            Reject<InvalidOperationException>(() => scene.Backend.RebuildShapes(scene.ShapeSlots, scene.CollisionLayer, scene.CollisionMask, false, 1));
            Check(scene.Backend.ShapeCount == 2, "Rejected recursive preparation preserves previous native geometry");
            shape.BuildHook = null;
            tree.PhysicsFrame(1d / 60);
            Check(scene.Backend.ShapeCount == 2 && PhysicsServer.BodyGetShapeCount(raw) == 3, "Failed input preparation is retryable without collapsing authoring slots");
            Console.WriteLine($"{backend}: neutral scene/raw body/Area shape inputs, disabled logical indices, rotated one-way metadata and recursive preparation recovery passed.");
        }
        finally { PhysicsServer.FreeRID(raw); PhysicsServer.FreeRID(area); }
        void Inspect(PhysicsColliderBackend owner, ReadOnlySpan<PhysicsColliderShape> slots, bool isSensor)
        {
            if (!validate) return;
            Check(slots.Length == 3, "Selected input preserves complete logical slot count");
            for (var i = 0; i < slots.Length; i++)
                Check(slots[i].Index == i && ReferenceEquals(slots[i].Shape, shape) && slots[i].Enabled == (i != 1), "Neutral logical slot identity/resource/disabled state is complete");
            Check(slots[0].Transform.Origin == new Vector2(-20, 0) && slots[2].Transform.Origin == new Vector2(20, 0), "Selected input carries authored local transforms");
            if (!isSensor)
                Check(slots[0].OneWay is { } oneWay && oneWay.LocalDirection.DistanceTo(Vector2.Down) < .00001f && oneWay.Margin == 7,
                    "Selected input preserves rotated one-way direction and margin");
            else foreach (ref readonly var slot in slots) Check(slot.OneWay is null, "Sensor inputs omit body-only one-way response");
            observed.Add(owner.RID);
        }
        void Configure(CollisionObject receiver)
        {
            for (var i = 0; i < 3; i++)
            {
                var owner = receiver.CreateShapeOwner(null);
                receiver.ShapeOwnerSetTransform(owner, new(i == 0 ? Mathf.Pi / 2 : 0, new((i - 1) * 20, 0)));
                receiver.ShapeOwnerSetDisabled(owner, i == 1);
                if (i == 0)
                {
                    receiver.ShapeOwnerSetOneWayCollision(owner, true); receiver.ShapeOwnerSetOneWayCollisionMargin(owner, 7);
                    receiver.ShapeOwnerSetOneWayCollisionDirection(owner, Vector2.Right);
                }
                receiver.ShapeOwnerAddShape(owner, shape);
            }
        }
    }
}
