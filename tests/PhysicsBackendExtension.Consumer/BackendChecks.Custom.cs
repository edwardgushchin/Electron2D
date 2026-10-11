using Electron2D;

public static partial class BackendChecks
{
    private static void CustomChecks()
    {
        CustomDataGuards();
        var data = Annulus.Create(5, 10);
        using var ring = new CustomShape(data); using var circle = new CircleShape { Radius = 1 };
        Check(ring.GetRect() == new Rect2(-10, -10, 20, 20) && PhysicsServer.ShapeGetType(ring.GetRID()) == PhysicsServer.ShapeType.Custom, "Custom exact data and type preserve the ring hole");
        Check(!ring.Collide(Transform.Identity, circle, Transform.Identity), "Standalone custom pair leaves the hole empty");
        Check(ring.Collide(Transform.Identity, circle, new(0, new(5, 0))) && circle.Collide(new(0, new(5, 0)), ring, Transform.Identity), "Custom standalone collision executes in both argument orders");
        Check(ring.CollideWithMotion(Transform.Identity, Vector2.Zero, circle, Transform.Identity, new(10, 0)), "Custom swept collision reaches the inner wall");
        using var contactCircle = new CircleShape { Radius = 1 };
        var pairs = ring.CollideAndGetContacts(Transform.Identity, contactCircle, new(0, new(5, 0)));
        Check(pairs.Length == 2 && pairs[0].DistanceTo(new(5, 0)) < .00001f, "Custom pair supplies real inner boundary contact points");
        var rawShape = PhysicsServer.CustomShapeCreate(data); var raw = PhysicsServer.BodyCreate();
        try
        {
            using var copied = (CustomShape)PhysicsServer.ShapeGetData(rawShape);
            Check(!ReferenceEquals(copied, ring) && ReferenceEquals(copied.Geometry, data), "Server custom data copies a resource while borrowing immutable geometry");
            PhysicsServer.BodyAddShape(raw, rawShape); PhysicsServer.BodySetMass(raw, 8);
            Check(PhysicsServer.BodyGetInertia(raw) == 0, "Detached automatic getter preserves unresolved configured semantics");
            PhysicsServer.BodyApplyTorqueImpulse(raw, 0);
            Check(Math.Abs(PhysicsServer.BodyGetInertia(raw) - 500) < .001f, $"Detached custom mass preserves the exact annulus polar inertia: {PhysicsServer.BodyGetInertia(raw)}");
            PhysicsServerManager.RegisterServer("Annulus", () => new Solver());
            using var world = new World("Annulus");
            using var root = new SubViewport { World = world };
            var moving = new RigidBody { Mass = 2, GravityScale = 0, CanSleep = false, Position = new(4.5f, 0), LinearVelocity = new(60, 0) };
            moving.AddChild(new CollisionShape { Shape = circle }); root.AddChild(moving);
            var area = new Area(); area.AddChild(new CollisionShape { Shape = ring }); root.AddChild(area);
            using var tree = new SceneTree(root);
            PhysicsServer.AreaSetGravity(world.Space, 0); PhysicsServer.AreaSetLinearDamp(world.Space, 0); PhysicsServer.AreaSetAngularDamp(world.Space, 0);
            PhysicsServer.BodySetGravityScale(raw, 0); PhysicsServer.BodySetCanSleep(raw, false); PhysicsServer.BodySetSpace(raw, world.Space);
            PhysicsServer.BodySetMaxContactsReported(raw, 2); moving.MaxContactsReported = 2;
            tree.PhysicsFrame(1d / 60);
            Check(Math.Abs(8 * PhysicsServer.BodyGetLinearVelocity(raw).X + 2 * moving.LinearVelocity.X - 120) < .001f && moving.LinearVelocity.X < 60,
                "Custom inner-wall contact exchanges actual momentum with a scene body");
            var state = PhysicsServer.BodyGetDirectState(moving.GetRID())!;
            Check(state.GetContactCount() == 1 && state.GetContactCollider(0) == raw && state.GetContactLocalNormal(0).X < -.9f && state.GetContactImpulse(0).X < 0,
                "Custom inner wall publishes physical scene/raw contact identity, normal and impulse");
            Check(area.GetOverlappingBodies().Contains(moving), "Custom Area overlap uses annulus geometry");
            using var point = new PhysicsPointQueryParameters { Position = PhysicsServer.BodyGetTransform(raw).Origin, CollideWithAreas = true, Exclude = [moving.GetRID()] };
            Check(world.DirectSpaceState.IntersectPoint(point).Length == 0, "Selected point queries do not fill the custom hole");
            using var ray = new PhysicsRayQueryParameters { From = point.Position, To = point.Position + new Vector2(8, 0), Exclude = [moving.GetRID()] };
            var hit = world.DirectSpaceState.IntersectRay(ray);
            Check(hit is not null && hit.Value.ColliderRID == raw && hit.Value.Position.DistanceTo(point.Position + new Vector2(5, 0)) < .00001f, "Selected ray exits the hole at its real inner boundary");
            using var query = new PhysicsShapeQueryParameters { Shape = circle, Transform = new(0, point.Position), Margin = 0, Exclude = [moving.GetRID()] };
            Check(world.DirectSpaceState.IntersectShape(query).Length == 0, "Selected shape query leaves custom hole empty");
            query.Motion = new(8, 0);
            Check(Math.Abs(world.DirectSpaceState.CastMotion(query).SafeFraction - .5f) < .00001f, "Custom shape sweep reaches the inner boundary at the analytic fraction");
            using var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space);
            using var map = new PhysicsSnapshotMap(world.Space); map.Bind(1, 1, raw); map.Bind(2, 1, moving.GetRID()); map.Bind(3, 1, area.GetRID());
            var snapshot = new PhysicsSnapshot(65536); map.Capture(snapshot);
            var pose = PhysicsServer.BodyGetTransform(raw); tree.PhysicsFrame(1d / 60); checkpoint.Restore();
            Check(PhysicsServer.BodyGetTransform(raw) == pose, "Custom private history restores actual motion");
            tree.PhysicsFrame(1d / 60); map.Apply(snapshot);
            Check(PhysicsServer.BodyGetTransform(raw) == pose, "Custom portable history restores matching canonical authoring");
            using var changed = new CustomShape(Annulus.Create(4, 10)); PhysicsServer.ShapeSetData(rawShape, changed);
            Check(PhysicsServer.ShapeGetType(rawShape) == PhysicsServer.ShapeType.Custom && PhysicsServer.BodyGetInertia(raw) != 500, "Server data mutation rebuilds the same RID with changed exact mass");
            Reject<InvalidOperationException>(() => checkpoint.Restore());
            var before = PhysicsServer.BodyGetTransform(raw); Reject<InvalidDataException>(() => map.Apply(snapshot));
            Check(PhysicsServer.BodyGetTransform(raw) == before, "Changed canonical geometry rejects portable restore before mutation");
            tree.PhysicsFrame(1d / 60);
            for (var i = 0; i < 64; i++) tree.PhysicsFrame(1d / 60);
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) tree.PhysicsFrame(1d / 60);
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0, $"Custom full frames allocated {owner}/{all} bytes");
            Console.WriteLine($"Custom annulus: real hole, mass, scene/raw impulse, Area/query, mutation and history, 64 full warm frames {owner}/{all} B passed.");
            PhysicsServer.BodySetSpace(raw, default);
        }
        finally { PhysicsServer.FreeRID(raw); PhysicsServer.FreeRID(rawShape); }
        BuiltInRejectsCustom(PhysicsServer.Backend.CPU, data, circle);
        if (Environment.GetEnvironmentVariable("ELECTRON2D_CUSTOM_GPU") == "1") BuiltInRejectsCustom(PhysicsServer.Backend.GPU, data, circle);
    }
    private sealed class CallbackShape(ShapeGeometry data, Action read) : Shape
    {
        protected override ShapeGeometry GetGeometryCore() { read(); return data; }
    }
    private static void BuiltInRejectsCustom(PhysicsServer.Backend backend, ShapeGeometry data, CircleShape circle)
    {
        using var world = new World(backend); using var shape = new CustomShape(data);
        var body = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.BodyAddShape(body, circle.GetRID()); PhysicsServer.BodySetSpace(body, world.Space);
            var view = PhysicsServer.BodyGetDirectState(body); var pose = PhysicsServer.BodyGetTransform(body);
            using var point = PhysicsServer.SpaceCreateCheckpoint(world.Space);
            Reject<NotSupportedException>(() => PhysicsServer.BodyAddShape(body, shape.GetRID()));
            Reject<NotSupportedException>(() => PhysicsServer.BodySetShape(body, 0, shape.GetRID()));
            using var callbackShape = new CallbackShape(data, () => Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(body)));
            Reject<NotSupportedException>(() => PhysicsServer.BodyAddShape(body, callbackShape.GetRID()));
            point.Restore();
            Check(PhysicsServer.BodyGetShapeCount(body) == 1 && PhysicsServer.BodyGetTransform(body) == pose && ReferenceEquals(view, PhysicsServer.BodyGetDirectState(body)), "Built-in rejection preserves prior geometry, body and direct identity");
            PhysicsServer.BodyAddShape(body, shape.GetRID(), disabled: true);
            Reject<NotSupportedException>(() => PhysicsServer.BodySetShapeDisabled(body, 1, false));
            Check(PhysicsServer.BodyGetShapeCount(body) == 2 && PhysicsServer.BodyGetShape(body, 1) == shape.GetRID(), "Built-in enable rejection retains authored slots");
            PhysicsServer.BodyRemoveShape(body, 1);
            PhysicsServer.BodySetSpace(body, default); PhysicsServer.BodyClearShapes(body); PhysicsServer.BodyAddShape(body, shape.GetRID());
            Reject<NotSupportedException>(() => PhysicsServer.BodySetSpace(body, world.Space));
            Check(!PhysicsServer.BodyGetSpace(body).IsValid(), "Unsupported transfer rejects before publishing builtin attachment");
            PhysicsServer.BodyClearShapes(body); PhysicsServer.BodyAddShape(body, callbackShape.GetRID());
            Reject<NotSupportedException>(() => PhysicsServer.BodySetSpace(body, world.Space));
            Check(PhysicsServer.BodyGetShapeCount(body) == 1 && !PhysicsServer.BodyGetSpace(body).IsValid(), "Procedural transfer preflight retains the detached collider during lifetime callbacks");
        }
        finally { PhysicsServer.FreeRID(body); }
    }
}
