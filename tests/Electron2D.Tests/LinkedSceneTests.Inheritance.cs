using Electron2D;
using static TextureArrayTests;

internal static partial class LinkedSceneTests
{
    private static void Inheritance()
    {
        Register();
        using var texture = NewTexture(Colors.Red);
        using var source = (Probe)MakeActor(texture);
        var receiver = new Probe { Name = "Receiver", Position = new(3, 4) };
        source.AddChild(receiver);
        receiver.Owner = source;
        source.Target = receiver;
        source.AddToGroup("Original", persistent: true);
        using var obsolete = new Node { Name = "Obsolete" };
        source.AddChild(obsolete);
        obsolete.Owner = source;
        using var inheritedConnection = _pulse.Subscribe(source, receiver, 3);
        using var template = new PackedScene();
        template.Pack(source);

        Reject<NotSupportedException>(() => template.Instantiate(PackedSceneEditState.MainInherited));
        Reject<ArgumentOutOfRangeException>(() => template.Instantiate((PackedSceneEditState)4));
        using var authored = BeginDerived(template);
        using var derived = new PackedScene();
        derived.Pack(authored);
        var state = derived.GetState();
        Check(state.GetBaseSceneState() == template.GetState() && state.GetNodeInstance(0) == template &&
              state.GetNodeCount() == 1 && state.GetNodePropertyCount(0) == 0 && state.GetNodeType(0).Length == 0,
            "An untouched derivative stores only a base link and root metadata.");
        using (var concrete = template.Instantiate())
        using (var copy = new PackedScene())
        {
            copy.Pack(concrete);
            Check(copy.GetState().GetBaseSceneState() is null, "Ordinary root instantiation still packs a concrete root.");
        }

        authored.Name = "Derived";
        authored.Tag = "Override";
        authored.AddToGroup("Authored", persistent: true);
        var renamed = (Probe)authored.GetNode("Receiver");
        renamed.Name = "Renamed";
        renamed.Position = new(13, 14);
        var local = new Probe { Name = "Local", Target = authored.GetNode("Visual") };
        renamed.AddChild(local);
        local.Owner = authored;
        var middle = new Node { Name = "Middle" };
        authored.AddChild(middle);
        middle.Owner = authored;
        authored.MoveChild(middle, 1);
        using var authoredConnection = _pulse.Subscribe(authored, local, 7);
        using (var blue = Image.CreateEmpty(1, 1, false, Image.Format.Rgba8))
        {
            blue.Fill(Colors.Blue);
            ((ImageTexture)((Sprite)authored.GetNode("Visual")).Texture!).Update(blue);
        }
        local.Data = ((Sprite)authored.GetNode("Visual")).Texture;

        derived.Pack(authored);
        Check(state.GetNodePropertyCount(0) == 1 && state.GetNodePropertyName(0, 0) == nameof(Probe.Tag) &&
              state.GetConnectionCount() == 1 && state.GetNodeGroups(0).SequenceEqual(new[] { "Authored" }),
            "Only the derived root's changed property, added group and authored binding are stored.");
        var folder = At(System.IO.Path.GetTempPath(), "electron2d-inherited-scene-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var basePath = At(folder, "base.e2dscene");
            var relativePath = At(folder, "derived.e2dscene");
            ResourceSaver.Save(template, basePath, SaverFlags.BundleResources);
            template.ResourcePath = basePath;
            ResourceSaver.Save(derived, relativePath, SaverFlags.RelativePaths);
            ResourceSaver.Save(derived, At(folder, "bundle.e2dscene"), SaverFlags.BundleResources | SaverFlags.SaveBigEndian | SaverFlags.Compress);
            Check(ResourceLoader.GetDependencies(relativePath).Length == 1 && ResourceLoader.GetClassesUsed(relativePath).Contains(typeof(Probe)),
                "Archive metadata finds the exact base dependency and compiled type.");

            source.RemoveChild(obsolete);
            source.RemoveFromGroup("Original");
            source.AddToGroup("BaseUpdate", persistent: true);
            source.Position = new(21, 22);
            receiver.Position = new(31, 32);
            receiver.Scale = new(2, 3);
            var inserted = new Node { Name = "BaseAdded" };
            source.AddChild(inserted);
            inserted.Owner = source;
            template.Pack(source);
            ResourceSaver.Save(template, basePath, SaverFlags.BundleResources);

            using (var runtime = (Probe)derived.Instantiate())
            using (var recaptured = new PackedScene())
            {
                Check(runtime.SceneFilePath.Length == 0, "An unsaved derived root does not report the base's file path.");
                recaptured.PackForSave(runtime);
                Check(recaptured.GetState().GetBaseSceneState() == state.GetBaseSceneState() &&
                      recaptured.GetState().GetConnectionCount() == 1, "Runtime save retains the immediate base and only outer bindings.");
                CheckDerived(runtime, updated: true);
            }

            using (var nextRoot = BeginDerived(derived))
            using (var next = new PackedScene())
            {
                nextRoot.Rotation = 0.75f;
                next.Pack(nextRoot);
                Check(next.GetState().GetBaseSceneState() == state && next.GetState().GetNodeCount() == 1,
                    "A third layer stores its immediate base and its own changes.");
                using var runtime = (Probe)next.Instantiate();
                Check(runtime.Rotation == 0.75f && ((Probe)runtime.GetNode("Renamed/Local")).Target == runtime.GetNode("Visual"),
                    "Multiple inheritance layers resolve the final live node references.");
            }

            Child("inherited-relative", folder);
            var payload = ResourceArchive.ReadPayload(relativePath, out _, out var version);
            Check(version == 10, "The inherited root edge uses the version-ten format.");
            var downgraded = At(folder, "downgraded.e2dscene");
            Envelope(downgraded, payload, 9);
            Reject<InvalidDataException>(() => ResourceLoader.Load<PackedScene>(downgraded, ResourceLoader.CacheMode.IgnoreDeep));

            File.Delete(basePath);
            Reject<FileNotFoundException>(() => ResourceLoader.Load<PackedScene>(relativePath, ResourceLoader.CacheMode.IgnoreDeep));
            ResourceLoader.SetAbortOnMissingResources(false);
            try { Reject<InvalidDataException>(() => ResourceLoader.Load<PackedScene>(relativePath, ResourceLoader.CacheMode.IgnoreDeep)); }
            finally { ResourceLoader.SetAbortOnMissingResources(true); }
            Child("inherited-bundled", folder);
        }
        finally { Directory.Delete(folder, true); }

        Reject<InvalidOperationException>(() => template.PackForSave(authored));
        Check(template.CanInstantiate(), "Rejected save preparation preserves the base template.");
        Reject<InvalidOperationException>(() => template.Pack(authored));
        Check(!template.CanInstantiate(), "Rejected cyclic Pack follows the documented clear-on-failure policy.");
        Console.WriteLine("Inherited scenes: sparse overlays, immediate base state, template updates, local resources, typed bindings and versioned fresh-process files passed.");
    }

