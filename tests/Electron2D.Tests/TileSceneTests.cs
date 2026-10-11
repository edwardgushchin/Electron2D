using System.Diagnostics;
using Electron2D;

internal static partial class TileSceneTests
{
    internal static void Run()
    {
        Metadata(); Flow(); Files(); Failures(); Console.WriteLine("Scene tiles: identities, shared borrowed templates, validation, deferred lifecycle, transforms, copies and fresh graphs passed.");
    }
    private static PackedScene Pack(Node node) { var scene = new PackedScene(); try { scene.Pack(node); return scene; } catch { scene.Dispose(); throw; } }
    private static void Metadata()
    {
        using var root = new Entity { Position = new(3, 4) }; using var scene = Pack(root); using var source = new TileSetScenesCollectionSource();
        Check(source.GetTilesCount() == 1 && source.HasTile(default) && source.GetTileID(0) == default && source.GetAlternativeTilesCount(new(9, 9)) == 0 && source.GetNextSceneTileID() == 1, "Single-slot empty collection");
        var changed = 0; source.Changed += _ => changed++; Check(source.CreateSceneTile(scene) == 1 && changed == 1 && source.GetNextSceneTileID() == 2, "Coherent creation and automatic ID");
        source.CreateSceneTile(scene, 0); source.CreateSceneTile(null, 20); Check(source.GetSceneTileID(0) == 0 && source.GetSceneTileID(2) == 20 && source.GetSceneTilesCount() == 3, "Sorted explicit/null definitions");
        Check(source.HasAlternativeTile(default, 1 | TileSetAtlasSource.TransformFlipH) && !source.HasAlternativeTile(new(1, 0), 1) && source.GetSceneTileScene(1 | TileSetAtlasSource.TransformTranspose) == scene && !source.HasSceneTileID(1 | TileSetAtlasSource.TransformFlipH), "Alternative flags versus exact authored IDs");
        source.RemoveSceneTile(1); Check(source.GetNextSceneTileID() == 2 && !scene.IsDisposed, "Monotonic cursor and borrowed removal"); source.SetSceneTileID(20, 2); Check(source.GetNextSceneTileID() == 3 && source.GetSceneTileScene(2) is null, "Rename/cursor");
        Reject<ArgumentException>(() => source.CreateSceneTile(scene, 0)); Reject<ArgumentOutOfRangeException>(() => source.CreateSceneTile(scene, -2)); Reject<KeyNotFoundException>(() => source.RemoveSceneTile(999)); Reject<ArgumentOutOfRangeException>(() => source.GetTileID(1));
        using var invalidRoot = new Node(); using var invalid = Pack(invalidRoot); using var empty = new PackedScene();
        Reject<ArgumentException>(() => source.CreateSceneTile(invalid, 7)); Reject<ArgumentException>(() => source.SetSceneTileScene(0, empty)); Check(!source.HasSceneTileID(7) && source.GetSceneTileScene(0) == scene, "Rejected root before mutation");
        using var shallow = (TileSetScenesCollectionSource)source.Duplicate(); using var deep = (TileSetScenesCollectionSource)source.Duplicate(true);
        Check(shallow.GetSceneTileScene(0) == scene && deep.GetSceneTileScene(0) != scene && deep.GetNextSceneTileID() == 3, "Copied template graph and cursor");
        source.CreateSceneTile(scene, 4); var before = changed; scene.Pack(root); Check(changed == before + 1, "Shared template observes one notification"); scene.Dispose(); Check(source.GetSceneTileScene(0) is null && source.GetSceneTileScene(4) is null, "Borrowed disposal clears all definitions");
        source.CreateSceneTile(null, 4097); Check(source.HasSceneTileID(4097) && source.GetSceneTileScene(4097) is null, "Authored high identities retain direct access despite transform-bit overlap");
        ((PropertyDescriptor<TileSetScenesCollectionSource, int>)source.GetPropertyList().Single(p => p.Name == "NextSceneID")).SetValue(source, 1073741823); source.CreateSceneTile(null, 1073741823); Check(source.GetNextSceneTileID() == 1, "Automatic cursor wraps its nonzero identity range");
        source.CreateSceneTile(null, int.MaxValue); Check(source.GetSceneTileScene(int.MaxValue) is null, "Complete nonnegative authored identity range");
    }
    private static void Flow()
    {
        using var authored = new Entity { Position = new(3, 4), Scale = new(2, 1) }; using var scene = Pack(authored); using var source = new TileSetScenesCollectionSource(); source.CreateSceneTile(scene, 1);
        using var controlRoot = new Control { Position = new(2, 3), Size = new(4, 4) }; using var controlScene = Pack(controlRoot); source.CreateSceneTile(controlScene, 2);
        using var set = new TileSet(); set.AddSource(source, 7); var layer = new TileMapLayer { TileSet = set }; layer.SetCell(new(-1, 2), 7, Vector2i.Zero, 1 | TileSetAtlasSource.TransformTranspose | TileSetAtlasSource.TransformFlipH); layer.SetCell(new(1, 0), 7, Vector2i.Zero, 2 | TileSetAtlasSource.TransformFlipV);
        using var viewport = new SubViewport(); viewport.AddChild(layer); layer.UpdateInternals(); Check(layer.GetChildCount() == 0, "Detached authoring does not instantiate");
        using var tree = new SceneTree(viewport); Check(layer.GetChildCount() == 0, "Tree entry defers creation"); tree.FlushDeferred(); var placed = layer.GetChildren(); var entity = (Entity)placed.Single(n => n is Entity); var control = (Control)placed.Single(n => n is Control);
        var expected = new Transform(new(0, 1), new(-1, 0), layer.MapToLocal(new(-1, 2))) * authored.Transform; Check(entity.Transform.IsEqualApprox(expected) && control.Position == layer.MapToLocal(new(1, 0)) + controlRoot.Position && entity.Owner is null && layer.GetCellTileData(new(-1, 2)) is null, "Entity basis and Control position/ownership");
        layer.FixInvalidTiles(); Check(layer.GetUsedCells().Length == 2, "Scene cells survive validity repair"); layer.Hide(); layer.CollisionEnabled = false; layer.NavigationEnabled = false; layer.NotifyRuntimeTileDataUpdate(); tree.FlushDeferred(); Check(layer.GetChildren().Contains(entity), $"Visibility and atlas-only policies preserve scene identity: oldDisposed={entity.IsDisposed}, oldTemplate={ReferenceEquals(entity.SpawnSceneIdentity, scene)}, children={layer.GetChildCount()}"); Check(!entity.IsVisibleInTree, "Hidden scene inherits visibility"); layer.Show();
        layer.Enabled = false; tree.FlushDeferred(); Check(entity.IsDisposed && layer.GetChildCount() == 0, "Disable retires all scenes"); layer.Enabled = true; tree.FlushDeferred(); entity = (Entity)layer.GetChildren().Single(n => n is Entity);
        using var runtimePack = Pack(layer); Check(runtimePack.GetState().GetNodeCount() == 1, "Runtime instances are not serialized as authored children");
        layer.EraseCell(new(-1, 2)); layer.UpdateInternals(); Check(entity.IsQueuedForDeletion && !entity.IsDisposed, "Explicit updates preserve deferred deletion"); tree.FlushDeferred(); Check(entity.IsDisposed && layer.GetChildCount() == 1, "Erasure completes at safe point");
        viewport.RemoveChild(layer); tree.FlushDeferred(); Check(layer.GetChildCount() == 0, "Exit retires detached children"); viewport.AddChild(layer); tree.FlushDeferred(); Check(layer.GetChildCount() == 1, "Reentry rebuilds scenes");
        var bytes = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 128; i++) layer.UpdateInternals(); Check(GC.GetAllocatedBytesForCurrentThread() == bytes, "Unchanged scene updates allocate zero");
    }
    private static void Files()
    {
        using var node = new Entity { Position = new(3, 4) }; using var scene = Pack(node); using var source = new TileSetScenesCollectionSource(); source.CreateSceneTile(scene, 1); source.CreateSceneTile(scene, 9); source.CreateSceneTile(scene, int.MaxValue);
        using var set = new TileSet(); set.AddSource(source, 4); using var layer = new TileMapLayer { TileSet = set }; layer.SetCell(new(2, -3), 4, Vector2i.Zero, 9); using var packed = Pack(layer);
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "scene-tiles-" + Guid.NewGuid() + ".e2dscene");
        try
        {
            ResourceSaver.Save(packed, path); var start = new ProcessStartInfo("dotnet") { RedirectStandardOutput = true, RedirectStandardError = true }; start.ArgumentList.Add(typeof(TileSceneTests).Assembly.Location); start.Environment.Remove("ELECTRON2D_TEST_TILE_SCENES"); start.Environment["ELECTRON2D_TEST_TILE_SCENES_FILE"] = path;
            using var child = Process.Start(start)!; var output = child.StandardOutput.ReadToEndAsync(); var errors = child.StandardError.ReadToEndAsync(); if (!child.WaitForExit(30000)) { child.Kill(true); throw new TimeoutException("Fresh scene tile file."); }
            Check(child.ExitCode == 0 && output.GetAwaiter().GetResult().Contains("Fresh scene tile graph passed"), errors.GetAwaiter().GetResult());
        }
        finally { System.IO.File.Delete(path); }
    }
    internal static void RunFile(string path)
    {
        using var packed = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.Ignore); using var layer = (TileMapLayer)packed.Instantiate(); var source = (TileSetScenesCollectionSource)layer.TileSet!.GetSource(4); Check(source.GetSceneTileScene(1) == source.GetSceneTileScene(9) && source.GetSceneTileScene(int.MaxValue) == source.GetSceneTileScene(1), "Shared portable scene identity and full authored ID range"); using var root = new SubViewport(); root.AddChild(layer); using var tree = new SceneTree(root); tree.FlushDeferred(); Check(((Entity)layer.GetChildren().Single()).Position == layer.MapToLocal(new(2, -3)) + new Vector2(3, 4), "Fresh-process placed scene"); Console.WriteLine("Fresh scene tile graph passed.");
    }
    private static void Failures()
    {
        using var authored = new Entity(); using var scene = Pack(authored); using var source = new TileSetScenesCollectionSource(); source.CreateSceneTile(scene, 1); using var set = new TileSet(); set.AddSource(source, 0); var layer = new TileMapLayer { TileSet = set }; layer.SetCell(default, 0, Vector2i.Zero, 1); using var root = new SubViewport(); root.AddChild(layer); using var tree = new SceneTree(root); tree.FlushDeferred(); var previous = layer.GetChildren().Single();
        using var bad = new Node(); scene.Pack(bad); Reject<InvalidOperationException>(layer.UpdateInternals); Check(previous.Parent == layer && !previous.IsQueuedForDeletion && layer.GetChildCount() == 1, "Invalid changed template preserves previous scene"); scene.Pack(authored); tree.FlushDeferred(); Check(previous.IsDisposed && layer.GetChildCount() == 1, "Repaired template retries");
        previous = layer.GetChildren().Single(); using var failureRoot = new FailingRoot(); using var failure = Pack(failureRoot); source.SetSceneTileScene(1, failure); FailingRoot.Fail = true;
        try { Reject<AggregateException>(layer.UpdateInternals); Check(layer.GetChildCount() == 1 && previous.Parent == layer && !previous.IsQueuedForDeletion, "Ready failure cleans candidate and preserves previous instance"); }
        finally { FailingRoot.Fail = false; source.SetSceneTileScene(1, scene); tree.FlushDeferred(); }
        using var recursiveSource = new TileSetScenesCollectionSource(); recursiveSource.CreateSceneTile(null, 1); using var recursiveSet = new TileSet(); recursiveSet.AddSource(recursiveSource, 0); using var recursiveAuthored = new TileMapLayer { TileSet = recursiveSet }; recursiveAuthored.SetCell(default, 0, Vector2i.Zero, 1); using var recursive = Pack(recursiveAuthored); recursiveSource.SetSceneTileScene(1, recursive);
        var top = new TileMapLayer { Name = "Recursive", TileSet = recursiveSet }; top.SetCell(default, 0, Vector2i.Zero, 1); root.AddChild(top); tree.FlushDeferred(); var nested = (TileMapLayer)top.GetChildren().Single(); Reject<InvalidOperationException>(nested.UpdateInternals); Check(nested.GetChildCount() == 0, "Recursive ancestor template rejects before spawning another generation");
    }
    private sealed class FailingRoot : Entity
    {
        internal static bool Fail;
        protected override void OnReady() { if (Fail) throw new InvalidOperationException("Fixture ready failure."); }
        protected override Func<Node> CreateSceneInstanceFactory() => Create;
        private static Node Create() => new FailingRoot();
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
