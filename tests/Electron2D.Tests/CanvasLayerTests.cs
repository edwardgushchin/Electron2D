using Mathf = Electron2D.Mathf;
using Electron2D;

internal static class CanvasLayerTests
{
    internal static void Run()
    {
        Configuration(); MembershipAndVisibility(); CoordinatesAndRendering();
        Console.WriteLine("Canvas layer configuration, membership, visibility and coordinates passed.");
    }

    private static void Configuration()
    {
        using var layer = new CanvasLayer();
        Check(layer is Node && layer.Layer == 1 && layer.Visible && !layer.FollowViewportEnabled && layer.FollowViewportScale == 1 && layer.CustomViewport is null, "Default neutral layer policies.");
        Check(layer.Transform == Transform.Identity && layer.Offset == Vector2.Zero && layer.Scale == Vector2.One && layer.Rotation == 0, "Identity components.");
        layer.Layer = int.MinValue; layer.Layer = int.MaxValue;
        Check(RenderingServer.CanvasLayerMin == int.MinValue && RenderingServer.CanvasLayerMax == int.MaxValue, "Published layer bounds cover every Int32 value.");
        layer.Rotation = 7; Check(layer.Rotation == 7, "Explicit rotation is retained without normalization.");
        layer.Transform = new(new(2, 0), new(1, 3), new(4, 5)); Near(layer.Scale, new(2, System.MathF.Sqrt(10))); layer.Offset = layer.Offset;
        Near(layer.Transform.Y, new(0, System.MathF.Sqrt(10))); Check(layer.Transform.Origin == new Vector2(4, 5), "Even an equal component assignment discards skew.");
        layer.Transform = new(new(-2, 0), new(0, 3), Vector2.Zero); Near(layer.Scale, new(2, -3)); Near(layer.Rotation, Mathf.Pi);
        layer.Scale = Vector2.Zero; Check(layer.Transform.Determinant() == 0, "Singular layer transforms are allowed.");
        Reject<ArgumentException>(() => layer.Rotation = float.NaN); Reject<ArgumentException>(() => layer.FollowViewportScale = float.PositiveInfinity);
        Reject<ArgumentException>(() => layer.Offset = new(float.NaN, 0)); Reject<ArgumentException>(() => layer.Scale = new(1, float.NegativeInfinity));
        Reject<ArgumentException>(() => layer.Transform = new(new(float.NaN, 0), Vector2.One, Vector2.Zero));
        layer.Transform = new(new(float.MaxValue, 0), Vector2.Down, Vector2.Zero); Reject<InvalidOperationException>(() => _ = layer.Scale);
        layer.Transform = new(new(2, 0), new(1, 3), new(4, 5)); layer.FollowViewportEnabled = true; layer.FollowViewportScale = -2;
        Near(layer.GetFinalTransform() * Vector2.One, new(-14, -16));
        using var viewport = new TestViewport(); layer.CustomViewport = viewport; layer.Visible = false;
        using var packed = new PackedScene(); packed.Pack(layer); using var copy = (CanvasLayer)packed.Instantiate();
        Check(copy.Transform == layer.Transform && copy.Layer == int.MaxValue && !copy.Visible && copy.FollowViewportEnabled && copy.FollowViewportScale == -2 && copy.CustomViewport is null, "Packing preserves the full matrix and settings, without viewport ownership.");
        Check(layer.GetPropertyList().Single(p => p.Name == "Transform").IsStored && !layer.GetPropertyList().Single(p => p.Name == "Offset").IsStored, "Matrix is the canonical stored transform.");
    }

