using MathF = Electron2D.MathF;
using Electron2D;

internal static class CameraTests
{
    internal static void Run()
    {
        DefaultsAndPacking(); CoordinatesAndLimits(); ParentTracking(); DragAndSmoothing(); MembershipAndFailures();
        Console.WriteLine("Camera configuration, tracking, smoothing and viewport ownership checks passed.");
    }

    private static void DefaultsAndPacking()
    {
        using var camera = new Camera();
        Check(camera.Enabled && camera.IgnoreRotation && camera.NotifyTransformChanges && camera.AnchorMode == Camera.AnchorModeEnum.DragCenter && camera.ProcessCallback == Camera.CameraProcessCallback.Idle, "Default camera policies.");
        Check(camera.Zoom == Vector2.One && camera.Offset == Vector2.Zero && camera.LimitEnabled && !camera.LimitSmoothed && !camera.PositionSmoothingEnabled && !camera.RotationSmoothingEnabled && camera.PositionSmoothingSpeed == 5 && camera.RotationSmoothingSpeed == 5, "Default tracking and smoothing.");
        foreach (var side in Enum.GetValues<Side>()) Check(camera.GetDragMargin(side) == .2f && camera.GetLimit(side) == (side <= Side.Top ? -10_000_000 : 10_000_000), "Default indexed properties.");
        Check(camera.GetTargetPosition() == Vector2.Zero && camera.GetScreenCenterPosition() == Vector2.Zero && camera.GetScreenRotation() == 0 && !camera.IsCurrent(), "Detached queries.");
        camera.ForceUpdateScroll(); camera.ResetSmoothing(); Reject<InvalidOperationException>(camera.Align); Reject<InvalidOperationException>(camera.MakeCurrent);
        Reject<ArgumentOutOfRangeException>(() => camera.Zoom = new(0, 1)); Reject<ArgumentOutOfRangeException>(() => camera.Zoom = new(MathF.Epsilon / 2, 1));
        Reject<ArgumentOutOfRangeException>(() => camera.Offset = new(float.NaN, 0)); Reject<ArgumentOutOfRangeException>(() => camera.PositionSmoothingSpeed = float.PositiveInfinity);
        Reject<ArgumentOutOfRangeException>(() => camera.AnchorMode = (Camera.AnchorModeEnum)4); Reject<ArgumentOutOfRangeException>(() => camera.ProcessCallback = (Camera.CameraProcessCallback)4);
        Reject<ArgumentOutOfRangeException>(() => camera.SetLimit((Side)4, 0)); Reject<ArgumentOutOfRangeException>(() => camera.GetDragMargin((Side)(-1)));
        Reject<ArgumentOutOfRangeException>(() => camera.SetDragMargin(Side.Top, float.NaN));
        camera.PositionSmoothingSpeed = -2; camera.RotationSmoothingSpeed = -3; Check(camera.PositionSmoothingSpeed == 0 && camera.RotationSmoothingSpeed == 0, "Negative finite smoothing speeds clamp to zero.");
        camera.Zoom = new(-2, 3); camera.Offset = new(4, 5); camera.DragLeftMargin = 2; camera.DragVerticalOffset = -3; camera.LimitLeft = 9; camera.ProcessCallback = Camera.CameraProcessCallback.Physics;
        using var viewport = new TestViewport(); camera.CustomViewport = viewport;
        var ownNames = new[] { "Zoom", "Offset", "Enabled", "AnchorMode", "LimitLeft", "ProcessCallback", "DragLeftMargin", "CustomViewport" };
        var descriptors = camera.GetPropertyList().Where(p => ownNames.Contains(p.Name)).ToArray();
        Check(descriptors.Length == ownNames.Length && descriptors.Single(p => p.Name == "CustomViewport").IsStored == false, "Typed camera configuration and runtime viewport metadata.");
        using var packed = new PackedScene(); packed.Pack(camera); using var copy = (Camera)packed.Instantiate();
        Check(copy.Zoom == camera.Zoom && copy.Offset == camera.Offset && copy.DragLeftMargin == 2 && copy.DragVerticalOffset == -3 && copy.LimitLeft == 9 && copy.ProcessCallback == Camera.CameraProcessCallback.Physics && copy.CustomViewport is null && !copy.IsCurrent(), "PackedScene copies camera configuration, never runtime ownership.");
    }

