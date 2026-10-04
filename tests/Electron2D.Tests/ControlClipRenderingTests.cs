using Electron2D;

internal static partial class RenderingRuntimeTests
{
    private static void VerifyControlClipping(string backend)
    {
        var window = new Window { Size = new(96, 80) };
        var parent = new ClipPainter(Colors.Red)
        {
            Position = new(10, 10),
            Size = new(20, 20),
            ClipContents = false
        };
        var child = new ClipBox(Colors.Blue) { Position = new(15, 0) };
        window.AddChild(parent);
        parent.AddChild(child);

        var outer = new Control { Name = "outer", Position = new(50, 5), Size = new(15, 20), ClipContents = true };
        var inner = new Control { Position = new(5, 5), Size = new(15, 20), ClipContents = true };
        var leaf = new ClipBox(Colors.Yellow) { Position = new(5, 0) };
        window.AddChild(outer); outer.AddChild(inner); inner.AddChild(leaf);
        window.AddChild(new ClipBox(Colors.Green) { Position = new(75, 10) });

        var rotated = new Control { Name = "rotated", Position = new(40, 50), Size = new(12, 10), RotationDegrees = 90, ClipContents = true };
        rotated.AddChild(new ClipBox(Colors.Magenta));
        window.AddChild(rotated);

        var sorted = new Control { Name = "sorted", Position = new(8, 50), Size = new(12, 12), ClipContents = true, YSortEnabled = true };
        sorted.AddChild(new ClipBox(Colors.Cyan) { Position = new(8, 0) });
        window.AddChild(sorted);

        var empty = new ClipPainter(Colors.White) { Name = "empty", Position = new(80, 45), ClipContents = true };
        empty.AddChild(new ClipBox(Colors.White));
        window.AddChild(empty);

        var frames = 0;
        window.Ready += _ =>
        {
            var renderer = RenderingServer.Service!;
            RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                using var image = renderer.Readback();
                frames++;
                try
                {
                    Pixel(image, 7, 7, Colors.Red);
                    Pixel(image, 25, 15, frames == 3 ? Colors.Red : Colors.Blue);
                    Pixel(image, 35, 15, frames is 2 or 4 ? Colors.Red : Colors.Blue);
                    Pixel(image, 62, 15, Colors.Yellow);
                    Pixel(image, 67, 15, Colors.Black);
                    Pixel(image, 77, 15, Colors.Green);
                    Pixel(image, 18, 55, Colors.Cyan);
                    Pixel(image, 25, 55, Colors.Black);
                    Pixel(image, 35, 55, Colors.Magenta);
                    Pixel(image, 45, 55, Colors.Black);
                    Pixel(image, 85, 50, Colors.Black);
                }
                catch (Exception error) { throw new InvalidOperationException($"Control clipping {backend}, frame {frames}.", error); }
                switch (frames)
                {
                    case 1: parent.ClipContents = true; break;
                    case 2: child.TopLevel = true; child.Position = new(30, 10); break;
                    case 3: child.TopLevel = false; child.Position = new(15, 0); break;
                    default: window.Tree!.Quit(); break;
                }
            };
        };
        Engine.Run(window);
        Check(frames == 4, "Control clipping rendered all four policy transitions.");
        Released(window);
        Console.WriteLine($"Control clipping pixels passed: {backend}.");
    }

    private static void VerifyRepeatedControlClipping(string backend)
    {
        var window = new Window { Size = new(64, 32) };
        var parallax = new Parallax
        {
            RepeatSize = new(20, 0),
            RepeatTimes = 1,
            ScrollScale = Vector2.Zero,
            IgnoreCameraScroll = true,
            FollowViewport = false
        };
        var clip = new Control { Position = new(10, 5), Size = new(10, 10), ClipContents = true };
        window.AddChild(parallax); parallax.AddChild(clip); clip.AddChild(new ClipBox(Colors.Cyan));
        var frames = 0;
        window.Ready += _ =>
        {
            var renderer = RenderingServer.Service!;
            RenderingServer.SetDefaultClearColor(Colors.Black);
            RenderingServer.FramePostDraw += () =>
            {
                using var image = renderer.Readback();
                frames++;
                try
                {
                    Pixel(image, 5, 8, Colors.Black);
                    Pixel(image, 15, 8, Colors.Cyan);
                    Pixel(image, 25, 8, Colors.Cyan);
                    Pixel(image, 35, 8, Colors.Cyan);
                    Pixel(image, 45, 8, Colors.Black);
                }
                catch (Exception error) { throw new InvalidOperationException($"Repeated Control clipping {backend}.", error); }
                window.Tree!.Quit();
            };
        };
        Engine.Run(window);
        Check(frames == 1, "Repeated Control clipping rendered one complete frame.");
        Released(window);
    }

    private sealed class ClipPainter(Color color) : Control
    {
        protected override void OnDraw() => DrawRect(new Rect2(-5, -5, 40, 40), color);
    }

    private sealed class ClipBox(Color color) : Entity
    {
        protected override void OnDraw() => DrawRect(new Rect2(0, 0, 20, 20), color);
    }
}
