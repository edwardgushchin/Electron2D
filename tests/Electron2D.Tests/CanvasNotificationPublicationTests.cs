using Electron2D;

internal static class CanvasNotificationPublicationTests
{
    private sealed class Writer : Entity
    {
        internal Action? Write;
        protected override void OnPhysicsProcess(double delta) => Write!();
    }
    private sealed class Reentry : Entity
    {
        internal Action? Publish;
        internal bool Armed;
        public override Transform GetTransform()
        {
            if (Armed && Tree?.IsProcessingWorkerStage == false) { Armed = false; Publish!(); }
            return base.GetTransform();
        }
    }
    internal static void Run(PhysicsServer.Backend backend)
    {
        using var world = new World(backend); using var root = new SubViewport { World = world };
        using var shape = new CircleShape { Radius = 1 };
        using var image = Image.CreateEmpty(16, 16, false, Image.Format.Rgba8); image.Fill(Colors.White); using var texture = ImageTexture.CreateFromImage(image);
        using var atlas = new TileSetAtlasSource { Texture = texture }; using var set = new TileSet(); set.AddPhysicsLayer(); set.SetPhysicsLayerCollisionLayer(0, 4); set.AddSource(atlas, 3);
        atlas.CreateTile(default); var data = atlas.GetTileData(default, 0); data.AddCollisionPolygon(0); data.SetCollisionPolygonPoints(0, 0, [new(-8, -8), new(8, -8), new(8, 8), new(-8, 8)]);
        var layer = new CanvasLayer(); var writer = new Writer { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread };
        var probe = new Reentry(); var body = new RigidBody { Position = new(100, 100), GravityScale = 0, CanSleep = false }; body.AddChild(new CollisionShape { Shape = shape });
        var tiles = new TileMapLayer { TileSet = set, PhysicsQuadrantSize = 1 }; tiles.SetCell(default, 3, Vector2i.Zero);
        writer.AddChild(probe); writer.AddChild(body); writer.AddChild(tiles); layer.AddChild(writer); root.AddChild(layer);
        using var tree = new SceneTree(root); tiles.UpdateInternals(); probe.ServerState = new(null!, false);
        var phase = 0; var cell = 0; var rid = body.GetRID(); var failure = new ApplicationException("Expected owner publication failure");
        using var query = new PhysicsPointQueryParameters { CollisionMask = 4 }; var hits = new PhysicsPointResult[1];
        probe.Publish = () =>
        {
            if (phase == 2) throw failure;
            if (phase == 0) { writer.RemoveChild(body); writer.AddChild(body); }
            else body.TopLevel = !body.TopLevel;
            tiles.Clear(); cell += 4; tiles.SetCell(new(cell, 0), 3, Vector2i.Zero); tiles.UpdateInternals();
        };
        writer.Write = () =>
        {
            probe.Armed = true; probe.PhysicsInterpolationMode = probe.PhysicsInterpolationMode == PhysicsInterpolationMode.Off ? PhysicsInterpolationMode.On : PhysicsInterpolationMode.Off;
            body.PropagateNotification(CanvasItem.NotificationExitCanvas); tiles.PropagateNotification(CanvasItem.NotificationExitCanvas);
        };
        for (phase = 0; phase < 3; phase++)
        {
            query.Position = new(16 * cell + 8, 8); query.CanvasInstanceID = layer.InstanceID;
            Check(world.DirectSpaceState.IntersectPoint(query, hits) == 1, "The old tile body exists before the request"); var oldTile = hits[0].ColliderRID;
            var oldTree = body.TreeAttachmentVersion; var oldCanvas = body.CanvasAttachmentVersion; var tick = PhysicsServer.SpaceGetTick(world.Space);
            Exception? error = null; try { tree.PhysicsFrame(.01); } catch (Exception caught) { error = caught; }
            var desired = phase == 2 ? 0 : layer.InstanceID;
            Check(PhysicsServer.SpaceGetTick(world.Space) == tick + 1 && PhysicsServer.BodyGetCanvasInstanceID(rid) == desired,
                "Retired tree/canvas attachments cannot receive an older queued identity");
            if (phase == 0) Check(body.TreeAttachmentVersion != oldTree, "The callback actually reattached the same body");
            if (phase == 1) Check(body.TreeAttachmentVersion == oldTree && body.CanvasAttachmentVersion != oldCanvas, "TopLevel actually rebound only the canvas attachment");
            query.Position = new(16 * cell + 8, 8); query.CanvasInstanceID = desired;
            Check(world.DirectSpaceState.IntersectPoint(query, hits) == 1 && PhysicsServer.BodyGetCanvasInstanceID(hits[0].ColliderRID) == desired,
                "Generated bodies created after the request keep their new canvas identity");
            if (phase < 2) Check(hits[0].ColliderRID != oldTile && error is null, "The callback actually replaced the target generated RID");
            else Check(error is AggregateException aggregate && aggregate.Flatten().InnerExceptions.Contains(failure), "Owner publication preserves original errors after later identities and numerical stepping finish");
        }
        Console.WriteLine($"Canvas publication lifecycle {backend}: tree/canvas attachment epochs, generated RID replacement, owner reentry/failure and selected stepping passed.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