    private static void MembershipAndVisibility()
    {
        var root = new TestViewport(); var layer = new CanvasLayer(); var direct = new Entity { Name = "Direct" }; var nested = new CanvasLayer(); var nestedItem = new Entity();
        var neutral = new Node(); var independent = new Entity(); var top = new Entity { TopLevel = true };
        root.AddChild(layer); layer.AddChild(direct); direct.AddChild(top); layer.AddChild(neutral); neutral.AddChild(independent); layer.AddChild(nested); nested.AddChild(nestedItem);
        Check(direct.GetCanvasLayerNode() is null, "Detached items have no canvas membership.");
        using var tree = new SceneTree(root);
        Check(direct.GetCanvasLayerNode() == layer && top.GetCanvasLayerNode() == layer && independent.GetCanvasLayerNode() == layer && nestedItem.GetCanvasLayerNode() == nested, "Nearest layer membership crosses neutral and TopLevel boundaries but stops at a nested layer.");
        var events = new List<string>(); layer.VisibilityChanged += _ => events.Add("layer"); direct.VisibilityChanged += _ => events.Add("direct"); direct.Hidden += _ => events.Add("hidden"); top.VisibilityChanged += _ => events.Add("top");
        layer.Hide(); Check(events.SequenceEqual(new[] { "layer", "direct", "hidden", "top" }), "Layer event precedes direct canvas visibility propagation.");
        Check(!direct.IsVisibleInTree && !top.IsVisibleInTree && independent.IsVisibleInTree && nestedItem.IsVisibleInTree, "Visibility stops at neutral and nested layer boundaries.");
        events.Clear(); layer.Hide(); Check(events.Count == 0, "Repeated visibility writes are silent.");
        Action<CanvasLayer> fail = _ => throw new ApplicationException("layer visibility"); layer.VisibilityChanged += fail;
        Reject<AggregateException>(layer.Show); Check(direct.IsVisibleInTree && top.IsVisibleInTree, "Throwing layer handlers still reconcile children."); layer.VisibilityChanged -= fail;
        Action<CanvasLayer> reenter = l => { if (!l.Visible) l.Show(); }; layer.VisibilityChanged += reenter; layer.Hide(); Check(layer.Visible && direct.IsVisibleInTree, "Reentry uses the latest committed visibility."); layer.VisibilityChanged -= reenter;
        direct.Reparent(root); Check(direct.GetCanvasLayerNode() is null && top.GetCanvasLayerNode() is null, "Reparenting refreshes the whole moved canvas subtree.");
        direct.Reparent(nested); Check(direct.GetCanvasLayerNode() == nested && top.GetCanvasLayerNode() == nested, "Reparenting adopts a different canvas.");
        direct.Notify(CanvasItem.NotificationExitCanvas); Check(direct.GetCanvasLayerNode() == nested, "Manual notification does not remove actual membership.");
        using var otherRoot = new TestViewport(); using var otherTree = new SceneTree(otherRoot);
        Reject<NotSupportedException>(() => layer.CustomViewport = otherRoot); Check(layer.CustomViewport is null && layer.CanvasViewport == root, "Cross-tree retargeting fails before mutation.");
        layer.CustomViewport = root; using var node = new Node(); layer.CustomViewport = node; Check(layer.CustomViewport is null && layer.CanvasViewport == root, "Non-viewport target restores default.");
        var captured = root.BeginSceneCapture(); try { Reject<InvalidOperationException>(() => layer.Offset = Vector2.One); } finally { Node.EndSceneCapture(captured); }
        Task.Run(() => { Reject<InvalidOperationException>(() => layer.GetFinalTransform()); Reject<InvalidOperationException>(() => layer.Visible = false); Reject<InvalidOperationException>(() => direct.GetCanvasLayerNode()); }).GetAwaiter().GetResult();
        root.RemoveChild(layer); Check(layer.CanvasViewport is null && direct.GetCanvasLayerNode() is null, "Exit drops runtime membership."); root.AddChild(layer);
        layer.Dispose(); Check(direct.IsDisposed && root.ChildCount == 0, "Layer disposal releases owned subtree."); Reject<ObjectDisposedException>(() => layer.GetFinalTransform());
        var failingLayer = new CanvasLayer(); var failingItem = new FaultItem(); failingLayer.AddChild(failingItem); root.AddChild(failingLayer);
        Reject<AggregateException>(() => root.RemoveChild(failingLayer)); Check(failingItem.GetCanvasLayerNode() is null && failingLayer.CanvasViewport is null, "Throwing canvas-exit callbacks still release both associations."); failingLayer.Dispose();
        using var noViewport = new CanvasLayer(); Reject<AggregateException>(() => new SceneTree(noViewport)); Check(noViewport.Tree is null && !noViewport.IsDisposed, "Activation without a viewport rolls back ownership.");
    }

