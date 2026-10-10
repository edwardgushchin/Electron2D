using Electron2D;

internal static class PhysicsAreaBackendTests
{
    internal static void Run(PhysicsServer.Backend backend)
    {
        using var world = new World(backend);
        using var region = new RectangleShape { Size = new(1000, 1000) };
        using var circle = new CircleShape { Radius = 3 };
        using var root = new SubViewport { World = world };
        var sceneArea = new Area
        {
            CollisionMask = 1u << 7,
            Priority = 5,
            Gravity = 20,
            GravitySpaceOverride = Area.SpaceOverride.Combine,
            AudioBusOverride = true,
            AudioBusName = "Region"
        };
        var slots = sceneArea.CreateShapeOwner(null);
        sceneArea.ShapeOwnerAddShape(slots, region); sceneArea.ShapeOwnerAddShape(slots, region);
        var sceneBody = new RigidBody { CanSleep = false, CollisionLayer = 1u << 7, CollisionMask = 0 };
        sceneBody.AddChild(new CollisionShape { Shape = circle });
        root.AddChild(sceneArea); root.AddChild(sceneBody);
        using var tree = new SceneTree(root);
        var space = world.Space;
        var rawArea = PhysicsServer.AreaCreate(); var rawBody = PhysicsServer.BodyCreate();
        var otherSpace = PhysicsServer.SpaceCreate(backend);
        var sceneEntries = 0; var sceneExits = 0; var rawEntries = 0; var rawExits = 0;
        sceneArea.BodyShapeEntered += (_, _, remote, local) =>
        {
            Check(remote == 0 && local is 0 or 1, "Scene observer reports logical slots"); sceneEntries++;
        };
        sceneArea.BodyShapeExited += (_, _, _, _) => sceneExits++;
        try
        {
            PhysicsServer.AreaSetGravity(space, 0); PhysicsServer.AreaSetLinearDamp(space, 0); PhysicsServer.AreaSetAngularDamp(space, 0);
            PhysicsServer.BodyAddShape(rawBody, circle.GetRID()); PhysicsServer.BodySetCanSleep(rawBody, false);
            PhysicsServer.BodySetCollisionLayer(rawBody, 1u << 7); PhysicsServer.BodySetCollisionMask(rawBody, 0);
            PhysicsServer.BodySetTransform(rawBody, new(0, new(20, 0))); PhysicsServer.BodySetSpace(rawBody, space);
            PhysicsServer.AreaAddShape(rawArea, region.GetRID()); PhysicsServer.AreaSetCollisionMask(rawArea, 1u << 7);
            PhysicsServer.AreaSetPriority(rawArea, 10); PhysicsServer.AreaSetGravity(rawArea, 40);
            PhysicsServer.AreaSetGravityVector(rawArea, new(1, 0));
            PhysicsServer.AreaSetGravitySpaceOverride(rawArea, Area.SpaceOverride.ReplaceCombine);
            PhysicsServer.AreaSetLinearDamp(rawArea, 1); PhysicsServer.AreaSetLinearDampSpaceOverride(rawArea, Area.SpaceOverride.Replace);
            PhysicsServer.AreaSetAngularDamp(rawArea, 2); PhysicsServer.AreaSetAngularDampSpaceOverride(rawArea, Area.SpaceOverride.Replace);
            PhysicsServer.AreaSetSpace(rawArea, space);
            PhysicsServer.AreaSetMonitorCallback(rawArea, (status, rid, _, remote, local) =>
            {
                Check((rid == rawBody || rid == sceneBody.GetRID()) && remote == 0 && local == 0, "Raw observer reports scene and raw body identities");
                if (status == PhysicsServer.AreaBodyStatus.Added) rawEntries++; else rawExits++;
            });
            sceneBody.LinearVelocity = new(120, 0); sceneBody.AngularVelocity = 6;
            PhysicsServer.BodySetLinearVelocity(rawBody, new(120, 0)); PhysicsServer.BodySetAngularVelocity(rawBody, 6);
            tree.PhysicsFrame(1d / 60);
            Near(sceneBody.GetGravity(), new(40, 20), "Scene receiver gets deduplicated mixed fields");
            var state = PhysicsServer.BodyGetDirectState(rawBody) ?? throw new InvalidOperationException("Attached body must provide live state");
            Near(state.TotalGravity, new(40, 20), "Raw receiver gets the same mixed fields");
            Near(state.LinearVelocity, new(118 + 40f / 60, 20f / 60), "Damping precedes force integration");
            Check(MathF.Abs(state.AngularVelocity - 5.8f) < .002f && state.TotalLinearDamp == 1 && state.TotalAngularDamp == 2,
                "Independent damping channels are published");
            Check(sceneEntries == 4 && rawEntries == 2 && sceneExits == 0 && rawExits == 0,
                "Scene and raw monitors observe each logical pair once");
            Check(tree.ResolveSpatialAudioBus(sceneBody, Vector2.Zero, 1, "Master") == "Region" &&
                tree.ResolveSpatialAudioBus(sceneBody, new(2000, 2000), 1, "Master") == "Master" &&
                tree.ResolveSpatialAudioBus(sceneBody, Vector2.Zero, 2, "Master") == "Master", "Selected audio containment and masks");
            sceneArea.Monitoring = false; sceneArea.Monitorable = false;
            tree.PhysicsFrame(1d / 60); Near(sceneBody.GetGravity(), new(40, 20), "Monitoring flags do not disable field or audio participation");
            Check(tree.ResolveSpatialAudioBus(sceneBody, Vector2.Zero, 1, "Master") == "Region", "Audio uses shape containment independently of monitoring");
            sceneArea.Monitoring = true; tree.PhysicsFrame(1d / 60);
            var entries = sceneEntries; var rawObserved = rawEntries;
            Collect();
            for (var i = 0; i < 64; i++)
            {
                tree.PhysicsFrame(1d / 60);
                _ = tree.ResolveSpatialAudioBus(sceneBody, Vector2.Zero, 1, "Master");
                _ = tree.ResolveSpatialAudioBus(sceneBody, new(2000, 2000), 1, "Master");
                _ = state.TotalGravity;
            }
            var all = GC.GetTotalAllocatedBytes(true); var owner = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++)
            {
                tree.PhysicsFrame(1d / 60);
                _ = tree.ResolveSpatialAudioBus(sceneBody, Vector2.Zero, 1, "Master");
                _ = tree.ResolveSpatialAudioBus(sceneBody, new(2000, 2000), 1, "Master");
                _ = state.TotalGravity;
            }
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0, $"Area step/audio/field reads allocate {owner}/{all} owner/all-thread bytes");
            Check(sceneEntries == entries && rawEntries == rawObserved && rawExits == 0, "Warmed scans preserve observer history");
            sceneArea.ShapeOwnerSetDisabled(slots, true); tree.PhysicsFrame(1d / 60);
            Near(sceneBody.GetGravity(), new(40, 0), "Disabled scene slots leave field reduction");
            Check(tree.ResolveSpatialAudioBus(sceneBody, Vector2.Zero, 1, "Master") == "Master", "Disabled slots leave audio containment");
            sceneArea.ShapeOwnerSetDisabled(slots, false); tree.PhysicsFrame(1d / 60);
            PhysicsServer.AreaSetShapeDisabled(rawArea, 0, true); tree.PhysicsFrame(1d / 60);
            Near(state.TotalGravity, new(0, 20), "Disabled raw slot leaves field reduction");
            Check(rawExits == 2, "Disabled raw slot publishes both observer departures");
            PhysicsServer.AreaSetShapeDisabled(rawArea, 0, false); tree.PhysicsFrame(1d / 60);
            Check(rawEntries == rawObserved + 2, "Restored raw slot reenters both receivers");
            PhysicsServer.AreaSetSpace(rawArea, otherSpace); tree.PhysicsFrame(1d / 60);
            Near(sceneBody.GetGravity(), new(0, 20), "Transferred area cannot remain in the previous owner reducer");
            PhysicsServer.AreaSetSpace(rawArea, space); tree.PhysicsFrame(1d / 60);
            Near(state.TotalGravity, new(40, 20), "Area reentry refreshes the selected owner's geometry");
            PhysicsServer.FreeRID(rawArea); rawArea = default; tree.PhysicsFrame(1d / 60);
            Near(state.TotalGravity, new(0, 20), "Freed area leaves no retained field influence");
            Console.WriteLine($"{backend}: scene/raw Area fields, logical monitoring, masks, audio containment, disable/transfer/reentry/free and 64 warm step/audio/field cycles: {owner}/{all} owner/all-thread B passed.");
        }
        finally
        {
            if (rawArea.IsValid()) PhysicsServer.FreeRID(rawArea);
            PhysicsServer.FreeRID(rawBody); PhysicsServer.FreeRID(otherSpace);
        }
    }
    private static void Collect() { GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers(); GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); }
    private static void Near(Vector2 actual, Vector2 expected, string message) => Check(actual.DistanceTo(expected) < .002f, $"{message}: {actual} != {expected}");
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
