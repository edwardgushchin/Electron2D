using Electron2D;

internal static partial class TileMapLayerTests
{
    private sealed class Assets : IDisposable
    {
        internal readonly Image Image = Image.CreateEmpty(64, 32, false, Image.Format.Rgba8);
        internal readonly ImageTexture Texture;
        internal readonly TileSetAtlasSource Atlas;
        internal readonly TileSet Set = new();
        internal Assets()
        {
            Image.Fill(Colors.White); Texture = ImageTexture.CreateFromImage(Image);
            Atlas = new() { Texture = Texture }; Set.AddPhysicsLayer(); Set.AddSource(Atlas, 3);
            Atlas.CreateTile(default); var data = Atlas.GetTileData(default, 0); data.AddCollisionPolygon(0);
            data.SetCollisionPolygonPoints(0, 0, [new(-8, -8), new(8, -8), new(8, 8), new(-8, 8)]);
        }
        public void Dispose() { Set.Dispose(); Atlas.Dispose(); Texture.Dispose(); Image.Dispose(); }
    }
    internal static void Run(PhysicsServer.Backend backend)
    {
        Resources(); Geometry(backend); Owners(backend); OneWay(backend); Lifecycle(backend); RuntimeData(backend);
        Console.WriteLine($"Tile layer passed: {backend}, resources/storage, merged quadrants/holes, owner projections, lifecycle and runtime updates.");
    }
    private static void Resources()
    {
        using var assets = new Assets(); var set = assets.Set; var atlas = assets.Atlas; var data = atlas.GetTileData(default, 0);
        Check(set.TileSize == new Vector2i(16, 16) && atlas.GetAtlasGridSize() == new Vector2i(4, 2), "Square and atlas defaults");
        Reject<ArgumentException>(() => atlas.CreateTile(default)); Reject<ArgumentOutOfRangeException>(() => set.TileSize = new(0, 16));
        Reject<InvalidOperationException>(() => data.Dispose());
        var points = data.GetCollisionPolygonPoints(0, 0); points[0] = new(100, 100); Check(data.GetCollisionPolygonPoints(0, 0)[0] == new Vector2(-8, -8), "Polygon reads do not alias");
        Reject<ArgumentException>(() => data.SetCollisionPolygonPoints(0, 0, [new(0, 0), new(1, 1)]));
        Reject<ArgumentException>(() => data.SetCollisionPolygonPoints(0, 0, [new(float.NaN, 0), new(1, 1), new(0, 1)]));
        var alt = atlas.CreateAlternativeTile(default); var other = atlas.GetTileData(default, alt); other.FlipH = true; other.Transpose = true; other.TextureOrigin = new(2, 1);
        other.AddCollisionPolygon(0); other.SetCollisionPolygonPoints(0, 0, [new(-8, 0), new(8, 0), new(0, 8)]);
        other.SetConstantLinearVelocity(0, new(12, 0)); other.SetCollisionPolygonOneWay(0, 0, true); other.SetCollisionPolygonOneWayMargin(0, 0, 2);
        set.AddPhysicsLayer(0); Check(data.GetCollisionPolygonsCount(0) == 0 && data.GetCollisionPolygonsCount(1) == 1, "Insert remaps tile physics data");
        set.MovePhysicsLayer(1, 0); Check(data.GetCollisionPolygonsCount(0) == 1, "Move remaps tile physics data"); set.RemovePhysicsLayer(1);
        using var material = new PhysicsMaterial { Friction = .8f, Rough = true, Bounce = .3f, Absorbent = true }; set.SetPhysicsLayerPhysicsMaterial(0, material);
        using var shallow = (TileSet)set.Duplicate(); var copiedAtlas = (TileSetAtlasSource)shallow.GetSource(3);
        Check(set.HasSource(3) && copiedAtlas != atlas && copiedAtlas.Texture == assets.Texture && shallow.GetPhysicsLayerPhysicsMaterial(0) == material, "Shallow copy duplicates source ownership but borrows texture and material");
        copiedAtlas.GetTileData(default, 0).SetCollisionPolygonOneWay(0, 0, true); Check(!data.IsCollisionPolygonOneWay(0, 0), "Owned data copies do not alias");
        using var copied = new TileSet(); copied.CopyFromResource(set);
        Check(set.HasSource(3) && copied.GetSource(3) != atlas, "CopyFromResource cannot steal a source from its original owner");
        using var target = new TileSet(); target.AddPhysicsLayer(); target.AddSource(copiedAtlas, 7);
        Check(!shallow.HasSource(3) && target.GetSource(7) == copiedAtlas, "Source transfer updates both sets");
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "e2d-tiles-" + Guid.NewGuid() + ".e2dres");
        try
        {
            ResourceSaver.Save(set, path); using var loaded = ResourceLoader.Load<TileSet>(path, ResourceLoader.CacheMode.Ignore);
            var savedAtlas = (TileSetAtlasSource)loaded.GetSource(3); var savedData = savedAtlas.GetTileData(default, alt);
            Check(savedData.FlipH && savedData.Transpose && savedData.TextureOrigin == new Vector2i(2, 1) && savedData.IsCollisionPolygonOneWay(0, 0) && savedData.GetCollisionPolygonOneWayMargin(0, 0) == 2 && savedData.GetConstantLinearVelocity(0) == new Vector2(12, 0), "Portable tile data survives full typed resource graph roundtrip");
            Check(loaded.GetPhysicsLayerPhysicsMaterial(0) is { Rough: true, Absorbent: true } && savedAtlas.Texture!.GetSize() == assets.Texture.GetSize(), "Texture and material graph roundtrip");
        }
        finally { System.IO.File.Delete(path); }
        atlas.RemoveAlternativeTile(default, alt); Check(other.IsDisposed, "Removed alternative invalidates borrowed data");
        Reject<ObjectDisposedException>(() => other.GetCollisionPolygonsCount(0)); copiedAtlas.Dispose(); Check(!target.HasSource(7), "Disposed source detaches set");
    }
    private static void Geometry(PhysicsServer.Backend backend)
    {
        using var assets = new Assets(); using var world = new World(backend); using var root = new SubViewport { World = world };
        var layer = new TileMapLayer { TileSet = assets.Set }; root.AddChild(layer); using var tree = new SceneTree(root);
        layer.SetCell(new(0, 0), 3, default(Vector2i)); layer.SetCell(new(1, 0), 3, default(Vector2i)); layer.SetCell(new(-1, -1), 3, default(Vector2i)); layer.UpdateInternals();
        var direct = world.DirectSpaceState;
        var first = Hit(direct, new(8, 8)); var neighbor = Hit(direct, new(24, 8)); var negative = Hit(direct, new(-8, -8));
        Check(first.ColliderRID == neighbor.ColliderRID && PhysicsServer.BodyGetShapeCount(first.ColliderRID) == 1, "Adjacent tiles merge into one convex fixture in a quadrant");
        Check(layer.GetCoordsForBodyRID(negative.ColliderRID) == new Vector2i(-1, -1) && layer.GetCoordsForBodyRID(first.ColliderRID) == default, "Negative quadrants floor correctly");
        Check(first.ColliderObject == layer && first.Collider is null && first.ColliderID == layer.InstanceID, "Generated raw collider projects the tile layer owner");
        Check(layer.GetChildCount() == 0 && layer.LocalToMap(new(-.1f, -.1f)) == new Vector2i(-1, -1), "No hidden nodes and negative cell conversion");
        layer.EraseCell(new(1, 0)); layer.UpdateInternals(); Check(!layer.HasBodyRID(first.ColliderRID) && Hits(direct, new(24, 8)) == 0, "Dirty quadrant retires old identity and erased geometry");
        Check(layer.HasBodyRID(negative.ColliderRID), "Editing one quadrant preserves another quadrant identity");
        layer.PhysicsQuadrantSize = 1; layer.UpdateInternals(); Check(layer.GetCoordsForBodyRID(Hit(direct, new(-8, -8)).ColliderRID) == new Vector2i(-1, -1), "Single-cell quadrants identify exact cells");
        layer.Clear(); layer.PhysicsQuadrantSize = 16;
        for (var y = 0; y < 3; y++) for (var x = 0; x < 3; x++) if (x != 1 || y != 1) layer.SetCell(new(x, y), 3, default(Vector2i));
        layer.UpdateInternals(); Check(Hits(direct, new(24, 24)) == 0 && Hits(direct, new(8, 24)) == 1, "Merged polygons preserve the ring hole");
        var bytes = layer.TileMapData; using var clone = new TileMapLayer { TileSet = assets.Set, TileMapData = bytes };
        Check(clone.GetUsedCells().Length == 8 && clone.GetUsedRect() == new Rect2i(0, 0, 3, 3), "Portable cell layout roundtrip");
        Reject<ArgumentException>(() => clone.TileMapData = [0, 0, 1]); Check(clone.GetUsedCells().Length == 8, "Malformed tile arrays cannot erase valid cells");
        clone.SetCell(new(65536, -65537), 3, default(Vector2i)); using var wrapped = new TileMapLayer { TileMapData = clone.TileMapData };
        Check(wrapped.GetCellSourceID(new(0, -1)) == 3, "Serialized cell coordinates wrap to signed 16 bits");
        using var packed = new PackedScene(); packed.Pack(clone); using var instance = (TileMapLayer)packed.Instantiate(); Check(instance.TileSet == assets.Set && instance.GetUsedCells().Length == 9, "Packed tile layer preserves authored resource and cells"); VerifyFreshScene(clone);
        layer.Clear(); layer.SetCell(default, 3, default(Vector2i)); layer.UpdateInternals();
        var data = assets.Atlas.GetTileData(default, 0); data.SetConstantLinearVelocity(0, new(20, 0)); data.SetConstantAngularVelocity(0, .5f); assets.Set.SetPhysicsLayerCollisionLayer(0, 8); assets.Set.SetPhysicsLayerCollisionMask(0, 4); assets.Set.SetPhysicsLayerCollisionPriority(0, 2);
        using var material = new PhysicsMaterial { Friction = .7f, Rough = true, Bounce = .2f, Absorbent = true }; assets.Set.SetPhysicsLayerPhysicsMaterial(0, material); layer.UpdateInternals();
        var body = Hit(direct, new(8, 8)).ColliderRID;
        Check(PhysicsServer.BodyGetDirectState(body)!.GetVelocityAtLocalPosition(new(2, 0)).IsEqualApprox(new Vector2(20, 1)), "Tile surface velocities reach backend");
        Check(PhysicsServer.BodyGetCollisionPriority(body) == 2 && PhysicsServer.BodyGetFriction(body) == -.7f && PhysicsServer.BodyGetBounce(body) == -.2f, "Layer filters, recovery priority and signed material policies reach backend");
        for (var i = 0; i < 60; i++) tree.PhysicsFrame(1d / 60);
        Check(Hit(direct, new(8, 8)).ColliderRID == body, "Surface velocity does not move static tile geometry");
        var alt = assets.Atlas.CreateAlternativeTile(default); var alternative = assets.Atlas.GetTileData(default, alt);
        alternative.FlipH = true; alternative.AddCollisionPolygon(0);
        alternative.SetCollisionPolygonPoints(0, 0, [new(-8, -8), new(0, -8), new(-8, 8)]);
        layer.SetCell(default, 3, default(Vector2i), alt); layer.UpdateInternals();
        Check(Hits(direct, new(2, 4)) == 1 && Hits(direct, new(14, 4)) == 0, "TileData image reflection leaves authored collision unchanged");
        layer.SetCell(default, 3, default(Vector2i), alt | TileSetAtlasSource.TransformFlipH); layer.UpdateInternals();
        Check(Hits(direct, new(14, 4)) == 1 && Hits(direct, new(2, 4)) == 0, "Cell horizontal reflection transforms actual collision geometry");
        layer.SetCell(default, 3, default(Vector2i), alt | TileSetAtlasSource.TransformTranspose | TileSetAtlasSource.TransformFlipV); layer.UpdateInternals();
        Check(Hits(direct, new(4, 14)) == 1 && Hits(direct, new(14, 4)) == 0, "Cell transpose then reflection transforms actual collision geometry");
        layer.Scale = new(-2, .5f); layer.Rotation = .4f; layer.Skew = .1f; layer.ForceUpdateTransform(); layer.UpdateInternals();
        Check(Hits(direct, layer.GlobalTransform * new Vector2(4, 14)) == 1 && Hits(direct, layer.GlobalTransform * new Vector2(14, 4)) == 0, "Reflected scaled skewed layer keeps physical placement aligned with its canvas");
    }
    private static void Owners(PhysicsServer.Backend backend)
    {
        using var assets = new Assets(); using var world = new World(backend); using var root = new SubViewport { World = world };
        var layer = new TileMapLayer { Name = "Tiles", TileSet = assets.Set, PhysicsQuadrantSize = 1 };
        layer.SetCell(default, 3, default(Vector2i)); layer.SetCell(new(1, 0), 3, default(Vector2i)); root.AddChild(layer);
        using var circle = new CircleShape { Radius = 10 }; using var sensorShape = new RectangleShape { Size = new(64, 32) };
        var sensor = new Area { Name = "Sensor", Position = new(16, 8) }; sensor.AddChild(new CollisionShape { Shape = sensorShape }); root.AddChild(sensor);
        var mover = new RigidBody { Name = "Mover", Position = new(8, -2), GravityScale = 0, ContactMonitor = true, MaxContactsReported = 8 };
        mover.AddChild(new CollisionShape { Shape = circle }); root.AddChild(mover);
        var entered = 0; var shapes = 0; var contacts = 0; var raw = new HashSet<RID>();
        sensor.BodyEntered += body => { if (body == layer) entered++; };
        sensor.BodyShapeEntered += (rid, body, _, _) => { if (body == layer) { shapes++; raw.Add(rid); } };
        mover.BodyEntered += body => { if (body == layer) contacts++; };
        using var tree = new SceneTree(root); layer.UpdateInternals(); tree.PhysicsFrame(1d / 60);
        Check(entered == 1 && shapes >= 2 && raw.Count == 2 && raw.All(layer.HasBodyRID), "Area deduplicates layer while preserving multiple physical body identities");
        using var directBody = PhysicsServer.BodyGetDirectState(mover.GetRID())!;
        Check(Enumerable.Range(0, directBody.GetContactCount()).Any(i => directBody.GetContactColliderObject<TileMapLayer>(i) == layer && layer.HasBodyRID(directBody.GetContactCollider(i))), "Direct body state retains tile owner and physical RID");
        Check(sensor.OverlapsBody(layer) && sensor.GetOverlappingBodies().Contains(layer) && contacts == 1 && mover.GetCollidingBodies().Contains(layer), "Area and rigid-body owner projections include tile layers");
        using var ray = PhysicsRayQueryParameters.Create(new(8, -40), new(8, 40)); ray.Exclude = [mover.GetRID()];
        var hit = world.DirectSpaceState.IntersectRay(ray)!.Value; Check(hit.ColliderObject == layer && layer.HasBodyRID(hit.ColliderRID), "Ray query reports generated tile owner");
        using var query = new PhysicsShapeQueryParameters { Shape = circle, Transform = new(0, new(8, 8)), Exclude = [mover.GetRID()] };
        Check(world.DirectSpaceState.IntersectShape(query).All(h => h.ColliderObject == layer) && world.DirectSpaceState.GetRestInfo(query)!.Value.ColliderObject == layer, "Shape and rest queries report tile owners");
        using var parameters = new PhysicsTestMotionParameters { From = new(0, new(8, -40)), Motion = new(0, 60) }; using var result = new PhysicsTestMotionResult();
        Check(PhysicsServer.BodyTestMotion(mover.GetRID(), parameters, result) && result.GetCollider() == layer && layer.HasBodyRID(result.GetColliderRID()), "Motion result reports tile owner and RID");
        parameters.ExcludeObjects = [layer.InstanceID]; Check(!PhysicsServer.BodyTestMotion(mover.GetRID(), parameters, result), "Object exclusion suppresses every body generated by that layer");
        var rayNode = new RayCast { Name = "Ray", Position = new(24, -40), TargetPosition = new(0, 80), Enabled = false }; root.AddChild(rayNode); rayNode.ForceRaycastUpdate();
        Check(rayNode.GetCollider() == layer && layer.HasBodyRID(rayNode.GetColliderRID()), "RayCast owner projection");
        var castNode = new ShapeCast { Name = "Cast", Shape = circle, Position = new(24, -40), TargetPosition = new(0, 80), Enabled = false }; root.AddChild(castNode); castNode.AddException(mover); castNode.ForceShapecastUpdate();
        Check(castNode.GetCollider(0) == layer && layer.HasBodyRID(castNode.GetColliderRID(0)), "ShapeCast owner projection");
        layer.CollisionEnabled = false; tree.FlushDeferred(); tree.PhysicsFrame(1d / 60); Check(!sensor.OverlapsBody(layer), "Removing tile bodies updates area membership");
    }
    private static void OneWay(PhysicsServer.Backend backend)
    {
        using var assets = new Assets(); using var world = new World(backend); using var root = new SubViewport { World = world };
        var data = assets.Atlas.GetTileData(default, 0); data.SetCollisionPolygonOneWay(0, 0, true); data.SetCollisionPolygonOneWayMargin(0, 0, 2);
        var layer = new TileMapLayer { TileSet = assets.Set }; layer.SetCell(default, 3, default(Vector2i)); root.AddChild(layer);
        using var shape = new CircleShape { Radius = 4 }; var body = new CharacterBody { Name = "Character", Position = new(8, -30) }; body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body);
        using var tree = new SceneTree(root); layer.UpdateInternals();
        using var parameters = new PhysicsTestMotionParameters { From = new(0, new(8, -30)), Motion = new(0, 60) }; using var result = new PhysicsTestMotionResult();
        Check(PhysicsServer.BodyTestMotion(body.GetRID(), parameters, result) && result.GetCollider() == layer, "One-way tile catches downward motion");
        parameters.From = new(0, new(8, 40)); parameters.Motion = new(0, -80);
        Check(!PhysicsServer.BodyTestMotion(body.GetRID(), parameters, result), "One-way tile allows upward motion");
        var prior = layer.GetCellSourceID(default);
        Reject<InvalidOperationException>(() => Task.Run(() => layer.EraseCell(default)).GetAwaiter().GetResult()); Check(layer.GetCellSourceID(default) == prior, "Rejected foreign-thread edits preserve cells");
        layer.Rotation = .3f; layer.ForceUpdateTransform(); var rid = Hit(world.DirectSpaceState, layer.GlobalTransform * new Vector2(8, 8)).ColliderRID;
        for (var i = 0; i < 20; i++) { layer.Rotation += .01f; layer.ForceUpdateTransform(); layer.UpdateInternals(); }
        Check(layer.HasBodyRID(rid), "Rotation changes preserve generated identities without rounding-triggered rebuilds");
    }
    private static void Lifecycle(PhysicsServer.Backend backend)
    {
        using var assets = new Assets(); using var world = new World(backend); using var nextWorld = new World(backend); using var root = new SubViewport { World = world };
        var layer = new TileMapLayer { TileSet = assets.Set }; layer.SetCell(default, 3, default(Vector2i)); root.AddChild(layer); using var tree = new SceneTree(root); tree.FlushDeferred();
        var body = Hit(world.DirectSpaceState, new(8, 8)).ColliderRID; layer.Hide(); tree.FlushDeferred(); Check(Hits(world.DirectSpaceState, new(8, 8)) == 1, "Hidden tile layer keeps collision");
        layer.Show(); tree.FlushDeferred(); body = Hit(world.DirectSpaceState, new(8, 8)).ColliderRID;
        layer.Position = new(40, 20); layer.ForceUpdateTransform(); Check(Hit(world.DirectSpaceState, new(48, 28)).ColliderRID == body, "Translation preserves generated identity");
        layer.Scale = new(2, 1); layer.ForceUpdateTransform(); tree.FlushDeferred(); Check(Hits(world.DirectSpaceState, new(64, 28)) == 1 && Hits(world.DirectSpaceState, new(80, 28)) == 0, "Layer scale is baked into collision geometry");
        root.World = nextWorld; tree.FlushDeferred(); Check(Hits(world.DirectSpaceState, new(64, 28)) == 0 && Hits(nextWorld.DirectSpaceState, new(64, 28)) == 1, "World reassignment moves tile physics");
        layer.Enabled = false; tree.FlushDeferred(); Check(Hits(nextWorld.DirectSpaceState, new(64, 28)) == 0, "Disabled layer removes collision");
        layer.Enabled = true; tree.FlushDeferred(); layer.UseKinematicBodies = true; tree.FlushDeferred(); body = Hit(nextWorld.DirectSpaceState, new(64, 28)).ColliderRID;
        Check(PhysicsServer.BodyGetMode(body) == PhysicsServer.BodyMode.Kinematic, "Kinematic mode uses actual kinematic bodies");
        layer.Position = new(60, 20); layer.ForceUpdateTransform(); tree.PhysicsFrame(1d / 60); Check(Hit(nextWorld.DirectSpaceState, new(84, 28)).ColliderRID == body, "Kinematic layer follows scheduled transform");
        layer.UseKinematicBodies = false; tree.FlushDeferred();
        for (var i = 0; i < 90; i++) tree.PhysicsFrame(1d / 60);
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 120; i++) { layer.UpdateInternals(); tree.PhysicsFrame(1d / 60); }
        var bytes = GC.GetAllocatedBytesForCurrentThread() - before; Console.WriteLine($"Tile unchanged {backend}: {bytes} B / 120 physics frames"); Check(bytes == 0, "Unchanged tile physics does not allocate");
        root.RemoveChild(layer); Check(Hits(nextWorld.DirectSpaceState, new(84, 28)) == 0, "Exit releases generated bodies"); root.AddChild(layer); tree.FlushDeferred(); Check(Hits(nextWorld.DirectSpaceState, new(84, 28)) == 1, "Reentry rebuilds bodies");
        assets.Set.Dispose(); tree.FlushDeferred(); Check(Hits(nextWorld.DirectSpaceState, new(84, 28)) == 0 && layer.TileSet is null, "Disposed tile set detaches consumers");
    }
    private sealed class RuntimeLayer : TileMapLayer
    {
        internal TileData? LastData;
        internal bool Cleanup;
        internal bool Fail;
        internal bool Reenter;
        protected override bool UseTileDataRuntimeUpdate(Vector2i coords) => coords.X == 0;
        protected override void TileDataRuntimeUpdate(Vector2i coords, TileData tileData) { LastData = tileData; if (Reenter) Reject<InvalidOperationException>(UpdateInternals); if (Fail) throw new InvalidOperationException("Callback failure"); tileData.SetConstantLinearVelocity(0, new(50, 0)); }
        protected override void UpdateCells(Vector2i[] coords, bool forcedCleanup) => Cleanup = forcedCleanup;
    }
    private static void RuntimeData(PhysicsServer.Backend backend)
    {
        using var assets = new Assets(); using var world = new World(backend); using var root = new SubViewport { World = world };
        var layer = new RuntimeLayer { TileSet = assets.Set }; layer.SetCell(default, 3, default(Vector2i)); layer.SetCell(new(1, 0), 3, default(Vector2i)); root.AddChild(layer);
        using var tree = new SceneTree(root); layer.UpdateInternals(); var a = Hit(world.DirectSpaceState, new(8, 8)).ColliderRID; var b = Hit(world.DirectSpaceState, new(24, 8)).ColliderRID;
        Check(a != b && PhysicsServer.BodyGetDirectState(a)!.GetVelocityAtLocalPosition(default) == new Vector2(50, 0) && PhysicsServer.BodyGetDirectState(b)!.GetVelocityAtLocalPosition(default) == default && assets.Atlas.GetTileData(default, 0).GetConstantLinearVelocity(0) == default, "Runtime data affects one cell and velocity policy separates bodies");
        layer.Fail = true; layer.NotifyRuntimeTileDataUpdate(); Reject<InvalidOperationException>(layer.UpdateInternals);
        Check(layer.LastData!.IsDisposed && layer.HasBodyRID(a) && layer.HasBodyRID(b), "Failed runtime callback releases its copy and preserves committed bodies");
        layer.Fail = false; layer.Reenter = true; layer.UpdateInternals();
        var old = layer.LastData; layer.NotifyRuntimeTileDataUpdate(); tree.FlushDeferred(); Check(old!.IsDisposed && layer.LastData != old, "Rebuilding invalidates previous runtime data");
        layer.Hide(); tree.FlushDeferred(); Check(layer.Cleanup && Hits(world.DirectSpaceState, new(8, 8)) == 1, "Hidden layer cleans runtime data and preserves authored collision");
    }
    private static PhysicsPointResult Hit(PhysicsDirectSpaceState state, Vector2 point)
    { using var query = new PhysicsPointQueryParameters { Position = point }; return state.IntersectPoint(query).Single(); }
    private static int Hits(PhysicsDirectSpaceState state, Vector2 point)
    { using var query = new PhysicsPointQueryParameters { Position = point }; return state.IntersectPoint(query).Length; }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
