using Electron2D;
using System.Diagnostics;

var backend = args.FirstOrDefault() == "gpu" ? PhysicsServer.Backend.GPU : PhysicsServer.Backend.CPU;
using var world = new World(backend); using var other = new World(backend);
using var circle = new CircleShape { Radius = 5 }; using var box = new RectangleShape { Size = new(100, 2) };
using var root = new SubViewport { World = world };
var scene = new RigidBody { CanSleep = false, MaxContactsReported = 4 };
scene.AddChild(new CollisionShape { Shape = circle }); root.AddChild(scene);
var floor = new StaticBody { Position = new(0, 8) }; floor.AddChild(new CollisionShape { Shape = box }); root.AddChild(floor);
using var tree = new SceneTree(root);
var body = PhysicsServer.BodyCreate(); var spare = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate();
try
{
    PhysicsServer.BodyAddShape(body, circle.GetRID()); PhysicsServer.BodySetMass(body, 2); PhysicsServer.BodySetInertia(body, 50);
    PhysicsServer.BodySetGravityScale(body, 0); PhysicsServer.BodySetCanSleep(body, false);
    PhysicsServer.BodySetTransform(body, new(0, new(100, 0))); PhysicsServer.BodySetSpace(body, world.Space);
    PhysicsServer.BodyAddShape(spare, circle.GetRID()); PhysicsServer.BodySetSpace(spare, other.Space);
    for (var i = 0; i < 120; i++) tree.PhysicsFrame(1d / 60);
    using var extension = new ControlledState(body); using var sceneExtension = new ControlledState(scene.GetRID());
    PhysicsDirectBodyState view = extension; PhysicsDirectBodyState sceneView = sceneExtension;
    Check(sceneExtension.CaptureContact(), "Real solved scene contact supplies the public typed contact family.");
    VerifyContacts();
    view.AngularVelocity = .5f; view.LinearVelocity = new(3, 4); view.Sleeping = true; view.Sleeping = false;
    view.Transform = new(.2f, new(100, 0)); view.CollisionLayer = 0x80000002; view.CollisionMask = 0x40000001;
    Check(PhysicsServer.BodyGetCollisionLayer(body) == 0x80000002 && PhysicsServer.BodyGetCollisionMask(body) == 0x40000001,
        "Inherited setters reach the real selected attachment with all authored bits.");
    view.SetConstantForce(new(3, 4)); view.SetConstantTorque(2); view.AddConstantCentralForce(new(1, 0)); view.AddConstantForce(new(0, 2), new(1, 0)); view.AddConstantTorque(3);
    Check(view.GetConstantForce() == new Vector2(4, 6) && MathF.Abs(view.GetConstantTorque() - 7) < .001f, "Persistent positioned force and torque execute.");
    view.ApplyCentralForce(new(2, 0)); view.ApplyForce(new(0, 2), new(1, 0)); view.ApplyTorque(1);
    view.LinearVelocity = Vector2.Zero; view.ApplyCentralImpulse(new(20, 0));
    Check(MathF.Abs(PhysicsServer.BodyGetLinearVelocity(body).X - 20) < .001f, "Consumer hook doubles a real impulse: 40 impulse / 2 mass gives 20 velocity.");
    view.ApplyImpulse(new(0, 2), new(1, 0)); view.ApplyTorqueImpulse(1); view.IntegrateForces();
    _ = view.GetConstantTorque(); _ = view.GetSpaceState(); _ = view.GetVelocityAtLocalPosition(new(1, 2));
    Poll();
    var allHooks = ((1UL << 38) - 1);
    Check((extension.Seen | sceneExtension.Seen) == allHooks, "Every required typed hook executes through inherited public API.");
    Reject<ArgumentOutOfRangeException>(() => view.LinearVelocity = new(float.NaN, 0));
    Reject<ArgumentOutOfRangeException>(() => view.AngularVelocity = float.PositiveInfinity);
    Reject<ArgumentException>(() => view.Transform = new(0, new(2, 1), 0, Vector2.Zero));
    Reject<ArgumentOutOfRangeException>(() => view.ApplyImpulse(new(float.NaN, 0)));
    Reject<ArgumentOutOfRangeException>(() => view.ApplyForce(Vector2.One, new(float.NaN, 0)));
    Reject<ArgumentOutOfRangeException>(() => view.GetVelocityAtLocalPosition(new(float.NaN, 0)));
    Reject<ArgumentOutOfRangeException>(() => sceneView.GetContactImpulse(-1)); Reject<ArgumentOutOfRangeException>(() => sceneView.GetContactCollider(1));
    extension.BadScalar = true; Reject<InvalidOperationException>(() => _ = view.InverseMass); extension.BadScalar = false;
    extension.BadVector = true; Reject<InvalidOperationException>(() => _ = view.LinearVelocity); extension.BadVector = false;
    extension.BadTransform = true; Reject<InvalidOperationException>(() => _ = view.Transform); extension.BadTransform = false;
    extension.BadCount = true; Reject<InvalidOperationException>(() => view.GetContactCount()); extension.BadCount = false;
    extension.WrongSpace = other.DirectSpaceState; Reject<InvalidOperationException>(() => view.GetSpaceState()); extension.WrongSpace = null;
    var validContact = sceneExtension.Contact; sceneExtension.Contact = default;
    Reject<InvalidOperationException>(() => sceneView.GetContactImpulse(0)); sceneExtension.Contact = validContact;
    var depth = 0;
    extension.OnRead = () =>
    {
        if (depth++ == 0)
        {
            _ = view.LinearVelocity;
            Reject<InvalidOperationException>(extension.Dispose);
            Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(body));
            Reject<InvalidOperationException>(() => PhysicsServer.BodySetSpace(body, other.Space));
            Reject<InvalidOperationException>(() => root.World = null);
            Reject<InvalidOperationException>(() => PhysicsServer.SpaceCreateCheckpoint(world.Space));
            Reject<AggregateException>(() => tree.PhysicsFrame(1d / 60));
        }
        depth--;
    };
    _ = view.LinearVelocity; extension.OnRead = null;
    sceneExtension.OnRead = () =>
    {
        Reject<InvalidOperationException>(scene.Dispose);
        Reject<InvalidOperationException>(() => root.RemoveChild(scene));
        Reject<InvalidOperationException>(floor.Dispose);
    };
    _ = sceneView.LinearVelocity; sceneExtension.OnRead = null;
    Check(ReferenceEquals(scene.Parent, root) && scene.GetRID().IsValid() && PhysicsServer.BodyGetSpace(body) == world.Space,
        "Rejected structural mutations preserve scene hierarchy, physical identity and attachment.");
    extension.Fail = true; Reject<InvalidOperationException>(() => _ = view.LinearVelocity); extension.Fail = false;
    _ = view.LinearVelocity;
    using var borrowed = PhysicsServer.BodyGetDirectState(body)!; borrowed.Dispose();
    _ = view.LinearVelocity;
    Reject<ArgumentException>(() => new ControlledState(default)); Reject<ArgumentException>(() => new ControlledState(area));
    var detached = PhysicsServer.BodyCreate(); try { Reject<InvalidOperationException>(() => new ControlledState(detached)); } finally { PhysicsServer.FreeRID(detached); }
    Reject<ArgumentException>(() => new PhysicsBodyContact(body, 0, body, 0, default, default, default, default, default, default));
    Reject<ArgumentException>(() => new PhysicsBodyContact(body, 0, spare, 0, default, default, default, default, default, default));
    Reject<ArgumentOutOfRangeException>(() => new PhysicsBodyContact(body, 2, scene.GetRID(), 0, default, default, default, default, default, default));
    Reject<ArgumentOutOfRangeException>(() => new PhysicsBodyContact(body, 0, scene.GetRID(), 0, new(float.NaN, 0), default, default, default, default, default));
    view.SetConstantForce(Vector2.Zero); view.SetConstantTorque(0); view.LinearVelocity = Vector2.Zero; view.AngularVelocity = 0;
    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers(); GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
    var warm = Stopwatch.StartNew(); var warmCycles = 0;
    do { for (var i = 0; i < 64; i++) Evaluate(); warmCycles += 64; } while (warm.Elapsed.TotalSeconds < 1);
    _ = GC.GetTotalAllocatedBytes(true); _ = GC.GetAllocatedBytesForCurrentThread();
    var all = GC.GetTotalAllocatedBytes(true); var owner = GC.GetAllocatedBytesForCurrentThread();
    for (var i = 0; i < 64; i++) Evaluate();
    owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
    Check(owner == 0 && all == 0, $"Warmed full body-state/contact dispatch allocated {owner}/{all} owner/all-thread B.");
    Task.Run(() => Reject<InvalidOperationException>(() => _ = view.LinearVelocity)).GetAwaiter().GetResult();
    PhysicsServer.BodySetSpace(body, other.Space); Reject<ObjectDisposedException>(() => _ = view.LinearVelocity);
    PhysicsServer.BodySetSpace(body, world.Space); Reject<ObjectDisposedException>(() => _ = view.LinearVelocity);
    using var reopened = new ControlledState(body); _ = reopened.LinearVelocity;
    var sampledRID = validContact.ColliderRID; var sampledID = validContact.ColliderID; var sampledImpulse = validContact.Impulse;
    floor.Dispose();
    Check(sceneView.GetContactCollider(0) == sampledRID && sceneView.GetContactColliderID(0) == sampledID &&
        sceneView.GetContactImpulse(0) == sampledImpulse && sceneView.GetContactColliderObject<Node>(0) is null,
        "Immutable contact survives collider release with sampled identity/impulse and expired weak target.");
    root.RemoveChild(scene); Reject<ObjectDisposedException>(() => _ = sceneView.LinearVelocity); root.AddChild(scene);
    Reject<ObjectDisposedException>(() => _ = sceneView.LinearVelocity);
    using var returnedScene = new ControlledState(scene.GetRID()) { HasContact = true, Contact = validContact };
    Reject<InvalidOperationException>(() => returnedScene.GetContactImpulse(0));
    var ownedSpace = PhysicsServer.SpaceCreate(backend); var ownedBody = PhysicsServer.BodyCreate();
    try
    {
        PhysicsServer.BodyAddShape(ownedBody, circle.GetRID()); PhysicsServer.BodySetSpace(ownedBody, ownedSpace); PhysicsServer.SpaceSetActive(ownedSpace, true);
        using var ownedView = new ControlledState(ownedBody);
        ownedView.OnRead = () =>
        {
            Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(ownedSpace));
            Reject<InvalidOperationException>(() => PhysicsServer.SpaceStep(ownedSpace, 1d / 60));
        };
        _ = ownedView.LinearVelocity; ownedView.OnRead = null;
        if (backend == PhysicsServer.Backend.GPU)
        {
            Reject<AggregateException>(() => PhysicsServer.SpaceStep(ownedSpace, float.MaxValue));
            Reject<InvalidOperationException>(() => _ = ownedView.LinearVelocity);
        }
    }
    finally { PhysicsServer.FreeRID(ownedBody); PhysicsServer.FreeRID(ownedSpace); }
    PhysicsServer.FreeRID(body); body = default; Reject<ObjectDisposedException>(() => _ = reopened.LinearVelocity);
    Console.WriteLine($"{backend}: separate public-only full body-state hooks, real scene/raw forces/contact/motion, validated results, nesting/borrowed lifetime, transfer/reentry and {warmCycles} warm/64 measured cycles: {owner}/{all} B passed.");

    void Poll()
    {
        Check(float.IsFinite(view.AngularVelocity) && view.LinearVelocity.IsFinite() && view.Transform.IsFinite() && view.CenterOfMass.IsFinite() && view.CenterOfMassLocal.IsFinite() &&
            view.InverseMass > 0 && view.InverseInertia > 0 && !view.Sleeping && view.Step > 0 && view.TotalGravity.IsFinite() && float.IsFinite(view.TotalLinearDamp) &&
            float.IsFinite(view.TotalAngularDamp) && view.CollisionLayer == 0x80000002 && view.CollisionMask == 0x40000001, "All inherited state getters.");
    }
    void VerifyContacts()
    {
        var c = sceneExtension.Contact;
        Check(sceneView.GetContactCount() == 1 && sceneView.GetContactCollider(0) == c.ColliderRID && sceneView.GetContactColliderID(0) == c.ColliderID &&
            sceneView.GetContactLocalShape(0) == c.LocalShape && sceneView.GetContactColliderShape(0) == c.ColliderShape &&
            sceneView.GetContactLocalPosition(0) == c.LocalPosition && sceneView.GetContactColliderPosition(0) == c.ColliderPosition &&
            sceneView.GetContactLocalNormal(0) == c.Normal && sceneView.GetContactLocalVelocityAtPosition(0) == c.LocalVelocity &&
            sceneView.GetContactColliderVelocityAtPosition(0) == c.ColliderVelocity && sceneView.GetContactImpulse(0) == c.Impulse &&
            ReferenceEquals(sceneView.GetContactColliderObject(0), floor) && ReferenceEquals(sceneView.GetContactColliderObject<Node>(0), floor), "Complete contact projection preserves captured values and weak scene/object identity.");
    }
    void Evaluate()
    {
        Poll(); VerifyContacts(); _ = view.GetSpaceState(); _ = view.GetVelocityAtLocalPosition(Vector2.One);
        view.LinearVelocity = view.LinearVelocity; view.AngularVelocity = view.AngularVelocity; view.Transform = view.Transform;
        view.CollisionLayer = view.CollisionLayer; view.CollisionMask = view.CollisionMask; view.Sleeping = false;
        view.SetConstantForce(Vector2.Zero); view.SetConstantTorque(0); _ = view.GetConstantForce(); _ = view.GetConstantTorque();
        view.AddConstantCentralForce(); view.AddConstantForce(Vector2.Zero); view.AddConstantTorque(0);
        view.ApplyCentralForce(); view.ApplyForce(Vector2.Zero); view.ApplyTorque(0); view.ApplyCentralImpulse(Vector2.Zero); view.ApplyImpulse(Vector2.Zero); view.ApplyTorqueImpulse(0); view.IntegrateForces();
    }
}
finally { if (body.IsValid()) PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(spare); PhysicsServer.FreeRID(area); }
static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}"); }
