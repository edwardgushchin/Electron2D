using Electron2D;
using static TextureArrayTests;

internal static partial class LinkedSceneTests
{
    internal static void RunManaged()
    {
        using var source = new Entity { Name = "Actor" };
        var visual = new Sprite { Name = "Visual", Position = new(7, 9) }; source.AddChild(visual); visual.Owner = source;
        using var actor = new PackedScene(); actor.Pack(source);
        using var level = new Entity { Name = "Level" };
        var instance = (Entity)actor.Instantiate(); instance.Name = "One"; level.AddChild(instance); instance.Owner = level;
        using var packed = new PackedScene(); packed.Pack(level);
        Check(ReferenceEquals(packed.GetState().GetNodeInstance(1), actor), "An actual subscene retains its template identity.");
        using var restored = packed.Instantiate();
        var nested = restored.GetNode("One");
        Check(nested.GetNode("Visual") is Sprite sprite && sprite.Position == new Vector2(7, 9) && sprite.Owner == nested, "Unselected template children reconstruct with their own authoring owner.");
        visual.Position = new(15, 18); actor.Pack(source);
        using var updated = packed.Instantiate();
        Check(((Sprite)updated.GetNode("One/Visual")).Position == new Vector2(15, 18), "Unselected linked contents follow current template data.");
        level.SetEditableInstance(instance, true);
        ((Sprite)instance.GetNode("Visual")).Position = new(22, 23);
        packed.Pack(level);
        using var overridden = packed.Instantiate();
        Check(((Sprite)overridden.GetNode("One/Visual")).Position == new Vector2(22, 23), "Editable instance state applies to reused template nodes.");
        Behavior();
        Files();
        Console.WriteLine("Linked scenes: template identity, implicit children, template updates and typed overrides passed.");
    }
}
