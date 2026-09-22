using Electron2D;
using EngineTimer = Electron2D.Timer;

internal static class SceneHierarchyTests
{
    internal static void Run()
    {
        Check(typeof(SceneNode).BaseType == typeof(ElectronObject) && typeof(CanvasItem).IsAbstract &&
            typeof(CanvasItem).BaseType == typeof(SceneNode) && typeof(Node).BaseType == typeof(CanvasItem) &&
            typeof(Sprite).BaseType == typeof(Node) && typeof(EngineTimer).BaseType == typeof(SceneNode) &&
            typeof(Viewport).BaseType == typeof(SceneNode) && typeof(Window).BaseType == typeof(Viewport), "Declared inheritance.");
        foreach (var name in new[] { "Position", "Transform", "Visible", "Material", "TopLevel", "Modulate" })
            Check(typeof(SceneNode).GetProperty(name) is null && typeof(EngineTimer).GetProperty(name) is null, $"Neutral API has no {name}.");
        Check(typeof(SceneNode).GetMethod("DrawRect") is null && typeof(CanvasItem).GetProperty("Position") is null,
            "Drawing and concrete placement remain separate.");
        Check(typeof(SceneNode).GetMethod("AddChild")!.GetParameters()[0].ParameterType == typeof(SceneNode) &&
            typeof(SceneNode).GetProperty("Parent")!.PropertyType == typeof(SceneNode) &&
            typeof(Texture).GetMethod("Draw")!.GetParameters()[0].ParameterType == typeof(CanvasItem), "API argument roles.");
        MixedTree();
        PackedHierarchy();
        CallbackFailures();
        Console.WriteLine("Scene hierarchy checks passed.");
    }

    private static void MixedTree()
    {
        var root = new SceneNode { Name = "root" };
        var spatial = new Node { Name = "spatial", Position = new(30, 20), ZIndex = 5, Modulate = Colors.Red };
        var neutral = new SceneNode { Name = "neutral" };
        var direct = new Node { Name = "direct", Position = new(2, 3), ZIndex = 1 };
        var independent = new Node { Name = "independent", Position = new(7, 8), ZIndex = 2 };
        var custom = new CustomCanvas { Name = "custom" };
        root.AddChild(spatial); spatial.AddChild(direct); spatial.AddChild(neutral); neutral.AddChild(independent);
        root.AddChild(custom);
        var underCustom = new Node { Position = new(1, 2) };
        custom.AddChild(underCustom); custom.Move(new(4, 5));
        using var tree = new SceneTree(root);
        Check(direct.GlobalPosition == new Vector2(32, 23) && independent.GlobalPosition == new Vector2(7, 8), "Neutral parent breaks transform chain.");
        Check(underCustom.GlobalPosition == new Vector2(5, 7), "Spatial child inherits abstract canvas transform.");
        Check(direct.EffectiveZIndex == 6 && independent.EffectiveZIndex == 2, "Neutral parent breaks relative Z.");
        var independentNotifications = 0;
        independent.TransformChanged += _ => independentNotifications++;
        spatial.Position += Vector2.One;
        Check(independentNotifications == 0, "Neutral parent stops transform notifications.");
        spatial.Hide();
        Check(!direct.IsVisibleInTree && independent.IsVisibleInTree && custom.IsVisibleInTree, "Neutral parent stops inherited visibility.");
        spatial.Show();
        var local = direct.Transform;
        direct.TopLevel = true;
        Check(direct.Transform == local && direct.GlobalTransform == local && direct.EffectiveZIndex == 1,
            "TopLevel keeps local state and detaches transform/Z chain.");
        direct.TopLevel = false;
        var before = direct.GlobalTransform;
        SceneNode neutralView = direct;
        neutralView.Reparent(neutral);
        Check(direct.GlobalTransform.IsEqualApprox(before), "Reparent through neutral API preserves spatial state.");
        neutralView.Reparent(custom);
        Check(direct.GlobalTransform.IsEqualApprox(before), "Reparent under another canvas model preserves spatial state.");
        Reject<ArgumentException>(() => direct.GetRelativeTransformToParent(root));
        using var singular = new Node { Scale = Vector2.Zero };
        Reject<InvalidOperationException>(() => direct.Reparent(singular));
        Check(direct.Parent == custom && direct.GlobalTransform.IsEqualApprox(before), "Singular destination rejects before mutation.");
        var timer = new EngineTimer { Name = "timer", OneShot = true };
        neutral.AddChild(timer);
        var timeouts = 0; timer.Timeout += _ => timeouts++;
        timer.Start(0.1); tree.ProcessFrame(0.1);
        Check(timeouts == 1 && timer.GetViewport() is null, "Neutral timer retains lifecycle and scheduling.");
        timer.CreateTween().TweenCallback(() => timeouts++);
        tree.ProcessFrame(0.1);
        Check(timeouts == 2 && root.GetNode("spatial/neutral/timer") == timer, "Neutral tween binding and path lookup.");
        timer.QueueFree(); tree.FlushDeferred();
        Check(timer.IsDisposed && neutral.ChildCount == 1, "Neutral deletion and ownership.");
    }

    private static void PackedHierarchy()
    {
        using var root = new SceneNode { Name = "mixed" };
        var group = new SceneNode { Name = "group" };
        var node = new Node { Name = "node", Position = new(4, 9), Modulate = Colors.Cyan };
        var timer = new EngineTimer { Name = "timer", WaitTime = 2 };
        root.AddChild(group); group.AddChild(node); root.AddChild(timer);
        group.Owner = root; node.Owner = root; timer.Owner = root;
        using var packed = new PackedScene(); packed.Pack(root);
        using var copy = packed.Instantiate();
        Check(copy.GetType() == typeof(SceneNode) && copy.GetChild(0).GetType() == typeof(SceneNode), "Packed neutral factories.");
        Check(copy.GetNode<Node>("group/node").Position == node.Position && copy.GetNode<Node>("group/node").Modulate == node.Modulate &&
            copy.GetNode<EngineTimer>("timer").WaitTime == 2, "Stored state is contributed by the proper base.");
        Check(copy.GetPropertyList().All(p => p.Name != "Visible" && p.Name != "Position"), "Neutral descriptors exclude canvas state.");
    }

    private static void CallbackFailures()
    {
        using var root = new Node();
        var first = new Node { Name = "first" }; var second = new Node { Name = "second" };
        root.AddChild(first); root.AddChild(second);
        var reached = 0;
        Action<CanvasItem> fail = _ => throw new InvalidOperationException("expected");
        first.TransformChanged += fail;
        second.TransformChanged += _ => reached++;
        Reject<AggregateException>(() => root.Position = Vector2.One);
        Check(reached == 1 && second.GlobalPosition == Vector2.One, "Failed transform callbacks do not skip siblings.");
        first.TransformChanged -= fail;
    }

    private sealed class CustomCanvas : CanvasItem
    {
        private Transform _local = Transform.Identity;
        public override Transform GetTransform() { ThrowIfDisposed(); return _local; }
        internal void Move(Vector2 position) { EnsureMutable(); _local.Origin = position; NotifyLocalTransformChanged(); }
    }

    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
