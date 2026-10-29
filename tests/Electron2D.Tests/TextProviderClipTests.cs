using Electron2D;

internal static class TextProviderClipTests
{
    internal static void Run(string backend)
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, backend);
        ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var root = new Window { Size = new(160, 140) };
        var parent = new Control { Position = new(10, 10), Size = new(80, 80), ClipContents = true };
        var canvas = new Entity(); parent.AddChild(canvas); root.AddChild(parent);
        canvas.Draw += item =>
        {
            using (item.BeginTextRecordingClip(new(10, 10, 40, 40)))
            {
                item.DrawSetTransform(new(20, 0));
                try
                {
                    using var inner = item.BeginTextRecordingClip(new(0, 0, 40, 40));
                    item.DrawRect(new(-100, -100, 200, 200), Colors.Red);
                    throw new CallbackFailure();
                }
                catch (CallbackFailure) { }
                item.DrawRect(new(-20, 30, 100, 5), Colors.Green);
            }
            item.DrawSetTransformMatrix(Transform.Identity);
            using (item.BeginTextRecordingClip(new(10, 10, 5, 5))) item.DrawAnimationSlice(1, 1, 1);
            item.DrawEndAnimation();
            item.DrawRect(new(60, 60, 10, 10), Colors.Blue);
        };
        var rotated = new Entity { Position = new(115, 20), Rotation = MathF.PI / 4 };
        root.AddChild(rotated);
        rotated.Draw += item =>
        {
            using var clip = item.BeginTextRecordingClip(new(0, 0, 20, 10));
            item.DrawRect(new(-100, -100, 200, 200), Colors.Yellow);
        };
        void Capture()
        {
            using var image = root.GetTexture().GetImage() ?? throw new InvalidOperationException("Text clip readback is unavailable.");
            Check(image.GetPixel(32, 25).R > .9f, "Nested clips intersect in output coordinates after the drawing transform.");
            Check(image.GetPixel(65, 25).R < .1f, "The transformed inner clip cannot extend the outer clip.");
            Check(image.GetPixel(25, 42).G > .4f && image.GetPixel(75, 42).G < .1f, "A callback failure restores the outer clip independently of the current drawing transform.");
            Check(image.GetPixel(75, 75).B > .9f, "Leaving the final scope restores ordinary parent clipping.");
            Check(image.GetPixel(110, 25).R > .9f && image.GetPixel(105, 25).R < .1f && image.GetPixel(130, 25).R < .1f, "Rotated clips use the canvas output bounds.");
            if (Environment.GetEnvironmentVariable("ELECTRON2D_CAPTURE_DIR") is { } directory)
                image.SavePNG(System.IO.Path.Combine(directory, "provider-clipping-" + backend + ".png"));
            root.Tree!.Quit();
        }
        root.Ready += _ => { RenderingServer.SetDefaultClearColor(Colors.Black); RenderingServer.FramePostDraw += Capture; };
        try { Engine.Run(root); }
        finally { if (RenderingServer.IsAvailable) RenderingServer.FramePostDraw -= Capture; }
        Console.WriteLine("Provider text clipping: transformed/nested scopes, callback failure restoration, parent clipping and rotated output bounds passed on " + backend + ".");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private sealed class CallbackFailure : Exception;
}
