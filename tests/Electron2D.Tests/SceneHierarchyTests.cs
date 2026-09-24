using Electron2D;
using EngineTimer = Electron2D.Timer;

internal static class SceneHierarchyTests
{
    internal static void Run()
    {
        Check(typeof(Node).BaseType == typeof(ElectronObject) && typeof(CanvasItem).IsAbstract &&
            typeof(CanvasItem).BaseType == typeof(Node) && typeof(Entity).BaseType == typeof(CanvasItem) &&
            typeof(Sprite).BaseType == typeof(Entity) && typeof(EngineTimer).BaseType == typeof(Node) &&
            typeof(Viewport).BaseType == typeof(Node) && typeof(Window).BaseType == typeof(Viewport), "Declared inheritance.");
        foreach (var name in new[] { "Position", "Transform", "Visible", "Material", "TopLevel", "Modulate", "ShowBehindParent", "YSortEnabled" })
            Check(typeof(Node).GetProperty(name) is null && typeof(EngineTimer).GetProperty(name) is null, $"Neutral API has no {name}.");
        Check(typeof(Node).GetMethod("DrawRect") is null && typeof(CanvasItem).GetProperty("Position") is null,
            "Drawing and concrete placement remain separate.");
        Check(typeof(Node).GetMethod("AddChild")!.GetParameters()[0].ParameterType == typeof(Node) &&
            typeof(Node).GetProperty("Parent")!.PropertyType == typeof(Node) &&
            typeof(Texture).GetMethod("Draw")!.GetParameters()[0].ParameterType == typeof(CanvasItem), "API argument roles.");
        MixedTree();
        CanvasOrderValues();
        CanvasAppearanceValues();
        PackedHierarchy();
        CallbackFailures();
        Console.WriteLine("Scene hierarchy checks passed.");
    }

    private static void MixedTree()
    {
        var root = new Node { Name = "root" };
        var spatial = new Entity { Name = "spatial", Position = new(30, 20), ZIndex = 5, Modulate = Colors.Red };
        var neutral = new Node { Name = "neutral" };
        var direct = new Entity { Name = "direct", Position = new(2, 3), ZIndex = 1 };
        var independent = new Entity { Name = "independent", Position = new(7, 8), ZIndex = 2 };
        var custom = new CustomCanvas { Name = "custom" };
        root.AddChild(spatial); spatial.AddChild(direct); spatial.AddChild(neutral); neutral.AddChild(independent);
        root.AddChild(custom);
        var underCustom = new Entity { Position = new(1, 2) };
        custom.AddChild(underCustom); custom.Move(new(4, 5));
        using var tree = new SceneTree(root);
        Check(direct.GlobalPosition == new Vector2(32, 23) && independent.GlobalPosition == new Vector2(7, 8), "Neutral parent breaks transform chain.");
        Check(underCustom.GlobalPosition == new Vector2(5, 7), "Spatial child inherits abstract canvas transform.");
        Reject<InvalidOperationException>(() => Task.Run(() => _ = custom.GetTransform()).GetAwaiter().GetResult());
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
        Check(!direct.ShowBehindParent && !direct.YSortEnabled, "Canvas ordering defaults.");
        Reject<InvalidOperationException>(() => Task.Run(() => direct.YSortEnabled = true).GetAwaiter().GetResult());
        Reject<InvalidOperationException>(() => Task.Run(() => direct.ShowBehindParent = true).GetAwaiter().GetResult());
        var before = direct.GlobalTransform;
        Node neutralView = direct;
        neutralView.Reparent(neutral);
        Check(direct.GlobalTransform.IsEqualApprox(before), "Reparent through neutral API preserves spatial state.");
        neutralView.Reparent(custom);
        Check(direct.GlobalTransform.IsEqualApprox(before), "Reparent under another canvas model preserves spatial state.");
        Reject<ArgumentException>(() => direct.GetRelativeTransformToParent(root));
        using var singular = new Entity { Transform = new Transform(Vector2.Zero, Vector2.Down, Vector2.Zero) };
        Reject<InvalidOperationException>(() => direct.Reparent(singular));
        Check(direct.Parent == custom && direct.GlobalTransform.IsEqualApprox(before), "Singular destination rejects before mutation.");
        var timer = new EngineTimer { Name = "timer", OneShot = true };
        neutral.AddChild(timer);
        var timeouts = 0; timer.Timeout += _ => timeouts++;
        timer.Start(0.1); tree.ProcessFrame(0.11);
        Check(timeouts == 1 && timer.GetViewport() is null, "Neutral timer retains lifecycle and scheduling.");
        timer.CreateTween().TweenCallback(() => timeouts++);
        tree.ProcessFrame(0.1);
        Check(timeouts == 2 && root.GetNode("spatial/neutral/timer") == timer, "Neutral tween binding and path lookup.");
        timer.QueueFree(); tree.FlushDeferred();
        Check(timer.IsDisposed && neutral.ChildCount == 1, "Neutral deletion and ownership.");
    }

