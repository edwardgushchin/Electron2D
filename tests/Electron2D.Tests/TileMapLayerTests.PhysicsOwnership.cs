using Electron2D;

internal static partial class TileMapLayerTests
{
    private static void OwnerCanvasLifetime(PhysicsServer.Backend backend)
    {
        using var assets = new Assets(); using var world = new World(backend);
        var root = new SubViewport { World = world }; var canvas = new CanvasLayer();
        var layer = new TileMapLayer { TileSet = assets.Set, PhysicsQuadrantSize = 1 };
        layer.SetCell(default, 3, default(Vector2i)); canvas.AddChild(layer); root.AddChild(canvas);
        using var tree = new SceneTree(root); layer.UpdateInternals(); tree.PhysicsFrame(1d / 60);
        using var query = new PhysicsPointQueryParameters { Position = new(8, 8) };
        var hits = new PhysicsPointResult[2]; var direct = world.DirectSpaceState;
        Check(direct.IntersectPoint(query, hits) == 0, "Default canvas excludes a tile body in another canvas.");
        query.CanvasInstanceID = canvas.InstanceID;
        Check(direct.IntersectPoint(query, hits) == 1 && hits[0].ColliderObject == layer && hits[0].ShapeIndex == 0 &&
            layer.GetCoordsForBodyRID(hits[0].ColliderRID) == default, "Canvas-qualified point hits retain tile layer, body and logical slot.");
        var sampled = hits[0]; var identity = layer.InstanceID;
        for (var i = 0; i < 64; i++) { tree.PhysicsFrame(1d / 60); direct.IntersectPoint(query, hits); }
        _ = GC.GetTotalAllocatedBytes(true); var ownerBefore = GC.GetAllocatedBytesForCurrentThread(); var allBefore = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 64; i++)
        {
            tree.PhysicsFrame(1d / 60);
            Check(direct.IntersectPoint(query, hits) == 1 && hits[0].ColliderObject == layer && layer.HasBodyRID(hits[0].ColliderRID), "Warm tile query retains owner identity.");
        }
        var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - ownerBefore; var allBytes = GC.GetTotalAllocatedBytes(true) - allBefore;
        Check(ownerBytes == 0 && allBytes == 0, $"Warmed tile frames and canvas-qualified queries: owner={ownerBytes}, all={allBytes}.");
        Console.WriteLine($"Tile owner/canvas warm frames ({backend}): owner={ownerBytes}, all={allBytes} bytes.");
        canvas.RemoveChild(layer); root.AddChild(layer); tree.FlushDeferred(); tree.PhysicsFrame(1d / 60);
        Check(direct.IntersectPoint(query, hits) == 0, "Reparenting retires old canvas membership.");
        query.CanvasInstanceID = 0;
        Check(direct.IntersectPoint(query, hits) == 1 && hits[0].ColliderObject == layer && sampled.ColliderObject == layer &&
            !layer.HasBodyRID(sampled.ColliderRID), "A sampled tile identity survives physical-body replacement while fresh queries use the new canvas.");
        layer.Dispose();
        Check(sampled.ColliderObject is null && sampled.ColliderID == identity && direct.IntersectPoint(query, hits) == 0,
            "Tile owner disposal invalidates weak payloads while preserving their sampled ID and removing query geometry.");
    }

    private static void OwnerRetirementFailures(PhysicsServer.Backend backend)
    {
        using var assets = new Assets(); using var world = new World(backend);
        using var navigation = new NavigationPolygon();
        navigation.SetVertices([new(0, 0), new(16, 0), new(0, 16)]); navigation.AddPolygon([0, 1, 2]); assets.Set.AddNavigationLayer();
        assets.Atlas.GetTileData(default, 0).SetNavigationPolygon(0, navigation);
        var root = new SubViewport { World = world };
        var layer = new TileMapLayer { Name = "Tiles", TileSet = assets.Set, PhysicsQuadrantSize = 1 };
        layer.SetCell(default, 3, default(Vector2i)); layer.SetCell(new(1, 0), 3, default(Vector2i));
        using var shape = new RectangleShape { Size = new(64, 32) };
        var sensor = new Area { Position = new(16, 8) }; sensor.AddChild(new CollisionShape { Shape = shape });
        root.AddChild(layer); root.AddChild(sensor); using var tree = new SceneTree(root);
        layer.UpdateInternals(); tree.PhysicsFrame(1d / 60);
        Check(PhysicsServer.SpaceGetBackend(world.Space) == backend, "Tile ownership uses the explicitly selected backend.");
        Check(NavigationServer.MapGetRegions(world.NavigationMap).Length == 2, "Tile ownership fixture has live navigation consumers.");
        var first = Hit(world.DirectSpaceState, new(8, 8)).ColliderRID;
        var neighbor = Hit(world.DirectSpaceState, new(24, 8)).ColliderRID;
        var exits = 0; var shapeExits = new HashSet<RID>();
        sensor.BodyExited += body => { if (body == layer) exits++; };
        Action<RID, Entity?, int, int> fail = (rid, body, other, local) =>
        {
            if (body != layer && rid != first && rid != neighbor) return;
            Check(other == 0 && local == 0, "Tile departure retains remote/local logical shape indices.");
            shapeExits.Add(rid); throw new ApplicationException("tile departure");
        };
        sensor.BodyShapeExited += fail;
        layer.SetCell(default, 3, default(Vector2i), TileSetAtlasSource.TransformFlipH);
        Reject<AggregateException>(layer.UpdateInternals);
        sensor.BodyShapeExited -= fail;
        var replacement = Hit(world.DirectSpaceState, new(8, 8));
        Check(!layer.HasBodyRID(first) && layer.HasBodyRID(replacement.ColliderRID) && replacement.ColliderObject == layer &&
            replacement.ColliderRID != first && Hit(world.DirectSpaceState, new(24, 8)).ColliderRID == neighbor,
            "Retirement callback failure preserves ownership of the prepared replacement and the untouched quadrant.");
        Check(shapeExits.SetEquals([first]) && exits == 0 && sensor.OverlapsBody(layer),
            "Removing one generated body retains the deduplicated tile owner while another body overlaps.");
        tree.FlushDeferred(); tree.PhysicsFrame(1d / 60);
        first = Hit(world.DirectSpaceState, new(8, 8)).ColliderRID;
        neighbor = Hit(world.DirectSpaceState, new(24, 8)).ColliderRID;
        exits = 0; shapeExits.Clear();
        sensor.BodyShapeExited += fail;
        layer.CollisionEnabled = false;
        Reject<AggregateException>(layer.UpdateInternals);
        sensor.BodyShapeExited -= fail;
        Check(shapeExits.SetEquals([first, neighbor]) && exits == 1 && !layer.HasBodyRID(first) && !layer.HasBodyRID(neighbor) &&
            !sensor.HasOverlappingBodies() && !sensor.GetOverlappingBodies().Contains(layer) &&
            Hits(world.DirectSpaceState, new(8, 8)) == 0 && Hits(world.DirectSpaceState, new(24, 8)) == 0,
            "A throwing departure cannot leave later tile bodies, owner mappings or overlap membership alive.");
        Reject<ArgumentException>(() => PhysicsServer.BodyGetSpace(first));
        Reject<ArgumentException>(() => PhysicsServer.BodyGetSpace(neighbor));
        layer.CollisionEnabled = true; tree.FlushDeferred(); tree.PhysicsFrame(1d / 60);
        Check(sensor.OverlapsBody(layer) && sensor.GetOverlappingBodies().Count(body => body == layer) == 1 &&
            layer.HasBodyRID(Hit(world.DirectSpaceState, new(8, 8)).ColliderRID), "Corrected authoring recreates owned geometry after callback failure.");
        first = Hit(world.DirectSpaceState, new(8, 8)).ColliderRID;
        neighbor = Hit(world.DirectSpaceState, new(24, 8)).ColliderRID;
        shapeExits.Clear(); sensor.BodyShapeExited += fail;
        Reject<AggregateException>(() => root.RemoveChild(layer)); sensor.BodyShapeExited -= fail;
        Check(layer.Parent is null && shapeExits.SetEquals([first, neighbor]) && !sensor.OverlapsBody(layer) &&
            NavigationServer.MapGetRegions(world.NavigationMap).Length == 0 &&
            Hits(world.DirectSpaceState, new(8, 8)) == 0 && Hits(world.DirectSpaceState, new(24, 8)) == 0,
            "Failed exit retires every generated body and detaches the authored layer.");
        root.AddChild(layer); tree.FlushDeferred(); tree.PhysicsFrame(1d / 60);
        Check(NavigationServer.MapGetRegions(world.NavigationMap).Length == 2, "Reentry restores navigation consumers after failed physics departure.");
        first = Hit(world.DirectSpaceState, new(8, 8)).ColliderRID;
        neighbor = Hit(world.DirectSpaceState, new(24, 8)).ColliderRID;
        shapeExits.Clear(); sensor.BodyShapeExited += fail;
        Reject<AggregateException>(layer.Dispose); sensor.BodyShapeExited -= fail;
        Check(layer.IsDisposed && shapeExits.SetEquals([first, neighbor]) && !sensor.HasOverlappingBodies() &&
            NavigationServer.MapGetRegions(world.NavigationMap).Length == 0 &&
            Hits(world.DirectSpaceState, new(8, 8)) == 0 && Hits(world.DirectSpaceState, new(24, 8)) == 0,
            "Failed disposal still retires every generated body and overlap snapshot.");
        Reject<ArgumentException>(() => PhysicsServer.BodyGetSpace(first));
        Reject<ArgumentException>(() => PhysicsServer.BodyGetSpace(neighbor));
        var deleted = false; var callbackLayer = new TileMapLayer { TileSet = assets.Set, PhysicsQuadrantSize = 1 };
        callbackLayer.SetCell(default, 3, default(Vector2i)); root.AddChild(callbackLayer);
        tree.FlushDeferred(); tree.PhysicsFrame(1d / 60);
        Action<RID, Entity?, int, int> delete = (_, owner, _, _) =>
        { if (owner == callbackLayer && !deleted) { deleted = true; callbackLayer.Dispose(); } };
        sensor.BodyShapeExited += delete;
        callbackLayer.SetCell(default, 3, default(Vector2i), TileSetAtlasSource.TransformFlipH);
        try { callbackLayer.UpdateInternals(); } finally { sensor.BodyShapeExited -= delete; }
        Check(deleted && callbackLayer.IsDisposed && !sensor.HasOverlappingBodies() &&
            NavigationServer.MapGetRegions(world.NavigationMap).Length == 0 && Hits(world.DirectSpaceState, new(8, 8)) == 0,
            "A departure handler can dispose the updating layer without republishing consumers or accessing retired node state.");
        Console.WriteLine($"Tile generated-body replacement/retirement and owner event failures passed on {backend}.");
    }
}