    private static void CoordinatesAndLimits()
    {
        var root = new TestViewport(); var camera = new Camera { Position = new(10, 20), LimitEnabled = false }; root.AddChild(camera);
        using var tree = new SceneTree(root);
        Check(root.GetCamera() == camera, "First enabled camera owns the viewport."); Near(root.CanvasTransform * camera.GlobalPosition, new(50, 40)); Near(camera.GetScreenCenterPosition(), new(10, 20));
        camera.Position = new(20, 30); camera.ForceUpdateTransform(); Near(root.CanvasTransform * Vector2.Zero, new(30, 10)); Near(camera.GetTargetPosition(), new(20, 30));
        camera.Zoom = new(2, 4); Near(root.CanvasTransform * new Vector2(21, 31), new(52, 44));
        camera.IgnoreRotation = false; camera.Rotation = MathF.Pi / 2; camera.ForceUpdateTransform(); Near(root.CanvasTransform * new Vector2(20, 31), new(52, 40)); Near(camera.GetScreenCenterPosition(), new(20, 30)); Near(camera.GetScreenRotation(), MathF.Pi / 2);
        camera.IgnoreRotation = true; Check(camera.GetScreenRotation() == 0, "Ignored rotation resets the cached angle.");
        camera.Zoom = new(-2, 4); Near(root.CanvasTransform * new Vector2(21, 31), new(48, 44));
        camera.Rotation = 0; camera.ForceUpdateTransform(); camera.IgnoreRotation = false; Near(camera.GetScreenCenterPosition(), new(70, 10)); camera.IgnoreRotation = true;
        camera.Zoom = Vector2.One; camera.AnchorMode = Camera.AnchorModeEnum.FixedTopLeft; Near(root.CanvasTransform * camera.Position, Vector2.Zero); Near(camera.GetScreenCenterPosition(), new(70, 70));
        camera.LimitLeft = 0; camera.LimitTop = 0; camera.LimitRight = 40; camera.LimitBottom = 20; camera.LimitEnabled = true;
        Near(camera.GetScreenCenterPosition(), new(70, 60));
        camera.AnchorMode = Camera.AnchorModeEnum.DragCenter; Near(camera.GetScreenCenterPosition(), new(20, 10));
        camera.Offset = new(3, 4); Near(camera.GetScreenCenterPosition(), new(23, 14));
        camera.LimitLeft = 40; camera.LimitRight = -40; Near(camera.GetScreenCenterPosition(), new(3, 14));
        root.GlobalCanvasTransform = new(0, new(6, 7)); camera.ForceUpdateScroll(); Check(root.GlobalCanvasTransform.Origin == new Vector2(6, 7), "Camera only changes the default canvas transform.");
        camera.LimitLeft = int.MaxValue; camera.LimitRight = int.MinValue; Near(camera.GetScreenCenterPosition().X, 2.5f);
        camera.LimitEnabled = false; camera.Offset = Vector2.Zero;
        camera.Position = new(float.MaxValue, 0); camera.ForceUpdateTransform(); var before = root.CanvasTransform;
        Reject<InvalidOperationException>(() => camera.Offset = new(float.MaxValue, 0)); Check(root.CanvasTransform == before, "Overflow preserves the last valid viewport matrix.");
        camera.NotifyTransformChanges = false; camera.Position = new(float.MaxValue / 2, 0); var targetBefore = camera.GetTargetPosition();
        Reject<InvalidOperationException>(camera.Align); Check(camera.GetTargetPosition() == targetBefore && root.CanvasTransform == before, "Failed alignment preserves the previous target and view.");
        camera.Offset = Vector2.Zero; camera.NotifyTransformChanges = true; camera.Position = Vector2.Zero; camera.ForceUpdateTransform();
        camera.Enabled = false; camera.DragHorizontalOffset = float.MaxValue; targetBefore = camera.GetTargetPosition();
        Reject<InvalidOperationException>(camera.Align); Check(camera.GetTargetPosition() == targetBefore, "Inactive alignment cannot publish a nonfinite target.");
        camera.DragHorizontalOffset = 0;
    }