    private static Probe BeginDerived(PackedScene template)
    {
        var previous = Engine.IsEditorHint;
        Engine.IsEditorHint = true;
        try { return (Probe)template.Instantiate(PackedSceneEditState.MainInherited); }
        finally { Engine.IsEditorHint = previous; }
    }

    private static void ReadInheritedFile(string folder, bool bundled)
    {
        Register();
        var path = At(folder, bundled ? "bundle.e2dscene" : "derived.e2dscene");
        using var scene = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep);
        using var state = scene.GetState();
        using var baseState = state.GetBaseSceneState();
        Check(baseState is not null && state.GetNodeInstance(0)?.GetState() == baseState, "The file exposes the immediate base state.");
        using var runtime = (Probe)scene.Instantiate();
        Check(runtime.SceneFilePath.EndsWith(bundled ? "bundle.e2dscene" : "derived.e2dscene", StringComparison.Ordinal),
            "A restored derived root carries its own file path.");
        CheckDerived(runtime, updated: !bundled);
    }

    private static void CheckDerived(Probe root, bool updated)
    {
        var receiver = (Probe)root.GetNode("Renamed");
        var local = (Probe)receiver.GetNode("Local");
        var visual = (Sprite)root.GetNode("Visual");
        var alias = (Sprite)root.GetNode("Alias");
        Check(root.Name == "Derived" && root.Tag == "Override" && root.Position == (updated ? new(21, 22) : Vector2.Zero) &&
              receiver.Position == new Vector2(13, 14) && receiver.Scale == (updated ? new(2, 3) : Vector2.One),
            "Explicit typed overrides survive while untouched base properties follow the current template.");
        Check(root.IsInGroup("Authored") && root.IsInGroup(updated ? "BaseUpdate" : "Original") &&
              root.HasNode(updated ? "BaseAdded" : "Obsolete") && !root.HasNode(updated ? "Obsolete" : "BaseAdded"),
            "Unchanged groups and nodes follow the template; locally authored groups survive.");
        Check(root.GetChild(1).Name == "Middle", "Insertion keeps its position without recording shifted inherited siblings.");
        Check(local.Owner == root && local.Target == visual && root.Target == receiver,
            "Inherited references follow identity through rename and local references resolve after overlays.");
        Check(visual.Texture == alias.Texture && visual.Texture == local.Data && visual.Texture!.GetLocalScene() == root &&
              visual.Texture.GetImage() is { } pixels && PixelValue(pixels, Colors.Blue),
            "In-place changes to scene-local texture content preserve aliases and ownership.");
        using var tree = new SceneTree(root);
        root.Emit();
        Check(receiver.Calls == 3 && local.Calls == 7, "Inherited and authored bindings each deliver once after activation.");
    }
}
