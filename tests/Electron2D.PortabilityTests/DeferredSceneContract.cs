using Electron2D;

internal static class DeferredSceneContract
{
    internal static void Run(string mode, string folder)
    {
        Directory.CreateDirectory(folder);
        var actorPath = System.IO.Path.Combine(folder, "actor.e2dscene");
        var parentPath = System.IO.Path.Combine(folder, "parent.e2dscene");
        if (mode == "produce")
        {
            using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8); image.Fill(Colors.Blue);
            using var texture = ImageTexture.CreateFromImage(image); texture.ResourceLocalToScene = true;
            using var source = new Sprite { Name = "Actor", Texture = texture, Position = new(7, 9) };
            using var actor = new PackedScene(); actor.Pack(source); ResourceSaver.Save(actor, actorPath, SaverFlags.BundleResources);
            actor.ResourcePath = actorPath;
            using var root = new Node { Name = "Level" };
            foreach (var name in new[] { "One", "Two" })
            {
                var node = (Sprite)actor.Instantiate(); node.Name = name; root.AddChild(node); node.Owner = root;
                node.Position = new(31, 43); node.SetSceneInstanceLoadPlaceholder(true);
            }
            using var parent = new PackedScene(); parent.Pack(root);
            ResourceSaver.Save(parent, parentPath, SaverFlags.BundleResources | SaverFlags.SaveBigEndian | SaverFlags.Compress);
        }
        else if (mode == "read")
        {
            using var parent = ResourceLoader.Load<PackedScene>(parentPath, ResourceLoader.CacheMode.IgnoreDeep);
            var root = parent.Instantiate(); using var tree = new SceneTree(root);
            var one = root.GetNode<InstancePlaceholder>("One"); var two = root.GetNode<InstancePlaceholder>("Two");
            Check(!ResourceLoader.HasCached(actorPath) && one.GetInstancePath() == actorPath && one.GetSceneInstanceLoadPlaceholder(), "Initial load is deferred.");
            Check(one.GetStoredPropertyNames().Contains(nameof(Entity.Position)) && one.GetStoredValue<Vector2>(nameof(Entity.Position)) == new Vector2(31, 43), "Typed pending scalar metadata.");
            var pendingTexture = one.GetStoredValue<ImageTexture>(nameof(Sprite.Texture));
            parent.Dispose();
            using (var pixels = pendingTexture.GetImage()!) Check(pixels.GetPixel(0, 0) == Colors.Blue, "Pending resource graph survives parent disposal.");
            var first = (Sprite)one.CreateInstance(replace: true)!; var second = (Sprite)two.CreateInstance()!;
            Check(first.Position == new Vector2(31, 43) && first.Texture != second.Texture && first.Texture!.GetLocalScene() == first, "Materialized local resources and overrides.");
            first.Dispose();
            using (var pixels = second.Texture!.GetImage()!) Check(pixels.GetPixel(0, 0) == Colors.Blue, "A second actor retains its shared template and pixels.");
            Check(ResourceLoader.HasCached(actorPath), "A live materialized actor retains the default template.");
            second.Dispose(); Check(!ResourceLoader.HasCached(actorPath), "Final actor retirement releases the engine-owned template.");
            tree.FlushDeferred(); Check(one.IsDisposed, "Replacement retirement uses the original tree.");
            Console.WriteLine("Public deferred scene contract passed: fresh file, typed values, local pixels and shared template lifetime.");
        }
        else throw new ArgumentException("Unknown deferred scene consumer mode.");
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