    private static void CoordinatesAndRendering()
    {
        var root = new TestViewport { CanvasTransform = new(0, new(10, 0)), GlobalCanvasTransform = new(0, new(5, 0)) };
        var layer = new CanvasLayer { Offset = new(3, 0) }; var item = new Entity { Position = new(2, 1) }; root.AddChild(layer); layer.AddChild(item);
        using var tree = new SceneTree(root);
        Near(item.GetGlobalTransform().Origin, new(2, 1)); Near(item.GetGlobalTransformWithCanvas().Origin, new(5, 1)); Near(item.GetViewportTransform().Origin, new(8, 0));
        Near(item.MakeCanvasPositionLocal(new(6, 2)), Vector2.One);
        using var input = new InputEventMouseMotion { Position = new(6, 2), Relative = new(2, 3) }; using var local = (InputEventMouseMotion)item.MakeInputLocal(input); Near(local.Position, Vector2.One); Near(local.Relative, new(2, 3));
        Near(root.GetCanvasRenderTransform(layer).Origin, new(8, 0));
        layer.FollowViewportEnabled = true; layer.FollowViewportScale = 2;
        Near(layer.GetFinalTransform().Origin, new(16, 0)); Near(root.GetCanvasRenderTransform(layer).Origin, new(-14, -40));
        layer.FollowViewportScale = -1; Near(layer.GetFinalTransform().Origin, new(7, 0)); Near(root.GetCanvasRenderTransform(layer).Origin, new(82, 80));
        layer.FollowViewportScale = 0; Near(layer.GetFinalTransform().Origin, new(10, 0)); Near(root.GetCanvasRenderTransform(layer).Origin, new(50, 40)); Reject<InvalidOperationException>(() => item.MakeCanvasPositionLocal(Vector2.Zero));
        root.SnapTransformsToPixel = true; Near(root.GetCanvasRenderTransform(layer).Origin, new(50, 40));
        layer.FollowViewportScale = 1; root.GlobalCanvasTransform = Transform.Identity; root.CanvasTransform = new(0, new(.5f, -.5f)); layer.Offset = new(.5f, -.5f);
        Near(root.GetCanvasRenderTransform(layer).Origin, new(0, -2)); Near(root.GetCanvasRenderTransform(null).Origin, new(0, -1));
        root.Size = new(101, 81); Near(root.GetCanvasRenderTransform(layer).Origin, new(2, 0)); Near(root.GetCanvasRenderTransform(null).Origin, new(1, 0));
        root.SnapTransformsToPixel = false; layer.FollowViewportScale = float.MaxValue; layer.Offset = new(2, 0); Reject<InvalidOperationException>(() => layer.GetFinalTransform()); Reject<InvalidOperationException>(() => root.GetCanvasRenderTransform(layer));
        layer.FollowViewportScale = 1;
        for (var i = 0; i < 1000; i++) { _ = item.GetCanvasTransform(); _ = root.GetCanvasRenderTransform(layer); }
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 1000; i++) { _ = item.GetCanvasTransform(); _ = root.GetCanvasRenderTransform(layer); }
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Warm layer coordinates allocate zero managed bytes.");
    }

    private sealed class TestViewport : Viewport { internal Vector2 Size = new(100, 80); public override Rect2 GetVisibleRect() => new(Vector2.Zero, Size); }
    private sealed class FaultItem : Entity { protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationExitCanvas) throw new ApplicationException("layer exit"); } }
    private static void Near(Vector2 actual, Vector2 expected) => Check(actual.IsEqualApprox(expected), $"Expected {expected}, got {actual}.");
    private static void Near(float actual, float expected) => Check(Mathf.IsEqualApprox(actual, expected), $"Expected {expected}, got {actual}.");
    private static void Check(bool ok, string message) { if (!ok) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
