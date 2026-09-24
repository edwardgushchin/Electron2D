using Electron2D;

internal static class ParallaxTests
{
    internal static void Run()
    {
        using (var detached = new Parallax())
        {
            Check(detached.Autoscroll == Vector2.Zero && detached.FollowViewport && !detached.IgnoreCameraScroll &&
                  detached.LimitBegin == new Vector2(-10_000_000, -10_000_000) &&
                  detached.LimitEnd == new Vector2(10_000_000, 10_000_000) &&
                  detached.RepeatSize == Vector2.Zero && detached.RepeatTimes == 1 &&
                  detached.ScreenOffset == Vector2.Zero && detached.ScrollOffset == Vector2.Zero &&
                  detached.ScrollScale == Vector2.One, "Parallax defaults.");
            Reject<ArgumentOutOfRangeException>(() => detached.ScrollScale = new(float.NaN, 1));
            Reject<ArgumentOutOfRangeException>(() => detached.RepeatSize = new(float.PositiveInfinity, 0));
            detached.RepeatSize = new(-3, 20); detached.RepeatTimes = -4;
            Check(detached.RepeatSize == new Vector2(0, 20) && detached.RepeatTimes == 1, "Repeat settings clamp.");
            detached.RepeatSize = new(20, 0); detached.RepeatTimes = 3; detached.ScrollScale = new(.5f, 1);
            using var packed = new PackedScene(); packed.Pack(detached);
            using var copy = (Parallax)packed.Instantiate();
            Check(copy.RepeatSize == detached.RepeatSize && copy.RepeatTimes == 3 && copy.ScrollScale == detached.ScrollScale &&
                  !copy.GetPropertyList().Any(property => property.Name == nameof(Entity.Position)), "Packed configuration omits calculated position.");
        }

        var root = new TestViewport();
        var camera = new Camera { AnchorMode = Camera.AnchorModeEnum.FixedTopLeft, LimitEnabled = false, Position = new(20, 0) };
        var parallax = new Parallax { ScrollScale = new(.5f, 1) };
        root.AddChild(camera); root.AddChild(parallax);
        using (var tree = new SceneTree(root))
        {
            camera.ForceUpdateScroll();
            Near(parallax.ScreenOffset, new(20, 0)); Near(parallax.Position, new(10, 0));
            parallax.ScrollOffset = new(5, 0); Near(parallax.Position, new(15, 0));
            parallax.IgnoreCameraScroll = true; camera.Position = new(30, 0); camera.ForceUpdateTransform();
            Near(parallax.ScreenOffset, new(20, 0)); Near(parallax.Position, new(15, 0));
            parallax.FollowViewport = false; parallax.ScrollOffset = new(6, 0); Near(parallax.Position, new(-4, 0));
            parallax.IgnoreCameraScroll = false; camera.ForceUpdateScroll(); Near(parallax.ScreenOffset, new(30, 0));
            Near(parallax.Position, new(-9, 0));
            parallax.LimitBegin = Vector2.Zero; parallax.LimitEnd = new(120, 80);
            parallax.ScreenOffset = new(80, 0); Near(parallax.Position, new(-4, 0));
            parallax.FollowViewport = true; parallax.RepeatSize = new(20, 0);
            Near(parallax.Position, new(76, 0));
            parallax.IgnoreCameraScroll = true; parallax.Autoscroll = new(3, 0); tree.Process(1);
            Near(parallax.ScrollOffset, new(9, 0)); Near(parallax.Position, new(79, 0));
            parallax.RepeatSize = Vector2.Zero; var previous = parallax.ScrollOffset;
            tree.Process(1); Near(parallax.ScrollOffset, previous);
            Reject<InvalidOperationException>(() => System.Threading.Tasks.Task.Run(() => parallax.ScrollOffset = Vector2.Zero).GetAwaiter().GetResult());
            parallax.IgnoreCameraScroll = false; var beforeDetach = parallax.ScreenOffset;
            root.RemoveChild(parallax); camera.Position = new(45, 0); camera.ForceUpdateTransform();
            Near(parallax.ScreenOffset, beforeDetach);
            root.AddChild(parallax); camera.ForceUpdateScroll(); Near(parallax.ScreenOffset, new(45, 0));
        }
        var failureRoot = new TestViewport();
        var failureCamera = new Camera { AnchorMode = Camera.AnchorModeEnum.FixedTopLeft, LimitEnabled = false };
        var failing = new Parallax { Name = "Failing", NotifyLocalTransformChanges = true };
        var succeeding = new Parallax { Name = "Succeeding" };
        failureRoot.AddChild(failureCamera); failureRoot.AddChild(failing); failureRoot.AddChild(succeeding);
        using (var tree = new SceneTree(failureRoot))
        {
            failing.LocalTransformChanged += _ => throw new InvalidOperationException("Parallax callback probe.");
            failureCamera.Position = new(20, 0);
            Reject<AggregateException>(failureCamera.ForceUpdateTransform);
            Near(succeeding.ScreenOffset, new(20, 0));
        }
        Console.WriteLine("Parallax camera, limits, repetition, automatic scroll, packing and ownership passed.");
    }