    private static void ParentTracking()
    {
        var root = new TestViewport(); var parent = new Entity { Position = new(7, 9), Rotation = MathF.Pi / 2, Scale = new(2, 2) };
        var camera = new Camera { Position = new(3, 4), LimitEnabled = false }; root.AddChild(parent); parent.AddChild(camera);
        using var tree = new SceneTree(root); Near(camera.GetScreenCenterPosition(), new(-1, 15));
        camera.IgnoreRotation = false; Near(camera.GetScreenRotation(), MathF.Pi / 2);
        parent.Position = new(10, 20); camera.ForceUpdateTransform(); Near(camera.GetScreenCenterPosition(), new(2, 26));
        camera.TopLevel = true; camera.ForceUpdateTransform(); Near(camera.GetScreenCenterPosition(), new(3, 4)); Near(camera.GetScreenRotation(), 0);
        camera.Visible = false; camera.Position = new(4, 5); camera.ForceUpdateTransform(); Near(camera.GetScreenCenterPosition(), new(4, 5));
        camera.NotifyTransformChanges = false; camera.Position = new(8, 9); Near(camera.GetScreenCenterPosition(), new(4, 5));
        tree.Process(0); Near(camera.GetScreenCenterPosition(), new(8, 9));
        Check(camera.IsCurrent() && !camera.ProcessEnabled, "Visibility and public processing flags do not disable internal camera tracking.");
    }

