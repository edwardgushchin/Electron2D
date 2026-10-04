using Electron2D;

internal static class LegacyParallaxTests
{
    internal static void Run()
    {
        using (var detached = new ParallaxBackground())
        using (var layer = new ParallaxLayer())
        {
            Check(detached.Layer == -100 && detached.ScrollOffset == Vector2.Zero &&
                  detached.ScrollBaseOffset == Vector2.Zero && detached.ScrollBaseScale == Vector2.One &&
                  detached.ScrollLimitBegin == Vector2.Zero && detached.ScrollLimitEnd == Vector2.Zero &&
                  !detached.ScrollIgnoreCameraZoom, "Background defaults.");
            Check(layer.MotionOffset == Vector2.Zero && layer.MotionScale == Vector2.One &&
                  layer.MotionMirroring == Vector2.Zero && layer.GetConfigurationWarnings().Length != 0,
                "Layer defaults and parent warning.");
            layer.MotionMirroring = new(-5, 12);
            Check(layer.MotionMirroring == new Vector2(0, 12), "Negative mirror intervals clamp.");
            Reject<ArgumentOutOfRangeException>(() => detached.ScrollBaseScale = new(float.NaN, 1));
            Reject<ArgumentOutOfRangeException>(() => layer.MotionScale = new(float.PositiveInfinity, 1));
        }

        var root = new TestViewport();
        var camera = new Camera { AnchorMode = AnchorMode.FixedTopLeft, LimitEnabled = false, Position = new(20, 0) };
        var background = new ParallaxBackground();
        var layerNode = new ParallaxLayer { Position = new(4, 6), Scale = new(2, 1), MotionScale = new(.5f, 1), MotionOffset = new(3, 0) };
        background.AddChild(layerNode); layerNode.Owner = background; root.AddChild(camera); root.AddChild(background);
        using (var tree = new SceneTree(root))
        {
            camera.ForceUpdateScroll();
            Near(layerNode.Position, new(-3, 6)); Near(layerNode.Scale, new(2, 1));
            Check(layerNode.GetConfigurationWarnings().Length == 0, "A direct background parent is valid.");
            camera.Position = new(30, 0); camera.ForceUpdateTransform(); Near(layerNode.Position, new(-8, 6));
            background.ScrollBaseOffset = new(2, 0); Near(layerNode.Position, new(-7, 6));
            background.ScrollBaseOffset = Vector2.Zero;
            background.ScrollLimitBegin = new(-10, 0); background.ScrollLimitEnd = new(120, 0);
            background.ScrollOffset = new(30, 0); Near(layerNode.Position, new(12, 6));
            background.ScrollLimitBegin = Vector2.Zero; background.ScrollLimitEnd = Vector2.Zero;
            camera.ForceUpdateScroll(); Near(layerNode.Position, new(-8, 6));
            background.ScrollIgnoreCameraZoom = true; camera.Zoom = new(2, 2); camera.ForceUpdateScroll();
            Near(layerNode.Position, new(-8, 6)); Near(layerNode.Scale, new(2, 1));
            background.ScrollIgnoreCameraZoom = false; camera.ForceUpdateScroll();
            Near(layerNode.Position, new(-16, 12)); Near(layerNode.Scale, new(4, 2));
            using var packed = new PackedScene(); packed.Pack(background);
            using var copy = (ParallaxBackground)packed.Instantiate();
            var copyLayer = (ParallaxLayer)copy.GetChild(0);
            Check(copy.Layer == -100 && copyLayer.Position == new Vector2(4, 6) &&
                  copyLayer.Scale == new Vector2(2, 1) && copyLayer.MotionOffset == new Vector2(3, 0),
                "Packed layers retain original transforms and typed motion settings.");
            var sibling = new ParallaxLayer { Name = "Sibling" };
            background.AddChild(sibling);
            layerNode.NotifyLocalTransformChanges = true;
            var callbacks = 0;
            layerNode.LocalTransformChanged += _ => { if (++callbacks == 1) throw new InvalidOperationException("Injected transform callback failure."); };
            Reject<AggregateException>(() => background.ScrollBaseOffset = new(2, 0));
            Check(callbacks >= 2 && sibling.Position == new Vector2(-58, 0),
                $"Layer scale and sibling movement continue after a position callback failure: callbacks={callbacks}, position={sibling.Position}.");
            background.ScrollBaseOffset = Vector2.Zero;
            Reject<InvalidOperationException>(() => System.Threading.Tasks.Task.Run(() => background.ScrollOffset = Vector2.Zero).GetAwaiter().GetResult());
            background.RemoveChild(layerNode); Near(layerNode.Position, new(4, 6)); Near(layerNode.Scale, new(2, 1));
            background.AddChild(layerNode); camera.ForceUpdateScroll(); Near(layerNode.Position, new(-16, 12));
        }
        Console.WriteLine("Legacy parallax camera motion, zoom, packing, validation and ownership passed.");
    }

    private sealed class TestViewport : Viewport { public override Rect2 GetVisibleRect() => new(0, 0, 100, 80); }
    private static void Near(Vector2 actual, Vector2 expected) => Check(actual.IsEqualApprox(expected), $"Expected {expected}, got {actual}.");
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}

internal static partial class RenderingRuntimeTests
{
    private static void VerifyLegacyParallax(string backend)
    {
        var window = new Window { Size = new(64, 32) };
        var camera = new Camera { AnchorMode = AnchorMode.FixedTopLeft, LimitEnabled = false, Position = new(4, 0) };
        var background = new ParallaxBackground();
        var layer = new ParallaxLayer { MotionMirroring = new(16, 0) };
        var mark = new CanvasNode { DrawAction = node => node.DrawRect(new(0, 0, 3, 3), Colors.Red) };
        var scaled = new ParallaxLayer { Name = "Scaled", Position = new(0, 8), Scale = new(2, 1), MotionScale = Vector2.Zero, MotionMirroring = new(8, 0) };
        scaled.AddChild(new CanvasNode { DrawAction = node => node.DrawRect(new(0, 0, 3, 3), Colors.Blue) });
        layer.AddChild(mark); background.AddChild(layer); window.AddChild(camera); window.AddChild(background);
        background.AddChild(scaled);
        var frames = 0;
        window.Ready += _ =>
        {
            camera.ForceUpdateScroll();
            var server = RenderingServer.Service!; RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                using var pixels = server.Readback(); frames++;
                Pixel(pixels, frames == 1 ? 13 : 9, 1, Colors.Red);
                Pixel(pixels, frames == 1 ? 29 : 25, 1, Colors.Black);
                Pixel(pixels, 1, 1, Colors.Black);
                Pixel(pixels, 1, 9, Colors.Blue); Pixel(pixels, 17, 9, Colors.Blue);
                Pixel(pixels, 25, 9, Colors.Black);
                if (frames == 1) { camera.Position = new(8, 0); camera.ForceUpdateTransform(); }
                else window.Tree!.Quit();
            };
        };
        Engine.Run(window); Released(window);
        Check(frames == 2 && mark.Draws == 1, "Legacy mirroring reuses retained drawing across camera motion.");
        Console.WriteLine($"Legacy parallax repeat pixels passed: {backend}.");
    }
}