    private static void CanvasOrderValues()
    {
        using var root = new Entity { ZIndex = 4096 };
        var first = new Entity { Name = "First", ZIndex = 1 };
        var second = new Entity { Name = "Second", ZIndex = -4096, ZAsRelative = false };
        root.AddChild(first);
        root.AddChild(second);
        using var tree = new SceneTree(root);

        Check(root.ZAsRelative && first.ZAsRelative && !second.ZAsRelative &&
            first.EffectiveZIndex == 4096 && second.EffectiveZIndex == -4096,
            "Canvas Z defaults, inherited clamping, and absolute child Z are stable.");
        first.ZAsRelative = false;
        Check(first.EffectiveZIndex == 1, "Absolute Z stops direct-parent accumulation.");
        first.ZAsRelative = true;
        Reject<ArgumentOutOfRangeException>(() => first.ZIndex = 4097);
        Reject<ArgumentOutOfRangeException>(() => first.ZIndex = -4097);
        Check(first.ZIndex == 1, "Invalid Z assignments preserve the previous value.");
        tree.EditedSceneRoot = root;
        var warningRefreshes = 0;
        tree.NodeConfigurationWarningChanged += (_, node) => { if (node == first) warningRefreshes++; };
        first.ZIndex = first.ZIndex;
        Check(warningRefreshes == 1, "Even an equal valid Z assignment requests warning refresh.");
        first.ZAsRelative = first.ZAsRelative;
        Check(warningRefreshes == 1, "An equal Z-relative assignment does not request warning refresh.");

        var orderChanges = 0;
        root.ChildOrderChanged += _ => orderChanges++;
        first.MoveToFront();
        Check(root.GetChild(1) == first && orderChanges == 1,
            "MoveToFront places the child last and notifies its parent once.");
        first.MoveToFront();
        Check(orderChanges == 1, "Moving an already-last child is a no-op.");
        Action<Node> fail = _ => throw new ApplicationException("order");
        root.ChildOrderChanged += fail;
        Reject<AggregateException>(second.MoveToFront);
        root.ChildOrderChanged -= fail;
        Check(root.GetChild(1) == second, "Failed order callbacks retain the committed sibling order.");

        Reject<InvalidOperationException>(() => Task.Run(() => _ = first.ZIndex).GetAwaiter().GetResult());
        Reject<InvalidOperationException>(() => Task.Run(() => _ = first.ZAsRelative).GetAwaiter().GetResult());
        Reject<InvalidOperationException>(() => Task.Run(() => _ = first.ShowBehindParent).GetAwaiter().GetResult());
        Reject<InvalidOperationException>(() => Task.Run(() => _ = first.YSortEnabled).GetAwaiter().GetResult());
        Reject<InvalidOperationException>(() => Task.Run(root.MoveToFront).GetAwaiter().GetResult());
        root.MoveToFront();
        Check(root.Parent is null, "MoveToFront on the scene root is a safe no-op on its owner thread.");

        using var stored = new Entity { Name = "StoredOrder", ZIndex = -3, ZAsRelative = false };
        using var packed = new PackedScene();
        packed.Pack(stored);
        using var copy = (Entity)packed.Instantiate();
        Check(copy.ZIndex == -3 && !copy.ZAsRelative,
            "PackedScene restores local Z and relative-order policy independently.");
    }