    private sealed class TestViewport : Viewport { public override Rect2 GetVisibleRect() => new(0, 0, 100, 80); }
    private static void Near(Vector2 actual, Vector2 expected) => Check(actual.IsEqualApprox(expected), $"Expected {expected}, got {actual}.");
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}

internal static partial class RenderingRuntimeTests
{
    private static void VerifyParallax(string backend)
    {
        var window = new Window { Size = new(64, 32) };
        var camera = new Camera { AnchorMode = Camera.AnchorModeEnum.FixedTopLeft, LimitEnabled = false, Position = new(4, 0) };
        var parallax = new Parallax { RepeatSize = new(16, 0), ScrollScale = Vector2.Zero };
        var mark = new CanvasNode { DrawAction = node => node.DrawRect(new(0, 0, 3, 3), Colors.Red) };
        var nested = new Parallax { RepeatSize = new(8, 0), ScrollScale = Vector2.Zero, IgnoreCameraScroll = true, FollowViewport = false };
        nested.AddChild(new CanvasNode { Position = new(0, 16), DrawAction = node => node.DrawRect(new(0, 0, 3, 3), Colors.Green) });
        var scaled = new Parallax { Name = "Scaled", RepeatSize = new(16, 0), ScrollScale = Vector2.Zero, Scale = new(2, 1), YSortEnabled = true };
        scaled.AddChild(new CanvasNode { Position = new(0, 8), DrawAction = node => node.DrawRect(new(0, 0, 3, 3), Colors.Blue) });
        parallax.AddChild(mark); parallax.AddChild(nested);
        window.AddChild(camera); window.AddChild(parallax); window.AddChild(scaled);
        var frames = 0;
        window.Ready += _ =>
        {
            camera.ForceUpdateScroll();
            var server = RenderingServer.Instance!; server.SetDefaultClearColor(Colors.Black);
            server.FramePostDraw += () =>
            {
                using var pixels = server.Readback(); frames++;
                Pixel(pixels, 1, 1, Colors.Red); Pixel(pixels, 17, 1, Colors.Red);
                Pixel(pixels, 33, 1, frames == 1 ? Colors.Black : Colors.Red);
                Pixel(pixels, 8, 1, Colors.Black);
                Pixel(pixels, 1, 17, Colors.Green); Pixel(pixels, 9, 17, Colors.Green); Pixel(pixels, 17, 17, Colors.Black);
                Pixel(pixels, 1, 9, Colors.Blue); Pixel(pixels, 33, 9, Colors.Blue); Pixel(pixels, 17, 9, Colors.Black);
                if (frames == 1)
                {
                    camera.Position = new(8, 0); camera.ForceUpdateTransform();
                    parallax.RepeatTimes = 3;
                }
                else window.Tree!.Quit();
            };
        };
        Engine.Instance.Run(window); Released(window);
        Check(frames == 2 && mark.Draws == 1, "Repeated canvas uses retained drawing after camera motion.");
        Console.WriteLine($"Parallax native repeat pixels passed: {backend}.");
    }
}