    private static void DragAndSmoothing()
    {
        var root = new TestViewport(); var camera = new Camera { LimitEnabled = false, DragHorizontalEnabled = true, DragVerticalEnabled = true }; root.AddChild(camera);
        using var tree = new SceneTree(root);
        camera.Position = new(5, 5); camera.ForceUpdateTransform(); Near(camera.GetTargetPosition(), Vector2.Zero);
        camera.Position = new(30, 20); camera.ForceUpdateTransform(); Near(camera.GetTargetPosition(), new(20, 12));
        camera.Align(); Near(camera.GetTargetPosition(), new(30, 20));
        camera.Zoom = new(2, 2); camera.Position = new(50, 40); camera.ForceUpdateTransform(); Near(camera.GetTargetPosition(), new(45, 36));
        camera.DragHorizontalOffset = 1; Near(camera.GetTargetPosition(), new(60, 36)); Check(camera.DragHorizontalOffset == 1, "Automatic drag never rewrites the configured offset.");
        camera.DragHorizontalEnabled = camera.DragVerticalEnabled = false; camera.DragHorizontalOffset = 0; camera.Zoom = Vector2.One; camera.Position = Vector2.Zero; camera.ForceUpdateTransform();
        camera.PositionSmoothingEnabled = true; camera.Position = new(100, 0); Near(camera.GetScreenCenterPosition(), Vector2.Zero);
        tree.Process(.1); Near(camera.GetScreenCenterPosition(), new(50, 0)); camera.ForceUpdateScroll(); Near(camera.GetScreenCenterPosition(), new(75, 0));
        camera.ProcessCallback = Camera.CameraProcessCallback.Physics; tree.Process(.1); Near(camera.GetScreenCenterPosition(), new(75, 0)); tree.PhysicsProcess(.1); Near(camera.GetScreenCenterPosition(), new(87.5f, 0));
        camera.ResetSmoothing(); Near(camera.GetScreenCenterPosition(), new(93.75f, 0)); camera.ForceUpdateScroll(); Near(camera.GetScreenCenterPosition(), new(100, 0));
        tree.Paused = true; camera.Position = new(200, 0); tree.PhysicsProcess(.1); Near(camera.GetScreenCenterPosition(), new(100, 0)); tree.Paused = false; tree.PhysicsProcess(.1); Near(camera.GetScreenCenterPosition(), new(150, 0));
        camera.IgnoreRotation = false; camera.RotationSmoothingEnabled = true; camera.Rotation = MathF.Pi / 2; camera.ForceUpdateTransform(); tree.PhysicsProcess(.1); Near(camera.GetScreenRotation(), MathF.Pi / 4);
        camera.Rotation = -MathF.Pi * .9f; tree.PhysicsProcess(.1); Near(camera.GetScreenRotation(), MathF.Pi * .675f);
        camera.PositionSmoothingEnabled = false; camera.RotationSmoothingEnabled = false; camera.IgnoreRotation = true; camera.Position = Vector2.Zero;
        camera.LimitLeft = 0; camera.LimitRight = 100; camera.LimitTop = 0; camera.LimitBottom = 80; camera.LimitSmoothed = true; camera.PositionSmoothingEnabled = true; camera.LimitEnabled = true;
        camera.ResetSmoothing(); camera.ForceUpdateScroll(); Near(camera.GetTargetPosition(), new(50, 40)); Near(camera.GetScreenCenterPosition(), new(50, 40));
        for (var i = 0; i < 1000; i++) tree.PhysicsProcess(.01);
        var allocated = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 1000; i++) tree.PhysicsProcess(.01);
        Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "Warm camera tracking allocates zero managed bytes.");
    }

    private static void MembershipAndFailures()
    {
        var root = new TestViewport(); var first = new Camera { Name = "First", Position = new(1, 0) }; var second = new FaultCamera { Name = "Second", Position = new(2, 0) }; var third = new Camera { Name = "Third", Position = new(3, 0) };
        root.AddChild(first); root.AddChild(second); root.AddChild(third); using var tree = new SceneTree(root);
        second.MakeCurrent(); Check(second.IsCurrent() && !first.IsCurrent(), "Explicit switch.");
        root.MoveChild(third, 0); second.Enabled = false; Check(root.GetCamera() == third, "Disabling selects by current tree order.");
        Near(third.GetScreenCenterPosition(), Vector2.Zero); tree.Process(0); Near(third.GetScreenCenterPosition(), new(3, 0));
        root.RemoveChild(third); Check(root.GetCamera() == first, "Removal releases current camera."); third.Dispose();
        first.Notify(Node.NotificationExitTree); Check(first.IsCurrent(), "Manual lifecycle notifications do not release membership.");
        root.RemoveChild(first); Check(root.GetCamera() is null && root.CanvasTransform == Transform.Identity, "Last eligible camera restores identity.");
        root.AddChild(first); Check(first.IsCurrent(), "Reentry can reclaim the viewport in the same frame.");
        second.Enabled = true; second.MakeCurrent(); second.FailExit = true; Reject<AggregateException>(() => root.RemoveChild(second)); Check(root.GetCamera() == first && second.Tree is null, "Throwing exit callback still releases viewport ownership."); second.Dispose();
        using var otherRoot = new TestViewport(); using var otherTree = new SceneTree(otherRoot);
        Reject<NotSupportedException>(() => first.CustomViewport = otherRoot); Check(first.CustomViewport is null && first.IsCurrent(), "Unsupported retarget rejects before mutation.");
        first.CustomViewport = root; Check(first.CustomViewport == root && first.IsCurrent(), "Explicit own viewport target.");
        using var neutral = new Node(); first.CustomViewport = neutral; Check(first.CustomViewport is null, "Non-viewport node selects the default viewport.");
        var captured = root.BeginSceneCapture(); try { Reject<InvalidOperationException>(() => first.Enabled = false); } finally { Node.EndSceneCapture(captured); }
        Task.Run(() => { Reject<InvalidOperationException>(() => first.GetTargetPosition()); Reject<InvalidOperationException>(() => first.Offset = Vector2.One); }).GetAwaiter().GetResult();
        var disposable = new Camera { CustomViewport = root }; root.AddChild(disposable); disposable.MakeCurrent(); disposable.Dispose();
        Check(root.GetCamera() == first && disposable.IsDisposed, "Direct active-camera disposal releases selection before dropping borrowed references.");
        first.Enabled = false; Reject<InvalidOperationException>(first.MakeCurrent); tree.Dispose(); Reject<ObjectDisposedException>(() => first.GetTargetPosition()); Reject<ObjectDisposedException>(() => root.GetCamera());
        using var unattached = new Camera(); Reject<AggregateException>(() => new SceneTree(unattached)); Check(!unattached.IsDisposed && unattached.Tree is null, "Viewport-less activation rolls back caller ownership.");
    }

    private sealed class TestViewport : Viewport { public override Rect GetVisibleRect() => new(0, 0, 100, 80); }
    private sealed class FaultCamera : Camera { internal bool FailExit; protected override void OnExitTree() { base.OnExitTree(); if (FailExit) throw new ApplicationException("camera exit"); } }
    private static void Near(Vector2 a, Vector2 b) => Check(a.IsEqualApprox(b), $"Expected {b}, got {a}.");
    private static void Near(float a, float b) => Check(MathF.IsEqualApprox(a, b), $"Expected {b}, got {a}.");
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
