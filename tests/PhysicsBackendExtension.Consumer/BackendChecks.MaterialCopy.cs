using Electron2D;

public static partial class BackendChecks
{
    private static void VerifyMaterialCopy(PhysicsServer.Backend? backend)
    {
        using var material = new PhysicsMaterial(); using var replacement = new PhysicsMaterial();
        var failures = 0;
        Action<Resource> fail = _ => { failures++; throw new InvalidOperationException("Expected material observer failure"); };
        material.Changed += fail;
        using var world = backend is { } selected ? new World(selected) : new World("ContactResponse");
        using var root = new SubViewport { World = world }; using var circle = new CircleShape { Radius = 2 };
        var body = new RigidBody
        {
            Name = "Body",
            Mass = 2,
            Inertia = 2,
            GravityScale = 0,
            CanSleep = false,
            LockRotation = true,
            MaxContactsReported = 4,
            LinearDampMode = RigidBody.DampMode.Replace,
            LinearDamp = 0,
            AngularDampMode = RigidBody.DampMode.Replace,
            AngularDamp = 0,
            PhysicsMaterialOverride = material
        };
        var peer = new StaticBody { Name = "Peer", Position = new(100, 0), PhysicsMaterialOverride = material };
        var stop = new StaticBody { Name = "Stop", Position = new(200, 0) };
        foreach (var node in new PhysicsBody[] { body, peer, stop }) { node.AddChild(new CollisionShape { Shape = circle }); root.AddChild(node); }
        using var tree = new SceneTree(root);
        PhysicsServer.BodySetFriction(body.GetRID(), .9f); PhysicsServer.BodySetBounce(body.GetRID(), .1f);
        PhysicsServer.BodySetFriction(peer.GetRID(), .8f); PhysicsServer.BodySetBounce(peer.GetRID(), .2f);
        PhysicsServer.BodySetFriction(stop.GetRID(), 0); PhysicsServer.BodySetBounce(stop.GetRID(), 0);
        tree.PhysicsFrame(1d / 120); stop.Position = new(3.98f, 0);
        using var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
        foreach (var values in new (float Friction, float Bounce, bool Rough, bool Absorbent)[]
            { (.35f, .75f, true, false), (.15f, .4f, false, true), (.6f, 0, false, false), (.25f, 1, true, false) })
        {
            replacement.Friction = values.Friction; replacement.Bounce = values.Bounce;
            replacement.Rough = values.Rough; replacement.Absorbent = values.Absorbent;
            var count = failures; Reject<InvalidOperationException>(() => material.CopyFromResource(replacement));
            Check(failures == count + 1 && material.Friction == values.Friction && material.Bounce == values.Bounce &&
                material.Rough == values.Rough && material.Absorbent == values.Absorbent,
                "One failed coalesced observer leaves all four copied material values committed");
            Impact(values.Absorbent ? 0 : values.Bounce);
            foreach (var node in new PhysicsBody[] { body, peer })
                Check(ReferenceEquals(node is RigidBody rigid ? rigid.PhysicsMaterialOverride : ((StaticBody)node).PhysicsMaterialOverride, material) &&
                    PhysicsServer.BodyGetFriction(node.GetRID()) == (values.Rough ? -values.Friction : values.Friction) &&
                    PhysicsServer.BodyGetBounce(node.GetRID()) == (values.Absorbent ? -values.Bounce : values.Bounce),
                    "Every borrower polls copied revision and retires body-local surface overrides");
        }

        var folder = System.IO.Path.GetFullPath(System.IO.Path.Combine("bin", "material-copy-assets", Guid.NewGuid().ToString("N")));
        System.IO.Directory.CreateDirectory(folder);
        try
        {
            var path = System.IO.Path.Combine(folder, "material.e2dres"); material.ResourcePath = path;
            replacement.ResourceName = "reloaded"; replacement.Friction = .45f; replacement.Bounce = .5f;
            replacement.Rough = false; replacement.Absorbent = false; ResourceSaver.Save(replacement, path);
            var count = failures;
            Reject<InvalidOperationException>(() => ResourceLoader.Load<PhysicsMaterial>(path, ResourceLoader.CacheMode.Replace));
            Check(failures == count + 1 && ReferenceEquals(ResourceLoader.GetCachedRef<PhysicsMaterial>(path), material) &&
                material.ResourceName == "reloaded" && material.ResourcePath == path && material.Friction == .45f && material.Bounce == .5f,
                "Failed archive replacement notification preserves cache identity and committed copied data");
            Impact(.5f);
        }
        finally { System.IO.Directory.Delete(folder, recursive: true); }
        Reject<InvalidOperationException>(() => material.Bounce = 1); Impact(1);
        material.Changed -= fail;
        for (var i = 0; i < 128; i++) Impact(1);
        Collect(); var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) Impact(1);
        owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(owner == 0 && all == 0, $"Recovered material contacts allocate {owner}/{all} B");
        Console.WriteLine($"Material copy {backend?.ToString() ?? "registered"}, threaded={ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread)}: four values, missed observers, shared borrowers, archive reload and 128 full contact frames {owner}/{all} B passed.");

        void Impact(float bounce)
        {
            body.Position = Vector2.Zero; body.LinearVelocity = new(30, 0); body.AngularVelocity = 0;
            tree.PhysicsFrame(1d / 120);
            if (MathF.Abs(body.LinearVelocity.X + 30 * bounce) >= .5f)
                throw new InvalidOperationException($"Copied bounce after a missed observer reaches real contact response: {body.LinearVelocity.X}, expected {-30 * bounce}");
            Check(state.GetContactCount() == 1 && state.GetContactCollider(0) == stop.GetRID() &&
                state.GetContactImpulse(0).DistanceTo((body.LinearVelocity - new Vector2(30, 0)) * 2) < .1f,
                "Copied material response retains contact identity and full-step momentum impulse");
        }
    }
}
