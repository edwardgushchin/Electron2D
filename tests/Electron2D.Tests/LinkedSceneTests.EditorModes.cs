using Electron2D;
using static TextureArrayTests;

internal static partial class LinkedSceneTests
{
    private static PropertyDescriptor Stored(Node node, string name) => node.GetPropertyList().Single(property => property.Name == name);

    private static void EditorModes()
    {
        Register();
        using var texture = NewTexture(Colors.Red);
        using var source = (Probe)MakeActor(texture);
        source.Target = source.GetNode("Visual");
        source.Data = texture;
        var tag = Stored(source, nameof(Probe.Tag));
        var target = Stored(source, nameof(Probe.Target));
        source.SetPropertyPinned(tag, true);
        Reject<ArgumentException>(() => source.SetPropertyPinned(Stored(source, nameof(Node.Name)), true));
        using var template = new PackedScene();
        template.Pack(source);
        Reject<NotSupportedException>(() => template.Instantiate(PackedSceneEditState.Main));
        Reject<NotSupportedException>(() => template.Instantiate(PackedSceneEditState.Instance));
        var previous = Engine.IsEditorHint;
        Engine.IsEditorHint = true;
        try
        {
            using (var main = (Probe)template.Instantiate(PackedSceneEditState.Main))
            using (var instance = (Probe)template.Instantiate(PackedSceneEditState.Instance))
            {
                Check(main.IsPropertyPinned(tag) && !instance.IsPropertyPinned(tag), "Main restores authored pins and Instance starts with its own layer.");
                Check(main.Data == texture && texture.GetLocalScene() == main &&
                      instance.Data != texture && instance.Data == ((Sprite)instance.GetNode("Visual")).Texture &&
                      instance.Data!.GetLocalScene() == instance, "Main borrows authored resources and Instance duplicates the local graph with aliases.");
                Check(main.PropertyCanRevert(tag) && !instance.PropertyCanRevert(tag),
                    "The main scene uses the property default while a nested instance starts at its source value.");
                instance.Tag = "Changed";
                instance.Target = null;
                Check(instance.PropertyCanRevert(tag) && instance.PropertyCanRevert(target), "Typed scalar and live-node overrides can revert.");
                instance.RevertProperty(tag);
                instance.RevertProperty(target);
                Check(instance.Tag == "Inner" && instance.Target == instance.GetNode("Visual"), "Revert restores the source value in the current hierarchy.");
                Reject<InvalidOperationException>(() => template.Instantiate(PackedSceneEditState.Main));
                Check(texture.GetLocalScene() == main && !texture.IsDisposed, "A competing main instance preserves the existing association.");
            }
            Check(texture.GetLocalScene() is null && !texture.IsDisposed, "Main disposal releases its association and leaves caller ownership intact.");
            FailNotification = true;
            try { Reject<ApplicationException>(() => template.Instantiate(PackedSceneEditState.Main)); }
            finally { FailNotification = false; Returned.Clear(); }
            Check(texture.GetLocalScene() is null && !texture.IsDisposed, "A failed main notification releases borrowed resources.");
            using (var retry = template.Instantiate(PackedSceneEditState.Main))
                Check(texture.GetLocalScene() == retry, "Main can retry after rollback.");

            using (var outer = new Node())
            using (var packedOuter = new PackedScene())
            {
                var actor = (Probe)template.Instantiate(PackedSceneEditState.Instance);
                outer.AddChild(actor);
                actor.Owner = outer;
                actor.Tag = "Outer";
                packedOuter.Pack(outer);
                using var editedOuter = packedOuter.Instantiate(PackedSceneEditState.Main);
                var editedActor = (Probe)editedOuter.GetChild(0);
                Check(editedActor.PropertyCanRevert(tag), "Main treats a linked child as an editable instance.");
                editedActor.RevertProperty(tag);
                Check(editedActor.Tag == "Inner" && editedActor.Data != texture, "Nested revert and resources use the nested source scope.");
            }

            using var derivative = (Probe)template.Instantiate(PackedSceneEditState.MainInherited);
            Check(!derivative.IsPropertyPinned(tag), "A new derived layer starts without the source's pin metadata.");
            using var tree = new SceneTree(derivative);
            tree.EditedSceneRoot = derivative;
            var changed = 0;
            derivative.EditorStateChanged += _ => changed++;
            derivative.SetPropertyPinned(tag, true);
            derivative.SetPropertyPinned(tag, true);
            Check(changed == 1, "Only a changed pin emits editor state.");
            using var saved = new PackedScene();
            saved.PackForSave(derivative);
            source.Tag = "Updated base";
            template.Pack(source);
            CheckPinnedFile(saved, tag);
        }
        finally { Engine.IsEditorHint = previous; }
        Console.WriteLine("Packed-scene editor modes: authored local ownership, nested typed revert, pins, rollback and file reload passed.");
    }

    private static void CheckPinnedFile(PackedScene saved, PropertyDescriptor tag)
    {
        var folder = At(System.IO.Path.GetTempPath(), "e2d-scene-editor-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var path = At(folder, "scene.e2dscene");
            ResourceSaver.Save(saved, path, SaverFlags.BundleResources);
            using var reloaded = ResourceLoader.Load<PackedScene>(path, ResourceLoader.CacheMode.IgnoreDeep);
            using var main = (Probe)reloaded.Instantiate(PackedSceneEditState.Main);
            using var gameplay = (Probe)reloaded.Instantiate();
            Check(main.IsPropertyPinned(tag) && main.Tag == "Inner" && main.PropertyCanRevert(tag) &&
                  !gameplay.IsPropertyPinned(tag) && gameplay.Tag == "Inner", "An equal-value pin remains an authored override after the base changes.");
            main.RevertProperty(tag);
            Check(main.Tag == "Updated base", "A reopened main scene reverts its pin value to the current base.");
        }
        finally { Directory.Delete(folder, true); }
    }
}