    private static void CanvasAppearanceValues()
    {
        using var material = new CanvasItemMaterial();
        using var ownMaterial = new CanvasItemMaterial();
        using var root = new Entity { Name = "AppearanceRoot" };
        var child = new Entity { Name = "AppearanceChild", UseParentMaterial = true };
        root.AddChild(child);
        using (var tree = new SceneTree(root))
        {
            Check(root.Modulate == Colors.White && root.SelfModulate == Colors.White &&
                root.Material is null && !root.UseParentMaterial,
                "Canvas appearance defaults are white modulation, no material and no inheritance.");
            var lists = 0;
            root.PropertyListChanged += _ => lists++;
            root.Material = material;
            root.Material = material;
            Check(lists == 2 && ReferenceEquals(child.CanvasMaterial, material),
                "Every material assignment refreshes tooling and direct children inherit the live material.");
            child.Material = ownMaterial;
            Check(ReferenceEquals(child.CanvasMaterial, material),
                "UseParentMaterial overrides an assigned local material without discarding it.");
            child.UseParentMaterial = false;
            Check(ReferenceEquals(child.CanvasMaterial, ownMaterial),
                "Disabling parent material restores the child's borrowed material.");
            child.UseParentMaterial = true;
            child.TopLevel = true;
            Check(child.CanvasMaterial is null, "TopLevel ends material inheritance.");
            child.TopLevel = false;
            Check(ReferenceEquals(child.CanvasMaterial, material), "Clearing TopLevel restores direct-parent inheritance.");

            Action<ElectronObject> fail = _ => throw new ApplicationException("material list");
            root.PropertyListChanged += fail;
            Reject<ApplicationException>(() => root.Material = null);
            root.PropertyListChanged -= fail;
            Check(root.Material is null && lists == 3 && child.CanvasMaterial is null,
                "Failed material-list callback observes the committed null assignment.");
            root.Material = material;
            using var disposed = new CanvasItemMaterial();
            disposed.Dispose();
            Reject<ObjectDisposedException>(() => root.Material = disposed);
            Check(ReferenceEquals(root.Material, material) && lists == 4,
                "A disposed material is rejected without replacing or notifying the previous one.");
            using var externallyDisposed = new CanvasItemMaterial();
            root.Material = externallyDisposed;
            externallyDisposed.Dispose();
            Check(ReferenceEquals(root.Material, externallyDisposed) && root.Material.IsDisposed,
                "A borrowed material remains inspectable after external disposal until replaced.");
            root.Material = material;
            Check(lists == 6, "Replacing an externally disposed material refreshes tooling state.");
            var neutral = new Node { Name = "Neutral" };
            root.AddChild(neutral);
            child.Reparent(neutral, keepGlobalTransform: false);
            Check(child.CanvasMaterial is null, "A neutral parent breaks canvas material inheritance.");
            child.UseParentMaterial = false;
            Check(ReferenceEquals(child.CanvasMaterial, ownMaterial),
                "A child under a neutral parent can still use its own borrowed material.");
            Reject<InvalidOperationException>(() => Task.Run(() => _ = root.Modulate).GetAwaiter().GetResult());
            Reject<InvalidOperationException>(() => Task.Run(() => _ = root.SelfModulate).GetAwaiter().GetResult());
            Reject<InvalidOperationException>(() => Task.Run(() => _ = root.Material).GetAwaiter().GetResult());
            Reject<InvalidOperationException>(() => Task.Run(() => _ = child.UseParentMaterial).GetAwaiter().GetResult());
            Reject<ArgumentException>(() => root.Modulate = new Color(float.NaN, 1f, 1f));
            Reject<ArgumentException>(() => root.SelfModulate = new Color(1f, float.PositiveInfinity, 1f));
            Check(root.Modulate == Colors.White && root.SelfModulate == Colors.White,
                "Invalid modulation leaves both color properties unchanged.");
        }
        Check(!material.IsDisposed && !ownMaterial.IsDisposed,
            "Canvas node disposal does not dispose borrowed materials.");
        using var stored = new Entity
        {
            Name = "StoredAppearance",
            Modulate = new Color(.5f, 1f, 1f),
            SelfModulate = new Color(1f, .5f, 1f),
            Material = material,
            UseParentMaterial = true,
        };
        using var packed = new PackedScene();
        packed.Pack(stored);
        using var copy = (Entity)packed.Instantiate();
        Check(copy.Modulate == stored.Modulate && copy.SelfModulate == stored.SelfModulate &&
            ReferenceEquals(copy.Material, material) && copy.UseParentMaterial,
            "PackedScene stores both modulation values, borrowed material and parent-material policy.");
    }

    private static void PackedHierarchy()
    {
        using var root = new Node { Name = "mixed" };
        var group = new Node { Name = "group" };
        var node = new Entity { Name = "node", Position = new(4, 9), Modulate = Colors.Cyan, ShowBehindParent = true, YSortEnabled = true };
        var timer = new EngineTimer { Name = "timer", WaitTime = 2 };
        root.AddChild(group); group.AddChild(node); root.AddChild(timer);
        group.Owner = root; node.Owner = root; timer.Owner = root;
        using var packed = new PackedScene(); packed.Pack(root);
        using var copy = packed.Instantiate();
        Check(copy.GetType() == typeof(Node) && copy.GetChild(0).GetType() == typeof(Node), "Packed neutral factories.");
        Check(copy.GetNode<Entity>("group/node").Position == node.Position && copy.GetNode<Entity>("group/node").Modulate == node.Modulate &&
            copy.GetNode<EngineTimer>("timer").WaitTime == 2 && copy.GetNode<Entity>("group/node").ShowBehindParent && copy.GetNode<Entity>("group/node").YSortEnabled, "Stored state is contributed by the proper base.");
        Check(copy.GetPropertyList().All(p => p.Name != "Visible" && p.Name != "Position"), "Neutral descriptors exclude canvas state.");
    }

    private static void CallbackFailures()
    {
        using var root = new Entity();
        var first = new Entity { Name = "first" }; var second = new Entity { Name = "second" };
        root.AddChild(first); root.AddChild(second);
        using var tree = new SceneTree(root); tree.ProcessFrame(0);
        first.NotifyTransformChanges = second.NotifyTransformChanges = true;
        var reached = 0;
        Action<CanvasItem> fail = _ => throw new InvalidOperationException("expected");
        first.TransformChanged += fail;
        second.TransformChanged += _ => reached++;
        root.Position = Vector2.One; Reject<AggregateException>(() => tree.ProcessFrame(0));
        Check(reached == 1 && second.GlobalPosition == Vector2.One, "Failed transform callbacks do not skip siblings.");
        first.TransformChanged -= fail;
    }

    private sealed class CustomCanvas : CanvasItem
    {
        private Transform _local = Transform.Identity;
        public override Transform GetTransform() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _local; }
        internal void Move(Vector2 position) { EnsureMutable(); _local.Origin = position; NotifyLocalTransformChanged(); }
    }

    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
