using Electron2D;
using static TextureArrayTests;

internal static partial class DeferredSceneTests
{
    private static int _factories;
    private static Action? _creating;
    private static Action? _ready;
    private static Actor? _lastCreated;
    private sealed class Actor : Entity
    {
        internal Node? Target;
        internal Resource? Data;
        internal Resource? Shared;
        internal Resource?[]? Items;
        internal string? Label;
        internal int[] Values = [];
        internal int AfterTarget;
        internal Node? TargetDuringSet;
        internal Vector2 NotifiedPosition;
        internal static Actor Create() { _factories++; _creating?.Invoke(); return _lastCreated = new Actor(); }
        protected override Func<Node> CreateSceneInstanceFactory() => Create;
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat([
            new PropertyDescriptor<Actor, Node?>(nameof(Target), node => node.Target, (node, value) => node.Target = value, _ => null, stored: true),
            new PropertyDescriptor<Actor, Resource?>(nameof(Data), node => node.Data, (node, value) => node.Data = value, stored: true),
            new PropertyDescriptor<Actor, Resource?>(nameof(Shared), node => node.Shared, (node, value) => node.Shared = value, stored: true),
            new PropertyDescriptor<Actor, Resource?[]?>(nameof(Items), node => node.Items, (node, value) => node.Items = value, stored: true),
            new PropertyDescriptor<Actor, string?>(nameof(Label), node => node.Label, (node, value) => node.Label = value, stored: true),
            new PropertyDescriptor<Actor, int[]>(nameof(Values), node => node.Values, (node, value) => node.Values = value, stored: true),
            new PropertyDescriptor<Actor, int>(nameof(AfterTarget), node => node.AfterTarget,
                (node, value) => { node.EnsureMutable(); node.AfterTarget = value; node.TargetDuringSet = node.Target; }, stored: true)]);
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what == NotificationSceneInstantiated) NotifiedPosition = Position;
        }
        protected override void OnReady() { base.OnReady(); _ready?.Invoke(); }
    }

    internal static void RunManaged()
    {
        using var source = new Actor { Name = "Actor", Position = new(7, 9) };
        var inherited = new Node { Name = "Inherited" }; source.AddChild(inherited); inherited.Owner = source;
        using var template = new PackedScene(); template.Pack(source); template.ResourcePath = "res://deferred-not-present.e2dscene";
        using var outer = new Node { Name = "Outer" };
        var actor = (Actor)template.Instantiate(); actor.Name = "Pending"; outer.AddChild(actor); actor.Owner = outer;
        actor.Position = new(42, 53);
        var editorChanges = 0; actor.EditorStateChanged += _ => editorChanges++;
        actor.SetSceneInstanceLoadPlaceholder(true); actor.SetSceneInstanceLoadPlaceholder(true);
        Check(editorChanges == 0, "The deferred loading flag does not emit an unrelated editor-state notification.");
        outer.SetEditableInstance(actor, true);
        var local = new Node { Name = "Local" }; actor.AddChild(local); local.Owner = outer;
        var sibling = new Node { Name = "Sibling" }; outer.AddChild(sibling); sibling.Owner = outer; actor.Target = sibling;
        actor.AfterTarget = 5;
        using var packed = new PackedScene(); packed.Pack(outer);
        Check(packed.GetState().IsNodeInstancePlaceholder(1) && packed.GetState().GetNodeInstance(1) is null && packed.GetState().GetNodeType(1) == "", "Deferred metadata retains a path instead of a template resource edge.");
        _factories = 0; var restored = packed.Instantiate();
        using var tree = new SceneTree(restored);
        var pending = restored.GetNode<InstancePlaceholder>("Pending");
        Check(_factories == 0 && pending.GetInstancePath() == template.ResourcePath && pending.GetSceneInstanceLoadPlaceholder(), "Initial loading does not call deferred template factories.");
        Check(pending.GetChildCount() == 1 && pending.GetChild(0).Name == "Local", "Absent inherited records are skipped and ordinary local children remain on the placeholder.");
        Check(pending.GetStoredValue<Vector2>(nameof(Entity.Position)) == new Vector2(42, 53) && pending.GetStoredValue<string>(nameof(Actor.Target)) == "../Sibling", "Pending values preserve their exact values and node paths.");
        Check(pending.GetStoredPropertyNames().Contains(nameof(Entity.Position)) && !pending.GetStoredPropertyNames().Contains(nameof(Node.ProcessMode)), "Only fields unavailable on the Node role remain pending.");
        Check(pending.GetStoredValue<Resource?>(nameof(Actor.Data)) is null && pending.GetStoredValue<Resource?[]?>(nameof(Actor.Items)) is null && pending.GetStoredValue<string?>(nameof(Actor.Label)) is null, "Null pending values retain their declared roles.");
        Reject<InvalidCastException>(() => pending.GetStoredValue<string>(nameof(Actor.Data)));
        var created = (Actor)pending.CreateInstance(customScene: template)!;
        Check(_factories == 1 && created.Parent == restored && created.GetIndex() < pending.GetIndex() && !pending.IsQueuedForDeletion, "Default creation retains the placeholder and inserts a sibling above it.");
        Check(created.Position == new Vector2(42, 53) && created.NotifiedPosition == new Vector2(7, 9) && created.Target == restored.GetNode("Sibling"), "Notification precedes pending overlays and external node references resolve from the placeholder.");
        var order = pending.GetStoredPropertyNames();
        Check(Array.IndexOf(order, nameof(Actor.Target)) < Array.IndexOf(order, nameof(Actor.AfterTarget)) && created.AfterTarget == 5 && created.TargetDuringSet == restored.GetNode("Sibling"),
            "Pending application order includes node references before later scalar setters.");
        Check(created.GetNodeOrNull("Inherited") is not null && created.GetNodeOrNull("Local") is null, "Creation reconstructs the template and does not transfer ordinary placeholder children.");
        var replacement = pending.CreateInstance(replace: true, customScene: template)!;
        Check(replacement.Parent == restored && pending.Parent is null && pending.IsQueuedForDeletion && !pending.IsDisposed, "Replacement queues the detached placeholder after structural preparation.");
        tree.FlushDeferred(); Check(pending.IsDisposed, "The original tree retires the detached placeholder.");
        using var detached = new InstancePlaceholder(); Check(detached.CreateInstance(customScene: template) is null, "A detached placeholder cannot create a sibling.");
        using var standalone = new PackedScene(); standalone.Pack(detached);
        using var standaloneInstance = standalone.Instantiate();
        Check(standaloneInstance is InstancePlaceholder empty && empty.GetInstancePath() == "" && !empty.GetSceneInstanceLoadPlaceholder(), "An empty standalone placeholder preserves its ordinary Node role.");
        RunFailures(); RunFiles();
        Console.WriteLine("Deferred scenes: no eager construction, pending typed state, references, ordering and replacement passed.");
    }
}
